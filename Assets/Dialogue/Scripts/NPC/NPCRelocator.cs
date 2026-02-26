using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles NPC relocation based on dialogue choices.
/// Moves NPCs to new locations when specific dialogues complete.
/// </summary>
public class NPCRelocator : MonoBehaviour
{
    [System.Serializable]
    public class DialogueRequirementGroup
    {
        [Tooltip("At least ONE of these dialogue IDs must be completed")]
        public List<string> anyOfDialogueIDs = new List<string>();
    }

    [System.Serializable]
    public class RelocationRule
    {
        [Tooltip("All groups must be satisfied (AND between groups)")]
        public List<DialogueRequirementGroup> requirementGroups = new List<DialogueRequirementGroup>();

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
    private HashSet<RelocationRule> executedRules = new HashSet<RelocationRule>();

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
        DebugLog($"Checking {relocationRules.Count} rules after '{dialogueID}'");

        foreach (var rule in relocationRules)
        {
            if (!AreRequirementsMet(rule))
                continue;

            // Optional: check choice requirement (only applies to the dialogue that just ended)
            if (rule.requiredChoiceIndex != -1 && rule.requiredChoiceIndex != choiceIndex)
                continue;

            if (executedRules.Contains(rule))
                continue;

            DebugLog($"All conditions met! Relocating {rule.npcToMove?.NPCID}");
            RelocateNPC(rule);
            executedRules.Add(rule);
        }
    }

    private bool AreRequirementsMet(RelocationRule rule)
    {
        foreach (var group in rule.requirementGroups)
        {
            bool groupSatisfied = false;

            foreach (var dialogueID in group.anyOfDialogueIDs)
            {
                if (GameManager.Instance.IsDialogueComplete(dialogueID))
                {
                    groupSatisfied = true;
                    break;
                }
            }

            // If one AND-group fails - entire rule fails
            if (!groupSatisfied)
                return false;
        }

        return true;
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