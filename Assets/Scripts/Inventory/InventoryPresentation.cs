using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }
public enum InventoryCategory { All, Weapon, Upper, Lower, Accessory, Skill, Material }
public enum InventorySort { Recent, Name, Rarity, Category, Type, Cooldown, Damage, Quantity }

public static class InventoryPresentation
{
    public sealed class Item
    {
        public SkillData Skill;
        public MaterialData Material;
        public long Quantity, Order;
        public string Name => Skill != null ? Skill.displayName : Material.displayName;
        public string Id => Skill != null ? Skill.id : Material.id;
        public ItemRarity Rarity => Skill != null ? Skill.rarity : Material.rarity;
        public InventoryCategory Category => Skill != null ? InventoryCategory.Skill : InventoryCategory.Material;
    }
    public static List<Item> QueryItems(SkillInventorySession skills,RewardInventorySession rewards,InventoryCategory category,string search,InventorySort sort,PlayerSkills player)
    {
        var all=new List<Item>();
        if(skills!=null&&(category==InventoryCategory.All||category==InventoryCategory.Skill))
            all.AddRange(skills.Owned.Select(e=>new Item{Skill=e.Skill,Order=e.RegistrationOrder}));
        if(rewards!=null&&(category==InventoryCategory.All||category==InventoryCategory.Material))
            all.AddRange(rewards.Materials.Select(e=>new Item{Material=e.Data,Quantity=e.Quantity,Order=e.RegistrationOrder}));
        var list=all.Where(e=>e.Name.IndexOf(search??"",StringComparison.OrdinalIgnoreCase)>=0);
        IOrderedEnumerable<Item> ordered;
        switch(sort)
        {
            case InventorySort.Name:ordered=list.OrderBy(e=>e.Name,StringComparer.CurrentCulture);break;
            case InventorySort.Rarity:ordered=list.OrderByDescending(e=>e.Rarity);break;
            case InventorySort.Category:ordered=list.OrderBy(e=>e.Category);break;
            case InventorySort.Type:ordered=list.OrderBy(e=>e.Skill!=null?(int)e.Skill.primaryType:0);break;
            case InventorySort.Cooldown:ordered=list.OrderBy(e=>e.Skill!=null?e.Skill.cooldown:0);break;
            case InventorySort.Damage:ordered=list.OrderByDescending(e=>e.Skill!=null?player.EstimatedDamage(e.Skill):0);break;
            case InventorySort.Quantity:ordered=list.OrderByDescending(e=>e.Quantity);break;
            default:ordered=list.OrderByDescending(e=>e.Order);break;
        }
        return ordered.ThenBy(e=>e.Name,StringComparer.CurrentCulture).ThenBy(e=>e.Id,StringComparer.Ordinal).ThenBy(e=>e.Category).ToList();
    }
    public static string MaterialTooltip(MaterialData data,long quantity) => $"<size=120%><b>{data.displayName}</b></size>\n{RarityName(data.rarity)} / 재료\n\n{data.description}\n\n보유 수량: {quantity:N0}";
    public static readonly string[] Categories = { "전체", "무기", "상의", "하의", "액세서리", "스킬", "재료" };
    public static string RarityName(ItemRarity r) => new[] { "일반", "고급", "희귀", "영웅", "전설" }[(int)r];
    public static Color RarityColor(ItemRarity r) => new[] { new Color(.52f,.59f,.65f), new Color(.39f,.77f,.48f), new Color(.34f,.61f,1), new Color(.76f,.43f,.92f), new Color(1f,.78f,.25f) }[(int)r];
    public static string TypeName(SkillData s) => s.primaryType == SkillDamageType.Physical ? "물리" : "마법";
    public static string Marker(SkillData s) => s == null ? "—" : new[] { "I", "II", "III", "IV" }[(int)s.kind];
    public static List<SkillInventorySession.Entry> Query(SkillInventorySession session, InventoryCategory category, string search, InventorySort sort, PlayerSkills player)
    {
        if (session == null || (category != InventoryCategory.All && category != InventoryCategory.Skill)) return new List<SkillInventorySession.Entry>();
        var list = session.Owned.Where(e => e.Skill.displayName.IndexOf(search ?? "", StringComparison.OrdinalIgnoreCase) >= 0);
        IOrderedEnumerable<SkillInventorySession.Entry> ordered;
        switch (sort)
        {
            case InventorySort.Name: ordered = list.OrderBy(e => e.Skill.displayName, StringComparer.CurrentCulture); break;
            case InventorySort.Rarity: ordered = list.OrderByDescending(e => e.Skill.rarity); break;
            case InventorySort.Type: ordered = list.OrderBy(e => e.Skill.primaryType); break;
            case InventorySort.Cooldown: ordered = list.OrderBy(e => e.Skill.cooldown); break;
            case InventorySort.Damage: ordered = list.OrderByDescending(e => player.EstimatedDamage(e.Skill)); break;
            case InventorySort.Category: ordered = list.OrderBy(e => 0); break; // Only owned skills exist in this scope.
            default: ordered = list.OrderByDescending(e => e.RegistrationOrder); break;
        }
        return ordered.ThenBy(e => e.Skill.displayName, StringComparer.CurrentCulture).ThenBy(e => e.Skill.id, StringComparer.Ordinal).ToList();
    }
    public static string SkillEffectDescription(SkillData s, PlayerSkills player = null)
    {
        float range = player != null ? player.EffectiveRange(s) : s.range;
        float width = player != null ? player.EffectiveWidth(s) : s.width;
        float radius = player != null ? player.EffectiveRadius(s) : s.radius;
        string effect;
        switch (s.kind)
        {
            case PlayerSkillKind.EarthSmash: effect = $"전방 {s.centerOffset:0.##} 지점을 중심으로 반경 {radius:0.##} 안의 적을 한 번 타격합니다. 벽 너머에는 닿지 않습니다."; break;
            case PlayerSkillKind.DrillDash: effect = $"{s.dashDuration:0.##}초 동안 최대 {range:0.##} 이동하며 폭 {width:0.##} 경로의 적을 한 번씩 타격합니다. 벽 앞에서 정지합니다."; break;
            case PlayerSkillKind.TealBeam: effect = $"길이 {range:0.##}, 폭 {width:0.##}의 광선으로 적을 관통합니다. 첫 벽에서 차단되며 발사 순간 한 번 타격합니다. 효과 {s.effectDuration:0.##}초."; break;
            default: effect = $"최대 {range:0.##} 거리로 투척합니다. 비행 {s.flightDuration:0.##}초, 착탄 후 {s.fuseDuration:0.##}초 대기 뒤 반경 {radius:0.##} 폭발로 한 번 타격합니다."; break;
        }
        return effect;
    }
    public static string SkillTooltip(SkillData s, PlayerSkills player, bool editable)
    {
        string effect=SkillEffectDescription(s, player);
        string equipped = player.Equipped(0)?.id == s.id ? "E 장착" : player.Equipped(1)?.id == s.id ? "R 장착" : "미장착";
        return $"<size=120%><b>{s.displayName}</b></size>\n{RarityName(s.rarity)} / 스킬 · {TypeName(s)}\n\n쿨타임  {s.cooldown:0.##}초     선딜레이  {s.windup:0.##}초\n\n{effect}\n\n기본 피해  {s.baseDamage:0.##}\n<color=#FFCF75>현재 예상 피해  {player.EstimatedDamage(s)}</color>\n적 한 마리 · 1회 사용 기준\n\n{equipped}\n{(editable ? "카드를 선택한 뒤 E 또는 R 슬롯을 클릭하세요.\n반대 슬롯의 스킬이면 서로 교환합니다." : "준비 중에만 변경할 수 있습니다")}";
    }
}
