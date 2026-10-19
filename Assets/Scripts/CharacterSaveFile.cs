using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

[Serializable] public sealed class SavedItem
{
    public string id;
    public long quantity = 1, order;
}
[Serializable] public sealed class CharacterSnapshot
{
    public int version = 1;
    public int level = 1, experience, points;
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
            if (data == null || data.version != 1 || data.level < 1 || data.experience < 0 || data.points < 0 || data.money < 0 || float.IsNaN(data.shakeMultiplier) || float.IsInfinity(data.shakeMultiplier)) throw new InvalidDataException("Invalid or unsupported character save");
            data.skills = data.skills ?? new List<SavedItem>(); data.materials = data.materials ?? new List<SavedItem>();
            return true;
        }
        catch (Exception ex) { data = null; error = ex.Message; return false; }
    }
    public static bool Write(string path, CharacterSnapshot data, bool preserveInvalidPrimary, out string error)
    {
        error = null;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
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
