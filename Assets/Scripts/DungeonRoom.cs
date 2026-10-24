using UnityEngine;

// 방 전체의 최상위 오브젝트에 붙입니다. RoomController는 기존 위치에 둡니다.
public class DungeonRoom : MonoBehaviour
{
    [SerializeField] private RoomController roomController;
    [SerializeField] private RoomExit roomExit;
    [SerializeField] private bool clearWithoutExit;
    [SerializeField] private Transform entrance;
    [SerializeField] private Vector2 localMin = new Vector2(-12.5f, -10f);
    [SerializeField] private Vector2 localMax = new Vector2(12.5f, 10f);

    public Transform Entrance => entrance;
    public RoomController Controller => roomController;
    public bool ClearWithoutExit => clearWithoutExit;
    public bool IsCleared => roomController != null && roomController.IsCleared;
    public Vector2 WorldMin => transform.TransformPoint(localMin);
    public Vector2 WorldMax => transform.TransformPoint(localMax);

    public bool IsConfigured()
    {
        return roomController != null && roomController.enabled &&
            entrance != null &&
            roomController.transform.IsChildOf(transform) &&
            entrance.IsChildOf(transform) &&
            roomController.gameObject.activeInHierarchy &&
            (clearWithoutExit || (roomExit != null && roomExit.transform.IsChildOf(transform) && roomExit.gameObject.activeInHierarchy)) &&
            localMin.x < localMax.x && localMin.y < localMax.y;
    }

    public void Bind(PlayerHealth player, DungeonRoomManager manager)
    {
        if (roomExit != null) roomExit.Bind(this, manager);
        foreach (MonoBehaviour component in GetComponentsInChildren<MonoBehaviour>(true))
            if (component is IMonsterAI monster) monster.SetTarget(player);
        foreach (StoneGolemBossAI boss in GetComponentsInChildren<StoneGolemBossAI>(true))
            boss.SetTarget(player, this);
    }
}
