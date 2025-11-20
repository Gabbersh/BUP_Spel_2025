using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles NPC relocation based on dialogue choices.
/// Moves NPCs to new locations when specific dialogues complete.
/// </summary>
public class NPCRelocator : MonoBehaviour
{
    [System.Serializable]
    public class RelocationRule
    {
        [Tooltip("Dialogue ID that triggers this relocation")]
        public string triggerDialogueID;

        [Tooltip("Required choice index (-1 = any choice, 0 = first choice, 1 = second choice, etc.)")]
        public int requiredChoiceIndex = -1;

        [Tooltip("NPC to relocate")]
        public NPCController npcToMove;

        [Tooltip("New POI location")]
        public PointOfInterest newPOI;

        [Tooltip("Disable the old POI?")]
        public bool disableOldPOI = true;
    }

    [Header("Relocation Rules")]
    [SerializeField] private List<RelocationRule> relocationRules = new List<RelocationRule>();

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // Store choices made during current dialogue
    private Dictionary<string, int> pendingChoices = new Dictionary<string, int>();

    private void Start()
    {
        GameEvents.OnDialogueEnded += OnDialogueEnded;
        GameEvents.OnChoiceMade += OnChoiceMade;
    }

    private void OnDestroy()
    {
        GameEvents.OnDialogueEnded -= OnDialogueEnded;
        GameEvents.OnChoiceMade -= OnChoiceMade;
    }

    private void OnChoiceMade(string dialogueID, int choiceIndex)
    {
        // Store the choice for when dialogue ends
        pendingChoices[dialogueID] = choiceIndex;
        DebugLog($"Choice recorded: {dialogueID}, choice {choiceIndex}");
    }

    private void OnDialogueEnded(string dialogueID)
    {
        DebugLog($"Dialogue ended: {dialogueID}. Checking completion...");

        // Only relocate if dialogue was completed (has #success tag)
        if (GameManager.Instance == null || !GameManager.Instance.IsDialogueComplete(dialogueID))
        {
            DebugLog($"Dialogue '{dialogueID}' NOT completed. Skipping relocation.");
            return;
        }

        DebugLog($"Dialogue '{dialogueID}' completed! Checking relocations...");

        // Get the choice that was made (if any)
        int choiceIndex = -1;
        if (pendingChoices.ContainsKey(dialogueID))
        {
            choiceIndex = pendingChoices[dialogueID];
            pendingChoices.Remove(dialogueID);
        }

        CheckForRelocations(dialogueID, choiceIndex);
    }

    private void CheckForRelocations(string dialogueID, int choiceIndex = -1)
    {
        DebugLog($"Checking {relocationRules.Count} rules for '{dialogueID}', choice {choiceIndex}");

        foreach (var rule in relocationRules)
        {
            // Must match dialogue ID
            if (rule.triggerDialogueID != dialogueID)
                continue;

            // Check if required choice was made (if specified)
            if (rule.requiredChoiceIndex != -1 && rule.requiredChoiceIndex != choiceIndex)
            {
                DebugLog($"Choice mismatch: required {rule.requiredChoiceIndex}, got {choiceIndex}");
                continue;
            }

            DebugLog($"Match found! Relocating {rule.npcToMove?.NPCID}");
            RelocateNPC(rule);
        }
    }

    private void RelocateNPC(RelocationRule rule)
    {
        if (rule.npcToMove == null || rule.newPOI == null)
        {
            Debug.LogWarning("[NPCRelocator] Invalid rule - missing NPC or POI");
            return;
        }

        DebugLog($"Relocating {rule.npcToMove.NPCID} to {rule.newPOI.name}");

        rule.npcToMove.RelocateToNewPOI(rule.newPOI, rule.disableOldPOI);

        DebugLog($"Relocation complete: {rule.npcToMove.NPCID}");
    }

    private void DebugLog(string message)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[NPCRelocator] {message}");
        }
    }

    /// <summary>
    /// Manually trigger relocation by rule index (for testing)
    /// </summary>
    public void TriggerRelocation(int ruleIndex)
    {
        if (ruleIndex >= 0 && ruleIndex < relocationRules.Count)
        {
            RelocateNPC(relocationRules[ruleIndex]);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Test First Relocation")]
    private void TestFirstRelocation()
    {
        if (relocationRules.Count > 0)
            TriggerRelocation(0);
    }
#endif
}