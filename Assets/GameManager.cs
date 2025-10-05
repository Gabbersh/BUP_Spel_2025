using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {  get; private set; }

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private HashSet<string> completedDialogues = new HashSet<string>();

    private Dictionary<string, object> storyFlags = new Dictionary<string, object>();

    private Dictionary<string, int> dialogueChoices = new Dictionary<string, int>();

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ==================== DIALOGUE COMPLETION TRACKING ====================

    // mark a dialogue as completed. once marked, it wont play again unless repeats are allowed
    public void MarkDialogueComplete(string dialogueID)
    {
        if (!completedDialogues.Contains(dialogueID))
        {
            completedDialogues.Add(dialogueID);

            if (showDebugLogs)
                Debug.Log($"[GameManager] Dialogue '{dialogueID}' marked as complete");

            OnProgressionChanged?.Invoke();
        }
    }

    // check if a specific dialogue has been completed
    public bool IsDialogueComplete(string dialogueID)
    {
        return completedDialogues.Contains(dialogueID);
    }

    // reset a dialogue
    public void ResetDialogue(string dialogueID)
    {
        completedDialogues.Remove(dialogueID);

        if(showDebugLogs)
            Debug.Log($"[GameManager] Dialogue '{dialogueID}' reset");
    }

    public void SetFlag(string flagName, object value)
    {
        storyFlags[flagName] = value;

        if (showDebugLogs)
            Debug.Log($"[GameManager] Flag '{flagName}' set to '{value}'");

        OnProgressionChanged?.Invoke();
    }

    public T GetFlag<T>(string flagName, T defaultValue = default)
    {
        if(storyFlags.TryGetValue(flagName, out object value))
        {
            return (T)value;
        }
        return defaultValue;
    }

    public bool CheckFlag(string flagName, object expectedValue)
    {
        if (storyFlags.TryGetValue(flagName, out object value))
        {
            return value.Equals(expectedValue);
        }
        return false;
    }

    public void RecordChoice(string dialogueID, int choiceIndex)
    {
        dialogueChoices[dialogueID] = choiceIndex;

        if (showDebugLogs)
            Debug.Log($"[GameManager] Choice {choiceIndex} recorded for dialogue '{dialogueID}'");
    }

    public int GetChoice(string dialogueID)
    {
        return dialogueChoices.TryGetValue(dialogueID, out int choice) ? choice : -1;
    }

    // ===================== PROGRESSION EVENT ======================

    public System.Action OnProgressionChanged;

    // ==================== SAVE/LOAD (Optional) ====================

    public ProgressionData GetProgressionData()
    {
        return new ProgressionData
        {
            completedDialogues = new List<string>(completedDialogues),
            storyFlags = new Dictionary<string, object>(storyFlags),
            dialogueChoices = new Dictionary<string, int>(dialogueChoices)
        };
    }

    public void LoadProgressionData(ProgressionData data)
    {
        completedDialogues = new HashSet<string>(data.completedDialogues);
        storyFlags = new Dictionary<string, object>(data.storyFlags);
        dialogueChoices = new Dictionary<string, int>(data.dialogueChoices);

        if (showDebugLogs)
            Debug.Log("[GameManager] Progression data loaded");

        OnProgressionChanged?.Invoke();
    }

    public void ResetAllProgress()
    {
        completedDialogues.Clear();
        storyFlags.Clear();
        dialogueChoices.Clear();

        if (showDebugLogs)
            Debug.Log("[GameManager] All progression reset");

        OnProgressionChanged?.Invoke();
    }
}

[System.Serializable]
public class ProgressionData
{
    public List<string> completedDialogues;
    public Dictionary<string, object> storyFlags;
    public Dictionary<string, int> dialogueChoices;
}

