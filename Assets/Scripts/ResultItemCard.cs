using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class ResultItemCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image icon, border;
    public TMP_Text label, quantity, marker;
    private InventoryUI inventory;
    private string description;
    public void Bind(InventoryPresentation.Item item,InventoryUI tooltip,PlayerSkills player)
    {
        inventory=tooltip;label.text=item.Name;quantity.text="x"+item.Quantity.ToString("N0");border.color=InventoryPresentation.RarityColor(item.Rarity);
        var sprite=item.Skill!=null?item.Skill.icon:item.Material.icon; icon.sprite=sprite;icon.enabled=sprite!=null;
        marker.text=sprite==null?(item.Skill!=null?InventoryPresentation.Marker(item.Skill):"◆"):"";
        description=item.Material!=null?InventoryPresentation.MaterialTooltip(item.Material,item.Quantity).Replace("보유 수량","획득 수량"):
            $"<b>{item.Skill.displayName}</b>\n{InventoryPresentation.RarityName(item.Rarity)} / 스킬 · {InventoryPresentation.TypeName(item.Skill)}\n\n"+
            InventoryPresentation.SkillEffectDescription(item.Skill)+$"\n쿨타임 {item.Skill.cooldown:0.##}초\n예상 피해 {player.EstimatedDamage(item.Skill)}";
        description+="\n\n획득한 아이템";
    }
    public void OnPointerEnter(PointerEventData _) { inventory?.ShowExternalTooltip((RectTransform)transform,description); }
    public void OnPointerExit(PointerEventData _) { inventory?.Leave((RectTransform)transform); }
    private void OnDisable() { inventory?.Leave((RectTransform)transform); }
}
