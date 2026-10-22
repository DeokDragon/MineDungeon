using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

// Explicit MCP verification. All fixtures live in a new Library directory, never user saves.
public static class ProgressionVerification
{
    [Serializable] private sealed class Envelope { public string payload, checksum; }
    public static void WriteFixture(string path, CharacterSnapshot data, string extra = "")
    {
        string payload = JsonUtility.ToJson(data);
        if (data.version == 1) payload = payload.Replace("\"progressionV2Applied\":true,", "").Replace("\"progressionV2Applied\":false,", "");
        if (data.version < 3) payload = System.Text.RegularExpressions.Regex.Replace(payload, "\"(?:healthPoints|physicalPoints|magicPoints)\":-?[0-9]+,", "");
        if (extra.Length > 0) payload = payload.Substring(0, payload.Length - 1) + "," + extra + "}";
        string checksum;
        using (var sha = SHA256.Create()) checksum = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(new Envelope { payload = payload, checksum = checksum }));
    }

    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit mode required");
        string root = Path.GetFullPath("Library/ProgressionVerification/" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var log = new List<string>();
        Action<bool, string> check = (ok, label) => { if (!ok) throw new Exception("FAIL " + label); log.Add("PASS " + label); };
        var go = new GameObject("Progression verification") { hideFlags = HideFlags.HideAndDontSave };
        go.SetActive(false);
        try
        {
            var growth = go.AddComponent<PlayerProgression>();
            var table = AssetDatabase.LoadAssetAtPath<LevelProgressionData>("Assets/Skills/LevelProgression.asset");
            typeof(PlayerProgression).GetField("configuration", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(growth, table);
            var restore = typeof(PlayerProgression).GetMethod("RestoreGrowth", BindingFlags.Instance | BindingFlags.NonPublic);
            Action<int, int, int> set = (l, e, p) => restore.Invoke(growth, new object[] { l, e, p, 0, 0, 0 });
            foreach (int points in new[] { 19, 20, 21 })
                for (int stat = 0; stat < 3; stat++)
                {
                    restore.Invoke(growth, new object[] { 50, 0, 150-points, stat == 0 ? points : 0, stat == 1 ? points : 0, stat == 2 ? points : 0 });
                    check(growth.HasHealth20Passive == (stat == 0 && points >= 20) &&
                        growth.HasPhysical20Passive == (stat == 1 && points >= 20) &&
                        growth.HasMagic20Passive == (stat == 2 && points >= 20), "independent passive boundary stat" + stat + " points" + points);
                }
            set(1, 0, 3);
            var potionHeal = typeof(PlayerPotion).GetMethod("CalculateHealAmount", BindingFlags.Static | BindingFlags.NonPublic);
            check((int)potionHeal.Invoke(null, new object[] {163, .35f, true}) == 69, "potion163 final single ceil ->69, not intermediate ceil70");
            check((int)potionHeal.Invoke(null, new object[] {163, .35f, false}) == 58, "inactive potion original ceil ->58");
            check((int)potionHeal.Invoke(null, new object[] {157, .35f, false}) == 55, "Health19 max157 original potion55");
            check((int)potionHeal.Invoke(null, new object[] {160, .35f, true}) == 68, "Health20 max160 boosted potion68");
            check((int)potionHeal.Invoke(null, new object[] {100, .35f, true}) == 42, "potion integer result no extra ceil");
            check(growth.Level == 1 && growth.UnspentStatPoints == 3 && table.MaximumLevel == 50, "new Lv1 / 3P / max50");
            check(table.experienceToNextLevel.Length == 49 && Enumerable.Range(1, 49).All(l => table.RequiredExperience(l) == 100 + 50 * (l - 1)) && table.experienceToNextLevel.Sum() == 63700, "49 table entries / 2500 final / 63700 total");
            check(PlayerSkillAttackPower.CalculateDamage(10, 7) == 11, "pickaxe base10 physical7 ->11");
            check(PlayerSkillAttackPower.CalculateDamage(15, 10) == 17 && PlayerSkillAttackPower.CalculateDamage(13, 50) == 20 &&
                PlayerSkillAttackPower.CalculateDamage(18, 25) == 23, "exact .5 rounds upward including even lower integer");
            check(PlayerSkillAttackPower.CalculateDamage(10, 4.999f) == 10 && PlayerSkillAttackPower.CalculateDamage(10, 5) == 11 &&
                PlayerSkillAttackPower.CalculateDamage(10, 5.001f) == 11, "below/at/above .5 boundary");
            check(PlayerSkillAttackPower.CalculateDamage(1, 49.9f) == 1, "stat is not rounded before damage");
            string[] skillNames = { "EarthSmash", "DrillDash", "TealBeam", "OreBlast" };
            int[] baseDamages = { 18, 13, 15, 18 }, baseline = { 19, 14, 16, 19 }, physical100 = { 26, 19, 16, 19 }, magic100 = { 19, 14, 22, 26 };
            for (int i = 0; i < skillNames.Length; i++)
            {
                var skill = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/Skills/" + skillNames[i] + ".asset");
                check(skill != null && skill.id == skillNames[i] && skill.baseDamage == baseDamages[i] &&
                    skill.primaryType == (i < 2 ? SkillDamageType.Physical : SkillDamageType.Magic), "editable base/type/ID " + skillNames[i]);
                check(skill.Damage(7, 6) == baseline[i], "initial damage " + skillNames[i]);
                check(skill.Damage(47, 6) == physical100[i] && skill.Damage(7, 46) == magic100[i], "physical/magic isolation " + skillNames[i]);
            }
            int notifications = 0, noticePoints = 0;
            growth.LeveledUp += (a, b, p) => { notifications++; noticePoints += p; };
            check(!growth.GrantExperience(-1) && growth.GrantExperience(0) && growth.Level == 1, "negative rejected / zero no-op");
            growth.GrantExperience(100);
            check(growth.Level == 2 && growth.Experience == 0 && growth.UnspentStatPoints == 6 && noticePoints == 3, "Lv2 total6P / notice+3");
            set(1, 0, 3); growth.GrantExperience(260);
            check(growth.Level == 3 && growth.Experience == 10 && growth.UnspentStatPoints == 9, "multiple level-ups and current-level EXP");
            set(1, 0, 3); growth.GrantExperience(63700);
            check(growth.Level == 50 && growth.Experience == 0 && growth.UnspentStatPoints == 150 && growth.Progress == 1, "exact Lv50 /150P / MAX");
            int before = notifications;
            growth.GrantExperience(int.MaxValue);
            check(growth.Level == 50 && growth.UnspentStatPoints == 150 && notifications == before, "cap ignores further EXP and notices");
            set(1, 0, 3); growth.GrantExperience(int.MaxValue);
            check(growth.Level == 50 && growth.UnspentStatPoints == 150, "large single grant safely caps");

            var scale = typeof(PlayerHealth).GetMethod("TryScaleHealth", BindingFlags.Static | BindingFlags.NonPublic);
            Action<int, int, int, bool, bool, int> scaleCheck = (hp, oldMax, newMax, dead, valid, expected) =>
            {
                var args = new object[] { hp, oldMax, newMax, dead, 0 };
                check((bool)scale.Invoke(null, args) == valid && (int)args[4] == expected,
                    "HP ratio " + hp + "/" + oldMax + "->" + newMax + " dead=" + dead);
            };
            scaleCheck(50, 100, 103, false, true, 52);
            scaleCheck(1, 100, 103, false, true, 2);
            scaleCheck(100, 100, 130, false, true, 130);
            scaleCheck(50, 100, 100, false, true, 50);
            scaleCheck(52, 103, 103, false, true, 52);
            scaleCheck(1, 400, 100, false, true, 1);
            scaleCheck(0, 100, 130, true, true, 0);
            scaleCheck(0, 100, 100, true, true, 0);
            scaleCheck(int.MaxValue - 1, int.MaxValue, int.MaxValue - 1, false, true, int.MaxValue - 1);
            scaleCheck(50, 0, 130, false, false, 50);
            scaleCheck(50, 100, 0, false, false, 50);
            scaleCheck(50, -100, 130, false, false, 50);
            scaleCheck(-1, 100, 130, false, false, -1);
            scaleCheck(101, 100, 130, false, false, 101);

            restore.Invoke(growth, new object[] { 50, 0, 53, 95, 2, 0 });
            check(growth.SpentStatPoints == 97 && growth.MaximumHealthBonus == 285 && Mathf.Abs(growth.PhysicalAttackBonus - .8f) < .0001f, "investment totals and unrounded bonuses");
            check(growth.InvestablePoints(PlayerProgression.Stat.Health, 10) == 5 && growth.InvestablePoints(PlayerProgression.Stat.Magic, 10) == 10, "x10 limited by stat cap");
            restore.Invoke(growth, new object[] { 50, 0, 50, 100, 0, 0 });
            check(growth.InvestablePoints(PlayerProgression.Stat.Health, 1) == 0, "100P cap preview");
            restore.Invoke(growth, new object[] { 1, 0, 1, 1, 1, 0 });
            check(growth.InvestablePoints(PlayerProgression.Stat.Magic, 5) == 1 && growth.InvestablePoints(PlayerProgression.Stat.Magic, 10) == 1, "partial investment preview for insufficient points");
            int invested; string investmentError;
            check(!growth.TryInvest(PlayerProgression.Stat.Magic, 1, out invested, out investmentError) && invested == 0 && growth.UnspentStatPoints == 1, "Edit mode API refuses investment without mutation");
            check(growth.InvestablePoints((PlayerProgression.Stat)99, 1) == 0 && growth.InvestablePoints(PlayerProgression.Stat.Health, -1) == 0 && growth.InvestablePoints(PlayerProgression.Stat.Health, 2) == 0, "invalid stat/multipliers rejected");
            set(1, 0, 3);

            string error; CharacterSnapshot read, migrated;
            foreach (int level in new[] { 1, 5, 50 })
            {
                string path = Path.Combine(root, "v2-level" + level, "character.json");
                var old = new CharacterSnapshot { version=2, level=level, points=level*3, money=123 };
                WriteFixture(path, old); File.Copy(path, path + ".bak");
                byte[] v2Original = File.ReadAllBytes(path);
                check(CharacterSaveFile.TryRead(path, out read, out error) && CharacterSaveFile.TryPrepareGrowth(read, growth, out migrated, out error), "v2 accepted Lv" + level);
                CharacterSaveFile.TryPrepareGrowth(read, growth, out migrated, out error);
                check(migrated.version == 3 && migrated.points == level*3 && migrated.healthPoints == 0 && migrated.physicalPoints == 0 && migrated.magicPoints == 0 && migrated.money == 123, "v2->v3 no reward recalculation Lv" + level);
                check(CharacterSaveFile.Write(path, migrated, false, out error) && Directory.GetFiles(path + ".pre-v3").Any(p => File.ReadAllBytes(p).SequenceEqual(v2Original)), "v2 source independently protected Lv" + level);
            }
            foreach (var bad in new[] {
                new CharacterSnapshot { level=50, points=50, healthPoints=101 },
                new CharacterSnapshot { healthPoints=-1, points=4 },
                new CharacterSnapshot { healthPoints=1 },
                new CharacterSnapshot { level=50, points=0, healthPoints=100, physicalPoints=100 } })
            {
                string path = Path.Combine(root, "invalid-stats-" + log.Count + ".json"); WriteFixture(path, bad);
                check(!CharacterSaveFile.TryRead(path, out read, out error) && !CharacterSaveFile.Write(path, bad, false, out error), "reader and writer reject invalid stat ranges/total " + log.Count);
            }
            for (int level = 1; level <= 5; level++)
            {
                var old = new CharacterSnapshot { version = 1, level = level, experience = level == 5 ? 0 : 37, points = level - 1,
                    money = 123456, eSlot = "unknown-skill", rSlot = "other", shakeMultiplier = .3f, lastSettledPlayId = "settled-run" };
                old.skills.Add(new SavedItem { id = "unknown-skill", quantity = 1, order = 7 });
                old.materials.Add(new SavedItem { id = "unknown-material", quantity = 987, order = 8 });
                string path = Path.Combine(root, "level" + level, "character.json");
                WriteFixture(path, old); WriteFixture(path + ".bak", new CharacterSnapshot { version = 1, points = 0, money = 55 });
                byte[] primary = File.ReadAllBytes(path), backup = File.ReadAllBytes(path + ".bak");
                check(CharacterSaveFile.TryRead(path, out read, out error) && CharacterSaveFile.TryPrepareGrowth(read, growth, out migrated, out error), "read valid v1 Lv" + level);
                CharacterSaveFile.TryPrepareGrowth(read, growth, out migrated, out error);
                check(migrated.level == old.level && migrated.experience == old.experience && migrated.points == level * 3 && migrated.version == 3 && migrated.progressionV2Applied && old.points == level - 1,
                    "one-time replacement / source unchanged Lv" + level);
                check(CharacterSaveFile.Write(path, migrated, false, out error), "atomic migration Lv" + level);
                check(Directory.GetFiles(path + ".pre-v2").Any(p => File.ReadAllBytes(p).SequenceEqual(primary)) && Directory.GetFiles(path + ".pre-v2").Any(p => File.ReadAllBytes(p).SequenceEqual(backup)), "both originals protected Lv" + level);
                for (int retry = 0; retry < 3; retry++)
                {
                    check(CharacterSaveFile.TryRead(path, out read, out error) && CharacterSaveFile.TryPrepareGrowth(read, growth, out migrated, out error) && read.points == level * 3 &&
                        read.money == old.money && read.skills[0].order == 7 && read.materials[0].quantity == 987 && read.eSlot == old.eSlot && read.rSlot == old.rSlot && read.shakeMultiplier == .3f && read.lastSettledPlayId == old.lastSettledPlayId, "reload preserves all data/no duplicate Lv" + level + "/" + retry);
                    check(CharacterSaveFile.Write(path, read, false, out error), "rotate recovery backup");
                }
                check(Directory.GetFiles(path + ".pre-v2").Any(p => File.ReadAllBytes(p).SequenceEqual(primary)) && Directory.GetFiles(path + ".pre-v2").Any(p => File.ReadAllBytes(p).SequenceEqual(backup)), "protected copies survive rotations Lv" + level);
            }
            foreach (var bad in new[] {
                new CharacterSnapshot { version=1, level=0, points=0 }, new CharacterSnapshot { version=1, level=6, points=5 },
                new CharacterSnapshot { version=1, level=2, points=0 }, new CharacterSnapshot { version=1, level=2, points=9 },
                new CharacterSnapshot { version=1, points=-1 }, new CharacterSnapshot { version=1, points=0, experience=-1 },
                new CharacterSnapshot { version=1, points=0, experience=100 }, new CharacterSnapshot { version=1, level=5, points=4, experience=1 },
                new CharacterSnapshot { level=51, points=153 }, new CharacterSnapshot { points=0 },
                new CharacterSnapshot { level=50, points=150, experience=1 }, new CharacterSnapshot { progressionV2Applied=false } })
                check(!CharacterSaveFile.TryPrepareGrowth(bad, growth, out migrated, out error), "anomaly blocked " + log.Count);
            string unknown = Path.Combine(root, "unknown.json");
            WriteFixture(unknown, new CharacterSnapshot { version=1, points=0 }, "\"investedPoints\":2");
            check(!CharacterSaveFile.TryRead(unknown, out read, out error) && error.StartsWith(CharacterSaveFile.GrowthConflict), "unknown v1 investment blocks instead of guessing");
            WriteFixture(unknown, new CharacterSnapshot { version=1, points=0, eSlot="a\\\"b:c" });
            check(CharacterSaveFile.TryRead(unknown, out read, out error), "escaped JSON strings do not become unknown fields");
            WriteFixture(unknown, new CharacterSnapshot { progressionV2Applied=false });
            check(!CharacterSaveFile.TryRead(unknown, out read, out error), "incomplete v2 marker blocked");
            string failure = Path.Combine(root, "failure", "character.json");
            WriteFixture(failure, new CharacterSnapshot { version=1, points=0 });
            File.Copy(failure, failure + ".bak");
            string original = File.ReadAllText(failure), originalBackup = File.ReadAllText(failure + ".bak");
            File.WriteAllText(failure + ".pre-v2", "block protection directory");
            check(!CharacterSaveFile.Write(failure, new CharacterSnapshot { progressionV2Applied=false }, false, out error) && File.ReadAllText(failure) == original, "writer rejects incomplete v2 before touching files");
            check(!CharacterSaveFile.Write(failure, new CharacterSnapshot { version=1, points=0 }, false, out error) && File.ReadAllText(failure) == original, "writer rejects inconsistent legacy schema");
            check(!CharacterSaveFile.Write(failure, new CharacterSnapshot(), false, out error) && File.ReadAllText(failure) == original && File.ReadAllText(failure + ".bak") == originalBackup, "protection failure preserves both originals");
            string locked = Path.Combine(root, "locked", "character.json");
            WriteFixture(locked, new CharacterSnapshot { version=1, points=0 }); File.Copy(locked, locked + ".bak");
            using (var held = new FileStream(locked + ".tmp", FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                check(!CharacterSaveFile.Write(locked, new CharacterSnapshot(), false, out error) && File.ReadAllText(locked) == original && File.ReadAllText(locked + ".bak") == originalBackup, "temp write failure preserves v1, no completion");
            check(CharacterSaveFile.Write(locked, new CharacterSnapshot(), false, out error) && CharacterSaveFile.TryRead(locked, out read, out error) && read.version == 3 && read.points == 3, "retry after releasing lock succeeds");
            File.WriteAllText(locked, "checksum broken");
            check(!CharacterSaveFile.TryRead(locked, out read, out error) && CharacterSaveFile.TryRead(locked + ".bak", out read, out error), "checksum detects corruption / backup readable");
            check(CharacterSaveFile.TryPrepareGrowth(read, growth, out migrated, out error) && CharacterSaveFile.Write(locked, migrated, true, out error) && Directory.GetFiles(Path.GetDirectoryName(locked), "*.corrupt-*").Length == 1, "recovery retains corrupt primary");
            return log.Count + " checks passed; " + root;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
            File.WriteAllLines(Path.Combine(root, "edit-results.txt"), log);
        }
    }
}
