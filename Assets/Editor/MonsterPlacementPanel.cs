using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[Serializable]
internal sealed class MonsterPlacementPanel
{
    [SerializeField] int mapIndex, speciesIndex;
    [SerializeField] Vector3 newPosition;
    Vector2 scroll;
    MonsterPlacementStore.Report report;
    string message, inspectedState;
    Action repaint;
    public void Enable(Action repaintWindow)
    {
        repaint = repaintWindow;
        Undo.undoRedoPerformed += Changed;
        EditorApplication.hierarchyChanged += SelectionChanged;
        Selection.selectionChanged += SelectionChanged;
        SceneView.duringSceneGui += SceneGUI;
    }
    public void Disable()
    {
        Undo.undoRedoPerformed -= Changed;
        EditorApplication.hierarchyChanged -= SelectionChanged;
        Selection.selectionChanged -= SelectionChanged;
        SceneView.duringSceneGui -= SceneGUI;
    }
    void Changed() { report = null; repaint?.Invoke(); }
    void SelectionChanged() { repaint?.Invoke(); }
    void Run(Action action)
    {
        try { action(); }
        catch (Exception e) { message = e.Message; }
    }
    string Path => MonsterPlacementStore.Paths[Mathf.Clamp(mapIndex, 0, 6)];
    bool Editing(out PrefabStage stage)
    { stage = PrefabStageUtility.GetCurrentPrefabStage(); return stage != null && stage.assetPath == Path; }
    EnemyHealth Selected(GameObject root)
    {
        var go = Selection.activeGameObject;
        var hp = go ? go.GetComponentInParent<EnemyHealth>() : null;
        return hp && hp.transform.IsChildOf(root.transform) ? hp : null;
    }
    void Focus(EnemyHealth hp)
    {
        Selection.activeGameObject = hp.gameObject;
        Tools.current = Tool.Move;
        if (SceneView.lastActiveSceneView) { SceneView.lastActiveSceneView.FrameSelected(); SceneView.lastActiveSceneView.Focus(); }
    }
    public void Draw()
    {
        EditorGUILayout.HelpBox("Map_01~07 전용 배치 편집입니다. 추가·삭제는 이 탭의 버튼, 이동은 Scene 뷰의 W 도구를 사용하세요.\nPrefab Mode의 Auto Save가 켜져 있으면 변경이 즉시 파일에 저장됩니다. 검사 후 저장하려면 Auto Save를 끄세요. Ctrl+Z / Ctrl+Y는 Unity Undo / Redo를 사용합니다.", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                int next = EditorGUILayout.Popup("일반 맵", mapIndex, Enumerable.Range(1, 7).Select(i => $"Map_{i:00}").ToArray());
                if (next != mapIndex) { mapIndex = next; report = null; message = null; }
                if (GUILayout.Button("Scene / Prefab Mode에서 열기", GUILayout.Width(220))) Run(() =>
                {
                    MonsterDataStore.Guard();
                    PrefabStageUtility.OpenPrefab(Path);
                    message = "몬스터를 선택하고 W 이동 도구로 옮기세요. Auto Save 설정을 먼저 확인하세요.";
                });
            }
            bool editing = Editing(out var stage);
            var root = editing ? stage.prefabContentsRoot : AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            if (!root) { EditorGUILayout.HelpBox("맵 프리팹이 없습니다.", MessageType.Error); return; }
            var enemies = MonsterPlacementStore.Enemies(root);
            string state = string.Join("|", enemies.Select(e => e.GetInstanceID() + ":" + e.transform.localPosition.ToString("R")));
            var controller = root.GetComponent<DungeonRoom>()?.Controller;
            if (controller)
                using (var so = new SerializedObject(controller))
                {
                    var refs = so.FindProperty("enemies");
                    state += ":" + string.Join(",", Enumerable.Range(0, refs.arraySize).Select(i => refs.GetArrayElementAtIndex(i).objectReferenceInstanceIDValue));
                }
            if (inspectedState != state) { inspectedState = state; report = null; }
            EditorGUILayout.LabelField($"총 {enemies.Length}마리 · " + string.Join(" / ", MonsterDataStore.Catalog.Select(s => s.Label + " " + enemies.Count(e => MonsterPlacementStore.Species(e) == s))));
            if (!editing) EditorGUILayout.HelpBox("현재는 저장된 맵 목록입니다. 추가·삭제·이동하려면 선택한 맵을 여세요.", MessageType.None);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            var selected = Selected(root);
            foreach (var hp in enemies)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var s = MonsterPlacementStore.Species(hp);
                    GUILayout.Label(s?.Label ?? "알 수 없는 종류", GUILayout.Width(80));
                    GUILayout.Label(hp.name, GUILayout.Width(180));
                    GUILayout.Label("부모 로컬 " + hp.transform.localPosition.ToString("F3"), GUILayout.Width(280));
                    using (new EditorGUI.DisabledScope(!editing))
                        if (GUILayout.Button(selected == hp ? "선택됨 / 찾기" : "Scene에서 찾기", GUILayout.Width(130))) Focus(hp);
                }
            }
            using (new EditorGUI.DisabledScope(!editing))
            {
                if (selected)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("선택: " + selected.name + " · 좌표 기준: " + selected.transform.parent.name, EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();
                    Vector3 position = EditorGUILayout.Vector3Field("부모 로컬 위치", selected.transform.localPosition);
                    if (EditorGUI.EndChangeCheck()) Run(() => { MonsterPlacementStore.Stage(Path); MonsterPlacementStore.Move(root, selected, position); Changed(); });
                    if (GUILayout.Button("선택한 몬스터 삭제")) Run(() =>
                    {
                        MonsterPlacementStore.Stage(Path);
                        if (!EditorUtility.DisplayDialog("몬스터 삭제", selected.name + " 한 개를 삭제하고 enemies 참조를 갱신할까요?", "삭제", "취소")) return;
                        MonsterPlacementStore.Delete(root, selected); Changed(); message = "선택 몬스터를 삭제했습니다. Undo 지원.";
                    });
                }
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("몬스터 한 개 추가", EditorStyles.boldLabel);
                speciesIndex = EditorGUILayout.Popup("종류", speciesIndex, MonsterDataStore.Catalog.Select(s => s.Label).ToArray());
                newPosition = EditorGUILayout.Vector3Field("Monsters 부모 로컬 위치", newPosition);
                if (GUILayout.Button("지정한 위치에 추가")) Run(() =>
                {
                    MonsterPlacementStore.Stage(Path);
                    var hp = MonsterPlacementStore.Add(root, speciesIndex, newPosition);
                    Changed(); Focus(hp); message = "기존 프리팹으로 추가하고 enemies를 갱신했습니다. 배치 검사를 실행하세요.";
                });
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("배치 / 참조 검사")) Run(() => { MonsterPlacementStore.Stage(Path); report = MonsterPlacementStore.Validate(root); message = "검사가 완료되었습니다."; });
                    if (GUILayout.Button("enemies 등록 목록 복구")) Run(() =>
                    {
                        MonsterPlacementStore.Stage(Path);
                        Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName("몬스터 등록 목록 복구");
                        MonsterPlacementStore.SyncReferences(root); Undo.IncrementCurrentGroup();
                        Changed(); message = "현재 맵의 EnemyHealth 목록으로 등록 참조를 갱신했습니다. Undo 지원.";
                    });
                    if (GUILayout.Button("검사 후 이 맵만 저장")) Run(() =>
                    {
                        MonsterPlacementStore.Stage(Path); report = MonsterPlacementStore.Validate(root);
                        if (report.Errors.Count > 0) { message = "검사 오류를 해결하세요."; return; }
                        MonsterPlacementStore.Save(stage); message = "선택한 맵을 저장했습니다.";
                    });
                }
            }
            if (report != null)
            {
                EditorGUILayout.LabelField($"검사: 오류 {report.Errors.Count} / 경고 {report.Warnings.Count}", EditorStyles.boldLabel);
                foreach (string error in report.Errors) EditorGUILayout.HelpBox(error, MessageType.Error);
                foreach (string warning in report.Warnings) EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
            else if (editing) EditorGUILayout.HelpBox("현재 배치의 검사 결과가 없습니다. 이동·추가·삭제 후 다시 검사하세요.", MessageType.None);
            EditorGUILayout.EndScrollView();
        }
        if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.None);
    }
    void SceneGUI(SceneView view)
    {
        if (!Editing(out var stage) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var hp = Selected(stage.prefabContentsRoot);
        if (!hp) return;
        Handles.color = Color.yellow;
        Handles.DrawWireDisc(hp.transform.position, Vector3.forward, MonsterPlacementStore.Radius(hp));
        Handles.Label(hp.transform.position + Vector3.up, (MonsterPlacementStore.Species(hp)?.Label ?? hp.name) + " · 로컬 " + hp.transform.localPosition.ToString("F2"));
        repaint?.Invoke();
    }
}
