using System.Collections;
using UnityEngine;
public sealed class CrystalMonsterAI : NormalMonsterAI
{
    // Visuals read the attack's existing snapshot; gameplay retains direction ownership.
    public Vector2 VisualAttackDirection => direction;
    [SerializeField] float projectileSpeed=7f, projectileDiameter=.35f, projectileRange=7.5f, retreatDistance=2.2f, retreatSpeed=1.8f;
    protected override void IdleMove(Vector2 delta)
    { if(Physics2D.Linecast(rb.position,target.transform.position,wallLayer)) base.IdleMove(delta); else if(Time.time<nextAttack && delta.magnitude<=retreatDistance) Move(-delta.normalized,retreatSpeed); else if(delta.magnitude>attackDistance) base.IdleMove(delta); else rb.linearVelocity=Vector2.zero; }
    protected override IEnumerator Attack()
    {
        float radius=projectileDiameter*.5f;
        yield return Warn(origin,radius,true,WallDistance(origin,direction,radius,projectileRange)); ClearWarning();
        StartCoroutine(Projectile(origin,direction));
        yield return Recover(recoveryDuration);
    }
    IEnumerator Projectile(Vector2 p,Vector2 dir)
    {
        var visual=Display(p,projectileDiameter*.5f); visual.SetProgress(1); float traveled=0;
        while(traveled<projectileRange)
        {
            yield return new WaitForFixedUpdate();
            float step=Mathf.Min(projectileSpeed*Time.fixedDeltaTime,projectileRange-traveled);
            float safe=WallDistance(p,dir,projectileDiameter*.5f,step);
            bool hit=SweepPlayer(p,dir,projectileDiameter*.5f,step,out float distance);
            if(hit && distance<=safe) { p+=dir*distance; target.TryTakeDamage(attackDamage); if(target.IsDead) yield break; break; }
            p+=dir*safe; traveled+=safe; visual.transform.position=p;
            if(safe<step) break;
        }
        if(visual) Destroy(visual.gameObject); Impact(p,.25f,false);
    }
}
