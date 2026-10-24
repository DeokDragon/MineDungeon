using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("체력")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private float invincibleDuration = 0.6f;

    [Header("부활 테스트 설정")]
    [SerializeField] private float reviveDelay = 1f;
    [SerializeField, Range(0.01f, 1f)]
    private float reviveHealthRatio = 1f;
    [SerializeField] private float reviveInvincibleDuration = 2f;
    [SerializeField] private Transform respawnPoint;

    private PlayerMovement movement;
    private PlayerAttack attack;
    private Rigidbody2D rb;
    private SpriteRenderer bodyRenderer;

    private Color originalColor;
    private float invincibleUntil;
    private float hurtFlashRemaining;
    public event System.Action Damaged;
    private bool movementWasEnabled;
    private bool attackWasEnabled;
    private PlayerProgression growth;
    private int effectiveMaxHealth;
    private bool initialized;
    private DungeonRoomManager regenerationRooms;
    private RoomController regenerationRoom;
    private float lastHealthDamageAt;
    private double regenerationFraction, regenerationUsed;

    internal static int RegenerationLimit(int maximum) => maximum > 0 ? (int)((long)maximum / 5) : 0;
    internal static double ScaleRegenerationUsed(double used, int previousMaximum, int nextMaximum)
    {
        int previous = RegenerationLimit(previousMaximum), next = RegenerationLimit(nextMaximum);
        return previous > 0 ? System.Math.Min(next, used / previous * next) : 0d;
    }
    private void ResetRegeneration()
    {
        regenerationFraction = regenerationUsed = 0d;
        lastHealthDamageAt = CombatTime;
    }
    public void ResetForNewRun()
    {
        regenerationRoom = null; regenerationRooms = null;
        ResetRegeneration(); attack?.ResetEmpoweredAttack();
    }
    private void AdvanceRegeneration(float now, float deltaTime)
    {
        var run = DungeonRunManager.Instance;
        if (Time.timeScale <= 0f || deltaTime <= 0f) return;
        if (IsDead || growth == null || !growth.HasHealth100Passive || run == null || !run.IsTracking ||
            run.IsEnding || run.IsRunEnded || run.IsRestarting || regenerationRooms == null ||
            regenerationRooms.IsTransitioning || regenerationRooms.CurrentRoom == null ||
            regenerationRooms.CurrentRoom.Controller != regenerationRoom || regenerationRoom == null ||
            regenerationRoom.IsCleared || regenerationRoom.RemainingEnemies <= 0 || CurrentHealth >= MaxHealth)
        { regenerationFraction = 0d; return; }
        double elapsed = System.Math.Min(deltaTime, System.Math.Max(0d, (double)now - lastHealthDamageAt - 5d));
        double remaining = System.Math.Max(0d, RegenerationLimit(MaxHealth) - regenerationUsed);
        regenerationFraction = System.Math.Min(remaining, regenerationFraction + MaxHealth * .01d * elapsed);
        int request = (int)System.Math.Min(MaxHealth - CurrentHealth, System.Math.Floor(regenerationFraction));
        if (request <= 0) return;
        int before = CurrentHealth;
        Heal(request);
        int actual = CurrentHealth - before;
        regenerationUsed += actual;
        regenerationFraction -= actual;
        if (CurrentHealth >= MaxHealth) regenerationFraction = 0d;
    }

    // A per-player combat clock: presentations freeze combat without pausing camera/UI time.
    private float presentationStartedAt, presentationElapsed;
    public bool CombatLocked { get; private set; }
    public float CombatTime => (CombatLocked ? presentationStartedAt : Time.time) - presentationElapsed;
    internal void SetBossPresentation(bool locked)
    {
        if (CombatLocked == locked) return;
        if (locked)
        {
            presentationStartedAt = Time.time;
            CombatLocked = true;
            attack?.CancelAttack();
            GetComponent<PlayerSkills>()?.CancelAll();
            movement?.CancelForPresentation();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
        else
        {
            presentationElapsed += Time.time - presentationStartedAt;
            CombatLocked = false;
        }
    }

    public bool IsDead { get; private set; }
    public event System.Action<PlayerHealth> Died;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => initialized ? effectiveMaxHealth : maxHealth;
    public int MaxHealthWithStatBonus => MaxHealth;

    private void Awake()
    {
        if (maxHealth <= 0)
        { Debug.LogWarning("Invalid base maximum HP; player health initialization blocked.", this); enabled = false; return; }
        effectiveMaxHealth = maxHealth;
        currentHealth = maxHealth;
        IsDead = false;
        initialized = true;

        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        rb = GetComponent<Rigidbody2D>();
        bodyRenderer = GetComponent<SpriteRenderer>();

        if (bodyRenderer != null)
            originalColor = bodyRenderer.color;
    }

    private void OnEnable() { RefreshGrowthHealth(); }
    // The persistent run manager's Awake can run after this component's Awake/OnEnable.
    private void Start() { RefreshGrowthHealth(); }
    private void OnDisable()
    {
        if (growth != null) growth.Changed -= OnGrowthChanged;
        growth = null;
    }
    private void OnGrowthChanged() { RefreshGrowthHealth(); }

    public bool RefreshGrowthHealth()
    {
        if (!initialized) return false;
        var run = DungeonRunManager.Instance;
        var source = run != null ? run.GetComponent<PlayerProgression>() : null;
        if (growth != source)
        {
            if (growth != null) growth.Changed -= OnGrowthChanged;
            growth = source;
            if (growth != null) growth.Changed += OnGrowthChanged;
        }
        if (growth == null) return false;
        long nextMaximum = (long)maxHealth + growth.MaximumHealthBonus;
        int adjusted;
        if (maxHealth <= 0 || growth.HealthPoints < 0 || growth.HealthPoints > 100 ||
            nextMaximum <= 0 || nextMaximum > int.MaxValue ||
            !TryScaleHealth(currentHealth, effectiveMaxHealth, (int)nextMaximum, IsDead, out adjusted))
        { Debug.LogWarning("Invalid HP state; growth HP update blocked without changing health.", this); return false; }
        if (effectiveMaxHealth != (int)nextMaximum)
        {
            regenerationUsed = ScaleRegenerationUsed(regenerationUsed, effectiveMaxHealth, (int)nextMaximum);
            regenerationFraction = System.Math.Min(regenerationFraction,
                System.Math.Max(0d, RegenerationLimit((int)nextMaximum) - regenerationUsed));
        }
        effectiveMaxHealth = (int)nextMaximum;
        currentHealth = adjusted;
        // This is a stat update, never Heal(), damage, potion use or resurrection.
        return true;
    }

    internal bool CanResetStatPassives
    {
        get { int adjusted; return initialized && !IsDead && maxHealth > 0 &&
            TryScaleHealth(currentHealth, effectiveMaxHealth, maxHealth, false, out adjusted); }
    }
    internal void ResetStatPassives()
    {
        ResetRegeneration(); attack?.ResetEmpoweredAttack();
        if (!RefreshGrowthHealth()) throw new System.InvalidOperationException("Committed reset HP refresh failed");
    }

    internal static bool TryScaleHealth(int current, int previousMaximum, int nextMaximum, bool dead, out int adjusted)
    {
        adjusted = current;
        if (previousMaximum <= 0 || nextMaximum <= 0 || current < 0 || current > previousMaximum) return false;
        if (dead) { adjusted = 0; return true; }
        if (previousMaximum == nextMaximum) return true;
        // Exact integer ceil(current / previousMaximum * nextMaximum), without float drift.
        adjusted = (int)System.Math.Min(nextMaximum, ((long)current * nextMaximum + previousMaximum - 1) / previousMaximum);
        return true;
    }

    private void Update()
    {
        if (CombatLocked) return;
        AdvanceRegeneration(CombatTime, Time.deltaTime);
        if (bodyRenderer == null)
            return;

        if (IsDead)
        {
            bodyRenderer.color = Color.gray;
            return;
        }

        if (hurtFlashRemaining > 0f)
        {
            hurtFlashRemaining = Mathf.Max(0f, hurtFlashRemaining - (Time.timeScale > 0f ? Time.deltaTime : 0f));
            bodyRenderer.color = new Color(1f, .18f, .12f, originalColor.a);
            return;
        }

        // 무적 시간 동안 몸체를 반투명하게 깜빡임
        Color color = originalColor;

        if (CombatTime < invincibleUntil)
        {
            color.a *= Mathf.PingPong(CombatTime * 10f, 1f)
                < 0.5f ? 0.35f : 1f;
        }

        bodyRenderer.color = color;
    }

    public void TakeDamage(int damage) { TryTakeDamage(damage); }

    public bool TryTakeDamage(int damage)
    {
        if (DungeonRunManager.Instance != null && DungeonRunManager.Instance.IsRunEnded)
            return false;
        if (CombatLocked || IsDead || damage <= 0 || CombatTime < invincibleUntil)
            return false;

        DungeonRunManager.Instance?.RecordDamageTaken(Mathf.Min(currentHealth,damage));
        int previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - damage);
        if (currentHealth < previousHealth)
        { lastHealthDamageAt = CombatTime; regenerationFraction = 0d; }
        invincibleUntil = CombatTime + invincibleDuration;
        hurtFlashRemaining = .10f;
        Damaged?.Invoke();
        if (Camera.main != null && Camera.main.TryGetComponent<CameraFollow>(out var camera))
            camera.Shake(.07f, .12f);

        Debug.Log(
            $"플레이어: {damage} 피해 / 남은 체력 {currentHealth}",
            this
        );

        if (currentHealth == 0)
            HandleDefeat();
        return true;
    }

    private void HandleDefeat()
    {
        IsDead = true;
        attack?.ResetEmpoweredAttack();
        Died?.Invoke(this);

        movementWasEnabled = movement != null && movement.enabled;
        attackWasEnabled = attack != null && attack.enabled;

        if (movement != null)
            movement.enabled = false;

        if (attack != null)
            attack.enabled = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        DungeonRunManager run = DungeonRunManager.Instance;

        if (run == null)
        {
            Debug.LogError(
                "씬에 DungeonRunManager가 없습니다.",
                this
            );
            return;
        }

        if (run.TryUseRevive())
        {
            StartCoroutine(ReviveRoutine());
        }
        else
        {
            run.FailRun();
        }
    }

    private IEnumerator ReviveRoutine()
    {
        Debug.Log("부활 대기 중", this);

        yield return new WaitForSeconds(reviveDelay);
        if (respawnPoint != null)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.position = (Vector2)respawnPoint.position;
            }
            else
            {
                transform.position = respawnPoint.position;
            }
        }
        else
        {
            Debug.LogError("Player Health에 Respawn Point를 연결해주세요.", this);
        }

        currentHealth = Mathf.Clamp(
            Mathf.CeilToInt(MaxHealth * reviveHealthRatio),
            1,
            MaxHealth
        );

        if (Camera.main != null && Camera.main.TryGetComponent<CameraFollow>(out var camera))
            camera.SnapToTarget();

        invincibleUntil = CombatTime + reviveInvincibleDuration;
        IsDead = false;
        ResetRegeneration();
        attack?.ResetEmpoweredAttack();
        DungeonRunManager.Instance?.NotifyRevived();
        hurtFlashRemaining = 0f;

        if (movement != null)
            movement.enabled = movementWasEnabled;

        if (attack != null)
            attack.enabled = attackWasEnabled;

        Debug.Log(
            $"부활 완료 / 체력 {currentHealth} / " +
            $"남은 부활 {DungeonRunManager.Instance.RemainingRevives}회",
            this
        );
    }
    public void FinishDungeon(bool clear)
    {
        regenerationRoom = null; regenerationRooms = null; ResetRegeneration();
        StopAllCoroutines(); GetComponent<PlayerSkills>()?.CancelAll();
        attack?.CancelAttack(); if(attack)attack.enabled=false;
        var potion=GetComponent<PlayerPotion>(); if(potion)potion.enabled=false;
        movement?.ClearSlow(); if(movement) { movement.enabled=false; if(clear)movement.enabled=true; }
        if(clear && IsDead) { IsDead=false;currentHealth=Mathf.Max(1,currentHealth);hurtFlashRemaining=0;invincibleUntil=0; }
        if(rb)rb.linearVelocity=Vector2.zero;
    }
    public void GrantInvincibility(float duration)
    {
        if (IsDead)
            return;

        invincibleUntil = Mathf.Max(
            invincibleUntil,
            CombatTime + duration
        );
    }
    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        int previousHealth = currentHealth;
        currentHealth = (int)System.Math.Min(MaxHealth, (long)currentHealth + amount);
        DungeonRunManager.Instance?.RecordHealing(currentHealth-previousHealth);

        Debug.Log(
            $"물약 회복: {currentHealth - previousHealth} / " +
            $"현재 체력 {currentHealth}",
            this
        );
    }
    // 방 전환은 체력, 물약, 부활 횟수를 초기화하지 않습니다.
    public void EnterRoom(Transform entrance)
    {
        if (entrance == null || IsDead) return;
        regenerationRooms = FindFirstObjectByType<DungeonRoomManager>();
        var entered = entrance.GetComponentInParent<DungeonRoom>();
        regenerationRoom = entered != null ? entered.Controller : null;
        ResetRegeneration();
        respawnPoint = entrance;
        bool restoreMovement = movement != null && movement.enabled;
        bool restoreAttack = attack != null && attack.enabled;
        if (movement != null) movement.enabled = false;
        if (attack != null) attack.enabled = false;
        Vector3 destination = entrance.position;
        destination.z = transform.position.z;
        transform.position = destination;
        if (rb != null)
        {
            rb.position = destination;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        if (movement != null) movement.enabled = restoreMovement;
        if (attack != null) attack.enabled = restoreAttack;
    }

}
