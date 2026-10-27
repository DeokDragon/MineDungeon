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

    [Header("P1 contact walk comparison - trial, not approved")]
    [SerializeField] private bool usePhase1ContactWalkTrial;
    [SerializeField] private Sprite[] phase1ContactWalkTrial;
    [SerializeField, Min(.01f)] private float phase1TrialCycle = .40f;
    private bool trial;

    [Header("P1 basic attacks - presentation only")]
    [SerializeField] private Sprite[] leapSlam, groundSmash, rockCharge, stoneFan, rockfall;
    private bool showingAttack;

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
        if (!body) return;
        if (health.IsDead || !ai.isActiveAndEnabled)
        {
            ClearAttackPose();
            return;
        }
        if (Time.timeScale == 0f) return;
        if (ShowAttack()) { showingAttack = true; return; }
        if (showingAttack) { showingAttack = false; frame = 0; }
        bool nextCombat = ai.IsPresenting || ai.ActivePattern != StoneGolemBossAI.Pattern.None;
        bool nextMove = ai.isActiveAndEnabled && !nextCombat &&
            ai.State == StoneGolemBossAI.BossState.Chase && actualSpeed > movementThreshold;
        bool nextTrial = ai.Phase == 1 && usePhase1ContactWalkTrial &&
            phase1ContactWalkTrial != null && phase1ContactWalkTrial.Length > 0;
        if (phase != ai.Phase || nextMove != moving || nextCombat != combat || trial != nextTrial) frame = 0;
        trial = nextTrial;
        phase = ai.Phase;
        moving = nextMove;
        combat = nextCombat;
        var frames = phase == 2 ? (moving ? phase2Move : phase2Idle)
            : (moving ? (trial ? phase1ContactWalkTrial : phase1Move) : phase1Idle);
        if (!combat && ai.isActiveAndEnabled)
        {
            float moveFps = trial ? phase1ContactWalkTrial.Length / phase1TrialCycle : moveFramesPerSecond;
            float fps = moving ? moveFps * actualSpeed / referenceMoveSpeed : idleFramesPerSecond;
            if (frames != null && frames.Length > 0) frame = (frame + Time.deltaTime * fps) % frames.Length;
        }
        // Keep the existing flipX and use a base pose throughout attacks/presentations.
        Show(frames, combat ? 0 : moving ? MoveIndex(frames) : (int)frame);
    }

    private void OnDisable() { ClearAttackPose(); }

    private void ClearAttackPose()
    {
        if (!showingAttack) return;
        showingAttack = false;
        frame = 0;
        Show(ai != null && ai.Phase == 2 ? phase2Idle : phase1Idle, 0);
    }

    private bool ShowAttack()
    {
        // P2 (including its basic combos) retains its original base-pose presentation.
        if (ai.Phase != 1 || ai.IsPresenting) return false;
        Sprite[] frames;
        switch (ai.ActivePattern)
        {
            case StoneGolemBossAI.Pattern.LeapSlam: frames = leapSlam; break;
            case StoneGolemBossAI.Pattern.GroundSmash: frames = groundSmash; break;
            case StoneGolemBossAI.Pattern.RockCharge: frames = rockCharge; break;
            case StoneGolemBossAI.Pattern.StoneFan: frames = stoneFan; break;
            case StoneGolemBossAI.Pattern.Rockfall: frames = rockfall; break;
            default: return false;
        }
        if (frames == null || frames.Length == 0) return false;
        float t = ai.VisualStateElapsed;
        float duration = Mathf.Max(.00001f, ai.VisualStateDuration);
        float normalized = Mathf.Clamp01(t / duration);
        int index;
        if (ai.State == StoneGolemBossAI.BossState.Warning)
        {
            switch (ai.ActivePattern)
            {
                case StoneGolemBossAI.Pattern.GroundSmash: index = Timed(normalized, 0, SmashPrepare); break;
                case StoneGolemBossAI.Pattern.LeapSlam: index = Timed(normalized, 0, LeapPrepare); break;
                case StoneGolemBossAI.Pattern.RockCharge: index = Timed(normalized, 0, ChargePrepare); break;
                default: index = Timed(normalized, 0, FanPrepare); break;
            }
        }
        else if (ai.State == StoneGolemBossAI.BossState.Jump)
            index = Timed(normalized, 3, LeapAir); // Hold F6 until actual landing changes AI state.
        else if (ai.State == StoneGolemBossAI.BossState.Charge)
            index = 3 + Mathf.FloorToInt(t / .1f) % 4;
        else if (ai.State == StoneGolemBossAI.BossState.Rockfall)
            index = t < duration ? Timed(normalized, 0, FanPrepare)
                : Timed(Mathf.Clamp01((t - duration) / .55f), 3, RockfallHold);
        else if (ai.State == StoneGolemBossAI.BossState.Recovery)
        {
            switch (ai.ActivePattern)
            {
                case StoneGolemBossAI.Pattern.LeapSlam: index = Timed(normalized, 6, LeapRecover); break;
                case StoneGolemBossAI.Pattern.GroundSmash: index = Timed(normalized, 5, SmashRecover); break;
                case StoneGolemBossAI.Pattern.RockCharge: index = Timed(normalized, 7, ChargeRecover); break;
                case StoneGolemBossAI.Pattern.StoneFan: index = Timed(normalized, 3, FanRecover); break;
                default: index = Timed(normalized, 6, RockfallRecover); break;
            }
        }
        else return false;
        Show(frames, Mathf.Min(index, frames.Length - 1));
        return true;
    }

    // README timing weights, normalized within the AI-owned interval.
    private static readonly float[] LeapPrepare = {250, 300, 450}, LeapAir = {160, 220, 220},
        LeapRecover = {80, 120, 170, 160, 170}, SmashPrepare = {250, 300, 350, 200, 100},
        SmashRecover = {70, 130, 190, 210, 200}, ChargePrepare = {250, 350, 400},
        ChargeRecover = {90, 160, 200, 250}, FanPrepare = {250, 300, 350},
        FanRecover = {70, 90, 150, 140, 150}, RockfallHold = {150, 200, 200},
        RockfallRecover = {160, 180, 180, 180};

    private static int Timed(float normalized, int first, float[] weights)
    {
        float total = 0, end = 0;
        foreach (float weight in weights) total += weight;
        float elapsed = normalized * total;
        for (int i = 0; i < weights.Length; i++)
        {
            end += weights[i];
            if (elapsed < end) return first + i;
        }
        return first + weights.Length - 1;
    }

    private int MoveIndex(Sprite[] frames)
    {
        if (trial || frames == null || moveFrameWeights == null || moveFrameWeights.Length != frames.Length)
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
