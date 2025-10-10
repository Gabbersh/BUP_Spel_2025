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
        if (requirement == null || GameManager.Instance == null)
            return true;

        // Check if already completed and shouldn't repeat
        if (requirement.oneTimeOnly &&
            !string.IsNullOrEmpty(requirement.dialogueID) &&
            GameManager.Instance.IsDialogueComplete(requirement.dialogueID))
        {
            return false;
        }

        // Check required dialogues
        if (!CheckRequiredDialogues(requirement.requiredDialogues))
            return false;

        // Check required flags
        if (!CheckRequiredFlags(requirement.requiredFlags))
            return false;

        // Check required choices
        if (!CheckRequiredChoices(requirement.requiredChoices))
            return false;

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
        if (required == null || required.Count == 0) return true;
        if (GameManager.Instance == null) return false;

        foreach (string dialogueID in required)
        {
            if (!GameManager.Instance.IsDialogueComplete(dialogueID))
            {
                return false;
            }
        }
        return true;
    }

    private bool CheckRequiredFlags(List<FlagRequirement> required)
    {
        if (required == null || required.Count == 0) return true;
        if (GameManager.Instance == null) return false;

        foreach (var flagReq in required)
        {
            if (!CheckSingleFlag(flagReq))
                return false;
        }
        return true;
    }

    private bool CheckSingleFlag(FlagRequirement requirement)
    {
        switch (requirement.type)
        {
            case FlagType.Bool:
                bool boolValue = bool.Parse(requirement.expectedValue);
                return GameManager.Instance.CheckFlag(requirement.flagName, boolValue);

            case FlagType.Int:
                int intValue = int.Parse(requirement.expectedValue);
                return GameManager.Instance.CheckFlag(requirement.flagName, intValue);

            case FlagType.String:
                return GameManager.Instance.CheckFlag(requirement.flagName, requirement.expectedValue);

            case FlagType.Exists:
                return GameManager.Instance.HasFlag(requirement.flagName);

            default:
                return true;
        }
    }

    private bool CheckRequiredChoices(List<ChoiceRequirement> required)
    {
        if (required == null || required.Count == 0) return true;
        if (GameManager.Instance == null) return false;

        foreach (var choiceReq in required)
        {
            int lastChoice = GameManager.Instance.GetLastChoice(choiceReq.dialogueID);

            if (!choiceReq.acceptableChoices.Contains(lastChoice))
                return false;
        }
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

        foreach (var flagReq in requirement.requiredFlags)
        {
            if (!CheckSingleFlag(flagReq))
            {
                return $"Requires flag: {flagReq.flagName}";
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

    [Tooltip("Flags that must be met")]
    public List<FlagRequirement> requiredFlags = new List<FlagRequirement>();

    [Tooltip("Specific choices that must have been made")]
    public List<ChoiceRequirement> requiredChoices = new List<ChoiceRequirement>();
}

/// <summary>
/// A single flag requirement
/// </summary>
[Serializable]
public class FlagRequirement
{
    public string flagName;
    public FlagType type = FlagType.Bool;
    public string expectedValue = "true";
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
/// Types of flags for easy validation
/// </summary>
public enum FlagType
{
    Bool,
    Int,
    String,
    Exists  // Just check if flag exists, ignore value
}