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
    [SerializeField] private Button presentationSkip;
    [SerializeField] private Image currentFill, damageTrail;
    [SerializeField] private string displayName = "돌 골렘";
    [SerializeField, Min(0f)] private float trailDelay = 1f, trailDuration = .4f, deathTrailDuration = .2f;
    private readonly HealthBarTrail gauge = new HealthBarTrail();
    private EnemyHealth health;
    private bool dying;
    private StoneGolemBossAI presentationBoss;
    private CameraFollow presentationCamera;
    private float deathStart, deathElapsed, deathRealStart;

    private void OnEnable()
    {
        if (presentationSkip != null)
        {
            presentationSkip.onClick.AddListener(SkipPresentation);
            presentationSkip.gameObject.SetActive(false);
        }
        if (rooms != null) { rooms.BossChanged += Bind; Bind(rooms.CurrentBoss); }
        if (resultUI != null) { resultUI.Shown += Hide; if (resultUI.IsShowing) Hide(); }
    }
    private void Unsubscribe()
    {
        ReleasePresentation();
        if (presentationBoss != null)
        {
            presentationBoss.PresentationStarted -= PresentationStarted;
            presentationBoss.PresentationEnded -= PresentationEnded;
        }
        presentationBoss = null;
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
        presentationBoss = health.GetComponent<StoneGolemBossAI>();
        if (presentationBoss != null)
        {
            presentationBoss.PresentationStarted += PresentationStarted;
            presentationBoss.PresentationEnded += PresentationEnded;
            if (presentationBoss.IsPresenting) PresentationStarted(presentationBoss, presentationBoss.IsTransforming);
        }
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
        bool presenting = presentationBoss != null && presentationBoss.isActiveAndEnabled &&
            presentationBoss.IsPresenting && health != null && !health.IsDead &&
            (DungeonRunManager.Instance == null || !DungeonRunManager.Instance.IsRunEnded) &&
            (resultUI == null || !resultUI.IsShowing);
        if (!presenting) ReleasePresentation(); // Also covers aborts without an Ended event.
        if (presentationSkip != null)
            presentationSkip.gameObject.SetActive(presenting && Time.timeScale > 0f);
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
    private void PresentationStarted(StoneGolemBossAI source, bool transformation)
    {
        if (source != presentationBoss) return;
        presentationCamera = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (presentationCamera != null) presentationCamera.FocusPresentation(source.transform);
        if (presentationSkip != null) presentationSkip.gameObject.SetActive(Time.timeScale > 0f);
    }
    private void PresentationEnded(StoneGolemBossAI source, bool transformation)
    {
        if (source == presentationBoss) ReleasePresentation();
    }
    private void ReleasePresentation()
    {
        if (presentationCamera != null && presentationBoss != null)
            presentationCamera.ReleasePresentation(presentationBoss.transform);
        presentationCamera = null;
        if (presentationSkip != null) presentationSkip.gameObject.SetActive(false);
    }
    private void SkipPresentation()
    {
        if (Time.timeScale <= 0f || presentationBoss == null || !presentationBoss.isActiveAndEnabled ||
            !presentationBoss.IsPresenting || health == null || health.IsDead ||
            (resultUI != null && resultUI.IsShowing) ||
            (DungeonRunManager.Instance != null && DungeonRunManager.Instance.IsRunEnded)) return;
        if (presentationSkip != null) presentationSkip.gameObject.SetActive(false);
        presentationBoss.SkipPresentation(); // Existing idempotent common completion path.
    }
    private void OnDisable()
    {
        if (presentationSkip != null) presentationSkip.onClick.RemoveListener(SkipPresentation);
        if (rooms != null) rooms.BossChanged -= Bind;
        if (resultUI != null) resultUI.Shown -= Hide;
        Unsubscribe();
        dying = false;
        if (panel != null) panel.SetActive(false);
    }
}
