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
    private bool ready, dirty, writing, invalidPrimary;
    private int transactions;
    private float writeAt;
    private readonly List<SavedItem> unknownSkills = new List<SavedItem>(), unknownMaterials = new List<SavedItem>();
    public bool LoadBlocked { get; private set; }
    public string Status { get; private set; }
    public string SavePath { get; private set; }
    public float ShakeMultiplier { get; private set; } = 1;
    public int SuccessfulWrites { get; private set; }
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
#endif
        LoadOnce();
        ready = true;
        growth.Changed += RequestSave; rewards.Changed += RequestSave; inventory.Changed += RequestSave;
    }
    private void LoadOnce()
    {
        if (!File.Exists(SavePath) && !File.Exists(SavePath + ".bak")) return;
        CharacterSnapshot data; string error;
        if (!CharacterSaveFile.TryRead(SavePath, out data, out error))
        {
            invalidPrimary = File.Exists(SavePath);
            Debug.LogWarning("Character save primary unreadable: " + error);
            if (!CharacterSaveFile.TryRead(SavePath + ".bak", out data, out error))
            {
                LoadBlocked = true; Status = "저장을 불러오지 못했습니다. 기존 파일을 보호하기 위해 저장과 귀환·종료를 차단했습니다. 파일 복구 후 다시 실행해 주세요.";
                Debug.LogWarning("Character save recovery failed; automatic overwrite blocked: " + error); return;
            }
            Status = "백업에서 성장 데이터를 복구했습니다."; dirty = true; writeAt = Time.unscaledTime + writeDelay;
        }
        growth.RestoreGrowth(data.level, data.experience, data.points);
        var knownSkills = new List<SavedItem>();
        foreach (var row in data.skills) { if (row == null || string.IsNullOrEmpty(row.id)) continue; if (FindSkill(row.id) != null) knownSkills.Add(row); else { unknownSkills.Add(row); Debug.LogWarning("Unknown saved skill retained: " + row.id); } }
        inventory.RestoreSaved(knownSkills, FindSkill, data.eSlot, data.rSlot);
        var knownMaterials = new List<SavedItem>();
        foreach (var row in data.materials) { if (row == null || string.IsNullOrEmpty(row.id) || row.quantity <= 0) continue; if (FindMaterial(row.id) != null) knownMaterials.Add(row); else { unknownMaterials.Add(row); Debug.LogWarning("Unknown saved material retained: " + row.id); } }
        rewards.RestoreSaved(data.money, knownMaterials, FindMaterial);
        ShakeMultiplier = Mathf.Clamp(data.shakeMultiplier, 0, 2);
    }
    private SkillData FindSkill(string id) => Array.Find(skillCatalog ?? Array.Empty<SkillData>(), s => s != null && s.id == id);
    private MaterialData FindMaterial(string id) => Array.Find(materialCatalog ?? Array.Empty<MaterialData>(), s => s != null && s.id == id);
    public CharacterSnapshot Capture()
    {
        var data = new CharacterSnapshot { level = growth.Level, experience = growth.Experience, points = growth.UnspentStatPoints, money = rewards.Money, eSlot = inventory.SavedSlot(0), rSlot = inventory.SavedSlot(1), shakeMultiplier = ShakeMultiplier };
        foreach (var entry in inventory.Owned) data.skills.Add(new SavedItem { id = entry.Skill.id, order = entry.RegistrationOrder });
        foreach (var entry in rewards.Materials) data.materials.Add(new SavedItem { id = entry.Data.id, quantity = entry.Quantity, order = entry.RegistrationOrder });
        data.skills.AddRange(unknownSkills); data.materials.AddRange(unknownMaterials); return data;
    }
    public void RequestSave() { if (!ready || LoadBlocked) return; dirty = true; writeAt = Time.unscaledTime + writeDelay; }
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
            invalidPrimary = false; dirty = false; SuccessfulWrites++; Status = ""; return true;
        }
        finally { writing = false; }
    }
    private void Update() { if (dirty && transactions == 0 && Time.unscaledTime >= writeAt) Flush(); }
    private void OnApplicationQuit() { if (ready && !LoadBlocked) Flush(); }
    private void OnDestroy()
    {
        if (!ready) return;
        growth.Changed -= RequestSave; rewards.Changed -= RequestSave; inventory.Changed -= RequestSave;
    }
}
