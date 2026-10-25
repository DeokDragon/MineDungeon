using UnityEngine;

// Presentation only. Boss AI, JumpVisual and StoneHitFeedback retain combat/transform ownership.
[DisallowMultipleComponent]
[RequireComponent(typeof(StoneGolemBossAI), typeof(Rigidbody2D))]
public sealed class StoneGolemBossSpriteAnimation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Sprite[] phase1Idle, phase1Move, phase2Idle, phase2Move;
    [SerializeField, Min(.01f)] private float idleFramesPerSecond = 6f;
    [SerializeField, Min(.01f)] private float moveFramesPerSecond = 8f;
    [SerializeField] private float[] moveFrameWeights;
    [SerializeField, Min(.01f)] private float referenceMoveSpeed = 1.4f;
    [SerializeField, Min(0f)] private float movementThreshold = .02f;

    private StoneGolemBossAI ai;
    private EnemyHealth health;
    private Rigidbody2D rb;
    private Vector2 previousPosition;
    private float actualSpeed;
    private double frame;
    private int phase;
    private bool moving, combat;

    private void Awake()
    {
        ai = GetComponent<StoneGolemBossAI>();
        health = GetComponent<EnemyHealth>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        previousPosition = rb.position;
        actualSpeed = 0;
        frame = 0;
        phase = ai.Phase;
        moving = combat = false;
        Show(phase == 2 ? phase2Idle : phase1Idle, 0);
    }

    private void Start() { previousPosition = rb.position; }

    private void FixedUpdate()
    {
        // Completed physics movement, matching the existing monster playback approach.
        actualSpeed = Vector2.Distance(rb.position, previousPosition) / Time.fixedDeltaTime;
        previousPosition = rb.position;
    }

    private void LateUpdate()
    {
        if (!body || health.IsDead || Time.timeScale == 0f) return;
        bool nextCombat = ai.IsPresenting || ai.ActivePattern != StoneGolemBossAI.Pattern.None;
        bool nextMove = ai.isActiveAndEnabled && !nextCombat &&
            ai.State == StoneGolemBossAI.BossState.Chase && actualSpeed > movementThreshold;
        if (phase != ai.Phase || nextMove != moving || nextCombat != combat) frame = 0;
        phase = ai.Phase;
        moving = nextMove;
        combat = nextCombat;
        var frames = phase == 2 ? (moving ? phase2Move : phase2Idle)
            : (moving ? phase1Move : phase1Idle);
        if (!combat && ai.isActiveAndEnabled)
        {
            float fps = moving ? moveFramesPerSecond * actualSpeed / referenceMoveSpeed : idleFramesPerSecond;
            if (frames != null && frames.Length > 0) frame = (frame + Time.deltaTime * fps) % frames.Length;
        }
        // Keep the existing flipX and use a base pose throughout attacks/presentations.
        Show(frames, combat ? 0 : moving ? MoveIndex(frames) : (int)frame);
    }

    private int MoveIndex(Sprite[] frames)
    {
        if (frames == null || moveFrameWeights == null || moveFrameWeights.Length != frames.Length)
            return (int)frame;
        float total = 0;
        foreach (float weight in moveFrameWeights) total += Mathf.Max(0, weight);
        if (total <= 0) return (int)frame;
        float elapsed = (float)(frame / frames.Length) * total, end = 0;
        for (int i = 0; i < frames.Length; i++)
        {
            end += Mathf.Max(0, moveFrameWeights[i]);
            if (elapsed < end) return i;
        }
        return frames.Length - 1;
    }

    private void Show(Sprite[] frames, int index)
    {
        if (body && frames != null && frames.Length > 0) body.sprite = frames[index];
    }
}
