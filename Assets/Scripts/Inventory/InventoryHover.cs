using UnityEngine;
using UnityEngine.EventSystems;

public sealed class InventoryHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private InventoryUI owner;
    [SerializeField, TextArea] private string note;
    [SerializeField] private int equippedSlot = -1;
    private SkillData skill;
    private MaterialData material;
    public void Bind(InventoryUI ui, SkillData value) { owner = ui; skill = value; material=null; }
    public void BindMaterial(InventoryUI ui,MaterialData value){owner=ui;material=value;skill=null;}
    public void OnPointerEnter(PointerEventData eventData) { if(owner==null)return; if(material!=null)owner.HoverMaterial((RectTransform)transform,material);else owner.Hover((RectTransform)transform, skill, note, equippedSlot); }
    public void OnPointerExit(PointerEventData eventData) { if (owner != null) owner.Leave((RectTransform)transform); }
}
