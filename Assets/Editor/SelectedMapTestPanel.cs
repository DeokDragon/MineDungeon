using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[Serializable]
internal sealed class SelectedMapTestPanel
{
    [SerializeField] DungeonFloorData floor;
    [SerializeField] int difficulty, mapIndex;
    string message;
    [SerializeField] SelectedMapTestHistoryPanel history = new SelectedMapTestHistoryPanel();
    Vector2 pageScroll;
    public void Draw()
    {
        pageScroll = EditorGUILayout.BeginScrollView(pageScroll);
        try { DrawContents(); } finally { EditorGUILayout.EndScrollView(); }
    }
    void DrawContents()
    {
        bool active = SelectedMapTestSafety.Active;
        EditorGUILayout.HelpBox("Editor 전용 선택 맵 전투 테스트입니다. 저장된 밸런스·독립 배치만 사용하며 편집 중인 초안은 적용하지 않습니다.\n개발 테스트에서만 레벨·콘텐츠 조건을 우회합니다. 매번 새 격리 저장과 씬 기본 캐릭터로 시작하며, 전멸 시 성공 / 부활 2회 소진 후 사망 시 실패입니다. 일반 보스 진행·결과 정산은 호출하지 않습니다.", MessageType.Info);
        using (new EditorGUI.DisabledScope(active || EditorApplication.isPlayingOrWillChangePlaymode))
        {
            MonsterProfileEditorStore.Selectors(ref floor, ref difficulty);
            mapIndex = EditorGUILayout.Popup("일반 맵", mapIndex, Enumerable.Range(1, 7).Select(i => $"Map_{i:00}").ToArray());
        }
        var map = AssetDatabase.LoadAssetAtPath<DungeonRoom>($"Assets/Prefabs/Rooms/ReferenceMaps/Map_{mapIndex + 1:00}.prefab");
        string validation = null;
        try { SelectedMapTestRunner.Validate(floor, difficulty, map); }
        catch (Exception e) { validation = e.Message; }
        if (validation != null) EditorGUILayout.HelpBox(validation, MessageType.Warning);
        else
        {
            var layout = floor.monsterProfile.GetDifficulty(difficulty).Find(map);
            EditorGUILayout.LabelField($"유효한 저장 데이터 · {layout.entries.Length}마리 · {(layout.entries.Length == 0 ? "설정 완료된 빈 방" : "전투 준비 완료")}");
        }
        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(active || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || validation != null))
                if (GUILayout.Button("테스트 시작"))
                    try { SelectedMapTestRunner.Start(floor, difficulty, map); message = null; } catch (Exception e) { message = e.Message; }
            using (new EditorGUI.DisabledScope(!active))
            {
                if (GUILayout.Button("동일 조건 재시작")) SelectedMapTestRunner.Stop(true);
                if (GUILayout.Button("테스트 종료")) SelectedMapTestRunner.Stop();
            }
        }
        if (message != null) EditorGUILayout.HelpBox(message, MessageType.Error);
        EditorGUILayout.Space(); EditorGUILayout.LabelField("테스트 상태", SelectedMapTestRunner.Status);
        if (active) EditorGUILayout.SelectableLabel("격리 저장: " + SelectedMapTestSafety.Expected, GUILayout.Height(20));
        var record = SelectedMapTestRunner.Stats;
        EditorGUILayout.LabelField("경과 전투 시간", record.seconds.ToString("F1") + "초 (게임 시간 · 일시정지 제외)");
        EditorGUILayout.LabelField("플레이어가 받은 총 피해", record.damageTaken.ToString());
        EditorGUILayout.LabelField("몬스터에게 가한 총 피해", record.damageDealt.ToString());
        EditorGUILayout.LabelField("몬스터 처치", record.kills + " / " + record.total);
        EditorGUILayout.LabelField("플레이어 사망 횟수", record.deaths.ToString());
        if (validation == null) EditorGUILayout.LabelField("선택 맵 예상 EXP (저장 데이터)", floor.monsterProfile.ExpectedExperience(difficulty, floor.monsterProfile.GetDifficulty(difficulty).Find(map)).ToString());
        EditorGUILayout.LabelField("이번 테스트 예상 / 실제 획득 EXP", (record.expectedExperienceKnown ? record.expectedExperience.ToString() : "미측정") + " / " + (record.experienceMeasured ? record.experience.ToString() : "미측정"));
        EditorGUILayout.HelpBox("피해량은 기존 전투 기록의 실제 HP 감소량 기준입니다(초과 피해 제외). 기본 이동·평타·E/R 스킬·물약·회피는 Game 뷰에서 조작하세요. 재시작은 Play를 완전히 종료한 뒤 다시 시작하므로 초기화에 시간이 걸릴 수 있습니다.", MessageType.None);
        history.Draw(floor, difficulty, map ? map.name : "");
    }
}
