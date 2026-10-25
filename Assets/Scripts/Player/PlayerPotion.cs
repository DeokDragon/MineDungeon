using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerPotion : MonoBehaviour
{
    [SerializeField, Range(0.01f, 1f)]
    private float healRatio = 0.35f;

    [SerializeField, Min(0.01f)]
    private float useDuration = 0.35f;

    private PlayerHealth health;
    private PlayerMovement movement;
    private PlayerAttack attack;
    private float remainingUseTime;

    public bool IsUsing { get; private set; }

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
    }

    internal static int CalculateHealAmount(int maximumHealth, float ratio, bool health20)
    {
        // Preserve the original inactive path; the bonus path rounds only once.
        return health20 ? (int)System.Math.Ceiling((double)maximumHealth * ratio * 1.2d)
            : Mathf.CeilToInt(maximumHealth * ratio);
    }

    private void Update()
    {
        if (health.IsDead)
        {
            CancelUse();
            return;
        }

        if (health.CombatLocked || Time.timeScale == 0f)
            return;

        if (IsUsing)
        {
            remainingUseTime -= Time.deltaTime;

            if (remainingUseTime <= 0f)
            {
                IsUsing = false;

                var run = DungeonRunManager.Instance;
                var growth = run != null ? run.GetComponent<PlayerProgression>() : null;
                int healAmount = CalculateHealAmount(health.MaxHealth, healRatio,
                    growth != null && growth.HasHealth20Passive);

                health.Heal(healAmount);
            }

            return;
        }

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.qKey.wasPressedThisFrame)
            TryStartUse();
    }

    private void TryStartUse()
    {
        if (health.CombatLocked || InventoryUI.BlockCombatInput) return;
        var skills = GetComponent<PlayerSkills>();
        if (skills != null && skills.IsCasting) return;
        if (health.CurrentHealth >= health.MaxHealth)
            return;

        if (movement != null && movement.IsDodging)
            return;

        if (attack != null && attack.IsAttacking)
            return;

        DungeonRunManager run = DungeonRunManager.Instance;

        if (run == null || !run.TryUsePotion())
            return;

        IsUsing = true;
        attack?.ResetCombo();
        remainingUseTime = useDuration;

        Debug.Log("물약 사용 중", this);
    }

    private void CancelUse()
    {
        IsUsing = false;
        remainingUseTime = 0f;
    }

    private void OnDisable()
    {
        CancelUse();
    }
}