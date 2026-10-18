using UnityEngine;

[DisallowMultipleComponent, RequireComponent(typeof(EnemyHealth))]
public sealed class EnemyReward : MonoBehaviour
{
    [SerializeField] private MonsterRewardData reward;
    private bool granted;
    public MonsterRewardData Reward => reward;
    internal void GrantConfirmedDeath(EnemyHealth source)
    {
        if(granted || source==null || source.gameObject!=gameObject || !source.IsDead)return;
        granted=true; // Set before any reward/UI callback can re-enter.
        var run=DungeonRunManager.Instance;
        if(run!=null)run.GetComponent<RewardInventorySession>()?.GrantKillReward(reward);
    }
}
