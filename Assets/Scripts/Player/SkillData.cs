using UnityEngine;

public enum PlayerSkillKind { EarthSmash, DrillDash, TealBeam, OreBlast }
public enum SkillDamageType { Physical, Magic }

[CreateAssetMenu(menuName = "Mine Dungeon/Player Skill")]
public sealed class SkillData : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;
    public PlayerSkillKind kind;
    public SkillDamageType primaryType;
    [Min(0)] public float physicalCoefficient, magicCoefficient;
    [Min(0)] public float cooldown, windup;
    [Header("Shape / motion (world units and seconds)")]
    [Min(0)] public float radius, range, width, centerOffset;
    [Min(.001f)] public float dashDuration = .25f;
    [Min(0)] public float flightDuration = .25f, fuseDuration = .6f, effectDuration = .15f;
    public int Damage(float physical, float magic) => Mathf.Max(0,
        Mathf.RoundToInt(physical * physicalCoefficient + magic * magicCoefficient));
}
