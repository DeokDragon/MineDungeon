using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-1100)]
public sealed class DungeonResultUI : MonoBehaviour
{
    [SerializeField] private DungeonResultView view;
    [SerializeField] private DungeonRoomManager rooms;
    [SerializeField] private PlayerHealth player;
    [SerializeField] private PauseMenu pauseMenu;
    private DungeonRunManager run;
    private InventoryUI inventory;
    private DungeonResultRecord record;
    private readonly List<ResultItemCard> cards=new List<ResultItemCard>();
    private bool animating, moving;
    private float elapsed;
    private int escapeFrame=-1;
    private static DungeonResultUI active;
    public bool IsShowing { get; private set; }
    public static bool BlocksPause => active!=null && (active.IsShowing || active.escapeFrame==Time.frameCount);
    public event System.Action Shown;
    private void Start()
    {
        active=this;run=DungeonRunManager.Instance;inventory=GetComponent<InventoryUI>();
        view.overlay.SetActive(false);view.reopen.SetActive(false);view.cardTemplate.gameObject.SetActive(false);
        view.closeButton.onClick.AddListener(Close);view.reopenButton.onClick.AddListener(Reopen);
        view.retryButton.onClick.AddListener(Retry);view.lobbyButton.onClick.AddListener(ReturnToLobby);
        view.saveRetryButton.onClick.AddListener(()=>CharacterSaveService.Current?.Flush());
        view.scroll.onValueChanged.AddListener(_=>inventory?.HideTooltip());
        run.RunEnded+=ShowResult;if(run.IsRunEnded)ShowResult();
    }
    private void ShowResult()
    {
        if(record!=null)return;record=run.Result;if(record==null)return;
        pauseMenu.EnterResultState();inventory.Close();rooms.StopRoomCombat();StoneCombatBurst.ClearNonFinale();
        view.eyebrow.text=record.cleared?"CHAMBER CONQUERED":"THE MINE CLAIMS ANOTHER";
        view.title.text=record.cleared?"던전 클리어":"던전 실패";
        view.title.color=record.cleared?new Color(1,.82f,.4f):new Color(.95f,.3f,.35f);
        view.subtitle.text=record.cleared?"DUNGEON CLEAR":"DUNGEON FAILED";
        view.subtitle.color=record.cleared?new Color(.17f,.95f,.9f):Color.gray;
        view.floor.text=$"◆ {record.floor.floorNumber}층 · {record.floorName} [{DungeonFloorData.DifficultyName(record.difficulty)}] ◆";
        view.failureNote.text=record.cleared?"":"이번 탐험에서 획득한 보상은 유지됩니다.";
        view.itemCount.text=$"획득 아이템  ·  {record.items.Count}종";view.empty.gameObject.SetActive(record.items.Count==0);
        foreach(var item in record.items){var card=Instantiate(view.cardTemplate,view.itemContent);card.Bind(item,inventory,player.GetComponent<PlayerSkills>());card.gameObject.SetActive(true);cards.Add(card);}
        view.timeLabel.text=record.cleared?"클리어 시간 / CLEAR TIME":"진행 시간 / TIME SURVIVED";
        view.statistics[0].text=$"{(int)record.seconds/60:00}:{(int)record.seconds%60:00}";
        view.statistics[1].text=$"<color=#FFD166>{record.damageDealt:N0}</color>";
        view.statistics[2].text=$"<color=#FF6B4A>{record.damageTaken:N0}</color>";
        view.statistics[3].text=$"<color=#4CD137>{record.healed:N0}</color>";
        view.statistics[4].text=$"{record.kills:N0} 마리";
        view.statistics[5].text=$"{record.usedRevives} / {record.maxRevives}";
        view.retryButton.interactable=view.lobbyButton.interactable=false;
        elapsed=0;animating=true;Open();Shown?.Invoke();RenderAnimation();
    }
    private void Open(){IsShowing=true;view.overlay.SetActive(true);view.reopen.SetActive(false);view.transform.SetAsLastSibling();}
    public void Close(){if(!IsShowing)return;animating=false;elapsed=2.1f;IsShowing=false;view.overlay.SetActive(false);view.reopen.SetActive(true);inventory.HideTooltip();escapeFrame=Time.frameCount;}
    public void Reopen(){if(record==null||pauseMenu.IsPaused)return;animating=false;elapsed=2.1f;Open();RenderAnimation();}
    private void Update()
    {
        if(record==null)return;
        Fit();
        if(IsShowing && Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame){Close();return;}
        if(animating){elapsed=Mathf.Min(2.1f,elapsed+Time.unscaledDeltaTime);if(elapsed>=2.1f)animating=false;RenderAnimation();}
        var save=CharacterSaveService.Current;
        bool failed=save==null||save.HasSaveError;
        bool pending=save!=null&&save.IsPending;
        view.status.text=failed?"저장 실패 · 재시도해 주세요":pending?"저장 중":"저장 완료";
        view.saveRetryButton.gameObject.SetActive(failed);
        view.retryButton.interactable=view.lobbyButton.interactable=!animating&&!pending&&!failed&&!moving;
    }
    private void RenderAnimation()
    {
        float intro=Mathf.Clamp01(elapsed/.4f),appear=Mathf.Clamp01((elapsed-.4f)/.6f),count=Mathf.Clamp01((elapsed-1f)/.6f),slide=Mathf.SmoothStep(0,1,Mathf.Clamp01((elapsed-1.6f)/.5f));
        view.overlayGroup.alpha=intro;view.titleRoot.anchoredPosition=new Vector2(0,Mathf.Lerp(0,view.content.rect.height*.389f,appear));
        view.rewardsGroup.alpha=appear;view.recordsGroup.alpha=slide;
        view.rewardsRoot.anchoredPosition=new Vector2(Mathf.Lerp(0,-view.content.rect.width*.243f,slide),-view.content.rect.height*.12346f);
        view.recordsRoot.anchoredPosition=new Vector2(Mathf.Lerp(view.content.rect.width*.29f,view.content.rect.width*.243f,slide),-view.content.rect.height*.12346f);
        for(int i=0;i<cards.Count;i++)cards[i].gameObject.SetActive(elapsed>=1f && count>=(float)i/Mathf.Max(1,cards.Count));
        view.gold.text=$"획득 골드  +{(long)(record.gold*(double)count):N0} G";
        view.experience.text=$"획득 경험치  +{(long)(record.experience*(double)count):N0} EXP";
        var growth=run.GetComponent<PlayerProgression>();int level=record.startLevel;long xp=record.startExperience;
        long earned=record.finalExperience-record.startExperience;
        for(int l=record.startLevel;l<record.finalLevel;l++)earned+=growth.RequiredAt(l);
        xp+=(long)(System.Math.Max(0,earned)*(double)count);
        while(level<growth.MaximumLevel && growth.RequiredAt(level)>0 && xp>=growth.RequiredAt(level)){xp-=growth.RequiredAt(level);level++;}
        if(count>=1){level=record.finalLevel;xp=record.finalExperience;}
        float fraction=level>=growth.MaximumLevel?1:Mathf.Clamp01((float)xp/Mathf.Max(1,growth.RequiredAt(level)));
        view.level.text=level>=growth.MaximumLevel?$"Lv. {level} · MAX":$"Lv. {level} · EXP {fraction*100:0.0}% ({xp:N0} / {growth.RequiredAt(level):N0})";
        view.experienceFill.rectTransform.anchorMax=new Vector2(fraction,1);
    }
    private void Fit()
    {
        var canvas=(RectTransform)transform;
        var size=new Vector2(Mathf.Min(1440,canvas.rect.width-48),Mathf.Min(810,canvas.rect.height-48));
        if(view.content.sizeDelta==size)return;
        view.content.sizeDelta=size;view.titleRoot.sizeDelta=new Vector2(size.x*.90f,size.y*.2099f);
        view.rewardsRoot.sizeDelta=view.recordsRoot.sizeDelta=new Vector2(size.x*.4583f,size.y*.7037f);
        var grid=view.itemContent.GetComponent<UnityEngine.UI.GridLayoutGroup>();
        float width=(size.x*.4583f*.9f*.965f-35)/4;grid.cellSize=new Vector2(width,Mathf.Min(110,width));
        RenderAnimation();
    }
    public void Retry(){if(!IsShowing||!view.retryButton.interactable)return;moving=run.RetryDungeon();}
    public void ReturnToLobby(){if(!IsShowing||!view.lobbyButton.interactable)return;moving=run.ReturnToMain();}
    private void OnDestroy(){if(run!=null)run.RunEnded-=ShowResult;if(active==this)active=null;inventory?.HideTooltip();}
}
