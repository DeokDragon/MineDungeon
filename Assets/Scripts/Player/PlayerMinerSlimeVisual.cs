using UnityEngine;

// Presentation only. WeaponPivot remains the unchanged damage origin.
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerMovement), typeof(PlayerAttack), typeof(PlayerHealth))]
public sealed class PlayerMinerSlimeVisual : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body, pickaxe, socket;
    [SerializeField] private Transform weaponPivot, visualGrip;
    [SerializeField] private Sprite[] idleFrames, moveFrames;
    // Clockwise screen-space octants from the supplied FrameAnchors.json.
    [SerializeField] private Vector2[] idleAnchors, moveAnchors;
    [SerializeField] private float idlePeriod = 1f, movePeriod = .72f;
    [SerializeField] private float movementThreshold = .02f;
    private Rigidbody2D rb;
    private PlayerMovement movement;
    private PlayerAttack attack;
    private PlayerHealth health;
    private Vector2 previousPosition, displacement;
    private float actualSpeed;
    private double elapsed;
    private bool moving, attacking;
    private int frame;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        health = GetComponent<PlayerHealth>();
    }

    private void OnEnable()
    {
        previousPosition = rb.position;
        elapsed = 0; actualSpeed = 0; moving = attacking = false; frame = 0;
        if (body && idleFrames.Length > 0) body.sprite = idleFrames[0];
    }
    private void Start() { previousPosition = rb.position; }
    private void FixedUpdate()
    {
        displacement = rb.position - previousPosition;
        // Room teleports are not walking.
        actualSpeed = displacement.magnitude > 2f ? 0f : displacement.magnitude / Time.fixedDeltaTime;
        previousPosition = rb.position;
    }
    private void LateUpdate()
    {
        if (!body || !pickaxe || !socket || !visualGrip || Time.timeScale == 0 || health.CombatLocked) return;
        if (!health.IsDead)
        {
            bool nextAttack = attack.IsAttacking;
            bool nextMove = movement.isActiveAndEnabled && actualSpeed > movementThreshold && !nextAttack;
            if (nextMove != moving || nextAttack != attacking) elapsed = 0;
            moving = nextMove; attacking = nextAttack;
            if (moving && Mathf.Abs(displacement.x) > .0001f) body.flipX = displacement.x < 0;
            var frames = moving ? moveFrames : idleFrames;
            if (frames.Length == 0) return;
            double period = System.Math.Max(.01, moving ? movePeriod : idlePeriod);
            elapsed = (elapsed + Time.deltaTime) % period;
            frame = attacking ? 0 : Mathf.Min(frames.Length - 1, (int)(elapsed / period * frames.Length));
            body.sprite = frames[frame];
        }
        UpdateAttachment();
    }
    private void UpdateAttachment()
    {
        var anchors = moving ? moveAnchors : idleAnchors;
        if (anchors == null || anchors.Length < (frame + 1) * 8) return;
        float angle = Mathf.Repeat(body.flipX ? 180f + attack.VisualAimAngle : -attack.VisualAimAngle, 360f) / 45f;
        int octant = Mathf.FloorToInt(angle);
        Vector2 anchor = Vector2.Lerp(anchors[frame * 8 + octant], anchors[frame * 8 + (octant + 1) % 8], angle - octant);
        if (body.flipX) anchor.x = 64f - anchor.x;
        Vector3 surface = body.transform.TransformPoint(new Vector3((anchor.x - 32f) / 64f, (32f - anchor.y) / 64f));
        float pixelWorld = body.transform.TransformVector(Vector3.right / 64f).magnitude;
        // Parent has no renderer: PlayerAttack continues thrusting only its child.
        visualGrip.position = surface + weaponPivot.right * (2f * pixelWorld);
        socket.transform.position = surface;
        socket.transform.rotation = weaponPivot.rotation;
        bool rear = weaponPivot.right.y > .02f;
        pickaxe.sortingOrder = body.sortingOrder + (rear ? -2 : 1);
        socket.sortingOrder = body.sortingOrder + (rear ? -1 : 2);
        // Do not shorten or mask the existing .4-world-unit third-hit thrust.
    }
}
