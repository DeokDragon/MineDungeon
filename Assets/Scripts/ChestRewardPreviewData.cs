using System;
using UnityEngine;

// Shared entry preview and automatic clear settlement table.
[CreateAssetMenu(menuName="Mine Dungeon/Chest Reward Preview (Test)")]
public sealed class ChestRewardPreviewData : ScriptableObject
{
    [Serializable] public sealed class Entry
    {
        public SkillData skill;
        public MaterialData material;
        public int displayOrder;
        [Range(0,100)] public float chancePercent;
        [Min(1)] public int minimum=1,maximum=1;
        public bool Valid => (skill!=null)!=(material!=null) && minimum>0&&maximum>=minimum&&
            !float.IsNaN(chancePercent)&&chancePercent>=0&&chancePercent<=100;
        public string Name=>skill!=null?skill.displayName:material!=null?material.displayName:"미설정";
        public Sprite Icon=>skill!=null?skill.icon:material!=null?material.icon:null;
        public ItemRarity Rarity=>skill!=null?skill.rarity:material!=null?material.rarity:ItemRarity.Common;
        public string Tooltip=> $"<b>{Name}</b>\n{InventoryPresentation.RarityName(Rarity)} / {(skill!=null?"스킬":"재료")}\n\n"+
            (skill!=null?InventoryPresentation.SkillEffectDescription(skill):material!=null?material.description:"")+
            $"\n\n수량: {minimum}~{maximum}\n클리어 1회 기준: {chancePercent:0.##}%\n항목별 독립 추첨 · 중복 보유 스킬은 추가 지급 없음";
    }
    public Entry[] entries=Array.Empty<Entry>();
}
