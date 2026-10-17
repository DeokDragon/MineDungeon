using UnityEngine;
using UnityEngine.EventSystems;

public sealed class InventoryHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private InventoryUI owner;
    [SerializeField, TextArea] private string note;
    [SerializeField] private int equippedSlot = -1;
    private SkillData skill;
    public void Bind(InventoryUI ui, SkillData value) { owner = ui; skill = value; }
    public void OnPointerEnter(PointerEventData eventData) { if (owner != null) owner.Hover((RectTransform)transform, skill, note, equippedSlot); }
    public void OnPointerExit(PointerEventData eventData) { if (owner != null) owner.Leave((RectTransform)transform); }
}
