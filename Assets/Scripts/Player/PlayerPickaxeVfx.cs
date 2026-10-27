using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Local presentation only. No colliders, damage, attack scheduling or input ownership.
[DefaultExecutionOrder(150)]
[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerAttack))]
public sealed class PlayerPickaxeVfx : MonoBehaviour
{
    [SerializeField] private SpriteRenderer pickaxe, body;
    [SerializeField] private Sprite[] headTrails, dust1, dust2, dust3, fragments;
    private PlayerAttack attack;
    private PlayerHealth health;
    private DungeonRoomManager rooms;
    private bool wasAttacking;
    private int previousStep;
    private float nextSample;
    private readonly List<Stamp> stamps = new List<Stamp>(16);
    private readonly List<SpriteRenderer> pool = new List<SpriteRenderer>(16);
    private readonly List<StoneMonsterSpriteEffect> impacts = new List<StoneMonsterSpriteEffect>(4);
    private static readonly float[] Lives = { .10f, .12f, .15f };
    private static readonly float[] Caps = { 89f / 255f, 114f / 255f, 127f / 255f };
    private struct Stamp { public SpriteRenderer view; public float born; public int step; }
    private void Awake() { attack = GetComponent<PlayerAttack>(); health = GetComponent<PlayerHealth>(); rooms = FindFirstObjectByType<DungeonRoomManager>(); }
    private void OnEnable()
    {
        attack.VisualDirectHit += DirectHit;
        attack.VisualAttackCancelled += ClearTrails;
    }
    private void OnDisable()
    {
        attack.VisualDirectHit -= DirectHit;
        attack.VisualAttackCancelled -= ClearTrails;
        ClearTrails();
        foreach (var view in pool) if (view) Destroy(view.gameObject);
        pool.Clear();
        foreach (var effect in impacts) if (effect) Destroy(effect.gameObject);
        impacts.Clear();
    }
    private void LateUpdate()
    {
        if (!pickaxe || !body) return;
        if (health.IsDead) { ClearTrails(); return; }
        if (Time.timeScale <= 0 || health.CombatLocked) return;
        float now = health.CombatTime;
        int step = Mathf.Clamp(attack.ComboStep, 1, 3) - 1;
        Vector3 head = pickaxe.transform.TransformPoint(new Vector3(29f / 64f, 0));
        bool active = attack.IsAttacking;
        if (active && (!wasAttacking || previousStep != step)) nextSample = now;
        float progress = attack.VisualAttackProgress;
        if (active && progress >= (step == 2 ? .10f : .12f) && progress <= (step == 2 ? .50f : .88f) && now >= nextSample)
        {
            if (headTrails != null && headTrails.Length > step && headTrails[step]) AddStamp(step, now, head);
            nextSample = now + .02f;
        }
        float weights = 0;
        float span = pickaxe.transform.TransformVector(Vector3.right * (8f / 64f)).magnitude;
        for (int i = stamps.Count - 1; i >= 0; i--)
        {
            Stamp stamp = stamps[i];
            float age = now - stamp.born;
            if (!stamp.view || age >= Lives[stamp.step] || (stamp.step == 2 && Mathf.Abs(Vector3.Dot(head - stamp.view.transform.position, pickaxe.transform.right)) > span))
            {
                if (stamp.view) stamp.view.gameObject.SetActive(false);
                stamps.RemoveAt(i); continue;
            }
            weights += Mathf.Pow(1f - age / Lives[stamp.step], 2f);
        }
        foreach (var stamp in stamps)
        {
            float weight = Mathf.Pow(1f - (now - stamp.born) / Lives[stamp.step], 2f);
            float cap = Caps[stamp.step];
            float alpha = 1f - Mathf.Pow(1f - cap, weight / Mathf.Max(1f, weights));
            stamp.view.color = new Color(1, 1, 1, alpha / cap);
        }
        wasAttacking = active; previousStep = step;
        impacts.RemoveAll(effect => !effect);
    }
    private void AddStamp(int step, float now, Vector3 head)
    {
        SpriteRenderer view = null;
        foreach (var candidate in pool) if (candidate && !candidate.gameObject.activeSelf) { view = candidate; break; }
        if (!view)
        {
            if (pool.Count >= 16) return;
            var go = new GameObject("Player Pickaxe Head Trail");
            SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            view = go.AddComponent<SpriteRenderer>(); pool.Add(view);
        }
        view.gameObject.SetActive(true);
        view.sprite = headTrails[step]; view.sharedMaterial = pickaxe.sharedMaterial;
        view.sortingLayerID = pickaxe.sortingLayerID; view.sortingOrder = pickaxe.sortingOrder - 1;
        view.transform.SetPositionAndRotation(head, pickaxe.transform.rotation);
        view.transform.localScale = pickaxe.transform.lossyScale;
        stamps.Add(new Stamp { view = view, born = now, step = step });
    }
    private void DirectHit(int combo, Vector2 point, Vector2 forward)
    {
        if (!isActiveAndEnabled) return;
        var frames = combo == 1 ? dust1 : combo == 2 ? dust2 : dust3;
        Burst("Hit Dust " + combo, frames, combo == 1 ? .18f : combo == 2 ? .24f : .30f, point, forward);
        if (combo == 3) Burst("Stone Fragments", fragments, .28f, point, forward);
    }
    private void Burst(string label, Sprite[] frames, float duration, Vector2 point, Vector2 forward)
    {
        Transform room = rooms && rooms.CurrentRoom ? rooms.CurrentRoom.transform : null;
        var effect = StoneMonsterSpriteEffect.Play(label, frames, duration, point, pickaxe, room,
            pickaxe.transform.lossyScale, body.sortingOrder + (point.y > body.bounds.center.y ? -1 : 1));
        if (!effect) return;
        effect.name = "Player Pickaxe " + label;
        effect.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg);
        impacts.Add(effect);
    }
    private void ClearTrails()
    {
        foreach (var stamp in stamps) if (stamp.view) stamp.view.gameObject.SetActive(false);
        stamps.Clear(); wasAttacking = false;
    }
}
