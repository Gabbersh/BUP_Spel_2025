using System.Collections.Generic;
using UnityEngine;

public class NPCRelocator : MonoBehaviour
{
    [System.Serializable]
    public class RelocationRule
    {
        [Tooltip("Dialogue ID that triggers this relocation")]
        public string triggerDialogueID;

        [Tooltip("Required choice index (-1 = any choice is fine, 0 = first choice, 1 = second choice, etc.")]
        public int requiredChoiceIndex;

        [Tooltip("NPC to relocate")]
        public NPCController npcToMove;

        [Tooltip("New POI location")]
        public PointOfInterest newPOI;

        [Tooltip("Should the old POI be disabled?")]
        public bool disableOldPOI = true;
    }

    [Header("Relocation Rules")]
    [SerializeField] private List<RelocationRule> relocationRules = new List<RelocationRule>();

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

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
        // Just store the choice, don't relocate yet
        pendingChoices[dialogueID] = choiceIndex;
        DebugLog($"Choice recorded: {dialogueID}, choice {choiceIndex}. Waiting for dialogue to end...");
    }

    private void OnDialogueEnded(string dialogueID)
    {
        DebugLog($"Dialogue ended: {dialogueID}. Checking if it was completed...");

        // CRITICAL FIX: Only relocate if dialogue was actually completed, not cancelled
        if (GameManager.Instance == null || !GameManager.Instance.IsDialogueComplete(dialogueID))
        {
            DebugLog($"Dialogue '{dialogueID}' was NOT completed (cancelled or failed). Skipping relocation.");
            return;
        }

        DebugLog($"Dialogue '{dialogueID}' was completed! Checking for relocations...");

        // Get the choice that was made (if any)
        int choiceIndex = -1;
        if (pendingChoices.ContainsKey(dialogueID))
        {
            choiceIndex = pendingChoices[dialogueID];
            pendingChoices.Remove(dialogueID); // Clean up
        }

        // Now check for relocations with the completed dialogue
        CheckForRelocations(dialogueID, choiceIndex);
    }

    private void CheckForRelocations(string dialogueID, int choiceIndex = -1)
    {
        DebugLog($"Checking {relocationRules.Count} rules for {dialogueID}, choice {choiceIndex}");

        foreach (var rule in relocationRules)
        {
            if (rule.triggerDialogueID != dialogueID)
                continue;

            if (rule.requiredChoiceIndex != -1 && rule.requiredChoiceIndex != choiceIndex)
            {
                DebugLog($"Choice mismatch: required {rule.requiredChoiceIndex}, got {choiceIndex}");
                continue;
            }

            DebugLog($"Match found! Relocating {rule.npcToMove?.NPCID}...");
            RelocateNPC(rule);
        }
    }

    private void RelocateNPC(RelocationRule rule)
    {
        if (rule.npcToMove == null || rule.newPOI == null)
        {
            Debug.LogWarning("[NPCRelocator] Invalid relocation rule - missing NPC or POI");
            return;
        }

        DebugLog($"Relocating {rule.npcToMove.NPCID} to {rule.newPOI.name}");

        rule.npcToMove.RelocateToNewPOI(rule.newPOI, rule.disableOldPOI);

        DebugLog($"Relocation complete for {rule.npcToMove.NPCID}");
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