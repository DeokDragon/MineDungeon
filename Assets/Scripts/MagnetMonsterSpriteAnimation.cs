using UnityEngine;

// Sprite-only locomotion. NormalMonsterAI owns attacks; StoneHitFeedback owns color/transforms.
[DisallowMultipleComponent]
[RequireComponent(typeof(MagnetMonsterAI), typeof(Rigidbody2D))]
public sealed class MagnetMonsterSpriteAnimation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Sprite[] idleFrames, moveFrames;
    [SerializeField, Min(.01f)] private float idleFramesPerSecond = 6f, moveFramesPerSecond = 10f;
    [SerializeField, Min(.01f), Tooltip("이 실제 이동 속도에서 Move 기본 FPS로 재생합니다.")]
    private float referenceMoveSpeed = 2.3f;
    [SerializeField, Min(0f)] private float movementThreshold = .02f;

    [Header("Combat (AI owns timing)")]
    [SerializeField] private Sprite[] windupFrames, impactFrames, recoveryFrames, hitFrames, deathFrames;
    [SerializeField, Min(.01f)] private float impactDuration = .24f, hitDuration = .15f, deathDuration = .56f;
    [Header("Magnetic visuals (PPU 64)")]
    [SerializeField] private Sprite[] chargeHaloFrames, fieldImpactFrames, hitFragmentsFrames, deathFragmentsFrames;
    [SerializeField, Min(.01f), Tooltip("Reference scale for a damage radius of 1.8; affects visuals only.")]
    private float fieldImpactScale = 1.028633f;
    private static readonly float[] WindupWeights = { .12f, .13f, .15f, .16f, .17f, .17f };
    private static readonly float[] ImpactWeights = { .05f, .07f, .06f, .06f };
    private static readonly float[] RecoveryWeights = { .14f, .15f, .16f, .16f, .15f };
    private readonly System.Collections.Generic.List<StoneMonsterSpriteEffect> ownedEffects =
        new System.Collections.Generic.List<StoneMonsterSpriteEffect>();
    private SpriteRenderer halo;
    private StoneMonsterSpriteEffect hitEffect;
    private StoneMonsterSpriteEffect lethalImpact;
    private float hitStarted = float.NegativeInfinity;
    private bool deathStarted;

    private MagnetMonsterAI ai;
    private Rigidbody2D rb;
    private EnemyHealth health;
    private Vector2 previousPosition;
    private float actualSpeed;
    private double frame;
    private bool moving, attacking;

    private void Awake()
    {
        ai = GetComponent<MagnetMonsterAI>();
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
        Sprite pose = attacking ? CombatPose() : frames[(int)frame];
        if (!attacking && Time.time - hitStarted < hitDuration && Has(hitFrames))
            pose = At(hitFrames, (Time.time - hitStarted) / hitDuration);
        if (pose) body.sprite = pose;
        if (halo)
        {
            halo.sprite = Timed(chargeHaloFrames,
                (Time.time - ai.PhaseStartedAt) / Mathf.Max(.01f, ai.PhaseDuration), WindupWeights);
            halo.flipX = body.flipX;
            halo.flipY = body.flipY;
        }
    }

    private void Face(float horizontal)
    {
        if (Mathf.Abs(horizontal) > .02f) body.flipX = horizontal < 0;
    }

    private static bool Has(Sprite[] frames) => frames != null && frames.Length > 0;
    private static Sprite At(Sprite[] frames, float progress) => Has(frames)
        ? frames[Mathf.Clamp((int)(Mathf.Clamp01(progress) * frames.Length), 0, frames.Length - 1)] : null;
    private static Sprite Timed(Sprite[] frames, float progress, float[] weights)
    {
        if (!Has(frames)) return null;
        if (frames.Length != weights.Length) return At(frames, progress);
        float total = 0;
        foreach (float w in weights) total += w;
        float elapsed = Mathf.Clamp01(progress) * total, end = 0;
        for (int i = 0; i < weights.Length; i++) { end += weights[i]; if (elapsed < end) return frames[i]; }
        return frames[frames.Length - 1];
    }
    private Sprite CombatPose()
    {
        float elapsed = Mathf.Max(0, Time.time - ai.PhaseStartedAt);
        Sprite pose = null;
        if (ai.Phase == MagnetMonsterAI.VisualPhase.Windup)
            pose = Timed(windupFrames, elapsed / Mathf.Max(.01f, ai.PhaseDuration), WindupWeights);
        else if (ai.Phase == MagnetMonsterAI.VisualPhase.Recovery)
        {
            float explosion = Mathf.Min(impactDuration, ai.PhaseDuration);
            pose = elapsed < explosion ? Timed(impactFrames, elapsed / Mathf.Max(.01f, explosion), ImpactWeights)
                : Timed(recoveryFrames, (elapsed - explosion) / Mathf.Max(.01f, ai.PhaseDuration - explosion), RecoveryWeights);
        }
        return pose ? pose : At(idleFrames, 0);
    }
    private StoneMonsterSpriteEffect Effect(string label, Sprite[] frames, float seconds,
        Vector3 position, bool detached = false, Vector3? scale = null, int order = 1)
    {
        var effect = StoneMonsterSpriteEffect.Play(label, frames, seconds, position, body,
            GetComponentInParent<DungeonRoom>()?.transform, scale ?? Vector3.one, body.sortingOrder + order);
        if (effect)
        {
            effect.name = "M1R " + label;
            if (!detached) { ownedEffects.RemoveAll(e => !e); ownedEffects.Add(effect); }
        }
        return effect;
    }
    private static void Stop(StoneMonsterSpriteEffect effect)
    { if (effect) { effect.gameObject.SetActive(false); Destroy(effect.gameObject); } }
    private void StopHalo()
    {
        if (halo) { halo.gameObject.SetActive(false); Destroy(halo.gameObject); }
        halo = null;
    }
    public bool PlayWindup()
    {
        StopHalo();
        Face(ai.VisualAttackDirection.x);
        if (Has(windupFrames)) body.sprite = windupFrames[0];
        if (!Has(chargeHaloFrames)) return false;
        var go = new GameObject("M1R ChargeHalo");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        go.transform.SetParent(body.transform, false); // Centered on the body; follows existing hit deformation.
        halo = go.AddComponent<SpriteRenderer>();
        halo.sprite = chargeHaloFrames[0];
        halo.sharedMaterial = body.sharedMaterial;
        halo.sortingLayerID = body.sortingLayerID;
        halo.sortingOrder = body.sortingOrder - 1;
        halo.flipX = body.flipX;
        halo.flipY = body.flipY;
        return true;
    }
    public bool PlayImpact(Vector2 destination, float damageRadius, bool showBodyPose)
    {
        StopHalo();
        if (showBodyPose && Has(impactFrames)) body.sprite = impactFrames[0];
        if (!Has(fieldImpactFrames)) return false;
        // A committed lethal impact outlives run-end AI cancellation for its existing .26s.
        // It remains room-owned; owner death/disable also disposes it immediately.
        if (!showBodyPose) Stop(lethalImpact);
        var impact = Effect("FieldImpact", fieldImpactFrames, .26f, destination, !showBodyPose,
            Vector3.one * (fieldImpactScale * damageRadius / 1.8f), -1);
        if (!showBodyPose) lethalImpact = impact;
        return true;
    }
    public bool PlayHit(Vector2 point)
    {
        if (!Has(hitFrames) || !Has(hitFragmentsFrames)) return false;
        hitStarted = Time.time;
        if (!ai.IsAttacking) body.sprite = hitFrames[0];
        Stop(hitEffect);
        hitEffect = Effect("HitFragments", hitFragmentsFrames, .18f, point);
        return true;
    }
    public bool PlayDeath()
    {
        if (!Has(deathFrames) || !Has(deathFragmentsFrames)) return false;
        if (deathStarted) return true;
        deathStarted = true;
        CancelCombatVisuals();
        Stop(lethalImpact);
        var death = Effect("Death", deathFrames, deathDuration, body.transform.position, true,
            body.transform.lossyScale, 0);
        if (death) death.transform.rotation = body.transform.rotation;
        Effect("DeathFragments", deathFragmentsFrames, deathDuration, body.transform.position, true);
        body.enabled = false;
        return true;
    }
    public void CancelCombatVisuals()
    {
        StopHalo();
        foreach (var effect in ownedEffects) Stop(effect);
        ownedEffects.Clear();
        hitEffect = null;
        hitStarted = float.NegativeInfinity;
    }
    private void OnDisable() { CancelCombatVisuals(); Stop(lethalImpact); lethalImpact = null; }
}
