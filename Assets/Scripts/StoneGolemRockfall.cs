using UnityEngine;

// One telegraph / one impact. The boss supplies the shared pattern clock.
public class StoneGolemRockfall : MonoBehaviour
{
    [SerializeField] private SpriteRenderer warning;
    [SerializeField] private SpriteRenderer progress;
    [SerializeField] private SpriteRenderer stone;
    [SerializeField] private Material telegraphMaterial;
    private StoneGolemTelegraph telegraph;
    [SerializeField, Min(0f)] private float visualHeight = 3f;
    [SerializeField, Min(0.01f)] private float visualFallDuration = 0.3f;
    private PlayerHealth target;
    private LayerMask walls;
    private float radius, warningDuration, spawnTime;
    private int damage;
    private bool finished;
    public Vector2 Center { get; private set; }
    public float SpawnTime => spawnTime;

    public void Initialize(PlayerHealth player, LayerMask wallMask, Vector2 center,
        float hitRadius, float duration, float startedAt, int hitDamage)
    {
        target = player;
        walls = wallMask;
        Center = center;
        radius = hitRadius;
        warningDuration = duration;
        spawnTime = startedAt;
        damage = hitDamage;
        transform.position = center;
        transform.localScale = Vector3.one;
        warning.gameObject.SetActive(false);
        progress.gameObject.SetActive(false);
        telegraph = StoneGolemTelegraph.Create(transform, telegraphMaterial, Center,
            Vector2.right, radius, 360f, 0f, false, new Color(1f, .45f, .05f),
            warning.sortingLayerID, warning.sortingOrder);
        Size(stone, radius * 1.1f);
        UpdateVisuals(0f);
    }

    public bool Tick(float patternTime)
    {
        if (finished) return false;
        if (target == null || target.IsDead) { Finish(); return false; }
        float age = Mathf.Max(0f, patternTime - spawnTime);
        UpdateVisuals(age);
        if (age + 0.00001f < warningDuration) return true;
        telegraph.SetProgress(1f);
        bool hitPlayer = false;
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(Center, radius))
        {
            if (collider.GetComponentInParent<PlayerHealth>() != target) continue;
            if (Physics2D.Linecast(Center, collider.ClosestPoint(Center), walls).collider != null) continue;
            hitPlayer = true;
            break; // Multiple colliders still produce exactly one PlayerHealth request.
        }
        Finish();
        if (hitPlayer) target.TakeDamage(damage);
        return false;
    }

    private void UpdateVisuals(float age)
    {
        float fraction = Mathf.Clamp01(age / warningDuration);
        telegraph.SetProgress(fraction);
        float falling = Mathf.Clamp01((age - warningDuration + visualFallDuration) / visualFallDuration);
        stone.gameObject.SetActive(age >= warningDuration - visualFallDuration);
        stone.transform.localPosition = Vector3.up * (visualHeight * (1f - falling));
    }

    private static void Size(SpriteRenderer sprite, float diameter)
    {
        Vector3 size = sprite.sprite.bounds.size;
        sprite.transform.localScale = new Vector3(diameter / size.x, diameter / size.y, 1f);
    }

    private void Finish()
    {
        finished = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
