using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("회피")]
    [SerializeField] private float dodgeDistance = 2f;
    [SerializeField, Min(0.01f)] private float dodgeDuration = 0.25f;
    [SerializeField] private float dodgeCooldown = 1f;
    [SerializeField] private float dodgeInvincibleTime = 0.15f;
    [SerializeField] private Camera aimCamera;

    private Rigidbody2D rb;
    private PlayerHealth health;
    private PlayerAttack attack;

    private Vector2 moveInput;
    private Vector2 dodgeDirection;
    private Vector2 aimDirection = Vector2.right;

    private float dodgeTimeRemaining;
    private float nextDodgeTime;

    public bool IsDodging { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<PlayerHealth>();
        attack = GetComponent<PlayerAttack>();

        if (aimCamera == null)
            aimCamera = Camera.main;
    }

    private void Update()
    {
        if (Time.timeScale == 0f)
            return;

        moveInput = Vector2.zero;

        if (health != null && health.IsDead)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.wKey.isPressed) moveInput.y += 1f;
        if (keyboard.sKey.isPressed) moveInput.y -= 1f;
        if (keyboard.aKey.isPressed) moveInput.x -= 1f;
        if (keyboard.dKey.isPressed) moveInput.x += 1f;

        moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        UpdateAimDirection();

        if (keyboard.spaceKey.wasPressedThisFrame &&
            !IsDodging &&
            Time.time >= nextDodgeTime)
        {
            StartDodge();
        }
    }

    private void UpdateAimDirection()
    {
        if (aimCamera == null || Mouse.current == null)
            return;

        Vector2 screenPoint = Mouse.current.position.ReadValue();

        Vector3 worldPoint = aimCamera.ScreenToWorldPoint(
            new Vector3(
                screenPoint.x,
                screenPoint.y,
                transform.position.z - aimCamera.transform.position.z
            )
        );

        Vector2 direction = worldPoint - transform.position;

        if (direction.sqrMagnitude > 0.001f)
            aimDirection = direction.normalized;
    }

    private void StartDodge()
    {
        PlayerPotion potion = GetComponent<PlayerPotion>();

        if (potion != null && potion.IsUsing)
            return;
        dodgeDirection = moveInput.sqrMagnitude > 0.001f
            ? moveInput.normalized
            : aimDirection;

        IsDodging = true;
        dodgeTimeRemaining = dodgeDuration;
        nextDodgeTime = Time.time + dodgeCooldown;

        if (attack != null)
            attack.CancelAttack();

        if (health != null)
            health.GrantInvincibility(dodgeInvincibleTime);
    }

    private void FixedUpdate()
    {
        if (health != null && health.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (IsDodging)
        {
            if (dodgeTimeRemaining > 0f)
            {
                float stepTime = Mathf.Min(
                    Time.fixedDeltaTime,
                    dodgeTimeRemaining
                );

                float speed = dodgeDistance / dodgeDuration;

                rb.linearVelocity = dodgeDirection * speed
                    * (stepTime / Time.fixedDeltaTime);

                dodgeTimeRemaining -= stepTime;
                return;
            }

            IsDodging = false;
        }

        rb.linearVelocity = moveInput * moveSpeed;
    }

    private void OnDisable()
    {
        moveInput = Vector2.zero;
        IsDodging = false;
        dodgeTimeRemaining = 0f;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }
}