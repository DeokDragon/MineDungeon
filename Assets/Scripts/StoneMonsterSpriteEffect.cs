using UnityEngine;
using UnityEngine.SceneManagement;

// One local, disposable visual. Lifetime never controls combat, rewards or room clearing.
public sealed class StoneMonsterSpriteEffect : MonoBehaviour
{
    private Sprite[] frames;
    private SpriteRenderer display;
    private float age, duration, startedAt;

    public static StoneMonsterSpriteEffect Play(string label, Sprite[] frames, float seconds,
        Vector3 position, SpriteRenderer source, Transform room, Vector3 scale, int order)
    {
        if (frames == null || frames.Length == 0 || !source) return null;
        var go = new GameObject("B1R " + label);
        SceneManager.MoveGameObjectToScene(go, source.gameObject.scene);
        go.transform.position = position;
        go.transform.localScale = scale;
        if (room) go.transform.SetParent(room, true);
        var effect = go.AddComponent<StoneMonsterSpriteEffect>();
        effect.frames = frames;
        effect.duration = Mathf.Max(.01f, seconds);
        effect.startedAt = Time.time;
        effect.display = go.AddComponent<SpriteRenderer>();
        effect.display.sharedMaterial = source.sharedMaterial;
        effect.display.sortingLayerID = source.sortingLayerID;
        effect.display.sortingOrder = order;
        effect.display.flipX = source.flipX;
        effect.display.flipY = source.flipY;
        effect.display.sprite = frames[0];
        return effect;
    }

    private void Update()
    {
        if (Time.timeScale == 0f) return;
        age = Time.time - startedAt;
        if (age >= duration) { gameObject.SetActive(false); Destroy(gameObject); return; }
        display.sprite = frames[Mathf.Min(frames.Length - 1, (int)(age / duration * frames.Length))];
    }

    // Room disable/transition also disposes detached death visuals, including while paused.
    private void OnDisable() { Destroy(gameObject); }
}
