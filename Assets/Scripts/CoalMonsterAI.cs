using System.Collections;
using UnityEngine;
public sealed class CoalMonsterAI : NormalMonsterAI
{
    // Read-only visual inputs; the existing attack still owns direction and lifecycle.
    public Vector2 VisualAttackDirection => direction;
    public Transform AttackVisuals => effects;
    public enum VisualPhase { Idle, Windup, Recovery }
    public VisualPhase Phase { get; private set; }
    public float PhaseStartedAt { get; private set; }
    public float PhaseDuration { get; private set; }
    CoalMonsterSpriteAnimation spriteAnimation;
    protected override void Awake() { base.Awake(); spriteAnimation=GetComponent<CoalMonsterSpriteAnimation>(); }
    void SetVisualPhase(VisualPhase phase,float seconds)
    { Phase=phase; PhaseStartedAt=Time.time; PhaseDuration=seconds; }
    [SerializeField] float warningRadius=2.2f, damageRadius=2f;
    protected override IEnumerator Attack()
    {
        SetVisualPhase(VisualPhase.Windup,warningDuration);
        if(!spriteAnimation || !spriteAnimation.PlayWindup()) StartCoroutine(BodyGlow(warningDuration+recoveryDuration,true));
        yield return Warn(origin,warningRadius); ClearWarning();
        // Keep the original damage/death order, including its lethal-target early exit.
        HitArea(origin,damageRadius); if(target.IsDead) yield break;
        SetVisualPhase(VisualPhase.Recovery,recoveryDuration);
        if(spriteAnimation && spriteAnimation.PlayExplosion(origin,damageRadius))
        {
            if(Camera.main && Camera.main.TryGetComponent<CameraFollow>(out var camera))
                camera.ShakeAt(origin,shakeStrength,shakeDuration);
        }
        else Impact(origin,damageRadius);
        yield return Recover(recoveryDuration);
        SetVisualPhase(VisualPhase.Idle,0);
    }
    public override void CancelAttacks()
    {
        base.CancelAttacks(); SetVisualPhase(VisualPhase.Idle,0);
        if(spriteAnimation) spriteAnimation.CancelCombatVisuals();
    }
}
