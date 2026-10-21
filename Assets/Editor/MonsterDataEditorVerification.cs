using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Explicit, isolated integration verification, invoked through MCP. No Play Mode or user save access.
public static class MonsterDataEditorVerification
{
    public static string Run()
    {
        MonsterDataStore.Guard();
        var results = new List<string>();
        string folder = "Assets/__MonsterDataVerification_" + Guid.NewGuid().ToString("N");
        var originals = MonsterDataStore.Paths.ToDictionary(p => p, File.ReadAllBytes);
        var draft = MonsterDataStore.Load();
        var objects = new List<UnityEngine.Object>();
        Action<bool, string> check = (ok, name) => { if (!ok) throw new Exception("FAIL: " + name); results.Add("PASS " + name); };
        Action<Action, string> rejects = (action, name) =>
        {
            bool rejected = false;
            try { action(); } catch (FormatException) { rejected = true; }
            check(rejected, name);
        };
        try
        {
            check(draft.Count == MonsterDataStore.Catalog.Sum(s => s.Fields.Length), "all five species serialized fields loaded");
            check(MonsterDataStore.BuildPlan(draft).Changes.Count == 0, "original twelve prefabs already agree; no-op plan");
            string csv = MonsterDataStore.ExportCsv(draft);
            check(MonsterDataStore.ImportCsv("\uFEFF" + csv, draft).All(p => p.Value == draft[p.Key]), "UTF8 BOM / CRLF CSV round-trip");
            var quoted = MonsterDataStore.ImportCsv("monsterId,field,value\n\"Stone\",\"maxHealth\",\"31\"\n", draft);
            check(quoted["Stone.maxHealth"] == "31" && draft["Stone.maxHealth"] == "30", "quoted partial CSV changes draft only");
            foreach (string bad in new[] { "", "monsterId,field,value\n", "id,field,value\nStone,maxHealth,31", "monsterId,field,value\nBoss,maxHealth,31",
                "monsterId,field,value\nStone,missing,1", "monsterId,field,value\nStone,maxHealth,", "monsterId,field,value\nStone,maxHealth,1.5",
                "monsterId,field,value\nStone,maxHealth,-1", "monsterId,field,value\nStone,moveSpeed,NaN", "monsterId,field,value\nStone,moveSpeed,Infinity",
                "monsterId,field,value\nMagnet,slowFraction,1.1", "monsterId,field,value\nStone,jumpDuration,0", "monsterId,field,value\nIron,chargeSpeed,0",
                "monsterId,field,value\nStone,maxHealth,31\nStone,maxHealth,32", "monsterId,field,value\nStone,maxHealth,\"31", "monsterId,field,value\nStone,maxHealth,\"31\"x" })
                rejects(() => MonsterDataStore.ImportCsv(bad, draft), "reject invalid CSV " + results.Count);
            check(MonsterDataStore.Parse(MonsterDataStore.Catalog[0].Fields[0], "2147483647") == int.MaxValue, "integer values retain full precision");
            var invalid = new Dictionary<string, string>(draft) { ["Stone.maxHealth"] = "" };
            rejects(() => MonsterDataStore.BuildPlan(invalid), "invalid draft blocked before mutation");

            AssetDatabase.CreateFolder("Assets", Path.GetFileName(folder));
            var paths = MonsterDataStore.Paths.Select(p => folder + "/" + Path.GetFileName(p)).ToArray();
            for (int i = 0; i < paths.Length; i++) check(AssetDatabase.CopyAsset(MonsterDataStore.Paths[i], paths[i]), "copy " + Path.GetFileName(paths[i]));
            var roots = paths.Select(MonsterDataStore.Root).ToArray();
            objects.AddRange(roots.SelectMany(r => r.GetComponentsInChildren<Component>(true)).Where(c => c).Cast<UnityEngine.Object>());
            var originalStates = objects.ToDictionary(o => o, EditorJsonUtility.ToJson);
            var references = new Dictionary<string, UnityEngine.Object>();
            foreach (var obj in objects)
                using (var so = new SerializedObject(obj))
                {
                    var p = so.GetIterator();
                    while (p.Next(true))
                        if (p.propertyType == SerializedPropertyType.ObjectReference)
                            references[obj.GetInstanceID() + ":" + p.propertyPath] = p.objectReferenceValue;
                }
            var ids = objects.ToDictionary(o => o, o => GlobalObjectId.GetGlobalObjectIdSlow(o).ToString());
            var bytes = paths.ToDictionary(p => p, File.ReadAllBytes);
            check(MonsterDataStore.Apply(MonsterDataStore.BuildPlan(draft, paths)) == 0 && paths.All(p => bytes[p].SequenceEqual(File.ReadAllBytes(p))), "no-op apply writes no files");
            var changed = new Dictionary<string, string>(draft);
            foreach (var s in MonsterDataStore.Catalog)
                foreach (var f in s.Fields)
                {
                    double value = MonsterDataStore.Parse(f, changed[MonsterDataStore.Key(s, f)]);
                    changed[MonsterDataStore.Key(s, f)] = MonsterDataStore.Number(value + (f.Key == "slowFraction" ? .05f : f.Integer ? 1 : .125f));
                }
            var plan = MonsterDataStore.BuildPlan(changed, paths);
            check(plan.ChangedPaths.Length == 12 && plan.Changes.Count > 400, "preview covers twelve copies and all editable fields");
            check(MonsterDataStore.Apply(plan) == 12, "apply saves all twelve copies");
            check(plan.Changes.All(c => MonsterDataStore.Read(c.Target, c.Field) == (c.Field.Integer ? c.After : (float)c.After)), "all requested numeric changes persisted");
            check(objects.All(o => GlobalObjectId.GetGlobalObjectIdSlow(o).ToString() == ids[o]), "internal object IDs unchanged");
            foreach (var obj in objects)
                using (var so = new SerializedObject(obj))
                {
                    var p = so.GetIterator();
                    while (p.Next(true))
                        if (p.propertyType == SerializedPropertyType.ObjectReference)
                            check(p.objectReferenceValue == references[obj.GetInstanceID() + ":" + p.propertyPath], "serialized object reference preserved");
                }
            foreach (var pair in originalStates)
            {
                if (pair.Key is EnemyHealth || pair.Key is IMonsterAI) continue;
                check(EditorJsonUtility.ToJson(pair.Key) == pair.Value, "structure/reference unchanged: " + pair.Key.GetType().Name);
            }
            Undo.PerformUndo();
            check(objects.All(o => EditorJsonUtility.ToJson(o) == originalStates[o]), "Undo restores all serialized components");
            var restoredBytes = paths.ToDictionary(p => p, File.ReadAllBytes);
            foreach (string p in paths) AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
            check(MonsterDataStore.BuildPlan(draft, paths).Changes.Count == 0, "Undo survives disk reimport");
            Undo.PerformRedo();
            check(MonsterDataStore.BuildPlan(changed, paths).Changes.Count == 0, "Redo restores changed values");
            check(paths.Any(p => !restoredBytes[p].SequenceEqual(File.ReadAllBytes(p))), "Redo persists to disk");
            check(MonsterDataStore.BuildPlan(changed, paths).Differences.Count > 400, "saved differences from common prefabs displayed");
            Undo.PerformUndo();
            var stale = MonsterDataStore.BuildPlan(changed, paths);
            var target = stale.Changes[0].Target;
            using (var so = new SerializedObject(target)) { so.FindProperty(stale.Changes[0].Field.Property).intValue = 88; so.ApplyModifiedPropertiesWithoutUndo(); }
            bool blocked = false;
            try { MonsterDataStore.Apply(stale); } catch (InvalidOperationException) { blocked = true; }
            check(blocked, "stale preview blocked");
            check(originals.All(p => p.Value.SequenceEqual(File.ReadAllBytes(p.Key))), "original twelve prefab bytes unchanged");
            return "PASS " + results.Count + " checks\n" + string.Join("\n", results.Where(r => !r.Contains("structure/reference")));
        }
        finally
        {
            foreach (var obj in objects) if (obj) Undo.ClearUndo(obj);
            if (AssetDatabase.IsValidFolder(folder)) AssetDatabase.DeleteAsset(folder);
            if (!originals.All(p => p.Value.SequenceEqual(File.ReadAllBytes(p.Key)))) Debug.LogError("Monster verification: original prefab bytes changed unexpectedly.");
        }
    }
}
