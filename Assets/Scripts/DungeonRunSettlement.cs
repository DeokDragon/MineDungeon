using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class DungeonResultRecord
{
    public string playId, floorName;
    public DungeonFloorData floor;
    public int difficulty, usedRevives, maxRevives, startLevel, startExperience, finalLevel, finalExperience;
    public float seconds;
    public long damageDealt, damageTaken, healed, kills, gold, experience;
    public bool cleared, settled;
    public readonly List<InventoryPresentation.Item> items=new List<InventoryPresentation.Item>();
}

public partial class DungeonRunManager
{
    public DungeonResultRecord Result { get; private set; }
    public string LastSettledPlayId { get; internal set; }
    public bool IsTracking { get; private set; }
    public bool IsEnding => pendingClear || pendingFailure || settling;
    private bool pendingClear, pendingFailure, settling, reviveInFlight;
    private int completedRevives;
    private DungeonRoomManager trackedRooms;
    private DungeonFloorData retryFloor;
    private int retryDifficulty;

    public void BeginTracking(DungeonRoomManager rooms)
    {
        var growth=GetComponent<PlayerProgression>(); trackedRooms=rooms;
        Result=new DungeonResultRecord { playId=Guid.NewGuid().ToString("N"),floor=rooms.CurrentFloor,
            floorName=rooms.CurrentFloor.displayName,difficulty=rooms.CurrentDifficulty,maxRevives=maxRevives,
            startLevel=growth.Level,startExperience=growth.Experience };
        IsTracking=true;
    }
    private void Update()
    {
        if(IsTracking && trackedRooms!=null && !trackedRooms.IsTransitioning) Result.seconds+=Time.deltaTime;
    }
    public void RecordDamageDealt(int amount) { if(IsTracking) Result.damageDealt=RewardInventorySession.SafeAdd(Result.damageDealt,amount); }
    public void RecordDamageTaken(int amount) { if(IsTracking) Result.damageTaken=RewardInventorySession.SafeAdd(Result.damageTaken,amount); }
    public void RecordHealing(int amount) { if(IsTracking) Result.healed=RewardInventorySession.SafeAdd(Result.healed,amount); }
    public void RecordKill() { if(IsTracking) Result.kills=RewardInventorySession.SafeAdd(Result.kills,1); }
    public void RecordMaterial(MaterialData data,long amount)
    {
        if((!IsTracking&&!settling)||Result==null||data==null||amount<=0)return;
        var row=Result.items.Find(i=>i.Material!=null&&i.Material.id==data.id);
        if(row!=null)row.Quantity=RewardInventorySession.SafeAdd(row.Quantity,amount);
        else Result.items.Add(new InventoryPresentation.Item{Material=data,Quantity=amount,Order=Result.items.Count});
    }
    public void NotifyRevived() { if(reviveInFlight) { completedRevives++;reviveInFlight=false; } }
    private void LateUpdate()
    {
        // Resolve after all normal-frame combat callbacks. A clear always wins this frame.
        if(IsRunEnded||(!pendingClear&&!pendingFailure))return;
        bool clear=pendingClear;pendingClear=pendingFailure=false;
#if UNITY_EDITOR
        if (EditorSelectedMapTest) { EditorFinishSelectedMapTest(clear); return; }
#endif
        IsRunCleared=clear;IsRunFailed=!clear;IsTracking=false;
        if(clear&&reviveInFlight){remainingRevives=Mathf.Min(maxRevives,remainingRevives+1);reviveInFlight=false;}
        var player=FindFirstObjectByType<PlayerHealth>();
        if(player)player.FinishDungeon(clear);
        if(trackedRooms)trackedRooms.StopRoomCombat();
        var save=CharacterSaveService.Current;
        if(Result!=null&&!Result.settled)
        {
            settling=true;save?.BeginTransaction();
            try
            {
                if(clear)GrantClearRewards(Result.floor.GetDifficulty(Result.difficulty)?.chestPreview);
                var reward=GetComponent<RewardInventorySession>();var growth=GetComponent<PlayerProgression>();
                Result.cleared=clear;Result.usedRevives=completedRevives;
                Result.gold=reward.RunMoney;Result.experience=reward.RunRewardExperience;
                Result.finalLevel=growth.Level;Result.finalExperience=growth.Experience;
                Result.settled=true;LastSettledPlayId=Result.playId;
            }
            finally{settling=false;save?.EndTransaction();}
            // Snapshot contains both the granted inventory and settlement marker.
            save?.Flush();
        }
        ResourcesChanged?.Invoke();RunEnded?.Invoke();
    }
    private void GrantClearRewards(ChestRewardPreviewData table)
    {
        if(table==null)return;
        foreach(var row in table.entries.Where(e=>e!=null&&e.Valid).OrderBy(e=>e.displayOrder))
        {
            if(row.chancePercent<=0 || (row.chancePercent<100&&UnityEngine.Random.value*100>=row.chancePercent))continue;
            if(row.skill!=null)
            {
                if(GetComponent<SkillInventorySession>().Register(row.skill))Result.items.Add(new InventoryPresentation.Item{Skill=row.skill,Quantity=1,Order=Result.items.Count});
            }
            else
            {
                long span=(long)row.maximum-row.minimum+1;
                int count=(int)Math.Min(row.maximum,row.minimum+(long)(UnityEngine.Random.value*span));
                GetComponent<RewardInventorySession>().GrantMaterial(row.material,count);
            }
        }
    }
    public bool RetryDungeon()
    {
        if(!IsRunEnded||Result==null||IsRestarting||CharacterSaveService.Current==null||!CharacterSaveService.Current.Flush())return false;
        retryFloor=Result.floor;retryDifficulty=Result.difficulty;
        if(ReturnToMain())return true;
        retryFloor=null;return false;
    }
    public void TryStartQueuedDungeon(DungeonRoomManager rooms)
    {
        if(retryFloor==null)return;
        StartCoroutine(StartQueuedDungeon(rooms));
    }
    private System.Collections.IEnumerator StartQueuedDungeon(DungeonRoomManager rooms)
    {
        while(IsRestarting)yield return null;
        var floor=retryFloor;int difficulty=retryDifficulty;retryFloor=null;
        if(rooms && floor && rooms.StartDungeon(floor,difficulty))
            FindFirstObjectByType<MineEntranceUI>()?.CompleteEntry();
    }
}
