using System.Collections.Generic;
using UnityEngine;

// Per-pair, reversible pass-through. Leaves hurtboxes queryable by player weapons.
public sealed class MonsterCollisionPass
{
    readonly List<(Collider2D a, Collider2D b)> pairs = new List<(Collider2D, Collider2D)>();
    public void Begin(Component owner, PlayerHealth player)
    {
        Restore();
        var others=new List<Collider2D>();
        var room=owner.GetComponentInParent<DungeonRoom>();
        if(room) foreach(var enemy in room.GetComponentsInChildren<EnemyHealth>())
            if(enemy.gameObject!=owner.gameObject) others.AddRange(enemy.GetComponentsInChildren<Collider2D>());
        if(player) others.AddRange(player.GetComponentsInChildren<Collider2D>());
        foreach(var a in owner.GetComponentsInChildren<Collider2D>()) foreach(var b in others)
            if(a && b && !a.isTrigger && !b.isTrigger && !Physics2D.GetIgnoreCollision(a,b))
            { Physics2D.IgnoreCollision(a,b,true); pairs.Add((a,b)); }
    }
    public void Restore() { foreach(var p in pairs) if(p.a && p.b) Physics2D.IgnoreCollision(p.a,p.b,false); pairs.Clear(); }
}
