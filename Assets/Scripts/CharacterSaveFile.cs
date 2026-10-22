using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable] public sealed class SavedItem
{
    public string id;
    public long quantity = 1, order;
}
[Serializable] public sealed class CharacterSnapshot
{
    public int version = 2;
    public bool progressionV2Applied = true;
    public int level = 1, experience, points = 3;
    public long money;
    public List<SavedItem> skills = new List<SavedItem>();
    public List<SavedItem> materials = new List<SavedItem>();
    public string eSlot, rSlot;
    public string lastSettledPlayId;
    public float shakeMultiplier = 1;
}

// No Unity objects, dungeon progress or derived combat stats are serialized.
public static class CharacterSaveFile
{
    // Semantic/unsupported saves must not silently fall back to an older character.
    public const string GrowthConflict = "Progression conflict: ";
    [Serializable] private sealed class Envelope { public string payload, checksum; }
    private static string Hash(string value)
    {
        using (var sha = SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }
    public static bool TryRead(string path, out CharacterSnapshot data, out string error)
    {
        data = null; error = null;
        try
        {
            var envelope = JsonUtility.FromJson<Envelope>(File.ReadAllText(path));
            if (envelope == null || string.IsNullOrEmpty(envelope.payload) || envelope.checksum != Hash(envelope.payload)) throw new InvalidDataException("Checksum mismatch");
            data = JsonUtility.FromJson<CharacterSnapshot>(envelope.payload);
            if (data == null) throw new InvalidDataException("Invalid character save");
            if (data.version != 1 && data.version != 2) throw new InvalidDataException(GrowthConflict + "unsupported save version " + data.version);
            // v1 has no investment feature. Unknown fields could encode investment data;
            // stop instead of discarding them through JsonUtility's unknown-field handling.
            const string known = "|version|level|experience|points|money|skills|materials|id|quantity|order|eSlot|rSlot|lastSettledPlayId|shakeMultiplier|";
            var fields = new HashSet<string>();
            // Scan complete JSON strings, so escaped quotes/colons inside item IDs are not fields.
            foreach (Match token in Regex.Matches(envelope.payload, @"""(?:\\.|[^""\\])*"""))
            {
                int next = token.Index + token.Length;
                while (next < envelope.payload.Length && char.IsWhiteSpace(envelope.payload[next])) next++;
                if (next >= envelope.payload.Length || envelope.payload[next] != ':') continue;
                string field = token.Value.Substring(1, token.Length - 2);
                fields.Add(field);
                if (!known.Contains("|" + field + "|") && !(data.version == 2 && field == "progressionV2Applied"))
                    throw new InvalidDataException(GrowthConflict + "unknown v" + data.version + " field " + field);
            }
            foreach (string field in new[] { "version", "level", "experience", "points" })
                if (!fields.Contains(field))
                    throw new InvalidDataException(GrowthConflict + "missing " + field);
            if (data.version == 2 && (!fields.Contains("progressionV2Applied") || !data.progressionV2Applied))
                throw new InvalidDataException(GrowthConflict + "incomplete v2 migration");
            if (data.level < 1 || data.experience < 0 || data.points < 0)
                throw new InvalidDataException(GrowthConflict + "negative growth or invalid level");
            if (data.money < 0 || float.IsNaN(data.shakeMultiplier) || float.IsInfinity(data.shakeMultiplier)) throw new InvalidDataException("Invalid character save");
            data.skills = data.skills ?? new List<SavedItem>(); data.materials = data.materials ?? new List<SavedItem>();
            return true;
        }
        catch (Exception ex) { data = null; error = ex.Message; return false; }
    }

    public static bool TryPrepareGrowth(CharacterSnapshot source, PlayerProgression growth, out CharacterSnapshot prepared, out string error)
    {
        prepared = null; error = null;
        bool legacy = source.version == 1;
        if ((!legacy && source.version != 2) || source.level < 1 || source.level > (legacy ? 5 : 50) ||
            source.level > growth.MaximumLevel || source.experience < 0 ||
            (source.level == (legacy ? 5 : growth.MaximumLevel) ? source.experience != 0 :
                source.experience >= growth.RequiredAt(source.level)) ||
            (legacy && source.level < 5 && source.experience >= 100 + 50 * (source.level - 1)) ||
            source.points != (legacy ? source.level - 1 : source.level * 3) ||
            (!legacy && !source.progressionV2Applied))
        { error = GrowthConflict + "v" + source.version + " Lv" + source.level + " EXP=" + source.experience +
            " points=" + source.points + "; invalid growth or migration state, no values were corrected"; return false; }
        prepared = JsonUtility.FromJson<CharacterSnapshot>(JsonUtility.ToJson(source));
        prepared.version = 2; prepared.progressionV2Applied = true;
        if (legacy) prepared.points = source.level * 3; // Known v1 investment is zero, never add to old points.
        return true;
    }

    private static void ProtectBeforeMigration(string path)
    {
        // Immutable, content-addressed copies survive all later .bak rotations and retries.
        string folder = path + ".pre-v2";
        foreach (string original in new[] { path, path + ".bak" })
        {
            if (!File.Exists(original)) continue;
            byte[] bytes = File.ReadAllBytes(original);
            string digest;
            using (var sha = SHA256.Create()) digest = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
            Directory.CreateDirectory(folder);
            string copy = Path.Combine(folder, (original == path ? "primary-" : "backup-") + digest + ".json");
            if (!File.Exists(copy))
            {
                string temp = copy + ".tmp-" + Guid.NewGuid().ToString("N");
                using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
                File.Move(temp, copy);
            }
            byte[] protectedBytes = File.ReadAllBytes(copy);
            if (Convert.ToBase64String(bytes) != Convert.ToBase64String(protectedBytes))
                throw new IOException("Pre-migration protection verification failed: " + copy);
        }
    }
    public static bool Write(string path, CharacterSnapshot data, bool preserveInvalidPrimary, out string error)
    {
        error = null;
        try
        {
            if (data == null || data.version != 2 || !data.progressionV2Applied)
                throw new InvalidDataException("Only completed v2 snapshots can be written");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            CharacterSnapshot existing; string readError;
            bool legacy = (TryRead(path, out existing, out readError) && existing.version == 1) |
                (TryRead(path + ".bak", out existing, out readError) && existing.version == 1);
            if (data.version == 2 && legacy) ProtectBeforeMigration(path);
            var payload = JsonUtility.ToJson(data);
            var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new Envelope { payload = payload, checksum = Hash(payload) }, true));
            string temp = path + ".tmp";
            using (var file = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)) { file.Write(bytes, 0, bytes.Length); file.Flush(true); }
            CharacterSnapshot previous; string previousError;
            if (File.Exists(path) && !TryRead(path, out previous, out previousError)) preserveInvalidPrimary = true;
            if (File.Exists(path) && preserveInvalidPrimary) File.Move(path, path + ".corrupt-" + Guid.NewGuid().ToString("N"));
            if (File.Exists(path)) File.Replace(temp, path, path + ".bak");
            else { if (!File.Exists(path + ".bak")) File.Copy(temp, path + ".bak"); File.Move(temp, path); }
            return true;
        }
        catch (Exception ex) { error = ex.Message; return false; }
    }
}
