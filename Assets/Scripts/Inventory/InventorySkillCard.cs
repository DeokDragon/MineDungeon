using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventorySkillCard : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image rarityBorder, selectedFrame, icon;
    [SerializeField] private TMP_Text nameText, marker, badge;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private InventoryHover hover;
    private InventoryUI owner;
    public SkillData Skill { get; private set; }
    public MaterialData Material { get; private set; }
    public void Bind(InventoryUI ui, SkillData data)
    {
        owner = ui; Skill = data; Material = null; hover.Bind(ui, data);
        button.onClick.AddListener(Select); Refresh();
    }
    public void Bind(InventoryUI ui, MaterialData data)
    {
        owner=ui;Skill=null;Material=data;hover.BindMaterial(ui,data);
        button.onClick.AddListener(Select);Refresh();
    }
    private void Select() { if(Skill!=null)owner.Select(Skill.id);else owner.SelectMaterial(Material.id); }
    public void Refresh()
    {
        if(Material!=null)
        {
            nameText.text=Material.displayName;rarityBorder.color=InventoryPresentation.RarityColor(Material.rarity);
            // Keep the selected material for details, without a persistent selection border.
            selectedFrame.gameObject.SetActive(false);
            icon.sprite=Material.icon;icon.enabled=Material.icon!=null;marker.text="*";marker.enabled=!icon.enabled;marker.color=Color.gray;
            badge.text="";if(quantityText!=null)quantityText.text=owner.MaterialQuantity(Material.id).ToString("N0");return;
        }
        if(quantityText!=null)quantityText.text="";
        nameText.text = Skill.displayName; rarityBorder.color = InventoryPresentation.RarityColor(Skill.rarity);
        selectedFrame.gameObject.SetActive(owner.SelectedId == Skill.id);
        icon.sprite = Skill.icon; icon.enabled = Skill.icon != null;
        marker.text = InventoryPresentation.Marker(Skill); marker.enabled = !icon.enabled;
        marker.color = Skill.primaryType == SkillDamageType.Physical ? new Color(1,.79f,.34f) : new Color(.2f,1,.85f);
        badge.text = owner.Player.Equipped(0)?.id == Skill.id ? "E" : owner.Player.Equipped(1)?.id == Skill.id ? "R" : "";
    }
}
