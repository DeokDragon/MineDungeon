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
    public bool IsPreparing { get; private set; } = true;
    private bool loadingRoom;
    public bool IsTransitioning => loadingRoom || pendingExit != null;
    private bool configured;
    public int TotalRoomCount => normalRoomCount + 1;
    public EnemyHealth CurrentBoss { get; private set; }
    public event System.Action<EnemyHealth> BossChanged;
    public DungeonFloorData CurrentFloor { get; private set; }
    public int CurrentDifficulty { get; private set; }
    public bool StartDungeon(DungeonFloorData floor,int difficulty)
    {
        var setting=floor!=null?floor.GetDifficulty(difficulty):null;
        var run=DungeonRunManager.Instance;
        var growth=run!=null?run.GetComponent<PlayerProgression>():null;
        if(!IsPreparing||!configured||IsTransitioning||player.IsDead||setting==null||!setting.IsPlayable||growth==null||growth.Level<Mathf.Max(1,setting.requiredLevel))return false;
        CurrentFloor=floor;CurrentDifficulty=difficulty;
        return StartDungeon();
    }

    private void OnRoomCleared(RoomController controller)
    {
        if (IsCompleted || currentRoom == null || controller != currentRoom.Controller ||
            CurrentRoomNumber != TotalRoomCount || RunEnded()) return;
        IsCompleted = true;
        pendingExit = null;
        DungeonRunManager.Instance.CompleteRun();
    }

    public void StopRoomCombat()
    {
        if(player) player.GetComponent<PlayerMovement>()?.ClearSlow();
        pendingExit = null;
        if (currentRoom == null) return;
        foreach (StoneGolemBossAI boss in currentRoom.GetComponentsInChildren<StoneGolemBossAI>(true))
            boss.enabled = false; // OnDisable cancels owned effects and pending hits.
        foreach (MonoBehaviour component in currentRoom.GetComponentsInChildren<MonoBehaviour>(true))
            if (component is IMonsterAI monster) { monster.CancelAttacks(); component.enabled = false; }

    }

    private void OnDestroy()
    {
        CurrentBoss = null;
        BossChanged?.Invoke(null);
        if (currentRoom != null) currentRoom.Controller.Cleared -= OnRoomCleared;
    }

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
        configured = true;
        DungeonRunManager.Instance?.TryStartQueuedDungeon(this);
    }

    public bool StartDungeon()
    {
        var setting=CurrentFloor!=null?CurrentFloor.GetDifficulty(CurrentDifficulty):null;
        var growth=DungeonRunManager.Instance!=null?DungeonRunManager.Instance.GetComponent<PlayerProgression>():null;
        if(setting==null||!setting.IsPlayable||growth==null||growth.Level<Mathf.Max(1,setting.requiredLevel))return false;
        if (!configured || !IsPreparing || IsTransitioning || currentRoom != null ||
            DungeonRunManager.Instance == null || DungeonRunManager.Instance.IsRestarting) return false;
        if (CharacterSaveService.Current != null && !CharacterSaveService.Current.Flush()) return false;
        normalRoomPrefabs=setting.normalRooms;bossRoomPrefab=setting.bossRoom;normalRoomCount=setting.normalRoomCount;
        IsPreparing = false; // Guard before generation; double clicks cannot create another room.
        DungeonRunManager.Instance.BeginNewRun();
        DungeonRunManager.Instance.GetComponent<RewardInventorySession>()?.BeginChallenge();
        player.GetComponent<PlayerSkills>()?.ResetForNewRun();
        player.Heal(player.MaxHealth);
        DungeonRunManager.Instance.BeginTracking(this);
        LoadRoom(1);
        return true;
    }

    private bool IsPrefab(DungeonRoom room)
    {
        return room != null && !room.gameObject.scene.IsValid() && room.gameObject.activeSelf &&
            room.transform.parent == null;
    }

    public void RequestNextRoom(DungeonRoom source, PlayerHealth enteringPlayer)
    {
        if (IsPreparing || !enabled || IsCompleted || pendingExit != null || source == null ||
            source != currentRoom || !source.IsCleared || enteringPlayer != player ||
            player.IsDead || Time.timeScale == 0f || RunEnded()) return;
        pendingExit = source;
    }

    private void LateUpdate()
    {
        // 물리 충돌 콜백이 끝난 뒤 전환하여 같은 프레임의 중복 진입을 막습니다.
        if (pendingExit == null) return;
        pendingExit = null;
        if (IsCompleted || player.IsDead || Time.timeScale == 0f || RunEnded()) return;

        if (CurrentRoomNumber == normalRoomCount + 1)
        {
            return;
        }
        LoadRoom(CurrentRoomNumber + 1);
    }

    private bool RunEnded()
    {
        return DungeonRunManager.Instance != null && DungeonRunManager.Instance.IsRunEnded;
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
        loadingRoom = true;
        player.GetComponent<PlayerMovement>()?.ClearSlow();
        bool isBoss = number > normalRoomCount;
        int chosenIndex = isBoss ? -1 : PickNormalRoom();
        DungeonRoom prefab = isBoss ? bossRoomPrefab : normalRoomPrefabs[chosenIndex];
        DungeonRoom oldRoom = currentRoom;
        var skills = player.GetComponent<PlayerSkills>();
        if (skills != null) skills.CancelAll();
        if (oldRoom != null) oldRoom.gameObject.SetActive(false);

        DungeonRoom next = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        if (!next.IsConfigured())
        {
            Debug.LogError($"{prefab.name}: DungeonRoom의 Controller, Exit, Entrance와 방 범위를 확인하세요.", this);
            next.gameObject.SetActive(false);
            Destroy(next.gameObject);
            if (oldRoom != null) oldRoom.gameObject.SetActive(true);
            enabled = false;
            loadingRoom = false;
            return;
        }

        if (oldRoom != null) oldRoom.Controller.Cleared -= OnRoomCleared;
        currentRoom = next;
        currentRoom.Controller.Cleared += OnRoomCleared;
        currentRoom.Bind(player, this);
        var roomBoss = currentRoom.GetComponentInChildren<StoneGolemBossAI>(true);
        CurrentBoss = roomBoss != null ? roomBoss.GetComponent<EnemyHealth>() : null;
        BossChanged?.Invoke(CurrentBoss);
        var damageScreen = FindFirstObjectByType<PlayerDamageScreen>();
        if (damageScreen != null) damageScreen.Clear();
        player.EnterRoom(currentRoom.Entrance);
        cameraFollow.SetRoom(player.transform, currentRoom.WorldMin, currentRoom.WorldMax);
        CurrentRoomNumber = number;
        if (!isBoss) previousIndex = chosenIndex;
        if (oldRoom != null) Destroy(oldRoom.gameObject);
        Debug.Log($"방 {number}/{normalRoomCount + 1} 입장: {prefab.name}", this);
        loadingRoom = false;
    }
}
