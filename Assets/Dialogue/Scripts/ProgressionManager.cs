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

        Debug.Log($"[Progression] Checking if dialogue '{requirement.dialogueID}' can play...");

        if (requirement.requiredDialogues != null)
        {
            Debug.Log($"[Progression] requiredDialogues count: {requirement.requiredDialogues.Count}");
            foreach (string req in requirement.requiredDialogues)
            {
                Debug.Log($"   Required dialogue: '{req}'   Completed? {GameManager.Instance.IsDialogueComplete(req)}");
            }
        }
        else
        {
            Debug.Log("[Progression] requiredDialogues list is NULL!");
        }

        // Check required dialogues
        if (!CheckRequiredDialogues(requirement.requiredDialogues))
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
        if (required == null)
        {
            Debug.LogWarning("[Progression] Required dialogue list is NULL!");
            return true; // Default to true to avoid blocking everything
        }

        if (required.Count == 0)
        {
            Debug.Log($"[Progression] No required dialogues listed.");
            return true;
        }

        foreach (string dialogueID in required)
        {
            if (string.IsNullOrEmpty(dialogueID))
            {
                Debug.LogWarning("[Progression] Empty dialogue ID found in requirements!");
                continue;
            }

            bool completed = GameManager.Instance != null &&
                             GameManager.Instance.IsDialogueComplete(dialogueID);

            Debug.Log($"[Progression] Requirement check: '{dialogueID}' complete? {completed}");

            if (!completed)
                return false;
        }

        return true;
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
/// SIMPLIFIED: Removed flag requirements - use only dialogue requirements and choice requirements
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