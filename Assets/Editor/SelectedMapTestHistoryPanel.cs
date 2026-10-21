using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

[Serializable] internal sealed class SelectedMapTestHistoryPanel
{
    bool onlyCurrent;
    Vector2 scroll;
    string message;
    public void Draw(DungeonFloorData floor, int difficulty, string map)
    {
        EditorGUILayout.Space(); EditorGUILayout.LabelField("전투 테스트 결과 비교", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("개발용 로컬 기록 · Library/MineDungeonEditor · 캐릭터 진행/보상과 분리");
        onlyCurrent = EditorGUILayout.Toggle("현재 선택 조건만 비교", onlyCurrent);
        try
        {
            var rows = SelectedMapTestHistory.Load().Where(r => !onlyCurrent || (floor && r.floorId == floor.id && r.difficulty == difficulty && r.mapId == map)).Reverse().ToArray();
            if (GUILayout.Button($"표시한 {rows.Length}개 기록 CSV 내보내기"))
            {
                string path = EditorUtility.SaveFilePanel("전투 결과 CSV", "", "selected-map-results.csv", "csv");
                if (path != "") { File.WriteAllText(path, SelectedMapTestHistory.Csv(rows), new UTF8Encoding(true)); message = "CSV를 내보냈습니다. 밸런스·배치·캐릭터 데이터는 변경하지 않습니다."; }
            }
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(260));
            Header("실행 ID", "시작 시각 (UTC)", "층 / 난이도 / 맵", "상태", "초", "받은 피해", "가한 피해", "처치/전체", "사망");
            foreach (var r in rows) Header(r.runId.Substring(0, Math.Min(8, r.runId.Length)), r.startedAtUtc,
                r.floorId + " / " + DungeonFloorData.DifficultyName(r.difficulty) + " / " + r.mapId,
                Label(r.status), r.seconds.ToString("F1"), r.damageTaken.ToString(), r.damageDealt.ToString(), r.kills + "/" + r.total, r.deaths.ToString());
            EditorGUILayout.EndScrollView();
        }
        catch (Exception e) { message = e.Message; }
        if (SelectedMapTestHistory.Error != null) EditorGUILayout.HelpBox(SelectedMapTestHistory.Error, MessageType.Error);
        if (message != null) EditorGUILayout.HelpBox(message, MessageType.Info);
    }
    static void Header(params string[] cells)
    {
        float[] widths = { 80, 245, 260, 90, 65, 90, 90, 90, 50 };
        using (new EditorGUILayout.HorizontalScope())
            for (int i = 0; i < cells.Length; i++) GUILayout.Label(cells[i], GUILayout.Width(widths[i]));
    }
    static string Label(string status)
    {
        switch (status)
        {
            case "success": return "성공"; case "failed": return "실패"; case "restarted": return "재시작 종료"; case "error": return "오류 종료"; default: return "사용자 종료";
        }
    }
}
