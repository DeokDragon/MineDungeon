using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("체력")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private float invincibleDuration = 0.6f;

    [Header("부활 테스트 설정")]
    [SerializeField] private float reviveDelay = 1f;
    [SerializeField, Range(0.01f, 1f)]
    private float reviveHealthRatio = 1f;
    [SerializeField] private float reviveInvincibleDuration = 2f;
    [SerializeField] private Transform respawnPoint;

    private PlayerMovement movement;
    private PlayerAttack attack;
    private Rigidbody2D rb;
    private SpriteRenderer bodyRenderer;

    private Color originalColor;
    private float invincibleUntil;
    private bool movementWasEnabled;
    private bool attackWasEnabled;

    public bool IsDead { get; private set; }
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        IsDead = false;

        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        rb = GetComponent<Rigidbody2D>();
        bodyRenderer = GetComponent<SpriteRenderer>();

        if (bodyRenderer != null)
            originalColor = bodyRenderer.color;
    }

    private void Update()
    {
        if (bodyRenderer == null)
            return;

        if (IsDead)
        {
            bodyRenderer.color = Color.gray;
            return;
        }

        // 무적 시간 동안 몸체를 반투명하게 깜빡임
        Color color = originalColor;

        if (Time.time < invincibleUntil)
        {
            color.a *= Mathf.PingPong(Time.time * 10f, 1f)
                < 0.5f ? 0.35f : 1f;
        }

        bodyRenderer.color = color;
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
            HandleDefeat();
    }

    private void HandleDefeat()
    {
        IsDead = true;

        movementWasEnabled = movement != null && movement.enabled;
        attackWasEnabled = attack != null && attack.enabled;

        if (movement != null)
            movement.enabled = false;

        if (attack != null)
            attack.enabled = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        DungeonRunManager run = DungeonRunManager.Instance;

        if (run == null)
        {
            Debug.LogError(
                "씬에 DungeonRunManager가 없습니다.",
                this
            );
            return;
        }

        if (run.TryUseRevive())
        {
            StartCoroutine(ReviveRoutine());
        }
        else
        {
            run.FailRun();
        }
    }

    private IEnumerator ReviveRoutine()
    {
        Debug.Log("부활 대기 중", this);

        yield return new WaitForSeconds(reviveDelay);
        if (respawnPoint != null)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.position = (Vector2)respawnPoint.position;
            }
            else
            {
                transform.position = respawnPoint.position;
            }
        }
        else
        {
            Debug.LogError("Player Health에 Respawn Point를 연결해주세요.", this);
        }

        currentHealth = Mathf.Clamp(
            Mathf.CeilToInt(maxHealth * reviveHealthRatio),
            1,
            maxHealth
        );

        invincibleUntil = Time.time + reviveInvincibleDuration;
        IsDead = false;

        if (movement != null)
            movement.enabled = movementWasEnabled;

        if (attack != null)
            attack.enabled = attackWasEnabled;

        Debug.Log(
            $"부활 완료 / 체력 {currentHealth} / " +
            $"남은 부활 {DungeonRunManager.Instance.RemainingRevives}회",
            this
        );
    }
    public void GrantInvincibility(float duration)
    {
        if (IsDead)
            return;

        invincibleUntil = Mathf.Max(
            invincibleUntil,
            Time.time + duration
        );
    }
    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        Debug.Log(
            $"물약 회복: {currentHealth - previousHealth} / " +
            $"현재 체력 {currentHealth}",
            this
        );
    }
}