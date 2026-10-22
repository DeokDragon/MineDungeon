using UnityEngine;

// Sprite-only locomotion. NormalMonsterAI owns attacks; StoneHitFeedback owns color/transforms.
[DisallowMultipleComponent]
[RequireComponent(typeof(CrystalMonsterAI), typeof(Rigidbody2D))]
public sealed class CrystalMonsterSpriteAnimation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Sprite[] idleFrames, moveFrames;
    [SerializeField, Min(.01f)] private float idleFramesPerSecond = 6f, moveFramesPerSecond = 10f;
    [SerializeField, Min(.01f), Tooltip("이 실제 이동 속도에서 Move 기본 FPS로 재생합니다.")]
    private float referenceMoveSpeed = 2.2f;
    [SerializeField, Min(0f)] private float movementThreshold = .02f;

    private CrystalMonsterAI ai;
    private Rigidbody2D rb;
    private EnemyHealth health;
    private Vector2 previousPosition;
    private float actualSpeed;
    private double frame;
    private bool moving, attacking;

    private void Awake()
    {
        ai = GetComponent<CrystalMonsterAI>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        previousPosition = rb.position;
        actualSpeed = 0;
        frame = 0;
        moving = attacking = false;
        if (body && idleFrames != null && idleFrames.Length > 0) body.sprite = idleFrames[0];
    }

    private void Start() { previousPosition = rb.position; }

    private void FixedUpdate()
    {
        actualSpeed = Vector2.Distance(rb.position, previousPosition) / Time.fixedDeltaTime;
        previousPosition = rb.position;
    }

    private void LateUpdate()
    {
        if (!body || health.IsDead || Time.timeScale == 0f) return;
        bool nextAttack = ai.IsAttacking;
        bool nextMove = ai.isActiveAndEnabled && !nextAttack && actualSpeed > movementThreshold;
        if (nextAttack && !attacking) Face(ai.VisualAttackDirection.x);
        else if (nextMove && ai.Target) Face(ai.Target.transform.position.x - rb.position.x);
        if (nextMove != moving || nextAttack != attacking) frame = 0;
        moving = nextMove;
        attacking = nextAttack;
        var frames = moving ? moveFrames : idleFrames;
        if (frames == null || frames.Length == 0) return;
        if (!attacking && ai.isActiveAndEnabled)
        {
            float fps = moving ? moveFramesPerSecond * actualSpeed / Mathf.Max(.01f, referenceMoveSpeed)
                : idleFramesPerSecond;
            frame = (frame + Time.deltaTime * fps) % frames.Length;
        }
        body.sprite = frames[attacking ? 0 : (int)frame];
    }

    private void Face(float horizontal)
    {
        if (Mathf.Abs(horizontal) > .02f) body.flipX = horizontal < 0;
    }
}
