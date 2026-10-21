#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

// Compiled only in the Editor. SessionState survives the Play Mode domain reload.
public static class SelectedMapTestSafety
{
    public const string Prefix = "MineDungeon.SelectedMapTest.";
    public static bool Active => SessionState.GetBool(Prefix + "Active", false);
    public static string Expected => SessionState.GetString(Prefix + "Path", "");
    public static bool IsOwnedPath(string path)
    {
        try
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/SelectedMapTests"));
            string full = Path.GetFullPath(path);
            var folder = new DirectoryInfo(Path.GetDirectoryName(full));
            Guid id;
            return Path.GetFileName(full) == "character.json" && Guid.TryParseExact(folder.Name, "N", out id) &&
                string.Equals(folder.Parent.FullName, root, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }
    public static bool Matches(string path) => Active && IsOwnedPath(Expected) && IsOwnedPath(path) &&
        string.Equals(Path.GetFullPath(path), Path.GetFullPath(Expected), StringComparison.OrdinalIgnoreCase);
}

public partial class DungeonRoomManager
{
    [SerializeField, HideInInspector] DungeonFloorData editorTestFloor;
    void EditorReleaseSelectedMap()
    {
        // Does not depend on singleton destruction order during Play exit.
        if (editorTestFloor) DestroyImmediate(editorTestFloor);
        editorTestFloor = null;
    }
    public DungeonRoom EditorTestRoom => currentRoom;
    public bool EditorTestReady => configured && IsPreparing;
    public bool EditorStartSelectedMap(DungeonFloorData floor, int difficulty, DungeonRoom map)
    {
        if (!SelectedMapTestSafety.Matches(CharacterSaveService.Current?.SavePath) ||
            !configured || !IsPreparing || IsTransitioning || !floor || !map ||
            !floor.monsterProfile || !floor.monsterProfile.GetDifficulty(difficulty)?.Find(map)) return false;
        floor.monsterProfile.ValidateMap(difficulty, floor.monsterProfile.GetDifficulty(difficulty).Find(map));
        // Only this in-memory run copy has a single normal room; real content rules are unchanged.
        var copy = Instantiate(floor); copy.hideFlags = HideFlags.HideAndDontSave;
        copy.name = "SelectedMapTestRun_" + floor.name; editorTestFloor = copy;
        if (copy.difficulties == null || copy.difficulties.Length != 4) copy.difficulties = new DungeonFloorData.Difficulty[4];
        var setting = new DungeonFloorData.Difficulty { normalRooms = new[] { map }, normalRoomCount = 1 };
        copy.difficulties[difficulty] = setting;
        CurrentFloor = copy; CurrentDifficulty = difficulty;
        DungeonRunManager.Instance.EditorSelectedMapTest = true;
        try
        {
            if (BeginConfiguredDungeon(setting) && currentRoom) return true;
            DungeonRunManager.Instance.EditorSelectedMapTest = false;
            CurrentFloor = null; EditorReleaseSelectedMap(); return false;
        }
        catch { DungeonRunManager.Instance.EditorSelectedMapTest = false; CurrentFloor = null; EditorReleaseSelectedMap(); throw; }
    }
}

public partial class DungeonRunManager
{
    public bool EditorSelectedMapTest { get; internal set; }
    public void EditorFinishSelectedMapTest(bool cleared)
    {
        if (!EditorSelectedMapTest || IsRunEnded) return;
        IsRunCleared = cleared; IsRunFailed = !cleared; IsTracking = false;
        pendingClear = pendingFailure = reviveInFlight = false;
        if (Result != null) { Result.cleared = cleared; Result.usedRevives = completedRevives; }
        var player = FindFirstObjectByType<PlayerHealth>(); if (player) player.FinishDungeon(cleared);
        if (trackedRooms) trackedRooms.StopRoomCombat();
        // No clear rewards, settlement marker, RunEnded/result-window event, or regular retry queue.
        ResourcesChanged?.Invoke();
    }
}
#endif
