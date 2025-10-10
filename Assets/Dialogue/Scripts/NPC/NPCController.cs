using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;

/// <summary>
/// Controls individual NPC behavior, dialogues, and state.
/// Handles all dialogue progression for a single character.
/// </summary>
public class NPCController : MonoBehaviour
{
    [Header("NPC Identity")]
    [SerializeField] private string npcID = ""; // Unique identifier
    [SerializeField] private string npcName = "Character";

    [Header("Point of Interest")]
    [SerializeField] private PointOfInterest associatedPOI;
    [SerializeField] private float interactionDistance = 0.5f;

    [Header("Dialogues")]
    [SerializeField] private List<NPCDialogue> dialogues = new List<NPCDialogue>();
    [SerializeField] private bool autoStartFirstDialogue = false;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject availableIndicator; // Optional: exclamation mark, etc.

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // State
    private NPCState currentState = NPCState.Locked;
    private int currentDialogueIndex = 0;
    private bool isWaitingAtPOI = false;
    private CameraMovement cameraMovement;

    // Properties
    public string NPCID => npcID;
    public string NPCName => npcName;
    public NPCState CurrentState => currentState;
    public bool IsAvailable => currentState == NPCState.Available;
    public bool IsInDialogue => currentState == NPCState.Talking;

    private void Awake()
    {
        ValidateSetup();
        cameraMovement = Camera.main?.GetComponent<CameraMovement>();
    }

    private void Start()
    {
        UpdateState();
        UpdateVisualIndicators();

        // Register with NPCManager
        if (NPCManager.Instance != null)
        {
            NPCManager.Instance.RegisterNPC(this);
        }
    }

    private void OnDestroy()
    {
        // Unregister from NPCManager
        if (NPCManager.Instance != null)
        {
            NPCManager.Instance.UnregisterNPC(this);
        }
    }

    private void Update()
    {
        UpdateInteractionState();
        HandleAutoDialogue();
    }

    // ==================== SETUP ====================

    private void ValidateSetup()
    {
        if (string.IsNullOrEmpty(npcID))
        {
            Debug.LogWarning($"[NPCController] {gameObject.name} has no NPC ID set!");
        }

        if (associatedPOI == null)
        {
            Debug.LogWarning($"[NPCController] {npcID} has no POI assigned!");
        }
    }

    // ==================== STATE MANAGEMENT ====================

    public void UpdateState()
    {
        NPCState newState = DetermineState();

        if (newState != currentState)
        {
            DebugLog($"State changed: {currentState} -> {newState}");
            currentState = newState;
            GameEvents.TriggerNPCStateChanged(npcID, newState);

            if (newState == NPCState.Available)
                GameEvents.TriggerNPCBecameAvailable(npcID);
            else if (currentState == NPCState.Available)
                GameEvents.TriggerNPCBecameUnavailable(npcID);

            UpdateVisualIndicators();
        }
    }

    private NPCState DetermineState()
    {
        // Currently talking
        if (DialogueManager.Instance != null && DialogueManager.Instance.DialogueIsPlaying)
        {
            return NPCState.Talking;
        }

        // No dialogues configured
        if (dialogues == null || dialogues.Count == 0)
        {
            return NPCState.Locked;
        }

        // Check if all dialogues completed
        if (currentDialogueIndex >= dialogues.Count)
        {
            return NPCState.Completed;
        }

        // Check current dialogue availability
        NPCDialogue currentDialogue = dialogues[currentDialogueIndex];

        if (currentDialogue.requirement != null && ProgressionManager.Instance != null)
        {
            bool canPlay = ProgressionManager.Instance.CanPlayDialogue(currentDialogue.requirement);

            if (!canPlay)
            {
                // Check if waiting for correct choice
                if (currentDialogue.requirement.requiredChoices != null &&
                    currentDialogue.requirement.requiredChoices.Count > 0)
                {
                    return NPCState.WaitingForChoice;
                }
                return NPCState.Locked;
            }
        }

        return NPCState.Available;
    }

    // ==================== INTERACTION ====================

    private void UpdateInteractionState()
    {
        if (cameraMovement == null || associatedPOI == null) return;

        // Check if camera is at POI and close enough
        bool atPOI = cameraMovement.IsInPOI;
        bool closeEnough = Vector3.Distance(
            cameraMovement.transform.position,
            associatedPOI.cameraTarget.position
        ) < interactionDistance;

        isWaitingAtPOI = atPOI && closeEnough;
    }

    private void HandleAutoDialogue()
    {
        if (!isWaitingAtPOI || !IsAvailable) return;
        if (DialogueManager.Instance == null || DialogueManager.Instance.DialogueIsPlaying) return;

        if (currentDialogueIndex == 0 && autoStartFirstDialogue)
        {
            StartCurrentDialogue();
        }
    }

    public void TryInteract()
    {
        if (!IsAvailable)
        {
            DebugLog($"Cannot interact - State: {currentState}");
            return;
        }

        if (!isWaitingAtPOI)
        {
            DebugLog("Not at POI yet");
            return;
        }

        StartCurrentDialogue();
    }

    private void StartCurrentDialogue()
    {
        if (currentDialogueIndex >= dialogues.Count)
        {
            DebugLog("No more dialogues");
            return;
        }

        NPCDialogue dialogue = dialogues[currentDialogueIndex];

        if (dialogue.inkJSON == null)
        {
            Debug.LogError($"[NPCController] {npcID} dialogue {currentDialogueIndex} has no Ink JSON!");
            return;
        }

        DebugLog($"Starting dialogue: {dialogue.requirement?.dialogueID}");

        // Subscribe to dialogue events
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded += OnDialogueComplete;
            DialogueManager.Instance.EnterDialogueMode(dialogue.inkJSON, dialogue.requirement?.dialogueID);
        }

        GameEvents.TriggerNPCInteracted(npcID);
        UpdateState();
    }

    private void OnDialogueComplete(string dialogueID)
    {
        // Unsubscribe
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= OnDialogueComplete;
        }

        DebugLog($"Dialogue complete: {dialogueID}");

        // Check if we should move to next dialogue
        NPCDialogue currentDialogue = dialogues[currentDialogueIndex];

        if (currentDialogue.requirement != null && ProgressionManager.Instance != null)
        {
            bool progressionMet = ProgressionManager.Instance.CanPlayDialogue(currentDialogue.requirement);

            // If still can't progress, stay on this dialogue (wrong choice scenario)
            if (!progressionMet && currentDialogue.repeatUntilCorrectChoice)
            {
                DebugLog("Wrong choice - staying on this dialogue");
                UpdateState();
                return;
            }
        }

        // Move to next dialogue
        currentDialogueIndex++;
        DebugLog($"Advanced to dialogue index: {currentDialogueIndex}");

        UpdateState();
        GameEvents.TriggerProgressionChanged();
    }

    // ==================== VISUAL FEEDBACK ====================

    private void UpdateVisualIndicators()
    {
        if (availableIndicator != null)
        {
            availableIndicator.SetActive(currentState == NPCState.Available);
        }
    }

    // ==================== PUBLIC API ====================

    /// <summary>
    /// Force move to specific dialogue index (for debugging/special cases)
    /// </summary>
    public void SetDialogueIndex(int index)
    {
        if (index >= 0 && index < dialogues.Count)
        {
            currentDialogueIndex = index;
            UpdateState();
        }
    }

    /// <summary>
    /// Get current dialogue requirement info
    /// </summary>
    public DialogueRequirement GetCurrentRequirement()
    {
        if (currentDialogueIndex >= dialogues.Count) return null;
        return dialogues[currentDialogueIndex].requirement;
    }

    /// <summary>
    /// Reset this NPC to beginning
    /// </summary>
    public void ResetNPC()
    {
        currentDialogueIndex = 0;
        UpdateState();
    }

    // ==================== UTILITY ====================

    private void DebugLog(string message)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[NPC:{npcID}] {message}");
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (associatedPOI != null && associatedPOI.cameraTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(associatedPOI.cameraTarget.position, interactionDistance);

            // Draw line to POI
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, associatedPOI.transform.position);
        }
    }
#endif
}

/// <summary>
/// A single dialogue entry for an NPC
/// </summary>
[System.Serializable]
public class NPCDialogue
{
    [Tooltip("The Ink JSON file for this dialogue")]
    public TextAsset inkJSON;

    [Tooltip("Requirements for this dialogue to be available")]
    public DialogueRequirement requirement;

    [Tooltip("If true, this dialogue can be repeated until correct choice is made")]
    public bool repeatUntilCorrectChoice = false;
}