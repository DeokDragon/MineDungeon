using System.Collections;
using UnityEngine;
public sealed class CrystalMonsterAI : NormalMonsterAI
{
    // Visuals read the attack's existing snapshot; gameplay retains direction ownership.
    public Vector2 VisualAttackDirection => direction;
    public enum VisualPhase { Idle, Windup, Recovery }
    public VisualPhase Phase { get; private set; }
    public float PhaseStartedAt { get; private set; }
    public float PhaseDuration { get; private set; }
    CrystalMonsterSpriteAnimation spriteAnimation;
    protected override void Awake() { base.Awake(); spriteAnimation=GetComponent<CrystalMonsterSpriteAnimation>(); }
    void SetVisualPhase(VisualPhase phase,float duration)
    { Phase=phase; PhaseStartedAt=Time.time; PhaseDuration=duration; }
    [SerializeField] float projectileSpeed=7f, projectileDiameter=.35f, projectileRange=7.5f, retreatDistance=2.2f, retreatSpeed=1.8f;
    protected override void IdleMove(Vector2 delta)
    { if(Physics2D.Linecast(rb.position,target.transform.position,wallLayer)) base.IdleMove(delta); else if(Time.time<nextAttack && delta.magnitude<=retreatDistance) Move(-delta.normalized,retreatSpeed); else if(delta.magnitude>attackDistance) base.IdleMove(delta); else rb.linearVelocity=Vector2.zero; }
    protected override IEnumerator Attack()
    {
        float radius=projectileDiameter*.5f;
        SetVisualPhase(VisualPhase.Windup,warningDuration);
        if(spriteAnimation) spriteAnimation.PlayWindup();
        yield return Warn(origin,radius,true,WallDistance(origin,direction,radius,projectileRange)); ClearWarning();
        SetVisualPhase(VisualPhase.Recovery,recoveryDuration);
        StartCoroutine(Projectile(origin,direction));
        yield return Recover(recoveryDuration);
        SetVisualPhase(VisualPhase.Idle,0);
    }
    IEnumerator Projectile(Vector2 p,Vector2 dir)
    {
        // Only the display changes. p/dir and the existing sweeps remain authoritative.
        var sprite=spriteAnimation?spriteAnimation.CreateProjectile(p,dir,Effects()):null;
        var visual=sprite?null:Display(p,projectileDiameter*.5f);
        if(visual) visual.SetProgress(1);
        if(spriteAnimation) spriteAnimation.PlayFire();
        float traveled=0;
        while(traveled<projectileRange)
        {
            yield return new WaitForFixedUpdate();
            float step=Mathf.Min(projectileSpeed*Time.fixedDeltaTime,projectileRange-traveled);
            float safe=WallDistance(p,dir,projectileDiameter*.5f,step);
            bool hit=SweepPlayer(p,dir,projectileDiameter*.5f,step,out float distance);
            if(hit && distance<=safe) { p+=dir*distance; target.TryTakeDamage(attackDamage); if(target.IsDead) yield break; break; }
            p+=dir*safe; traveled+=safe;
            if(sprite) sprite.transform.position=p; else if(visual) visual.transform.position=p;
            if(safe<step) break;
        }
        if(sprite) { sprite.gameObject.SetActive(false); Destroy(sprite.gameObject); }
        if(visual) Destroy(visual.gameObject);
        if(!spriteAnimation || !spriteAnimation.PlayProjectileImpact(p)) Impact(p,.25f,false);
    }
    public override void CancelAttacks()
    {
        base.CancelAttacks(); SetVisualPhase(VisualPhase.Idle,0);
        if(spriteAnimation) spriteAnimation.CancelCombatVisuals();
    }
}
