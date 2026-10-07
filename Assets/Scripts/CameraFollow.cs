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

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        if (target != null)
            transform.position = ClampPosition(target.position);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 destination = ClampPosition(target.position);

        Vector3 nextPosition = Vector3.SmoothDamp(
            transform.position,
            destination,
            ref followVelocity,
            smoothTime
        );

        transform.position = ClampPosition(nextPosition);
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
        if (target != null) transform.position = ClampPosition(target.position);
    }

}