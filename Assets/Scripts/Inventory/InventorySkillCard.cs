using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventorySkillCard : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image rarityBorder, selectedFrame, icon;
    [SerializeField] private TMP_Text nameText, marker, badge;
    [SerializeField] private InventoryHover hover;
    private InventoryUI owner;
    public SkillData Skill { get; private set; }
    public void Bind(InventoryUI ui, SkillData data)
    {
        owner = ui; Skill = data; hover.Bind(ui, data);
        button.onClick.AddListener(Select); Refresh();
    }
    private void Select() { owner.Select(Skill.id); }
    public void Refresh()
    {
        nameText.text = Skill.displayName; rarityBorder.color = InventoryPresentation.RarityColor(Skill.rarity);
        selectedFrame.gameObject.SetActive(owner.SelectedId == Skill.id);
        icon.sprite = Skill.icon; icon.enabled = Skill.icon != null;
        marker.text = InventoryPresentation.Marker(Skill); marker.enabled = !icon.enabled;
        marker.color = Skill.primaryType == SkillDamageType.Physical ? new Color(1,.79f,.34f) : new Color(.2f,1,.85f);
        badge.text = owner.Player.Equipped(0)?.id == Skill.id ? "E" : owner.Player.Equipped(1)?.id == Skill.id ? "R" : "";
    }
}
