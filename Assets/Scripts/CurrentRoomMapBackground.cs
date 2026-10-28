using UnityEngine;
using UnityEngine.UI;

// Clips only the background to the inside contour of the fixed 560x400 v1 frame.
// Bands come from the frame's central transparent region; UVs retain the original
// background position and alpha. No mask/material or map-mesh changes are needed.
[AddComponentMenu("")]
public sealed class CurrentRoomMapBackground : Image
{
    static readonly Vector4[] InteriorBands =
    {
        new Vector4(35, 15, 524, 16),
        new Vector4(35, 16, 525, 17),
        new Vector4(33, 17, 526, 18),
        new Vector4(23, 18, 527, 19),
        new Vector4(23, 19, 528, 20),
        new Vector4(23, 20, 534, 21),
        new Vector4(24, 21, 534, 22),
        new Vector4(25, 22, 533, 25),
        new Vector4(17, 25, 542, 30),
        new Vector4(17, 30, 543, 31),
        new Vector4(14, 31, 544, 33),
        new Vector4(14, 33, 546, 364),
        new Vector4(14, 364, 545, 365),
        new Vector4(14, 365, 544, 367),
        new Vector4(14, 367, 542, 368),
        new Vector4(15, 368, 542, 369),
        new Vector4(17, 369, 542, 375),
        new Vector4(25, 375, 530, 379),
        new Vector4(24, 379, 530, 380),
        new Vector4(31, 380, 527, 382),
        new Vector4(32, 382, 527, 383),
        new Vector4(33, 383, 526, 384),
    };

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        if (!sprite) { base.OnPopulateMesh(vh); return; }
        vh.Clear();
        var rect = GetPixelAdjustedRect();
        var uv = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
        foreach (var b in InteriorBands)
        {
            float x0 = b.x / 560f, y0 = b.y / 400f;
            float x1 = b.z / 560f, y1 = b.w / 400f;
            int i = vh.currentVertCount;
            Add(vh, rect, uv, x0, y0);
            Add(vh, rect, uv, x0, y1);
            Add(vh, rect, uv, x1, y1);
            Add(vh, rect, uv, x1, y0);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }

    void Add(VertexHelper vh, Rect rect, Vector4 uv, float x, float y)
    {
        vh.AddVert(new Vector2(rect.xMin + rect.width * x, rect.yMin + rect.height * y),
            color, new Vector2(Mathf.Lerp(uv.x, uv.z, x), Mathf.Lerp(uv.y, uv.w, y)));
    }
}
