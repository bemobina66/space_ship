using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    public static string PathName => Path.Combine(Application.persistentDataPath, "savegame.json");

    public static void Write(SaveData data)
    {
        try { File.WriteAllText(PathName, JsonUtility.ToJson(data, true)); }
        catch (Exception e) { Debug.LogError("Save failed: " + e.Message); }
    }

    public static SaveData Read()
    {
        try
        {
            if (!File.Exists(PathName)) return null;
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(PathName));
            return Valid(data) ? data : null;
        }
        catch (Exception e) { Debug.LogWarning("Save could not be read: " + e.Message); return null; }
    }

    public static bool Valid(SaveData d)
    {
        if (d == null || d.level < 1 || d.level > 3) return false;
        int[] rounds = { 5, 7, 10 };
        return d.round >= 1 && d.round <= rounds[d.level - 1]
            && d.lives >= 1 && d.lives <= 3 && d.weapon >= 1 && d.weapon <= 3
            && d.missiles >= 0 && d.food >= 0 && d.food < 10 && d.score >= 0
            && d.entryScore >= 0 && d.entryMissiles >= 0
            && d.entryWeapon >= 1 && d.entryWeapon <= 3 && d.entryFood >= 0 && d.entryFood < 10;
    }

    public static void ResetForDeath(SaveData d)
    {
        d.round = 1; d.lives = 3; d.score = d.entryScore;
        d.missiles = d.entryMissiles; d.weapon = d.entryWeapon; d.food = d.entryFood;
    }

    public static void Delete()
    {
        try { if (File.Exists(PathName)) File.Delete(PathName); }
        catch (Exception e) { Debug.LogError("Save deletion failed: " + e.Message); }
    }
}
