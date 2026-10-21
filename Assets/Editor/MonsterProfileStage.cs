using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class MonsterProfileStage : PreviewSceneStage
{
    [SerializeField] public MonsterMapProfile source, draft;
    [SerializeField] public GameObject root;
    readonly Dictionary<Component, string> protectedState = new Dictionary<Component, string>();
    public static MonsterProfileStage Current => StageUtility.GetCurrentStage() as MonsterProfileStage;
    public static void Open(MonsterMapProfile source, MonsterMapProfile draft)
    {
        MonsterDataStore.Guard();
        var stage = CreateInstance<MonsterProfileStage>(); stage.source = source; stage.draft = draft;
        StageUtility.GoToStage(stage, true);
    }
    protected override bool OnOpenStage()
    {
        if (!base.OnOpenStage() || !source || !draft || !draft.map) return false;
        root = (GameObject)PrefabUtility.InstantiatePrefab(draft.map.gameObject, scene);
        PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        var enemies = MonsterProfileRuntime.Compose(root, draft, false);
        for (int i = 0; i < enemies.Length; i++) Register(enemies[i], draft.entries[i]);
        foreach (var c in root.GetComponentsInChildren<Component>(true))
            if (c && !(c is RoomController) && !c.GetComponentInParent<EnemyHealth>()) protectedState[c] = EditorJsonUtility.ToJson(c);
        return true;
    }
    public void Register(EnemyHealth hp, MonsterMapProfile.Entry entry)
    {
        var marker = hp.gameObject.AddComponent<MonsterProfileEntryMarker>(); marker.hideFlags = HideFlags.HideInInspector;
        marker.template = entry.template; marker.entryId = entry.id; marker.embeddedPath = entry.embeddedPath;
        foreach (var c in hp.GetComponentsInChildren<Component>(true))
            if (c && c != hp.transform && !(c is MonsterProfileEntryMarker)) protectedState[c] = EditorJsonUtility.ToJson(c);
    }
    public void RegisterNew(EnemyHealth hp, int kind) => Register(hp, MonsterProfileEditorStore.Entry(hp,
        AssetDatabase.LoadAssetAtPath<GameObject>(MonsterDataStore.Catalog[kind].Path).GetComponent<EnemyHealth>(), Guid.NewGuid().ToString("N"), ""));
    public void Capture()
    {
        if (!root || !draft) return;
        foreach (var pair in protectedState)
            if (pair.Key && EditorJsonUtility.ToJson(pair.Key) != pair.Value) throw new InvalidOperationException("지형/기본 능력치/필수 참조 변경은 저장할 수 없습니다. 위치만 편집하세요.");
        draft.entries = MonsterPlacementStore.Enemies(root).Select(hp =>
        {
            var marker = hp.GetComponent<MonsterProfileEntryMarker>();
            if (!marker || !marker.template) throw new InvalidOperationException("배치 탭의 추가 버튼을 사용하세요. 배치 원본 참조가 없습니다.");
            return MonsterProfileEditorStore.Entry(hp, marker.template, marker.entryId, marker.embeddedPath);
        }).ToArray();
    }
    protected override GUIContent CreateHeaderContent() => new GUIContent(source ? source.name + " (배치 초안)" : "몬스터 배치 초안");
    protected override void OnCloseStage()
    {
        try { Capture(); } catch (Exception e) { Debug.LogWarning("배치 초안 보관 실패: " + e.Message); }
        base.OnCloseStage();
    }
}
