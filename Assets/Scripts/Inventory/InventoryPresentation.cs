using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ItemRarity { Common, Uncommon, Rare, Epic }
public enum InventoryCategory { All, Weapon, Upper, Lower, Accessory, Skill, Material }
public enum InventorySort { Recent, Name, Rarity, Category, Type, Cooldown, Damage }

public static class InventoryPresentation
{
    public static readonly string[] Categories = { "전체", "무기", "상의", "하의", "액세서리", "스킬", "재료" };
    public static string RarityName(ItemRarity r) => new[] { "일반", "고급", "희귀", "영웅" }[(int)r];
    public static Color RarityColor(ItemRarity r) => new[] { new Color(.52f,.59f,.65f), new Color(.39f,.77f,.48f), new Color(.34f,.61f,1), new Color(.76f,.43f,.92f) }[(int)r];
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
    public static string SkillTooltip(SkillData s, PlayerSkills player, bool editable)
    {
        string effect;
        switch (s.kind)
        {
            case PlayerSkillKind.EarthSmash: effect = $"전방 {s.centerOffset:0.##} 지점을 중심으로 반경 {s.radius:0.##} 안의 적을 한 번 타격합니다. 벽 너머에는 닿지 않습니다."; break;
            case PlayerSkillKind.DrillDash: effect = $"{s.dashDuration:0.##}초 동안 최대 {s.range:0.##} 이동하며 폭 {s.width:0.##} 경로의 적을 한 번씩 타격합니다. 벽 앞에서 정지합니다."; break;
            case PlayerSkillKind.TealBeam: effect = $"길이 {s.range:0.##}, 폭 {s.width:0.##}의 광선으로 적을 관통합니다. 첫 벽에서 차단되며 발사 순간 한 번 타격합니다. 효과 {s.effectDuration:0.##}초."; break;
            default: effect = $"최대 {s.range:0.##} 거리로 투척합니다. 비행 {s.flightDuration:0.##}초, 착탄 후 {s.fuseDuration:0.##}초 대기 뒤 반경 {s.radius:0.##} 폭발로 한 번 타격합니다."; break;
        }
        string equipped = player.Equipped(0)?.id == s.id ? "E 장착" : player.Equipped(1)?.id == s.id ? "R 장착" : "미장착";
        return $"<size=120%><b>{s.displayName}</b></size>\n{RarityName(s.rarity)} / 스킬 · {TypeName(s)}\n\n쿨타임  {s.cooldown:0.##}초     선딜레이  {s.windup:0.##}초\n\n{effect}\n\n물리 계수 ×{s.physicalCoefficient:0.##}\n마법 계수 ×{s.magicCoefficient:0.##}\n<color=#FFCF75>현재 예상 피해  {player.EstimatedDamage(s)}</color>\n적 한 마리 · 1회 사용 기준\n\n{equipped}\n{(editable ? "카드를 선택한 뒤 E 또는 R 슬롯을 클릭하세요.\n반대 슬롯의 스킬이면 서로 교환합니다." : "준비 중에만 변경할 수 있습니다")}";
    }
}
