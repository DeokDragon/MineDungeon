#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEngine;

// Temporary actor input for explicit isolated Editor measurements. Never added to scenes/prefabs.
[DefaultExecutionOrder(20000)]
public sealed class EasyBalanceInputProbe : MonoBehaviour
{
    public Action Frame;
    public Vector2 Move, Aim;
    public bool Held, Potion, Dodge;
    PlayerMovement movement;PlayerAttack attack;PlayerPotion potion;
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    void Awake(){movement=GetComponent<PlayerMovement>();attack=GetComponent<PlayerAttack>();potion=GetComponent<PlayerPotion>();}
    void LateUpdate()
    {
        Frame?.Invoke();
        if(!Application.isPlaying||Time.timeScale<=0f||!movement||!attack)return;
        typeof(PlayerMovement).GetField("moveInput",Flags).SetValue(movement,Move);
        if(Aim.sqrMagnitude>.001f){typeof(PlayerMovement).GetField("aimDirection",Flags).SetValue(movement,Aim.normalized);typeof(PlayerAttack).GetField("aimAngle",Flags).SetValue(attack,Mathf.Atan2(Aim.y,Aim.x)*Mathf.Rad2Deg);}
        typeof(PlayerAttack).GetMethod("ProcessAttackInput",Flags).Invoke(attack,new object[]{false,Held});
        if(Potion&&!potion.IsUsing)typeof(PlayerPotion).GetMethod("TryStartUse",Flags).Invoke(potion,null);
        if(Dodge&&!movement.IsDodging&&GetComponent<PlayerHealth>().CombatTime>=(float)typeof(PlayerMovement).GetField("nextDodgeTime",Flags).GetValue(movement))typeof(PlayerMovement).GetMethod("StartDodge",Flags).Invoke(movement,null);
    }
}
#endif
