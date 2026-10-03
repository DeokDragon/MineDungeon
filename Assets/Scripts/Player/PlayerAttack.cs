using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
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

    private float aimAngle;
    private float attackAngle;
    private float attackStartTime;
    private float nextAttackTime;
    private bool isAttacking;
    public bool IsAttacking => isAttacking;
    private bool damageApplied;
    private bool waitForAttackRelease;

    private readonly HashSet<EnemyHealth> hitEnemies =
        new HashSet<EnemyHealth>();

    private void Awake()
    {
        if (aimCamera == null)
            aimCamera = Camera.main;
    }

    private void Update()
    {
        if (weaponPivot == null || aimCamera == null)
            return;

        if (Time.timeScale == 0f)
            return;
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

        bool blockNewAttack = waitForAttackRelease;

        if (waitForAttackRelease && !mouse.leftButton.isPressed)
            waitForAttackRelease = false;

        bool attackInput = !blockNewAttack &&
            (mouse.leftButton.wasPressedThisFrame
            || mouse.leftButton.isPressed);

        if (attackInput && !isAttacking && Time.time >= nextAttackTime)
        {
            isAttacking = true;
            damageApplied = false;
            attackStartTime = Time.time;
            nextAttackTime = Time.time + attackInterval;
            attackAngle = aimAngle;
        }

        float displayAngle = aimAngle;

        if (isAttacking)
        {
            float progress = Mathf.Clamp01(
                (Time.time - attackStartTime) / swingDuration
            );

            // 한 번 휘두를 때 피해 판정은 한 번만 실행
            if (!damageApplied && progress >= 0.5f)
            {
                damageApplied = true;
                ApplyDamage();
            }

            float swingOffset = Mathf.Lerp(
                -swingAngle * 0.5f,
                swingAngle * 0.5f,
                progress
            );

            displayAngle = attackAngle + swingOffset;

            if (progress >= 1f)
            {
                isAttacking = false;
                displayAngle = aimAngle;
            }
        }

        weaponPivot.rotation = Quaternion.Euler(0f, 0f, displayAngle);
        PlayerPotion potion = GetComponent<PlayerPotion>();

        if (potion != null && potion.IsUsing)
            return;
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
            attackRange,
            enemyLayer
        );

        hitEnemies.Clear();

        foreach (Collider2D target in targets)
        {
            EnemyHealth health = target.GetComponentInParent<EnemyHealth>();

            if (health == null || hitEnemies.Contains(health))
                continue;

            Vector2 hitPoint = target.ClosestPoint(origin);
            Vector2 toTarget = hitPoint - origin;

            // 전방 부채꼴 안에 있는지 확인
            if (toTarget.sqrMagnitude > 0.0001f &&
                Vector2.Angle(forward, toTarget) > hitAngle * 0.5f)
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
            health.TakeDamage(attackDamage);
        }
    }

    private void OnDisable()
    {
        isAttacking = false;
        nextAttackTime = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (weaponPivot == null)
            return;

        // Scene 화면에 최대 공격 거리 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(weaponPivot.position, attackRange);
    }
    public void CancelAttack()
    {
        isAttacking = false;
        damageApplied = true;

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
    }
}