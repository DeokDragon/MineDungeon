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
        public bool experienceConfigured;
        public int[] experience = new int[5];
        public int bossExperience;
        // Optional floor/difficulty-only playtest values. No changes to shared boss prefabs.
        public bool bossBalanceConfigured;
        public int bossHealth;
        // Leap, smash, charge, fan, rockfall, extra shockwave, enhanced smash/fan/rockfall.
        public int[] bossDamage = Array.Empty<int>();
        public MonsterMapProfile Find(DungeonRoom map) => maps?.SingleOrDefault(p => p && p.map == map);
    }
    public Difficulty[] difficulties = { new Difficulty(), new Difficulty(), new Difficulty(), new Difficulty() };
    public Difficulty GetDifficulty(int index) => difficulties != null && index >= 0 && index < difficulties.Length ? difficulties[index] : null;
    public static void ValidateExperienceValues(Difficulty d)
    {
        if (d == null || d.experience == null || d.experience.Length != 5 || d.experience.Any(n => n < 0) || d.bossExperience < 0)
            throw new InvalidOperationException("5종 및 보스 EXP는 0~int.MaxValue 정수여야 합니다.");
    }
    public void ValidateExperience(int index)
    {
        var d = GetDifficulty(index);
        if (d == null || !d.experienceConfigured) throw new InvalidOperationException("층·난이도 EXP 미설정");
        ValidateExperienceValues(d);
    }
    public long ExpectedExperience(int index, MonsterMapProfile map)
    {
        ValidateExperience(index); ValidateMap(index, map);
        long sum = 0;
        foreach (var entry in map.entries) sum = checked(sum + GetDifficulty(index).experience[(int)MonsterProfileRuntime.Kind(entry.template)]);
        return sum;
    }
    public void ApplyExperience(int index, DungeonRoom room, bool boss)
    {
        ValidateExperience(index);
        foreach (var hp in room.GetComponentsInChildren<EnemyHealth>())
        {
            var reward = hp.GetComponent<EnemyReward>();
            if (!reward || !reward.Reward) throw new InvalidOperationException("몬스터 보상 참조 누락: " + hp.name);
            if (boss && !hp.GetComponent<StoneGolemBossAI>()) throw new InvalidOperationException("보스 EXP 대상이 돌골렘이 아닙니다.");
            reward.ConfigureSpawnExperience(boss ? GetDifficulty(index).bossExperience : GetDifficulty(index).experience[(int)MonsterProfileRuntime.Kind(hp)]);
        }
    }
    public void ApplyBossBalance(int index, DungeonRoom room)
    {
        var d = GetDifficulty(index);
        if (d == null || !d.bossBalanceConfigured) return;
        if (d.bossHealth < 1 || d.bossDamage == null || d.bossDamage.Length != 9 || d.bossDamage.Any(n => n < 1))
            throw new InvalidOperationException("Invalid boss playtest HP/damage configuration");
        var bosses = room.GetComponentsInChildren<StoneGolemBossAI>();
        if (bosses.Length != 1) throw new InvalidOperationException("Boss playtest requires exactly one golem");
        bosses[0].GetComponent<EnemyHealth>().ConfigureSpawnHealth(d.bossHealth);
        bosses[0].ConfigureSpawnDamage(d.bossDamage);
    }
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
        if (d.bossBalanceConfigured && (d.bossHealth < 1 || d.bossDamage == null || d.bossDamage.Length != 9 || d.bossDamage.Any(n => n < 1)))
            throw new InvalidOperationException("Invalid boss playtest HP/damage configuration");
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
