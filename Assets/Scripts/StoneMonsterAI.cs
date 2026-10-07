using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class StoneMonsterAI : MonoBehaviour
{
    private enum State { Chase, Warning, Jump, Recovery }

    [Header("연결")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform body;
    [SerializeField] private Transform attackWarning;
    [SerializeField] private LayerMask wallLayer;

    [Header("이동")]
    [SerializeField] private float moveSpeed = 1.8f;
    [SerializeField] private float stopDistance = 1.3f;

    [Header("공격")]
    [SerializeField] private float warningDuration = 0.7f;
    [SerializeField, Min(0.01f)] private float jumpDuration = 0.4f;
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float attackRadius = 0.8f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float recoveryDuration = 0.8f;
    [SerializeField] private float attackInterval = 2.5f;

    private Rigidbody2D rb;
    private Collider2D ownCollider;
    private Collider2D playerCollider;
    private PlayerHealth playerHealth;

    private State state;
    private float timer;
    private float nextAttackTime;
    private Vector2 landingPosition;
    private Vector2 jumpStartPosition;
    private Vector3 bodyStartPosition;
    private bool ignoringPlayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ownCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            playerCollider = player.GetComponent<Collider2D>();
        }

        if (body != null)
            bodyStartPosition = body.localPosition;

        if (attackWarning != null)
        {
            attackWarning.localScale = new Vector3(
                attackRadius * 2f, attackRadius * 2f, 1f
            );
            attackWarning.gameObject.SetActive(false);
        }

        state = State.Chase;
    }

    private void FixedUpdate()
    {
        if (player == null || body == null ||
            playerHealth == null || playerCollider == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (playerHealth.IsDead)
        {
            rb.linearVelocity = Vector2.zero;

            ResetVisuals();
            SetIgnorePlayer(false);

            state = State.Chase;
            timer = 0f;

            // 부활 직후 예고 없이 공격하지 않도록 대기
            nextAttackTime = Time.time + 1f;
            return;
        }

        switch (state)
        {
            case State.Chase:
                Chase();
                break;

            case State.Warning:
                rb.linearVelocity = Vector2.zero;
                timer += Time.fixedDeltaTime;

                if (timer >= warningDuration)
                {
                    state = State.Jump;
                    timer = 0f;
                    jumpStartPosition = rb.position;
                    SetIgnorePlayer(true);
                }
                break;

            case State.Jump:
                Jump();
                break;

            case State.Recovery:
                rb.linearVelocity = Vector2.zero;
                timer += Time.fixedDeltaTime;

                if (timer >= recoveryDuration)
                    state = State.Chase;
                break;
        }
    }

    private void LateUpdate()
    {
        // 親が動いても、予告位置は固定する
        if (attackWarning != null &&
            attackWarning.gameObject.activeSelf)
        {
            attackWarning.position = new Vector3(
                landingPosition.x, landingPosition.y, 0f
            );
        }
    }

    private void Chase()
    {
        Vector2 direction = (Vector2)player.position - rb.position;
        float distance = direction.magnitude;

        if (distance > stopDistance)
        {
            float speed = Mathf.Min(
                moveSpeed,
                (distance - stopDistance) / Time.fixedDeltaTime
            );

            rb.linearVelocity = direction.normalized * speed;
            return;
        }

        rb.linearVelocity = Vector2.zero;

        if (Time.time < nextAttackTime)
            return;

        // 벽으로 가려진 상대에게는 점프를 시작하지 않음
        if (Physics2D.Linecast(
            rb.position, player.position, wallLayer).collider != null)
        {
            return;
        }

        // 예고 시작 시 플레이어 위치를 저장
        landingPosition = player.position;
        nextAttackTime = Time.time + attackInterval;
        timer = 0f;
        state = State.Warning;

        if (attackWarning != null)
            attackWarning.gameObject.SetActive(true);
    }

    private void Jump()
    {
        // 마지막 이동의 물리 처리가 끝난 다음 착지 판정
        if (timer >= jumpDuration)
        {
            Land();
            return;
        }

        timer = Mathf.Min(
            timer + Time.fixedDeltaTime, jumpDuration
        );

        float progress = timer / jumpDuration;

        Vector2 nextPosition = Vector2.Lerp(
            jumpStartPosition, landingPosition, progress
        );

        rb.linearVelocity =
            (nextPosition - rb.position) / Time.fixedDeltaTime;

        // 몸체 이미지만 위로 올렸다가 내림
        float height = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
        body.localPosition =
            bodyStartPosition + Vector3.up * height;
    }

    private void Land()
    {
        rb.linearVelocity = Vector2.zero;
        ResetVisuals();

        Vector2 center = rb.position;
        Vector2 closestPoint = playerCollider.ClosestPoint(center);

        bool inRange =
            Vector2.Distance(center, closestPoint) <= attackRadius;

        bool blocked = Physics2D.Linecast(
            center, closestPoint, wallLayer
        ).collider != null;

        // 착지 한 번당 피해 판정 한 번
        if (inRange && !blocked)
            playerHealth.TakeDamage(attackDamage);

        SetIgnorePlayer(false);
        timer = 0f;
        state = State.Recovery;
    }

    private void SetIgnorePlayer(bool ignore)
    {
        if (ignoringPlayer == ignore)
            return;

        if (ownCollider != null && playerCollider != null)
            Physics2D.IgnoreCollision(
                ownCollider, playerCollider, ignore
            );

        ignoringPlayer = ignore;
    }

    private void ResetVisuals()
    {
        if (body != null)
            body.localPosition = bodyStartPosition;

        if (attackWarning != null)
            attackWarning.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        ResetVisuals();
        SetIgnorePlayer(false);
    }
    public void SetTarget(PlayerHealth target)
    {
        SetIgnorePlayer(false);
        playerHealth = target;
        player = target != null ? target.transform : null;
        playerCollider = target != null ? target.GetComponent<Collider2D>() : null;
    }

}