using UnityEngine;

// Local renderer preview; keep 1.0 until a tone is approved. Rims/ore/lanterns are excluded.
[DisallowMultipleComponent]
public sealed class Map01EnvironmentToneTrial : MonoBehaviour
{
    [SerializeField] private MeshRenderer[] wallInteriors;
    [SerializeField, Range(.8f, 1f)] private float wallBrightness = 1f;
    private MaterialPropertyBlock block;
    private float applied = -1;
    private static readonly int Tint = Shader.PropertyToID("_Color");
    private void OnEnable() { applied = -1; Apply(); }
    private void LateUpdate() { Apply(); }
    public void SetWallBrightness(float value) { wallBrightness = Mathf.Clamp(value, .8f, 1f); Apply(); }
    private void Apply()
    {
        if (Mathf.Approximately(applied, wallBrightness) || wallInteriors == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        float tint = QualitySettings.activeColorSpace == ColorSpace.Linear
            ? Mathf.GammaToLinearSpace(wallBrightness) : wallBrightness;
        block.SetColor(Tint, new Color(tint, tint, tint, 1));
        foreach (var view in wallInteriors) if (view) view.SetPropertyBlock(block);
        applied = wallBrightness;
    }
    private void OnDisable()
    {
        if (wallInteriors != null) foreach (var view in wallInteriors) if (view) view.SetPropertyBlock(null);
    }
}
