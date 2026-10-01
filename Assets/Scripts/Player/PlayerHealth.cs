using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private float invincibleDuration = 0.6f;

    private float invincibleUntil;

    public bool IsDead { get; private set; }

    private void Awake()
    {
        currentHealth = maxHealth;
        IsDead = false;
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || damage <= 0 || Time.time < invincibleUntil)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        invincibleUntil = Time.time + invincibleDuration;

        Debug.Log(
            $"플레이어: {damage} 피해 / 남은 체력 {currentHealth}",
            this
        );

        if (currentHealth == 0)
            Die();
    }

    private void Die()
    {
        IsDead = true;

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.enabled = false;

        PlayerAttack attack = GetComponent<PlayerAttack>();
        if (attack != null)
            attack.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Debug.Log("플레이어 전투 불능", this);
    }
}