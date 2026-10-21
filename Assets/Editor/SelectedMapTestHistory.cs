using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

internal static class SelectedMapTestHistory
{
    internal static readonly string Path = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Library/MineDungeonEditor/SelectedMapTestHistory.json"));
    const string MetaKey = SelectedMapTestSafety.Prefix + "HistoryMeta";
    [Serializable] internal sealed class Row
    {
        public string runId, startedAtUtc, floorId, floorName, mapId, status;
        public int difficulty, deaths, total;
        public float seconds;
        public long damageTaken, damageDealt, kills;
    }
    [Serializable] sealed class FileData { public int version = 1; public List<Row> rows = new List<Row>(); }
    internal static string Error { get; private set; }
    internal static Row[] Load()
    {
        if (!File.Exists(Path)) return Array.Empty<Row>();
        var data = JsonUtility.FromJson<FileData>(File.ReadAllText(Path, Encoding.UTF8));
        if (data == null || data.version != 1 || data.rows == null || data.rows.Any(r => r == null || string.IsNullOrEmpty(r.runId)) || data.rows.GroupBy(r => r.runId).Any(g => g.Count() > 1))
            throw new IOException("개발용 테스트 기록 형식 오류. 기존 파일을 덮어쓰지 않습니다: " + Path);
        return data.rows.ToArray();
    }
    internal static void Begin(DungeonFloorData floor, int difficulty, DungeonRoom map)
    {
        var row = new Row { runId = Guid.NewGuid().ToString("N"), startedAtUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            floorId = floor.id, floorName = floor.displayName, difficulty = difficulty, mapId = map.name };
        SessionState.SetString(MetaKey, JsonUtility.ToJson(row)); Error = null;
    }
    internal static void Upsert(Row row)
    {
        var rows = Load().ToList(); int index = rows.FindIndex(r => r.runId == row.runId);
        if (index >= 0 && (rows[index].status == "success" || rows[index].status == "failed")) return;
        if (index < 0) rows.Add(row); else rows[index] = row;
        var data = new FileData { rows = rows };
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
        string temporary = Path + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonUtility.ToJson(data, true), new UTF8Encoding(false));
            if (File.Exists(Path)) File.Replace(temporary, Path, Path + ".bak"); else File.Move(temporary, Path);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    internal static void Archive(SelectedMapTestRunner.Record stats, string status)
    {
        try
        {
            var row = JsonUtility.FromJson<Row>(SessionState.GetString(MetaKey, "{}"));
            if (row == null || string.IsNullOrEmpty(row.runId)) return;
            if (SessionState.GetString(MetaKey + "Final", "") == row.runId) return;
            row.status = status; row.seconds = stats.seconds; row.damageTaken = stats.damageTaken; row.damageDealt = stats.damageDealt;
            row.kills = stats.kills; row.deaths = stats.deaths; row.total = stats.total;
            Upsert(row); Error = null;
            if (status == "success" || status == "failed") SessionState.SetString(MetaKey + "Final", row.runId);
        }
        catch (Exception e) { Error = "테스트 기록 저장 실패 (캐릭터 저장과 무관): " + e.Message; }
    }
    static string Text(string text)
    {
        text = text ?? "";
        if (text.Length > 0 && "=+-@\t\r\n".Contains(text[0])) text = "'" + text;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }
    internal static string Csv(Row[] rows)
    {
        var b = new StringBuilder("runId,startedAtUtc,floorId,floorName,difficultyId,mapId,status,seconds,damageTaken,damageDealt,kills,totalMonsters,deaths\r\n");
        foreach (var r in rows)
            b.Append(string.Join(",", new[] { Text(r.runId), Text(r.startedAtUtc), Text(r.floorId), Text(r.floorName), r.difficulty.ToString(CultureInfo.InvariantCulture), Text(r.mapId), Text(r.status),
                r.seconds.ToString("R", CultureInfo.InvariantCulture), r.damageTaken.ToString(CultureInfo.InvariantCulture), r.damageDealt.ToString(CultureInfo.InvariantCulture), r.kills.ToString(CultureInfo.InvariantCulture), r.total.ToString(CultureInfo.InvariantCulture), r.deaths.ToString(CultureInfo.InvariantCulture) })).Append("\r\n");
        return b.ToString();
    }
}
