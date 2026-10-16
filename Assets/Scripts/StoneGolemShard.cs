using UnityEngine;

// Ticked exclusively by its owning boss, so cancellation and pause are atomic.
[RequireComponent(typeof(SpriteRenderer))]
public class StoneGolemShard : MonoBehaviour
{
    private PlayerHealth target;
    private LayerMask walls;
    private Vector2 direction;
    private float speed, radius, remainingDistance;
    private int damage;
    private bool finished;

    public Vector2 Direction => direction;
    public float RemainingDistance => remainingDistance;

    public void Initialize(PlayerHealth player, LayerMask wallMask, Vector2 origin,
        Vector2 heading, float velocity, float range, float diameter, int hitDamage)
    {
        target = player;
        walls = wallMask;
        direction = heading.normalized;
        speed = velocity;
        radius = diameter * 0.5f;
        remainingDistance = range;
        damage = hitDamage;
        transform.position = origin;
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        Vector3 size = sprite.sprite.bounds.size;
        transform.localScale = new Vector3(diameter / size.x, diameter / size.y, 1f);
    }

    public bool Tick(float deltaTime)
    {
        if (finished) return false;
        if (target == null || target.IsDead) { Finish(); return false; }
        Vector2 origin = transform.position;
        float step = Mathf.Min(speed * deltaTime, remainingDistance);
        float wallDistance = float.PositiveInfinity;
        float playerDistance = float.PositiveInfinity;
        // Explicit overlaps also cover a player moving into a stationary shard.
        foreach (Collider2D collider in Physics2D.OverlapCircleAll(origin, radius))
        {
            if ((walls.value & (1 << collider.gameObject.layer)) != 0) wallDistance = 0f;
            else if (collider.GetComponentInParent<PlayerHealth>() == target) playerDistance = 0f;
        }
        foreach (RaycastHit2D hit in Physics2D.CircleCastAll(origin, radius, direction, step))
        {
            if ((walls.value & (1 << hit.collider.gameObject.layer)) != 0)
                wallDistance = Mathf.Min(wallDistance, hit.distance);
            else if (hit.collider.GetComponentInParent<PlayerHealth>() == target)
                playerDistance = Mathf.Min(playerDistance, hit.distance);
        }
        float contact = Mathf.Min(wallDistance, playerDistance);
        if (contact <= step)
        {
            transform.position = origin + direction * contact;
            Finish(); // Mark finished before TakeDamage can invoke the owner's cancellation.
            if (playerDistance < wallDistance) target.TakeDamage(damage);
            return false;
        }
        transform.position = origin + direction * step;
        remainingDistance -= step;
        if (remainingDistance <= 0.0001f) { Finish(); return false; }
        return true;
    }

    private void Finish()
    {
        finished = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
