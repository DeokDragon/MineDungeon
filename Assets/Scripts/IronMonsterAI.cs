using System.Collections;
using UnityEngine;

public sealed class IronMonsterAI : NormalMonsterAI
{
    [SerializeField] float chargeSpeed=8.5f, chargeWidth=1.2f, chargeDistance=6f, wallRecovery=1.5f;
    readonly MonsterCollisionPass passage=new MonsterCollisionPass();
    protected override IEnumerator Attack()
    {
        float radius=chargeWidth*.5f;
        float length=WallDistance(origin,direction,radius,chargeDistance);
        bool endsAtWall=length<chargeDistance;
        yield return Warn(origin,radius,true,length); ClearWarning();
        // Sweeps own all collision decisions during charge; no global layer changes.
        UnlockPosition(); passage.Begin(this,target);
        float traveled=0, trailTimer=0; bool wall=false;
        while(traveled<length)
        {
            yield return new WaitForFixedUpdate();
            trailTimer-=Time.fixedDeltaTime;
            if(trailTimer<=0) { StartCoroutine(BodyGlow(.15f,false));trailTimer=.08f; }
            float step=Mathf.Min(chargeSpeed*Time.fixedDeltaTime,length-traveled);
            Vector2 p=rb.position;
            float safe=WallDistance(p,direction,radius,step);
            bool playerHit=SweepPlayer(p,direction,radius,step,out float hitDistance);
            if(playerHit && hitDistance<=safe)
            { rb.position=p+direction*Mathf.Max(0,hitDistance-.02f); target.TryTakeDamage(attackDamage); if(target.IsDead) yield break; break; }
            rb.position=p+direction*safe; traveled+=safe;
            if(safe<step) { wall=true; break; }
            if(traveled>=length-.001f) { wall=endsAtWall; break; }
        }
        rb.linearVelocity=Vector2.zero; passage.Restore();
        if(wall) { Impact(rb.position+direction*radius,.6f); StartCoroutine(BodyGlow(wallRecovery,true)); }
        yield return Recover(wall?wallRecovery:recoveryDuration);
    }
    public override void CancelAttacks() { passage.Restore(); base.CancelAttacks(); }
}
