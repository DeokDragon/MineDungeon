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

    [Header("Combat (AI owns timing)")]
    [SerializeField] private Sprite[] windupFrames, fireFrames, recoveryFrames, hitFrames, deathFrames;
    [SerializeField, Min(.01f)] private float fireDuration = .18f, hitDuration = .15f, deathDuration = .45f;
    [Header("Crystal visuals (PPU 64, scale 1)")]
    [SerializeField] private Sprite[] coreChargeFrames, fireFlashFrames, projectileFrames, impactFrames, deathBurstFrames;
    [SerializeField] private Vector2 coreOffset = new Vector2(.0234375f, .046875f);
    private static readonly float[] WindupWeights = { .10f, .11f, .12f, .13f, .14f, .15f };
    private static readonly float[] FireWeights = { .04f, .07f, .07f };
    private static readonly float[] RecoveryWeights = { .14f, .13f, .11f, .09f, .05f };
    private readonly System.Collections.Generic.List<StoneMonsterSpriteEffect> ownedEffects =
        new System.Collections.Generic.List<StoneMonsterSpriteEffect>();
    private sealed class ProjectileDisplay { public SpriteRenderer renderer; public float started; }
    private readonly System.Collections.Generic.List<ProjectileDisplay> projectiles =
        new System.Collections.Generic.List<ProjectileDisplay>();
    private SpriteRenderer coreCharge;
    private StoneMonsterSpriteEffect fireFlash, hitEffect;
    private float hitStarted = float.NegativeInfinity;
    private bool deathStarted;

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

        // Flight survives the end of the body's recovery; only the AI projectile loop ends it.
        projectiles.RemoveAll(p => !p.renderer);
        foreach (var p in projectiles)
            p.renderer.sprite = At(projectileFrames, (Time.time - p.started) % .28f / .28f);
        if (coreCharge)
        {
            coreCharge.transform.localPosition = MirroredCoreOffset();
            coreCharge.sprite = At(coreChargeFrames, (Time.time - ai.PhaseStartedAt) / Mathf.Max(.01f, ai.PhaseDuration));
        }
        if (fireFlash)
        {
            fireFlash.transform.position = CorePosition();
            fireFlash.transform.localScale = body.transform.lossyScale;
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
        if (ai.Phase == CrystalMonsterAI.VisualPhase.Windup)
            pose = Timed(windupFrames, elapsed / Mathf.Max(.01f, ai.PhaseDuration), WindupWeights);
        else if (ai.Phase == CrystalMonsterAI.VisualPhase.Recovery)
        {
            float fire = Mathf.Min(fireDuration, ai.PhaseDuration);
            pose = elapsed < fire ? Timed(fireFrames, elapsed / Mathf.Max(.01f, fire), FireWeights)
                : Timed(recoveryFrames, (elapsed - fire) / Mathf.Max(.01f, ai.PhaseDuration - fire), RecoveryWeights);
        }
        return pose ? pose : At(idleFrames, 0);
    }

    private Vector3 MirroredCoreOffset() => new Vector3(coreOffset.x * (body.flipX ? -1 : 1), coreOffset.y, 0);
    private Vector3 CorePosition() => body.transform.TransformPoint(MirroredCoreOffset());

    private SpriteRenderer Display(string label, Sprite[] frames, Transform parent, int order)
    {
        if (!Has(frames) || !body) return null;
        var go = new GameObject("C2R " + label);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        go.transform.SetParent(parent, false);
        var display = go.AddComponent<SpriteRenderer>();
        display.sprite = frames[0];
        display.sharedMaterial = body.sharedMaterial;
        display.sortingLayerID = body.sortingLayerID;
        display.sortingOrder = body.sortingOrder + order;
        return display;
    }

    private StoneMonsterSpriteEffect Effect(string label, Sprite[] frames, float seconds, Vector3 position,
        bool detached = false, Vector3? scale = null)
    {
        var effect = StoneMonsterSpriteEffect.Play(label, frames, seconds, position, body,
            GetComponentInParent<DungeonRoom>()?.transform, scale ?? Vector3.one, body.sortingOrder + 1);
        if (effect)
        {
            effect.name = "C2R " + label;
            if (!detached) { ownedEffects.RemoveAll(e => !e); ownedEffects.Add(effect); }
        }
        return effect;
    }

    private static void Stop(StoneMonsterSpriteEffect effect)
    { if (effect) { effect.gameObject.SetActive(false); Destroy(effect.gameObject); } }

    private void StopCore()
    {
        if (coreCharge) { coreCharge.gameObject.SetActive(false); Destroy(coreCharge.gameObject); }
        coreCharge = null;
    }

    public void PlayWindup()
    {
        StopCore();
        Face(ai.VisualAttackDirection.x);
        if (Has(windupFrames)) body.sprite = windupFrames[0];
        coreCharge = Display("CoreCharge", coreChargeFrames, body.transform, 1);
        if (coreCharge) coreCharge.transform.localPosition = MirroredCoreOffset();
    }

    public void PlayFire()
    {
        StopCore();
        if (Has(fireFrames)) body.sprite = fireFrames[0];
        Stop(fireFlash);
        fireFlash = Effect("FireFlash", fireFlashFrames, fireDuration, CorePosition(), false, body.transform.lossyScale);
    }

    public SpriteRenderer CreateProjectile(Vector2 position, Vector2 direction, Transform owner)
    {
        var display = Display("Projectile", projectileFrames, owner, 1);
        if (!display) return null;
        display.transform.position = position; // Physics origin, never the chest offset.
        display.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        projectiles.RemoveAll(p => !p.renderer);
        projectiles.Add(new ProjectileDisplay { renderer = display, started = Time.time });
        return display;
    }

    public bool PlayProjectileImpact(Vector2 point)
    {
        if (!Has(impactFrames)) return false;
        Effect("CrystalImpact", impactFrames, .20f, point);
        return true;
    }

    public bool PlayHit(Vector2 point)
    {
        if (!Has(hitFrames) || !Has(impactFrames)) return false;
        hitStarted = Time.time;
        if (!ai.IsAttacking) body.sprite = hitFrames[0];
        Stop(hitEffect);
        hitEffect = Effect("HitImpact", impactFrames, .20f, point);
        return true;
    }

    public bool PlayDeath()
    {
        if (!Has(deathFrames) || !Has(deathBurstFrames)) return false;
        if (deathStarted) return true;
        deathStarted = true;
        CancelCombatVisuals();
        var death = Effect("Death", deathFrames, deathDuration, body.transform.position, true, body.transform.lossyScale);
        if (death)
        {
            death.GetComponent<SpriteRenderer>().sortingOrder = body.sortingOrder;
            death.transform.rotation = body.transform.rotation;
        }
        Effect("DeathBurst", deathBurstFrames, deathDuration, body.transform.position, true);
        body.enabled = false;
        return true;
    }

    public void CancelCombatVisuals()
    {
        StopCore();
        foreach (var effect in ownedEffects) Stop(effect);
        ownedEffects.Clear();
        foreach (var p in projectiles)
            if (p.renderer) { p.renderer.gameObject.SetActive(false); Destroy(p.renderer.gameObject); }
        projectiles.Clear();
        fireFlash = hitEffect = null;
        hitStarted = float.NegativeInfinity;
    }

    private void OnDisable() { CancelCombatVisuals(); }
}
