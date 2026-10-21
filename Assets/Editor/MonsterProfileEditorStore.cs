using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal static class MonsterProfileEditorStore
{
    internal const string Folder = "Assets/Dungeons/MonsterProfiles";
    static DungeonFloorData[] cachedFloors;
    internal static DungeonFloorData[] Floors() => cachedFloors ?? (cachedFloors = AssetDatabase.FindAssets("t:DungeonFloorData", new[] { "Assets/Dungeons" })
        .Select(g => AssetDatabase.LoadAssetAtPath<DungeonFloorData>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(f => f.floorNumber).ToArray());
    internal static void Selectors(ref DungeonFloorData floor, ref int difficulty)
    {
        var floors = Floors(); if (floors.Length == 0) return;
        int i = Array.IndexOf(floors, floor); if (i < 0) i = 0;
        floor = floors[EditorGUILayout.Popup("층", i, floors.Select(f => f.floorNumber + "층 · " + f.displayName).ToArray())];
        difficulty = EditorGUILayout.Popup("난이도", difficulty, Enumerable.Range(0, 4).Select(DungeonFloorData.DifficultyName).ToArray());
    }
    internal static MonsterFloorProfile Ensure(DungeonFloorData floor)
    {
        MonsterDataStore.Guard();
        if (floor.monsterProfile) return floor.monsterProfile;
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Dungeons", "MonsterProfiles");
        var asset = ScriptableObject.CreateInstance<MonsterFloorProfile>();
        AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(Folder + "/" + floor.name + "_Monsters.asset"));
        Undo.RecordObject(floor, "몬스터 프로필 연결"); floor.monsterProfile = asset; EditorUtility.SetDirty(floor); AssetDatabase.SaveAssetIfDirty(floor);
        return asset;
    }
    internal static MonsterMapProfile Find(DungeonFloorData floor, int difficulty, DungeonRoom map) => floor?.monsterProfile?.GetDifficulty(difficulty)?.Find(map);
    internal static MonsterMapProfile CreateMap(DungeonFloorData floor, int difficulty, DungeonRoom map)
    {
        var balance = Ensure(floor);
        if (Find(floor, difficulty, map)) throw new InvalidOperationException("이미 배치 프로필이 있습니다.");
        var asset = ScriptableObject.CreateInstance<MonsterMapProfile>(); asset.map = map;
        AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(Folder + $"/{floor.name}_D{difficulty}_{map.name}.asset"));
        Undo.RecordObject(balance, "몬스터 배치 프로필 연결");
        var d = balance.GetDifficulty(difficulty); d.maps = (d.maps ?? Array.Empty<MonsterMapProfile>()).Concat(new[] { asset }).ToArray();
        EditorUtility.SetDirty(balance); AssetDatabase.SaveAssetIfDirty(balance); return asset;
    }
    internal static string Relative(Transform root, Transform child)
    { string path = child.name; while (child.parent && child.parent != root) { child = child.parent; path = child.name + "/" + path; } return path; }
    internal static MonsterMapProfile.Entry Entry(EnemyHealth hp, EnemyHealth template, string id, string embeddedPath) => new MonsterMapProfile.Entry
    {
        template = template, id = id, embeddedPath = embeddedPath,
        localPosition = hp.transform.localPosition, localEuler = hp.transform.localEulerAngles, localScale = hp.transform.localScale
    };
    internal static T Draft<T>(T asset) where T : ScriptableObject
    { var copy = UnityEngine.Object.Instantiate(asset); copy.hideFlags = HideFlags.HideAndDontSave; copy.name = asset.name; return copy; }
    internal static string Json(UnityEngine.Object asset)
    {
        var flags = asset.hideFlags;
        bool transient = !EditorUtility.IsPersistent(asset);
        if (transient) asset.hideFlags = HideFlags.None;
        try { return EditorJsonUtility.ToJson(asset); } finally { if (transient) asset.hideFlags = flags; }
    }
    internal static MonsterPlacementStore.Report ValidateDraft(MonsterMapProfile draft)
    {
        var ids = new System.Collections.Generic.HashSet<string>();
        foreach (var entry in draft.entries)
        {
            if (entry == null || !entry.template || string.IsNullOrEmpty(entry.id) || !ids.Add(entry.id) || !MonsterProfileRuntime.ValidPose(entry))
                throw new InvalidOperationException("배치 ID/원본 참조/좌표가 올바르지 않습니다.");
            MonsterProfileRuntime.Kind(entry.template);
            if (entry.reservedAttackAdjustment != 0 || entry.reservedHpAdjustment != 0) throw new InvalidOperationException("개체별 보정은 미지원입니다.");
        }
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        try
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(draft.map.gameObject, scene);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            MonsterProfileRuntime.Compose(root, draft, false);
            return MonsterPlacementStore.Validate(root);
        }
        finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
    }
    internal static void Save(ScriptableObject draft, ScriptableObject asset, string baseline)
    {
        MonsterDataStore.Guard(); string path = AssetDatabase.GetAssetPath(asset);
        if (Json(asset) != baseline) throw new InvalidOperationException("원본 데이터가 변경되었습니다. 다시 불러온 뒤 편집하세요.");
        if (!AssetDatabase.IsOpenForEdit(path) || (File.GetAttributes(path) & FileAttributes.ReadOnly) != 0) throw new IOException("쓰기 불가: " + path);
        // hideFlags belong to the draft's lifetime, not the persisted game data.
        var flags = draft.hideFlags; draft.hideFlags = asset.hideFlags;
        try
        {
            if (Json(draft) == Json(asset)) return;
            Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("층·난이도 몬스터 데이터 저장");
            SessionState.SetString("MineMonsterProfile.Undo." + group, path);
            Undo.RegisterCompleteObjectUndo(asset, "층·난이도 몬스터 데이터 저장"); EditorUtility.CopySerialized(draft, asset);
            EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); Undo.IncrementCurrentGroup();
        }
        finally { draft.hideFlags = flags; }
    }
    [InitializeOnLoadMethod] static void Hook()
    {
        Undo.undoRedoEvent -= PersistUndo; Undo.undoRedoEvent += PersistUndo;
        EditorApplication.projectChanged -= Invalidate; EditorApplication.projectChanged += Invalidate;
    }
    static void Invalidate() { cachedFloors = null; }
    static void PersistUndo(in UndoRedoInfo info)
    {
        if (info.undoName != "층·난이도 몬스터 데이터 저장") return;
        var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SessionState.GetString("MineMonsterProfile.Undo." + info.undoGroup, ""));
        if (asset) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
    }
    internal static void InitializeExistingEasy()
    {
        var floor = AssetDatabase.LoadAssetAtPath<DungeonFloorData>("Assets/Dungeons/Floor1_RockMine.asset");
        if (floor.monsterProfile) throw new InvalidOperationException("기존 프로필을 덮어쓰지 않습니다.");
        var balance = Ensure(floor); balance.configured = true;
        var d = balance.GetDifficulty(0); d.configured = true;
        foreach (var map in floor.GetDifficulty(0).normalRooms)
        {
            var profile = CreateMap(floor, 0, map); profile.configured = true;
            profile.entries = map.GetComponentsInChildren<EnemyHealth>(true).Select(hp => Entry(hp, hp, Guid.NewGuid().ToString("N"), Relative(map.transform, hp.transform))).ToArray();
            EditorUtility.SetDirty(profile); AssetDatabase.SaveAssetIfDirty(profile);
        }
        EditorUtility.SetDirty(balance); AssetDatabase.SaveAssetIfDirty(balance);
    }
}
