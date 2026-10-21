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
    [SerializeField] bool showAggro = true, allAggro;
    [SerializeField] bool profileMode = true;
    [SerializeField] DungeonFloorData floor;
    [SerializeField] int difficulty;
    [SerializeField] MonsterMapProfile profileSource, profileDraft;
    [SerializeField] string profileBaseline;
    bool placementPreview;
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
    void Changed()
    {
        report = null; placementPreview = false;
        if (profileMode && profileSource && profileBaseline != MonsterProfileEditorStore.Json(profileSource))
            message = "저장 데이터에 Undo/Redo가 반영되었습니다. Scene 초안은 유지됩니다. 저장된 배치 다시 불러오기로 확인하세요.";
        repaint?.Invoke();
    }
    void SelectionChanged() { repaint?.Invoke(); }
    void Run(Action action)
    {
        try { action(); }
        catch (Exception e) { message = e.Message; }
    }
    string Path => MonsterPlacementStore.Paths[Mathf.Clamp(mapIndex, 0, 6)];
    bool Editing(out GameObject root)
    {
        root = null;
        if (profileMode)
        {
            var stage = MonsterProfileStage.Current;
            var map = AssetDatabase.LoadAssetAtPath<DungeonRoom>(Path);
            if (stage && stage.source == MonsterProfileEditorStore.Find(floor, difficulty, map)) root = stage.root;
        }
        else
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null && stage.assetPath == Path) root = stage.prefabContentsRoot;
        }
        return root;
    }
    void GuardEditing()
    {
        MonsterDataStore.Guard();
        if (!Editing(out _)) throw new InvalidOperationException("선택한 조합의 Scene 편집을 먼저 여세요.");
        if (!profileMode) MonsterPlacementStore.Stage(Path);
    }
    void OpenProfile(MonsterMapProfile source)
    {
        if (MonsterProfileStage.Current) MonsterProfileStage.Current.Capture();
        if (profileDraft && profileSource != source && MonsterProfileEditorStore.Json(profileDraft) != profileBaseline &&
            !EditorUtility.DisplayDialog("미저장 배치 초안", "현재 초안을 버리고 다른 조합을 열까요?", "변경", "취소")) return;
        if (!profileDraft || profileSource != source)
        {
            if (profileDraft) UnityEngine.Object.DestroyImmediate(profileDraft);
            profileSource = source; profileDraft = MonsterProfileEditorStore.Draft(source); profileBaseline = MonsterProfileEditorStore.Json(source);
        }
        if (!MonsterProfileStage.Current || MonsterProfileStage.Current.source != source) MonsterProfileStage.Open(source, profileDraft);
    }
    void SaveProfile()
    {
        var stage = MonsterProfileStage.Current;
        if (stage && stage.draft == profileDraft) stage.Capture();
        report = MonsterProfileEditorStore.ValidateDraft(profileDraft);
        if (report.Errors.Count > 0) throw new InvalidOperationException("배치 오류를 먼저 해결하세요.");
        var balance = floor.monsterProfile;
        if (profileDraft.configured && balance && balance.configured && balance.GetDifficulty(difficulty).configured) balance.ValidateMap(difficulty, profileDraft);
        if (!EditorUtility.DisplayDialog("독립 배치 저장", $"{floor.floorNumber}층 {DungeonFloorData.DifficultyName(difficulty)} / {profileDraft.map.name}\n{profileSource.entries.Length} → {profileDraft.entries.Length}마리\n설정 완료: {profileSource.configured} → {profileDraft.configured}\n이 조합의 배치만 저장할까요?", "저장", "취소")) return;
        MonsterProfileEditorStore.Save(profileDraft, profileSource, profileBaseline);
        profileBaseline = MonsterProfileEditorStore.Json(profileSource); placementPreview = false;
        message = "선택한 조합의 배치만 저장했습니다. Ctrl+Z / Ctrl+Y 지원.";
    }
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
        profileMode = GUILayout.Toolbar(profileMode ? 0 : 1, new[] { "층·난이도 독립 배치", "기본 프리팹 편집 (기존)" }) == 0;
        EditorGUILayout.HelpBox(profileMode ? "독립 배치 초안입니다. Scene의 W 도구로 위치를 편집하고, 변경 미리보기 후 저장하세요. 원본 맵의 지형·기본 능력치는 저장하지 않습니다.\n미설정과 의도한 0마리는 ‘배치 설정 완료’로 구분합니다. 배치 설정만으로 미구현 던전이 활성화되지는 않습니다." : "기존 Map_01~07 기본 프리팹 편집입니다. Auto Save가 켜지면 즉시 파일에 저장될 수 있습니다.\n독립 배치 프로필의 좌표·구성은 별도로 저장됩니다. 추가·삭제는 이 탭의 버튼, 이동은 Scene의 W 도구를 사용하세요.", MessageType.Info);
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
        {
            if (profileMode) MonsterProfileEditorStore.Selectors(ref floor, ref difficulty);
            if (profileMode && GUILayout.Button("저장된 배치 다시 불러오기")) Run(() =>
            {
                var saved = MonsterProfileEditorStore.Find(floor, difficulty, AssetDatabase.LoadAssetAtPath<DungeonRoom>(Path));
                if (!saved) throw new InvalidOperationException("저장된 배치 프로필이 없습니다.");
                if (!EditorUtility.DisplayDialog("배치 초안 초기화", "현재 Scene 초안을 버리고 저장된 배치를 다시 불러올까요?", "불러오기", "취소")) return;
                if (MonsterProfileStage.Current) StageUtility.GoToMainStage();
                if (profileDraft) UnityEngine.Object.DestroyImmediate(profileDraft);
                profileDraft = null; profileSource = null; OpenProfile(saved);
            });
            using (new EditorGUILayout.HorizontalScope())
            {
                int next = EditorGUILayout.Popup("일반 맵", mapIndex, Enumerable.Range(1, 7).Select(i => $"Map_{i:00}").ToArray());
                if (next != mapIndex) { mapIndex = next; report = null; message = null; }
                if (GUILayout.Button(profileMode ? "독립 배치 Scene 열기" : "Scene / Prefab Mode에서 열기", GUILayout.Width(220))) Run(() =>
                {
                    MonsterDataStore.Guard();
                    if (profileMode)
                    {
                        var map = AssetDatabase.LoadAssetAtPath<DungeonRoom>(Path);
                        var source = MonsterProfileEditorStore.Find(floor, difficulty, map);
                        if (!source) throw new InvalidOperationException("이 조합의 빈 배치 프로필을 먼저 만드세요.");
                        OpenProfile(source);
                    }
                    else PrefabStageUtility.OpenPrefab(Path);
                    message = profileMode ? "W 도구로 위치를 편집한 뒤 배치 변경 미리보기에서 이 조합만 저장하세요." : "몬스터를 선택하고 W 이동 도구로 옮기세요. Auto Save 설정을 먼저 확인하세요.";
                });
            }
            bool editing = Editing(out var editRoot);
            EditorGUI.BeginChangeCheck();
            using (new EditorGUILayout.HorizontalScope())
            {
                showAggro = EditorGUILayout.ToggleLeft("Scene 인식 범위 표시", showAggro, GUILayout.Width(190));
                using (new EditorGUI.DisabledScope(!showAggro))
                    allAggro = GUILayout.Toolbar(allAggro ? 1 : 0, new[] { "선택한 몬스터만", "전체 몬스터" }) == 1;
            }
            if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();
            if (profileMode && !editing)
            {
                var map = AssetDatabase.LoadAssetAtPath<DungeonRoom>(Path);
                var profile = MonsterProfileEditorStore.Find(floor, difficulty, map);
                EditorGUILayout.HelpBox(!profile || !profile.configured ? "선택한 조합: 배치 미설정" : $"선택한 조합: 설정 완료 / {profile.entries.Length}마리", MessageType.Info);
                if (!profile && GUILayout.Button("이 조합의 빈 배치 프로필 만들기")) Run(() => { var p = MonsterProfileEditorStore.CreateMap(floor, difficulty, map); OpenProfile(p); });
                if (profile)
                    foreach (var e in profile.entries) EditorGUILayout.LabelField((e.template ? MonsterProfileRuntime.Kind(e.template).ToString() : "참조 누락") + " · " + e.localPosition);
                if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, MessageType.None);
                return;
            }
            var root = editing ? editRoot : AssetDatabase.LoadAssetAtPath<GameObject>(Path);
            if (!root) { EditorGUILayout.HelpBox("맵 프리팹이 없습니다.", MessageType.Error); return; }
            if (profileMode && profileDraft) profileDraft.configured = EditorGUILayout.Toggle("이 배치 설정 완료 (0마리도 가능)", profileDraft.configured);
            var enemies = MonsterPlacementStore.Enemies(root);
            string state = string.Join("|", enemies.Select(e => e.GetInstanceID() + ":" + e.transform.localPosition.ToString("R") + ":" + AggroRadius(e)));
            var controller = root.GetComponent<DungeonRoom>()?.Controller;
            if (controller)
                using (var so = new SerializedObject(controller))
                {
                    var refs = so.FindProperty("enemies");
                    state += ":" + string.Join(",", Enumerable.Range(0, refs.arraySize).Select(i => refs.GetArrayElementAtIndex(i).objectReferenceInstanceIDValue));
                }
            if (inspectedState != state) { inspectedState = state; report = null; placementPreview = false; SceneView.RepaintAll(); }
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
                    if (EditorGUI.EndChangeCheck()) Run(() => { GuardEditing(); MonsterPlacementStore.Move(root, selected, position); Changed(); });
                    if (GUILayout.Button("선택한 몬스터 삭제")) Run(() =>
                    {
                        GuardEditing();
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
                    GuardEditing();
                    var hp = MonsterPlacementStore.Add(root, speciesIndex, newPosition, profileMode ? (Action<EnemyHealth>)(e => MonsterProfileStage.Current.RegisterNew(e, speciesIndex)) : null);
                    Changed(); Focus(hp); message = "기존 프리팹으로 추가하고 enemies를 갱신했습니다. 배치 검사를 실행하세요.";
                });
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("배치 / 참조 검사")) Run(() => { GuardEditing(); report = MonsterPlacementStore.Validate(root); message = "검사가 완료되었습니다."; });
                    if (GUILayout.Button("enemies 등록 목록 복구")) Run(() =>
                    {
                        GuardEditing();
                        Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName("몬스터 등록 목록 복구");
                        MonsterPlacementStore.SyncReferences(root); Undo.IncrementCurrentGroup();
                        Changed(); message = "현재 맵의 EnemyHealth 목록으로 등록 참조를 갱신했습니다. Undo 지원.";
                    });
                    if (GUILayout.Button(profileMode ? "배치 변경 미리보기" : "검사 후 이 맵만 저장")) Run(() =>
                    {
                        GuardEditing();
                        if (profileMode) { MonsterProfileStage.Current.Capture(); placementPreview = true; return; }
                        report = MonsterPlacementStore.Validate(root);
                        if (report.Errors.Count > 0) { message = "검사 오류를 해결하세요."; return; }
                        MonsterPlacementStore.Save(PrefabStageUtility.GetCurrentPrefabStage()); message = "선택한 맵을 저장했습니다.";
                    });
                }
            }
            if (profileMode && placementPreview && profileDraft)
            {
                EditorGUILayout.LabelField($"배치 변경 미리보기: {profileSource.entries.Length} → {profileDraft.entries.Length}마리", EditorStyles.boldLabel);
                foreach (var entry in profileDraft.entries)
                {
                    var before = profileSource.entries.FirstOrDefault(e => e.id == entry.id);
                    EditorGUILayout.LabelField((entry.template ? MonsterProfileRuntime.Kind(entry.template).ToString() : "참조 누락") + " · " + (before == null ? "신규" : before.localPosition.ToString()) + " → " + entry.localPosition);
                }
                foreach (var deleted in profileSource.entries.Where(e => !profileDraft.entries.Any(n => n.id == e.id))) EditorGUILayout.LabelField("삭제: " + deleted.id);
                if (GUILayout.Button("확인 후 이 조합의 배치 저장")) Run(SaveProfile);
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
        if (!Editing(out var root) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach (var enemy in AggroTargets(root))
        {
            Handles.color = new Color(.2f, .85f, 1f, .8f);
            Handles.DrawWireDisc(enemy.transform.position, Vector3.forward, AggroRadius(enemy));
            Handles.Label(enemy.transform.position + Vector3.down, "인식 " + AggroRadius(enemy).ToString("0.##") + " (임시)");
        }
        var hp = Selected(root);
        if (!hp) return;
        Handles.color = Color.yellow;
        Handles.DrawWireDisc(hp.transform.position, Vector3.forward, MonsterPlacementStore.Radius(hp));
        Handles.Label(hp.transform.position + Vector3.up, (MonsterPlacementStore.Species(hp)?.Label ?? hp.name) + " · 로컬 " + hp.transform.localPosition.ToString("F2"));
        repaint?.Invoke();
    }
    internal EnemyHealth[] AggroTargets(GameObject root)
    {
        if (!showAggro) return Array.Empty<EnemyHealth>();
        if (allAggro) return MonsterPlacementStore.Enemies(root);
        var selected = Selected(root);
        return selected ? new[] { selected } : Array.Empty<EnemyHealth>();
    }
    internal static float AggroRadius(EnemyHealth enemy)
    {
        var s = MonsterPlacementStore.Species(enemy);
        if (s == null) return 0;
        using (var so = new SerializedObject(enemy.GetComponent(s.AI)))
        {
            float radius = so.FindProperty("aggroRadius").floatValue;
            return float.IsNaN(radius) || float.IsInfinity(radius) ? 0 : Mathf.Max(0, radius);
        }
    }
}
