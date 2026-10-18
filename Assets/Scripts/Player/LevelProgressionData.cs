using UnityEngine;

[CreateAssetMenu(menuName = "Mine Dungeon/Level Progression")]
public sealed class LevelProgressionData : ScriptableObject
{
    [Tooltip("임시 테스트 상한. 필요 경험치 표와 함께 확장하세요.")]
    [Min(1)] public int testMaximumLevel = 5;
    public int[] experienceToNextLevel = { 100, 150, 200, 250 };

    // A missing or non-positive entry ends the usable table, never a free level.
    public int MaximumLevel
    {
        get
        {
            int available = experienceToNextLevel == null ? 0 : experienceToNextLevel.Length;
            int count = Mathf.Min(Mathf.Max(1, testMaximumLevel) - 1, available);
            int valid = 0;
            while (valid < count && experienceToNextLevel[valid] > 0) valid++;
            return valid + 1;
        }
    }
    public int RequiredExperience(int level) => level >= 1 && level < MaximumLevel
        ? experienceToNextLevel[level - 1] : 0;
}
