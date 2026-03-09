using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Universal quest manager that handles all quests in the game.
/// Each quest can be triggered by dialogue choices and track objectives.
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("Quests")]
    [SerializeField] private List<Quest> quests = new List<Quest>();

    [Serializable]
    public class ScriptedFadeEvent
    {
        [Header("Dialogue Requirements (AND between groups, OR within group)")]
        public List<Quest.DialogueRequirementGroup> requirementGroups = new List<Quest.DialogueRequirementGroup>();

        [HideInInspector]
        public bool hasTriggered = false;
    }

    [Header("Scripted Events")]
    [SerializeField] private List<ScriptedFadeEvent> fadeEvents = new List<ScriptedFadeEvent>();

    [Header("Outro Event")]
    [SerializeField] private List<string> triggerOutroAfterDialogues = new List<string>();

    [SerializeField] private CameraMovement cameraMovement;

    private bool outroTriggered = false;

    [Header("External References")]
    [SerializeField] private QuestHintManager questHintManager;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitializeQuests();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    // ==================== INITIALIZATION ====================

    private void InitializeQuests()
    {
        foreach (var quest in quests)
        {
            // Subscribe to interactable pickups
            foreach (var objective in quest.objectives)
            {
                if (objective.type == QuestObjectiveType.PickupItem && objective.targetObject != null)
                {
                    var interactable = objective.targetObject.GetComponent<Interactable>();
                    if (interactable != null)
                    {
                        interactable.OnPickedUp += (item) => OnObjectiveItemPickedUp(quest, objective);
                    }
                }
            }
        }
    }

    private void SubscribeToEvents()
    {
        GameEvents.OnChoiceMade += OnChoiceMade;
        GameEvents.OnDialogueEnded += OnDialogueEnded;
    }

    private void UnsubscribeFromEvents()
    {
        GameEvents.OnChoiceMade -= OnChoiceMade;
        GameEvents.OnDialogueEnded -= OnDialogueEnded;
    }

    // ==================== EVENT HANDLERS ====================

    private bool AreDialogueRequirementsMet(Quest quest, string currentDialogueID)
    {
        if (quest.requirementGroups == null || quest.requirementGroups.Count == 0)
            return false;

        foreach (var group in quest.requirementGroups)
        {
            bool groupSatisfied = false;

            foreach (var dialogueID in group.anyOfDialogueIDs)
            {
                // Allow the current dialogue to count immediately for OnChoice
                if (dialogueID == currentDialogueID)
                {
                    groupSatisfied = true;
                    break;
                }

                if (GameManager.Instance.IsDialogueComplete(dialogueID))
                {
                    groupSatisfied = true;
                    break;
                }
            }

            if (!groupSatisfied)
                return false;
        }

        return true;
    }

    private void OnChoiceMade(string dialogueID, int choiceIndex)
    {
        DebugLog($"Choice made: dialogue='{dialogueID}', choice={choiceIndex}");

        foreach (var quest in quests)
        {
            if (quest.triggerType != QuestTriggerType.OnChoice)
                continue;

            if (IsQuestActive(quest.questID) || IsQuestComplete(quest.questID))
                continue;

            // If specific choice required
            if (quest.triggerChoiceIndex != -1 &&
                quest.triggerChoiceIndex != choiceIndex)
                continue;

            // Check AND/OR dialogue requirements
            if (AreDialogueRequirementsMet(quest, dialogueID))
            {
                StartQuest(quest);
            }
        }
    }

    private void OnDialogueEnded(string dialogueID)
    {
        foreach (var quest in quests)
        {
            if (quest.triggerType != QuestTriggerType.OnDialogueComplete)
                continue;

            if (IsQuestActive(quest.questID) || IsQuestComplete(quest.questID))
                continue;

            if (AreDialogueRequirementsMet(quest, dialogueID))
            {
                StartQuest(quest);
            }
        }

        foreach (var fadeEvent in fadeEvents)
        {
            if (fadeEvent.hasTriggered)
                continue;

            if (AreDialogueRequirementsMet(new Quest { requirementGroups = fadeEvent.requirementGroups }, dialogueID))
            {
                if (GameManager.Instance.IsDialogueComplete(dialogueID))
                {
                    fadeEvent.hasTriggered = true;
                    ScreenFade.Instance.FadeOutThenIn();
                }
            }
        }

        // OUTRO EVENT
        if (!outroTriggered &&
            triggerOutroAfterDialogues.Contains(dialogueID) &&
            GameManager.Instance.IsDialogueComplete(dialogueID))
        {
            outroTriggered = true;

            if (cameraMovement != null)
                cameraMovement.StartOutro();
        }
    }

    private void OnObjectiveItemPickedUp(Quest quest, QuestObjective objective)
    {
        if (!IsQuestActive(quest.questID)) return;

        DebugLog($"Quest '{quest.questID}': Objective '{objective.description}' completed!");

        // Mark objective as complete
        objective.isCompleted = true;

        // Set objective flag
        if (!string.IsNullOrEmpty(objective.completionFlag))
        {
            GameManager.Instance?.SetFlag(objective.completionFlag, true);
        }

        // Check if all objectives are complete
        if (AreAllObjectivesComplete(quest))
        {
            CompleteQuest(quest);
        }
    }

    // ==================== QUEST CONTROL ====================

    private void StartQuest(Quest quest)
    {
        if (string.IsNullOrEmpty(quest.questID))
        {
            Debug.LogError("[QuestManager] Quest has no ID!");
            return;
        }

        DebugLog($"Starting quest: {quest.questID}");

        // Set quest as active
        GameManager.Instance?.SetFlag($"quest_{quest.questID}_active", true);

        // If quest has no objectives, complete immediately
        if (quest.objectives == null || quest.objectives.Count == 0)
        {
            CompleteQuest(quest);
            return;
        }

        // Activate all objective objects
        foreach (var objective in quest.objectives)
        {
            if (objective.targetObject != null)
            {
                objective.targetObject.SetActive(true);
                DebugLog($"Activated objective: {objective.targetObject.name}");
            }
        }

        // Set start flag if specified
        if (!string.IsNullOrEmpty(quest.startFlag))
        {
            GameManager.Instance?.SetFlag(quest.startFlag, true);
        }

        // Activate quest mode in QuestHintManager
        if (questHintManager != null)
        {
            questHintManager.ActivateQuestMode();
            DebugLog($"Activated quest hints for '{quest.questID}'");
        }

        DebugLog($"Quest '{quest.questID}' started!");
    }

    private void CompleteQuest(Quest quest)
    {
        DebugLog($"Quest '{quest.questID}' COMPLETED!");

        // Mark quest as complete
        GameManager.Instance?.SetFlag($"quest_{quest.questID}_active", false);
        GameManager.Instance?.SetFlag($"quest_{quest.questID}_complete", true);

        // Set completion flag if specified
        if (!string.IsNullOrEmpty(quest.completionFlag))
        {
            GameManager.Instance?.SetFlag(quest.completionFlag, true);
            ProgressionManager.Instance?.NotifyProgressionChanged();
        }

        // Deactivate objective objects if needed
        foreach (var objective in quest.objectives)
        {
            if (objective.deactivateOnComplete && objective.targetObject != null)
            {
                objective.targetObject.SetActive(false);
            }
        }
    }

    // ==================== QUERY METHODS ====================

    /// <summary>
    /// Check if a quest is currently active
    /// </summary>
    public bool IsQuestActive(string questID)
    {
        if (GameManager.Instance == null) return false;
        return GameManager.Instance.GetFlag($"quest_{questID}_active", false);
    }

    /// <summary>
    /// Check if a quest is complete
    /// </summary>
    public bool IsQuestComplete(string questID)
    {
        if (GameManager.Instance == null) return false;
        return GameManager.Instance.GetFlag($"quest_{questID}_complete", false);
    }

    /// <summary>
    /// Check if all objectives of a quest are complete
    /// </summary>
    private bool AreAllObjectivesComplete(Quest quest)
    {
        foreach (var objective in quest.objectives)
        {
            if (!objective.isCompleted) return false;
        }
        return true;
    }

    /// <summary>
    /// Get a quest by ID
    /// </summary>
    public Quest GetQuest(string questID)
    {
        return quests.Find(q => q.questID == questID);
    }

    // ==================== MANUAL CONTROL ====================

    /// <summary>
    /// Manually start a quest (useful for testing)
    /// </summary>
    public void ManualStartQuest(string questID)
    {
        Quest quest = GetQuest(questID);
        if (quest != null)
        {
            StartQuest(quest);
        }
        else
        {
            Debug.LogWarning($"[QuestManager] Quest '{questID}' not found!");
        }
    }

    /// <summary>
    /// Manually complete a quest (useful for testing)
    /// </summary>
    public void ManualCompleteQuest(string questID)
    {
        Quest quest = GetQuest(questID);
        if (quest != null)
        {
            foreach (var objective in quest.objectives)
            {
                objective.isCompleted = true;
            }
            CompleteQuest(quest);
        }
    }

    /// <summary>
    /// Reset a quest (for testing)
    /// </summary>
    public void ResetQuest(string questID)
    {
        Quest quest = GetQuest(questID);
        if (quest != null)
        {
            GameManager.Instance?.RemoveFlag($"quest_{questID}_active");
            GameManager.Instance?.RemoveFlag($"quest_{questID}_complete");

            if (!string.IsNullOrEmpty(quest.startFlag))
            {
                GameManager.Instance?.RemoveFlag(quest.startFlag);
            }

            if (!string.IsNullOrEmpty(quest.completionFlag))
            {
                GameManager.Instance?.RemoveFlag(quest.completionFlag);
            }

            foreach (var objective in quest.objectives)
            {
                objective.isCompleted = false;
                if (objective.targetObject != null)
                {
                    objective.targetObject.SetActive(false);
                }

                if (!string.IsNullOrEmpty(objective.completionFlag))
                {
                    GameManager.Instance?.RemoveFlag(objective.completionFlag);
                }
            }

            DebugLog($"Quest '{questID}' reset");
        }
    }

    // ==================== DEBUG ====================

    private void DebugLog(string message)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[QuestManager] {message}");
        }
    }

    /// <summary>
    /// Print all quest statuses
    /// </summary>
    [ContextMenu("Debug: Print All Quests")]
    public void DebugPrintAllQuests()
    {
        Debug.Log("=== ALL QUESTS ===");
        foreach (var quest in quests)
        {
            bool active = IsQuestActive(quest.questID);
            bool complete = IsQuestComplete(quest.questID);
            Debug.Log($"{quest.questID}: Active={active}, Complete={complete}");

            foreach (var objective in quest.objectives)
            {
                Debug.Log($"  - {objective.description}: {(objective.isCompleted ? "DONE" : "PENDING")}");
            }
        }
    }
}

// ==================== DATA STRUCTURES ====================

/// <summary>
/// A single quest with objectives
/// </summary>
[Serializable]
public class Quest
{
    [Header("Quest Identity")]
    [Tooltip("Unique ID for this quest")]
    public string questID = "";

    [Tooltip("Display name for the quest (optional)")]
    public string questName = "";

    [Tooltip("Description of the quest")]
    [TextArea(2, 4)]
    public string description = "";

    [Header("Trigger Settings")]
    [Tooltip("How this quest starts")]
    public QuestTriggerType triggerType = QuestTriggerType.OnChoice;

    [Serializable]
    public class DialogueRequirementGroup
    {
        [Tooltip("At least ONE of these dialogue IDs must be completed")]
        public List<string> anyOfDialogueIDs = new List<string>();
    }

    [Header("Dialogue Requirements (AND between groups, OR within group)")]
    public List<DialogueRequirementGroup> requirementGroups = new List<DialogueRequirementGroup>();

    [Tooltip("Choice index that triggers quest (for OnChoice trigger)")]
    public int triggerChoiceIndex = 0;

    [Header("Objectives")]
    [Tooltip("List of objectives to complete")]
    public List<QuestObjective> objectives = new List<QuestObjective>();

    [Header("Flags (Optional)")]
    [Tooltip("Flag to set when quest starts (optional)")]
    public string startFlag = "";

    [Tooltip("Flag to set when quest is complete (optional)")]
    public string completionFlag = "";
}



/// <summary>
/// A single objective within a quest
/// </summary>
[Serializable]
public class QuestObjective
{
    [Tooltip("Type of objective")]
    public QuestObjectiveType type = QuestObjectiveType.PickupItem;

    [Tooltip("Description of this objective")]
    public string description = "";

    [Tooltip("The GameObject involved (item to pickup, NPC to talk to, etc.)")]
    public GameObject targetObject;

    [Tooltip("Deactivate the target object when objective is complete?")]
    public bool deactivateOnComplete = false;

    [Tooltip("Flag to set when this objective is complete (optional)")]
    public string completionFlag = "";

    [HideInInspector]
    public bool isCompleted = false;
}

/// <summary>
/// How a quest is triggered
/// </summary>
public enum QuestTriggerType
{
    OnChoice,           // Triggered when a specific choice is made
    OnDialogueComplete, // Triggered when a dialogue ends
    Manual              // Must be triggered manually via code
}

/// <summary>
/// Types of quest objectives
/// </summary>
public enum QuestObjectiveType
{
    PickupItem,    // Pick up a specific item
    TalkToNPC,     // Talk to a specific NPC (future use)
    VisitLocation, // Visit a location (future use)
    Custom         // Custom objective type (future use)
}