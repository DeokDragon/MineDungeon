using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor-only safety and launch glue for the isolated prototype scene.
[InitializeOnLoad]
public static class Cave01PlaySession
{
    public const string ScenePath = "Assets/Prototypes/Cave01/Cave01_Test.unity";
    public const string BossScenePath = "Assets/Prototypes/BossRoom24x22/BossRoom24x22_Test.unity";
    public const string ScenePathV2 = "Assets/Prototypes/Cave01V2/Cave01V2_Test.unity";
    public const string ScenePath02 = "Assets/Prototypes/Cave02/Cave02_Test.unity";
    public const string ScenePath03 = "Assets/Prototypes/Cave03/Cave03_Test.unity";
    public const string ScenePath05 = "Assets/Prototypes/Cave05/Cave05_Test.unity";
    public const string ScenePath06 = "Assets/Prototypes/Cave06/Cave06_Test.unity";
    public const string ScenePath04 = "Assets/Prototypes/Cave04/Cave04_Test.unity";
    public const string ScenePath07 = "Assets/Prototypes/Cave07/Cave07_Test.unity";
    public const string ScenePath08 = "Assets/Prototypes/Cave08/Cave08_Test.unity";
    public const string ScenePath09 = "Assets/Prototypes/Cave09/Cave09_Test.unity";
    public const string ScenePath10 = "Assets/Prototypes/Cave10/Cave10_Test.unity";
    public const string SequenceScenePath = "Assets/Prototypes/Cave03/Cave02_Cave03_Test.unity";
    const string Key = "Cave01Prototype.";
    static Cave01PlaySession()
    {
        EditorApplication.playModeStateChanged += Mode;
        EditorApplication.update += Tick;
    }
    [MenuItem("Tools/MineDungeon/Prototypes/Open Cave 01")]
    static void Open()
    {
        OpenScene(ScenePath);
    }
    [MenuItem("Tools/MineDungeon/Prototypes/Open Boss Room 24x22")]
    static void OpenBossRoom()
    {
        OpenScene(BossScenePath);
    }
    [MenuItem("Tools/MineDungeon/Prototypes/Open Cave 01 V2")]
    static void OpenV2()
    {
        OpenScene(ScenePathV2);
    }
    [MenuItem("Tools/MineDungeon/Prototypes/Open Cave 02")]
    static void Open02()
    {
        OpenScene(ScenePath02);
    }
    [MenuItem("Tools/MineDungeon/Prototypes/Open Cave 03")]
    static void Open03()
    {
        OpenScene(ScenePath03);
    }
    [MenuItem("Tools/MineDungeon/Prototypes/Open Cave 05")]
    static void Open05()
    {
        OpenScene(ScenePath05);
    }
    [MenuItem("Tools/MineDungeon/Prototypes/Open Cave 06")]
    static void Open06()
    {
        OpenScene(ScenePath06);
    }
    [MenuItem("Tools/MineDungeon/Prototypes/Open Cave 04")]
    static void Open04()
    {
        OpenScene(ScenePath04);
    }
    [MenuItem("Tools/MineDungeon/Prototypes/Open Cave 07")]
    static void Open07()
    {
        OpenScene(ScenePath07);
    }
    [MenuItem("Tools/MineDungeon/Prototypes/Open Cave 08")]
    static void Open08()
    {
        OpenScene(ScenePath08);
    }
    [MenuItem("Tools/MineDungeon/Prototypes/Open Cave 09")]
    static void Open09()
    {
        OpenScene(ScenePath09);
    }
    [MenuItem("Tools/MineDungeon/Prototypes/Open Cave 10")]
    static void Open10()
    {
        OpenScene(ScenePath10);
    }
    [MenuItem("Tools/MineDungeon/Prototypes/Open Cave 02 then Cave 03")]
    static void OpenSequence()
    {
        OpenScene(SequenceScenePath);
    }
    static void OpenScene(string path)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(path);
    }
    static void Mode(PlayModeStateChange state)
    {
        // The selected-map runner owns its Main start scene and isolated save override.
        if (state == PlayModeStateChange.ExitingEditMode && SelectedMapTestSafety.Active) return;
        string scenePath = SceneManager.GetActiveScene().path;
        if (state == PlayModeStateChange.ExitingEditMode && (scenePath == BossScenePath || scenePath == ScenePath || scenePath == ScenePathV2 || scenePath == ScenePath02 || scenePath == ScenePath03 || scenePath == ScenePath05 || scenePath == ScenePath06 || scenePath == ScenePath04 || scenePath == ScenePath07 || scenePath == ScenePath08 || scenePath == ScenePath09 || scenePath == ScenePath10 || scenePath == SequenceScenePath))
        {
            if (EditorSceneManager.playModeStartScene != null && AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene) != scenePath)
            {
                Debug.LogError("Cave prototype: a different Play start scene is configured. Clear it before testing.");
                EditorApplication.isPlaying = false;
                return;
            }
            SessionState.SetString(Key + "scene", scenePath);
            SessionState.SetString(Key + "oldSave", SessionState.GetString("MineDungeon.SaveTestPath", ""));
            string path = Path.GetFullPath("Library/Cave01Sessions/" + Guid.NewGuid().ToString("N") + "/character.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            SessionState.SetString(Key + "path", path);
            SessionState.SetString("MineDungeon.SaveTestPath", path);
            SessionState.SetBool(Key + "active", true);
            SessionState.SetBool(Key + "started", false);
        }
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key + "active", false))
        {
            SessionState.SetString("MineDungeon.SaveTestPath", SessionState.GetString(Key + "oldSave", ""));
            SessionState.SetBool(Key + "active", false);
        }
    }
    static void Tick()
    {
        if (SelectedMapTestSafety.Active) return;
        if (!EditorApplication.isPlaying || !SessionState.GetBool(Key + "active", false) || SessionState.GetBool(Key + "started", false)) return;
        string scenePath = SessionState.GetString(Key + "scene", ScenePath);
        if (SceneManager.GetActiveScene().path != scenePath) return;
        var save = CharacterSaveService.Current;
        var rooms = UnityEngine.Object.FindFirstObjectByType<DungeonRoomManager>();
        if (!save || !rooms || !rooms.EditorTestReady) return;
        SessionState.SetBool(Key + "started", true);
        if (save.SavePath != SessionState.GetString(Key + "path", ""))
        {
            Debug.LogError("Cave prototype save isolation failed; stopping.");
            EditorApplication.isPlaying = false;
            return;
        }
        string floorPath = scenePath == BossScenePath
            ? "Assets/Prototypes/BossRoom24x22/BossRoom24x22_Floor.asset"
            : scenePath == ScenePath04
            ? "Assets/Prototypes/Cave04/Cave04_Floor.asset"
            : scenePath == ScenePath07
            ? "Assets/Prototypes/Cave07/Cave07_Floor.asset"
            : scenePath == ScenePath08
            ? "Assets/Prototypes/Cave08/Cave08_Floor.asset"
            : scenePath == ScenePath09
            ? "Assets/Prototypes/Cave09/Cave09_Floor.asset"
            : scenePath == ScenePath10
            ? "Assets/Prototypes/Cave10/Cave10_Floor.asset"
            : scenePath == ScenePath06
            ? "Assets/Prototypes/Cave06/Cave06_Floor.asset"
            : scenePath == ScenePath05
            ? "Assets/Prototypes/Cave05/Cave05_Floor.asset"
            : scenePath == SequenceScenePath
            ? "Assets/Prototypes/Cave03/Cave02_Cave03_Floor.asset"
            : scenePath == ScenePath03
            ? "Assets/Prototypes/Cave03/Cave03_Floor.asset"
            : scenePath == ScenePath02
            ? "Assets/Prototypes/Cave02/Cave02_Floor.asset"
            : scenePath == ScenePathV2
            ? "Assets/Prototypes/Cave01V2/Cave01V2_Floor.asset"
            : "Assets/Prototypes/Cave01/Cave01_Floor.asset";
        var floor = AssetDatabase.LoadAssetAtPath<DungeonFloorData>(floorPath);
        Camera.main.orthographicSize = 4.5f;
        bool entered = scenePath == SequenceScenePath
            ? rooms.EditorStartPrototypeSequence(floor, 0)
            : rooms.StartDungeon(floor, 0);
        if (!entered) { Debug.LogError("Cave prototype entry failed."); EditorApplication.isPlaying = false; return; }
        UnityEngine.Object.FindFirstObjectByType<MineEntranceUI>().EditorCompleteDungeonEntry();
    }
}
