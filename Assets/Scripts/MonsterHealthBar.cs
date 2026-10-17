using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(EnemyHealth))]
public sealed class MonsterHealthBar : MonoBehaviour
{
    public enum DisplayType { Normal, Elite, Hidden }
    [SerializeField] private DisplayType displayType;
    [SerializeField] private Transform visual;
    [SerializeField] private SpriteRenderer background, damageTrail, currentFill, eliteBorder;
    [SerializeField] private Vector2 groundOffset = new Vector2(0f, -.63f);
    [SerializeField, Min(.01f)] private float width = 1f, height = .09f;
    [SerializeField, Min(1f)] private float eliteWidthMultiplier = 1.2f;
    [SerializeField, Min(0f)] private float hideAfter = 3f, trailDelay = .2f, trailDuration = .25f;
    private EnemyHealth health;
    private readonly HealthBarTrail gauge = new HealthBarTrail();
    private float sinceDamage;
    private bool started, revealed;

    private void Start() { started = true; Bind(); }
    private void OnEnable() { if (started) Bind(); }
    private void Bind()
    {
        health = GetComponent<EnemyHealth>();
        health.HealthChanged += Changed;
        health.Died += Died;
        gauge.Reset(HealthBarTrail.Fraction(health));
        sinceDamage = 0f;
        revealed = false;
        Render();
    }
    private void Changed(EnemyHealth source)
    {
        if (gauge.SetValue(HealthBarTrail.Fraction(source)))
        {
            revealed = true;
            sinceDamage = 0f;
        }
        Render();
    }
    private void Died(EnemyHealth source) { if (visual != null) visual.gameObject.SetActive(false); }
    private void LateUpdate()
    {
        if (health == null) return;
        if (Time.timeScale > 0f)
        {
            sinceDamage += Time.deltaTime;
            gauge.Tick(Time.deltaTime, trailDelay, trailDuration);
        }
        Render();
    }
    private void Render()
    {
        if (visual == null || health == null) return;
        bool elite = displayType == DisplayType.Elite;
        bool show = displayType != DisplayType.Hidden && !health.IsDead && health.CurrentHealth > 0
            && (elite || (revealed && sinceDamage < hideAfter));
        visual.gameObject.SetActive(show);
        // World-aligned and world-sized, independent of the jumping/hit-reacting body.
        visual.position = transform.position + (Vector3)groundOffset;
        visual.rotation = Quaternion.identity;
        Vector3 parentScale = visual.parent.lossyScale;
        visual.localScale = new Vector3(SafeInverse(parentScale.x), SafeInverse(parentScale.y), 1f);
        float w = width * (elite ? eliteWidthMultiplier : 1f);
        Size(background, w + .06f, height + .06f, 0f);
        Size(eliteBorder, w + .10f, height + .10f, 0f);
        eliteBorder.enabled = elite;
        Size(damageTrail, w * gauge.Trail, height, (gauge.Trail - 1f) * w * .5f);
        Size(currentFill, w * gauge.Current, height, (gauge.Current - 1f) * w * .5f);
    }
    private static float SafeInverse(float value) => Mathf.Abs(value) > .0001f ? 1f / value : 1f;
    private static void Size(SpriteRenderer sprite, float w, float h, float x)
    {
        sprite.transform.localPosition = new Vector3(x, 0f, 0f);
        var size = sprite.sprite.bounds.size;
        sprite.transform.localScale = new Vector3(w / size.x, h / size.y, 1f);
    }
    private void OnDisable()
    {
        if (health != null) { health.HealthChanged -= Changed; health.Died -= Died; }
        health = null;
        if (visual != null) visual.gameObject.SetActive(false);
    }
}
