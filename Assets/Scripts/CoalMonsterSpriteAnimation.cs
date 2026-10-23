using UnityEngine;

// Sprite-only locomotion. NormalMonsterAI owns attacks; StoneHitFeedback owns color/transforms.
[DisallowMultipleComponent]
[RequireComponent(typeof(CoalMonsterAI), typeof(Rigidbody2D))]
public sealed class CoalMonsterSpriteAnimation : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Sprite[] idleFrames, moveFrames;
    [SerializeField, Min(.01f)] private float idleFramesPerSecond = 6f, moveFramesPerSecond = 10f;
    [SerializeField, Min(.01f), Tooltip("이 실제 이동 속도에서 Move 기본 FPS로 재생합니다.")]
    private float referenceMoveSpeed = 3f;
    [SerializeField, Min(0f)] private float movementThreshold = .02f;

    [Header("Combat (AI owns timing)")]
    [SerializeField] private Sprite[] windupFrames, explosionFrames, coolingFrames, hitFrames, deathFrames;
    [SerializeField, Min(.01f)] private float explosionDuration = .24f, hitDuration = .15f, deathDuration = .45f;
    [Header("Coal visuals (PPU 64)")]
    [SerializeField] private Sprite[] furnaceChargeFrames, heatExplosionFrames, embersFrames, hitFragmentsFrames, deathBurstFrames;
    [SerializeField, Min(.01f), Tooltip("Reference scale for a damage radius of 2; affects visuals only.")]
    private float heatExplosionScale = 2.94857f;
    private static readonly float[] WindupWeights = { .10f, .12f, .14f, .15f, .16f, .18f };
    private static readonly float[] CoolingWeights = { .18f, .19f, .20f, .21f, .18f };
    private readonly System.Collections.Generic.List<StoneMonsterSpriteEffect> ownedEffects =
        new System.Collections.Generic.List<StoneMonsterSpriteEffect>();
    private SpriteRenderer furnace;
    private StoneMonsterSpriteEffect hitEffect;
    private float hitStarted = float.NegativeInfinity;
    private bool deathStarted;

    private CoalMonsterAI ai;
    private Rigidbody2D rb;
    private EnemyHealth health;
    private Vector2 previousPosition;
    private float actualSpeed;
    private double frame;
    private bool moving, attacking;

    private void Awake()
    {
        ai = GetComponent<CoalMonsterAI>();
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
        if (furnace)
        {
            furnace.sprite = Timed(furnaceChargeFrames,
                (Time.time - ai.PhaseStartedAt) / Mathf.Max(.01f, ai.PhaseDuration), WindupWeights);
            furnace.flipX = body.flipX;
            furnace.flipY = body.flipY;
        }
        // Fallback only when the new furnace resource is unavailable.
        if (attacking && ai.AttackVisuals)
            foreach (Transform cue in ai.AttackVisuals)
                if (cue.name == "Attack body cue" && cue.TryGetComponent<SpriteRenderer>(out var display))
                { display.sprite = body.sprite; display.flipX = body.flipX; display.flipY = body.flipY; }
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
        if (ai.Phase == CoalMonsterAI.VisualPhase.Windup)
            pose = Timed(windupFrames, elapsed / Mathf.Max(.01f, ai.PhaseDuration), WindupWeights);
        else if (ai.Phase == CoalMonsterAI.VisualPhase.Recovery)
        {
            float explosion = Mathf.Min(explosionDuration, ai.PhaseDuration);
            pose = elapsed < explosion ? At(explosionFrames, elapsed / Mathf.Max(.01f, explosion))
                : Timed(coolingFrames, (elapsed - explosion) / Mathf.Max(.01f, ai.PhaseDuration - explosion), CoolingWeights);
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
            effect.name = "C1R " + label;
            if (!detached) { ownedEffects.RemoveAll(e => !e); ownedEffects.Add(effect); }
        }
        return effect;
    }
    private static void Stop(StoneMonsterSpriteEffect effect)
    { if (effect) { effect.gameObject.SetActive(false); Destroy(effect.gameObject); } }
    private void StopFurnace()
    {
        if (furnace) { furnace.gameObject.SetActive(false); Destroy(furnace.gameObject); }
        furnace = null;
    }
    public bool PlayWindup()
    {
        StopFurnace();
        Face(ai.VisualAttackDirection.x);
        if (Has(windupFrames)) body.sprite = windupFrames[0];
        if (!Has(furnaceChargeFrames)) return false;
        var go = new GameObject("C1R FurnaceCharge");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        go.transform.SetParent(body.transform, false); // Same 64px pivot, follows hit deformation exactly.
        furnace = go.AddComponent<SpriteRenderer>();
        furnace.sprite = furnaceChargeFrames[0];
        furnace.sharedMaterial = body.sharedMaterial;
        furnace.sortingLayerID = body.sortingLayerID;
        furnace.sortingOrder = body.sortingOrder + 1;
        furnace.flipX = body.flipX;
        furnace.flipY = body.flipY;
        return true;
    }
    public bool PlayExplosion(Vector2 origin, float damageRadius)
    {
        StopFurnace();
        if (Has(explosionFrames)) body.sprite = explosionFrames[0];
        if (!Has(heatExplosionFrames) || !Has(embersFrames)) return false;
        // Scale the art to the existing radius; never feed the visual scale into HitArea.
        Effect("HeatExplosion", heatExplosionFrames, explosionDuration, origin, false,
            Vector3.one * (heatExplosionScale * damageRadius / 2f), -1);
        Effect("CoalEmbers", embersFrames, explosionDuration, origin);
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
        if (!Has(deathFrames) || !Has(deathBurstFrames)) return false;
        if (deathStarted) return true;
        deathStarted = true;
        CancelCombatVisuals();
        var death = Effect("Death", deathFrames, deathDuration, body.transform.position, true,
            body.transform.lossyScale, 0);
        if (death) death.transform.rotation = body.transform.rotation;
        Effect("DeathBurst", deathBurstFrames, deathDuration, body.transform.position, true);
        body.enabled = false;
        return true;
    }
    public void CancelCombatVisuals()
    {
        StopFurnace();
        foreach (var effect in ownedEffects) Stop(effect);
        ownedEffects.Clear();
        hitEffect = null;
        hitStarted = float.NegativeInfinity;
    }
    private void OnDisable() { CancelCombatVisuals(); }
}
