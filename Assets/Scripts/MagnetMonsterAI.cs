using System.Collections;
using UnityEngine;
public sealed class MagnetMonsterAI : NormalMonsterAI
{
    // Visuals read the existing attack snapshot; destination and damage remain AI-owned.
    public Vector2 VisualAttackDirection => direction;
    [SerializeField] float warningRadius=2f, damageRadius=1.8f;
    [SerializeField,Range(0,1)] float slowFraction=.3f;
    [SerializeField] float slowDuration=1.5f;
    protected override IEnumerator Attack()
    {
        yield return Warn(destination,warningRadius); ClearWarning();
        if(HitArea(destination,damageRadius) && target && !target.IsDead) target.GetComponent<PlayerMovement>()?.ApplySlow(slowFraction,slowDuration,warningMaterial);
        if(target.IsDead) yield break;
        Impact(destination,damageRadius,false);
        yield return Recover(recoveryDuration);
    }
}
