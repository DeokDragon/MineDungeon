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
    [SerializeField] private Sprite[] attack1Frames, attack2Frames, attack3Frames;
    [SerializeField] private Sprite[] attack1Sockets, attack2Sockets, attack3Sockets, elasticFrames;
    [SerializeField] private Vector2[] attackSurfaceAnchors;
    [SerializeField] private SpriteRenderer elasticLobe;
    private Sprite defaultSocket;
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
        defaultSocket = socket ? socket.sprite : null;
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
            if (attacking)
            {
                float p = attack.VisualAttackProgress;
                var poses = attack.ComboStep == 1 ? attack1Frames : attack.ComboStep == 2 ? attack2Frames : attack3Frames;
                if (poses != null && poses.Length > 0)
                {
                    frame = AttackFrame(attack.ComboStep, p);
                    body.sprite = poses[Mathf.Min(frame, poses.Length - 1)];
                }
            }
        }
        UpdateAttachment();
    }
    private void UpdateAttachment()
    {
        var anchors = moving ? moveAnchors : idleAnchors;
        int anchorFrame = attacking ? 0 : frame;
        if (anchors == null || anchors.Length < (anchorFrame + 1) * 8) return;
        // The weapon stays on the original reference anchor; deformed surfaces only drive the lobe.
        Vector3 surface = AnchorWorld(attacking ? idleAnchors : anchors, anchorFrame * 8);
        float pixelWorld = body.transform.TransformVector(Vector3.right / 64f).magnitude;
        // Parent has no renderer: PlayerAttack continues thrusting only its child.
        visualGrip.position = surface + weaponPivot.right * (2f * pixelWorld);
        socket.transform.position = surface;
        socket.transform.rotation = weaponPivot.rotation;
        bool showAttack = attacking && !health.IsDead && attack.IsAttacking;
        if (showAttack)
        {
            var sockets = attack.ComboStep == 1 ? attack1Sockets : attack.ComboStep == 2 ? attack2Sockets : attack3Sockets;
            int key = attack.ComboStep == 3 ? Mathf.RoundToInt(attack.VisualAttackProgress * 16f) : frame;
            if (sockets != null && sockets.Length > 0) socket.sprite = sockets[Mathf.Min(key, sockets.Length - 1)];
            // Read the existing thrust result, never apply a second thrust to the weapon.
            socket.transform.position = pickaxe.transform.position - weaponPivot.right * (2f * pixelWorld);
        }
        else if (defaultSocket) socket.sprite = defaultSocket;
        if (elasticLobe)
        {
            elasticLobe.enabled = showAttack && attack.ComboStep == 3 && elasticFrames != null && elasticFrames.Length == 17;
            if (elasticLobe.enabled)
            {
                elasticLobe.sprite = elasticFrames[Mathf.RoundToInt(attack.VisualAttackProgress * 16f)];
                elasticLobe.transform.position = AnchorWorld(attackSurfaceAnchors, (12 + frame) * 8);
                elasticLobe.transform.rotation = weaponPivot.rotation;
            }
        }
        bool rear = weaponPivot.right.y > .02f;
        if (elasticLobe) elasticLobe.sortingOrder = body.sortingOrder + (rear ? -3 : 1);
        pickaxe.sortingOrder = body.sortingOrder + (rear ? -2 : 2);
        socket.sortingOrder = body.sortingOrder + (rear ? -1 : 3);
        // Existing .4-world-unit thrust is unchanged; supplied art spans the connection.
    }
    private int AttackFrame(int step, float progress)
    {
        if (step == 3) return Mathf.Min(7, (int)(progress * 8f));
        float t = progress * .25f;
        return t < .04f ? 0 : t < .08f ? 1 : t < .125f ? 2 : t < .17f ? 3 : t < .21f ? 4 : 5;
    }
    private Vector3 AnchorWorld(Vector2[] anchors, int offset)
    {
        float angle = Mathf.Repeat(body.flipX ? 180f + attack.VisualAimAngle : -attack.VisualAimAngle, 360f) / 45f;
        int sector = Mathf.FloorToInt(angle);
        int octant = sector % 8;
        Vector2 anchor = Vector2.Lerp(anchors[offset + octant], anchors[offset + (octant + 1) % 8], angle - sector);
        if (body.flipX) anchor.x = 64f - anchor.x;
        return body.transform.TransformPoint(new Vector3((anchor.x - 32f) / 64f, (32f - anchor.y) / 64f));
    }
    private void OnDisable()
    {
        if (elasticLobe) elasticLobe.enabled = false;
        if (socket && defaultSocket) socket.sprite = defaultSocket;
    }

}
