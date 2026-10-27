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
    [Header("P2 attacks - enhanced and basic combo presentation")]
    [SerializeField] private Sprite[] phase2LeapSlam, phase2GroundSmash, phase2RockCharge, phase2StoneFan, phase2Rockfall;
    [Header("Presentation timing trials - not final balance")]
    [SerializeField] private Sprite[] introFrames, transformFrames, transformChips;
    [SerializeField] private SpriteRenderer chipsVisual;
    [SerializeField, Min(.01f)] private float introDuration = 2f, transformDuration = 2.5f;
    private bool presentationActive, presentationTransform;
    private float presentationStartedAt;
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
        ai.PresentationStarted -= OnPresentationStarted;
        ai.PresentationEnded -= OnPresentationEnded;
        ai.PresentationStarted += OnPresentationStarted;
        ai.PresentationEnded += OnPresentationEnded;
        ClearPresentationVisual(false);
        previousPosition = rb.position;
        actualSpeed = 0;
        frame = 0;
        phase = ai.Phase;
        moving = combat = false;
        Show(phase == 2 ? phase2Idle : phase1Idle, 0);
        if (ai.IsPresenting) OnPresentationStarted(ai, ai.IsTransforming);
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
        if (!body)
        {
            ClearPresentationVisual(false);
            if (ai.IsPresenting) ai.EndPresentation();
            return;
        }
        if (health.IsDead || !ai.isActiveAndEnabled)
        {
            ClearPresentationVisual(!health.IsDead);
            ClearAttackPose();
            return;
        }
        if (presentationActive && !ai.IsPresenting) ClearPresentationVisual(true);
        if (Time.timeScale == 0f) return;
        if (ai.IsPresenting)
        {
            if (!presentationActive) OnPresentationStarted(ai, ai.IsTransforming);
            TickPresentation();
            return;
        }
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
        // Keep existing flipX; presentations or missing attack art use the base pose.
        Show(frames, combat ? 0 : moving ? MoveIndex(frames) : (int)frame);
    }

    private void OnDisable()
    {
        if (ai != null)
        {
            ai.PresentationStarted -= OnPresentationStarted;
            ai.PresentationEnded -= OnPresentationEnded;
        }
        ClearPresentationVisual(health != null && !health.IsDead);
        // Disabling just the art driver must not leave its AI-owned combat lock forever.
        if (ai != null && ai.isActiveAndEnabled && ai.IsPresenting)
        {
            ai.EndPresentation();
            if (health != null && !health.IsDead) Show(ai.Phase == 2 ? phase2Idle : phase1Idle, 0);
        }
        ClearAttackPose();
    }

    private void OnPresentationStarted(StoneGolemBossAI source, bool transformation)
    {
        if (source != ai || presentationActive) return;
        var frames = transformation ? transformFrames : introFrames;
        if (!body || frames == null || frames.Length == 0) { ai.EndPresentation(); return; }
        presentationActive = true;
        presentationTransform = transformation;
        presentationStartedAt = Time.time;
        showingAttack = false;
        frame = 0;
        if (chipsVisual) { chipsVisual.enabled = false; chipsVisual.sprite = null; }
        Show(frames, 0);
    }

    private void OnPresentationEnded(StoneGolemBossAI source, bool transformation)
    {
        if (source == ai) ClearPresentationVisual(true);
    }

    private void TickPresentation()
    {
        if (!presentationActive || !ai.IsPresenting) return;
        float duration = presentationTransform ? transformDuration : introDuration;
        float elapsed = Mathf.Max(0f, Time.time - presentationStartedAt);
        if (elapsed >= duration) { ai.EndPresentation(); return; }
        float normalized = elapsed / duration;
        Show(presentationTransform ? transformFrames : introFrames,
            Timed(normalized, 0, presentationTransform ? TransformTiming : IntroTiming));
        if (chipsVisual)
        {
            // The chip clock is normalized with the body trial; no combat clock is advanced.
            float sourceTime = normalized * 2.5f;
            bool visible = presentationTransform && sourceTime >= .86f && sourceTime < 2.34f &&
                transformChips != null && transformChips.Length == 8;
            chipsVisual.enabled = visible;
            if (visible)
            {
                chipsVisual.sprite = transformChips[Timed((sourceTime - .86f) / 1.48f, 0, ChipsTiming)];
                chipsVisual.flipX = body.flipX;
                chipsVisual.flipY = body.flipY;
            }
            else chipsVisual.sprite = null;
        }
    }

    private void ClearPresentationVisual(bool restoreIdle)
    {
        bool wasActive = presentationActive;
        presentationActive = false;
        if (chipsVisual) { chipsVisual.enabled = false; chipsVisual.sprite = null; }
        if (wasActive)
        {
            frame = 0;
            moving = combat = false;
            if (restoreIdle && health != null && !health.IsDead && ai != null)
                Show(ai.Phase == 2 ? phase2Idle : phase1Idle, 0);
        }
    }

    private static readonly float[] IntroTiming = {180, 180, 200, 200, 220, 220, 200, 200, 200, 200},
        TransformTiming = {160, 170, 170, 180, 180, 180, 190, 190, 190, 190, 180, 180, 180, 160},
        ChipsTiming = {180, 190, 190, 190, 190, 180, 180, 180};

    private void ClearAttackPose()
    {
        if (!showingAttack) return;
        showingAttack = false;
        frame = 0;
        Show(ai != null && ai.Phase == 2 ? phase2Idle : phase1Idle, 0);
    }

    private bool ShowAttack()
    {
        if (ai.IsPresenting) return false;
        bool phase2 = ai.Phase == 2;
        Sprite[] frames;
        switch (ai.ActivePattern)
        {
            case StoneGolemBossAI.Pattern.LeapSlam: frames = phase2 ? phase2LeapSlam : leapSlam; break;
            case StoneGolemBossAI.Pattern.GroundSmash: frames = phase2 ? phase2GroundSmash : groundSmash; break;
            case StoneGolemBossAI.Pattern.RockCharge: frames = phase2 ? phase2RockCharge : rockCharge; break;
            case StoneGolemBossAI.Pattern.StoneFan: frames = phase2 ? phase2StoneFan : stoneFan; break;
            case StoneGolemBossAI.Pattern.Rockfall: frames = phase2 ? phase2Rockfall : rockfall; break;
            default: return false;
        }
        if (frames == null || frames.Length == 0) return false;
        float t = ai.VisualStateElapsed;
        float duration = Mathf.Max(.00001f, ai.VisualStateDuration);
        float normalized = Mathf.Clamp01(t / duration);
        int index;
        if (phase2 && Phase2Index(normalized, out index)) { }
        else if (ai.State == StoneGolemBossAI.BossState.Warning)
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

    private bool Phase2Index(float normalized, out int index)
    {
        var state = ai.State;
        var pattern = ai.ActivePattern;
        index = 0;
        if (state == StoneGolemBossAI.BossState.SecondaryWarning)
            index = pattern == StoneGolemBossAI.Pattern.LeapSlam
                ? Sequence(normalized, ShockPrepareFrames, ShockPrepare)
                : Sequence(normalized, SmashSecondFrames, SmashSecond);
        else if (state == StoneGolemBossAI.BossState.FanFollowup)
            index = Sequence(normalized, FanSecondFrames, FanSecond);
        else if (state == StoneGolemBossAI.BossState.RockfallGap) index = 10;
        else if (state == StoneGolemBossAI.BossState.ComboAInterval)
            index = Timed(normalized, 6, ComboLanding);
        else if (state == StoneGolemBossAI.BossState.Warning && ai.VisualComboFollowup &&
            pattern == StoneGolemBossAI.Pattern.StoneFan)
            index = Sequence(normalized, ComboFanFrames, ComboFan);
        else if (state == StoneGolemBossAI.BossState.Warning &&
            ai.VisualPreviousState == StoneGolemBossAI.BossState.ComboAInterval &&
            pattern == StoneGolemBossAI.Pattern.GroundSmash)
            index = Sequence(normalized, ComboSmashFrames, ComboSmash);
        else if (state == StoneGolemBossAI.BossState.Recovery &&
            pattern == StoneGolemBossAI.Pattern.LeapSlam && ai.IsEnhanced)
            index = ai.VisualSecondaryStarted
                ? Sequence(normalized, ShockRecoverFrames, LeapRecover)
                : Sequence(normalized, LeapWaitFrames, LeapWait);
        else if (state == StoneGolemBossAI.BossState.Rockfall &&
            ai.VisualPreviousState == StoneGolemBossAI.BossState.RockfallGap &&
            ai.VisualStateElapsed < ai.VisualStateDuration)
            index = Timed(normalized, 1, RockfallRepeat);
        else return false;
        return true;
    }

    // Frame numbers below are zero-based; time weights come from the P2 README.
    private static readonly int[] LeapWaitFrames = {6, 7, 8, 11}, ShockPrepareFrames = {12, 13},
        ShockRecoverFrames = {14, 15, 8, 9, 10}, SmashSecondFrames = {5, 6, 10, 1, 2, 3, 4},
        FanSecondFrames = {3, 4, 8, 2}, ComboSmashFrames = {10, 1, 2, 3, 4}, ComboFanFrames = {8, 2};
    private static readonly float[] LeapWait = {80, 120, 200, 900}, ShockPrepare = {300, 500},
        SmashSecond = {70, 130, 200, 250, 300, 150, 100}, FanSecond = {70, 90, 340, 400},
        ComboLanding = {80, 120}, ComboSmash = {200, 300, 350, 250, 100},
        ComboFan = {60, 140}, RockfallRepeat = {300, 600};

    private static int Sequence(float normalized, int[] indices, float[] weights)
    {
        return indices[Timed(normalized, 0, weights)];
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
