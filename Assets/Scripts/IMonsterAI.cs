// Minimal lifecycle shared by normal monsters; health and rewards stay in EnemyHealth.
public interface IMonsterAI
{
    void SetTarget(PlayerHealth target);
    void CancelAttacks();
}
