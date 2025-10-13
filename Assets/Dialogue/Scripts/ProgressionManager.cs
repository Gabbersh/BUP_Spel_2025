using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles all progression logic, requirements, and validation.
/// Central place for checking if dialogues should be available.
/// </summary>
public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    public event Action OnProgressionUpdated;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // ==================== DIALOGUE AVAILABILITY ====================

    /// <summary>
    /// Check if a dialogue meets all its requirements
    /// </summary>
    public bool CanPlayDialogue(DialogueRequirement requirement)
    {
        // CRITICAL DEBUG LOG - Shows when this is called
        Debug.Log($"[ProgressionManager] ===== CanPlayDialogue called for: '{requirement?.dialogueID}' =====");

        if (requirement == null || GameManager.Instance == null)
        {
            Debug.Log("[ProgressionManager] requirement or GameManager is null - returning TRUE");
            return true;
        }

        // Check if already completed and shouldn't repeat
        if (requirement.oneTimeOnly &&
            !string.IsNullOrEmpty(requirement.dialogueID) &&
            GameManager.Instance.IsDialogueComplete(requirement.dialogueID))
        {
            Debug.Log("[ProgressionManager] Dialogue already complete and one-time-only - returning FALSE");
            return false;
        }

        // Check required dialogues
        Debug.Log("[ProgressionManager] Checking required dialogues...");
        if (!CheckRequiredDialogues(requirement.requiredDialogues))
        {
            Debug.Log("[ProgressionManager] Required dialogues check FAILED");
            return false;
        }

        // Check required choices
        Debug.Log("[ProgressionManager] Checking required choices...");
        if (!CheckRequiredChoices(requirement.requiredChoices))
        {
            Debug.Log("[ProgressionManager] Required choices check FAILED");
            return false;
        }

        // Check required flags
        Debug.Log("[ProgressionManager] Checking required flags...");
        if (!CheckRequiredFlags(requirement.requiredFlags))
        {
            Debug.Log("[ProgressionManager] Required flags check FAILED");
            return false;
        }

        Debug.Log("[ProgressionManager] ===== All checks PASSED - returning TRUE =====");
        return true;
    }

    /// <summary>
    /// Check if specific choice was made in a dialogue
    /// </summary>
    public bool WasChoiceMade(string dialogueID, int choiceIndex)
    {
        if (GameManager.Instance == null) return false;
        return GameManager.Instance.GetLastChoice(dialogueID) == choiceIndex;
    }

    /// <summary>
    /// Check if any of the specified choices were made
    /// </summary>
    public bool WasAnyChoiceMade(string dialogueID, int[] acceptableChoices)
    {
        if (GameManager.Instance == null) return false;

        int lastChoice = GameManager.Instance.GetLastChoice(dialogueID);
        foreach (int choice in acceptableChoices)
        {
            if (lastChoice == choice) return true;
        }
        return false;
    }

    // ==================== REQUIREMENT CHECKS ====================

    private bool CheckRequiredDialogues(List<string> required)
    {
        if (required == null || required.Count == 0)
        {
            Debug.Log("[ProgressionManager] No required dialogues - returning TRUE");
            return true;
        }

        if (GameManager.Instance == null) return false;

        Debug.Log($"[ProgressionManager] Checking {required.Count} required dialogues");

        foreach (string dialogueID in required)
        {
            bool isComplete = GameManager.Instance.IsDialogueComplete(dialogueID);
            Debug.Log($"[ProgressionManager] Dialogue '{dialogueID}' complete? {isComplete}");

            if (!isComplete)
            {
                Debug.Log($"[ProgressionManager] ✗ Required dialogue '{dialogueID}' NOT complete");
                return false;
            }
        }

        Debug.Log("[ProgressionManager] ✓ All required dialogues complete");
        return true;
    }

    private bool CheckRequiredChoices(List<ChoiceRequirement> required)
    {
        if (required == null || required.Count == 0)
        {
            Debug.Log("[ProgressionManager] No required choices - returning TRUE");
            return true;
        }

        if (GameManager.Instance == null) return false;

        Debug.Log($"[ProgressionManager] Checking {required.Count} required choices");

        foreach (var choiceReq in required)
        {
            int lastChoice = GameManager.Instance.GetLastChoice(choiceReq.dialogueID);
            Debug.Log($"[ProgressionManager] Dialogue '{choiceReq.dialogueID}': last choice = {lastChoice}, acceptable = [{string.Join(", ", choiceReq.acceptableChoices)}]");

            if (!choiceReq.acceptableChoices.Contains(lastChoice))
            {
                Debug.Log($"[ProgressionManager] ✗ Choice requirement NOT met");
                return false;
            }
        }

        Debug.Log("[ProgressionManager] ✓ All choice requirements met");
        return true;
    }

    private bool CheckRequiredFlags(List<FlagRequirement> required)
    {
        if (required == null || required.Count == 0)
        {
            Debug.Log("[ProgressionManager] No required flags - returning TRUE");
            return true;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("[ProgressionManager] GameManager.Instance is NULL!");
            return false;
        }

        Debug.Log($"[ProgressionManager] Checking {required.Count} required flags");

        foreach (var flagReq in required)
        {
            if (string.IsNullOrEmpty(flagReq.flagName))
            {
                Debug.LogWarning("[ProgressionManager] Found empty flag name!");
                continue;
            }

            Debug.Log($"[ProgressionManager] Checking flag: '{flagReq.flagName}'");

            // Check if flag exists
            if (!GameManager.Instance.HasFlag(flagReq.flagName))
            {
                Debug.Log($"[ProgressionManager] ✗ Flag '{flagReq.flagName}' does NOT exist - requirement NOT met");
                return false;
            }

            // Check boolean flags
            if (flagReq.checkType == FlagCheckType.Boolean)
            {
                bool flagValue = GameManager.Instance.GetFlag(flagReq.flagName, false);
                Debug.Log($"[ProgressionManager] Flag '{flagReq.flagName}' = {flagValue}, expected {flagReq.expectedBoolValue}");

                if (flagValue != flagReq.expectedBoolValue)
                {
                    Debug.Log($"[ProgressionManager] ✗ Flag check FAILED - requirement NOT met");
                    return false;
                }
            }
            // Check integer flags
            else if (flagReq.checkType == FlagCheckType.Integer)
            {
                int flagValue = GameManager.Instance.GetFlag(flagReq.flagName, 0);
                Debug.Log($"[ProgressionManager] Flag '{flagReq.flagName}' = {flagValue}, expected {flagReq.expectedIntValue}");

                if (flagValue != flagReq.expectedIntValue)
                {
                    Debug.Log($"[ProgressionManager] ✗ Flag check FAILED - requirement NOT met");
                    return false;
                }
            }
        }

        Debug.Log("[ProgressionManager] ✓ All flag requirements met!");
        return true;
    }

    // ==================== PROGRESSION UPDATES ====================

    public void NotifyProgressionChanged()
    {
        OnProgressionUpdated?.Invoke();
    }

    // ==================== HELPER METHODS ====================

    /// <summary>
    /// Get a readable string describing why a dialogue can't be played
    /// </summary>
    public string GetBlockReason(DialogueRequirement requirement)
    {
        if (requirement == null) return "No requirements";
        if (GameManager.Instance == null) return "GameManager missing";

        if (requirement.oneTimeOnly &&
            GameManager.Instance.IsDialogueComplete(requirement.dialogueID))
        {
            return "Already completed (one-time only)";
        }

        foreach (string dialogueID in requirement.requiredDialogues)
        {
            if (!GameManager.Instance.IsDialogueComplete(dialogueID))
            {
                return $"Requires dialogue: {dialogueID}";
            }
        }

        foreach (var choiceReq in requirement.requiredChoices)
        {
            int lastChoice = GameManager.Instance.GetLastChoice(choiceReq.dialogueID);
            if (!choiceReq.acceptableChoices.Contains(lastChoice))
            {
                return $"Requires specific choice in: {choiceReq.dialogueID}";
            }
        }

        foreach (var flagReq in requirement.requiredFlags)
        {
            if (!GameManager.Instance.HasFlag(flagReq.flagName))
            {
                return $"Requires flag: {flagReq.flagName}";
            }
        }

        return "Available";
    }
}

// ==================== DATA STRUCTURES ====================

/// <summary>
/// Defines all requirements for a dialogue to be playable
/// </summary>
[Serializable]
public class DialogueRequirement
{
    [Tooltip("Unique ID for this dialogue")]
    public string dialogueID;

    [Tooltip("Can only be played once")]
    public bool oneTimeOnly = true;

    [Tooltip("Dialogues that must be complete first")]
    public List<string> requiredDialogues = new List<string>();

    [Tooltip("Specific choices that must have been made")]
    public List<ChoiceRequirement> requiredChoices = new List<ChoiceRequirement>();

    [Tooltip("Flags that must be set")]
    public List<FlagRequirement> requiredFlags = new List<FlagRequirement>();
}

/// <summary>
/// A choice requirement - checks if correct choice was made
/// </summary>
[Serializable]
public class ChoiceRequirement
{
    [Tooltip("The dialogue ID to check")]
    public string dialogueID;

    [Tooltip("Acceptable choice indices (0-based)")]
    public List<int> acceptableChoices = new List<int>();
}

/// <summary>
/// A flag requirement - checks if a flag has a specific value
/// </summary>
[Serializable]
public class FlagRequirement
{
    [Tooltip("The flag name to check")]
    public string flagName;

    [Tooltip("Type of check to perform")]
    public FlagCheckType checkType = FlagCheckType.Boolean;

    [Tooltip("Expected boolean value (if checking boolean)")]
    public bool expectedBoolValue = true;

    [Tooltip("Expected integer value (if checking integer)")]
    public int expectedIntValue = 1;
}

/// <summary>
/// Type of flag check
/// </summary>
public enum FlagCheckType
{
    Boolean,  // Check if flag is true/false
    Integer   // Check if flag equals a specific number
}