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
        if (isDead || damage <= 0) return false;
        currentHealth = Mathf.Max(0, currentHealth - damage);
        HealthChanged?.Invoke(this);
        if (currentHealth > 0)
        {
            if (feedback != null) feedback.Hit(contact, direction);
            return true;
        }
        isDead = true;
        // Snapshot before AI OnDisable restores the jumping pivot.
        if (feedback != null) feedback.Break(direction);
        var boss = GetComponent<StoneGolemBossAI>();
        if (boss != null) boss.enabled = false;
        var monster = GetComponent<StoneMonsterAI>();
        if (monster != null) { monster.StopAllCoroutines(); monster.enabled = false; }
        foreach (var collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
        Died?.Invoke(this);
        Destroy(gameObject);
        return true;
    }
}
