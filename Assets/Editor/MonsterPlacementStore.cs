using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Only the ten regular cave maps are offered by the UI. No runtime spawning or terrain writes.
internal static class MonsterPlacementStore
{
    internal static string[] Paths => Enumerable.Range(1, 10)
        .Select(i => $"Assets/Prefabs/Rooms/RegularCaves/Map_{i:00}.prefab").ToArray();
    internal static MonsterDataStore.Species Species(EnemyHealth hp) => !hp ? null :
        MonsterDataStore.Catalog.SingleOrDefault(s => hp.GetComponent(s.AI));
    internal static EnemyHealth[] Enemies(GameObject root) => root.GetComponentsInChildren<EnemyHealth>(true);
    internal static PrefabStage Stage(string path)
    {
        MonsterDataStore.Guard();
        if (!Paths.Contains(path)) throw new InvalidOperationException("편집 대상은 Map_01~10뿐입니다.");
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage == null || stage.assetPath != path) throw new InvalidOperationException("선택한 맵을 먼저 Prefab Mode에서 여세요.");
        if (!AssetDatabase.IsOpenForEdit(path) || (File.GetAttributes(path) & FileAttributes.ReadOnly) != 0)
            throw new IOException("읽기 전용 맵입니다.");
        return stage;
    }
    internal static RoomController Controller(GameObject root)
    {
        var room = root.GetComponent<DungeonRoom>();
        if (!room || !room.Controller || !room.Controller.transform.IsChildOf(root.transform))
            throw new InvalidOperationException("맵의 RoomController 참조가 올바르지 않습니다.");
        return room.Controller;
    }
    internal static void SyncReferences(GameObject root)
    {
        var controller = Controller(root);
        var enemies = Enemies(root);
        using (var so = new SerializedObject(controller))
        {
            var p = so.FindProperty("enemies");
            if (p.arraySize == enemies.Length && enemies.Select((e, i) => p.GetArrayElementAtIndex(i).objectReferenceValue == e).All(x => x)) return;
            Undo.RecordObject(controller, "몬스터 등록 목록 갱신");
            p.arraySize = enemies.Length;
            for (int i = 0; i < enemies.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = enemies[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
    }
    internal static void Edit(GameObject root, string label, Action action)
    {
        MonsterDataStore.Guard();
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(label);
        try { action(); EditorSceneManager.MarkSceneDirty(root.scene); Undo.FlushUndoRecordObjects(); Undo.CollapseUndoOperations(group); }
        catch { Undo.RevertAllDownToGroup(group); throw; }
        finally { Undo.IncrementCurrentGroup(); }
    }
    internal static EnemyHealth Add(GameObject root, int speciesIndex, Vector3 localPosition, Action<EnemyHealth> initialize = null)
    {
        if (speciesIndex < 0 || speciesIndex >= MonsterDataStore.Catalog.Length) throw new ArgumentOutOfRangeException(nameof(speciesIndex));
        if (!Finite(localPosition)) throw new InvalidOperationException("좌표는 유한한 숫자여야 합니다.");
        var source = MonsterDataStore.Root(MonsterDataStore.Catalog[speciesIndex].Path);
        var sourceErrors = new Report(); CheckMonster(source.GetComponent<EnemyHealth>(), sourceErrors);
        if (sourceErrors.Errors.Count > 0) throw new InvalidOperationException(string.Join("\n", sourceErrors.Errors));
        Controller(root);
        // Preserve the established parent coordinate system, including the empty-map case.
        var parent = root.transform.Find("Monsters");
        if (!parent) throw new InvalidOperationException("기존 Monsters 부모가 없습니다. 지형/계층은 자동 생성하지 않습니다.");
        EnemyHealth added = null;
        Edit(root, "몬스터 추가", () =>
        {
            Undo.RegisterCompleteObjectUndo(Controller(root), "몬스터 추가");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
            instance.name = GameObjectUtility.GetUniqueNameForSibling(parent, source.name);
            instance.transform.localPosition = localPosition;
            initialize?.Invoke(instance.GetComponent<EnemyHealth>());
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            Undo.RegisterCreatedObjectUndo(instance, "몬스터 추가");
            added = instance.GetComponent<EnemyHealth>();
            SyncReferences(root);
        });
        return added;
    }
    internal static void Delete(GameObject root, EnemyHealth enemy)
    {
        if (!enemy || !enemy.transform.IsChildOf(root.transform) || enemy.gameObject == root || Species(enemy) == null ||
            enemy.GetComponent<StoneGolemBossAI>() || enemy.GetComponentsInChildren<EnemyHealth>(true).Length != 1)
            throw new InvalidOperationException("선택한 맵의 일반 몬스터 한 개만 삭제할 수 있습니다.");
        Controller(root);
        Edit(root, "몬스터 삭제", () =>
        {
            Undo.RegisterCompleteObjectUndo(Controller(root), "몬스터 삭제");
            Undo.DestroyObjectImmediate(enemy.gameObject); SyncReferences(root);
        });
    }
    internal static void Move(GameObject root, EnemyHealth enemy, Vector3 localPosition)
    {
        if (!enemy || !enemy.transform.IsChildOf(root.transform) || !Finite(localPosition)) throw new InvalidOperationException("몬스터/좌표가 올바르지 않습니다.");
        if (enemy.transform.localPosition == localPosition) return;
        Edit(root, "몬스터 위치 변경", () =>
        {
            Undo.RecordObject(enemy.transform, "몬스터 위치 변경");
            enemy.transform.localPosition = localPosition;
            if (PrefabUtility.IsPartOfPrefabInstance(enemy)) PrefabUtility.RecordPrefabInstancePropertyModifications(enemy.transform);
        });
    }
    internal static bool Finite(Vector3 p) => !float.IsNaN(p.x) && !float.IsNaN(p.y) && !float.IsNaN(p.z) &&
        !float.IsInfinity(p.x) && !float.IsInfinity(p.y) && !float.IsInfinity(p.z);
    static bool Active(Transform t)
    {
        for (; t; t = t.parent) if (!t.gameObject.activeSelf) return false;
        return true;
    }
    internal sealed class Report
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<string> Warnings = new List<string>();
    }
    static void CheckMonster(EnemyHealth hp, Report report)
    {
        if (!hp) { report.Errors.Add("EnemyHealth 누락"); return; }
        string name = hp.name;
        var s = Species(hp);
        if (s == null || hp.GetComponent<StoneGolemBossAI>()) { report.Errors.Add(name + ": 일반 몬스터 5종 AI가 아닙니다."); return; }
        if (!Active(hp.transform) || !hp.enabled || !((Behaviour)hp.GetComponent(s.AI)).enabled)
            report.Errors.Add(name + ": 비활성 몬스터/AI/체력은 전멸 진행을 막을 수 있습니다.");
        var rb = hp.GetComponent<Rigidbody2D>();
        if (!rb || !rb.simulated || !hp.GetComponents<Collider2D>().Any(c => c.enabled && !c.isTrigger)) report.Errors.Add(name + ": 몸체 Rigidbody2D/충돌체 누락 또는 비활성");
        var reward = hp.GetComponent<EnemyReward>();
        if (!reward || !reward.Reward) report.Errors.Add(name + ": 보상 참조 누락");
        foreach (var component in hp.GetComponentsInChildren<Component>(true))
        {
            if (!component) { report.Errors.Add(name + ": Missing Script"); continue; }
            using (var so = new SerializedObject(component))
            {
                var p = so.GetIterator();
                while (p.Next(true))
                    if (p.propertyType == SerializedPropertyType.ObjectReference && !p.objectReferenceValue && p.objectReferenceInstanceIDValue != 0)
                        report.Errors.Add(name + ": 깨진 참조 " + p.propertyPath);
            }
        }
        using (var so = new SerializedObject(hp.GetComponent(s.AI)))
        {
            foreach (string field in s.Id == "Stone" ? new[] { "body", "attackWarning" } : new[] { "body", "warningMaterial" })
                if (!so.FindProperty(field).objectReferenceValue) report.Errors.Add(name + ": AI 필수 참조 누락 " + field);
        }
        using (var so = new SerializedObject(hp))
            if (!so.FindProperty("spriteRenderer").objectReferenceValue) report.Errors.Add(name + ": 체력 SpriteRenderer 참조 누락");
    }
    internal static Report Validate(GameObject root)
    {
        var report = new Report();
        var room = root.GetComponent<DungeonRoom>(); var layout = root.GetComponent<RoomWalkableLayout>();
        RoomController controller;
        try { controller = Controller(root); } catch (Exception e) { report.Errors.Add(e.Message); return report; }
        var enemies = Enemies(root);
        using (var so = new SerializedObject(controller))
        {
            var p = so.FindProperty("enemies"); var registered = new HashSet<EnemyHealth>();
            for (int i = 0; i < p.arraySize; i++)
            {
                var hp = p.GetArrayElementAtIndex(i).objectReferenceValue as EnemyHealth;
                if (!hp) report.Errors.Add($"enemies[{i}]: 빈 참조");
                else if (!registered.Add(hp)) report.Errors.Add(hp.name + ": enemies 중복 참조");
                else if (!enemies.Contains(hp)) report.Errors.Add(hp.name + ": 맵 외부 참조");
            }
            foreach (var hp in enemies) if (!registered.Contains(hp)) report.Errors.Add(hp.name + ": enemies 등록 누락");
        }
        if (enemies.Length == 0) report.Warnings.Add("몬스터가 없습니다. 이 방은 입장 시 바로 클리어됩니다.");
        foreach (var hp in enemies) CheckMonster(hp, report);
        if (!layout || !room.Entrance || !layout.roomExit || layout.floorAreas == null || layout.rockAreas == null)
        { report.Warnings.Add("기존 이동 가능 영역/입구/출구 정보가 없어 지형·접근성을 확정할 수 없습니다."); return report; }
        if (root.transform.lossyScale != Vector3.one || Quaternion.Angle(root.transform.rotation, Quaternion.identity) > .01f)
        { report.Warnings.Add("루트 회전/배율이 기본값이 아니므로 기존 지형 검사의 정확도를 보장할 수 없습니다."); return report; }
        // A tiny preview-scene proxy reuses the runtime's exact navigation queries without
        // SelectExit (which would move doors/barriers) or altering the stage's navigation cache.
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            int cases = layout.chooseExitOnAwake ? layout.exitCandidates.Length : 1;
            if (cases == 0) report.Errors.Add("무작위 출구 후보가 없습니다.");
            for (int i = 0; i < cases; i++)
            {
                var proxy = new GameObject("Placement validation only");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(proxy, scene);
                proxy.transform.position = root.transform.position;
                EditorUtility.CopySerialized(room, proxy.AddComponent<DungeonRoom>());
                var query = proxy.AddComponent<RoomWalkableLayout>(); EditorUtility.CopySerialized(layout, query);
                if (layout.chooseExitOnAwake) query.selectedExit = i;
                string suffix = cases > 1 ? $" (출구 후보 {i + 1})" : "";
                foreach (var hp in enemies)
                {
                    if (!Finite(hp.transform.localPosition)) { report.Errors.Add(hp.name + ": 유효하지 않은 좌표"); continue; }
                    float radius = Radius(hp);
                    if (!query.ContainsBody(hp.transform.position, radius)) report.Errors.Add(hp.name + " " + hp.transform.localPosition + ": 바닥 밖/암벽 내부 또는 몸체 여유 부족" + suffix);
                    else if (!query.Reachable(room.Entrance.position, hp.transform.position, radius)) report.Errors.Add(hp.name + " " + hp.transform.localPosition + ": 입구에서 접근 불가" + suffix);
                }
                Vector2 door = layout.chooseExitOnAwake ? root.transform.TransformPoint(layout.exitCandidates[i]) : layout.roomExit.transform.position;
                if (!query.Reachable(room.Entrance.position, door - Vector2.up * 1.5f, .65f)) report.Errors.Add("입구→출구 앞 경로 단절" + suffix);
                UnityEngine.Object.DestroyImmediate(proxy);
            }
        }
        catch (Exception e) { report.Warnings.Add("기존 경로 검사로 확정할 수 없습니다: " + e.Message); }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        for (int i = 0; i < enemies.Length; i++)
        {
            var hp = enemies[i];
            if (Mathf.Abs(hp.transform.localPosition.z) > .001f) report.Warnings.Add(hp.name + ": 2D 배치의 로컬 Z가 0이 아닙니다.");
            Vector2 local = root.transform.InverseTransformPoint(hp.transform.position);
            if (layout.monsterAreas != null && !layout.monsterAreas.Any(a => a.Contains(local))) report.Warnings.Add(hp.name + ": 기존 권장 몬스터 영역 밖입니다.");
            if (Vector2.Distance(hp.transform.position, room.Entrance.position) < Radius(hp) + .65f) report.Warnings.Add(hp.name + ": 입구와 가까워 부활/진입 공간 확인 필요");
            foreach (var wall in root.GetComponentsInChildren<Collider2D>(true).Where(c => c.enabled && !c.isTrigger && Active(c.transform) && c.gameObject.layer == LayerMask.NameToLayer("Wall") && !c.GetComponentInParent<EnemyHealth>()))
            {
                if (wall is BoxCollider2D box && Mathf.Abs(Mathf.DeltaAngle(wall.transform.eulerAngles.z, 0)) < .01f)
                {
                    Vector2 center = box.transform.TransformPoint(box.offset);
                    Vector2 size = Vector2.Scale(box.size, box.transform.lossyScale);
                    var bounds = new Rect(center - size * .5f, size);
                    if (bounds.Contains(hp.transform.position)) report.Errors.Add(hp.name + ": 실제 벽/닫힌 출구 내부 (" + wall.name + ")");
                    else if (bounds.Overlaps(new Rect((Vector2)hp.transform.position - Vector2.one * Radius(hp), Vector2.one * Radius(hp) * 2)))
                        report.Warnings.Add(hp.name + ": 실제 벽/출구와 몸체 간격 확인 필요 (" + wall.name + ")");
                }
                else report.Warnings.Add("벽 " + wall.name + ": 회전/비직사각 충돌체는 기존 영역 데이터만으로 확정할 수 없습니다.");
            }
            for (int j = i + 1; j < enemies.Length; j++)
                if (Vector2.Distance(hp.transform.position, enemies[j].transform.position) < Radius(hp) + Radius(enemies[j])) report.Errors.Add(hp.name + " " + hp.transform.localPosition + " / " + enemies[j].name + " " + enemies[j].transform.localPosition + ": 몸체 여유 겹침. 위치를 조정하세요.");
        }
        report.Warnings.Add("경로 검사는 기존 0.5 그리드와 보수적 몸체 여유 기준입니다. 실제 전투·동적 충돌·출구 주변 혼잡은 Scene/Play에서 확인하세요.");
        return report;
    }
    internal static float Radius(EnemyHealth hp)
    {
        float radius = .65f;
        foreach (var c in hp.GetComponents<Collider2D>().Where(c => !c.isTrigger))
        {
            Vector2 scale = c.transform.lossyScale;
            if (c is BoxCollider2D box) radius = Mathf.Max(radius, Mathf.Max(Mathf.Abs(box.size.x * scale.x), Mathf.Abs(box.size.y * scale.y)) * .5f + box.offset.magnitude);
            else if (c is CircleCollider2D circle) radius = Mathf.Max(radius, circle.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y)) + circle.offset.magnitude);
        }
        return radius;
    }
    internal static void Save(PrefabStage stage)
    {
        if (stage != Stage(stage.assetPath)) throw new InvalidOperationException("편집 중인 맵이 바뀌었습니다.");
        var report = Validate(stage.prefabContentsRoot);
        if (report.Errors.Count > 0) throw new InvalidOperationException("검사 오류를 해결한 뒤 저장하세요.\n" + string.Join("\n", report.Errors));
        if (!stage.scene.isDirty) return;
        PrefabUtility.SaveAsPrefabAsset(stage.prefabContentsRoot, stage.assetPath, out bool success);
        if (!success) throw new IOException("맵 저장 실패");
        stage.ClearDirtiness();
    }
}
