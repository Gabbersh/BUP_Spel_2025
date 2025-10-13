using System.Collections.Generic;
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

    [Header("Visual Feedback")]
    [SerializeField] private GameObject availableIndicator; // Optional: exclamation mark, etc.

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // State
    private NPCState currentState = NPCState.Locked;
    private int currentDialogueIndex = 0;
    private bool isWaitingAtPOI = false;
    private CameraMovement cameraMovement;
    private float lastDialogueEndTime = -999f;
    private float dialogueCooldown = 0.5f; // Prevent immediate re-trigger after dialogue ends
    private bool hasLeftPOISinceLastDialogue = true; // Require player to leave and return for repeat dialogues

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

        bool wasWaitingAtPOI = isWaitingAtPOI;
        isWaitingAtPOI = atPOI && closeEnough;

        // Track when player leaves POI - allows dialogue to restart on return
        if (wasWaitingAtPOI && !isWaitingAtPOI)
        {
            hasLeftPOISinceLastDialogue = true;
            Debug.Log($"[NPC:{npcID}] Player left POI - dialogue can restart on return");
        }
    }

    private void HandleAutoDialogue()
    {
        if (!isWaitingAtPOI || !IsAvailable) return;
        if (DialogueManager.Instance == null || DialogueManager.Instance.DialogueIsPlaying) return;

        // COOLDOWN: Don't auto-start dialogue immediately after one just ended
        if (Time.time - lastDialogueEndTime < dialogueCooldown)
        {
            Debug.Log($"[NPC:{npcID}] Cooldown active - waiting {dialogueCooldown - (Time.time - lastDialogueEndTime):F2}s before auto-start");
            return;
        }

        // IMPORTANT: Require player to leave and return to POI before auto-starting again
        // This prevents immediate dialogue restart for repeat scenarios (wrong choice)
        if (!hasLeftPOISinceLastDialogue)
        {
            Debug.Log($"[NPC:{npcID}] Player must leave POI and return to restart dialogue");
            return;
        }

        // Auto-start any available dialogue
        StartCurrentDialogue();
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

        string dialogueIDToStart = dialogue.requirement?.dialogueID ?? "NO_ID";
        Debug.Log($"[NPC:{npcID}] Starting dialogue at index {currentDialogueIndex}, ID: '{dialogueIDToStart}', InkJSON: {dialogue.inkJSON.name}");

        // Reset flag - player must leave and return again after this dialogue
        hasLeftPOISinceLastDialogue = false;

        // Subscribe to dialogue events
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded += OnDialogueComplete;
            DialogueManager.Instance.EnterDialogueMode(dialogue.inkJSON, dialogue.requirement?.dialogueID, npcName);
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

        // Set cooldown timer
        lastDialogueEndTime = Time.time;

        Debug.Log($"[NPC:{npcID}] OnDialogueComplete for '{dialogueID}', index: {currentDialogueIndex}");

        // Get the current dialogue
        if (currentDialogueIndex >= dialogues.Count)
        {
            Debug.LogWarning($"[NPC:{npcID}] Index out of range!");
            UpdateState();
            return;
        }

        NPCDialogue currentDialogue = dialogues[currentDialogueIndex];

        // DIRECT CHOICE CHECK: If repeatUntilCorrectChoice is true, check if correct choice was made
        if (currentDialogue.repeatUntilCorrectChoice &&
            currentDialogue.requirement != null &&
            currentDialogue.requirement.requiredChoices != null &&
            currentDialogue.requirement.requiredChoices.Count > 0 &&
            GameManager.Instance != null)
        {
            // Get the last choice made for this dialogue
            int lastChoice = GameManager.Instance.GetLastChoice(dialogueID, -1);

            // Check if it's in the acceptable choices list
            var req = currentDialogue.requirement.requiredChoices[0];
            bool isCorrectChoice = req.acceptableChoices.Contains(lastChoice);

            Debug.Log($"[NPC:{npcID}] Last choice: {lastChoice}, Acceptable: [{string.Join(",", req.acceptableChoices)}], Correct: {isCorrectChoice}");

            if (!isCorrectChoice)
            {
                Debug.Log($"[NPC:{npcID}] ❌ WRONG CHOICE - Staying on this dialogue");
                UpdateState();
                return;
            }
            else
            {
                Debug.Log($"[NPC:{npcID}] ✓ CORRECT CHOICE - Advancing");
            }
        }

        // Check if this is the last dialogue AND it should repeat
        bool isLastDialogue = currentDialogueIndex >= dialogues.Count - 1;
        bool shouldRepeat = currentDialogue.requirement != null && !currentDialogue.requirement.oneTimeOnly;

        if (isLastDialogue && shouldRepeat)
        {
            Debug.Log($"[NPC:{npcID}] Last dialogue set to repeat");
            UpdateState();
            return;
        }

        // Advance to next dialogue
        currentDialogueIndex++;
        hasLeftPOISinceLastDialogue = true;

        if (isLastDialogue)
        {
            Debug.Log($"[NPC:{npcID}] ✓ COMPLETED all dialogues (advanced to index: {currentDialogueIndex})");
        }
        else
        {
            Debug.Log($"[NPC:{npcID}] ✓ ADVANCED to dialogue index: {currentDialogueIndex}");
        }

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

    // ==================== RELOCATION SUPPORT ====================

    /// <summary>
    /// Move this NPC to a new POI
    /// </summary>
    public void RelocateToNewPOI(PointOfInterest newPOI, bool disableOldPOI = true)
    {
        if (newPOI == null)
        {
            Debug.LogWarning($"[NPCController] Cannot relocate {npcID} - newPOI is null");
            return;
        }

        Debug.Log($"[NPC:{npcID}] ========== RELOCATING to {newPOI.name} ==========");
        Debug.Log($"[NPC:{npcID}] Current dialogue index before relocation: {currentDialogueIndex}");

        // Store old POI for optional disabling
        PointOfInterest oldPOI = associatedPOI;

        // Update to new POI
        associatedPOI = newPOI;

        // Debug log to verify POI change
        Debug.Log($"[NPC:{npcID}] Associated POI changed from {oldPOI?.name} to {associatedPOI.name}");

        // Use characterPosition if assigned, otherwise use POI position
        if (newPOI.characterPosition != null)
        {
            transform.position = newPOI.characterPosition.position;
            transform.rotation = newPOI.characterPosition.rotation;
            Debug.Log($"[NPC:{npcID}] Using characterPosition for placement");
        }
        else
        {
            transform.position = newPOI.transform.position;
            Debug.LogWarning($"[NPC:{npcID}] {newPOI.name} has no characterPosition assigned! Using POI position instead.");
        }

        // Optionally disable old POI
        if (disableOldPOI && oldPOI != null)
        {
            oldPOI.gameObject.SetActive(false);
        }

        // Update character visibility component if it exists
        CharacterVisibility visibility = GetComponent<CharacterVisibility>();
        if (visibility != null)
        {
            visibility.poi = newPOI;
        }

        // Save relocation flag
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetFlag($"NPC_{npcID}_relocated", true);
        }

        // IMPORTANT: Reset cooldown to prevent immediate dialogue trigger
        // Give time for dialogue index to advance
        lastDialogueEndTime = Time.time;
        Debug.Log($"[NPC:{npcID}] Reset cooldown timer after relocation");

        // Update NPC state
        UpdateState();
        UpdateVisualIndicators();

        Debug.Log($"[NPC:{npcID}] Current dialogue index after relocation: {currentDialogueIndex}");
        Debug.Log($"[NPC:{npcID}] ========== RELOCATION COMPLETE ==========");
    }

    /// <summary>
    /// Check if this NPC has been relocated
    /// </summary>
    public bool HasBeenRelocated()
    {
        if (GameManager.Instance == null) return false;
        return GameManager.Instance.GetFlag($"NPC_{npcID}_relocated", false);
    }

    /// <summary>
    /// Get current POI (useful for external systems)
    /// </summary>
    public PointOfInterest GetCurrentPOI()
    {
        return associatedPOI;
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