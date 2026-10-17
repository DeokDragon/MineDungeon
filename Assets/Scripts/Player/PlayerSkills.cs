using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth), typeof(Rigidbody2D), typeof(PlayerSkillAttackPower))]
public partial class PlayerSkills : MonoBehaviour
{
    [SerializeField] private SkillData eSkill, rSkill;
    [SerializeField] private PlayerSkillAttackPower attackPower;
    [SerializeField] private Camera aimCamera;
    [SerializeField] private LayerMask enemyLayer, wallLayer;
    [SerializeField] private PlayerSkillVisual effectPrefab;
    private PlayerHealth health;
    private PlayerMovement movement;
    private PlayerAttack attack;
    private PlayerPotion potion;
    private Rigidbody2D rb;
    private DungeonRunManager run;
    private readonly float[] readyAt = new float[2];
    private readonly List<Cast> live = new List<Cast>();
    private readonly List<PlayerSkillVisual> visuals = new List<PlayerSkillVisual>();
    private Cast blocking;
    public bool IsCasting => blocking != null;
    public bool OwnsMovement => IsCasting && blocking.data.kind != PlayerSkillKind.TealBeam;
    public float MovementMultiplier => IsCasting ? (blocking.data.kind == PlayerSkillKind.TealBeam ? .5f : 0f) : 1f;
    public int ActiveEffectCount => visuals.Count;
    public int EstimatedDamage(SkillData skill) => skill != null && attackPower != null ? skill.Damage(attackPower.PhysicalAttack, attackPower.MagicAttack) : 0;
    internal void ApplyInventoryLoadout(SkillData e, SkillData r) { eSkill = e; rSkill = r; }
    public void ResetForNewRun() { CancelAll(); readyAt[0] = readyAt[1] = 0f; }
    public SkillData Equipped(int slot) => slot == 0 ? eSkill : slot == 1 ? rSkill : null;
    public float CooldownRemaining(int slot) => slot >= 0 && slot < 2 ? Mathf.Max(0, readyAt[slot] - Time.time) : 0f;
    private sealed class Cast
    {
        public SkillData data;
        public int damage;
        public Vector2 origin, direction, target;
        public bool cancelled;
        public readonly HashSet<EnemyHealth> hit = new HashSet<EnemyHealth>();
    }
    private void Awake()
    {
        health = GetComponent<PlayerHealth>(); movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>(); potion = GetComponent<PlayerPotion>(); rb = GetComponent<Rigidbody2D>();
        if (attackPower == null) attackPower = GetComponent<PlayerSkillAttackPower>();
        if (aimCamera == null) aimCamera = Camera.main;
        if (Duplicate(eSkill, rSkill)) { Debug.LogWarning("Duplicate player skill: R slot disabled.", this); rSkill = null; }
        if (SkillInventorySession.Instance != null) SkillInventorySession.Instance.Bind(this);
    }
    private static bool Duplicate(SkillData a, SkillData b) => a != null && b != null &&
        (a == b || a.kind == b.kind || (!string.IsNullOrEmpty(a.id) && a.id == b.id));
    private void OnValidate()
    {
        if (Duplicate(eSkill, rSkill)) { rSkill = null; Debug.LogWarning("E/R must use different skills.", this); }
    }
    private void OnEnable() { health.Died += OnDeath; BindRun(); }
    private void Start() { BindRun(); }
    private void BindRun()
    {
        if (run != null) return;
        run = DungeonRunManager.Instance;
        if (run != null) run.RunEnded += CancelAll;
    }
    private void OnDeath(PlayerHealth _) { CancelAll(); }
    private void OnDisable()
    {
        if (health != null) health.Died -= OnDeath;
        if (run != null) run.RunEnded -= CancelAll;
        run = null; CancelAll();
    }
    private void Update()
    {
        if (health.IsDead || (run != null && (run.IsRunEnded || run.IsRestarting))) { if (live.Count > 0) CancelAll(); return; }
        if (Time.timeScale <= 0 || Keyboard.current == null || Mouse.current == null || aimCamera == null) return;
        int slot = Keyboard.current.eKey.wasPressedThisFrame ? 0 : Keyboard.current.rKey.wasPressedThisFrame ? 1 : -1;
        if (slot < 0) return;
        Vector2 mouse = Mouse.current.position.ReadValue();
        Vector3 target = aimCamera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, transform.position.z - aimCamera.transform.position.z));
        TryCast(slot, target);
    }
    public bool TryCast(int slot, Vector2 mouseWorld)
    {
        SkillData data = Equipped(slot);
        if (InventoryUI.BlockCombatInput || !isActiveAndEnabled || data == null || effectPrefab == null || Time.timeScale <= 0 || health.IsDead || IsCasting ||
            (run != null && (run.IsRunEnded || run.IsRestarting)) || (movement != null && movement.IsDodging) ||
            (potion != null && potion.IsUsing) || (attack != null && attack.IsAttacking) || CooldownRemaining(slot) > 0) return false;
        if (data.kind == PlayerSkillKind.DrillDash && !FreePosition(rb.position)) return false;
        var c = new Cast { data = data, damage = data.Damage(attackPower.PhysicalAttack, attackPower.MagicAttack), origin = rb.position };
        Vector2 offset = mouseWorld - c.origin;
        c.direction = offset.sqrMagnitude > .00001f ? offset.normalized : Vector2.right;
        c.target = SafePoint(c.origin, c.direction, data.centerOffset, .05f);
        if (data.kind == PlayerSkillKind.OreBlast)
            c.target = SafePoint(c.origin, c.direction, Mathf.Min(offset.magnitude, data.range), .12f);
        readyAt[slot] = Time.time + data.cooldown;
        live.Add(c); blocking = c;
        if (OwnsMovement) rb.linearVelocity = Vector2.zero;
        switch (data.kind)
        {
            case PlayerSkillKind.EarthSmash: StartCoroutine(Smash(c)); break;
            case PlayerSkillKind.TealBeam: StartCoroutine(Beam(c)); break;
            case PlayerSkillKind.DrillDash: StartCoroutine(Drill(c)); break;
            case PlayerSkillKind.OreBlast: StartCoroutine(Blast(c)); break;
        }
        return true;
    }
    private Color ColorFor(Cast c) => c.data.primaryType == SkillDamageType.Physical ? new Color(1,.79f,.34f) : new Color(.2f,1,.85f);
    private PlayerSkillVisual Visual(Cast c)
    {
        var v = Instantiate(effectPrefab); v.name = "Player Skill - " + c.data.displayName;
        visuals.Add(v); return v;
    }
    private void Remove(PlayerSkillVisual v)
    {
        visuals.Remove(v); if (v != null) { v.gameObject.SetActive(false); Destroy(v.gameObject); }
    }
    private void Unlock(Cast c) { if (blocking == c) blocking = null; }
    private IEnumerator Smash(Cast c)
    {
        var v = Visual(c); v.Configure(c.target, c.direction, 0, c.data.radius * 2, true, ColorFor(c));
        float start = Time.time;
        while (Time.time < start + c.data.windup) { v.SetProgress(Mathf.InverseLerp(start, start + c.data.windup, Time.time)); yield return null; }
        if (c.cancelled) yield break;
        if (ClearLine(c.origin, c.target)) HitCircle(c, c.target);
        if (c.cancelled) yield break;
        Unlock(c); v.SetProgress(1); v.AddChips();
        yield return Fade(v, .2f); live.Remove(c);
    }
    private IEnumerator Beam(Cast c)
    {
        var v = Visual(c); float start = Time.time;
        while (Time.time < start + c.data.windup)
        {
            v.Configure(rb.position, c.direction, StripLength(rb.position, c.direction, c.data.range, c.data.width), c.data.width, false, ColorFor(c));
            v.SetAlpha(.12f); yield return null;
        }
        if (c.cancelled) yield break;
        Vector2 origin = rb.position;
        float length = StripLength(origin, c.direction, c.data.range, c.data.width);
        v.Configure(origin, c.direction, length, c.data.width, false, ColorFor(c));
        HitStrip(c, origin, origin + c.direction * length, c.data.width);
        if (c.cancelled) yield break;
        Unlock(c); yield return Fade(v, c.data.effectDuration); live.Remove(c);
    }
    private IEnumerator Fade(PlayerSkillVisual v, float duration)
    {
        float start = Time.time;
        while (Time.time < start + duration) { if (v == null) yield break; float t = (Time.time - start) / duration; v.SetAlpha(.28f * (1 - t)); v.AnimateChips(t); yield return null; }
        Remove(v);
    }
    private bool ClearLine(Vector2 a, Vector2 b) => Physics2D.Linecast(a, b, wallLayer).collider == null;
    private Vector2 SafePoint(Vector2 origin, Vector2 direction, float distance, float radius)
    {
        var hit = Physics2D.CircleCast(origin, radius, direction, distance, wallLayer);
        return origin + direction * (hit.collider != null ? Mathf.Max(0, hit.distance - .03f) : distance);
    }
    private float StripLength(Vector2 origin, Vector2 direction, float length, float width)
    {
        var hit = Physics2D.BoxCast(origin, new Vector2(.02f, width), Vector2.SignedAngle(Vector2.right, direction), direction, length, wallLayer);
        return hit.collider != null ? Mathf.Max(0, hit.distance - .03f) : length;
    }
    private void HitCircle(Cast c, Vector2 center)
    {
        foreach (var collider in Physics2D.OverlapCircleAll(center, c.data.radius, enemyLayer))
            Hit(c, collider, center, c.direction);
    }
    private void HitStrip(Cast c, Vector2 from, Vector2 to, float width)
    {
        float length = Vector2.Distance(from, to);
        if (length < .001f) return;
        foreach (var collider in Physics2D.OverlapBoxAll((from + to) * .5f, new Vector2(length, width), Vector2.SignedAngle(Vector2.right, c.direction), enemyLayer))
        {
            Vector2 point = collider.ClosestPoint(from);
            Vector2 axis = from + c.direction * Mathf.Clamp(Vector2.Dot(point - from, c.direction), 0, length);
            if (ClearLine(axis, point)) Hit(c, collider, from, c.direction);
        }
    }
    private void Hit(Cast c, Collider2D collider, Vector2 from, Vector2 direction)
    {
        if (c.cancelled) return;
        var enemy = collider.GetComponentInParent<EnemyHealth>();
        if (enemy == null || enemy.IsDead || c.hit.Contains(enemy)) return;
        Vector2 point = collider.ClosestPoint(from);
        if (!ClearLine(from, point)) return;
        c.hit.Add(enemy); enemy.TryTakeDamage(c.damage, point, direction);
    }
    public void CancelAll()
    {
        foreach (var c in live) c.cancelled = true;
        RestoreDash();
        StopAllCoroutines(); blocking = null; live.Clear();
        foreach (var v in visuals) if (v != null) { v.gameObject.SetActive(false); Destroy(v.gameObject); }
        visuals.Clear(); if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}
