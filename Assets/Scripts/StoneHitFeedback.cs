using UnityEngine;

[DisallowMultipleComponent]
public sealed class StoneHitFeedback : MonoBehaviour
{
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private StoneCombatBurst burstPrefab;
    [SerializeField] private bool boss;
    [SerializeField] private float displacement = .1f;
    [SerializeField] private float compression = .08f;
    [SerializeField] private float reactionDuration = .12f;
    [SerializeField] private float flashDuration = .08f;
    private Vector3 restPosition, restScale;
    private Color restColor;
    private Vector2 direction;
    private float age = float.PositiveInfinity;
    public bool IsBoss => boss;

    private void Awake()
    {
        restPosition = body.transform.localPosition;
        restScale = body.transform.localScale;
        restColor = body.color;
    }

    public void Hit(Vector2 point, Vector2 incoming)
    {
        age = 0f;
        direction = incoming.sqrMagnitude > .001f ? incoming.normalized : Vector2.right;
        Spawn(point, direction, false);
        ApplyReaction();
    }

    public void Break(Vector2 incoming)
    {
        Restore();
        Spawn(body.transform.position, incoming, true);
        if (Camera.main != null && Camera.main.TryGetComponent<CameraFollow>(out var camera))
            camera.Shake(boss ? .10f : .04f, boss ? .20f : .10f, boss);
    }

    private void Spawn(Vector2 point, Vector2 incoming, bool death)
    {
        if (burstPrefab == null) return;
        var burst = Instantiate(burstPrefab);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(burst.gameObject, gameObject.scene);
        burst.Initialize(GetComponentInParent<DungeonRoom>(), point, incoming, body, death, boss);
    }

    private void LateUpdate()
    {
        if (Time.timeScale == 0f) return;
        if (age >= Mathf.Max(reactionDuration, flashDuration)) return;
        age += Time.deltaTime;
        ApplyReaction();
    }

    private void ApplyReaction()
    {
        float strength = 1f - Mathf.Clamp01(age / reactionDuration);
        Vector3 localDirection = body.transform.parent.InverseTransformVector(direction);
        body.transform.localPosition = restPosition + localDirection * (displacement * strength);
        Vector3 axes = body.transform.parent.InverseTransformDirection(direction).normalized;
        body.transform.localScale = new Vector3(restScale.x * (1f - compression * strength * Mathf.Abs(axes.x)),
            restScale.y * (1f - compression * strength * Mathf.Abs(axes.y)), restScale.z);
        body.color = age < flashDuration ? new Color(1f, .97f, .85f, restColor.a) : restColor;
    }

    private void Restore()
    {
        age = float.PositiveInfinity;
        if (body == null) return;
        body.transform.localPosition = restPosition;
        body.transform.localScale = restScale;
        body.color = restColor;
    }

    private void OnDisable() { Restore(); }
}
