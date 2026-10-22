using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// A component on the existing persistent run manager; not another singleton.
[DefaultExecutionOrder(-1300), DisallowMultipleComponent]
public sealed class CharacterSaveService : MonoBehaviour
{
    [SerializeField] private SkillData[] skillCatalog;
    [SerializeField] private MaterialData[] materialCatalog;
    [SerializeField, Min(.05f)] private float writeDelay = .4f;
    private PlayerProgression growth;
    private RewardInventorySession rewards;
    private SkillInventorySession inventory;
    private bool ready, dirty, writing, invalidPrimary, stopping;
    private int transactions, lastSavedLevel = 1;
    private float writeAt;
    private readonly List<SavedItem> unknownSkills = new List<SavedItem>(), unknownMaterials = new List<SavedItem>();
    public bool IsPending => dirty || writing;
    public bool HasSaveError => LoadBlocked || (!string.IsNullOrEmpty(Status) && Status.StartsWith("저장 실패"));
    public bool LoadBlocked { get; private set; }
    public string Status { get; private set; }
    public string SavePath { get; private set; }
    public float ShakeMultiplier { get; private set; } = 1;
    public int SuccessfulWrites { get; private set; }
    internal bool CanCommitInvestment => ready && isActiveAndEnabled && !LoadBlocked && !writing && !stopping && transactions == 0;
    public static CharacterSaveService Current => DungeonRunManager.Instance != null ? DungeonRunManager.Instance.GetComponent<CharacterSaveService>() : null;
    private void Awake()
    {
        if (SkillInventorySession.Instance == null || SkillInventorySession.Instance.gameObject != gameObject) { enabled = false; return; }
        growth = GetComponent<PlayerProgression>(); rewards = GetComponent<RewardInventorySession>(); inventory = GetComponent<SkillInventorySession>();
        SavePath = Path.Combine(Application.persistentDataPath, "character.json");
#if UNITY_EDITOR
        // Editor development does not overwrite the standalone player's save.
        SavePath = Path.Combine(Application.persistentDataPath, "Editor", "character.json");
        string testPath = UnityEditor.SessionState.GetString("MineDungeon.SaveTestPath", "");
        if (!string.IsNullOrEmpty(testPath)) SavePath = testPath;
        // Validate before any load or write, including Awake/quit paths.
        if (SelectedMapTestSafety.Active && !SelectedMapTestSafety.Matches(SavePath))
        { LoadBlocked = true; Status = "선택 맵 테스트 저장 경로 격리 실패"; enabled = false; return; }
#endif
        LoadOnce();
        lastSavedLevel = growth.Level;
        ready = true;
        growth.Changed += RequestSave; rewards.Changed += RequestSave; inventory.Changed += RequestSave;
    }
    private void LoadOnce()
    {
        if (!File.Exists(SavePath) && !File.Exists(SavePath + ".bak")) return;
        CharacterSnapshot data; string error;
        if (!CharacterSaveFile.TryRead(SavePath, out data, out error))
        {
            if (error != null && error.StartsWith(CharacterSaveFile.GrowthConflict)) { BlockGrowthLoad(error); return; }
            invalidPrimary = File.Exists(SavePath);
            Debug.LogWarning("Character save primary unreadable: " + error);
            if (!CharacterSaveFile.TryRead(SavePath + ".bak", out data, out error))
            {
                LoadBlocked = true; Status = "저장을 불러오지 못했습니다. 기존 파일을 보호하기 위해 저장과 귀환·종료를 차단했습니다. 파일 복구 후 다시 실행해 주세요.";
                Debug.LogWarning("Character save recovery failed; automatic overwrite blocked: " + error); return;
            }
            Status = "백업에서 성장 데이터를 복구했습니다."; dirty = true; writeAt = Time.unscaledTime + writeDelay;
        }
        CharacterSnapshot prepared;
        if (!CharacterSaveFile.TryPrepareGrowth(data, growth, out prepared, out error)) { BlockGrowthLoad(error); return; }
        if (data.version < 3)
        {
            // Commit the complete snapshot before applying anything to the live sessions.
            if (!CharacterSaveFile.Write(SavePath, prepared, invalidPrimary, out error))
            { BlockGrowthLoad("이전 저장 실패: " + error); return; }
            invalidPrimary = false; dirty = false; SuccessfulWrites++;
            Status = "성장 데이터를 v3로 이전했습니다.";
        }
        data = prepared;
        GetComponent<DungeonRunManager>().LastSettledPlayId=data.lastSettledPlayId;
        growth.RestoreGrowth(data.level, data.experience, data.points, data.healthPoints, data.physicalPoints, data.magicPoints);
        var knownSkills = new List<SavedItem>();
        foreach (var row in data.skills) { if (row == null || string.IsNullOrEmpty(row.id)) continue; if (FindSkill(row.id) != null) knownSkills.Add(row); else { unknownSkills.Add(row); Debug.LogWarning("Unknown saved skill retained: " + row.id); } }
        inventory.RestoreSaved(knownSkills, FindSkill, data.eSlot, data.rSlot);
        var knownMaterials = new List<SavedItem>();
        foreach (var row in data.materials) { if (row == null || string.IsNullOrEmpty(row.id) || row.quantity <= 0) continue; if (FindMaterial(row.id) != null) knownMaterials.Add(row); else { unknownMaterials.Add(row); Debug.LogWarning("Unknown saved material retained: " + row.id); } }
        rewards.RestoreSaved(data.money, knownMaterials, FindMaterial);
        ShakeMultiplier = Mathf.Clamp(data.shakeMultiplier, 0, 2);
    }
    private void BlockGrowthLoad(string error)
    {
        LoadBlocked = true; Status = "성장 저장을 확인해야 합니다. 원본 보호를 위해 저장을 차단했습니다. " + error;
        Debug.LogWarning(Status);
    }
    private SkillData FindSkill(string id) => Array.Find(skillCatalog ?? Array.Empty<SkillData>(), s => s != null && s.id == id);
    private MaterialData FindMaterial(string id) => Array.Find(materialCatalog ?? Array.Empty<MaterialData>(), s => s != null && s.id == id);
    public CharacterSnapshot Capture()
    {
        var data = new CharacterSnapshot { level = growth.Level, experience = growth.Experience, points = growth.UnspentStatPoints,
            healthPoints = growth.HealthPoints, physicalPoints = growth.PhysicalPoints, magicPoints = growth.MagicPoints,
            money = rewards.Money, eSlot = inventory.SavedSlot(0), rSlot = inventory.SavedSlot(1), shakeMultiplier = ShakeMultiplier };
        data.lastSettledPlayId=GetComponent<DungeonRunManager>().LastSettledPlayId;
        foreach (var entry in inventory.Owned) data.skills.Add(new SavedItem { id = entry.Skill.id, order = entry.RegistrationOrder });
        foreach (var entry in rewards.Materials) data.materials.Add(new SavedItem { id = entry.Data.id, quantity = entry.Quantity, order = entry.RegistrationOrder });
        data.skills.AddRange(unknownSkills); data.materials.AddRange(unknownMaterials); return data;
    }

    internal bool TryCommitInvestment(PlayerProgression source, PlayerProgression.Stat stat, int amount, out string error)
    {
        error = null;
        if (!CanCommitInvestment || source != growth || !growth.CanInvest || amount <= 0 ||
            amount > growth.UnspentStatPoints || growth.PointsIn(stat) < 0 || amount > 100 - growth.PointsIn(stat))
        { error = "현재 상태에서는 투자할 수 없습니다."; return false; }
        var candidate = Capture();
        candidate.points -= amount;
        if (stat == PlayerProgression.Stat.Health) candidate.healthPoints += amount;
        else if (stat == PlayerProgression.Stat.Physical) candidate.physicalPoints += amount;
        else candidate.magicPoints += amount;
        writing = true;
        try
        {
            // No live mutation or Changed event until the entire candidate is on disk.
            if (!CharacterSaveFile.Write(SavePath, candidate, invalidPrimary, out error))
            { Status = "저장 실패: 스탯 투자가 적용되지 않았습니다. 다시 시도해 주세요."; return false; }
            invalidPrimary = false; dirty = false; lastSavedLevel = growth.Level; SuccessfulWrites++; Status = "";
            // Existing Changed subscribers may request an ordinary follow-up save; any such
            // changes remain dirty, rather than being cleared after notification.
            growth.ApplySavedInvestment(candidate.points, candidate.healthPoints, candidate.physicalPoints, candidate.magicPoints);
            return true;
        }
        finally { writing = false; }
    }
#if UNITY_EDITOR
    // Synchronous save service: no old worker can finish after this transaction.
    public bool EditorResetProgress(string defaultE, string defaultR, out string error)
    {
        error = null;
        var run = GetComponent<DungeonRunManager>();
        if (!Application.isPlaying || !ready || writing || transactions != 0 || run.IsRestarting)
        { error = "저장·보상 지급·씬 전환이 끝난 후 다시 시도해 주세요."; return false; }
        if (!Application.CanStreamedLevelBeLoaded("Assets/Scenes/Main.unity"))
        { error = "Main 씬을 불러올 수 없습니다."; return false; }
        var data = new CharacterSnapshot { shakeMultiplier = ShakeMultiplier, skills = inventory.EditorStartingSkills() };
        data.eSlot = data.skills.Exists(x => x.id == defaultE) ? defaultE : data.skills.Count > 0 ? data.skills[0].id : null;
        data.rSlot = data.skills.Exists(x => x.id == defaultR) && defaultR != data.eSlot ? defaultR : null;
        if (data.rSlot == null) foreach (var item in data.skills) if (item.id != data.eSlot) { data.rSlot = item.id; break; }
        writing = true; dirty = false;
        try
        {
            // Two atomic writes replace both primary and recovery backup with the new snapshot.
            if (!CharacterSaveFile.Write(SavePath, data, invalidPrimary, out error) ||
                !CharacterSaveFile.Write(SavePath, data, false, out error))
            { LoadBlocked = true; Status = "저장 실패: 초기화를 완료하지 못했습니다. " + error; return false; }
            FindFirstObjectByType<DungeonRoomManager>()?.StopRoomCombat();
            FindFirstObjectByType<PlayerHealth>()?.FinishDungeon(false);
            LoadBlocked = false; invalidPrimary = false; unknownSkills.Clear(); unknownMaterials.Clear();
            growth.RestoreGrowth(data.level, data.experience, data.points);
            inventory.RestoreSaved(data.skills, FindSkill, data.eSlot, data.rSlot);
            rewards.RestoreSaved(0, data.materials, FindMaterial);
            run.LastSettledPlayId = null;
            dirty = false; Status = ""; SuccessfulWrites += 2;
        }
        catch (Exception ex) { error = ex.Message; LoadBlocked = true; Status = "저장 실패: " + error; return false; }
        finally { writing = false; }
        // ReturnToMain resets the run and removes scene-owned combat/UI objects.
        if (!run.ReturnToMain()) { error = "초기 데이터는 저장됐지만 Main 씬 이동에 실패했습니다."; return false; }
        return true;
    }
#endif
    public void RequestSave()
    {
        if (!ready || LoadBlocked) return;
        dirty = true; writeAt = Time.unscaledTime + writeDelay;
        if (growth.Level != lastSavedLevel && transactions == 0) Flush();
    }
    public void BeginTransaction() { transactions++; }
    public void EndTransaction() { transactions = Math.Max(0, transactions - 1); RequestSave(); }
    public void SetShakeMultiplier(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) return;
        ShakeMultiplier = Mathf.Clamp(value, 0, 2); RequestSave();
    }
    public bool Flush()
    {
        if (!ready || LoadBlocked || writing || transactions > 0) return false;
        if (!dirty && File.Exists(SavePath)) return true;
        writing = true;
        try
        {
            string error;
            if (!CharacterSaveFile.Write(SavePath, Capture(), invalidPrimary, out error)) { Status = "저장 실패: 재시도하거나 취소해 주세요."; Debug.LogWarning("Character save failed: " + error); writeAt = Time.unscaledTime + 5; return false; }
            invalidPrimary = false; dirty = false; lastSavedLevel = growth.Level; SuccessfulWrites++; Status = ""; return true;
        }
        finally { writing = false; }
    }
    private void Update() { if (dirty && transactions == 0 && Time.unscaledTime >= writeAt) Flush(); }
    private void OnApplicationQuit() { stopping = true; if (ready && !LoadBlocked) Flush(); }
    private void OnDestroy()
    {
        stopping = true;
        if (!ready) return;
        growth.Changed -= RequestSave; rewards.Changed -= RequestSave; inventory.Changed -= RequestSave;
    }
}
