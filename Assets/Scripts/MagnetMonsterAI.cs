using System.Collections;
using UnityEngine;
public sealed class MagnetMonsterAI : NormalMonsterAI
{
    // Visuals read the existing attack snapshot; destination and damage remain AI-owned.
    public Vector2 VisualAttackDirection => direction;
    public enum VisualPhase { Idle, Windup, Recovery }
    public VisualPhase Phase { get; private set; }
    public float PhaseStartedAt { get; private set; }
    public float PhaseDuration { get; private set; }
    MagnetMonsterSpriteAnimation spriteAnimation;
    protected override void Awake() { base.Awake(); spriteAnimation=GetComponent<MagnetMonsterSpriteAnimation>(); }
    void SetVisualPhase(VisualPhase phase,float seconds)
    { Phase=phase; PhaseStartedAt=Time.time; PhaseDuration=seconds; }
    [SerializeField] float warningRadius=2f, damageRadius=1.8f;
    [SerializeField,Range(0,1)] float slowFraction=.3f;
    [SerializeField] float slowDuration=1.5f;
    protected override IEnumerator Attack()
    {
        SetVisualPhase(VisualPhase.Windup,warningDuration);
        if(spriteAnimation) spriteAnimation.PlayWindup();
        yield return Warn(destination,warningRadius); ClearWarning();
        if(HitArea(destination,damageRadius) && target && !target.IsDead) target.GetComponent<PlayerMovement>()?.ApplySlow(slowFraction,slowDuration,warningMaterial);
        bool targetDead=target.IsDead;
        if(!targetDead) SetVisualPhase(VisualPhase.Recovery,recoveryDuration);
        // Keep damage/death/slow order. Display a committed impact even if the lethal hit
        // synchronously canceled this AI via TargetDied; it never re-enables the attack.
        bool displayed=spriteAnimation && spriteAnimation.PlayImpact(destination,damageRadius,!targetDead);
        if(targetDead) yield break;
        if(!displayed) Impact(destination,damageRadius,false);
        yield return Recover(recoveryDuration);
        SetVisualPhase(VisualPhase.Idle,0);
    }
    public override void CancelAttacks()
    {
        base.CancelAttacks(); SetVisualPhase(VisualPhase.Idle,0);
        if(spriteAnimation) spriteAnimation.CancelCombatVisuals();
    }
}
