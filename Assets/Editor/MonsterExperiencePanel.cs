using System;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEngine;

[Serializable]
internal sealed class MonsterExperiencePanel
{
    [SerializeField] DungeonFloorData floor;
    [SerializeField] int difficulty;
    [SerializeField] MonsterFloorProfile source;
    [SerializeField] string baseline;
    [SerializeField] string[] values;
    [SerializeField] bool configured;
    bool preview;
    string message;
    Vector2 scroll;
    void Load()
    {
        source = floor ? floor.monsterProfile : null; preview = false; message = null;
        if (!source) { values = null; return; }
        baseline = MonsterProfileEditorStore.Json(source);
        var d = source.GetDifficulty(difficulty);
        MonsterFloorProfile.ValidateExperienceValues(d);
        configured = d.experienceConfigured;
        values = d.experience.Concat(new[] { d.bossExperience }).Select(n => n.ToString(CultureInfo.InvariantCulture)).ToArray();
    }
    internal static int Parse(string value)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) || n < 0)
            throw new InvalidOperationException("EXP는 0~2147483647 정수여야 합니다: " + value);
        return n;
    }
    MonsterFloorProfile Draft()
    {
        var numbers = values.Select(Parse).ToArray();
        var draft = MonsterProfileEditorStore.Draft(source);
        var d = draft.GetDifficulty(difficulty);
        d.experienceConfigured = configured; d.experience = numbers.Take(5).ToArray(); d.bossExperience = numbers[5];
        MonsterFloorProfile.ValidateExperienceValues(d);
        return draft;
    }
    internal void Draw()
    {
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
        {
            var oldFloor = floor; int oldDifficulty = difficulty;
            MonsterProfileEditorStore.Selectors(ref floor, ref difficulty);
            if (oldFloor != floor || oldDifficulty != difficulty || (floor && source != floor.monsterProfile) || values == null)
            {
                if (values != null && !EditorUtility.DisplayDialog("EXP 초안 전환", "다른 선택으로 이동하면 미저장 EXP 초안이 사라집니다.", "이동", "취소"))
                { floor = oldFloor; difficulty = oldDifficulty; }
                else { try { Load(); } catch (Exception e) { message = e.Message; } }
            }
            if (!floor) return;
            if (!source)
            {
                EditorGUILayout.HelpBox("층 프로필 미설정. EXP 및 배치는 자동 생성되지 않습니다.", MessageType.Warning);
                return;
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.HelpBox("층·난이도·종류별 처치 EXP입니다. 설정 완료와 EXP 0은 별개입니다. 보상 에셋의 기본값은 변경하지 않습니다.\n1층 쉬움 초기값은 임시 테스트 수치이며 다른 조건은 직접 작성해야 합니다. 초안은 저장 확인 후 적용됩니다.", MessageType.Info);
            configured = EditorGUILayout.Toggle("EXP 설정 완료", configured);
            for (int i = 0; i < 6; i++) values[i] = EditorGUILayout.TextField(i == 5 ? "돌골렘 보스 EXP" : MonsterDataStore.Catalog[i].Label + " EXP", values[i]);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("변경 미리보기")) preview = true;
                if (GUILayout.Button("저장값 다시 불러오기") && EditorUtility.DisplayDialog("초안 초기화", "저장값을 다시 불러올까요?", "불러오기", "취소")) Load();
            }
            try
            {
                var draft = Draft();
                try
                {
                    if (preview)
                    {
                        var old = source.GetDifficulty(difficulty); var next = draft.GetDifficulty(difficulty);
                        EditorGUILayout.LabelField($"설정 완료: {old.experienceConfigured} → {next.experienceConfigured}");
                        for (int i = 0; i < 6; i++) EditorGUILayout.LabelField((i == 5 ? "돌골렘" : MonsterDataStore.Catalog[i].Label) + $": {(i == 5 ? old.bossExperience : old.experience[i])} → {(i == 5 ? next.bossExperience : next.experience[i])}");
                        if (GUILayout.Button("확인 후 선택 조합 EXP 저장") && EditorUtility.DisplayDialog("EXP 저장", floor.floorNumber + "층 " + DungeonFloorData.DifficultyName(difficulty) + "의 표시한 EXP만 저장할까요?", "저장", "취소"))
                        { MonsterProfileEditorStore.Save(draft, source, baseline); Load(); message = "저장 완료. Undo/Redo 지원."; }
                    }
                    DrawAnalysis(draft);
                }
                finally { UnityEngine.Object.DestroyImmediate(draft); }
            }
            catch (Exception e) { EditorGUILayout.HelpBox(e.Message, MessageType.Warning); }
            if (message != null) EditorGUILayout.HelpBox(message, MessageType.Info);
            EditorGUILayout.EndScrollView();
        }
    }
    void DrawAnalysis(MonsterFloorProfile draft)
    {
        EditorGUILayout.Space(); EditorGUILayout.LabelField("현재 EXP 초안 기준 예상 경험치", EditorStyles.boldLabel);
        bool ready = draft.GetDifficulty(difficulty).experienceConfigured;
        for (int i = 1; i <= 7; i++)
        {
            var map = AssetDatabase.LoadAssetAtPath<DungeonRoom>($"Assets/Prefabs/Rooms/ReferenceMaps/Map_{i:00}.prefab");
            var layout = draft.GetDifficulty(difficulty).Find(map);
            if (!layout || !layout.configured) { EditorGUILayout.LabelField(map.name + ": 배치 미설정"); continue; }
            try
            {
                draft.ValidateMap(difficulty, layout);
                var counts = new int[5]; foreach (var entry in layout.entries) counts[(int)MonsterProfileRuntime.Kind(entry.template)]++;
                string count = string.Join(" / ", counts.Select((n, k) => MonsterDataStore.Catalog[k].Label + " " + n));
                EditorGUILayout.LabelField(map.name + " · " + count + " · " + (ready ? draft.ExpectedExperience(difficulty, layout) + " EXP" : "EXP 미설정"));
            }
            catch (Exception e) { EditorGUILayout.HelpBox(map.name + ": " + e.Message, MessageType.Warning); }
        }
        try
        {
            draft.ValidateExperience(difficulty);
            var d = floor.GetDifficulty(difficulty);
            if (d == null || d.normalRooms == null) throw new InvalidOperationException("일반 맵 후보 미설정");
            var totals = d.normalRooms.Select(m => draft.ExpectedExperience(difficulty, draft.GetDifficulty(difficulty).Find(m))).ToArray();
            var one = MonsterExperienceAnalysis.Calculate(totals, 1, 0);
            var full = MonsterExperienceAnalysis.Calculate(totals, d.normalRoomCount, draft.GetDifficulty(difficulty).bossExperience);
            EditorGUILayout.LabelField($"일반 방 1개 · 최소 {one.min} / 최대 {one.max} / 평균 {one.average:F2} EXP");
            EditorGUILayout.LabelField($"일반 방 {d.normalRoomCount}개 + 보스 · 최소 {full.min} / 최대 {full.max} / 평균 {full.average:F2} EXP");
            EditorGUILayout.LabelField($"실제 후보 {totals.Length}개: 첫 방 균등, 이후 직전 후보 인덱스 제외. 의도한 0마리 배치는 0 EXP.");
        }
        catch (Exception e) { EditorGUILayout.HelpBox("전체 분석 미확정: " + e.Message, MessageType.Warning); }
    }
}
