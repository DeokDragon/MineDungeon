using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerDamageScreen : MonoBehaviour
{
    [SerializeField] private PlayerHealth player;
    [SerializeField] private Image[] edges;
    [SerializeField] private float duration = .15f;
    [SerializeField] private float opacity = .22f;
    private float remaining;
    private void OnEnable() { player.Damaged += OnDamaged; SetAlpha(0f); }
    private void OnDamaged() { remaining = duration; SetAlpha(opacity); }
    public void Clear() { remaining = 0f; SetAlpha(0f); }
    private void Update()
    {
        if (DungeonRunManager.Instance != null && DungeonRunManager.Instance.IsRunEnded) remaining = 0f;
        remaining = Mathf.Max(0f, remaining - (Time.timeScale > 0f ? Time.deltaTime : 0f));
        SetAlpha(opacity * remaining / duration);
    }
    private void SetAlpha(float alpha)
    {
        foreach (var edge in edges) { edge.color = new Color(1f, .08f, .04f, alpha); edge.raycastTarget = false; }
    }
    private void OnDisable() { if (player != null) player.Damaged -= OnDamaged; remaining = 0f; SetAlpha(0f); }
}
