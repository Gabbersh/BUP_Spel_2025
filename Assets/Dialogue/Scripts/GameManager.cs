using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Save Settings")]
    [SerializeField] private bool autoSave = true;
    [SerializeField] private string saveFileName = "game_save";

    private GameSaveData saveData;

    public event Action OnGameDataLoaded;
    public event Action OnGameDataSaved;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeGameData();
    }

    private void OnApplicationQuit()
    {
        if (autoSave) SaveGame();
    }

    private void InitializeGameData()
    {
        LoadGame();
    }

    // ==================== SAVE/LOAD ====================

    public void SaveGame()
    {
        try
        {
            string json = JsonUtility.ToJson(saveData, true);
            PlayerPrefs.SetString(saveFileName, json);
            PlayerPrefs.Save();

            OnGameDataSaved?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameManager] Failed to save game: {e.Message}");
        }
    }

    public void LoadGame()
    {
        if (PlayerPrefs.HasKey(saveFileName))
        {
            try
            {
                string json = PlayerPrefs.GetString(saveFileName);
                saveData = JsonUtility.FromJson<GameSaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameManager] Failed to load game: {e.Message}");
                saveData = new GameSaveData();
            }
        }
        else
        {
            saveData = new GameSaveData();
        }

        OnGameDataLoaded?.Invoke();
    }

    public void ResetGame()
    {
        saveData = new GameSaveData();
        SaveGame();
    }

    // ==================== DIALOGUE TRACKING ====================

    public void MarkDialogueComplete(string dialogueID)
    {
        if (string.IsNullOrEmpty(dialogueID)) return;

        if (!saveData.completedDialogues.Contains(dialogueID))
        {
            saveData.completedDialogues.Add(dialogueID);
            if (autoSave) SaveGame();
        }
    }

    public bool IsDialogueComplete(string dialogueID)
    {
        return !string.IsNullOrEmpty(dialogueID) &&
               saveData.completedDialogues.Contains(dialogueID);
    }

    public void RecordChoice(string dialogueID, int choiceIndex)
    {
        if (string.IsNullOrEmpty(dialogueID)) return;

        string key = $"{dialogueID}_choice";
        SetFlag(key, choiceIndex);
    }

    public int GetLastChoice(string dialogueID, int defaultValue = -1)
    {
        if (string.IsNullOrEmpty(dialogueID)) return defaultValue;

        string key = $"{dialogueID}_choice";
        return GetFlag(key, defaultValue);
    }

    // ==================== FLAG SYSTEM ====================

    public void SetFlag<T>(string flagName, T value)
    {
        if (string.IsNullOrEmpty(flagName)) return;

        string valueString = value.ToString();

        if (saveData.flags.ContainsKey(flagName))
            saveData.flags[flagName] = valueString;
        else
            saveData.flags.Add(flagName, valueString);

        if (autoSave) SaveGame();
    }

    public T GetFlag<T>(string flagName, T defaultValue = default)
    {
        if (string.IsNullOrEmpty(flagName) || !saveData.flags.ContainsKey(flagName))
            return defaultValue;

        try
        {
            string valueString = saveData.flags[flagName];
            return (T)Convert.ChangeType(valueString, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    public bool CheckFlag<T>(string flagName, T expectedValue)
    {
        T actualValue = GetFlag<T>(flagName);
        return EqualityComparer<T>.Default.Equals(actualValue, expectedValue);
    }

    public bool HasFlag(string flagName)
    {
        return !string.IsNullOrEmpty(flagName) && saveData.flags.ContainsKey(flagName);
    }

    public void RemoveFlag(string flagName)
    {
        if (saveData.flags.ContainsKey(flagName))
        {
            saveData.flags.Remove(flagName);
            if (autoSave) SaveGame();
        }
    }

    // ==================== HELPER METHODS ====================

    public void DebugPrintAllFlags()
    {
        Debug.Log("=== CURRENT FLAGS ===");
        foreach (var kvp in saveData.flags)
        {
            Debug.Log($"{kvp.Key} = {kvp.Value}");
        }
        Debug.Log("=== COMPLETED DIALOGUES ===");
        foreach (var dialogue in saveData.completedDialogues)
        {
            Debug.Log($"- {dialogue}");
        }
    }
}

[Serializable]
public class GameSaveData
{
    public Dictionary<string, string> flags = new Dictionary<string, string>();
    public List<string> completedDialogues = new List<string>();
}