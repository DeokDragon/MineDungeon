using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private int currentHealth;

    [SerializeField] private SpriteRenderer spriteRenderer;
    private StoneHitFeedback feedback;
    private bool isDead;

    public bool IsDead => isDead;
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public event System.Action<EnemyHealth> HealthChanged;
    public event System.Action<EnemyHealth> Died;
    public event System.Action<EnemyHealth> Damaged;
    public void ConfigureSpawnHealth(int value)
    {
        if (!gameObject.scene.IsValid() || isDead || value < 1) throw new System.InvalidOperationException("생성 시 유효한 HP만 적용할 수 있습니다.");
        maxHealth = currentHealth = value;
        HealthChanged?.Invoke(this);
    }

    private void Awake()
    {
        currentHealth = maxHealth;

        feedback = GetComponent<StoneHitFeedback>();
    }

    public void TakeDamage(int damage)
    {
        TryTakeDamage(damage, spriteRenderer != null ? (Vector2)spriteRenderer.bounds.center : (Vector2)transform.position, Vector2.right);
    }

    public bool TryTakeDamage(int damage, Vector2 contact, Vector2 direction)
    {
        if ((TryGetComponent<StoneGolemBossAI>(out var presentingBoss) && presentingBoss.IsPresenting) || isDead || damage <= 0 || (DungeonRunManager.Instance!=null && DungeonRunManager.Instance.IsRunEnded)) return false;
        DungeonRunManager.Instance?.RecordDamageDealt(Mathf.Min(currentHealth,damage));
        currentHealth = Mathf.Max(0, currentHealth - damage);
        // Lock lethal hits before callbacks; rewards precede room/result listeners.
        if (currentHealth == 0)
        {
            isDead = true;
            DungeonRunManager.Instance?.RecordKill();
            GetComponent<EnemyReward>()?.GrantConfirmedDeath(this);
        }
        HealthChanged?.Invoke(this);
        if (currentHealth > 0)
        {
            // All current EnemyHealth damage callers are player attacks/skills. Notify only
            // after damage was accepted; lethal targets cannot resume any behavior.
            Damaged?.Invoke(this);
            if (feedback != null) feedback.Hit(contact, direction);
            return true;
        }
        // Snapshot before AI OnDisable restores the jumping pivot.
        if (feedback != null) feedback.Break(direction);
        var boss = GetComponent<StoneGolemBossAI>();
        if (boss != null) boss.enabled = false;
        foreach (MonoBehaviour component in GetComponents<MonoBehaviour>())
            if (component is IMonsterAI monster) { monster.CancelAttacks(); component.enabled = false; }
        foreach (var collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
        Died?.Invoke(this);
        Destroy(gameObject);
        return true;
    }
}
