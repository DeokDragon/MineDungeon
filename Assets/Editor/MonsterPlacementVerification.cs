using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Explicit integration checks on a disposable copied map. Never writes original maps or saves.
public static class MonsterPlacementVerification
{
    public static string Run()
    {
        MonsterDataStore.Guard();
        var results = new List<string>();
        Action<bool, string> check = (ok, text) => { if (!ok) throw new Exception("FAIL " + text); results.Add("PASS " + text); };
        string folder = "Assets/__PlacementVerification_" + Guid.NewGuid().ToString("N");
        string path = folder + "/Map_01.prefab";
        var protectedPaths = MonsterDataStore.Paths.Concat(MonsterDataStore.Paths.Select(p => p + ".meta"))
            .Concat(new[] { "Assets/Prefabs/Rooms/Room_Normal_01.prefab", "Assets/Prefabs/Rooms/Room_Normal_02 Variant.prefab", "Assets/Prefabs/Rooms/Room_Boss_Test Variant.prefab", "Assets/Dungeons/Floor1_RockMine.asset" }
                .SelectMany(p => new[] { p, p + ".meta" })).Where(File.Exists).ToArray();
        var bytes = protectedPaths.ToDictionary(p => p, File.ReadAllBytes);
        GameObject root = null;
        try
        {
            foreach (string original in MonsterPlacementStore.Paths)
            {
                var r = AssetDatabase.LoadAssetAtPath<GameObject>(original);
                var before = r.GetComponentsInChildren<Component>(true).Where(c => c).ToDictionary(c => c, c => EditorJsonUtility.ToJson(c));
                var report = MonsterPlacementStore.Validate(r);
                check(report.Errors.Count == 0, Path.GetFileName(original) + " baseline validation: " + string.Join(";", report.Errors));
                check(before.All(p => EditorJsonUtility.ToJson(p.Key) == p.Value), "validation does not mutate map or exit selection");
            }
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
            check(AssetDatabase.CopyAsset(MonsterPlacementStore.Paths[0], path), "isolated map copied");
            root = PrefabUtility.LoadPrefabContents(path);
            var originalEnemies = MonsterPlacementStore.Enemies(root);
            var originalIds = originalEnemies.Select(e => e.GetInstanceID()).ToArray();
            var protectedComponents = root.GetComponentsInChildren<Component>(true).Where(c => c && !c.GetComponentInParent<EnemyHealth>() && !(c is RoomController)).ToDictionary(c => c, c => EditorJsonUtility.ToJson(c));
            var sourceStats = MonsterDataStore.Load();
            var start = originalEnemies[0].transform.localPosition;
            for (int species = 0; species < 5; species++)
            {
                var hp = MonsterPlacementStore.Add(root, species, start + Vector3.right * 2);
                check(MonsterPlacementStore.Enemies(root).Length == 7 && Registered(root), "add / registration " + species);
                check(hp.GetComponent<EnemyReward>().Reward != null && hp.GetComponent<Rigidbody2D>() && hp.GetComponent<Collider2D>() && MonsterPlacementStore.Species(hp) == MonsterDataStore.Catalog[species], "source components retained " + species);
                check(originalEnemies.Select(e => e.GetInstanceID()).SequenceEqual(originalIds), "existing objects retained " + species);
                var s = MonsterDataStore.Catalog[species];
                check(s.Fields.All(f => MonsterDataStore.Read(MonsterDataStore.ComponentFor(hp, s, f), f) == MonsterDataStore.Parse(f, sourceStats[MonsterDataStore.Key(s, f)])), "source balance retained " + species);
                Undo.PerformUndo();
                check(MonsterPlacementStore.Enemies(root).Length == 6 && Registered(root), "add Undo " + species);
                Undo.PerformRedo();
                hp = MonsterPlacementStore.Enemies(root).Last();
                check(MonsterPlacementStore.Enemies(root).Length == 7 && Registered(root), "add Redo " + species);
                var p = hp.transform.localPosition;
                MonsterPlacementStore.Move(root, hp, p + Vector3.up);
                Undo.PerformUndo(); check(hp.transform.localPosition == p, "move Undo " + species);
                Undo.PerformRedo(); check(hp.transform.localPosition == p + Vector3.up, "move Redo " + species);
                MonsterPlacementStore.Delete(root, hp);
                check(MonsterPlacementStore.Enemies(root).Length == 6 && Registered(root), "delete / registration " + species);
                Undo.PerformUndo(); check(MonsterPlacementStore.Enemies(root).Length == 7 && Registered(root), "delete Undo " + species);
                Undo.PerformRedo(); check(MonsterPlacementStore.Enemies(root).Length == 6 && Registered(root), "delete Redo " + species);
            }
            var first = originalEnemies[0]; var old = first.transform.localPosition;
            MonsterPlacementStore.Move(root, first, new Vector3(999, 999, 0));
            check(MonsterPlacementStore.Validate(root).Errors.Any(e => e.Contains("바닥")), "invalid floor position rejected");
            Undo.PerformUndo();
            check(first.transform.localPosition == old, "invalid position Undo");
            using (var so = new SerializedObject(MonsterPlacementStore.Controller(root)))
            {
                var p = so.FindProperty("enemies"); p.arraySize = 3;
                p.GetArrayElementAtIndex(0).objectReferenceValue = first;
                p.GetArrayElementAtIndex(1).objectReferenceValue = first;
                p.GetArrayElementAtIndex(2).objectReferenceValue = null;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            var bad = MonsterPlacementStore.Validate(root);
            check(bad.Errors.Any(e => e.Contains("중복")) && bad.Errors.Any(e => e.Contains("빈 참조")) && bad.Errors.Any(e => e.Contains("누락")), "duplicate / null / missing registration detected");
            MonsterPlacementStore.SyncReferences(root); Undo.FlushUndoRecordObjects();
            check(Registered(root), "registration repair");
            check(protectedComponents.All(p => EditorJsonUtility.ToJson(p.Key) == p.Value), "terrain / entrance / exit / camera components unchanged");
            // Leave one new nested monster in the disposable map and verify disk persistence.
            var added = MonsterPlacementStore.Add(root, 1, start + Vector3.up * 3);
            var local = added.transform.localPosition;
            PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
            check(saved, "isolated map saved");
            foreach (var c in root.GetComponentsInChildren<Component>(true)) if (c) Undo.ClearUndo(c);
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) Undo.ClearUndo(t.gameObject);
            PrefabUtility.UnloadPrefabContents(root); root = null;
            root = PrefabUtility.LoadPrefabContents(path);
            check(Registered(root) && MonsterPlacementStore.Enemies(root).Length == 7 && MonsterPlacementStore.Enemies(root).Last().transform.localPosition == local, "reload retains nested monster position and registration");
            PrefabUtility.UnloadPrefabContents(root); root = null;
            var balance = new Dictionary<string, string>(sourceStats) { ["Iron.attackDamage"] = "16" };
            var balancePlan = MonsterDataStore.BuildPlan(balance, new[] { path });
            check(MonsterDataStore.Apply(balancePlan) == 1 && MonsterDataStore.BuildPlan(balance, new[] { path }).Changes.Count == 0, "phase one stats apply supports newly added nested monsters");
            Undo.PerformUndo();
            check(MonsterDataStore.BuildPlan(sourceStats, new[] { path }).Changes.Count == 0, "phase one nested stats Undo persisted");
            check(protectedPaths.All(p => bytes[p].SequenceEqual(File.ReadAllBytes(p))), "all protected original bytes unchanged");
            check(MonsterDataStore.ImportCsv(MonsterDataStore.ExportCsv(sourceStats), sourceStats).All(p => p.Value == sourceStats[p.Key]), "phase one CSV round-trip retained");
            return string.Join("\n", results);
        }
        finally
        {
            if (root)
            {
                foreach (var c in root.GetComponentsInChildren<Component>(true)) if (c) Undo.ClearUndo(c);
                foreach (var t in root.GetComponentsInChildren<Transform>(true)) Undo.ClearUndo(t.gameObject);
                PrefabUtility.UnloadPrefabContents(root);
            }
            if (AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
        }
    }
    static bool Registered(GameObject root)
    {
        var enemies = MonsterPlacementStore.Enemies(root);
        using (var so = new SerializedObject(MonsterPlacementStore.Controller(root)))
        {
            var p = so.FindProperty("enemies");
            var refs = Enumerable.Range(0, p.arraySize).Select(i => p.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            return refs.Length == enemies.Length && refs.All(r => r) && refs.Distinct().Count() == refs.Length && enemies.All(e => refs.Contains(e));
        }
    }
}
