using UnityEngine;

// Uses NormalMonsterAI's existing state; never writes movement, color or transforms.
[DisallowMultipleComponent]
[RequireComponent(typeof(IronMonsterAI), typeof(Rigidbody2D))]
public sealed class IronMonsterSpriteAnimation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Sprite[] idleFrames, moveFrames;
    [SerializeField, Min(.01f)] private float idleFramesPerSecond = 6f, moveFramesPerSecond = 10f;
    [SerializeField, Min(.01f), Tooltip("이 실제 이동 속도에서 Move 기본 FPS로 재생합니다.")]
    private float referenceMoveSpeed = 2.4f;
    [SerializeField, Min(0f)] private float movementThreshold = .02f;

    [Header("Combat (AI owns all timing)")]
    [SerializeField] private Sprite[] windupFrames, chargeFrames, wallImpactFrames, recoveryFrames, hitFrames, deathFrames;
    [SerializeField, Min(.01f)] private float chargeFramesPerSecond = 12f, wallImpactDuration = .24f,
        hitDuration = .15f, deathDuration = .45f;
    [Header("One-shot VFX and one looping charge trail")]
    [SerializeField] private Sprite[] chargeDustFrames, chargeTrailFrames, wallImpactFxFrames, hitSparksFrames, deathFragmentsFrames;
    private readonly System.Collections.Generic.List<StoneMonsterSpriteEffect> ownedEffects =
        new System.Collections.Generic.List<StoneMonsterSpriteEffect>();
    private StoneMonsterSpriteEffect hitEffect;
    private SpriteRenderer trail;
    private float hitStarted = float.NegativeInfinity, trailStarted;
    private bool deathStarted;
    public bool HasWallImpactEffects => Has(wallImpactFrames) && Has(wallImpactFxFrames);

    private IronMonsterAI ai;
    private Rigidbody2D rb;
    private EnemyHealth health;
    private Vector2 previousPosition;
    private float actualSpeed;
    private double frame;
    private bool moving, attacking;

    private void Awake()
    {
        ai = GetComponent<IronMonsterAI>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        previousPosition = rb.position;
        actualSpeed = 0;
        frame = 0;
        moving = attacking = false;
        hitStarted = float.NegativeInfinity;
        deathStarted = false;
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
        Sprite pose = attacking ? CombatPose() : frames[(int)frame];
        if (!attacking && Time.time - hitStarted < hitDuration && Has(hitFrames))
            pose = At(hitFrames, (Time.time - hitStarted) / hitDuration);
        if (pose) body.sprite = pose;

        if (trail && ai.Phase == IronMonsterAI.VisualPhase.Charge)
        {
            trail.sprite = At(chargeTrailFrames, (Time.time - trailStarted) % .42f / .42f);
            trail.transform.position = (Vector3)rb.position + Vector3.down * .2f - (Vector3)ai.VisualAttackDirection * .25f;
        }

        // Existing charge/wall cues copy the sprite, so mirror those local visuals too.
        if (attacking && ai.AttackVisuals)
            foreach (Transform cue in ai.AttackVisuals)
                if (cue.name == "Attack body cue" && cue.TryGetComponent<SpriteRenderer>(out var display))
                { display.flipX = body.flipX; display.flipY = body.flipY; }
    }

    private void Face(float horizontal)
    {
        if (Mathf.Abs(horizontal) > .02f) body.flipX = horizontal < 0;
    }

    private static bool Has(Sprite[] frames) => frames != null && frames.Length > 0;
    private static Sprite At(Sprite[] frames, float progress) => Has(frames)
        ? frames[Mathf.Clamp((int)(Mathf.Clamp01(progress) * frames.Length), 0, frames.Length - 1)] : null;

    private Sprite CombatPose()
    {
        float elapsed = Mathf.Max(0, Time.time - ai.PhaseStartedAt);
        Sprite pose = null;
        switch (ai.Phase)
        {
            case IronMonsterAI.VisualPhase.Windup:
                pose = At(windupFrames, elapsed / Mathf.Max(.01f, ai.PhaseDuration));
                break;
            case IronMonsterAI.VisualPhase.Charge:
                if (Has(chargeFrames)) pose = chargeFrames[(int)(elapsed * chargeFramesPerSecond) % chargeFrames.Length];
                break;
            case IronMonsterAI.VisualPhase.Recovery:
                float impact = ai.VisualWallImpact ? Mathf.Min(wallImpactDuration, ai.PhaseDuration * .5f) : 0;
                pose = elapsed < impact ? At(wallImpactFrames, elapsed / impact)
                    : At(recoveryFrames, (elapsed - impact) / Mathf.Max(.01f, ai.PhaseDuration - impact));
                break;
        }
        return pose ? pose : At(idleFrames, 0);
    }

    private StoneMonsterSpriteEffect Effect(string label, Sprite[] frames, float seconds, Vector3 position,
        int order, bool detached = false, Vector3? scale = null)
    {
        // Reuse the existing disposable sprite player unchanged; no new manager/effect system.
        var effect = StoneMonsterSpriteEffect.Play(label, frames, seconds, position, body,
            GetComponentInParent<DungeonRoom>()?.transform, scale ?? Vector3.one, order);
        if (effect)
        {
            effect.name = "I3R " + label;
            if (!detached) { ownedEffects.RemoveAll(e => !e); ownedEffects.Add(effect); }
        }
        return effect;
    }

    private static void Orient(Transform visual, Vector2 direction)
    {
        if (!visual) return;
        visual.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        var display = visual.GetComponent<SpriteRenderer>();
        display.flipX = display.flipY = false; // Rotation already includes the full attack heading.
    }

    public void PlayChargeStarted(Vector2 position, Vector2 direction)
    {
        EndChargeVisuals();
        Face(direction.x);
        if (Has(chargeFrames)) body.sprite = chargeFrames[0];
        var dust = Effect("ChargeDust", chargeDustFrames, .20f, position, body.sortingOrder - 2);
        if (dust) Orient(dust.transform, direction);
        if (!Has(chargeTrailFrames) || !ai.AttackVisuals) return;
        trail = new GameObject("I3R ChargeTrail").AddComponent<SpriteRenderer>();
        trail.transform.SetParent(ai.AttackVisuals, false);
        trail.sharedMaterial = body.sharedMaterial;
        trail.sortingLayerID = body.sortingLayerID;
        trail.sortingOrder = body.sortingOrder - 2;
        trailStarted = Time.time;
        trail.sprite = chargeTrailFrames[0];
        Orient(trail.transform, direction);
    }

    public void EndChargeVisuals()
    {
        if (trail) { trail.gameObject.SetActive(false); Destroy(trail.gameObject); }
        trail = null;
    }

    public bool PlayWallImpact(Vector2 point, Vector2 direction)
    {
        if (!HasWallImpactEffects) return false;
        body.sprite = wallImpactFrames[0];
        var effect = Effect("WallImpact", wallImpactFxFrames, .30f, point, body.sortingOrder + 1);
        if (effect) Orient(effect.transform, direction);
        return true;
    }

    private static void Stop(StoneMonsterSpriteEffect effect)
    {
        if (effect) { effect.gameObject.SetActive(false); Destroy(effect.gameObject); }
    }

    public bool PlayHit(Vector2 point)
    {
        if (!Has(hitFrames) || !Has(hitSparksFrames)) return false;
        hitStarted = Time.time;
        if (!ai.IsAttacking) body.sprite = hitFrames[0];
        Stop(hitEffect); // At most one active sparks sheet per monster, even on rapid hits.
        hitEffect = Effect("HitSparks", hitSparksFrames, .18f, point, body.sortingOrder + 1);
        return true;
    }

    public bool PlayDeath()
    {
        if (!Has(deathFrames) || !Has(deathFragmentsFrames)) return false;
        if (deathStarted) return true;
        deathStarted = true;
        CancelCombatVisuals();
        var death = Effect("Death", deathFrames, deathDuration, body.transform.position,
            body.sortingOrder, true, body.transform.lossyScale);
        if (death) death.transform.rotation = body.transform.rotation;
        Effect("DeathFragments", deathFragmentsFrames, deathDuration, body.transform.position,
            body.sortingOrder + 1, true);
        body.enabled = false; // EnemyHealth removes the actor now; detached visuals finish independently.
        return true;
    }

    public void CancelCombatVisuals()
    {
        EndChargeVisuals();
        foreach (var effect in ownedEffects) Stop(effect);
        ownedEffects.Clear();
        hitEffect = null;
        hitStarted = float.NegativeInfinity;
    }

    private void OnDisable() { CancelCombatVisuals(); }
}
