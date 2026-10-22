using UnityEngine;

// Sprite-only playback: JumpVisual and StoneHitFeedback retain transform/color ownership.
[DisallowMultipleComponent]
[RequireComponent(typeof(StoneMonsterAI), typeof(Rigidbody2D))]
public sealed class StoneMonsterSpriteAnimation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] moveFrames;
    [SerializeField, Min(.01f)] private float idleFramesPerSecond = 6f;
    [SerializeField, Min(.01f)] private float moveFramesPerSecond = 10f;
    [SerializeField, Min(.01f), Tooltip("이 속도에서 Move 기본 FPS로 재생합니다. 실제 이동량에 비례합니다.")]
    private float referenceMoveSpeed = 2.6f;
    [SerializeField, Min(0f)] private float movementThreshold = .02f;

    [Header("Combat poses (AI owns timing)")]
    [SerializeField] private Sprite[] windupFrames, jumpFrames, landingFrames, recoveryFrames, hitFrames, deathFrames;
    [SerializeField, Min(.01f)] private float landingDuration = .18f, hitDuration = .15f, deathDuration = .45f;
    [Header("One-shot effects (PPU 64, world scale 1)")]
    [SerializeField] private Sprite[] jumpDustFrames, landingShockwaveFrames, landingDustFrames,
        stoneFragmentsFrames, hitFragmentsFrames, deathBurstFrames;

    private StoneMonsterAI ai;
    private Rigidbody2D rb;
    private EnemyHealth health;
    private Vector2 previousPosition;
    private float actualSpeed;
    private double frame;
    private bool moving, combat;
    private float hitStarted = float.NegativeInfinity;
    private bool deathStarted;
    private readonly System.Collections.Generic.List<StoneMonsterSpriteEffect> effects =
        new System.Collections.Generic.List<StoneMonsterSpriteEffect>();

    private void Awake()
    {
        ai = GetComponent<StoneMonsterAI>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        previousPosition = rb.position;
        actualSpeed = 0f;
        frame = 0;
        moving = combat = false;
        hitStarted = float.NegativeInfinity;
        deathStarted = false;
        Show(idleFrames, 0);
    }

    private void FixedUpdate()
    {
        // Measure completed physics movement, so pushing against a wall stays Idle.
        actualSpeed = Vector2.Distance(rb.position, previousPosition) / Time.fixedDeltaTime;
        previousPosition = rb.position;
    }

    private void LateUpdate()
    {
        if (!body || (health && health.IsDead) || Time.timeScale == 0f) return;
        bool nextCombat = !ai.CanPlayLocomotion;
        bool nextMoving = !nextCombat && actualSpeed > movementThreshold;
        if (nextMoving != moving || nextCombat != combat) frame = 0;
        moving = nextMoving;
        combat = nextCombat;
        Sprite pose;
        if (combat)
        {
            pose = CombatPose();
        }
        else
        {
            var frames = moving ? moveFrames : idleFrames;
            if (frames == null || frames.Length == 0) return;
            float fps = moving ? moveFramesPerSecond * actualSpeed / Mathf.Max(.01f, referenceMoveSpeed) : idleFramesPerSecond;
            frame = (frame + Time.deltaTime * fps) % frames.Length;
            pose = frames[(int)frame];
        }
        // Preserve the attack silhouette, especially its short landing contact pose.
        // Hit flash, transform reaction and fragments still run during combat.
        if (!combat && Time.time - hitStarted < hitDuration && Has(hitFrames))
            pose = At(hitFrames, (Time.time - hitStarted) / hitDuration);
        if (pose) body.sprite = pose;
    }

    private static bool Has(Sprite[] frames) => frames != null && frames.Length > 0;
    private static Sprite At(Sprite[] frames, float progress) => Has(frames)
        ? frames[Mathf.Clamp((int)(Mathf.Clamp01(progress) * frames.Length), 0, frames.Length - 1)] : null;

    private Sprite CombatPose()
    {
        Sprite pose = null;
        if (ai.IsWindingUp) pose = At(windupFrames, ai.VisualPhaseTime / Mathf.Max(.01f, ai.WarningDuration));
        else if (ai.IsJumping) pose = At(jumpFrames, ai.VisualPhaseTime / Mathf.Max(.01f, ai.JumpDuration));
        else if (ai.IsRecovering)
        {
            // Landing and Recovery share the existing window; never add 0.18s to combat.
            float landing = Mathf.Min(landingDuration, ai.RecoveryDuration * .5f);
            pose = ai.VisualPhaseTime < landing ? At(landingFrames, ai.VisualPhaseTime / landing)
                : At(recoveryFrames, (ai.VisualPhaseTime - landing) / Mathf.Max(.01f, ai.RecoveryDuration - landing));
        }
        return pose ? pose : At(idleFrames, 0);
    }

    private Transform Room => GetComponentInParent<DungeonRoom>()?.transform;

    private StoneMonsterSpriteEffect Effect(string label, Sprite[] frames, float seconds, Vector3 position,
        int order, bool detached = false, Vector3? scale = null)
    {
        var effect = StoneMonsterSpriteEffect.Play(label, frames, seconds, position, body, Room, scale ?? Vector3.one, order);
        if (effect && !detached)
        {
            effects.RemoveAll(e => !e);
            effects.Add(effect);
        }
        return effect;
    }

    public void PlayJumpDust(Vector2 position)
    {
        Effect("JumpDust", jumpDustFrames, .20f, position, body.sortingOrder - 2);
    }

    public bool PlayLanding(Vector2 position)
    {
        if (!Has(landingShockwaveFrames) || !Has(landingDustFrames)) return false;
        // Ground effects use world scale 1: max shockwave radius ~57.6px / 64 = 0.9.
        Effect("LandingShockwave", landingShockwaveFrames, .25f, position, body.sortingOrder - 2);
        Effect("LandingDust", landingDustFrames, .25f, position, body.sortingOrder - 1);
        Effect("StoneFragments", stoneFragmentsFrames, .30f, position, body.sortingOrder + 1);
        if (Has(landingFrames)) body.sprite = landingFrames[0];
        return true;
    }

    public bool PlayHit(Vector2 point)
    {
        if (!Has(hitFrames) || !Has(hitFragmentsFrames)) return false;
        hitStarted = Time.time;
        if (ai.CanPlayLocomotion) body.sprite = hitFrames[0];
        Effect("HitFragments", hitFragmentsFrames, .18f, point, body.sortingOrder + 1);
        return true;
    }

    // AI calls this only during moving Chase and once at warning start.
    // No calls during Jump/Recovery: the attack keeps its initial visual facing.
    public void FaceTarget(float horizontalDelta)
    {
        if (body && Mathf.Abs(horizontalDelta) > .02f) body.flipX = horizontalDelta < 0f;
    }

    public bool PlayDeath()
    {
        if (!Has(deathFrames) || !Has(deathBurstFrames)) return false;
        if (deathStarted) return true;
        deathStarted = true;
        CancelCombatVisuals();
        // EnemyHealth still removes the actor immediately. Only these visuals survive.
        var death = Effect("Death", deathFrames, deathDuration, body.transform.position,
            body.sortingOrder, true, body.transform.lossyScale);
        if (death) death.transform.rotation = body.transform.rotation;
        Effect("DeathBurst", deathBurstFrames, deathDuration, body.transform.position, body.sortingOrder + 1, true);
        body.enabled = false;
        return true;
    }

    public void CancelCombatVisuals()
    {
        hitStarted = float.NegativeInfinity;
        foreach (var effect in effects)
            if (effect) { effect.gameObject.SetActive(false); Destroy(effect.gameObject); }
        effects.Clear();
    }

    private void OnDisable() { CancelCombatVisuals(); }

    private void Show(Sprite[] frames, int index)
    {
        if (body && frames != null && frames.Length > index && frames[index]) body.sprite = frames[index];
    }
}
