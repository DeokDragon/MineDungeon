using System;
using System.Collections.Generic;
using UnityEngine;

// Session state on the existing persistent run manager. Assets contain definitions only.
[DisallowMultipleComponent]
public sealed class RewardInventorySession : MonoBehaviour
{
    public sealed class Stack
    {
        public MaterialData Data { get; }
        public long Quantity { get; internal set; }
        public long RegistrationOrder { get; }
        internal Stack(MaterialData data,long order){Data=data;RegistrationOrder=order;}
    }
    private readonly List<Stack> materials = new List<Stack>();
    private readonly Dictionary<string,long> runMaterials = new Dictionary<string,long>(StringComparer.Ordinal);
    public IReadOnlyList<Stack> Materials => materials;
    public long Money { get; private set; }
    public long RunMoney { get; private set; }
    public long RunRewardExperience { get; private set; }
    public event Action Changed;
    public event Action<MaterialData,long> MaterialGranted;
    public event Action ChallengeStarted;
    public long Quantity(string id) => Find(id)?.Quantity ?? 0;
    public long RunQuantity(string id) => runMaterials.TryGetValue(id,out var count) ? count : 0;
    private Stack Find(string id) => materials.Find(s=>s.Data.id==id);
    public static long SafeAdd(long a,long b) => b <= 0 ? a : a > long.MaxValue-b ? long.MaxValue : a+b;
    public bool GrantMoney(long amount)
    {
        if(amount<0)return false;
        long next=SafeAdd(Money,amount), accepted=next-Money;
        if(accepted==0)return true;
        Money=next;RunMoney=SafeAdd(RunMoney,accepted);Changed?.Invoke();return true;
    }
    public bool GrantMaterial(MaterialData data,long amount)
    {
        if(data==null||!data.IsValid||amount<=0)return false;
        var stack=Find(data.id);
        if(stack==null){stack=new Stack(data,SkillInventorySession.Instance!=null?SkillInventorySession.Instance.NextRegistrationOrder():materials.Count);materials.Add(stack);}
        long next=SafeAdd(stack.Quantity,amount),accepted=next-stack.Quantity;
        if(accepted==0)return false;
        stack.Quantity=next;runMaterials[data.id]=SafeAdd(RunQuantity(data.id),accepted);
        Changed?.Invoke();MaterialGranted?.Invoke(stack.Data,accepted);return true;
    }
    internal void GrantKillReward(MonsterRewardData reward)
    {
        if(reward==null)return;
        var totals=new Dictionary<string,long>(StringComparer.Ordinal);
        var definitions=new Dictionary<string,MaterialData>(StringComparer.Ordinal);
        if(reward.materials!=null)foreach(var drop in reward.materials)
        {
            int amount=drop!=null?drop.Roll():0;if(amount<=0)continue;
            string id=drop.material.id;totals[id]=SafeAdd(totals.TryGetValue(id,out var old)?old:0,amount);definitions[id]=drop.material;
        }
        GrantMoney(Math.Max(0,reward.money));
        int xp=Math.Max(0,reward.experience);
        RunRewardExperience=SafeAdd(RunRewardExperience,xp);
        GetComponent<PlayerProgression>()?.GrantExperience(xp);
        foreach(var pair in totals)GrantMaterial(definitions[pair.Key],pair.Value);
        Changed?.Invoke();
    }
    // Called only when preparation actually starts a challenge, not scene reload/retry.
    public void BeginChallenge()
    {
        RunMoney=RunRewardExperience=0;runMaterials.Clear();ChallengeStarted?.Invoke();Changed?.Invoke();
    }
}
