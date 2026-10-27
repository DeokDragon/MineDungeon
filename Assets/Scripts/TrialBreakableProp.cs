using UnityEngine;

// Map01 experiment: a visual target, deliberately without physics or rewards.
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class TrialBreakableProp : MonoBehaviour
{
    [SerializeField] private Sprite debrisSprite;
    private SpriteRenderer display;
    private PlayerAttack attack;
    private Vector3 restScale;
    private float age;
    public bool IsBroken { get; private set; }

    private void Awake() { display = GetComponent<SpriteRenderer>(); restScale = transform.localScale; }
    private void OnEnable() { Bind(); }
    private void Start() { Bind(); }
    private void Bind()
    {
        if (attack) return;
        attack = FindFirstObjectByType<PlayerAttack>();
        if (attack) attack.BasicAttackResolved += CheckContact;
    }
    private void OnDisable()
    {
        if (attack) attack.BasicAttackResolved -= CheckContact;
        attack = null;
    }
    private void CheckContact(Vector2 origin, Vector2 forward, float range, float angle, LayerMask walls)
    {
        if (IsBroken || !isActiveAndEnabled || !display) return;
        Vector2 point = display.bounds.ClosestPoint(origin);
        Vector2 delta = point - origin;
        if (delta.sqrMagnitude > range * range ||
            (delta.sqrMagnitude > .0001f && Vector2.Angle(forward, delta) > angle * .5f) ||
            Physics2D.Linecast(origin, point, walls).collider) return;
        IsBroken = true;
        if (debrisSprite) display.sprite = debrisSprite;
        // Reuse delivered debris art; no drop, extra damage, particles or obstacle.
        display.color = new Color(1.18f, 1.12f, 1.02f, 1f);
    }
    private void Update()
    {
        if (!IsBroken || Time.timeScale <= 0) return;
        age += Time.deltaTime;
        float t = Mathf.Clamp01(age / .22f);
        transform.localScale = restScale * Mathf.Lerp(1.06f, .82f, t);
        display.color = Color.Lerp(new Color(1.18f, 1.12f, 1.02f, 1f), new Color(1, 1, 1, 0), t);
        if (t >= 1f) Destroy(gameObject);
    }
}
