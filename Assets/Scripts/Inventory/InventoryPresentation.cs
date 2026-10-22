using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }
public enum InventoryCategory { All, Weapon, Upper, Lower, Accessory, Skill, Material }
public enum InventorySort { Recent, Name, Rarity, Category, Type, Cooldown, Damage, Quantity }

public static class InventoryPresentation
{
    public static readonly string[] PassiveTitles = {
        "물약 회복량 +20%", "일반 전투 방 클리어 회복", "조건부 체력 재생",
        "기본 공격 사거리 +15%", "전방 충격파", "3회 직접 적중 후 강화",
        "마법 스킬 범위 +20%", "마법 스킬 쿨타임 -15%", "최초 유효 적중 추가 폭발"
    };
    public static readonly string[] PassiveDetails = {
        "체력 20P · 자동 해금\n물약 기본 회복량 +20%. 최종 회복 요청량에서 한 번 올림합니다. 다른 회복에는 적용하지 않습니다.",
        "체력 50P · 자동 해금\n일반 전투 방 클리어 시 최대 HP의 10% 회복. 방 인스턴스당 1회. 빈 방·보스 방은 제외합니다.",
        "체력 100P · 자동 해금\n살아 있는 적이 있는 현재 방에서 실제 피해 후 5초 뒤 초당 최대 HP의 1% 재생. 방당 20% 한도. 최초 입장·부활 후에도 5초 대기하며 방 이동·부활 시 한도가 초기화됩니다.",
        "물리 20P · 자동 해금\n곡괭이 기본 공격 사거리 +15%. 충격파 범위에는 적용하지 않습니다.",
        "물리 50P · 자동 해금\n직접 유효 적중한 기본 공격당 충격파 1회. 전방 90도·사거리 2.0. 반올림 전 기본 공격 피해의 30%. 직접 대상도 생존 시 추가 피해를 받습니다.\n30%는 초기 테스트값입니다.",
        "물리 100P · 자동 해금\n직접 적중한 기본 공격 3회 후 다음 기본 공격과 충격파 피해 +100%. 다수 적도 공격당 충전 1회. 강화 공격은 재충전하지 않으며 시작 후 빗나감·취소에도 소비됩니다. 방 이동 유지, 사망·부활 초기화.",
        "마법 20P · 자동 해금\n청록 광선 거리·폭 +20%, 불안정 광석 발파 폭발 반경 +20%. 발파 투척 거리와 추가 폭발 범위는 유지합니다.",
        "마법 50P · 자동 해금\n마법 스킬 쿨타임 -15%. 장비 감소율과 가산하며 합계 100% 이상은 0초. 물리 스킬에는 적용하지 않습니다.",
        "마법 100P · 자동 해금\n마법 스킬 최초 유효 대상 중심으로 반경 1.5 추가 폭발. 반올림 전 원스킬 피해의 40%. 광선·발파가 내부 쿨타임 1.5초 공유. 쿨타임 중 첫 적중 시 해당 시전의 기회는 소멸하며 연쇄 폭발은 없습니다.\n40%는 초기 테스트값입니다."
    };
    public static bool PassiveUnlocked(PlayerProgression growth, int index)
    {
        switch (index)
        {
            case 0: return growth.HasHealth20Passive; case 1: return growth.HasHealth50Passive; case 2: return growth.HasHealth100Passive;
            case 3: return growth.HasPhysical20Passive; case 4: return growth.HasPhysical50Passive; case 5: return growth.HasPhysical100Passive;
            case 6: return growth.HasMagic20Passive; case 7: return growth.HasMagic50Passive; case 8: return growth.HasMagic100Passive;
            default: return false;
        }
    }
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
            case InventorySort.Cooldown:ordered=list.OrderBy(e=>e.Skill!=null?(player!=null?player.EffectiveCooldown(e.Skill):e.Skill.cooldown):0);break;
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
            case InventorySort.Cooldown: ordered = list.OrderBy(e => player != null ? player.EffectiveCooldown(e.Skill) : e.Skill.cooldown); break;
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
        return $"<size=120%><b>{s.displayName}</b></size>\n{RarityName(s.rarity)} / 스킬 · {TypeName(s)}\n\n쿨타임  {player.EffectiveCooldown(s):0.###}초     선딜레이  {s.windup:0.##}초\n\n{effect}\n\n기본 피해  {s.baseDamage:0.##}\n<color=#FFCF75>현재 예상 피해  {player.EstimatedDamage(s)}</color>\n적 한 마리 · 1회 사용 기준\n\n{equipped}\n{(editable ? "카드를 선택한 뒤 E 또는 R 슬롯을 클릭하세요.\n반대 슬롯의 스킬이면 서로 교환합니다." : "준비 중에만 변경할 수 있습니다")}";
    }
}
