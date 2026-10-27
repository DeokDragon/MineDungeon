using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(EnemyHealth))]
public partial class StoneGolemBossAI : MonoBehaviour
{
    public enum BossState { Chase, Warning, Jump, Recovery, Waiting, Charge, Rockfall, Presentation, SecondaryWarning, FanFollowup, RockfallGap, ComboAInterval }
    public enum Pattern { None, LeapSlam, GroundSmash, RockCharge, StoneFan, Rockfall }

    [Header("References (player is bound by DungeonRoom)")]
    [SerializeField] private Transform body;
    [SerializeField] private SpriteRenderer attackWarning;
    [SerializeField] private LayerMask wallLayer;

    [Header("Movement / shared timing")]
    [SerializeField, Min(0f)] private float moveSpeed = 1.4f;
    [SerializeField, Min(0f)] private float stopDistance = 2f;
    [SerializeField, Min(0f)] private float commonWait = 0.4f;
    [SerializeField, Min(0.001f)] private float wallSkin = 0.03f;

    [Header("Leap slam")]
    [SerializeField, Min(0.01f)] private float leapRadius = 2.2f;
    [SerializeField, Min(0.01f)] private float maxLeapDistance = 7f;
    [SerializeField, Min(0.01f)] private float leapWarning = 1f;
    [SerializeField, Min(0.01f)] private float leapDuration = 0.6f;
    [SerializeField, Min(0f)] private float leapCooldown = 8f;
    [SerializeField, Min(0f)] private float leapRecovery = 0.7f;
    [SerializeField, Min(1)] private int leapDamage = 20;
    [SerializeField, Min(0f)] private float jumpHeight = 1.8f;

    [Header("Ground smash")]
    [SerializeField, Min(0.01f)] private float smashRadius = 3.5f;
    [SerializeField, Min(0f)] private float smashSelectionDistance = 3.5f;
    [SerializeField, Min(0.01f)] private float smashWarning = 1.2f;
    [SerializeField, Min(0f)] private float smashCooldown = 6f;
    [SerializeField, Min(0f)] private float smashRecovery = 0.8f;
    [SerializeField, Min(1)] private int smashDamage = 15;

    [Header("Rock charge / second-pass test values")]
    [SerializeField, Min(.01f)] private float chargeWidth = 2.4f;
    [SerializeField] private SpriteRenderer chargePath;
    [SerializeField] private SpriteRenderer chargeStartCap;
    [SerializeField] private SpriteRenderer chargeEndCap;
    [SerializeField, Min(0.01f)] private float chargeMaxDistance = 9f;
    [SerializeField, Min(0.01f)] private float chargeWarning = 1f;
    [SerializeField, Min(0.01f)] private float chargeSpeed = 10f;
    [SerializeField, Min(0.01f)] private float chargeMaxDuration = 0.9f;
    [SerializeField, Min(0f)] private float chargeCooldown = 10f;
    [SerializeField, Min(0f)] private float chargeRecovery = 0.7f;
    [SerializeField, Min(0f)] private float chargeWallRecovery = 1.2f;
    [SerializeField, Min(1)] private int chargeDamage = 20;

    [Header("Stone fan")]
    [SerializeField] private StoneGolemShard shardPrefab;
    [SerializeField] private Material fanWarningMaterial;
    [SerializeField, Range(1f, 180f)] private float fanAngle = 105f;
    [SerializeField, Min(2)] private int shardCount = 5;
    [SerializeField, Min(0.01f)] private float shardRange = 9f;
    [SerializeField, Min(0.01f)] private float shardSpeed = 6f;
    [SerializeField, Min(0.01f)] private float shardDiameter = 0.4f;
    [SerializeField, Min(0.01f)] private float fanWarning = 0.9f;
    [SerializeField, Min(0f)] private float fanCooldown = 7f;
    [SerializeField, Min(0f)] private float fanRecovery = 0.6f;
    [SerializeField, Min(1)] private int shardDamage = 10;

    [Header("Rockfall")]
    [SerializeField] private StoneGolemRockfall rockfallPrefab;
    [SerializeField, Min(0.01f)] private float rockfallRadius = 1.8f;
    [SerializeField, Min(0.01f)] private float rockfallWarning = 0.9f;
    [SerializeField, Min(0f)] private float rockfallCooldown = 12f;
    [SerializeField, Min(0f)] private float rockfallRecovery = 0.7f;
    [SerializeField, Min(1)] private int rockfallDamage = 20;

    [Header("Impact camera shake (amplitude in world units)")]
    [SerializeField, Min(0f)] private float leapShakeAmplitude = .07f;
    [SerializeField, Min(0f)] private float leapShakeDuration = .15f;
    [SerializeField, Min(0f)] private float smashShakeAmplitude = .06f;
    [SerializeField, Min(0f)] private float smashShakeDuration = .12f;
    [SerializeField, Min(0f)] private float wallShakeAmplitude = .09f;
    [SerializeField, Min(0f)] private float wallShakeDuration = .18f;

    private int playtestShockwave, playtestEnhancedSmash, playtestEnhancedFan, playtestEnhancedRockfall;
    public void ConfigureSpawnDamage(int[] values)
    {
        if (player != null || values == null || values.Length != 9 || System.Array.Exists(values, n => n < 1))
            throw new System.InvalidOperationException("Configure golem damage before binding combat");
        leapDamage=values[0]; smashDamage=values[1]; chargeDamage=values[2]; shardDamage=values[3]; rockfallDamage=values[4];
        playtestShockwave=values[5]; playtestEnhancedSmash=values[6]; playtestEnhancedFan=values[7]; playtestEnhancedRockfall=values[8];
    }
    private int PatternDamage(Pattern pattern, bool secondary = false)
    {
        if(pattern==Pattern.LeapSlam) return enhanced && secondary && playtestShockwave>0 ? playtestShockwave : leapDamage;
        if(pattern==Pattern.GroundSmash) return enhanced && playtestEnhancedSmash>0 ? playtestEnhancedSmash : smashDamage;
        if(pattern==Pattern.StoneFan) return enhanced && playtestEnhancedFan>0 ? playtestEnhancedFan : shardDamage;
        if(pattern==Pattern.Rockfall) return enhanced && playtestEnhancedRockfall>0 ? playtestEnhancedRockfall : rockfallDamage;
        return chargeDamage;
    }

    private Rigidbody2D rb;
    private CircleCollider2D hitbox;
    private EnemyHealth health;
    private PlayerHealth player;
    private Collider2D playerCollider;
    private DungeonRoom room;
    private Vector3 bodyRestPosition;
    private Vector2 attackCenter;
    private Vector2 jumpStart;
    private float timer;
    private float nextLeapTime;
    private float nextSmashTime;
    private float nextChargeTime;
    private float nextFanTime, nextRockfallTime;
    private Vector2 fanOrigin, fanDirection;
    private GameObject effectsRoot;
    private StoneGolemTelegraph telegraph;
    private readonly List<StoneGolemShard> shards = new List<StoneGolemShard>();
    private readonly List<StoneGolemRockfall> rocks = new List<StoneGolemRockfall>();
    private Vector2 chargeStart;
    private Vector2 chargeDirection;
    private float chargeDistance;
    private bool chargeEndsAtWall;
    private float recoveryDuration;
    private bool ignoringPlayer;
    private bool previousIgnorePlayer;
    private bool targetWasDead;
    private bool damageApplied;

    public BossState State { get; private set; }
    public Pattern ActivePattern { get; private set; }
    public Pattern LastPattern { get; private set; }
    // Presentation reads the existing physics clock; it never advances combat.
    public float VisualStateElapsed => timer;
    public float VisualStateDuration => State == BossState.Recovery ? recoveryDuration :
        State == BossState.Jump ? leapDuration : State == BossState.Charge ? chargeMaxDuration :
        State == BossState.Rockfall ? rockfallWarning :
        comboFollowup ? comboFollowupDelay : ActivePattern == Pattern.StoneFan ? fanWarning :
        ActivePattern == Pattern.RockCharge ? chargeWarning :
        ActivePattern == Pattern.LeapSlam ? leapWarning : smashWarning;
    public PlayerHealth Target => player;
    public Vector2 AttackCenter => attackCenter;

    private float BodyRadius => hitbox.radius * Mathf.Max(
        Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<CircleCollider2D>();
        health = GetComponent<EnemyHealth>();
        if (body != null) bodyRestPosition = body.localPosition;
        HideWarning();
        // Defensive guard against accidentally attaching both AIs in the Inspector.
        StoneMonsterAI ordinaryAI = GetComponent<StoneMonsterAI>();
        if (ordinaryAI != null) ordinaryAI.enabled = false;
        if (body == null || attackWarning == null || attackWarning.sprite == null ||
            wallLayer.value == 0 || hitbox.offset != Vector2.zero)
        {
            Debug.LogError("StoneGolemBossAI: assign Body, circular Warning, Wall mask and a centered circle collider.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        health.Died += OnBossDied;
        health.HealthChanged += OnBossHealthChanged;
        if (player != null) player.Died += OnPlayerDied;
        CancelAttack();
    }

    public void SetTarget(PlayerHealth target, DungeonRoom owner)
    {
        AbortPresentation();
        CancelAttack(); // Restore the old collision pair before replacing the target.
        if (player != null) player.Died -= OnPlayerDied;
        player = target;
        room = owner;
        floorBounds.Clear();
        playerCollider = player != null ? player.GetComponent<Collider2D>() : null;
        targetWasDead = player != null && player.IsDead;
        if (isActiveAndEnabled && player != null) player.Died += OnPlayerDied;
    }

    private void FixedUpdate()
    {
        if (Time.timeScale == 0f) return;
        rb.linearVelocity = Vector2.zero;
        if (health.IsDead || player == null || playerCollider == null || room == null)
        {
            CancelAttack();
            return;
        }
        if (player.IsDead)
        {
            if (!targetWasDead) CancelAttack();
            targetWasDead = true;
            return;
        }
        if (targetWasDead)
        {
            targetWasDead = false;
            CancelAttack(); // A full new wait/telegraph after revival; no suspended hit.
        }

        ReservePhaseTransition();
        if (IsPresenting) { if (!presentationDriven) EndPresentation(); return; }
        if (!introCompleted) { BeginPresentation(false); return; }
        if (transitionPending && ActivePattern == Pattern.None) { BeginPresentation(true); return; }
        timer += Time.fixedDeltaTime;
        switch (State)
        {
            case BossState.Waiting:
                if (timer + .00001f >= commonWait) SetState(BossState.Chase);
                break;
            case BossState.Chase:
                if (!TrySelectPattern()) Chase();
                break;
            case BossState.Warning:
                TickAim();
                float warningDuration = comboFollowup ? comboFollowupDelay : ActivePattern == Pattern.StoneFan ? fanWarning :
                    ActivePattern == Pattern.RockCharge ? chargeWarning :
                    (ActivePattern == Pattern.LeapSlam ? leapWarning : smashWarning);
                if (telegraph != null)
                    telegraph.SetProgress(ActivePattern == Pattern.LeapSlam
                        ? Mathf.Min(timer, leapWarning) / (leapWarning + leapDuration)
                        : timer / warningDuration);
                if (timer + .00001f >= warningDuration)
                {
                    if (ActivePattern == Pattern.LeapSlam)
                    {
                        if (!PathClear(rb.position, attackCenter)) { CancelAttack(); break; }
                        jumpStart = rb.position;
                        IgnorePlayer();
                        SetState(BossState.Jump);
                    }
                    else if (ActivePattern == Pattern.RockCharge)
                    {
                        CompleteWarning();
                        HideWarning();
                        IgnorePlayer(); // Sweeps handle contact without physically pushing the player.
                        SetState(BossState.Charge);
                    }
                    else if (ActivePattern == Pattern.StoneFan) FireShards();
                    else ResolveAttack();
                }
                break;
            case BossState.Jump:
                TickJump();
                break;
            case BossState.Charge:
                TickCharge();
                break;
            case BossState.Recovery:
                if (ActivePattern == Pattern.StoneFan)
                {
                    TickShards();
                    if (player.IsDead || !isActiveAndEnabled) return;
                }
                if (timer + .00001f >= recoveryDuration && shards.Count == 0)
                {
                    FinishPattern();
                }
                break;
            case BossState.FanFollowup:
                TickShards();
                if (player.IsDead || !isActiveAndEnabled) return;
                fanOrigin = rb.position;
                fanDirection = ((Vector2)player.transform.position - fanOrigin).normalized;
                if (telegraph != null)
                {
                    telegraph.transform.SetPositionAndRotation(fanOrigin,
                        Quaternion.Euler(0, 0, Mathf.Atan2(fanDirection.y, fanDirection.x) * Mathf.Rad2Deg));
                    telegraph.SetProgress(timer / fanWarning);
                }
                if (timer + .00001f >= fanWarning) FireShards();
                break;
            case BossState.ComboAInterval:
                if (timer + .00001f >= comboFollowupDelay)
                {
                    comboNext = Pattern.None;
                    StartPattern(Pattern.GroundSmash, false); // Full normal warning and cooldown start now.
                }
                break;
            case BossState.RockfallGap:
                if (timer + .00001f >= rockfallRoundGap) BeginRockfallRound();
                break;
            case BossState.SecondaryWarning:
                TickSecondaryWarning();
                break;
            case BossState.Rockfall:
                TickRockfalls();
                break;
        }
    }

    private void CreateEffectsRoot()
    {
        ClearOwnedEffects();
        effectsRoot = new GameObject(name + " Attacks");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(effectsRoot, gameObject.scene);
        // Intentionally unparented with unit scale: all dimensions are world units.
    }

    private void ShowFanWarning()
    {
        telegraph = StoneGolemTelegraph.Create(effectsRoot.transform, fanWarningMaterial,
            fanOrigin, fanDirection, shardRange, fanAngle, 0f, false,
            new Color(.7f, .45f, 1f), attackWarning.sortingLayerID, attackWarning.sortingOrder);
    }

    private void FireShards()
    {
        CompleteWarning();
        HideWarning();
        float heading = Mathf.Atan2(fanDirection.y, fanDirection.x) * Mathf.Rad2Deg;
        for (int i = 0; i < shardCount; i++)
        {
            float angle = (heading + Mathf.Lerp(-fanAngle * 0.5f, fanAngle * 0.5f,
                i / (float)Mathf.Max(1, shardCount - 1))) * Mathf.Deg2Rad;
            StoneGolemShard shard = Instantiate(shardPrefab, effectsRoot.transform);
            shard.Initialize(player, wallLayer, fanOrigin, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)),
                shardSpeed, shardRange, shardDiameter, PatternDamage(Pattern.StoneFan));
            shards.Add(shard);
        }
        recoveryDuration = fanRecovery;
        if (enhanced && !secondaryStarted)
        {
            secondaryStarted = true;
            // Start the second full warning immediately; keep the first volley alive.
            fanOrigin = rb.position;
            fanDirection = ((Vector2)player.transform.position - fanOrigin).normalized;
            ShowFanWarning();
            SetState(BossState.FanFollowup);
        }
        else SetState(BossState.Recovery); // Both volleys remain owned and tick together.
    }

    private void TickShards()
    {
        for (int i = shards.Count - 1; i >= 0; i--)
        {
            bool alive = shards[i] != null && shards[i].Tick(Time.fixedDeltaTime);
            // TakeDamage can synchronously cancel this entire pattern and clear the lists.
            if (player.IsDead || !isActiveAndEnabled) return;
            if (!alive) shards.RemoveAt(i);
        }
    }

    private void ClearOwnedEffects()
    {
        if (effectsRoot != null)
        {
            effectsRoot.SetActive(false);
            Destroy(effectsRoot);
            effectsRoot = null;
        }
        telegraph = null;
        shards.Clear();
        rocks.Clear();
    }

    // The telegraph and movement use the same swept circle, including rounded ends.
    // Boundaries are intersected along the ray, never clamped onto a different direction.
    private float ChargeWallDistance(Vector2 start, Vector2 direction, float limit, out bool blocked)
    {
        float available = limit;
        Vector2 min = room.WorldMin + Vector2.one * (ChargeRadius + wallSkin);
        Vector2 max = room.WorldMax - Vector2.one * (ChargeRadius + wallSkin);
        if (start.x < min.x || start.x > max.x || start.y < min.y || start.y > max.y)
        {
            blocked = true;
            return 0f;
        }
        if (direction.x > 0.0001f) available = Mathf.Min(available, (max.x - start.x) / direction.x);
        else if (direction.x < -0.0001f) available = Mathf.Min(available, (min.x - start.x) / direction.x);
        if (direction.y > 0.0001f) available = Mathf.Min(available, (max.y - start.y) / direction.y);
        else if (direction.y < -0.0001f) available = Mathf.Min(available, (min.y - start.y) / direction.y);
        RaycastHit2D wall = Physics2D.CircleCast(start, ChargeRadius, direction, limit + wallSkin, wallLayer);
        if (wall.collider != null) available = Mathf.Min(available, Mathf.Max(0f, wall.distance - wallSkin));
        blocked = available < limit;
        return Mathf.Max(0f, available);
    }

    private void TickCharge()
    {
        float remaining = Mathf.Max(0f, chargeDistance - Vector2.Dot(rb.position - chargeStart, chargeDirection));
        float stepTime = Mathf.Min(Time.fixedDeltaTime, Mathf.Max(0f, chargeMaxDuration - (timer - Time.fixedDeltaTime)));
        float step = Mathf.Min(chargeSpeed * stepTime, remaining);
        bool wallBlocked;
        float safeStep = ChargeWallDistance(rb.position, chargeDirection, step, out wallBlocked);

        float playerDistance = float.PositiveInfinity;
        if (Vector2.Distance(rb.position, playerCollider.ClosestPoint(rb.position)) <= ChargeRadius)
            playerDistance = 0f;
        else
        {
            // Query the full interval so a fast charge cannot skip a thin player collider.
            foreach (RaycastHit2D hit in Physics2D.CircleCastAll(rb.position, ChargeRadius,
                chargeDirection, step, 1 << playerCollider.gameObject.layer))
                if (hit.collider == playerCollider) playerDistance = Mathf.Min(playerDistance, hit.distance);
        }

        bool hitPlayer = playerDistance <= safeStep;
        float travel = hitPlayer ? Mathf.Max(0f, playerDistance - wallSkin) : safeStep;
        // Swept movement is applied directly: no pending MovePosition velocity can push
        // the player after collision or survive the transition into recovery.
        rb.position += chargeDirection * travel;
        rb.linearVelocity = Vector2.zero;
        if (hitPlayer)
        {
            bool applyDamage = !damageApplied;
            FinishCharge(false);
            if (applyDamage && !player.IsDead && !health.IsDead)
                player.TakeDamage(chargeDamage);
        }
        else if (wallBlocked || remaining - travel <= 0.001f || timer >= chargeMaxDuration)
            FinishCharge(wallBlocked || (chargeEndsAtWall && remaining - travel <= 0.001f));
        TryBeginComboFollowup();
    }

    private void FinishCharge(bool hitWall)
    {
        if (damageApplied) return;
        damageApplied = true;
        if (hitWall)
        {
            comboNext = Pattern.None;
            // Bounds can also stop a charge. Only a physical wall contact shakes.
            RaycastHit2D contact = Physics2D.CircleCast(rb.position, ChargeRadius,
                chargeDirection, wallSkin * 2f + .01f, wallLayer);
            if (contact.collider != null)
                ImpactShake(contact.point, wallShakeAmplitude, wallShakeDuration);
        }
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        HideWarning();
        RestorePlayerCollision();
        recoveryDuration = hitWall ? chargeWallRecovery : chargeRecovery;
        SetState(BossState.Recovery);
    }

    private void UpdateChargeWarning()
    {
        CreateEffectsRoot();
        telegraph = StoneGolemTelegraph.Create(effectsRoot.transform, fanWarningMaterial,
            chargeStart, chargeDirection, ChargeRadius, 360f, chargeDistance, true,
            new Color(1f, .4f, .05f), attackWarning.sortingLayerID, attackWarning.sortingOrder);
    }

    private bool TryGetLanding(out Vector2 landing)
    {
        Vector2 start = rb.position;
        Vector2 delta = (Vector2)player.transform.position - start;
        float distance = Mathf.Min(delta.magnitude, maxLeapDistance);
        Vector2 direction = delta.normalized;
        RaycastHit2D wall = Physics2D.CircleCast(start, BodyRadius + wallSkin,
            direction, distance, wallLayer);
        if (wall.collider != null) distance = Mathf.Min(distance, Mathf.Max(0f, wall.distance - wallSkin));
        landing = ClampToRoom(start + direction * distance);
        // A blocked/zero-length leap is not a selectable attack.
        return Vector2.Distance(start, landing) >= 0.25f && PathClear(start, landing);
    }

    private Vector2 ClampToRoom(Vector2 point)
    {
        float margin = BodyRadius + wallSkin;
        Vector2 min = room.WorldMin + Vector2.one * margin;
        Vector2 max = room.WorldMax - Vector2.one * margin;
        return new Vector2(Mathf.Clamp(point.x, min.x, max.x), Mathf.Clamp(point.y, min.y, max.y));
    }

    private bool PathClear(Vector2 from, Vector2 to)
    {
        if (room == null || Vector2.Distance(ClampToRoom(to), to) > 0.001f) return false;
        float radius = BodyRadius + wallSkin;
        if (Physics2D.OverlapCircle(to, radius, wallLayer) != null) return false;
        Vector2 delta = to - from;
        return delta.sqrMagnitude < 0.000001f ||
            Physics2D.CircleCast(from, radius, delta.normalized, delta.magnitude, wallLayer).collider == null;
    }

    private void Chase()
    {
        Vector2 delta = (Vector2)player.transform.position - rb.position;
        // ClosestPoint also protects against a larger player collider.
        float gap = Vector2.Distance(rb.position, playerCollider.ClosestPoint(rb.position)) - BodyRadius;
        float travel = Mathf.Min(moveSpeed * Time.fixedDeltaTime,
            Mathf.Min(delta.magnitude - stopDistance, gap - 0.15f));
        if (travel <= 0f) return;
        Vector2 next = rb.position + delta.normalized * travel;
        if (PathClear(rb.position, next)) rb.MovePosition(next);
    }

    private void TickJump()
    {
        // Resolve only after the preceding physics step reached the displayed center.
        if (timer >= leapDuration && Vector2.Distance(rb.position, attackCenter) < 0.01f)
        {
            ResolveAttack();
            return;
        }
        // MovePosition lands on the following physics step. Do not complete early
        // while the actual landing check above is still pending.
        if (telegraph != null) telegraph.SetProgress(Mathf.Min(.999f,
            (leapWarning + timer) / (leapWarning + leapDuration)));
        Vector2 next = Vector2.Lerp(jumpStart, attackCenter, Mathf.Clamp01(timer / leapDuration));
        if (!PathClear(rb.position, next)) { CancelAttack(); return; }
        rb.MovePosition(next);
        body.localPosition = bodyRestPosition + Vector3.up *
            (Mathf.Sin(Mathf.Clamp01(timer / leapDuration) * Mathf.PI) * jumpHeight);
        if (timer > leapDuration + 0.2f) CancelAttack(); // Never hit at a different landing point.
    }

    private void ResolveAttack()
    {
        if (damageApplied) return;
        damageApplied = true;
        CompleteWarning();
        ImpactShake(attackCenter, ActivePattern == Pattern.LeapSlam ? leapShakeAmplitude : smashShakeAmplitude,
            ActivePattern == Pattern.LeapSlam ? leapShakeDuration : smashShakeDuration);
        HideWarning();
        body.localPosition = bodyRestPosition;
        RestorePlayerCollision();
        recoveryDuration = ActivePattern == Pattern.LeapSlam
            ? (enhanced && !secondaryStarted ? leapShockwaveWait : leapRecovery)
            : smashRecovery;
        SetState(BossState.Recovery);
        float radius = ActivePattern == Pattern.LeapSlam ? leapRadius : smashRadius;
        int damage = PatternDamage(ActivePattern);
        Vector2 point = playerCollider.ClosestPoint(attackCenter);
        if (!health.IsDead && !player.IsDead && Vector2.Distance(attackCenter, point) <= radius &&
            Physics2D.Linecast(attackCenter, point, wallLayer).collider == null)
            player.TakeDamage(damage);
        if (player.IsDead || !isActiveAndEnabled) return;
        if (TryBeginComboFollowup()) return;
        if (enhanced && ActivePattern == Pattern.GroundSmash && !secondaryStarted)
        {
            secondaryStarted = true;
            ShowWarning(SecondaryRadius, new Color(1f, .2f, .05f, .4f));
            SetState(BossState.SecondaryWarning);
        }
    }

    private void ShowWarning(float radius, Color color)
    {
        CreateEffectsRoot();
        telegraph = StoneGolemTelegraph.Create(effectsRoot.transform, fanWarningMaterial,
            attackCenter, Vector2.right, radius, 360f, 0f, false, color,
            attackWarning.sortingLayerID, attackWarning.sortingOrder);
    }

    private static void ImpactShake(Vector2 center, float amplitude, float duration)
    {
        if (Camera.main != null && Camera.main.TryGetComponent<CameraFollow>(out var camera))
            camera.ShakeAt(center, amplitude, duration);
    }

    private void CompleteWarning()
    {
        if (telegraph != null) telegraph.SetProgress(1f);
    }

    private void HideWarning()
    {
        if (telegraph != null) telegraph.gameObject.SetActive(false);
        if (attackWarning != null) attackWarning.gameObject.SetActive(false);
        if (chargePath != null) chargePath.gameObject.SetActive(false);
        if (chargeStartCap != null) chargeStartCap.gameObject.SetActive(false);
        if (chargeEndCap != null) chargeEndCap.gameObject.SetActive(false);
    }

    private void IgnorePlayer()
    {
        if (ignoringPlayer || playerCollider == null) return;
        previousIgnorePlayer = Physics2D.GetIgnoreCollision(hitbox, playerCollider);
        Physics2D.IgnoreCollision(hitbox, playerCollider, true);
        ignoringPlayer = true;
    }

    private void RestorePlayerCollision()
    {
        if (!ignoringPlayer) return;
        if (hitbox != null && playerCollider != null)
            Physics2D.IgnoreCollision(hitbox, playerCollider, previousIgnorePlayer);
        ignoringPlayer = false;
    }

    private void SetState(BossState state) { State = state; timer = 0f; }

    private void CancelAttack()
    {
        AbortPresentation();
        comboNext = Pattern.None;
        enhanced = secondaryStarted = comboFollowup = aimLocked = false;
        ClearOwnedEffects();
        HideWarning();
        RestorePlayerCollision();
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.MovePosition(rb.position); }
        if (body != null) body.localPosition = bodyRestPosition;
        damageApplied = true;
        ActivePattern = Pattern.None;
        SetState(BossState.Waiting);
    }

    private void OnPlayerDied(PlayerHealth _) { targetWasDead = true; AbortPresentation(); CancelAttack(); }
    private void OnBossDied(EnemyHealth _) { enabled = false; }

    private void OnDisable()
    {
        AbortPresentation();
        CancelAttack();
        if (health != null) { health.Died -= OnBossDied; health.HealthChanged -= OnBossHealthChanged; }
        if (player != null) player.Died -= OnPlayerDied;
    }
}
