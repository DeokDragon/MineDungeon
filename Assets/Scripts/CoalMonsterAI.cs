using System.Collections;
using UnityEngine;
public sealed class CoalMonsterAI : NormalMonsterAI
{
    [SerializeField] float warningRadius=2.2f, damageRadius=2f;
    protected override IEnumerator Attack()
    {
        StartCoroutine(BodyGlow(warningDuration+recoveryDuration,true));
        yield return Warn(origin,warningRadius); ClearWarning();
        HitArea(origin,damageRadius); if(target.IsDead) yield break; Impact(origin,damageRadius);
        yield return Recover(recoveryDuration);
    }
}
