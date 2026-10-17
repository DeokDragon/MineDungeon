using UnityEngine;

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
    [SerializeField, Range(0f, 2f)] private float shakeIntensity = 1f;
    private float shakeRemaining, shakeDuration, shakeAmplitude;
    private bool finaleShake;
    private float finaleShakeUntil;

    public void Shake(float amplitude, float duration, bool finale = false)
    {
        shakeAmplitude = Mathf.Max(shakeRemaining > 0f ? shakeAmplitude : 0f, amplitude);
        shakeRemaining = Mathf.Max(shakeRemaining, duration);
        shakeDuration = shakeRemaining;
        finaleShake |= finale;
        if (finale) finaleShakeUntil = Time.realtimeSinceStartup + duration;
    }

    public void ResetShake()
    {
        shakeRemaining = shakeAmplitude = 0f;
        finaleShake = false;
        if (cam != null) transform.position = ClampPosition(basePosition);
    }

    private void OnDisable()
    {
        ResetShake();
        if (cam != null) transform.position = ClampPosition(basePosition);
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        if (target != null)
            transform.position = basePosition = ClampPosition(target.position);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 destination = ClampPosition(target.position);

        Vector3 nextPosition = Time.timeScale > 0f && Time.deltaTime > 0f ? Vector3.SmoothDamp(
            basePosition,
            destination,
            ref followVelocity,
            smoothTime
        ) : basePosition;

        basePosition = ClampPosition(nextPosition);
        bool finalClock = finaleShake && DungeonRunManager.Instance != null && DungeonRunManager.Instance.IsRunCleared;
        float dt = finalClock ? Time.unscaledDeltaTime : (Time.timeScale > 0f ? Time.deltaTime : 0f);
        if (dt <= 0f) return; // Keep the existing offset fixed during pause.
        shakeRemaining = Mathf.Max(0f, finalClock ? finaleShakeUntil - Time.realtimeSinceStartup : shakeRemaining - dt);
        Vector2 offset = shakeRemaining > 0f
            ? Random.insideUnitCircle * (shakeAmplitude * shakeIntensity * shakeRemaining / shakeDuration)
            : Vector2.zero;
        if (shakeRemaining == 0f) ResetShake();
        transform.position = ClampPosition(basePosition + (Vector3)offset);
    }

    private Vector3 ClampPosition(Vector3 position)
    {
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
    public void SetRoom(Transform player, Vector2 minimum, Vector2 maximum)
    {
        target = player;
        mapMin = minimum;
        mapMax = maximum;
        if (cam == null) cam = GetComponent<Camera>();
        followVelocity = Vector3.zero;
        ResetShake();
        if (target != null) transform.position = basePosition = ClampPosition(target.position);
    }

}
