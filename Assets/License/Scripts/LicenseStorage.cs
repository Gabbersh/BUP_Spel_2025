using System;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
public class LocalLicense
{
    public string tier;
    public string expiryUtc; // "yyyy-MM-dd"
}

public static class LicenseStorage
{
    private const string FileName = "license.json";

    private static string FilePath =>
        Path.Combine(Application.persistentDataPath, FileName);

    public static bool TryLoad(out LocalLicense license)
    {
        license = null;

        try
        {
            if (!File.Exists(FilePath))
                return false;

            var json = File.ReadAllText(FilePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
                return false;

            license = JsonUtility.FromJson<LocalLicense>(json);
            return license != null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"License load failed: {e.Message}");
            return false;
        }
    }

    public static bool Save(LocalLicense license)
    {
        try
        {
            var json = JsonUtility.ToJson(license);
            File.WriteAllText(FilePath, json, Encoding.UTF8);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"License save failed: {e.Message}");
            return false;
        }
    }

    public static void Delete()
    {
        try
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"License delete failed: {e.Message}");
        }
    }
}