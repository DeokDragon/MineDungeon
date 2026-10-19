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
    public int UnspentStatPoints { get; private set; }
    public int MaximumLevel => configuration != null ? configuration.MaximumLevel : 1;
    public bool IsMaximumLevel => Level >= MaximumLevel;
    public int RequiredExperience => configuration != null ? configuration.RequiredExperience(Level) : 0;
    public int RequiredAt(int level) => configuration != null ? configuration.RequiredExperience(level) : 0;
    public float Progress => IsMaximumLevel ? 1f : RequiredExperience > 0
        ? Mathf.Clamp01((float)Experience / RequiredExperience) : 0f;
    public event Action Changed;
    public event Action<int, int, int> LeveledUp;

    internal void RestoreGrowth(int level, int experience, int points)
    {
        Level = Mathf.Clamp(level, 1, MaximumLevel);
        Experience = IsMaximumLevel ? 0 : Mathf.Clamp(experience, 0, Mathf.Max(0, RequiredExperience - 1));
        UnspentStatPoints = Math.Max(0, points);
        Changed?.Invoke(); // Restore values, never grant rewards or fire level-up bonuses.
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
        int awarded = (int)Math.Min(gained, (long)int.MaxValue - UnspentStatPoints);
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
