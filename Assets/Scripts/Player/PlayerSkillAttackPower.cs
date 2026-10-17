using UnityEngine;

// Skill-only adapter. A future equipment provider can override these properties.
public class PlayerSkillAttackPower : MonoBehaviour
{
    [SerializeField, Min(0)] private float physicalAttack = 7f;
    [SerializeField, Min(0)] private float magicAttack = 6f;
    public virtual float PhysicalAttack => physicalAttack;
    public virtual float MagicAttack => magicAttack;
}
