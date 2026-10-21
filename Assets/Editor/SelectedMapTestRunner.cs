using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Owns Editor lifecycle only. Combat, damage counters, revives and room creation use the game code.
[InitializeOnLoad]
internal static class SelectedMapTestRunner
{
    const string P = SelectedMapTestSafety.Prefix;
    const string OverrideKey = "MineDungeon.SaveTestPath";
    const string Main = "Assets/Scenes/Main.unity";
    static DungeonRoomManager rooms;
    static PlayerHealth player;
    internal static string Status => SessionState.GetString(P + "Status", "대기");
    [Serializable] internal sealed class Record
    {
        public float seconds;
        public long damageTaken, damageDealt, kills;
        public int deaths, total;
    }
    internal static Record Stats => JsonUtility.FromJson<Record>(SessionState.GetString(P + "Stats", "{}"));
    static SelectedMapTestRunner()
    {
        EditorApplication.playModeStateChanged += Mode;
        EditorApplication.update += Tick;
        AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
        Application.logMessageReceived += Log;
        bool resumedRunning = SelectedMapTestSafety.Active && SessionState.GetString(P + "State", "") == "running";
        EditorApplication.delayCall += () => Recover(resumedRunning);
    }
    internal static void Validate(DungeonFloorData floor, int difficulty, DungeonRoom map)
    {
        if (!floor || difficulty < 0 || difficulty > 3 || !map) throw new InvalidOperationException("층·난이도·맵을 선택하세요.");
        string path = AssetDatabase.GetAssetPath(map);
        bool allowed = false;
        for (int i = 1; i <= 7; i++) if (path == $"Assets/Prefabs/Rooms/ReferenceMaps/Map_{i:00}.prefab") allowed = true;
        // Prefab assets have activeInHierarchy=false; IsConfigured is the runtime instance check.
        using (var serialized = new SerializedObject(map))
            if (!allowed || !map.gameObject.activeSelf || !map.Controller || !map.Controller.enabled || !map.Entrance ||
                !serialized.FindProperty("roomExit").objectReferenceValue || map.WorldMin.x >= map.WorldMax.x || map.WorldMin.y >= map.WorldMax.y)
                throw new InvalidOperationException("Map_01~07의 유효한 일반 맵만 테스트할 수 있습니다.");
        var p = floor.monsterProfile;
        if (!p) throw new InvalidOperationException("층 밸런스 프로필 미설정");
        p.ValidateBalance(difficulty);
        var layout = p.GetDifficulty(difficulty).Find(map);
        if (!layout || !layout.configured) throw new InvalidOperationException("선택한 층·난이도·맵의 독립 배치 미설정");
        p.ValidateMap(difficulty, layout);
        foreach (var species in MonsterDataStore.Catalog)
            p.Calculate(difficulty, AssetDatabase.LoadAssetAtPath<GameObject>(species.Path).GetComponent<EnemyHealth>(), out _, out _);
        if (EditorUtility.IsDirty(floor) || EditorUtility.IsDirty(p) || EditorUtility.IsDirty(layout))
            throw new InvalidOperationException("선택한 데이터의 미저장 변경을 먼저 저장하세요. 편집기 초안은 테스트에 적용되지 않습니다.");
    }
    internal static void Start(DungeonFloorData floor, int difficulty, DungeonRoom map)
    {
        if (SelectedMapTestSafety.Active || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            throw new InvalidOperationException("편집 모드에서 테스트를 시작하세요.");
        if (StageUtility.GetCurrentStage() != StageUtility.GetMainStage()) throw new InvalidOperationException("배치/프리팹 Scene 편집을 닫은 뒤 시작하세요.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty || string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path)) throw new InvalidOperationException("현재 씬의 미저장 변경을 먼저 저장하세요.");
        Validate(floor, difficulty, map);
        SelectedMapTestHistory.Load(); // Refuse to overwrite corrupt developer history.
        var main = AssetDatabase.LoadAssetAtPath<SceneAsset>(Main); if (!main) throw new InvalidOperationException("Main 씬이 없습니다.");
        string path = Path.GetFullPath(Path.Combine("Library/SelectedMapTests", Guid.NewGuid().ToString("N"), "character.json"));
        if (!SelectedMapTestSafety.IsOwnedPath(path)) throw new IOException("테스트 경로 검증 실패");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        // Probe only the new, uniquely owned path; never read/copy/reset real character data.
        try { File.WriteAllText(path + ".probe", "isolation"); File.Delete(path + ".probe"); }
        catch { Directory.Delete(Path.GetDirectoryName(path), true); throw; }
        SessionState.SetString(P + "OldOverride", SessionState.GetString(OverrideKey, ""));
        SessionState.SetString(P + "OldScene", AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetBool(P + "OldOptionsEnabled", EditorSettings.enterPlayModeOptionsEnabled);
        SessionState.SetInt(P + "OldOptions", (int)EditorSettings.enterPlayModeOptions);
        SessionState.SetBool(P + "OldBackground", Application.runInBackground);
        SessionState.SetFloat(P + "OldTimeScale", Time.timeScale); SessionState.SetBool(P + "OldAudio", AudioListener.pause);
        SessionState.SetString(P + "Floor", AssetDatabase.GetAssetPath(floor)); SessionState.SetInt(P + "Difficulty", difficulty);
        SessionState.SetString(P + "Map", AssetDatabase.GetAssetPath(map)); SessionState.SetString(P + "Path", path);
        SessionState.SetString(P + "Stats", "{}"); SessionState.SetBool(P + "Restart", false);
        SessionState.SetBool(P + "RestartPending", false);
        SessionState.SetBool(P + "SpawnVerified", false);
        SessionState.SetString(P + "State", "queued"); SessionState.SetString(P + "Status", "격리 Play 준비 중");
        SessionState.SetFloat(P + "Deadline", (float)EditorApplication.timeSinceStartup + 45);
        SessionState.SetBool(P + "Active", true);
        SelectedMapTestHistory.Begin(floor, difficulty, map);
        try
        {
            SessionState.SetString(OverrideKey, path);
            if (SessionState.GetString(OverrideKey, "") != path) throw new IOException("저장 override 설정 실패");
            EditorSceneManager.playModeStartScene = main;
            // Full scene/domain reload prevents stale player/singleton/coroutine state on every retry.
            EditorSettings.enterPlayModeOptionsEnabled = false;
            Application.runInBackground = true;
            EditorApplication.isPlaying = true;
        }
        catch { Cleanup(); throw; }
    }
    internal static void Stop(bool restart = false)
    {
        if (!SelectedMapTestSafety.Active) return;
        Archive(Status.StartsWith("오류") ? "error" : restart ? "restarted" : "stopped");
        SessionState.SetBool(P + "Restart", restart);
        SessionState.SetString(P + "State", "stopping");
        if (restart) SessionState.SetString(P + "Status", "재시작 준비 중");
        else if (Status == "전투 중" || Status.Contains("준비")) SessionState.SetString(P + "Status", "사용자 종료");
        if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
        else Cleanup();
    }
    static void Mode(PlayModeStateChange state)
    {
        if (!SelectedMapTestSafety.Active) return;
        if (state == PlayModeStateChange.ExitingPlayMode) Archive(Status.StartsWith("오류") ? "error" : SessionState.GetBool(P + "Restart", false) ? "restarted" : "stopped");
        if (state == PlayModeStateChange.EnteredEditMode) Cleanup();
        // Do not restore the override at ExitingPlayMode: OnApplicationQuit may still flush.
    }
    static void Recover(bool resumedRunning)
    {
        if (!SelectedMapTestSafety.Active) return;
        if (!EditorApplication.isPlayingOrWillChangePlaymode) Cleanup();
        else if (EditorApplication.isPlaying && resumedRunning) Error("스크립트 재로드로 테스트 종료");
    }
    static void BeforeReload()
    {
        if (SelectedMapTestSafety.Active && SessionState.GetString(P + "State", "") == "running")
        {
            Archive("error");
            SessionState.SetString(P + "Status", "스크립트 재로드로 테스트 종료");
        }
        if (player) player.Died -= Died;
    }
    static void Died(PlayerHealth ignored)
    {
        var record = Stats; record.deaths++; SessionState.SetString(P + "Stats", JsonUtility.ToJson(record));
    }
    static void Log(string message, string stack, LogType type)
    {
        if (SelectedMapTestSafety.Active && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) Error(message);
    }
    static void Error(string error)
    {
        SessionState.SetString(P + "Status", "오류: " + error); Stop();
    }
    static void Tick()
    {
        if (!SelectedMapTestSafety.Active)
        {
            // A delegate scheduled during Play exit can be lost in its domain reload. Keep the request in SessionState.
            if (SessionState.GetBool(P + "RestartPending", false) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling)
            {
                SessionState.SetBool(P + "RestartPending", false);
                try { Start(AssetDatabase.LoadAssetAtPath<DungeonFloorData>(SessionState.GetString(P + "Floor", "")), SessionState.GetInt(P + "Difficulty", 0), AssetDatabase.LoadAssetAtPath<DungeonRoom>(SessionState.GetString(P + "Map", ""))); }
                catch (Exception e) { SessionState.SetString(P + "Status", "재시작 실패: " + e.Message); }
            }
            return;
        }
        if (!EditorApplication.isPlaying) return;
        try
        {
            string state = SessionState.GetString(P + "State", ""); if (state == "stopping") return;
            if (!SelectedMapTestSafety.Matches(SessionState.GetString(OverrideKey, ""))) throw new IOException("저장 override 격리 상태 변경");
            var save = CharacterSaveService.Current;
            if (save && (!SelectedMapTestSafety.Matches(save.SavePath) || save.LoadBlocked)) throw new IOException("실제 SavePath 격리 확인 실패");
            if (state == "queued")
            {
                if ((float)EditorApplication.timeSinceStartup > SessionState.GetFloat(P + "Deadline", 0)) throw new TimeoutException("Main 초기화 대기 시간 초과");
                rooms = UnityEngine.Object.FindFirstObjectByType<DungeonRoomManager>();
                if (!save || !rooms || !rooms.EditorTestReady) return;
                var floor = AssetDatabase.LoadAssetAtPath<DungeonFloorData>(SessionState.GetString(P + "Floor", ""));
                var map = AssetDatabase.LoadAssetAtPath<DungeonRoom>(SessionState.GetString(P + "Map", ""));
                int difficulty = SessionState.GetInt(P + "Difficulty", 0); Validate(floor, difficulty, map);
                // Actual service path has been verified before starting combat or flushing any test data.
                Time.timeScale = 1; AudioListener.pause = false;
                player = UnityEngine.Object.FindFirstObjectByType<PlayerHealth>(); if (!player) throw new InvalidOperationException("플레이어 누락");
                var entrance = UnityEngine.Object.FindFirstObjectByType<MineEntranceUI>();
                if (!entrance) throw new InvalidOperationException("Main 입장 UI 누락");
                player.Died += Died;
                if (!rooms.EditorStartSelectedMap(floor, difficulty, map)) throw new InvalidOperationException("선택 맵 생성 실패");
                entrance.EditorCompleteDungeonEntry();
                VerifySpawn(floor.monsterProfile, difficulty, floor.monsterProfile.GetDifficulty(difficulty).Find(map), rooms.EditorTestRoom);
                var record = new Record { total = floor.monsterProfile.GetDifficulty(difficulty).Find(map).entries.Length };
                SessionState.SetString(P + "Stats", JsonUtility.ToJson(record));
                SessionState.SetString(P + "State", "running"); SessionState.SetString(P + "Status", "전투 중");
            }
            else
            {
                var run = DungeonRunManager.Instance;
                if (!rooms || !rooms.EditorTestRoom || !player || !run || !run.EditorSelectedMapTest || run.Result == null)
                    throw new InvalidOperationException("테스트 씬/플레이어/실행 상태가 변경되었습니다.");
                var record = Stats; var result = run.Result;
                record.seconds = result.seconds; record.damageTaken = result.damageTaken; record.damageDealt = result.damageDealt; record.kills = result.kills;
                SessionState.SetString(P + "Stats", JsonUtility.ToJson(record));
                if (run.IsRunEnded)
                {
                    SessionState.SetString(P + "Status", run.IsRunCleared ? "성공 · 몬스터 전멸" : "실패 · 부활 소진");
                    Archive(run.IsRunCleared ? "success" : "failed");
                }
            }
        }
        catch (Exception e) { Error(e.Message); }
    }
    static void Archive(string status)
    {
        var record = Stats; var run = DungeonRunManager.Instance;
        if (run && run.EditorSelectedMapTest && run.Result != null)
        {
            record.seconds = run.Result.seconds; record.damageTaken = run.Result.damageTaken; record.damageDealt = run.Result.damageDealt; record.kills = run.Result.kills;
            if (run.IsRunEnded) status = run.IsRunCleared ? "success" : "failed";
        }
        SessionState.SetString(P + "Stats", JsonUtility.ToJson(record));
        SelectedMapTestHistory.Archive(record, status);
    }
    static void VerifySpawn(MonsterFloorProfile balance, int difficulty, MonsterMapProfile layout, DungeonRoom room)
    {
        using (var serialized = new SerializedObject(room.Controller))
        {
            var enemies = serialized.FindProperty("enemies");
            if (enemies.arraySize != layout.entries.Length) throw new InvalidOperationException("생성된 몬스터 수 불일치");
            for (int i = 0; i < enemies.arraySize; i++)
            {
                var hp = enemies.GetArrayElementAtIndex(i).objectReferenceValue as EnemyHealth; var entry = layout.entries[i];
                balance.Calculate(difficulty, entry.template, out int expectedHp, out int expectedAttack);
                if (!hp || MonsterProfileRuntime.Kind(hp) != MonsterProfileRuntime.Kind(entry.template) ||
                    Vector3.Distance(hp.transform.localPosition, entry.localPosition) > .0001f || hp.MaxHealth != expectedHp || hp.CurrentHealth != expectedHp || MonsterProfileRuntime.Attack(hp) != expectedAttack)
                    throw new InvalidOperationException("생성된 몬스터 수치/배치 불일치: " + entry.id);
            }
        }
        SessionState.SetBool(P + "SpawnVerified", true);
    }
    static void Cleanup()
    {
        if (!SelectedMapTestSafety.Active || EditorApplication.isPlaying) return;
        Archive(Status.StartsWith("오류") ? "error" : SessionState.GetBool(P + "Restart", false) ? "restarted" : "stopped");
        if (player) player.Died -= Died; player = null; rooms = null;
        bool restart = SessionState.GetBool(P + "Restart", false);
        string path = SelectedMapTestSafety.Expected;
        try
        {
            SessionState.SetString(OverrideKey, SessionState.GetString(P + "OldOverride", ""));
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(P + "OldScene", ""));
            EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt(P + "OldOptions", 0);
            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(P + "OldOptionsEnabled", false);
            Application.runInBackground = SessionState.GetBool(P + "OldBackground", false);
            Time.timeScale = SessionState.GetFloat(P + "OldTimeScale", 1); AudioListener.pause = SessionState.GetBool(P + "OldAudio", false);
            if (SelectedMapTestSafety.IsOwnedPath(path) && Directory.Exists(Path.GetDirectoryName(path))) Directory.Delete(Path.GetDirectoryName(path), true);
        }
        catch (Exception e) { restart = false; SessionState.SetString(P + "Status", "설정 복원/임시 파일 정리 오류: " + e.Message); }
        finally { SessionState.SetBool(P + "Active", false); SessionState.SetString(P + "Path", ""); SessionState.SetString(P + "State", ""); }
        SessionState.SetBool(P + "RestartPending", restart);
    }
}
