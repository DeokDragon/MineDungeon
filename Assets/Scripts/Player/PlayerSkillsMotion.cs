using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerSkills
{
    private sealed class CollisionPair { public Collider2D player, enemy; }
    private sealed class DashState
    {
        public Cast cast;
        public float elapsed, travelled;
        public readonly List<Vector2> path = new List<Vector2>();
        public readonly List<CollisionPair> ignored = new List<CollisionPair>();
    }
    private DashState dash;
    private Collider2D[] BodyColliders => GetComponentsInChildren<Collider2D>();

    private IEnumerator Drill(Cast c)
    {
        var v = Visual(c);
        v.Configure(c.origin, c.direction, StripLength(c.origin, c.direction, c.range, c.width), c.width, false, ColorFor(c));
        float start = Time.time;
        while (Time.time < start + c.data.windup) { v.SetProgress(Mathf.InverseLerp(start, start + c.data.windup, Time.time)); yield return null; }
        if (c.cancelled) yield break;
        dash = new DashState { cast = c }; dash.path.Add(rb.position);
        var state = dash;
        while (dash == state) yield return null;
        if (c.cancelled) yield break;
        Unlock(c); yield return Fade(v, .12f); live.Remove(c);
    }

    private void FixedUpdate()
    {
        if (dash == null || Time.timeScale <= 0f) return;
        if (health.IsDead || (run != null && run.IsRunEnded)) { CancelAll(); return; }
        var state = dash; var c = state.cast;
        float duration = Mathf.Max(.001f, c.data.dashDuration);
        float dt = Mathf.Min(Time.fixedDeltaTime, duration - state.elapsed);
        float wanted = Mathf.Min(c.range - state.travelled, c.range / duration * dt);
        Vector2 from = rb.position;
        float distance = BodyTravel(from, c.direction, wanted);
        Vector2 to = from + c.direction * distance;
        IgnoreEnemies(from, to, state);
        // The swept body query, not a discrete end-point overlap, limits this move.
        rb.linearVelocity = Vector2.zero;
        rb.position = to; transform.position = new Vector3(to.x, to.y, transform.position.z);
        Physics2D.SyncTransforms();
        state.path.Add(to); state.elapsed += dt; state.travelled += distance;
        HitStrip(c, from, to, c.width);
        if (c.cancelled || dash != state) return;
        if (distance < wanted - .0001f || state.elapsed >= duration - .0001f || state.travelled >= c.range - .0001f)
        {
            RestoreDash(); Unlock(c);
        }
    }

    private float BodyTravel(Vector2 origin, Vector2 direction, float wanted)
    {
        float distance = wanted;
        foreach (var body in BodyColliders)
        {
            if (!body.enabled || body.isTrigger) continue;
            Vector2 offset = (Vector2)body.bounds.center - rb.position;
            var hit = Physics2D.BoxCast(origin + offset, (Vector2)body.bounds.size + Vector2.one * .02f, 0, direction, wanted, wallLayer);
            if (hit.collider != null) distance = Mathf.Min(distance, Mathf.Max(0, hit.distance - .025f));
        }
        return distance;
    }
    private bool FreePosition(Vector2 position)
    {
        foreach (var body in BodyColliders)
        {
            if (!body.enabled || body.isTrigger) continue;
            Vector2 offset = (Vector2)body.bounds.center - rb.position;
            foreach (var other in Physics2D.OverlapBoxAll(position + offset, (Vector2)body.bounds.size + Vector2.one * .025f, 0, enemyLayer | wallLayer))
                if (!other.isTrigger && other.attachedRigidbody != rb && other.enabled) return false;
        }
        return true;
    }
    private void IgnoreEnemies(Vector2 from, Vector2 to, DashState state)
    {
        float length = Vector2.Distance(from, to);
        // Include the player's whole physical body even if an Inspector attack width is narrower.
        float bodyWidth = state.cast.width;
        foreach (var body in BodyColliders) bodyWidth = Mathf.Max(bodyWidth, body.bounds.size.magnitude + .05f);
        foreach (var enemy in Physics2D.OverlapBoxAll((from + to) * .5f,
            new Vector2(length + bodyWidth, bodyWidth), Vector2.SignedAngle(Vector2.right, state.cast.direction), enemyLayer))
        {
            if (enemy.GetComponentInParent<EnemyHealth>() == null) continue;
            foreach (var body in BodyColliders)
            {
                if (body.isTrigger || enemy.isTrigger || Physics2D.GetIgnoreCollision(body, enemy)) continue;
                Physics2D.IgnoreCollision(body, enemy, true);
                state.ignored.Add(new CollisionPair { player = body, enemy = enemy });
            }
        }
    }
    private void RestoreDash()
    {
        if (dash == null) return;
        var state = dash; dash = null;
        // Walk backward on the actual travelled polyline; never jump forward or across a wall.
        Vector2 destination = rb.position;
        bool found = FreePosition(destination);
        for (int i = state.path.Count - 1; i > 0 && !found; i--)
        {
            Vector2 a = state.path[i], b = state.path[i - 1];
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / .025f));
            for (int j = 1; j <= steps; j++)
            {
                Vector2 candidate = Vector2.Lerp(a, b, (float)j / steps);
                if (!FreePosition(candidate)) continue;
                destination = candidate; found = true; break;
            }
        }
        if (found)
        {
            rb.position = destination; transform.position = new Vector3(destination.x, destination.y, transform.position.z);
            Physics2D.SyncTransforms();
        }
        rb.linearVelocity = Vector2.zero;
        foreach (var pair in state.ignored)
            if (pair.player != null && pair.enemy != null) Physics2D.IgnoreCollision(pair.player, pair.enemy, false);
        state.ignored.Clear();
    }

    private IEnumerator Blast(Cast c)
    {
        var v = Visual(c); float started = Time.time;
        v.Configure(c.target, c.direction, 0, .25f, true, ColorFor(c)); v.SetAlpha(.12f);
        while (Time.time < started + c.data.windup) yield return null;
        if (c.cancelled) yield break;
        Unlock(c);
        float landedAt = started + c.data.windup + c.data.flightDuration;
        while (Time.time < landedAt)
        {
            float t = c.data.flightDuration > 0 ? Mathf.Clamp01((Time.time - started - c.data.windup) / c.data.flightDuration) : 1;
            Vector2 position = Vector2.Lerp(c.origin, c.target, t) + Vector2.up * (Mathf.Sin(t * Mathf.PI) * .7f);
            v.Configure(position, c.direction, 0, .28f, true, ColorFor(c)); v.SetAlpha(.5f); yield return null;
        }
        if (c.cancelled) yield break;
        v.Configure(c.target, c.direction, 0, c.radius * 2, true, ColorFor(c));
        float explodeAt = landedAt + c.data.fuseDuration;
        while (Time.time < explodeAt)
        {
            v.SetProgress(c.data.fuseDuration > 0 ? (Time.time - landedAt) / c.data.fuseDuration : 1); yield return null;
        }
        if (c.cancelled) yield break;
        v.SetProgress(1); HitCircle(c, c.target);
        if (c.cancelled) yield break;
        v.AddChips(); yield return Fade(v, .2f); live.Remove(c);
    }
}
