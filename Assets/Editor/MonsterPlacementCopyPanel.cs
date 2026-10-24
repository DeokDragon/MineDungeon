using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[Serializable] internal sealed class MonsterPlacementCopyPanel
{
    [SerializeField] DungeonFloorData sourceFloor, targetFloor;
    [SerializeField] int sourceDifficulty, targetDifficulty, sourceMap, targetMap;
    bool expanded;
    Vector2 scroll;
    MonsterMapProfile source, target, draft;
    string sourceBaseline, targetBaseline, selectionKey, error;
    MonsterPlacementStore.Report report;
    public void Clear() { if (draft) UnityEngine.Object.DestroyImmediate(draft); draft = null; report = null; }
    internal static void Apply(MonsterMapProfile source, MonsterMapProfile target, MonsterMapProfile draft, string sourceBaseline, string targetBaseline)
    {
        MonsterDataStore.Guard();
        if (MonsterProfileStage.Current) throw new InvalidOperationException("독립 배치 Scene을 닫은 뒤 프로필 복사를 적용하세요.");
        if (MonsterProfileEditorStore.Json(source) != sourceBaseline) throw new InvalidOperationException("원본이 변경되었습니다. 복사 미리보기를 다시 만드세요.");
        var report = MonsterProfileEditorStore.ValidateDraft(draft);
        if (report.Errors.Count != 0) throw new InvalidOperationException(string.Join("\n", report.Errors));
        MonsterProfileEditorStore.Save(draft, target, targetBaseline);
    }
    public void Draw()
    {
        expanded = EditorGUILayout.Foldout(expanded, "독립 배치 프로필 복사 (기본 프리팹 배치와 별도)", true);
        if (!expanded) return;
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(280));
        EditorGUILayout.HelpBox("저장된 원본의 종류·좌표·회전·배율만 대상 독립 배치에 복사합니다. 기본 능력치는 공용 프리팹을 사용하며 밸런스와 대상 설정 완료 상태는 유지합니다. 대상이 없으면 기존 기능으로 빈 프로필을 먼저 만드세요.", MessageType.Info);
        EditorGUILayout.LabelField("원본", EditorStyles.boldLabel); MonsterProfileEditorStore.Selectors(ref sourceFloor, ref sourceDifficulty);
        sourceMap = EditorGUILayout.Popup("원본 맵", sourceMap, Enumerable.Range(1, 10).Select(i => $"Map_{i:00}").ToArray());
        EditorGUILayout.LabelField("대상", EditorStyles.boldLabel); MonsterProfileEditorStore.Selectors(ref targetFloor, ref targetDifficulty);
        targetMap = EditorGUILayout.Popup("대상 맵", targetMap, Enumerable.Range(1, 10).Select(i => $"Map_{i:00}").ToArray());
        string key = (sourceFloor ? sourceFloor.GetInstanceID() : 0) + ":" + sourceDifficulty + ":" + sourceMap + ":" + (targetFloor ? targetFloor.GetInstanceID() : 0) + ":" + targetDifficulty + ":" + targetMap;
        if (selectionKey != key) { Clear(); selectionKey = key; error = null; }
        if (GUILayout.Button("원본 / 대상 비교·검증"))
            try
            {
                Clear();
                source = MonsterProfileEditorStore.Find(sourceFloor, sourceDifficulty, AssetDatabase.LoadAssetAtPath<DungeonRoom>(MonsterPlacementStore.Paths[sourceMap]));
                target = MonsterProfileEditorStore.Find(targetFloor, targetDifficulty, AssetDatabase.LoadAssetAtPath<DungeonRoom>(MonsterPlacementStore.Paths[targetMap]));
                draft = MonsterPlacementBatch.CopyProfile(source, target);
                sourceBaseline = MonsterProfileEditorStore.Json(source); targetBaseline = MonsterProfileEditorStore.Json(target);
                report = MonsterProfileEditorStore.ValidateDraft(draft); error = null;
            }
            catch (Exception e) { error = e.Message; }
        if (draft && report != null)
        {
            EditorGUILayout.LabelField($"{sourceFloor.name}/{sourceDifficulty}/{source.map.name} → {targetFloor.name}/{targetDifficulty}/{target.map.name}");
            EditorGUILayout.LabelField($"대상 {target.entries.Length} → {draft.entries.Length}마리 · 설정 완료 {target.configured} 유지");
            foreach (var e in target.entries) EditorGUILayout.LabelField("기존: " + (e.template ? MonsterProfileRuntime.Kind(e.template).ToString() : "참조 누락") + " " + e.localPosition);
            foreach (var e in draft.entries) EditorGUILayout.LabelField("복사: " + MonsterProfileRuntime.Kind(e.template) + " " + e.localPosition);
            foreach (string e in report.Errors) EditorGUILayout.HelpBox(e, MessageType.Error);
            foreach (string w in report.Warnings) EditorGUILayout.HelpBox(w, MessageType.Warning);
            if (MonsterProfileStage.Current && GUILayout.Button("독립 배치 Scene 닫기 (초안 유지)")) StageUtility.GoToMainStage();
            using (new EditorGUI.DisabledScope(report.Errors.Count != 0 || MonsterProfileStage.Current))
                if (GUILayout.Button("확인 후 대상 배치 전체 덮어쓰기"))
                    try
                    {
                        if (MonsterProfileEditorStore.Find(sourceFloor, sourceDifficulty, AssetDatabase.LoadAssetAtPath<DungeonRoom>(MonsterPlacementStore.Paths[sourceMap])) != source ||
                            MonsterProfileEditorStore.Find(targetFloor, targetDifficulty, AssetDatabase.LoadAssetAtPath<DungeonRoom>(MonsterPlacementStore.Paths[targetMap])) != target)
                            throw new InvalidOperationException("프로필 연결이 변경되었습니다. 비교·검증을 다시 실행하세요.");
                        if (EditorUtility.DisplayDialog("대상 독립 배치 덮어쓰기", $"{targetFloor.name}/{DungeonFloorData.DifficultyName(targetDifficulty)}/{target.map.name}\n기존 {target.entries.Length}마리를 {draft.entries.Length}마리로 전부 대체할까요? 원본과 밸런스는 유지됩니다.", "덮어쓰기", "취소"))
                        { Apply(source, target, draft, sourceBaseline, targetBaseline); Clear(); error = "대상 독립 배치만 저장했습니다. Undo/Redo 지원."; }
                    }
                    catch (Exception e) { error = e.Message; }
        }
        if (error != null) EditorGUILayout.HelpBox(error, MessageType.Info);
        EditorGUILayout.EndScrollView();
    }
}
