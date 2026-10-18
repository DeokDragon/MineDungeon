using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MineRewardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private InventoryUI inventory;
    private string description;
    public void Bind(InventoryUI target,string text){inventory=target;description=text;}
    public void OnPointerEnter(PointerEventData e){inventory.ShowExternalTooltip((RectTransform)transform,description);}
    public void OnPointerExit(PointerEventData e){inventory.Leave((RectTransform)transform);}
}
