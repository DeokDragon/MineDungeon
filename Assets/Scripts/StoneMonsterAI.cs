using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class StoneMonsterAI : MonoBehaviour
{
    [Header("추적 대상")]
    [SerializeField] private Transform player;

    [Header("이동 설정")]
    [SerializeField, Min(0f)] private float moveSpeed = 1.8f;
    [SerializeField, Min(0f)] private float stopDistance = 1.3f;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 direction =
            (Vector2)player.position - rb.position;

        float distance = direction.magnitude;

        // 가까워지면 멈춤
        if (distance <= stopDistance)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // 정지 거리를 지나치지 않도록 이번 이동 속도를 제한
        float speed = Mathf.Min(
            moveSpeed,
            (distance - stopDistance) / Time.fixedDeltaTime
        );

        rb.linearVelocity = direction.normalized * speed;
    }

    private void OnDisable()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }
}