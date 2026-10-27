using UnityEngine;

// Presentation only: RoomExit owns the collider, open state and immediate room travel.
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class MineExitSpriteAnimation : MonoBehaviour
{
    [SerializeField] private RoomExit exit;
    [SerializeField] private Sprite locked, opened;
    [SerializeField] private Sprite[] opening, torchLoop;
    [SerializeField] private SpriteRenderer sparkleVisual, glowVisual;
    [SerializeField] private Sprite[] openingSparkle, tunnelGlow;
    private SpriteRenderer visual;
    private bool wasOpen;
    private float openedAt;
    private bool animateOpening;

    private static readonly float[] OpeningTimes = {.06f, .07f, .08f, .10f, .11f, .10f};
    private static readonly float[] TorchTimes = {.18f, .16f, .20f, .16f};
    private static readonly float[] SparkleTimes = {.04f, .06f, .07f, .08f, .08f, .07f};
    private const float OpeningDuration = .52f, TorchDuration = .70f;
    private const float SparkleDuration = .40f, GlowDuration = 2.80f;

    private void Awake()
    {
        visual = GetComponent<SpriteRenderer>();
        if (!exit) exit = GetComponentInParent<RoomExit>();
    }

    private void OnEnable()
    {
        if (!visual) visual = GetComponent<SpriteRenderer>();
        wasOpen = exit && exit.IsOpen;
        animateOpening = false;
        openedAt = Time.time;
        visual.sprite = wasOpen ? opened : locked;
        HideFeedback();
        if (wasOpen) ShowFeedback(glowVisual, tunnelGlow, 0);
    }

    private void LateUpdate()
    {
        if (!exit || !visual) return;
        bool isOpen = exit.IsOpen;
        if (isOpen != wasOpen)
        {
            wasOpen = isOpen;
            openedAt = Time.time;
            animateOpening = isOpen;
        }
        if (!isOpen) { visual.sprite = locked; HideFeedback(); return; }
        float elapsed = Mathf.Max(0f, Time.time - openedAt);
        if (animateOpening && elapsed < OpeningDuration)
        {
            Show(opening, Index(elapsed, OpeningTimes), opened);
            HideFeedback();
        }
        else
        {
            float loopAge = animateOpening ? elapsed - OpeningDuration : elapsed;
            Show(torchLoop, Index(Mathf.Repeat(loopAge, TorchDuration), TorchTimes), opened);
            // The single sparkle window belongs to this opening, never to the repeating loops.
            if (animateOpening && loopAge < SparkleDuration)
                ShowFeedback(sparkleVisual, openingSparkle, Index(loopAge, SparkleTimes));
            else Hide(sparkleVisual);
            ShowFeedback(glowVisual, tunnelGlow,
                Mathf.Min(7, Mathf.FloorToInt(Mathf.Repeat(loopAge, GlowDuration) / .35f)));
        }
    }

    private void OnDisable() { HideFeedback(); }

    private void HideFeedback() { Hide(sparkleVisual); Hide(glowVisual); }

    private static void Hide(SpriteRenderer renderer)
    {
        if (!renderer) return;
        renderer.enabled = false;
        renderer.sprite = null;
    }

    private static void ShowFeedback(SpriteRenderer renderer, Sprite[] frames, int index)
    {
        if (!renderer) return;
        bool valid = frames != null && index >= 0 && index < frames.Length && frames[index];
        renderer.enabled = valid;
        renderer.sprite = valid ? frames[index] : null;
    }

    private void Show(Sprite[] frames, int index, Sprite fallback)
    {
        visual.sprite = frames != null && index < frames.Length && frames[index]
            ? frames[index] : fallback;
    }

    private static int Index(float elapsed, float[] times)
    {
        float end = 0f;
        for (int i = 0; i < times.Length; i++)
        {
            end += times[i];
            if (elapsed < end) return i;
        }
        return times.Length - 1;
    }
}
