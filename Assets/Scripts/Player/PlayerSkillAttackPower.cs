using UnityEngine;

// Shared attack-stat adapter. Existing virtual properties remain equipment extension points.
public class PlayerSkillAttackPower : MonoBehaviour
{
    [SerializeField, Min(0)] private float physicalAttack = 7f;
    [SerializeField, Min(0)] private float magicAttack = 6f;
    private PlayerProgression Growth => DungeonRunManager.Instance != null ? DungeonRunManager.Instance.GetComponent<PlayerProgression>() : null;
    public float BasePhysicalAttack => physicalAttack;
    public float BaseMagicAttack => magicAttack;
    public virtual float PhysicalAttack => physicalAttack + (Growth != null ? Growth.PhysicalAttackBonus : 0f);
    public virtual float MagicAttack => magicAttack + (Growth != null ? Growth.MagicAttackBonus : 0f);

    public static int CalculateDamage(float baseDamage, float attackStat)
        => RoundDamage(UnroundedDamage(baseDamage, attackStat));

    public static double UnroundedDamage(float baseDamage, float attackStat)
    {
        if (float.IsNaN(baseDamage) || float.IsInfinity(baseDamage) || baseDamage < 0 ||
            float.IsNaN(attackStat) || float.IsInfinity(attackStat) || attackStat < 0) return 0;
        return (double)baseDamage * (1d + (double)attackStat / 100d);
    }
    public static int RoundDamage(double damage)
    {
        if (double.IsNaN(damage) || damage < 0) return 0;
        // Round once at the damage boundary: exact .5 always rounds upward, never to even.
        return (int)System.Math.Min(int.MaxValue, System.Math.Floor(damage + .5d));
    }
}
