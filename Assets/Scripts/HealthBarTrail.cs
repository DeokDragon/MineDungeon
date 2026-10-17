using UnityEngine;

// Presentation only: values always come from EnemyHealth.
public sealed class HealthBarTrail
{
    public float Current { get; private set; }
    public float Trail { get; private set; }
    private float start, elapsed;

    public static float Fraction(EnemyHealth health) => health.MaxHealth > 0
        ? Mathf.Clamp01((float)health.CurrentHealth / health.MaxHealth) : 0f;

    public void Reset(float value)
    {
        Current = Trail = start = Mathf.Clamp01(value);
        elapsed = 0f;
    }

    public bool SetValue(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(value, Current)) return false;
        bool damaged = value < Current;
        Current = value;
        // Never rewind to an earlier HP value when hit during the descent.
        Trail = Mathf.Max(Trail, Current);
        start = Trail;
        elapsed = 0f;
        return damaged;
    }

    public void Tick(float delta, float delay, float duration)
    {
        elapsed += Mathf.Max(0f, delta);
        float progress = duration > 0f ? Mathf.Clamp01((elapsed - delay) / duration)
            : (elapsed >= delay ? 1f : 0f);
        Trail = Mathf.Lerp(start, Current, Mathf.SmoothStep(0f, 1f, progress));
    }
}
