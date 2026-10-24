using System.Collections.Generic;
using UnityEngine;

public class RoomController : MonoBehaviour
{
    [SerializeField] private EnemyHealth[] enemies;
    [SerializeField] private RoomExit roomExit;

    private readonly HashSet<EnemyHealth> livingEnemies = new HashSet<EnemyHealth>();
    bool started;
    public void ConfigureSpawnEnemies(EnemyHealth[] value)
    {
        if (started || !gameObject.scene.IsValid()) throw new System.InvalidOperationException("몬스터 등록은 방 Start 전에만 설정할 수 있습니다.");
        enemies = value;
    }

    public int RemainingEnemies => livingEnemies.Count;
    public bool IsCleared { get; private set; }
    public bool HadCombatTargets { get; private set; }
    private bool clearRecoveryConsumed;
    internal bool TryConsumeClearRecovery()
    {
        if (!IsCleared || !HadCombatTargets || clearRecoveryConsumed) return false;
        clearRecoveryConsumed = true;
        return true;
    }
    public event System.Action<RoomController> Cleared;

    private void Awake()
    {
        if (roomExit != null)
            roomExit.SetOpen(false);
    }

    private void Start()
    {
        started = true;
        var room = GetComponentInParent<DungeonRoom>();
        if ((roomExit == null && (room == null || !room.ClearWithoutExit)) || enemies == null)
        {
            Debug.LogError("Assign the room's enemies and exit in the Inspector.", this);
            return;
        }

        bool hasMissingEnemy = false;
        foreach (EnemyHealth enemy in enemies)
        {
            if (enemy == null)
            {
                hasMissingEnemy = true;
                continue;
            }

            if (!enemy.IsDead && livingEnemies.Add(enemy))
            {
                HadCombatTargets = true;
                enemy.Died += OnEnemyDied;
            }
        }

        if (hasMissingEnemy)
        {
            Debug.LogError("Room enemy list contains a missing reference. Exit stays locked.", this);
            enabled = false;
            return;
        }

        TryClearRoom();
    }

    private void OnEnemyDied(EnemyHealth enemy)
    {
        if (!livingEnemies.Remove(enemy))
            return;

        enemy.Died -= OnEnemyDied;
        if (enabled)
            TryClearRoom();
    }

    private void TryClearRoom()
    {
        if (IsCleared || livingEnemies.Count != 0)
            return;

        IsCleared = true;
        if (roomExit != null) roomExit.SetOpen(true);
        Cleared?.Invoke(this);
        Debug.Log(roomExit != null ? "Room cleared! The exit is open." : "Room cleared!", this);
    }

    private void OnDestroy()
    {
        foreach (EnemyHealth enemy in livingEnemies)
        {
            if (enemy != null)
                enemy.Died -= OnEnemyDied;
        }
    }
}
