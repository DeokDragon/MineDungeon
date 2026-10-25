using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    private float CombatTime => GetComponent<PlayerHealth>().CombatTime;
    private bool CombatLocked => GetComponent<PlayerHealth>().CombatLocked;

    [SerializeField] private Transform weaponPivot;
    [SerializeField] private Camera aimCamera;

    [Header("공격 동작")]
    [SerializeField, Min(0.01f)] private float attackInterval = 0.6f;
    [SerializeField, Min(0.01f)] private float swingDuration = 0.25f;
    [SerializeField] private float swingAngle = 120f;

    [Header("피해 판정")]
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField, Range(0f, 360f)] private float hitAngle = 120f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private LayerMask wallLayer;

    // Initial combo test values; keep existing serialized swing fields and references.
    [SerializeField, Min(0.01f)] private float thrustDuration = 0.32f;
    [SerializeField, Min(0f)] private float thrustRange = 1.7f;
    [SerializeField, Range(0f, 360f)] private float thrustHitAngle = 90f;
    [SerializeField, Min(0f)] private float comboConnectTime = 0.85f;
    [SerializeField, Min(0f)] private float thrustVisualDistance = 0.4f;

    private int comboStep;
    private bool queuedAttack;
    private float comboExpiresAt;
    private Transform[] weaponVisuals;
    private Vector3[] weaponVisualPositions;
    public int ComboStep => comboStep;
    private float CurrentDuration => comboStep == 3 ? thrustDuration : swingDuration;
    private float CurrentHitAngle => comboStep == 3 ? thrustHitAngle : hitAngle;
    private double ComboDamageMultiplier => comboStep == 3 ? 1.3d : 1d;

    private float aimAngle;
    private float attackAngle;
    private float attackStartTime;
    private float nextAttackTime;
    private bool isAttacking;
    public bool IsAttacking => isAttacking;
    public float AttackInterval => attackInterval;
    public float EffectiveAttackRange
    {
        get
        {
            var run = DungeonRunManager.Instance;
            var growth = run != null ? run.GetComponent<PlayerProgression>() : null;
            return (comboStep == 3 ? thrustRange : attackRange) * (growth != null && growth.HasPhysical20Passive ? 1.15f : 1f);
        }
    }
    private int empoweredCharge;
    private bool empoweredReady, empoweredSwing;
    public void ResetEmpoweredAttack() { empoweredCharge = 0; empoweredReady = empoweredSwing = false; }
    private bool HasPhysical100
    {
        get
        {
            var run = DungeonRunManager.Instance;
            var growth = run != null ? run.GetComponent<PlayerProgression>() : null;
            return growth != null && growth.HasPhysical100Passive;
        }
    }
    // Called only after the existing input/action/interval checks succeed.
    private bool BeginAttack()
    {
        var health = GetComponent<PlayerHealth>();
        var run = DungeonRunManager.Instance;
        var skills = GetComponent<PlayerSkills>();
        var movement = GetComponent<PlayerMovement>();
        var potion = GetComponent<PlayerPotion>();
        if (!isActiveAndEnabled || weaponPivot == null || (CombatLocked || InventoryUI.BlockCombatInput) || Time.timeScale <= 0f ||
            (health != null && health.IsDead) || (run != null && (run.IsRunEnded || run.IsRestarting)) ||
            (skills != null && skills.IsCasting) || (movement != null && movement.IsDodging) ||
            (potion != null && potion.IsUsing) || isAttacking || CombatTime < nextAttackTime) return false;
        empoweredSwing = HasPhysical100 && empoweredReady;
        if (empoweredSwing) { empoweredReady = false; empoweredCharge = 0; }
        comboStep = CombatTime > comboExpiresAt ? 1 : comboStep % 3 + 1;
        isAttacking = true; damageApplied = false;
        attackStartTime = CombatTime; nextAttackTime = CombatTime + attackInterval; attackAngle = aimAngle;
        comboExpiresAt = attackStartTime + CurrentDuration + comboConnectTime;
        queuedAttack = false;
        return true;
    }
    private bool damageApplied;
    private bool waitForAttackRelease;
    private PlayerSkillAttackPower attackPower;
    public int EstimatedDamage => PlayerSkillAttackPower.CalculateDamage(attackDamage,
        attackPower != null ? attackPower.PhysicalAttack : 0f);

    private readonly HashSet<EnemyHealth> hitEnemies =
        new HashSet<EnemyHealth>();

    private readonly HashSet<EnemyHealth> shockwaveHits = new HashSet<EnemyHealth>();
    public const float ShockwaveRange = 2f, ShockwaveAngle = 90f;

    private void Awake()
    {
        attackPower = GetComponent<PlayerSkillAttackPower>();
        if (aimCamera == null)
            aimCamera = Camera.main;
        // Move sprites only: the pivot remains the following damage origin.
        var sprites = weaponPivot != null ? weaponPivot.GetComponentsInChildren<SpriteRenderer>(true) : new SpriteRenderer[0];
        var visuals = new List<Transform>();
        foreach (var sprite in sprites)
            if (sprite.transform != weaponPivot) visuals.Add(sprite.transform);
        weaponVisuals = visuals.ToArray();
        weaponVisualPositions = new Vector3[weaponVisuals.Length];
        for (int i = 0; i < weaponVisuals.Length; i++)
            weaponVisualPositions[i] = weaponVisuals[i].localPosition;
    }

    private void Update()
    {
        if (weaponPivot == null || aimCamera == null)
            return;

        if ((CombatLocked || InventoryUI.BlockCombatInput) || Time.timeScale == 0f)
            return;
        var skills = GetComponent<PlayerSkills>();
        if (skills != null && skills.IsCasting) return;
        var potion = GetComponent<PlayerPotion>();
        if (potion != null && potion.IsUsing) return;
        PlayerMovement movement = GetComponent<PlayerMovement>();

        if (movement != null && movement.IsDodging)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 screenPosition = mouse.position.ReadValue();

        Vector3 worldPosition = aimCamera.ScreenToWorldPoint(
            new Vector3(
                screenPosition.x,
                screenPosition.y,
                weaponPivot.position.z - aimCamera.transform.position.z
            )
        );

        Vector2 direction = worldPosition - weaponPivot.position;

        if (direction.sqrMagnitude > 0.001f)
        {
            aimAngle = Mathf.Atan2(direction.y, direction.x)
                * Mathf.Rad2Deg;
        }

        UpdateAttackMotion();
        ProcessAttackInput(mouse.leftButton.wasPressedThisFrame, mouse.leftButton.isPressed);
    }

    private void ProcessAttackInput(bool pressed, bool held)
    {
        // Expiry uses the scheduled motion end, not the time a click was queued.
        if (comboStep != 0 && !isAttacking && CombatTime > comboExpiresAt)
            ResetCombo();
        bool blockNewAttack = waitForAttackRelease;
        if (waitForAttackRelease && !held) waitForAttackRelease = false;
        if (blockNewAttack) return;
        if (pressed) queuedAttack = true;
        if ((queuedAttack || held) && !isAttacking)
        {
            // Recheck action restrictions and interval at actual execution.
            if (BeginAttack()) UpdateAttackMotion();
        }
    }

    private void UpdateAttackMotion()
    {
        float thrustOffset = 0f;
        float displayAngle = aimAngle;

        if (isAttacking)
        {
            float progress = Mathf.Clamp01(
                (CombatTime - attackStartTime) / CurrentDuration
            );

            // 한 번 휘두를 때 피해 판정은 한 번만 실행
            if (!damageApplied && progress >= 0.5f)
            {
                damageApplied = true;
                ApplyDamage();
            }

            // Positive Z is counterclockwise: step 1 decreases, step 2 increases.
            float swingOffset = comboStep == 3 ? 0f : Mathf.Lerp(
                swingAngle * 0.5f, -swingAngle * 0.5f, progress) * (comboStep == 1 ? 1f : -1f);
            if (comboStep == 3)
                thrustOffset = thrustVisualDistance * (1f - Mathf.Abs(progress * 2f - 1f));

            displayAngle = attackAngle + swingOffset;

            if (progress >= 1f)
            {
                isAttacking = false;
                displayAngle = aimAngle;
            }
        }

        weaponPivot.rotation = Quaternion.Euler(0f, 0f, displayAngle);
        SetThrustVisual(thrustOffset);
    }

    private void SetThrustVisual(float distance)
    {
        if (weaponVisuals == null) return;
        for (int i = 0; i < weaponVisuals.Length; i++)
            if (weaponVisuals[i] != null)
                weaponVisuals[i].localPosition = weaponVisualPositions[i] +
                    weaponVisuals[i].parent.InverseTransformVector(weaponPivot.right * distance);
    }

    public void ResetCombo()
    {
        comboStep = 0;
        comboExpiresAt = 0f;
        queuedAttack = false;
    }

    private void ApplyDamage()
    {
        Vector2 origin = weaponPivot.position;

        float radians = attackAngle * Mathf.Deg2Rad;
        Vector2 forward = new Vector2(
            Mathf.Cos(radians),
            Mathf.Sin(radians)
        );

        Collider2D[] targets = Physics2D.OverlapCircleAll(
            origin,
            EffectiveAttackRange,
            enemyLayer
        );

        hitEnemies.Clear();
        double rawDamage = PlayerSkillAttackPower.UnroundedDamage(attackDamage,
            attackPower != null ? attackPower.PhysicalAttack : 0f);
        bool enhanced = empoweredSwing;
        double multiplier = ComboDamageMultiplier * (enhanced ? 2d : 1d);
        int damage = PlayerSkillAttackPower.RoundDamage(rawDamage * multiplier);
        var run = DungeonRunManager.Instance;
        var growth = run != null ? run.GetComponent<PlayerProgression>() : null;
        bool shockwave = growth != null && growth.HasPhysical50Passive;
        Vector2 shockwaveOrigin = transform.position;
        bool accepted = false;

        foreach (Collider2D target in targets)
        {
            EnemyHealth health = target.GetComponentInParent<EnemyHealth>();

            if (health == null || hitEnemies.Contains(health))
                continue;

            Vector2 hitPoint = target.ClosestPoint(origin);
            Vector2 toTarget = hitPoint - origin;

            // 전방 부채꼴 안에 있는지 확인
            if (toTarget.sqrMagnitude > 0.0001f &&
                Vector2.Angle(forward, toTarget) > CurrentHitAngle * 0.5f)
            {
                continue;
            }

            // 플레이어와 적 사이에 벽이 있으면 피해 차단
            RaycastHit2D wallHit = Physics2D.Linecast(
                origin,
                hitPoint,
                wallLayer
            );

            if (wallHit.collider != null)
                continue;

            hitEnemies.Add(health);
            if (health.TryTakeDamage(damage, hitPoint, forward)) accepted = true;
        }
        if (accepted && !enhanced && HasPhysical100 && (GetComponent<PlayerHealth>() == null || !GetComponent<PlayerHealth>().IsDead))
        {
            empoweredCharge = Mathf.Min(3, empoweredCharge + 1);
            if (empoweredCharge == 3) empoweredReady = true;
        }
        if (accepted && shockwave)
            ApplyShockwave(shockwaveOrigin, forward, PlayerSkillAttackPower.RoundDamage(rawDamage * .30d * multiplier));
    }

    private void ApplyShockwave(Vector2 origin, Vector2 forward, int damage)
    {
        shockwaveHits.Clear();
        foreach (var target in Physics2D.OverlapCircleAll(origin, ShockwaveRange, enemyLayer))
        {
            var enemy = target.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead || shockwaveHits.Contains(enemy)) continue;
            Vector2 point = target.ClosestPoint(origin);
            Vector2 direction = point - origin;
            if (direction.sqrMagnitude > .0001f && Vector2.Angle(forward, direction) > ShockwaveAngle * .5f) continue;
            if (Physics2D.Linecast(origin, point, wallLayer).collider != null) continue;
            shockwaveHits.Add(enemy);
            enemy.TryTakeDamage(damage, point, forward);
        }
    }

    private void OnDisable()
    {
        CancelAttack();
        ResetCombo();
        nextAttackTime = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (weaponPivot == null)
            return;

        // Scene 화면에 최대 공격 거리 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(weaponPivot.position, EffectiveAttackRange);
    }
    public void CancelAttack()
    {
        isAttacking = false;
        damageApplied = true;
        SetThrustVisual(0f);

        if (weaponPivot != null)
        {
            weaponPivot.rotation = Quaternion.Euler(
                0f, 0f, aimAngle
            );
        }
    }
    public void RequireFreshAttackInput()
    {
        waitForAttackRelease = true;
        queuedAttack = false;
    }
}