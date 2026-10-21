using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Exchange format only: persistent data remains in the existing floor profiles.
internal static class MonsterBalanceCsv
{
    internal const string Multipliers = "floorId,difficultyId,floorHpMultiplier,floorAttackMultiplier,difficultyHpMultiplier,difficultyAttackMultiplier,floorConfigured,difficultyConfigured";
    internal const string Adjustments = "floorId,difficultyId,monsterId,hpAdjustment,attackAdjustment,floorConfigured,difficultyConfigured";
    internal sealed class Item
    {
        internal DungeonFloorData floor;
        internal MonsterFloorProfile source, draft;
        internal string baseline;
        internal List<string> rows = new List<string>();
    }
    internal sealed class Batch : IDisposable
    {
        internal List<Item> items = new List<Item>();
        public void Dispose() { foreach (var i in items) if (i.draft) UnityEngine.Object.DestroyImmediate(i.draft); items.Clear(); }
        internal void Apply()
        {
            Validate();
            MonsterProfileEditorStore.SaveMany(items.Select(i => new MonsterProfileEditorStore.Change(i.draft, i.source, i.baseline)).ToArray());
        }
        internal void Validate()
        {
            foreach (var i in items)
                try
                {
                    if (!i.floor || i.floor.monsterProfile != i.source || MonsterProfileEditorStore.Json(i.source) != i.baseline)
                        throw new InvalidOperationException("원본이 변경되었습니다. CSV 초안을 버리고 다시 가져오세요.");
                    MonsterProfileEditorStore.ValidateBalanceDraft(i.draft);
                }
                catch (Exception e) { throw new InvalidOperationException(string.Join(", ", i.rows) + " / " + (i.floor ? i.floor.id : "삭제된 층") + ": " + e.Message); }
        }
    }
    static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);
    static string Quote(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
    internal static string Export(bool adjustments, DungeonFloorData[] floors = null)
    {
        floors = floors ?? MonsterProfileEditorStore.Floors();
        CheckFloorIds(floors);
        var b = new StringBuilder(adjustments ? Adjustments : Multipliers); b.Append("\r\n");
        foreach (var f in floors.Where(f => f.monsterProfile))
        {
            var p = f.monsterProfile;
            for (int d = 0; d < 4; d++)
            {
                var v = p.GetDifficulty(d); if (v == null || f.GetDifficulty(d) == null) continue;
                string prefix = Quote(f.id) + "," + d + ",", status = "," + p.configured + "," + v.configured + "\r\n";
                if (!adjustments) b.Append(prefix + Number(p.hpMultiplier) + "," + Number(p.attackMultiplier) + "," + Number(v.hpMultiplier) + "," + Number(v.attackMultiplier) + status);
                else
                {
                    if (v.adjustments == null || v.adjustments.Length != 5) throw new InvalidOperationException(f.id + ": 5종 보정 데이터 누락");
                    for (int m = 0; m < 5; m++) b.Append(prefix + MonsterDataStore.Catalog[m].Id + "," + v.adjustments[m].hp.ToString(CultureInfo.InvariantCulture) + "," + v.adjustments[m].attack.ToString(CultureInfo.InvariantCulture) + status);
                }
            }
        }
        return b.ToString();
    }
    static void CheckFloorIds(DungeonFloorData[] floors)
    {
        if (floors.Any(f => !f || string.IsNullOrWhiteSpace(f.id)) || floors.GroupBy(f => f.id).Any(g => g.Count() > 1))
            throw new InvalidOperationException("기존 층 ID가 비었거나 중복됩니다. ID를 확인하세요.");
    }
    // Blank numeric cells and omitted numeric columns mean 'keep current'. IDs are always required.
    // Failed import leaves both the previous batch and persistent assets unchanged.
    internal static Batch Import(string csv, bool adjustments, Batch previous = null, DungeonFloorData[] floors = null, string filename = "CSV")
    {
        floors = floors ?? MonsterProfileEditorStore.Floors(); CheckFloorIds(floors);
        var result = new Batch();
        try
        {
            if (previous != null)
                foreach (var i in previous.items) result.items.Add(new Item { floor = i.floor, source = i.source, baseline = i.baseline, draft = MonsterProfileEditorStore.Draft(i.draft), rows = new List<string>(i.rows) });
            var rows = MonsterDataStore.CsvRows(csv.TrimStart('\uFEFF'));
            string[] allowed = (adjustments ? Adjustments : Multipliers).Split(',');
            if (rows.Count < 2) throw new FormatException("헤더와 데이터 행이 필요합니다.");
            var header = rows[0].Select(c => c.Trim()).ToArray();
            if (header.Distinct().Count() != header.Length || header.Any(c => !allowed.Contains(c))) throw new FormatException("1행: 중복 또는 알 수 없는 열: " + string.Join(",", header));
            var required = adjustments ? new[] { "floorId", "difficultyId", "monsterId" } : new[] { "floorId", "difficultyId" };
            if (required.Any(c => !header.Contains(c))) throw new FormatException("1행: 필수 열 " + string.Join(",", required));
            var numeric = allowed.Except(required).Where(c => !c.EndsWith("Configured")).ToArray();
            if (!numeric.Any(c => header.Contains(c))) throw new FormatException("1행: 숫자 열이 하나 이상 필요합니다.");
            var seen = new HashSet<string>(); var shared = new Dictionary<string, string>(); int count = 0;
            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r]; if (row.All(string.IsNullOrWhiteSpace)) continue;
                string context = filename + " " + (r + 1) + "행";
                try
                {
                    if (row.Count != header.Length) throw new FormatException("열 개수가 헤더와 다릅니다.");
                    Func<string, string> cell = c => { int n = Array.IndexOf(header, c); return n < 0 ? "" : row[n].Trim(); };
                    string fid = cell("floorId"), did = cell("difficultyId"), mid = adjustments ? cell("monsterId") : "";
                    var floor = floors.SingleOrDefault(f => f.id == fid);
                    if (!floor || !floor.monsterProfile) throw new FormatException("층 ID 또는 기존 밸런스 프로필 없음: '" + fid + "'");
                    if (!int.TryParse(did, NumberStyles.None, CultureInfo.InvariantCulture, out int d) || did != d.ToString(CultureInfo.InvariantCulture) || d < 0 || d > 3 || floor.GetDifficulty(d) == null || floor.monsterProfile.GetDifficulty(d) == null)
                        throw new FormatException("난이도 ID 오류: '" + did + "' (0~3)");
                    int m = adjustments ? Array.FindIndex(MonsterDataStore.Catalog, s => s.Id == mid) : -1;
                    if (adjustments && m < 0) throw new FormatException("몬스터 ID 오류: '" + mid + "'");
                    if (!seen.Add(fid + "/" + d + "/" + mid)) throw new FormatException("중복 항목: " + fid + "/" + d + "/" + mid);
                    var item = result.items.SingleOrDefault(i => i.floor == floor);
                    if (item == null)
                    {
                        item = new Item { floor = floor, source = floor.monsterProfile, draft = MonsterProfileEditorStore.Draft(floor.monsterProfile), baseline = MonsterProfileEditorStore.Json(floor.monsterProfile) };
                        result.items.Add(item);
                    }
                    var p = item.draft; var v = p.GetDifficulty(d);
                    foreach (string flag in new[] { "floorConfigured", "difficultyConfigured" })
                        if (cell(flag) != "" && (!bool.TryParse(cell(flag), out bool state) || state != (flag == "floorConfigured" ? p.configured : v.configured)))
                            throw new FormatException(flag + "='" + cell(flag) + "': 설정 상태는 CSV로 변경할 수 없습니다.");
                    foreach (string field in numeric)
                    {
                        string raw = cell(field); if (raw == "") continue;
                        if (adjustments)
                        {
                            if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)) throw new FormatException(field + "='" + raw + "': int 범위 정수가 필요합니다.");
                            if (v.adjustments == null || v.adjustments.Length != 5) throw new FormatException("기존 5종 보정 데이터 누락");
                            if (field == "hpAdjustment") v.adjustments[m].hp = value; else v.adjustments[m].attack = value;
                        }
                        else
                        {
                            if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) || float.IsNaN(value) || float.IsInfinity(value) || value < 0)
                                throw new FormatException(field + "='" + raw + "': 0 이상 유한 숫자가 필요합니다.");
                            if (field.StartsWith("floor"))
                            {
                                string key = fid + "/" + field, normalized = Number(value);
                                if (shared.TryGetValue(key, out string prior) && prior != normalized) throw new FormatException(field + "='" + raw + "': 같은 층의 공유 배율 충돌");
                                shared[key] = normalized;
                            }
                            switch (field)
                            {
                                case "floorHpMultiplier": p.hpMultiplier = value; break;
                                case "floorAttackMultiplier": p.attackMultiplier = value; break;
                                case "difficultyHpMultiplier": v.hpMultiplier = value; break;
                                case "difficultyAttackMultiplier": v.attackMultiplier = value; break;
                            }
                        }
                    }
                    item.rows.Add(context + " [" + string.Join(",", row) + "]"); count++;
                }
                catch (Exception e) { throw new FormatException(context + ": " + e.Message); }
            }
            if (count == 0) throw new FormatException("데이터 행이 없습니다.");
            // Cross-file import may temporarily be invalid until a complementary correction file is loaded.
            // Validation/preview and apply remain blocked until the entire draft is valid.
            return result;
        }
        catch { result.Dispose(); throw; }
    }
    internal static string Preview(Batch batch)
    {
        batch.Validate(); var text = new StringBuilder(); int changes = 0;
        foreach (var item in batch.items)
        {
            text.AppendLine(item.floor.id + " · " + item.floor.displayName + " / " + AssetDatabase.GetAssetPath(item.source));
            var before = item.source; var after = item.draft;
            Action<string, object, object> diff = (name, a, b) => { if (!a.Equals(b)) { changes++; text.AppendLine(name + ": " + a + " → " + b); } };
            diff("층 HP 배율", before.hpMultiplier, after.hpMultiplier); diff("층 공격 배율", before.attackMultiplier, after.attackMultiplier);
            bool shared = before.hpMultiplier != after.hpMultiplier || before.attackMultiplier != after.attackMultiplier;
            var oldCalc = MonsterProfileEditorStore.Draft(before); var newCalc = MonsterProfileEditorStore.Draft(after);
            try
            {
                oldCalc.configured = newCalc.configured = true;
                for (int d = 0; d < 4; d++)
                {
                    var a = before.GetDifficulty(d); var b = after.GetDifficulty(d);
                    bool changed = shared || a.hpMultiplier != b.hpMultiplier || a.attackMultiplier != b.attackMultiplier || !a.adjustments.SequenceEqual(b.adjustments);
                    if (!changed) continue;
                    string title = "난이도 " + d + " " + DungeonFloorData.DifficultyName(d);
                    text.AppendLine(title + " · 층 " + (after.configured ? "설정 완료" : "미설정") + " / 난이도 " + (b.configured ? "설정 완료" : "미설정 (계산 참고값, 입장 활성화 안 함)"));
                    diff(title + " HP 배율", a.hpMultiplier, b.hpMultiplier); diff(title + " 공격 배율", a.attackMultiplier, b.attackMultiplier);
                    oldCalc.GetDifficulty(d).configured = newCalc.GetDifficulty(d).configured = true;
                    Action<string, EnemyHealth> final = (label, hp) =>
                    {
                        string prior;
                        try { oldCalc.Calculate(d, hp, out int h, out int at); prior = h + " / " + at; }
                        catch (Exception e) { prior = "기존값 오류: " + e.Message; }
                        newCalc.Calculate(d, hp, out int nextHp, out int nextAttack);
                        text.AppendLine(label + " 최종 HP / 공격: " + prior + " → " + nextHp + " / " + nextAttack);
                    };
                    for (int m = 0; m < 5; m++)
                    {
                        var s = MonsterDataStore.Catalog[m];
                        diff(s.Label + " HP 보정", a.adjustments[m].hp, b.adjustments[m].hp); diff(s.Label + " 공격 보정", a.adjustments[m].attack, b.adjustments[m].attack);
                        final("공용 " + s.Label, AssetDatabase.LoadAssetAtPath<GameObject>(s.Path).GetComponent<EnemyHealth>());
                    }
                    foreach (var map in b.maps ?? Array.Empty<MonsterMapProfile>())
                        if (map && map.configured)
                            foreach (var entry in map.entries) final(map.map.name + "/" + MonsterProfileRuntime.Kind(entry.template) + " [" + entry.id + "]", entry.template);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(oldCalc); UnityEngine.Object.DestroyImmediate(newCalc); }
        }
        return changes == 0 ? "변경사항 없음. 에셋을 다시 저장하지 않습니다." : text.ToString();
    }
}
