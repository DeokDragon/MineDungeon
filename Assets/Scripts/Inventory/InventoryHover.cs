using UnityEngine;
using UnityEngine.EventSystems;

public sealed class InventoryHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private InventoryUI owner;
    [SerializeField, TextArea] private string note;
    [SerializeField] private int equippedSlot = -1;
    [SerializeField] private int statCardIndex = -1;
    [SerializeField] private int feedbackIndex = -1;
    private SkillData skill;
    private MaterialData material;
    public void Bind(InventoryUI ui, SkillData value) { owner = ui; skill = value; material=null; }
    public void BindMaterial(InventoryUI ui,MaterialData value){owner=ui;material=value;skill=null;}
    public void OnPointerEnter(PointerEventData eventData) { if(owner==null)return; if(feedbackIndex>=0){owner.SetGrowthButtonPointer(feedbackIndex,true,false);return;} if(statCardIndex>=0){owner.SetStatCardHover(statCardIndex,true);return;} if(material!=null)owner.HoverMaterial((RectTransform)transform,material);else owner.Hover((RectTransform)transform, skill, note, equippedSlot); }
    public void OnPointerExit(PointerEventData eventData) { if(owner==null)return; if(feedbackIndex>=0){owner.SetGrowthButtonPointer(feedbackIndex,false,false);return;} if(statCardIndex>=0)owner.SetStatCardHover(statCardIndex,false);else owner.Leave((RectTransform)transform); }
    public void OnPointerDown(PointerEventData eventData) { if(owner!=null && feedbackIndex>=0 && eventData.button==PointerEventData.InputButton.Left) owner.SetGrowthButtonPointer(feedbackIndex,true,true); }
    public void OnPointerUp(PointerEventData eventData) { if(owner!=null && feedbackIndex>=0 && eventData.button==PointerEventData.InputButton.Left) owner.ReleaseGrowthButton(feedbackIndex); }
    private void OnDisable() { if(owner!=null && feedbackIndex>=0) owner.ClearGrowthButtonPointer(feedbackIndex); }
}
