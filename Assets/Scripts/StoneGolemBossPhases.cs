using System;
using System.Collections.Generic;
using UnityEngine;

public partial class StoneGolemBossAI
{
    [Header("Phase 2 / initial test values (not final balance)")]
    [SerializeField, Range(0f, 1f)] private float comboChance = .30f;
    [SerializeField, Min(.01f)] private float secondaryWarning = .8f;
    [SerializeField, Min(1f)] private float secondaryRadiusMultiplier = 1.5f;
    [SerializeField, Range(0f, 1f)] private float rockfallPlayerWeight = .35f;
    [SerializeField, Min(.01f)] private float rockfallPlayerSpread = 3f;

    private bool introCompleted, transitionPending, enhanced, secondaryStarted;
    private bool transforming, presentationDriven;
    private Pattern comboNext;
    private int remainingRockfallRounds;
    private readonly List<Bounds> floorBounds = new List<Bounds>();
    private readonly HashSet<PlayerHealth> roundHits = new HashSet<PlayerHealth>();
    private float CombatTime => player != null ? player.CombatTime : Time.time;
    public int Phase { get; private set; } = 1;
    public bool TransitionPending => transitionPending;
    public bool IsPresenting => State == BossState.Presentation;
    public bool IsTransforming => IsPresenting && transforming;
    public bool IntroCompleted => introCompleted;
    public Pattern QueuedPattern => comboNext;
    public bool IsEnhanced => enhanced;

    // An art/camera driver subscribes before room binding and calls EndPresentation
    // on completion. Without a driver, the next physics tick completes the state.
    public event Action<StoneGolemBossAI, bool> PresentationStarted;
    public event Action<StoneGolemBossAI, bool> PresentationEnded;

    private void OnBossHealthChanged(EnemyHealth _) => ReservePhaseTransition();
    private void ReservePhaseTransition()
    {
        if (!health.IsDead && Phase == 1 && health.CurrentHealth <= health.MaxHealth * .5f)
            transitionPending = true;
    }

    private void BeginPresentation(bool transformation)
    {
        if (IsPresenting || health.IsDead || player == null || player.IsDead) return;
        CancelAttack();
        transforming = transformation;
        presentationDriven = PresentationStarted != null;
        SetState(BossState.Presentation);
        player.SetBossPresentation(true);
        PresentationStarted?.Invoke(this, transformation);
    }

    public void SkipPresentation() => EndPresentation();
    public void EndPresentation()
    {
        if (!IsPresenting) return;
        bool wasTransformation = transforming;
        if (!health.IsDead)
        {
            if (wasTransformation) { Phase = 2; transitionPending = false; }
            else introCompleted = true;
        }
        AbortPresentation();
        CancelAttack();
        PresentationEnded?.Invoke(this, wasTransformation);
    }

    private void AbortPresentation()
    {
        if (!IsPresenting) return;
        if (player != null) player.SetBossPresentation(false);
        if (Camera.main != null && Camera.main.TryGetComponent<CameraFollow>(out var camera))
            camera.SnapToTarget();
        transforming = presentationDriven = false;
        SetState(BossState.Waiting);
    }

    private bool CanStart(Pattern pattern, bool respectLast = true)
    {
        if (respectLast && LastPattern == pattern) return false;
        Vector2 direction = ((Vector2)player.transform.position - rb.position).normalized;
        switch (pattern)
        {
            case Pattern.LeapSlam:
                return CombatTime >= nextLeapTime && TryGetLanding(out _);
            case Pattern.GroundSmash:
                return CombatTime >= nextSmashTime &&
                    Vector2.Distance(rb.position, player.transform.position) <= smashSelectionDistance &&
                    !Physics2D.Linecast(rb.position, playerCollider.ClosestPoint(rb.position), wallLayer);
            case Pattern.RockCharge:
                return CombatTime >= nextChargeTime && chargePath != null && chargeStartCap != null &&
                    chargeEndCap != null && direction.sqrMagnitude > .5f &&
                    ChargeWallDistance(rb.position, direction, Mathf.Min(chargeMaxDistance,
                        chargeSpeed * chargeMaxDuration), out _) >= .05f;
            case Pattern.StoneFan:
                float distance = Vector2.Distance(rb.position, player.transform.position);
                return CombatTime >= nextFanTime && shardPrefab != null && fanWarningMaterial != null &&
                    distance > .01f && distance <= shardRange &&
                    !Physics2D.Linecast(rb.position, player.transform.position, wallLayer);
            case Pattern.Rockfall:
                // Room-wide placement has no target-distance/line-of-sight restriction.
                return CombatTime >= nextRockfallTime && rockfallPrefab != null && CacheFloor();
            default: return false;
        }
    }

    private bool TrySelectPattern()
    {
        if (transitionPending || IsPresenting) return false;
        if (Phase == 2 && UnityEngine.Random.value < comboChance)
        {
            bool a = CanStart(Pattern.LeapSlam) && CanStart(Pattern.GroundSmash);
            bool b = CanStart(Pattern.RockCharge) && CanStart(Pattern.StoneFan);
            if (a || b)
            {
                bool selectA = a && (!b || UnityEngine.Random.Range(0, 2) == 0);
                comboNext = selectA ? Pattern.GroundSmash : Pattern.StoneFan;
                StartPattern(selectA ? Pattern.LeapSlam : Pattern.RockCharge, false);
                return true;
            }
        }
        var available = new List<Pattern>(5);
        for (Pattern p = Pattern.LeapSlam; p <= Pattern.Rockfall; p++)
            if (CanStart(p)) available.Add(p);
        if (available.Count == 0) return false;
        StartPattern(available[UnityEngine.Random.Range(0, available.Count)], Phase == 2);
        return true;
    }

    private void StartPattern(Pattern pattern, bool useEnhanced)
    {
        ActivePattern = LastPattern = pattern;
        enhanced = useEnhanced;
        secondaryStarted = damageApplied = false;
        Vector2 direction = ((Vector2)player.transform.position - rb.position).normalized;
        switch (pattern)
        {
            case Pattern.LeapSlam:
                TryGetLanding(out attackCenter);
                nextLeapTime = CombatTime + leapCooldown;
                ShowWarning(leapRadius, new Color(1f, .35f, .05f, .4f));
                break;
            case Pattern.GroundSmash:
                attackCenter = rb.position;
                nextSmashTime = CombatTime + smashCooldown;
                ShowWarning(smashRadius, new Color(1f, .1f, .15f, .35f));
                break;
            case Pattern.RockCharge:
                chargeStart = rb.position;
                chargeDirection = direction;
                chargeDistance = ChargeWallDistance(rb.position, direction,
                    Mathf.Min(chargeMaxDistance, chargeSpeed * chargeMaxDuration), out chargeEndsAtWall);
                attackCenter = chargeStart + direction * chargeDistance;
                nextChargeTime = CombatTime + chargeCooldown;
                UpdateChargeWarning();
                break;
            case Pattern.StoneFan:
                fanOrigin = rb.position;
                fanDirection = direction;
                nextFanTime = CombatTime + fanCooldown;
                CreateEffectsRoot();
                ShowFanWarning();
                break;
            case Pattern.Rockfall:
                nextRockfallTime = CombatTime + rockfallCooldown;
                float roll = UnityEngine.Random.value;
                remainingRockfallRounds = !enhanced ? 1 : roll < .20f ? 1 : roll < .65f ? 2 : 3;
                CreateEffectsRoot();
                BeginRockfallRound();
                return;
        }
        SetState(BossState.Warning);
    }

    private void FinishPattern()
    {
        if (enhanced && !secondaryStarted && (ActivePattern == Pattern.LeapSlam ||
            ActivePattern == Pattern.GroundSmash || ActivePattern == Pattern.StoneFan))
        {
            secondaryStarted = true;
            if (ActivePattern == Pattern.StoneFan)
            {
                fanOrigin = rb.position;
                fanDirection = ((Vector2)player.transform.position - fanOrigin).normalized;
                // Re-aim once at the second telegraph; never track during its warning.
                CreateEffectsRoot();
                ShowFanWarning();
            }
            else
            {
                attackCenter = rb.position;
                ShowWarning(SecondaryRadius, new Color(1f, .2f, .05f, .4f));
            }
            SetState(BossState.SecondaryWarning);
            return;
        }
        ClearOwnedEffects();
        Pattern queued = comboNext;
        comboNext = Pattern.None;
        // The reservation was checked at combo start. Do not add a new distance gate
        // between its steps or consume the second cooldown before its telegraph.
        if (queued != Pattern.None && !player.IsDead && !health.IsDead)
        {
            StartPattern(queued, false);
            return;
        }
        ActivePattern = Pattern.None;
        SetState(BossState.Waiting);
    }

    private float SecondaryRadius => (ActivePattern == Pattern.LeapSlam ? leapRadius : smashRadius) * secondaryRadiusMultiplier;
    private void TickSecondaryWarning()
    {
        if (telegraph != null) telegraph.SetProgress(timer / secondaryWarning);
        if (timer < secondaryWarning) return;
        if (ActivePattern == Pattern.StoneFan) { FireShards(); return; }
        CompleteWarning();
        HideWarning();
        recoveryDuration = ActivePattern == Pattern.LeapSlam ? leapRecovery : smashRecovery;
        SetState(BossState.Recovery);
        Vector2 point = playerCollider.ClosestPoint(attackCenter);
        if (Vector2.Distance(attackCenter, point) <= SecondaryRadius &&
            !Physics2D.Linecast(attackCenter, point, wallLayer))
            player.TakeDamage(ActivePattern == Pattern.LeapSlam ? leapDamage : smashDamage);
    }

    private bool CacheFloor()
    {
        if (floorBounds.Count > 0) return true;
        // BossRoom24x22 uses eight rectangular Floor sprites, including the entry.
        // Read their actual geometry instead of the canvas bounds or an assumed box.
        foreach (var sprite in room.GetComponentsInChildren<SpriteRenderer>())
            if (sprite.name == "Floor" && sprite.sprite != null) floorBounds.Add(sprite.bounds);
        return floorBounds.Count > 0;
    }

    private bool OnFloor(Vector2 point)
    {
        foreach (var bounds in floorBounds)
            if (point.x >= bounds.min.x && point.x <= bounds.max.x &&
                point.y >= bounds.min.y && point.y <= bounds.max.y)
                return Physics2D.OverlapPoint(point, wallLayer) == null;
        return false;
    }

    private Vector2 SampleRockfallCenter()
    {
        CacheFloor();
        // Rejection sampling over the union gives uniform world area, including
        // overlapping Floor sprites, rather than weighting by renderer count.
        for (int i = 0; i < 256; i++)
        {
            Vector2 point = i < 32 && UnityEngine.Random.value < rockfallPlayerWeight
                ? (Vector2)player.transform.position + UnityEngine.Random.insideUnitCircle * rockfallPlayerSpread
                : new Vector2(UnityEngine.Random.Range(room.WorldMin.x, room.WorldMax.x),
                    UnityEngine.Random.Range(room.WorldMin.y, room.WorldMax.y));
            if (OnFloor(point)) return point;
        }
        // Existing valid floor centers are a bounded fallback, never an inside-wall point.
        foreach (var floor in floorBounds) if (OnFloor(floor.center)) return floor.center;
        return rb.position;
    }

    private void BeginRockfallRound()
    {
        remainingRockfallRounds--;
        roundHits.Clear();
        SetState(BossState.Rockfall);
        int count = UnityEngine.Random.Range(7, 9);
        for (int i = 0; i < count; i++)
        {
            var rock = Instantiate(rockfallPrefab, effectsRoot.transform);
            rock.Initialize(player, wallLayer, SampleRockfallCenter(), rockfallRadius,
                rockfallWarning, 0f, rockfallDamage, roundHits);
            rocks.Add(rock);
        }
    }

    private void TickRockfalls()
    {
        for (int i = rocks.Count - 1; i >= 0; i--)
        {
            bool alive = rocks[i] != null && rocks[i].Tick(timer);
            if (player.IsDead || !isActiveAndEnabled) return;
            if (!alive) rocks.RemoveAt(i);
        }
        if (rocks.Count != 0) return;
        if (remainingRockfallRounds > 0) { BeginRockfallRound(); return; }
        recoveryDuration = rockfallRecovery;
        SetState(BossState.Recovery);
    }
}
