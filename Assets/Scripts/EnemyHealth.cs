using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private int currentHealth;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float flashTimer;
    private bool isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void Update()
    {
        if (flashTimer <= 0f)
            return;

        flashTimer -= Time.deltaTime;

        if (flashTimer <= 0f && spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        Debug.Log(
            $"{name}: {damage} 피해 / 남은 체력 {currentHealth}",
            this
        );

        if (currentHealth == 0)
        {
            isDead = true;
            Destroy(gameObject);
            return;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            flashTimer = 0.12f;
        }
    }
}