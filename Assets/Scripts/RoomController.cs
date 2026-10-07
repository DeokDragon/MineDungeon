using System.Collections.Generic;
using UnityEngine;

public class RoomController : MonoBehaviour
{
    [SerializeField] private EnemyHealth[] enemies;
    [SerializeField] private RoomExit roomExit;

    private readonly HashSet<EnemyHealth> livingEnemies = new HashSet<EnemyHealth>();

    public int RemainingEnemies => livingEnemies.Count;
    public bool IsCleared { get; private set; }

    private void Awake()
    {
        if (roomExit != null)
            roomExit.SetOpen(false);
    }

    private void Start()
    {
        if (roomExit == null || enemies == null)
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
                enemy.Died += OnEnemyDied;
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
        roomExit.SetOpen(true);
        Debug.Log("Room cleared! The exit is open.", this);
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
