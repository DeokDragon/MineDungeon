using UnityEngine;

// Owned by PlayerSkills; no collision, damage, or independent clock.
public sealed class PlayerSkillVisual : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField] private Sprite circleSprite, squareSprite;
    private LineRenderer outline;
    private SpriteRenderer fill;
    private Color tint;
    private Vector2 size;
    private bool circle;
    private SpriteRenderer[] chips;
    public void AddChips()
    {
        chips = new SpriteRenderer[6];
        for (int i = 0; i < chips.Length; i++)
        {
            var chip = new GameObject("Decorative chip").AddComponent<SpriteRenderer>();
            chip.transform.SetParent(transform, false); chip.sprite = squareSprite; chip.sharedMaterial = material;
            chip.sortingOrder = 0; chip.transform.localScale = new Vector3(.07f, .045f, 1f); chips[i] = chip;
        }
    }
    public void AnimateChips(float t)
    {
        if (chips == null) return;
        for (int i = 0; i < chips.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / chips.Length;
            chips[i].transform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * (size.x * .25f + t * .6f);
            chips[i].transform.localRotation = Quaternion.Euler(0, 0, i * 47 + t * 160);
            var color = tint; color.a = .8f * (1 - t); chips[i].color = color;
        }
    }

    public void Configure(Vector2 position, Vector2 direction, float length, float width, bool isCircle, Color color)
    {
        transform.SetPositionAndRotation(position, Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
        transform.localScale = Vector3.one;
        tint = color; circle = isCircle;
        if (outline == null)
        {
            outline = new GameObject("Outline").AddComponent<LineRenderer>();
            outline.transform.SetParent(transform, false); outline.useWorldSpace = false;
            outline.sharedMaterial = material; outline.loop = true; outline.widthMultiplier = .035f;
            outline.sortingOrder = 0;
            fill = new GameObject("Fill").AddComponent<SpriteRenderer>();
            fill.transform.SetParent(transform, false); fill.sharedMaterial = material; fill.sortingOrder = -1;
        }
        fill.sprite = circle ? circleSprite : squareSprite;
        size = circle ? Vector2.one * width : new Vector2(length, width);
        fill.transform.localPosition = circle ? Vector3.zero : new Vector3(length * .5f, 0, 0);
        int count = circle ? 64 : 4; outline.positionCount = count;
        var points = new Vector3[count];
        for (int i = 0; i < count; i++)
            points[i] = circle ? new Vector3(Mathf.Cos(i * Mathf.PI * 2 / count), Mathf.Sin(i * Mathf.PI * 2 / count)) * width * .5f
                : new Vector3(i == 0 || i == 3 ? 0 : length, i < 2 ? -width * .5f : width * .5f);
        outline.SetPositions(points); SetProgress(1); SetAlpha(.22f);
    }
    public void SetProgress(float progress)
    {
        if (fill == null) return;
        float t = Mathf.Clamp01(progress);
        var bounds = fill.sprite.bounds.size;
        fill.transform.localScale = new Vector3(size.x / bounds.x * t, size.y / bounds.y * (circle ? t : 1), 1);
        if (!circle) fill.transform.localPosition = new Vector3(size.x * t * .5f, 0, 0);
    }
    public void SetAlpha(float alpha)
    {
        if (fill == null) return;
        var c = tint; c.a = alpha; fill.color = c;
        c.a = Mathf.Clamp01(alpha * 3); outline.startColor = outline.endColor = c;
    }
}
