using System.Collections.Generic;
using UnityEngine;

// Short-lived decorative sprites only. No colliders, rigidbodies or gameplay timers.
public sealed class StoneCombatBurst : MonoBehaviour
{
    [SerializeField] private Sprite chipSprite;
    [SerializeField] private Sprite dustSprite;
    [SerializeField] private Material spriteMaterial;
    [SerializeField] private int hitChips = 4;
    [SerializeField] private int deathChips = 10;
    [SerializeField] private int bossChips = 12;
    private static readonly List<StoneCombatBurst> live = new List<StoneCombatBurst>();
    private sealed class Piece
    {
        public SpriteRenderer sprite;
        public Vector3 start, velocity, scale;
        public float duration, spin, grow;
        public Color color;
    }
    private readonly List<Piece> pieces = new List<Piece>();
    private DungeonRoom owner;
    private float age, duration;
    private float spawnedRealtime;
    private bool finale;
    public float Age => age;
    public static int ActiveCount => live.Count;

    public void Initialize(DungeonRoom room, Vector2 point, Vector2 direction, SpriteRenderer body, bool death, bool boss)
    {
        while (live.Count >= 24) live[0].Clear();
        live.Add(this);
        owner = room;
        finale = death && boss;
        spawnedRealtime = Time.realtimeSinceStartup;
        transform.position = point;
        transform.localScale = Vector3.one;
        duration = death ? (boss ? .7f : .45f) : .35f;
        Vector2 forward = direction.sqrMagnitude > .001f ? direction.normalized : Vector2.right;
        float heading = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
        int count = death ? (boss ? bossChips : deathChips) : hitChips;
        for (int i = 0; i < count; i++)
        {
            float angle = (death ? i * 360f / count + Random.Range(-15f, 15f) : heading + Random.Range(-55f, 55f)) * Mathf.Deg2Rad;
            float size = Random.Range(.06f, .14f) * (death ? (boss ? 2f : 1.4f) : 1f);
            float shade = Random.Range(.23f, .43f);
            var chip = Add("Stone chip", chipSprite, new Vector2(size, size * Random.Range(.6f, 1.4f)),
                new Color(shade, shade + .03f, shade + .06f, .95f), death ? duration : Random.Range(.25f, .35f), 0);
            chip.velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(1.2f, boss && death ? 3f : 2f);
            chip.spin = Random.Range(-400f, 400f);
        }
        if (death)
        {
            var ghost = Add("Breaking body", body.sprite, body.bounds.size, new Color(1f, .97f, .85f, body.color.a), .05f, body.sortingOrder);
            ghost.sprite.transform.rotation = body.transform.rotation;
            for (int i = 0; i < 3; i++)
            {
                var dust = Add("Dust", dustSprite, Vector2.one * (boss ? .8f : .4f), new Color(.35f, .31f, .25f, .20f), duration, -1);
                dust.start = new Vector3(Random.Range(-.25f, .25f), Random.Range(-.15f, .15f));
                dust.grow = boss ? 2f : 1f;
                dust.velocity = new Vector3((i - 1) * .4f, .2f);
            }
        }
        else
        {
            for (int i = 0; i < 2; i++)
            {
                var flash = Add("Impact flash", chipSprite, new Vector2(.32f, .045f), new Color(1f, .98f, .78f), .08f, body.sortingOrder + 1);
                flash.sprite.transform.rotation = Quaternion.Euler(0, 0, heading + 45f + 90f * i);
            }
        }
    }

    private Piece Add(string label, Sprite sprite, Vector2 size, Color color, float lifetime, int order)
    {
        var go = new GameObject(label);
        go.transform.SetParent(transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = spriteMaterial;
        renderer.color = color;
        renderer.sortingOrder = order; // Dust/chips stay underneath boss warning layers (1-3).
        Vector3 scale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1f);
        go.transform.localScale = scale;
        var piece = new Piece { sprite = renderer, scale = scale, duration = lifetime, color = color };
        pieces.Add(piece);
        return piece;
    }

    private void Update()
    {
        if (owner == null || !owner.gameObject.activeInHierarchy) { Clear(); return; }
        // Results allow world time to flow; ESC pause must also freeze the terminal burst.
        float nextAge = age + Time.deltaTime;
        float dt = nextAge - age;
        age = nextAge;
        foreach (var p in pieces)
        {
            if (age >= p.duration) { p.sprite.enabled = false; continue; }
            float t = age / p.duration;
            p.sprite.transform.localPosition = p.start + p.velocity * age;
            p.sprite.transform.Rotate(0, 0, p.spin * dt);
            p.sprite.transform.localScale = p.scale * (1f + p.grow * t);
            var color = p.color; color.a *= 1f - t;
            p.sprite.color = color;
        }
        if (age >= duration) Clear();
    }

    public static void ClearNonFinale()
    {
        for (int i = live.Count - 1; i >= 0; i--) if (!live[i].finale) live[i].Clear();
    }
    private void Clear() { live.Remove(this); gameObject.SetActive(false); Destroy(gameObject); }
    private void OnDestroy() { live.Remove(this); }
}
