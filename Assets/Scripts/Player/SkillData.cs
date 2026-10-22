using UnityEngine;

public enum PlayerSkillKind { EarthSmash, DrillDash, TealBeam, OreBlast }
public enum SkillDamageType { Physical, Magic }

[CreateAssetMenu(menuName = "Mine Dungeon/Player Skill")]
public sealed class SkillData : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;
    public ItemRarity rarity = ItemRarity.Common;
    public PlayerSkillKind kind;
    public SkillDamageType primaryType;
    [Tooltip("초기 테스트 기본 피해. 최종 밸런스 아님.")]
    [Min(0)] public float baseDamage;
    // Retain old serialized values for asset compatibility; no longer used in damage or UI.
    [HideInInspector] public float physicalCoefficient, magicCoefficient;
    [Min(0)] public float cooldown, windup;
    [Header("Shape / motion (world units and seconds)")]
    [Min(0)] public float radius, range, width, centerOffset;
    [Min(.001f)] public float dashDuration = .25f;
    [Min(0)] public float flightDuration = .25f, fuseDuration = .6f, effectDuration = .15f;
    public int Damage(float physical, float magic) => PlayerSkillAttackPower.CalculateDamage(
        baseDamage, primaryType == SkillDamageType.Physical ? physical : magic);
}
