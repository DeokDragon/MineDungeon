using UnityEngine;

// 방 전체의 최상위 오브젝트에 붙입니다. RoomController는 기존 위치에 둡니다.
public class DungeonRoom : MonoBehaviour
{
    [SerializeField] private RoomController roomController;
    [SerializeField] private RoomExit roomExit;
    [SerializeField] private Transform entrance;
    [SerializeField] private Vector2 localMin = new Vector2(-12.5f, -10f);
    [SerializeField] private Vector2 localMax = new Vector2(12.5f, 10f);

    public Transform Entrance => entrance;
    public bool IsCleared => roomController != null && roomController.IsCleared;
    public Vector2 WorldMin => transform.TransformPoint(localMin);
    public Vector2 WorldMax => transform.TransformPoint(localMax);

    public bool IsConfigured()
    {
        return roomController != null && roomController.enabled &&
            roomExit != null && entrance != null &&
            roomController.transform.IsChildOf(transform) &&
            roomExit.transform.IsChildOf(transform) && entrance.IsChildOf(transform) &&
            roomController.gameObject.activeInHierarchy &&
            roomExit.gameObject.activeInHierarchy &&
            localMin.x < localMax.x && localMin.y < localMax.y;
    }

    public void Bind(PlayerHealth player, DungeonRoomManager manager)
    {
        roomExit.Bind(this, manager);
        foreach (StoneMonsterAI monster in GetComponentsInChildren<StoneMonsterAI>(true))
            monster.SetTarget(player);
    }
}
