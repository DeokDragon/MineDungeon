using UnityEngine;

// Sizes the window, never the Canvas or its text/icon transforms.
[ExecuteAlways, RequireComponent(typeof(RectTransform))]
public sealed class CenteredWindowLayout : MonoBehaviour
{
    [SerializeField] private Vector2 preferredSize = new Vector2(1440, 810);
    [SerializeField, Min(0)] private float screenMargin = 24;
    private void OnEnable() { Fit(); }
    private void LateUpdate() { Fit(); }
    private void Fit()
    {
        var rect = (RectTransform)transform;
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        var available = ((RectTransform)canvas.transform).rect.size;
        var size = new Vector2(Mathf.Min(preferredSize.x, Mathf.Max(1, available.x - screenMargin * 2)),
            Mathf.Min(preferredSize.y, Mathf.Max(1, available.y - screenMargin * 2)));
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = Vector2.zero;
        if (rect.sizeDelta != size) rect.sizeDelta = size;
    }
}
