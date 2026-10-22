using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(EnemyHealth))]
public sealed class EnemyReward : MonoBehaviour
{
    [SerializeField] private MonsterRewardData reward;
    private bool granted;
    private int? experienceOverride;
    public MonsterRewardData Reward => reward;
    public int EffectiveExperience => experienceOverride ?? (reward ? Mathf.Max(0, reward.experience) : 0);
    public void ConfigureSpawnExperience(int value)
    {
        if (!gameObject.scene.IsValid() || granted || GetComponent<EnemyHealth>().IsDead || value < 0)
            throw new System.InvalidOperationException("생성된 살아 있는 몬스터에 0 이상 EXP만 적용할 수 있습니다.");
        experienceOverride = value;
    }
    internal void GrantConfirmedDeath(EnemyHealth source)
    {
        if(granted || source==null || source.gameObject!=gameObject || !source.IsDead)return;
        granted=true; // Set before any reward/UI callback can re-enter.
        var run=DungeonRunManager.Instance;
        if(run!=null)run.GetComponent<RewardInventorySession>()?.GrantKillReward(reward, experienceOverride);
    }
}
