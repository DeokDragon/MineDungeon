using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("플레이어")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("UI")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text reviveText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text potionText;

    private void Start()
    {
        if (healthBar != null)
        {
            healthBar.interactable = false;
            healthBar.minValue = 0f;
        }

        RefreshUI();
    }

    private void Update()
    {
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (playerHealth == null)
            return;

        if (healthBar != null)
        {
            healthBar.maxValue = playerHealth.MaxHealth;
            healthBar.value = playerHealth.CurrentHealth;
        }

        if (healthText != null)
        {
            healthText.text =
                $"HP {playerHealth.CurrentHealth} / " +
                $"{playerHealth.MaxHealth}";
        }

        DungeonRunManager run = DungeonRunManager.Instance;

        if (potionText != null)
        {
            PlayerPotion potion = playerHealth.GetComponent<PlayerPotion>();

            string usingText = potion != null && potion.IsUsing
                ? "  Using..."
                : "";

            potionText.text = run != null
                ? $"Potions {run.RemainingPotions} / {run.MaxPotions} [Q]{usingText}"
                : "Potions -- / --";
        }

        if (reviveText != null)
        {
            reviveText.text = run != null
                ? $"Revives {run.RemainingRevives} / {run.MaxRevives}"
                : "Revives -- / --";
        }

        if (statusText == null)
            return;

        if (run != null && run.IsRunFailed)
        {
            statusText.text = "RUN FAILED";
            statusText.color = Color.red;
        }
        else if (playerHealth.IsDead)
        {
            statusText.text = "Reviving...";
            statusText.color = Color.yellow;
        }
        else
        {
            statusText.text = "";
        }
    }
}