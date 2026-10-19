using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1400)]
[DisallowMultipleComponent]
public sealed class SkillInventorySession : MonoBehaviour
{
    public static SkillInventorySession Instance { get; private set; }
    [Header("테스트 지급: 실제 획득 목록으로 교체 가능 / 배열 순서 = 최초 등록 순서")]
    [SerializeField] private SkillData[] testStartingSkills;
    public sealed class Entry
    {
        public SkillData Skill { get; }
        public long RegistrationOrder { get; }
        internal Entry(SkillData skill, long order) { Skill = skill; RegistrationOrder = order; }
    }
    private readonly List<Entry> owned = new List<Entry>();
    public IReadOnlyList<Entry> Owned => owned;
    private string eId, rId;
    private bool initializedLoadout;
    private PlayerSkills player;
    public event Action Changed;
    private long registrationSequence;
    public long NextRegistrationOrder() { long order=registrationSequence; if(registrationSequence<long.MaxValue)registrationSequence++; return order; }
    public string SavedSlot(int slot) => slot == 0 ? eId : rId;
    internal void ObserveOrder(long order) { if (order >= registrationSequence) registrationSequence = order < long.MaxValue ? order + 1 : long.MaxValue; }
    internal void RestoreSaved(List<SavedItem> rows, Func<string, SkillData> resolve, string e, string r)
    {
        owned.Clear(); registrationSequence = 0;
        foreach (var row in rows)
        {
            var data = resolve(row.id); if (data == null || Find(row.id) != null) continue;
            long order = Math.Max(0, row.order); owned.Add(new Entry(data, order)); ObserveOrder(order);
        }
        eId = Find(e) != null ? e : null;
        rId = Find(r) != null && r != eId ? r : null;
        initializedLoadout = true;
        if (player != null) player.ApplyInventoryLoadout(Find(eId), Find(rId));
        Changed?.Invoke();
    }
    private void Awake()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (testStartingSkills != null) foreach (var skill in testStartingSkills) Register(skill);
    }
    // Idempotent registration; opening UI and scene reloads never grant additional copies.
    public bool Register(SkillData skill)
    {
        if (skill == null || string.IsNullOrWhiteSpace(skill.id) || Find(skill.id) != null) return false;
        owned.Add(new Entry(skill, NextRegistrationOrder())); Changed?.Invoke(); return true;
    }
    public SkillData Find(string id)
    {
        foreach (var entry in owned) if (entry.Skill != null && entry.Skill.id == id) return entry.Skill;
        return null;
    }
    public void Bind(PlayerSkills target)
    {
        player = target;
        if (!initializedLoadout)
        {
            var e = target.Equipped(0); var r = target.Equipped(1);
            eId = e != null && Find(e.id) != null ? e.id : owned.Count > 0 ? owned[0].Skill.id : null;
            rId = r != null && Find(r.id) != null && r.id != eId ? r.id : null;
            if (rId == null) foreach (var entry in owned) if (entry.Skill.id != eId) { rId = entry.Skill.id; break; }
            initializedLoadout = true;
        }
        player.ApplyInventoryLoadout(Find(eId), Find(rId)); Changed?.Invoke();
    }
    public bool TryEquip(int slot, string id, DungeonRoomManager rooms)
    {
        var run = DungeonRunManager.Instance;
        if (rooms == null || !rooms.IsPreparing || rooms.IsTransitioning || player == null ||
            player.GetComponent<PlayerHealth>().IsDead || (run != null && (run.IsRunEnded || run.IsRestarting)) ||
            slot < 0 || slot > 1 || Find(id) == null) return false;
        string current = slot == 0 ? eId : rId;
        if (current == id) return true;
        if (slot == 0) { if (rId == id) rId = eId; eId = id; }
        else { if (eId == id) eId = rId; rId = id; }
        player.ApplyInventoryLoadout(Find(eId), Find(rId)); Changed?.Invoke(); return true;
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
