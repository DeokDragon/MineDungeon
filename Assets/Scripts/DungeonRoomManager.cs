using UnityEngine;

// Player / Camera와 마찬가지로 방 밖의 별도 오브젝트에 붙입니다.
public class DungeonRoomManager : MonoBehaviour
{
    [SerializeField] private PlayerHealth player;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private DungeonRoom[] normalRoomPrefabs;
    [SerializeField] private DungeonRoom bossRoomPrefab;
    [SerializeField, Min(1)] private int normalRoomCount = 3;

    private DungeonRoom currentRoom;
    private DungeonRoom pendingExit;
    private int previousIndex = -1;
    public int CurrentRoomNumber { get; private set; }
    public bool IsCompleted { get; private set; }

    private void Start()
    {
        if (player == null || cameraFollow == null || normalRoomCount < 1 ||
            normalRoomPrefabs == null || normalRoomPrefabs.Length == 0 ||
            bossRoomPrefab == null)
        {
            Debug.LogError("DungeonRoomManager: Player, Camera Follow, 일반 방과 보스 방 프리팹을 연결하세요.", this);
            enabled = false;
            return;
        }
        foreach (DungeonRoom prefab in normalRoomPrefabs)
        {
            if (!IsPrefab(prefab))
            {
                Debug.LogError("일반 방 목록에는 Project 창의 활성화된 프리팹을 연결하세요.", this);
                enabled = false;
                return;
            }
        }
        if (!IsPrefab(bossRoomPrefab))
        {
            Debug.LogError("Boss Room Prefab에는 Project 창의 활성화된 프리팹을 연결하세요.", this);
            enabled = false;
            return;
        }
        LoadRoom(1);
    }

    private bool IsPrefab(DungeonRoom room)
    {
        return room != null && !room.gameObject.scene.IsValid() && room.gameObject.activeSelf &&
            room.transform.parent == null;
    }

    public void RequestNextRoom(DungeonRoom source, PlayerHealth enteringPlayer)
    {
        if (!enabled || IsCompleted || pendingExit != null || source == null ||
            source != currentRoom || !source.IsCleared || enteringPlayer != player ||
            player.IsDead || Time.timeScale == 0f || RunFailed()) return;
        pendingExit = source;
    }

    private void LateUpdate()
    {
        // 물리 충돌 콜백이 끝난 뒤 전환하여 같은 프레임의 중복 진입을 막습니다.
        if (pendingExit == null) return;
        pendingExit = null;
        if (player.IsDead || Time.timeScale == 0f || RunFailed()) return;

        if (CurrentRoomNumber == normalRoomCount + 1)
        {
            IsCompleted = true;
            Debug.Log("던전 클리어! (현재 단계: 클리어 로그까지 구현 / 로비 귀환은 추후 연결)", this);
            return;
        }
        LoadRoom(CurrentRoomNumber + 1);
    }

    private bool RunFailed()
    {
        return DungeonRunManager.Instance != null && DungeonRunManager.Instance.IsRunFailed;
    }

    private int PickNormalRoom()
    {
        int count = normalRoomPrefabs.Length;
        if (count == 1) return 0;
        if (previousIndex < 0) return Random.Range(0, count);
        int index = Random.Range(0, count - 1);
        return index >= previousIndex ? index + 1 : index;
    }

    private void LoadRoom(int number)
    {
        bool isBoss = number > normalRoomCount;
        int chosenIndex = isBoss ? -1 : PickNormalRoom();
        DungeonRoom prefab = isBoss ? bossRoomPrefab : normalRoomPrefabs[chosenIndex];
        DungeonRoom oldRoom = currentRoom;
        if (oldRoom != null) oldRoom.gameObject.SetActive(false);

        DungeonRoom next = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        if (!next.IsConfigured())
        {
            Debug.LogError($"{prefab.name}: DungeonRoom의 Controller, Exit, Entrance와 방 범위를 확인하세요.", this);
            next.gameObject.SetActive(false);
            Destroy(next.gameObject);
            if (oldRoom != null) oldRoom.gameObject.SetActive(true);
            enabled = false;
            return;
        }

        currentRoom = next;
        currentRoom.Bind(player, this);
        player.EnterRoom(currentRoom.Entrance);
        cameraFollow.SetRoom(player.transform, currentRoom.WorldMin, currentRoom.WorldMax);
        CurrentRoomNumber = number;
        if (!isBoss) previousIndex = chosenIndex;
        if (oldRoom != null) Destroy(oldRoom.gameObject);
        Debug.Log($"방 {number}/{normalRoomCount + 1} 입장: {prefab.name}", this);
    }
}
