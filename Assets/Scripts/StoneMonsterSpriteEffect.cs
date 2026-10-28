using UnityEngine;
using UnityEngine.SceneManagement;

// One local, disposable visual. Lifetime never controls combat, rewards or room clearing.
public sealed class StoneMonsterSpriteEffect : MonoBehaviour
{
    private Sprite[] frames;
    private SpriteRenderer display;
    private float age, duration, startedAt;
    private float[] frameWeights;
    public bool DelaysClearResult { get; private set; }
    public bool IsFinished { get; private set; }
    public event System.Action<StoneMonsterSpriteEffect> Finished;

    public static StoneMonsterSpriteEffect Play(string label, Sprite[] frames, float seconds,
        Vector3 position, SpriteRenderer source, Transform room, Vector3 scale, int order,
        float[] frameWeights = null, bool delaysClearResult = false)
    {
        if (frames == null || frames.Length == 0 || !source) return null;
        var go = new GameObject("B1R " + label);
        SceneManager.MoveGameObjectToScene(go, source.gameObject.scene);
        go.transform.position = position;
        go.transform.localScale = scale;
        if (room) go.transform.SetParent(room, true);
        var effect = go.AddComponent<StoneMonsterSpriteEffect>();
        effect.frames = frames;
        effect.frameWeights = frameWeights;
        effect.DelaysClearResult = delaysClearResult;
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
        if (age >= duration) { Complete(); gameObject.SetActive(false); Destroy(gameObject); return; }
        int index = Mathf.Min(frames.Length - 1, (int)(age / duration * frames.Length));
        if (frameWeights != null && frameWeights.Length == frames.Length)
        {
            float total = 0f, end = 0f;
            foreach (float weight in frameWeights) total += weight;
            float elapsed = age / duration * total;
            for (int i = 0; i < frameWeights.Length; i++)
            {
                end += frameWeights[i];
                if (elapsed < end) { index = i; break; }
            }
        }
        display.sprite = frames[index];
    }

    // Room disable/transition also disposes detached death visuals, including while paused.
    private void Complete()
    {
        if (IsFinished) return;
        IsFinished = true;
        if (display != null) display.enabled = false;
        var finished = Finished;
        Finished = null;
        finished?.Invoke(this);
    }
    private void OnDisable() { Complete(); Destroy(gameObject); }
    private void OnDestroy() { Complete(); }
}
