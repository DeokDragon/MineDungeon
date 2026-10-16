using System.Collections.Generic;
using UnityEngine;

// Pure presentation: progress is supplied by the attack's clock, never advanced here.
public sealed class StoneGolemTelegraph : MonoBehaviour
{
    private readonly List<Vector3> boundary = new List<Vector3>();
    private Mesh backgroundMesh, fillMesh;
    private MeshRenderer fillRenderer;
    private bool linear;
    private float radius, length;
    public float Progress { get; private set; }

    public static StoneGolemTelegraph Create(Transform owner, Material material,
        Vector2 origin, Vector2 direction, float radius, float angle, float length,
        bool linear, Color color, int sortingLayer, int sortingOrder)
    {
        var go = new GameObject("Attack Telegraph");
        go.transform.SetParent(owner, false); // Owner is the boss's unscaled world effects root.
        go.transform.SetPositionAndRotation(origin,
            Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg));
        var view = go.AddComponent<StoneGolemTelegraph>();
        view.linear = linear;
        view.radius = radius;
        view.length = length;
        if (linear)
        {
            for (int i = 0; i <= 32; i++)
            {
                float a = (-90f + i * 180f / 32f) * Mathf.Deg2Rad;
                view.boundary.Add(new Vector3(length + Mathf.Cos(a) * radius, Mathf.Sin(a) * radius));
            }
            for (int i = 0; i <= 32; i++)
            {
                float a = (90f + i * 180f / 32f) * Mathf.Deg2Rad;
                view.boundary.Add(new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius));
            }
        }
        else
        {
            if (angle < 360f) view.boundary.Add(Vector3.zero);
            for (int i = 0; i <= 96; i++)
            {
                float a = (-angle * 0.5f + angle * i / 96f) * Mathf.Deg2Rad;
                view.boundary.Add(new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius);
            }
        }
        view.backgroundMesh = new Mesh { name = "Telegraph Background" };
        view.fillMesh = new Mesh { name = "Telegraph Fill" };
        BuildMesh(view.backgroundMesh, view.boundary);
        if (!linear) BuildMesh(view.fillMesh, view.boundary);
        view.AddSurface("Background", view.backgroundMesh, material, Tint(color, .14f), sortingLayer, sortingOrder);
        view.fillRenderer = view.AddSurface("Fill", view.fillMesh, material, Tint(color, .48f), sortingLayer, sortingOrder + 1);
        var outline = new GameObject("Outline").AddComponent<LineRenderer>();
        outline.transform.SetParent(go.transform, false);
        outline.useWorldSpace = false;
        outline.loop = true;
        outline.positionCount = view.boundary.Count;
        outline.SetPositions(view.boundary.ToArray());
        outline.widthMultiplier = .035f;
        outline.sharedMaterial = material;
        outline.startColor = outline.endColor = Color.white;
        outline.sortingLayerID = sortingLayer;
        outline.sortingOrder = sortingOrder + 2;
        SetTint(outline, Tint(color, .85f));
        view.SetProgress(0f);
        return view;
    }

    private static Color Tint(Color c, float alpha) { c.a = alpha; return c; }
    private static void SetTint(Renderer renderer, Color color)
    {
        var properties = new MaterialPropertyBlock();
        properties.SetColor("_Color", color);
        renderer.SetPropertyBlock(properties);
    }

    private MeshRenderer AddSurface(string label, Mesh mesh, Material material, Color color, int layer, int order)
    {
        var go = new GameObject(label);
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingLayerID = layer;
        renderer.sortingOrder = order;
        SetTint(renderer, color);
        return renderer;
    }

    public void SetProgress(float progress)
    {
        Progress = Mathf.Clamp01(progress);
        fillRenderer.enabled = Progress > 0f;
        if (!linear)
        {
            // Linear radius progression, matching the original rockfall warning.
            fillRenderer.transform.localScale = new Vector3(Progress, Progress, 1f);
            return;
        }
        // Clip the original capsule with a moving front; never enlarge its silhouette.
        float front = Mathf.Lerp(-radius, length + radius, Progress);
        var clipped = new List<Vector3>();
        for (int i = 0; i < boundary.Count; i++)
        {
            Vector3 a = boundary[i], b = boundary[(i + 1) % boundary.Count];
            bool insideA = a.x <= front, insideB = b.x <= front;
            if (insideA) clipped.Add(a);
            if (insideA != insideB) clipped.Add(Vector3.Lerp(a, b, (front - a.x) / (b.x - a.x)));
        }
        BuildMesh(fillMesh, clipped);
    }

    private static void BuildMesh(Mesh mesh, List<Vector3> polygon)
    {
        mesh.Clear();
        if (polygon.Count < 3) return;
        var triangles = new int[(polygon.Count - 2) * 3];
        var colors = new Color[polygon.Count];
        var uv = new Vector2[polygon.Count];
        for (int i = 0; i < polygon.Count; i++) { colors[i] = Color.white; uv[i] = Vector2.one * .5f; }
        for (int i = 0; i < polygon.Count - 2; i++)
        { triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = i + 2; }
        mesh.SetVertices(polygon);
        mesh.colors = colors;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    private void OnDestroy()
    {
        if (backgroundMesh != null) Destroy(backgroundMesh);
        if (fillMesh != null) Destroy(fillMesh);
    }
}
