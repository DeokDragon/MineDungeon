using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerSkillHUD : MonoBehaviour
{
    [System.Serializable] private sealed class Slot
    {
        public Image icon, cooldownOverlay;
        public TMP_Text nameText, keyText, marker, remainingText;
    }
    [SerializeField] private PlayerSkills player;
    [SerializeField] private Slot eSlot, rSlot;
    private void Awake()
    {
        foreach (var slot in new[] { eSlot, rSlot })
        {
            // TMP instantiates the text material; the shared font asset is preserved.
            slot.remainingText.color = Color.white;
            slot.remainingText.outlineColor = Color.black;
            slot.remainingText.outlineWidth = .18f;
        }
    }
    private void LateUpdate()
    {
        Draw(eSlot, 0, "E"); Draw(rSlot, 1, "R");
    }
    private void Draw(Slot view, int slot, string key)
    {
        if (player == null) return;
        SkillData data = player.Equipped(slot);
        view.keyText.text = key;
        view.nameText.text = data != null ? data.displayName : "—";
        view.icon.sprite = data != null ? data.icon : null;
        view.icon.enabled = data != null && data.icon != null;
        view.marker.text = data == null ? "—" : new[] { "I", "II", "III", "IV" }[(int)data.kind];
        view.marker.enabled = !view.icon.enabled;
        view.marker.color = data != null && data.primaryType == SkillDamageType.Magic
            ? new Color(.25f, 1f, .85f) : new Color(1f, .8f, .4f);
        float remaining = player.CooldownRemaining(slot);
        float applied = player.AppliedCooldown(slot);
        view.cooldownOverlay.gameObject.SetActive(remaining > 0f);
        // Bottom-anchored height leaves the icon visible from the top as time elapses.
        float fraction = applied > 0f ? Mathf.Clamp01(remaining / applied) : 1f;
        view.cooldownOverlay.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            view.icon.rectTransform.rect.height * fraction);
        view.remainingText.text = FormatRemaining(remaining);
    }

    internal static string FormatRemaining(float remaining)
    {
        if (remaining <= 0f) return "";
        return remaining >= 10f ? Mathf.Ceil(remaining).ToString("0")
            : (Mathf.Ceil(remaining * 10f) / 10f).ToString("0.0");
    }
}
