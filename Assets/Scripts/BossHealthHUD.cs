using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BossHealthHUD : MonoBehaviour
{
    [SerializeField] private DungeonRoomManager rooms;
    [SerializeField] private DungeonResultUI resultUI;
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text bossName;
    [SerializeField] private Image currentFill, damageTrail;
    [SerializeField] private string displayName = "돌 골렘";
    [SerializeField, Min(0f)] private float trailDelay = 1f, trailDuration = .4f, deathTrailDuration = .2f;
    private readonly HealthBarTrail gauge = new HealthBarTrail();
    private EnemyHealth health;
    private bool dying;
    private float deathStart, deathElapsed, deathRealStart;

    private void OnEnable()
    {
        if (rooms != null) { rooms.BossChanged += Bind; Bind(rooms.CurrentBoss); }
        if (resultUI != null) { resultUI.Shown += Hide; if (resultUI.IsShowing) Hide(); }
    }
    private void Unsubscribe()
    {
        if (health != null) { health.HealthChanged -= Changed; health.Died -= Died; }
        health = null;
    }
    private void Bind(EnemyHealth next)
    {
        Unsubscribe();
        dying = false;
        deathElapsed = 0f;
        gauge.Reset(0f);
        bossName.text = "";
        panel.SetActive(next != null && !next.IsDead && (resultUI == null || !resultUI.IsShowing));
        if (!panel.activeSelf) { Render(0f, 0f); return; }
        health = next;
        health.HealthChanged += Changed;
        health.Died += Died;
        gauge.Reset(HealthBarTrail.Fraction(health));
        bossName.text = displayName;
        Render(gauge.Current, gauge.Trail);
    }
    private void Changed(EnemyHealth source)
    {
        gauge.SetValue(HealthBarTrail.Fraction(source));
        Render(gauge.Current, gauge.Trail);
    }
    private void Died(EnemyHealth source)
    {
        // Copy presentation state before EnemyHealth destroys its GameObject.
        deathStart = gauge.Trail;
        deathElapsed = 0f;
        deathRealStart = Time.realtimeSinceStartup;
        dying = true;
        Unsubscribe();
        Render(0f, deathStart);
    }
    private void Update()
    {
        if (!panel.activeSelf) return;
        if (dying)
        {
            bool finale = DungeonRunManager.Instance != null && DungeonRunManager.Instance.IsRunCleared;
            if (finale) deathElapsed = Time.realtimeSinceStartup - deathRealStart;
            else if (Time.timeScale > 0f) deathElapsed += Time.deltaTime;
            float t = deathTrailDuration > 0f ? Mathf.Clamp01(deathElapsed / deathTrailDuration) : 1f;
            Render(0f, Mathf.Lerp(deathStart, 0f, Mathf.SmoothStep(0f, 1f, t)));
            return;
        }
        if (health == null || !health.gameObject.activeInHierarchy) { Hide(); return; }
        if (Time.timeScale > 0f) gauge.Tick(Time.deltaTime, trailDelay, trailDuration);
        Render(gauge.Current, gauge.Trail);
    }
    private static void Fill(Image image, float value)
    {
        // Both layers share the full bar rect; the right anchor alone clips the length.
        var rect = image.rectTransform;
        rect.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
    }
    private void Render(float current, float trail) { Fill(currentFill, current); Fill(damageTrail, trail); }
    private void Hide() { Bind(null); }
    private void OnDisable()
    {
        if (rooms != null) rooms.BossChanged -= Bind;
        if (resultUI != null) resultUI.Shown -= Hide;
        Unsubscribe();
        dying = false;
        if (panel != null) panel.SetActive(false);
    }
}
