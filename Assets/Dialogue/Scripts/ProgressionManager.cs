using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SIMPLIFIED: Handles dialogue progression logic.
/// Easy to understand - dialogues play in sequence or based on simple conditions.
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
    /// Check if a dialogue can be played based on its requirements
    /// </summary>
    public bool CanPlayDialogue(DialogueRequirement requirement)
    {
        if (requirement == null || GameManager.Instance == null)
        {
            return true;
        }

        // Check if already completed and shouldn't repeat
        if (requirement.oneTimeOnly &&
            !string.IsNullOrEmpty(requirement.dialogueID) &&
            GameManager.Instance.IsDialogueComplete(requirement.dialogueID))
        {
            return false;
        }

        // Check required flag (if any)
        if (requirement.requiresFlag != null && !string.IsNullOrEmpty(requirement.requiresFlag.flagName))
        {
            if (!CheckFlag(requirement.requiresFlag))
            {
                return false;
            }
        }

        return true;
    }

    // ==================== FLAG CHECKING ====================

    private bool CheckFlag(FlagRequirement flagReq)
    {
        if (GameManager.Instance == null || string.IsNullOrEmpty(flagReq.flagName))
        {
            return false;
        }

        // Check if flag exists
        if (!GameManager.Instance.HasFlag(flagReq.flagName))
        {
            return false;
        }

        // Check value based on type
        if (flagReq.checkType == FlagCheckType.Boolean)
        {
            bool flagValue = GameManager.Instance.GetFlag(flagReq.flagName, false);
            return flagValue == flagReq.expectedBoolValue;
        }
        else if (flagReq.checkType == FlagCheckType.Integer)
        {
            int flagValue = GameManager.Instance.GetFlag(flagReq.flagName, 0);
            return flagValue == flagReq.expectedIntValue;
        }

        return false;
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

        if (requirement.requiresFlag != null && !string.IsNullOrEmpty(requirement.requiresFlag.flagName))
        {
            if (!GameManager.Instance.HasFlag(requirement.requiresFlag.flagName))
            {
                return $"Requires flag: {requirement.requiresFlag.flagName}";
            }
        }

        return "Available";
    }
}

// ==================== DATA STRUCTURES ====================

/// <summary>
/// SIMPLIFIED: Easy-to-understand requirements for dialogues.
/// Most dialogues just need a dialogueID!
/// </summary>
[Serializable]
public class DialogueRequirement
{
    [Header("Basic Info")]
    [Tooltip("Unique ID for this dialogue (e.g. 'alex_stadium_first')")]
    public string dialogueID;

    [Tooltip("Can this dialogue be played multiple times?")]
    public bool oneTimeOnly = true;

    [Header("Quest Integration (Optional)")]
    [Tooltip("Requires a quest flag to be set (e.g. item collected)")]
    public FlagRequirement requiresFlag;
}

/// <summary>
/// Requires a game flag to be set (for quest integration)
/// </summary>
[Serializable]
public class FlagRequirement
{
    [Tooltip("The flag name to check (e.g. 'quest_sword_found')")]
    public string flagName;

    [Tooltip("Type of check")]
    public FlagCheckType checkType = FlagCheckType.Boolean;

    [Tooltip("Expected value (for boolean flags)")]
    public bool expectedBoolValue = true;

    [Tooltip("Expected value (for integer flags)")]
    public int expectedIntValue = 1;
}

/// <summary>
/// Type of flag check
/// </summary>
public enum FlagCheckType
{
    Boolean,  // Check if flag is true/false
    Integer   // Check if flag equals a number
}