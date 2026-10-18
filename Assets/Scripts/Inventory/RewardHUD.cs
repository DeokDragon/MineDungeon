using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RewardHUD : MonoBehaviour
{
    [System.Serializable] private sealed class Row
    {
        public RectTransform rect;
        public CanvasGroup group;
        public Image icon;
        public TMP_Text label;
        [System.NonSerialized] public float elapsed;
    }
    private struct Notice { public MaterialData data; public long amount; }
    [SerializeField] private TMP_Text hudMoney, inventoryMoney;
    [SerializeField] private Row[] rows;
    [SerializeField,Min(0)] private float holdDuration=3f,fadeDuration=.3f;
    [SerializeField,Min(1)] private float rowSpacing=64f,moveSpeed=12f;
    private readonly Queue<Notice> pending=new Queue<Notice>();
    private readonly List<Row> visible=new List<Row>();
    private RewardInventorySession rewards;
    private DungeonRunManager run;
    public int VisibleCount => visible.Count;
    public int PendingCount => pending.Count;
    private void Start()
    {
        run=DungeonRunManager.Instance;rewards=run.GetComponent<RewardInventorySession>();
        rewards.Changed+=RefreshMoney;rewards.MaterialGranted+=Enqueue;rewards.ChallengeStarted+=Clear;
        run.RunEnded+=Clear;Clear();RefreshMoney();
    }
    private void RefreshMoney()
    {
        if(hudMoney!=null)hudMoney.text=$"돈 {rewards.Money:N0}";
        if(inventoryMoney!=null)inventoryMoney.text=$"보유 돈: {rewards.Money:N0}";
    }
    private void Enqueue(MaterialData data,long amount)
    {
        if(run.IsRunEnded||run.IsRestarting)return;
        pending.Enqueue(new Notice{data=data,amount=amount});FillSlots();
    }
    private void FillSlots()
    {
        while(pending.Count>0&&visible.Count<Mathf.Min(5,rows.Length))
        {
            Row free=null;foreach(var row in rows)if(!visible.Contains(row)){free=row;break;}
            if(free==null)return;
            var n=pending.Dequeue();free.elapsed=0;free.group.alpha=1;
            free.label.text=$"{n.data.displayName} ×{n.amount:N0}";free.icon.sprite=n.data.icon;free.icon.enabled=n.data.icon!=null;
            free.rect.anchoredPosition=Vector2.zero;free.rect.gameObject.SetActive(true);visible.Insert(0,free);
        }
    }
    private void Update()
    {
        float dt=Time.timeScale>0?Time.deltaTime:0;
        if(dt<=0)return;
        for(int i=visible.Count-1;i>=0;i--)
        {
            var row=visible[i];row.elapsed+=dt;
            row.group.alpha=row.elapsed<=holdDuration?1:fadeDuration>0?Mathf.Clamp01(1-(row.elapsed-holdDuration)/fadeDuration):0;
            if(row.elapsed>=holdDuration+fadeDuration){row.rect.gameObject.SetActive(false);visible.RemoveAt(i);}
        }
        FillSlots();
        for(int i=0;i<visible.Count;i++)
            visible[i].rect.anchoredPosition=Vector2.Lerp(visible[i].rect.anchoredPosition,new Vector2(0,-i*rowSpacing),1-Mathf.Exp(-moveSpeed*dt));
    }
    private void Clear()
    {
        pending.Clear();visible.Clear();if(rows!=null)foreach(var row in rows){row.elapsed=0;row.group.alpha=0;row.rect.gameObject.SetActive(false);}
    }
    private void OnDisable(){Clear();}
    private void OnDestroy()
    {
        if(rewards!=null){rewards.Changed-=RefreshMoney;rewards.MaterialGranted-=Enqueue;rewards.ChallengeStarted-=Clear;}
        if(run!=null)run.RunEnded-=Clear;
    }
}
