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
        view.cooldownOverlay.rectTransform.anchorMax = new Vector2(1, data != null && applied > 0 ? Mathf.Clamp01(remaining / applied) : 0);
        view.remainingText.text = remaining > 0f ? remaining.ToString("0.0") : "";
    }
}
