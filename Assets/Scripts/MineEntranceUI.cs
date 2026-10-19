using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-1200)]
public sealed class MineEntranceUI : MonoBehaviour
{
    [SerializeField] private DungeonFloorData[] floors;
    [SerializeField] private Transform entrance;
    [SerializeField,Min(.1f)] private float interactionRadius=2.5f;
    [SerializeField] private GameObject lobbyRoot,panel;
    [SerializeField] private PlayerHealth player;
    [SerializeField] private DungeonRoomManager rooms;
    [SerializeField] private InventoryUI inventory;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private TMP_Text prompt,title,requiredText,currentLevelText,entryLabel,notice,previewStatus;
    [SerializeField] private Button closeButton,enterButton;
    [SerializeField] private Button[] floorButtons,difficultyButtons;
    [SerializeField] private Image representative;
    [SerializeField] private RectTransform rewardContent;
    [SerializeField] private ScrollRect rewardScroll;
    [SerializeField] private GameObject rewardCardTemplate;
    private static MineEntranceUI active;
    private DungeonRunManager run;
    private PlayerProgression growth;
    private float noticeUntil;
    private bool entering;
    public bool IsOpen { get; private set; }
    public static bool IsModalOpen => active!=null&&active.IsOpen;
    public int SelectedFloorIndex { get; private set; }
    public int SelectedDifficulty { get; private set; }
    public DungeonFloorData SelectedFloor => floors!=null&&SelectedFloorIndex>=0&&SelectedFloorIndex<floors.Length?floors[SelectedFloorIndex]:null;
    public bool InRange => entrance!=null&&player!=null&&Vector2.Distance(player.transform.position,entrance.position)<=interactionRadius;
    private void Awake(){active=this;panel.SetActive(false);prompt.text="";rewardCardTemplate.SetActive(false);}
    private void Start()
    {
        run=DungeonRunManager.Instance;growth=run.GetComponent<PlayerProgression>();growth.Changed+=Refresh;run.RunEnded+=OnResult;
        closeButton.onClick.AddListener(Close);enterButton.onClick.AddListener(Enter);
        for(int i=0;i<floorButtons.Length;i++){int n=i;floorButtons[i].onClick.AddListener(()=>SelectFloor(n));}
        for(int i=0;i<difficultyButtons.Length;i++){int n=i;difficultyButtons[i].onClick.AddListener(()=>SelectDifficulty(n));}
        rewardScroll.onValueChanged.AddListener(_=>inventory.HideTooltip());Refresh();
    }
    private bool CanInteract => rooms.IsPreparing&&!rooms.IsTransitioning&&!run.IsRunEnded&&!run.IsRestarting&&!player.IsDead&&!pauseMenu.IsPaused&&!inventory.IsOpen&&!entering;
    private static bool Typing()
    {
        var selected=EventSystem.current!=null?EventSystem.current.currentSelectedGameObject:null;
        return selected!=null&&((selected.GetComponent<TMP_InputField>()?.isFocused??false)||(selected.GetComponent<InputField>()?.isFocused??false));
    }
    private void Update()
    {
        if(run==null)return;
        prompt.text=!IsOpen&&CanInteract&&InRange?"[T] 광산 입장":"";
        var keys=Keyboard.current;
        if(IsOpen&&keys!=null)
        {
            if(keys.escapeKey.wasPressedThisFrame){Close();return;}
            if(keys.iKey.wasPressedThisFrame&&!Typing()){Close();inventory.OpenFromExternal();return;}
        }
        else if(keys!=null&&keys.tKey.wasPressedThisFrame&&!Typing())TryOpen();
        if(noticeUntil>0&&Time.unscaledTime>=noticeUntil){notice.text="";noticeUntil=0;}
    }
    public bool TryOpen()
    {
        if(IsOpen||!CanInteract||!InRange||player.GetComponent<PlayerSkills>().IsCasting||player.GetComponent<PlayerMovement>().IsDodging||player.GetComponent<PlayerPotion>().IsUsing||player.GetComponent<PlayerAttack>().IsAttacking)return false;
        IsOpen=true;panel.SetActive(true);panel.transform.SetAsLastSibling();prompt.text="";notice.text="";inventory.SuppressAfterModal();Refresh();return true;
    }
    public void Close()
    {
        if(!IsOpen)return;
        inventory.HideTooltip();panel.SetActive(false);IsOpen=false;
        inventory.SuppressAfterModal();notice.text="";noticeUntil=0;
    }
    private void OnResult(){Close();prompt.text="";}
    public void SelectFloor(int index)
    {
        if(floors==null||index<0||index>=floors.Length)return;
        if(SelectedFloorIndex!=index){SelectedFloorIndex=index;SelectedDifficulty=0;}
        Refresh();
    }
    public void SelectDifficulty(int index){if(index<0||index>3)return;SelectedDifficulty=index;Refresh();}
    private void Hint(string message){notice.text=message;noticeUntil=Time.unscaledTime+1.2f;}
    public void Enter()
    {
        if(!IsOpen||entering||!InRange||!CanInteract)return;
        var setting=SelectedFloor?.GetDifficulty(SelectedDifficulty);
        if(setting==null||!setting.IsPlayable){Hint("준비 중인 던전입니다.");return;}
        if(growth.Level<Mathf.Max(1,setting.requiredLevel)){Hint($"입장하려면 Lv.{Mathf.Max(1,setting.requiredLevel)} 이상이 필요합니다.");return;}
        entering=true;
        if(!rooms.StartDungeon(SelectedFloor,SelectedDifficulty)){entering=false;Hint("입장 상태를 확인해 주세요.");return;}
        Close();lobbyRoot.SetActive(false);prompt.text="";
    }
    private void Refresh()
    {
        if(growth==null)return;
        var floor=SelectedFloor;var setting=floor?.GetDifficulty(SelectedDifficulty);
        title.text=floor!=null?$"{floor.floorNumber}층 · {floor.displayName} — {DungeonFloorData.DifficultyName(SelectedDifficulty)}":"광산 입장";
        int required=setting!=null?Mathf.Max(1,setting.requiredLevel):1;
        requiredText.text=$"입장 필요 레벨: Lv.{required}";
        currentLevelText.text=$"현재 Lv.{growth.Level} / 필요 Lv.{required}";
        bool playable=setting!=null&&setting.IsPlayable;
        entryLabel.text=!playable?"준비 중":growth.Level<required?"입장 불가":"입장하기";
        enterButton.image.color=playable&&growth.Level>=required?new Color(.14f,.48f,.43f):new Color(.24f,.27f,.29f);
        enterButton.interactable=true; // Disabled-looking buttons still explain the restriction.
        representative.sprite=floor!=null?floor.previewImage:null;representative.enabled=representative.sprite!=null;
        for(int i=0;i<floorButtons.Length;i++)
        {
            var f=i<floors.Length?floors[i]:null;floorButtons[i].GetComponentInChildren<TMP_Text>().text=f==null?"준비 중":$"{f.floorNumber}층 · {f.displayName}\n최소 입장 Lv.{Mathf.Max(1,f.GetDifficulty(0)?.requiredLevel??1)}";
            Highlight(floorButtons[i],i==SelectedFloorIndex);
        }
        for(int i=0;i<difficultyButtons.Length;i++)Highlight(difficultyButtons[i],i==SelectedDifficulty);
        inventory.HideTooltip();notice.text="";
        foreach(Transform child in rewardContent)if(child.gameObject!=rewardCardTemplate){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        var table=setting?.chestPreview;
        previewStatus.text=table==null?"클리어 보상표 준비 중":"테스트 보상표 · 클리어 1회 기준 · 항목별 독립 추첨";
        if(floor!=null&&floor.testConfiguration)requiredText.text+=" (테스트 설정)";
        if(table!=null)foreach(var row in table.entries.Where(e=>e!=null&&e.Valid).OrderBy(e=>e.displayOrder))
        {
            var card=Instantiate(rewardCardTemplate,rewardContent);card.name="ChestPreview_"+row.Name;card.SetActive(true);
            card.transform.Find("Name").GetComponent<TMP_Text>().text=row.Name;
            card.transform.Find("Chance").GetComponent<TMP_Text>().text=$"{row.chancePercent:0.##}%";
            var icon=card.transform.Find("Icon").GetComponent<Image>();icon.sprite=row.Icon;icon.enabled=row.Icon!=null;
            card.transform.Find("Marker").GetComponent<TMP_Text>().text=row.Icon!=null?"":row.skill!=null?InventoryPresentation.Marker(row.skill):"*";
            card.transform.Find("Border").GetComponent<Image>().color=InventoryPresentation.RarityColor(row.Rarity);
            card.GetComponent<MineRewardHover>().Bind(inventory,row.Tooltip);
        }
        rewardScroll.horizontalNormalizedPosition=0;
    }
    private static void Highlight(Button button,bool selected)
    {button.image.color=selected?new Color(.12f,.34f,.35f):new Color(.10f,.13f,.17f);var border=button.transform.Find("SelectionBorder");if(border!=null)border.gameObject.SetActive(selected);}
    private void OnDisable(){Close();if(active==this)active=null;}
    private void OnDestroy(){if(growth!=null)growth.Changed-=Refresh;if(run!=null)run.RunEnded-=OnResult;if(active==this)active=null;}
}
