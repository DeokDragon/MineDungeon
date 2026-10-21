using UnityEngine;

// Initial recognition only. Each AI owns its nonserialized latch; no shared aggro state.
public static class MonsterAggro
{
    static readonly RaycastHit2D[] SightHit = new RaycastHit2D[1];
    public static bool CanRecognize(Vector2 position, PlayerHealth player, float radius, LayerMask walls)
    {
        if (!player || player.IsDead || float.IsNaN(radius) || float.IsInfinity(radius) || radius < 0 ||
            ((Vector2)player.transform.position - position).sqrMagnitude > radius * radius) return false;
        var filter = new ContactFilter2D();
        filter.SetLayerMask(walls);
        filter.useTriggers = false;
        return Physics2D.Linecast(position, player.transform.position, filter, SightHit) == 0;
    }
}
