using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Slider healthBar;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text reviveText, statusText, potionText;
    [Header("성장 / 남은 생명")]
    [SerializeField] private TMP_Text levelText, experienceText;
    [SerializeField] private Image experienceFill;
    [SerializeField] private Image[] lifeTalismans;
    [SerializeField] private Color activeLife = new Color(.25f, 1f, .85f);
    [SerializeField] private Color spentLife = new Color(.18f, .21f, .23f);
    [Header("레벨업 알림")]
    [SerializeField] private CanvasGroup levelNotice;
    [SerializeField] private TMP_Text noticeText;
    [SerializeField, Min(0)] private float noticeHold = 1.5f, noticeFade = .3f;
    private DungeonRunManager run;
    private PlayerProgression growth;
    private PlayerPotion potion;
    private float noticeElapsed;
    private int noticeFrom, noticeTo, noticePoints;
    private bool showingNotice;

    private void Start()
    {
        run = DungeonRunManager.Instance;
        growth = run != null ? run.GetComponent<PlayerProgression>() : null;
        potion = playerHealth != null ? playerHealth.GetComponent<PlayerPotion>() : null;
        if (healthBar != null) { healthBar.interactable = false; healthBar.minValue = 0; }
        if (reviveText != null) reviveText.gameObject.SetActive(false);
        if (run != null) { run.ResourcesChanged += RefreshUI; run.RunEnded += ClearNotice; }
        if (growth != null) { growth.Changed += RefreshUI; growth.LeveledUp += OnLevelUp; }
        ClearNotice(); RefreshUI();
    }
    private void LateUpdate()
    {
        RefreshUI();
        if (!showingNotice) return;
        if (run != null && (run.IsRunEnded || run.IsRestarting)) { ClearNotice(); return; }
        noticeElapsed += Time.timeScale > 0f ? Time.deltaTime : 0f;
        levelNotice.alpha = noticeElapsed <= noticeHold ? 1f : noticeFade > 0
            ? Mathf.Clamp01(1f - (noticeElapsed - noticeHold) / noticeFade) : 0f;
        if (noticeElapsed >= noticeHold + noticeFade) ClearNotice();
    }
    private void RefreshUI()
    {
        if (playerHealth == null) return;
        if (healthBar != null) { healthBar.maxValue = Mathf.Max(1, playerHealth.MaxHealth); healthBar.value = playerHealth.CurrentHealth; }
        if (healthText != null) healthText.text = $"{playerHealth.CurrentHealth} / {playerHealth.MaxHealth}";
        if (potionText != null) potionText.text = run == null ? "—" : $"{run.RemainingPotions} / {run.MaxPotions}" + (potion != null && potion.IsUsing ? " · 사용 중" : "");
        if (statusText != null) statusText.text = playerHealth.IsDead && run != null && !run.IsRunFailed ? "부활 대기 중" : "";
        if (lifeTalismans != null) for (int i = 0; i < lifeTalismans.Length; i++)
            if (lifeTalismans[i] != null) lifeTalismans[i].color = run != null && i < run.RemainingLives ? activeLife : spentLife;
        if (growth == null) return;
        if (levelText != null) levelText.text = $"Lv. {growth.Level}";
        if (experienceText != null) experienceText.text = growth.IsMaximumLevel ? "MAX" : $"EXP {growth.Experience} / {growth.RequiredExperience}";
        if (experienceFill != null) experienceFill.rectTransform.anchorMax = new Vector2(growth.Progress, 1);
    }
    private void OnLevelUp(int from, int to, int points)
    {
        if (levelNotice == null || noticeText == null || (run != null && (run.IsRunEnded || run.IsRestarting))) return;
        if (!showingNotice) { noticeFrom = from; noticePoints = 0; }
        noticeTo = to; noticePoints += points; noticeElapsed = 0; showingNotice = true;
        noticeText.text = noticeTo - noticeFrom == 1 ? $"레벨 업! Lv. {noticeTo}\n스탯 포인트 +{noticePoints}"
            : $"레벨 업! Lv. {noticeFrom} → Lv. {noticeTo}\n스탯 포인트 +{noticePoints}";
        levelNotice.gameObject.SetActive(true); levelNotice.alpha = 1;
    }
    private void ClearNotice() { showingNotice = false; if (levelNotice != null) { levelNotice.alpha = 0; levelNotice.gameObject.SetActive(false); } }
    private void OnDisable() { ClearNotice(); }
    private void OnDestroy()
    {
        if (run != null) { run.ResourcesChanged -= RefreshUI; run.RunEnded -= ClearNotice; }
        if (growth != null) { growth.Changed -= RefreshUI; growth.LeveledUp -= OnLevelUp; }
    }
}
