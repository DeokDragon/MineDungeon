using System;
using System.Linq;
using UnityEngine;

public enum MineMonsterKind { Stone, Iron, Crystal, Coal, Magnet }

[CreateAssetMenu(menuName = "Mine Dungeon/Monster Floor Profile")]
public sealed class MonsterFloorProfile : ScriptableObject
{
    public bool configured;
    public float hpMultiplier = 1, attackMultiplier = 1;
    [Serializable] public struct Adjustment { public int hp, attack; }
    [Serializable] public sealed class Difficulty
    {
        public bool configured;
        public float hpMultiplier = 1, attackMultiplier = 1;
        public Adjustment[] adjustments = new Adjustment[5];
        public MonsterMapProfile[] maps = Array.Empty<MonsterMapProfile>();
        public MonsterMapProfile Find(DungeonRoom map) => maps?.SingleOrDefault(p => p && p.map == map);
    }
    public Difficulty[] difficulties = { new Difficulty(), new Difficulty(), new Difficulty(), new Difficulty() };
    public Difficulty GetDifficulty(int index) => difficulties != null && index >= 0 && index < difficulties.Length ? difficulties[index] : null;
    public bool CanStart(int index, DungeonRoom[] maps, out string error)
    {
        try
        {
            ValidateBalance(index);
            if (maps == null || maps.Length == 0) throw new InvalidOperationException("일반 맵 후보 미설정");
            var difficulty = GetDifficulty(index);
            foreach (var map in maps)
            {
                var profile = difficulty.Find(map);
                if (!profile || !profile.configured) throw new InvalidOperationException((map ? map.name : "맵") + ": 몬스터 배치 미설정");
                ValidateMap(index, profile);
            }
            error = null; return true;
        }
        catch (Exception e) { error = e.Message; return false; }
    }
    public void ValidateBalance(int index)
    {
        var d = GetDifficulty(index);
        if (!configured || d == null || !d.configured) throw new InvalidOperationException("층/난이도 밸런스 미설정");
        foreach (float n in new[] { hpMultiplier, attackMultiplier, d.hpMultiplier, d.attackMultiplier })
            if (float.IsNaN(n) || float.IsInfinity(n) || n < 0) throw new InvalidOperationException("배율은 0 이상 유한 숫자여야 합니다.");
        if (d.adjustments == null || d.adjustments.Length != 5) throw new InvalidOperationException("5종 추가 보정 데이터가 필요합니다.");
    }
    public void ValidateMap(int index, MonsterMapProfile map)
    {
        ValidateBalance(index);
        if (!map || !map.map || !map.configured || map.entries == null) throw new InvalidOperationException("배치 미설정");
        var ids = new System.Collections.Generic.HashSet<string>();
        foreach (var e in map.entries)
        {
            if (e == null || string.IsNullOrEmpty(e.id) || !ids.Add(e.id) || !e.template || !MonsterProfileRuntime.ValidPose(e))
                throw new InvalidOperationException("배치 ID/프리팹/좌표가 올바르지 않습니다.");
            if (e.reservedHpAdjustment != 0 || e.reservedAttackAdjustment != 0) throw new InvalidOperationException("개체별 보정은 아직 지원하지 않습니다.");
            Calculate(index, e.template, out _, out _);
        }
    }
    public void Calculate(int index, EnemyHealth template, out int hp, out int attack)
    {
        ValidateBalance(index);
        var d = GetDifficulty(index);
        int kind = (int)MonsterProfileRuntime.Kind(template);
        var a = d.adjustments[kind];
        hp = Final(template.MaxHealth, hpMultiplier, d.hpMultiplier, a.hp, 1);
        attack = Final(MonsterProfileRuntime.Attack(template), attackMultiplier, d.attackMultiplier, a.attack, 0);
    }
    public static int Final(int basic, float floor, float difficulty, int addition, int minimum)
    {
        double value = (double)basic * floor * difficulty + addition;
        // Product -> species addition -> one conversion. Exact .5 goes to the larger integer.
        double rounded = Math.Floor(value + .5d);
        if (double.IsNaN(value) || double.IsInfinity(value) || rounded < minimum || rounded > int.MaxValue)
            throw new InvalidOperationException($"최종 수치는 {minimum}~{int.MaxValue} 정수 범위여야 합니다.");
        return (int)rounded;
    }
}
