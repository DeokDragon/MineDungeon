using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float smoothTime = 0.15f;

    [Header("맵의 왼쪽 아래 / 오른쪽 위 좌표")]
    [SerializeField] private Vector2 mapMin = new Vector2(-20f, -15f);
    [SerializeField] private Vector2 mapMax = new Vector2(20f, 15f);

    private Camera cam;
    private Vector3 followVelocity;
    private Vector3 basePosition;
    private Transform presentationTarget;
    private Vector3 presentationFrom, presentationDestination;
    private float presentationTravel, presentationElapsed;
    private bool presentationReturning;
    [SerializeField, Range(0f, 2f)] private float shakeIntensity = 1f;
    [Header("Impact distance falloff (player world position)")]
    [SerializeField, Min(0f)] private float impactFullStrengthDistance = 3f;
    [SerializeField, Min(0f)] private float impactZeroStrengthDistance = 10f;
    private struct ShakeRequest
    {
        public float amplitude, duration, scaledStart, realStart;
        public bool finale;
    }
    private readonly List<ShakeRequest> shakeRequests = new List<ShakeRequest>();
    private Vector2 shakeOffset;
    public float CurrentShakeAmplitude { get; private set; }

    // Existing player / enemy feedback callers retain their API and undiminished strength.
    public void Shake(float amplitude, float duration, bool finale = false)
    {
        if (!isActiveAndEnabled || amplitude <= 0f || duration <= 0f || shakeIntensity <= 0f) return;
        if (!finale && DungeonRunManager.Instance != null && DungeonRunManager.Instance.IsRunEnded) return;
        shakeRequests.Add(new ShakeRequest { amplitude = amplitude, duration = duration,
            scaledStart = Time.time, realStart = Time.realtimeSinceStartup, finale = finale });
    }

    public void ShakeAt(Vector2 impactCenter, float amplitude, float duration)
    {
        if (target == null) return;
        float distance = Vector2.Distance(target.position, impactCenter);
        float outer = Mathf.Max(impactFullStrengthDistance + .001f, impactZeroStrengthDistance);
        float attenuation = 1f - Mathf.InverseLerp(impactFullStrengthDistance, outer, distance);
        Shake(amplitude * attenuation, duration);
    }

    public void ResetShake()
    {
        shakeRequests.Clear();
        CurrentShakeAmplitude = 0f;
        shakeOffset = Vector2.zero;
        if (cam != null) transform.position = ClampPosition(basePosition);
    }

    private void OnDisable()
    {
        presentationTarget = null;
        ResetShake();
        if (cam != null) transform.position = ClampPosition(basePosition);
    }

    public void SetShakeSetting(float value) { shakeIntensity = Mathf.Clamp(value, 0, 2); if (shakeIntensity == 0) ResetShake(); }

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        if (CharacterSaveService.Current != null) shakeIntensity = CharacterSaveService.Current.ShakeMultiplier;
        if (target != null)
            transform.position = basePosition = ClampPosition(target.position);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        if (presentationTarget != null)
        {
            if (Time.timeScale > 0f) presentationElapsed += Time.deltaTime;
            float t = presentationTravel > 0f ? Mathf.Clamp01(presentationElapsed / presentationTravel) : 1f;
            // Zero speed and acceleration at both ends, followed by a stationary focus.
            float eased = t * t * t * (t * (t * 6f - 15f) + 10f);
            basePosition = ClampPosition(Vector3.Lerp(presentationFrom, presentationDestination, eased));
            if (t >= 1f) basePosition = ClampPosition(presentationReturning ? target.position : presentationTarget.position);
            transform.position = basePosition;
            return;
        }
        Vector3 destination = ClampPosition(target.position);

        Vector3 nextPosition = Time.timeScale > 0f && Time.deltaTime > 0f ? Vector3.SmoothDamp(
            basePosition,
            destination,
            ref followVelocity,
            smoothTime
        ) : basePosition;

        basePosition = ClampPosition(nextPosition);
        UpdateShake();
        transform.position = ClampPosition(basePosition + (Vector3)shakeOffset);
    }

    private void UpdateShake()
    {
        bool ended = DungeonRunManager.Instance != null && DungeonRunManager.Instance.IsRunEnded;
        bool cleared = DungeonRunManager.Instance != null && DungeonRunManager.Instance.IsRunCleared;
        float strongest = 0f;
        for (int i = shakeRequests.Count - 1; i >= 0; i--)
        {
            ShakeRequest request = shakeRequests[i];
            bool finalClock = request.finale && cleared;
            float elapsed = Time.time - request.scaledStart;
            if (elapsed >= request.duration || (ended && !finalClock))
            {
                shakeRequests.RemoveAt(i);
                continue;
            }
            float strength = request.amplitude * (1f - Mathf.Clamp01(elapsed / request.duration));
            strongest = Mathf.Max(strongest, strength);
        }
        CurrentShakeAmplitude = strongest * shakeIntensity;
        if (CurrentShakeAmplitude <= 0f) shakeOffset = Vector2.zero;
        else if (Time.timeScale > 0f)
            shakeOffset = Random.insideUnitCircle * CurrentShakeAmplitude;
        // Otherwise retain the current offset while ESC pauses scaled requests.
    }

    private Vector3 ClampPosition(Vector3 position)
    {
        // Fit the actual viewport when aspect/zoom changes, including very wide windows.
        float fit = Mathf.Min((mapMax.y-mapMin.y)*.5f, (mapMax.x-mapMin.x)*.5f/Mathf.Max(.01f,cam.aspect));
        if (fit > 0f && cam.orthographicSize > fit) cam.orthographicSize = fit;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float minX = mapMin.x + halfWidth;
        float maxX = mapMax.x - halfWidth;
        float minY = mapMin.y + halfHeight;
        float maxY = mapMax.y - halfHeight;

        // 화면이 맵보다 크면 해당 축은 중앙에 고정
        float x = minX <= maxX
            ? Mathf.Clamp(position.x, minX, maxX)
            : (mapMin.x + mapMax.x) * 0.5f;

        float y = minY <= maxY
            ? Mathf.Clamp(position.y, minY, maxY)
            : (mapMin.y + mapMax.y) * 0.5f;

        return new Vector3(x, y, -10f);
    }
    public void SnapToTarget()
    {
        followVelocity = Vector3.zero;
        ResetShake();
        if (target != null) transform.position = basePosition = ClampPosition(target.position);
    }
    // Presentation changes only the camera focus; player tracking and room bounds stay intact.
    public void FocusPresentation(Transform focus, float travelDuration = 0f)
    {
        if (focus == null || !isActiveAndEnabled) return;
        presentationTarget = focus;
        presentationReturning = false;
        presentationFrom = basePosition;
        presentationDestination = ClampPosition(focus.position);
        presentationTravel = Mathf.Max(0f, travelDuration);
        presentationElapsed = 0f;
        followVelocity = Vector3.zero;
        ResetShake();
        if (presentationTravel == 0f) transform.position = basePosition = presentationDestination;
    }
    public void ReturnFromPresentation(Transform focus, float travelDuration)
    {
        if (presentationTarget != focus || presentationReturning || target == null) return;
        presentationReturning = true;
        presentationFrom = basePosition;
        presentationDestination = ClampPosition(target.position);
        presentationTravel = Mathf.Max(0f, travelDuration);
        presentationElapsed = 0f;
    }
    public bool IsPresentationMoving(Transform focus) => presentationTarget == focus && presentationElapsed < presentationTravel;
    public void ReleasePresentation(Transform focus)
    {
        if (presentationTarget != focus) return;
        presentationTarget = null;
        SnapToTarget();
    }
    public void SetRoom(Transform player, Vector2 minimum, Vector2 maximum)
    {
        presentationTarget = null;
        target = player;
        mapMin = minimum;
        mapMax = maximum;
        if (cam == null) cam = GetComponent<Camera>();
        followVelocity = Vector3.zero;
        ResetShake();
        if (target != null) transform.position = basePosition = ClampPosition(target.position);
    }

}
