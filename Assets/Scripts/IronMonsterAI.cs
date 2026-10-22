using System.Collections;
using UnityEngine;

public sealed class IronMonsterAI : NormalMonsterAI
{
    // Read-only visual inputs; the existing attack owns direction and lifecycle.
    public Vector2 VisualAttackDirection => direction;
    public Transform AttackVisuals => effects;
    public enum VisualPhase { Idle, Windup, Charge, Recovery }
    public VisualPhase Phase { get; private set; }
    public float PhaseStartedAt { get; private set; }
    public float PhaseDuration { get; private set; }
    public bool VisualWallImpact { get; private set; }
    IronMonsterSpriteAnimation spriteAnimation;
    protected override void Awake() { base.Awake(); spriteAnimation=GetComponent<IronMonsterSpriteAnimation>(); }
    void SetVisualPhase(VisualPhase phase,float seconds)
    { Phase=phase; PhaseStartedAt=Time.time; PhaseDuration=seconds; }
    [SerializeField] float chargeSpeed=8.5f, chargeWidth=1.2f, chargeDistance=6f, wallRecovery=1.5f;
    readonly MonsterCollisionPass passage=new MonsterCollisionPass();
    protected override IEnumerator Attack()
    {
        float radius=chargeWidth*.5f;
        float length=WallDistance(origin,direction,radius,chargeDistance);
        bool endsAtWall=length<chargeDistance;
        VisualWallImpact=false; SetVisualPhase(VisualPhase.Windup,warningDuration);
        yield return Warn(origin,radius,true,length); ClearWarning();
        // Sweeps own all collision decisions during charge; no global layer changes.
        UnlockPosition(); passage.Begin(this,target);
        SetVisualPhase(VisualPhase.Charge,0);
        if(spriteAnimation) spriteAnimation.PlayChargeStarted(rb.position,direction);
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
        // Confirm visual contact too: a wall removed during warning must not emit impact art.
        VisualWallImpact=wall && WallDistance(rb.position,direction,radius,.05f)<.05f;
        SetVisualPhase(VisualPhase.Recovery,wall?wallRecovery:recoveryDuration);
        if(spriteAnimation) spriteAnimation.EndChargeVisuals();
        if(wall)
        {
            Vector2 point=rb.position+direction*radius;
            if(VisualWallImpact && spriteAnimation && spriteAnimation.PlayWallImpact(point,direction))
            {
                if(Camera.main && Camera.main.TryGetComponent<CameraFollow>(out var camera))
                    camera.ShakeAt(point,shakeStrength,shakeDuration);
            }
            else if(!spriteAnimation || !spriteAnimation.HasWallImpactEffects)
            { Impact(point,.6f); StartCoroutine(BodyGlow(wallRecovery,true)); }
        }
        yield return Recover(wall?wallRecovery:recoveryDuration);
        SetVisualPhase(VisualPhase.Idle,0);
    }
    public override void CancelAttacks()
    {
        passage.Restore(); base.CancelAttacks();
        SetVisualPhase(VisualPhase.Idle,0); VisualWallImpact=false;
        if(spriteAnimation) spriteAnimation.CancelCombatVisuals();
    }
}
