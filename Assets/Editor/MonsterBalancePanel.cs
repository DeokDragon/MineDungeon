using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[Serializable]
internal sealed class MonsterBalancePanel
{
    [SerializeField] DungeonFloorData floor;
    [SerializeField] int difficulty;
    [SerializeField] MonsterFloorProfile draft, source;
    [SerializeField] string baseline;
    string message;
    Vector2 scroll;
    bool preview;
    bool showMapFinals;
    void Load()
    {
        source = floor.monsterProfile;
        if (draft) UnityEngine.Object.DestroyImmediate(draft);
        draft = source ? MonsterProfileEditorStore.Draft(source) : null;
        baseline = source ? MonsterProfileEditorStore.Json(source) : null; preview = false;
    }
    public void Draw()
    {
        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling))
        {
            var old = floor; int oldDifficulty = difficulty;
            MonsterProfileEditorStore.Selectors(ref floor, ref difficulty);
            if (old != floor)
            {
                if (draft && MonsterProfileEditorStore.Json(draft) != baseline && !EditorUtility.DisplayDialog("초안 변경", "현재 밸런스 초안을 버리고 층을 바꿀까요?", "변경", "취소")) floor = old;
                else Load();
            }
            if (!floor) return;
            if (source != floor.monsterProfile) Load();
            if (!source)
            {
                EditorGUILayout.HelpBox("밸런스 미설정. 프로필 생성만으로 던전이 활성화되지 않습니다.", MessageType.Info);
                if (GUILayout.Button("빈 밸런스 프로필 만들기")) { MonsterProfileEditorStore.Ensure(floor); Load(); }
                return;
            }
            if (!draft) Load();
            if (oldDifficulty != difficulty)
            {
                if (draft && MonsterProfileEditorStore.Json(draft) != baseline && !EditorUtility.DisplayDialog("미저장 밸런스", "현재 초안을 버리고 난이도를 바꿀까요?", "변경", "취소")) difficulty = oldDifficulty;
                else Load();
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.HelpBox("최종값 = 기본값 × 층 배율 × 난이도 배율 + 종류별 보정. 마지막에 한 번 반올림하며 정확히 .5는 큰 정수로 올립니다.\n층 배율은 같은 층의 모든 난이도가 공유합니다. 종류별 보정은 선택한 난이도에만 저장합니다. 새 수치는 기획 확정 전 직접 설정하세요.", MessageType.Info);
            draft.configured = EditorGUILayout.Toggle("층 배율 설정 완료", draft.configured);
            draft.hpMultiplier = EditorGUILayout.FloatField("층 HP 배율", draft.hpMultiplier);
            draft.attackMultiplier = EditorGUILayout.FloatField("층 공격력 배율", draft.attackMultiplier);
            var d = draft.GetDifficulty(difficulty);
            d.configured = EditorGUILayout.Toggle("선택 난이도 설정 완료", d.configured);
            d.hpMultiplier = EditorGUILayout.FloatField("난이도 HP 배율", d.hpMultiplier);
            d.attackMultiplier = EditorGUILayout.FloatField("난이도 공격력 배율", d.attackMultiplier);
            for (int i = 0; i < 5; i++)
            {
                var s = MonsterDataStore.Catalog[i];
                var hp = AssetDatabase.LoadAssetAtPath<GameObject>(s.Path).GetComponent<EnemyHealth>();
                EditorGUILayout.LabelField(s.Label + $" · 기본 HP {hp.MaxHealth} / 공격력 {MonsterProfileRuntime.Attack(hp)}", EditorStyles.boldLabel);
                d.adjustments[i].hp = EditorGUILayout.IntField("HP 추가 보정", d.adjustments[i].hp);
                d.adjustments[i].attack = EditorGUILayout.IntField("공격력 추가 보정", d.adjustments[i].attack);
                try { draft.Calculate(difficulty, hp, out int health, out int damage); EditorGUILayout.LabelField($"최종 HP {health} / 공격력 {damage}"); }
                catch (Exception e) { EditorGUILayout.HelpBox(e.Message, MessageType.Warning); }
            }
            showMapFinals = EditorGUILayout.Foldout(showMapFinals, "작성된 맵의 개체별 실제 최종 수치", true);
            if (showMapFinals)
                foreach (var map in d.maps.Where(m => m && m.configured))
                    foreach (var entry in map.entries)
                    {
                        try
                        {
                            draft.Calculate(difficulty, entry.template, out int health, out int damage);
                            EditorGUILayout.LabelField($"{map.map.name} / {MonsterProfileRuntime.Kind(entry.template)} [{entry.id.Substring(0, Math.Min(6, entry.id.Length))}] · HP {entry.template.MaxHealth} → {health}, 공격 {MonsterProfileRuntime.Attack(entry.template)} → {damage}");
                        }
                        catch (Exception e) { EditorGUILayout.HelpBox(e.Message, MessageType.Warning); }
                    }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("변경 미리보기")) { preview = true; message = null; }
                if (GUILayout.Button("저장 수치 다시 불러오기") && EditorUtility.DisplayDialog("초안 초기화", "밸런스 초안을 다시 불러올까요?", "불러오기", "취소")) Load();
            }
            if (preview)
            {
                var original = source.GetDifficulty(difficulty);
                EditorGUILayout.LabelField($"층 HP {source.hpMultiplier} → {draft.hpMultiplier} / 공격 {source.attackMultiplier} → {draft.attackMultiplier}");
                EditorGUILayout.LabelField($"난이도 HP {original.hpMultiplier} → {d.hpMultiplier} / 공격 {original.attackMultiplier} → {d.attackMultiplier}");
                for (int i = 0; i < 5; i++) EditorGUILayout.LabelField($"{MonsterDataStore.Catalog[i].Label}: HP 보정 {original.adjustments[i].hp} → {d.adjustments[i].hp}, 공격 보정 {original.adjustments[i].attack} → {d.adjustments[i].attack}");
                if (GUILayout.Button("확인 후 이 층 밸런스 저장"))
                {
                    try
                    {
                        if (draft.configured && d.configured)
                        {
                            draft.ValidateBalance(difficulty);
                            foreach (var s in MonsterDataStore.Catalog) draft.Calculate(difficulty, AssetDatabase.LoadAssetAtPath<GameObject>(s.Path).GetComponent<EnemyHealth>(), out _, out _);
                            foreach (var map in d.maps.Where(m => m && m.configured)) draft.ValidateMap(difficulty, map);
                        }
                        // Save only selected difficulty changes, plus deliberately shared floor multipliers.
                        var combined = MonsterProfileEditorStore.Draft(source);
                        combined.configured = draft.configured; combined.hpMultiplier = draft.hpMultiplier; combined.attackMultiplier = draft.attackMultiplier;
                        combined.difficulties[difficulty].configured = d.configured;
                        combined.difficulties[difficulty].hpMultiplier = d.hpMultiplier; combined.difficulties[difficulty].attackMultiplier = d.attackMultiplier;
                        combined.difficulties[difficulty].adjustments = d.adjustments.ToArray();
                        if (combined.configured)
                            for (int index = 0; index < combined.difficulties.Length; index++)
                                if (combined.GetDifficulty(index).configured)
                                {
                                    combined.ValidateBalance(index);
                                    foreach (var s in MonsterDataStore.Catalog) combined.Calculate(index, AssetDatabase.LoadAssetAtPath<GameObject>(s.Path).GetComponent<EnemyHealth>(), out _, out _);
                                    foreach (var map in combined.GetDifficulty(index).maps.Where(m => m && m.configured)) combined.ValidateMap(index, map);
                                }
                        if (EditorUtility.DisplayDialog("밸런스 저장", "표시한 층 배율과 선택한 난이도의 보정·배율을 저장할까요?", "저장", "취소"))
                        { MonsterProfileEditorStore.Save(combined, source, baseline); Load(); message = "저장 완료. Ctrl+Z / Ctrl+Y 지원."; }
                        UnityEngine.Object.DestroyImmediate(combined);
                    }
                    catch (Exception e) { message = e.Message; }
                }
            }
            EditorGUILayout.EndScrollView();
            if (message != null) EditorGUILayout.HelpBox(message, MessageType.Info);
        }
    }
}
