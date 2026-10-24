using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Editor-only adapter. Prefab components remain the sole source of game data.
internal static class MonsterDataStore
{
    internal sealed class Field
    {
        public string Key, Label, Property;
        public bool Health, Integer;
        public float Min, Max = float.MaxValue;
        public Field(string key, string label, float min = 0, bool integer = false, bool health = false)
        { Key = Property = key; Label = label; Min = min; Integer = integer; Health = health; }
    }

    internal sealed class Species
    {
        public string Id, Label;
        public Type AI;
        public Field[] Fields;
        public string Path => "Assets/Prefabs/Monsters/" + Id + "Monster.prefab";
        public Species(string id, string label, Type ai, params Field[] extra)
        {
            Id = id; Label = label; AI = ai;
            Fields = new[] {
                new Field("maxHealth", "HP", 1, true, true),
                new Field("attackDamage", "공격력", 0, true),
                new Field("moveSpeed", "이동 속도"),
                new Field("attackInterval", "공격 쿨타임"),
                new Field(id == "Stone" ? "stopDistance" : "attackDistance", "공격 시작 거리"),
                new Field("warningDuration", "예고 시간"),
                new Field("recoveryDuration", "후딜레이"),
                new Field("aggroRadius", "인식 반경 (임시)"),
                new Field("shakeStrength", "흔들림 강도"),
                new Field("shakeDuration", "흔들림 시간")
            }.Concat(extra).ToArray();
        }
    }

    internal static readonly Species[] Catalog = {
        new Species("Stone", "돌", typeof(StoneMonsterAI), new Field("jumpDuration", "도약 시간", .01f),
            new Field("jumpHeight", "도약 높이"), new Field("maxJumpDistance", "최대 도약 거리"), new Field("attackRadius", "착지 반경")),
        new Species("Iron", "철", typeof(IronMonsterAI), new Field("chargeSpeed", "돌진 속도", .0001f),
            new Field("chargeWidth", "돌진 폭", .0001f), new Field("chargeDistance", "돌진 거리"), new Field("wallRecovery", "벽 충돌 후딜레이")),
        new Species("Crystal", "수정", typeof(CrystalMonsterAI), new Field("projectileSpeed", "투사체 속도", .0001f),
            new Field("projectileDiameter", "투사체 지름", .0001f), new Field("projectileRange", "투사체 거리"),
            new Field("retreatDistance", "후퇴 거리"), new Field("retreatSpeed", "후퇴 속도")),
        new Species("Coal", "석탄", typeof(CoalMonsterAI), new Field("warningRadius", "예고 반경"), new Field("damageRadius", "피해 반경")),
        new Species("Magnet", "자철석", typeof(MagnetMonsterAI), new Field("warningRadius", "예고 반경"), new Field("damageRadius", "피해 반경"),
            new Field("slowFraction", "감속 비율 (0~1)") { Max = 1 }, new Field("slowDuration", "감속 시간"))
    };
    internal static string[] Paths => Catalog.Select(s => s.Path).Concat(Enumerable.Range(1, 10)
        .Select(i => $"Assets/Prefabs/Rooms/RegularCaves/Map_{i:00}.prefab")).ToArray();
    internal static string Key(Species s, Field f) => s.Id + "." + f.Key;
    internal static string Number(double value) => value == Math.Truncate(value) ? value.ToString("R", CultureInfo.InvariantCulture) : ((float)value).ToString("R", CultureInfo.InvariantCulture);

    internal static double Parse(Field f, string text)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new FormatException("빈 값은 사용할 수 없습니다.");
        if (f.Integer)
        {
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) || n < f.Min)
                throw new FormatException($"{f.Label}: {f.Min} 이상 정수가 필요합니다.");
            return n;
        }
        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ||
            float.IsNaN(value) || float.IsInfinity(value) || value < f.Min || value > f.Max)
            throw new FormatException($"{f.Label}: {f.Min}~{f.Max} 범위의 유한한 숫자가 필요합니다.");
        return value;
    }

    internal static Component ComponentFor(EnemyHealth hp, Species s, Field f) => f.Health ? hp : hp.GetComponent(s.AI);
    internal static double Read(Component component, Field field)
    {
        if (!component) throw new InvalidOperationException("필수 컴포넌트가 없습니다.");
        using (var so = new SerializedObject(component))
        {
            var p = so.FindProperty(field.Property);
            if (p == null || p.propertyType != (field.Integer ? SerializedPropertyType.Integer : SerializedPropertyType.Float))
                throw new InvalidOperationException(component.GetType().Name + "." + field.Property + " 필드 불일치");
            return field.Integer ? (double)p.intValue : p.floatValue;
        }
    }
    internal static Dictionary<string, string> Load()
    {
        var result = new Dictionary<string, string>();
        foreach (var s in Catalog)
        {
            var root = Root(s.Path);
            var hp = root.GetComponent<EnemyHealth>();
            foreach (var f in s.Fields)
                result.Add(Key(s, f), Number(Read(ComponentFor(hp, s, f), f)));
        }
        return result;
    }
    internal static GameObject Root(string path)
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!root || PrefabUtility.GetPrefabAssetType(root) != PrefabAssetType.Regular)
            throw new InvalidOperationException("일반 프리팹이 없거나 Variant입니다: " + path);
        return root;
    }
    internal static Dictionary<string, double> Validate(Dictionary<string, string> draft)
    {
        var values = new Dictionary<string, double>();
        foreach (var s in Catalog)
            foreach (var f in s.Fields)
            {
                string key = Key(s, f);
                if (!draft.TryGetValue(key, out string text)) throw new FormatException("누락된 필드: " + key);
                try { values.Add(key, Parse(f, text)); }
                catch (FormatException e) { throw new FormatException(key + ": " + e.Message); }
            }
        if (draft.Count != values.Count) throw new FormatException("알 수 없는 몬스터 ID 또는 필드가 있습니다.");
        return values;
    }

    internal sealed class Change
    {
        public string Path, ObjectPath, Id;
        public Component Target;
        public Field Field;
        public double Before, After;
        public string Description => $"{ObjectPath} | {Id}.{Field.Key}: {Number(Before)} → {Number(After)}";
    }
    internal sealed class Plan
    {
        public readonly List<Change> Changes = new List<Change>();
        public readonly List<string> Differences = new List<string>();
        public readonly Dictionary<string, string> Fingerprints = new Dictionary<string, string>();
        public string[] ChangedPaths => Changes.Select(c => c.Path).Distinct().ToArray();
    }
    static string Fingerprint(string path)
    {
        using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
    }
    static string ObjectPath(Transform t, Transform root)
    {
        var parts = new List<string>();
        while (t) { parts.Add(t.name + "[" + t.GetSiblingIndex() + "]"); if (t == root) break; t = t.parent; }
        parts.Reverse(); return string.Join("/", parts);
    }
    // Path override is used only by the isolated Editor verification, never by the window or CSV.
    internal static Plan BuildPlan(Dictionary<string, string> draft, string[] paths = null)
    {
        Guard();
        if (MonsterProfileStage.Current) throw new InvalidOperationException("독립 배치 Scene을 닫은 뒤 기본 수치를 적용하세요. 이후 배치를 다시 열면 새 기본값을 표시합니다.");
        var values = Validate(draft);
        var baseline = Load();
        var plan = new Plan();
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        foreach (string path in paths ?? Paths)
        {
            if (stage != null && stage.assetPath == path) throw new InvalidOperationException("Prefab Mode를 닫아 주세요: " + path);
            var root = Root(path);
            if (!AssetDatabase.IsOpenForEdit(path) || (File.GetAttributes(path) & FileAttributes.ReadOnly) != 0)
                throw new IOException("쓰기 불가: " + path);
            foreach (var obj in root.GetComponentsInChildren<Component>(true))
                if (obj && EditorUtility.IsDirty(obj)) throw new InvalidOperationException("저장되지 않은 프리팹 변경을 먼저 저장하세요: " + path);
            plan.Fingerprints.Add(path, Fingerprint(path));
            foreach (var hp in root.GetComponentsInChildren<EnemyHealth>(true))
            {
                if (hp.GetComponent<StoneGolemBossAI>()) throw new InvalidOperationException("보스 포함 대상은 거부합니다: " + path);
                var matches = Catalog.Where(s => hp.GetComponent(s.AI)).ToArray();
                if (matches.Length != 1) throw new InvalidOperationException("몬스터 AI 식별 실패: " + path + "/" + hp.name);
                var s = matches[0];
                foreach (var f in s.Fields)
                {
                    var target = ComponentFor(hp, s, f);
                    double before = Read(target, f), after = values[Key(s, f)];
                    string location = ObjectPath(hp.transform, root.transform);
                    double common = Parse(f, baseline[Key(s, f)]);
                    if (before != common) plan.Differences.Add($"{path} | {location} | {s.Id}.{f.Key}: 공용 {Number(common)} / 개체 {Number(before)}");
                    if (before != after) plan.Changes.Add(new Change { Path = path, ObjectPath = location, Id = s.Id,
                        Target = target, Field = f, Before = before, After = after });
                }
            }
        }
        return plan;
    }
    internal static void Guard()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Play/컴파일/에셋 갱신 중에는 적용할 수 없습니다.");
    }

    // Persistent prefab objects participate in Unity Undo; save only affected prefab assets.
    // The subscription survives closing the window. It is reinstalled after domain reload.
    const string UndoPathsKey = "MineDungeon.MonsterData.UndoPaths";
    [InitializeOnLoadMethod]
    static void InitializeUndo() { Undo.undoRedoEvent -= PersistUndo; Undo.undoRedoEvent += PersistUndo; }
    static void PersistUndo(in UndoRedoInfo info)
    {
        if (info.undoName != "광산던전 몬스터 수치 적용" || EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach (string path in SessionState.GetString(UndoPathsKey + "." + info.undoGroup, "").Split('|'))
        {
            if (string.IsNullOrEmpty(path)) continue;
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root && root.GetComponentsInChildren<Component>(true).Any(c => c && EditorUtility.IsDirty(c))) Save(path);
        }
    }
    static void Save(string path)
    {
        PrefabUtility.SavePrefabAsset(Root(path), out bool success);
        if (!success) throw new IOException("프리팹 저장 실패: " + path);
    }
    internal static int Apply(Plan plan)
    {
        Guard();
        if (MonsterProfileStage.Current) throw new InvalidOperationException("독립 배치 Scene을 닫은 뒤 기본 수치를 적용하세요.");
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        foreach (var pair in plan.Fingerprints)
        {
            if (stage != null && stage.assetPath == pair.Key) throw new InvalidOperationException("Prefab Mode를 닫고 다시 미리보기 하세요.");
            if (!AssetDatabase.IsOpenForEdit(pair.Key) || (File.GetAttributes(pair.Key) & FileAttributes.ReadOnly) != 0)
                throw new IOException("쓰기 불가: " + pair.Key);
            if (Root(pair.Key).GetComponentsInChildren<Component>(true).Any(c => c && EditorUtility.IsDirty(c)))
                throw new InvalidOperationException("미리보기 이후 저장되지 않은 변경이 있습니다. 먼저 저장하고 다시 미리보기 하세요.");
            if (Fingerprint(pair.Key) != pair.Value) throw new InvalidOperationException("미리보기 이후 파일이 변경되었습니다. 다시 미리보기 하세요: " + pair.Key);
        }
        foreach (var c in plan.Changes)
            if (!c.Target || Read(c.Target, c.Field) != c.Before) throw new InvalidOperationException("미리보기 이후 개체가 변경되었습니다. 다시 미리보기 하세요.");
        if (plan.Changes.Count == 0) return 0;
        var paths = plan.ChangedPaths;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        SessionState.SetString(UndoPathsKey + "." + group, string.Join("|", paths));
        Undo.SetCurrentGroupName("광산던전 몬스터 수치 적용");
        try
        {
            Undo.RegisterCompleteObjectUndo(plan.Changes.Select(c => (UnityEngine.Object)c.Target).Distinct().ToArray(), "광산던전 몬스터 수치 적용");
            foreach (var c in plan.Changes)
            {
                using (var so = new SerializedObject(c.Target))
                {
                    var p = so.FindProperty(c.Field.Property);
                    if (c.Field.Integer) p.intValue = checked((int)c.After); else p.floatValue = (float)c.After;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                if (PrefabUtility.IsPartOfPrefabInstance(c.Target)) PrefabUtility.RecordPrefabInstancePropertyModifications(c.Target);
                EditorUtility.SetDirty(c.Target);
            }
            foreach (var path in paths) Save(path);
            Undo.CollapseUndoOperations(group);
            return paths.Length;
        }
        catch
        {
            Undo.RevertAllDownToGroup(group);
            foreach (var path in paths) Save(path);
            throw;
        }
        finally { Undo.IncrementCurrentGroup(); }
    }

    internal static string ExportCsv(Dictionary<string, string> draft)
    {
        Validate(draft);
        var b = new StringBuilder("monsterId,field,value\r\n");
        foreach (var s in Catalog)
            foreach (var f in s.Fields) b.Append(s.Id).Append(',').Append(f.Key).Append(',').Append(draft[Key(s, f)].Trim()).Append("\r\n");
        return b.ToString();
    }
    // Strict long format supports full exports and partial balancing patches. Atomic import.
    internal static Dictionary<string, string> ImportCsv(string csv, Dictionary<string, string> current)
    {
        var rows = CsvRows(csv.TrimStart('\uFEFF'));
        if (rows.Count < 2 || rows[0].Count != 3 || string.Join(",", rows[0]) != "monsterId,field,value")
            throw new FormatException("CSV 헤더는 monsterId,field,value 이어야 합니다.");
        var next = new Dictionary<string, string>(current);
        var seen = new HashSet<string>();
        for (int i = 1; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row.Count != 3) throw new FormatException($"CSV {i + 1}행: 열은 3개여야 합니다.");
            var s = Catalog.SingleOrDefault(x => x.Id == row[0].Trim());
            var f = s?.Fields.SingleOrDefault(x => x.Key == row[1].Trim());
            if (f == null) throw new FormatException($"CSV {i + 1}행: 알 수 없는 ID/필드");
            string key = Key(s, f);
            if (!seen.Add(key)) throw new FormatException("중복 필드: " + key);
            Parse(f, row[2]);
            next[key] = row[2].Trim();
        }
        Validate(next);
        return next;
    }
    internal static List<List<string>> CsvRows(string csv)
    {
        var rows = new List<List<string>>(); var row = new List<string>(); var cell = new StringBuilder();
        bool quoted = false, closed = false;
        for (int i = 0; i < csv.Length; i++)
        {
            char c = csv[i];
            if (quoted)
            {
                if (c == '"') { if (i + 1 < csv.Length && csv[i + 1] == '"') { cell.Append('"'); i++; } else { quoted = false; closed = true; } }
                else cell.Append(c);
                continue;
            }
            if (c == '"' && cell.Length == 0 && !closed) { quoted = true; continue; }
            if (c == ',' || c == '\r' || c == '\n')
            {
                row.Add(cell.ToString()); cell.Clear(); closed = false;
                if (c != ',') { rows.Add(row); row = new List<string>(); if (c == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n') i++; }
            }
            else { if (closed || c == '"') throw new FormatException($"CSV {rows.Count + 1}행: 따옴표 형식 오류"); cell.Append(c); }
        }
        if (quoted) throw new FormatException($"CSV {rows.Count + 1}행: 따옴표가 닫히지 않았습니다.");
        if (cell.Length > 0 || row.Count > 0 || closed) { row.Add(cell.ToString()); rows.Add(row); }
        return rows;
    }
}
