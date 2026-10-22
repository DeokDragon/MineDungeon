using System;
using UnityEngine;

// Lives on the existing persistent DungeonRunManager, not another singleton.
[DisallowMultipleComponent]
public sealed class PlayerProgression : MonoBehaviour
{
    [SerializeField] private LevelProgressionData configuration;
    [Header("개발용 지급 (Play Mode Context Menu)")]
    [SerializeField, Min(0)] private int debugExperience = 260;
    public int Level { get; private set; } = 1;
    public int Experience { get; private set; }
    public int UnspentStatPoints { get; private set; } = 3;
    public enum Stat { Health, Physical, Magic }
    public int HealthPoints { get; private set; }
    public int PhysicalPoints { get; private set; }
    public int MagicPoints { get; private set; }
    public int SpentStatPoints => HealthPoints + PhysicalPoints + MagicPoints;
    public int MaximumHealthBonus => HealthPoints * 3;
    public float PhysicalAttackBonus => PhysicalPoints * .4f;
    public float MagicAttackBonus => MagicPoints * .4f;
    public bool HasHealth20Passive => HealthPoints >= 20;
    public bool HasPhysical20Passive => PhysicalPoints >= 20;
    public bool HasMagic20Passive => MagicPoints >= 20;
    public bool HasHealth50Passive => HealthPoints >= 50;
    public bool HasPhysical50Passive => PhysicalPoints >= 50;
    public bool HasMagic50Passive => MagicPoints >= 50;
    public int MaximumLevel => configuration != null ? configuration.MaximumLevel : 1;
    public bool IsMaximumLevel => Level >= MaximumLevel;
    public int RequiredExperience => configuration != null ? configuration.RequiredExperience(Level) : 0;
    public int RequiredAt(int level) => configuration != null ? configuration.RequiredExperience(level) : 0;
    public float Progress => IsMaximumLevel ? 1f : RequiredExperience > 0
        ? Mathf.Clamp01((float)Experience / RequiredExperience) : 0f;
    public event Action Changed;
    public event Action<int, int, int> LeveledUp;

    internal void RestoreGrowth(int level, int experience, int points, int healthPoints = 0, int physicalPoints = 0, int magicPoints = 0)
    {
        if (level < 1 || level > MaximumLevel || experience < 0 || points < 0 ||
            (level == MaximumLevel ? experience != 0 : experience >= RequiredAt(level)) ||
            !ValidPointTotal(level, points, healthPoints, physicalPoints, magicPoints))
            throw new ArgumentException("Invalid growth state; refusing to alter saved progression.");
        Level = level;
        Experience = experience;
        UnspentStatPoints = points;
        HealthPoints = healthPoints; PhysicalPoints = physicalPoints; MagicPoints = magicPoints;
        Changed?.Invoke(); // Restore values, never grant rewards or fire level-up bonuses.
    }

    internal static bool ValidPointTotal(int level, int unspent, int health, int physical, int magic) =>
        level >= 1 && level <= 50 && unspent >= 0 && health >= 0 && health <= 100 &&
        physical >= 0 && physical <= 100 && magic >= 0 && magic <= 100 &&
        (long)unspent + health + physical + magic == level * 3L;

    public int PointsIn(Stat stat) => stat == Stat.Health ? HealthPoints : stat == Stat.Physical ? PhysicalPoints : stat == Stat.Magic ? MagicPoints : -1;

    // Pure preview for future UI. The committing API separately enforces runtime permissions.
    public int InvestablePoints(Stat stat, int multiplier)
    {
        int current = PointsIn(stat);
        if (current < 0 || (multiplier != 1 && multiplier != 5 && multiplier != 10)) return 0;
        return Math.Min(multiplier, Math.Min(UnspentStatPoints, 100 - current));
    }

    public bool CanInvest
    {
        get
        {
            var run = DungeonRunManager.Instance;
            if (!Application.isPlaying || !isActiveAndEnabled || run == null || run.gameObject != gameObject ||
                run.IsRunEnded || run.IsRestarting || run.IsTracking ||
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/Main.unity") return false;
#if UNITY_EDITOR
            if (run.EditorSelectedMapTest) return false;
#endif
            var rooms = FindFirstObjectByType<DungeonRoomManager>();
            var health = FindFirstObjectByType<PlayerHealth>();
            var save = GetComponent<CharacterSaveService>();
            return rooms != null && rooms.isActiveAndEnabled && rooms.IsPreparing && !rooms.IsTransitioning &&
                !rooms.IsCompleted && rooms.CurrentRoomNumber == 0 && health != null && health.isActiveAndEnabled &&
                !health.IsDead && health.gameObject.scene == rooms.gameObject.scene &&
                rooms.gameObject.scene == UnityEngine.SceneManagement.SceneManager.GetActiveScene() &&
                save != null && save.CanCommitInvestment;
        }
    }

    public bool TryInvest(Stat stat, int multiplier, out int invested, out string error)
    {
        invested = 0; error = null;
        int amount = InvestablePoints(stat, multiplier);
        if (amount <= 0) { error = "투자 요청이나 사용 가능한 포인트를 확인해 주세요."; return false; }
        if (!CanInvest) { error = "메인 씬의 정상 준비 상태에서만 투자할 수 있습니다."; return false; }
        if (!GetComponent<CharacterSaveService>().TryCommitInvestment(this, stat, amount, out error)) return false;
        invested = amount;
        return true;
    }

    // Called only after the candidate snapshot has been durably committed; emits no level-up reward.
    internal void ApplySavedInvestment(int unspent, int health, int physical, int magic)
    {
        UnspentStatPoints = unspent; HealthPoints = health; PhysicalPoints = physical; MagicPoints = magic;
        Changed?.Invoke();
    }

    public bool GrantExperience(int amount)
    {
        if (amount < 0 || configuration == null) return false;
        if (amount == 0 || IsMaximumLevel) return true;
        int before = Level;
        long remaining = (long)Experience + amount;
        while (Level < MaximumLevel)
        {
            int cost = configuration.RequiredExperience(Level);
            if (cost <= 0 || remaining < cost) break;
            remaining -= cost;
            Level++;
        }
        Experience = IsMaximumLevel ? 0 : (int)Math.Min(int.MaxValue, remaining);
        int gained = Level - before;
        int awarded = (int)Math.Min((long)gained * 3, (long)int.MaxValue - UnspentStatPoints);
        UnspentStatPoints += awarded;
        Changed?.Invoke();
        if (gained > 0) LeveledUp?.Invoke(before, Level, awarded);
        return true;
    }

#if UNITY_EDITOR
    [ContextMenu("개발용/설정한 경험치 지급 (Play Mode)")]
    private void DebugGrantExperience()
    {
        if (!Application.isPlaying) return;
        // Select the surviving session even when invoked on a duplicate during reload.
        var run = DungeonRunManager.Instance;
        if (run != null) run.GetComponent<PlayerProgression>()?.GrantExperience(debugExperience);
    }
#endif
}
