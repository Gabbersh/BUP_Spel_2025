using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls individual NPC behavior, dialogues, and state.
/// Handles all dialogue progression for a single character.
/// SIMPLIFIED: Easy to use - just add Ink files in order!
/// </summary>
public class NPCController : MonoBehaviour
{
    [Header("NPC Identity")]
    [SerializeField] private string npcID = "";
    [SerializeField] private string npcName = "Character";

    [Header("Point of Interest")]
    [SerializeField] private PointOfInterest associatedPOI;
    [SerializeField] private float interactionDistance = 0.5f;

    [Header("Dialogues")]
    [Tooltip("Add your Ink dialogues in order. They'll play sequentially.")]
    [SerializeField] private List<NPCDialogue> dialogues = new List<NPCDialogue>();

    [Header("Default Dialogues")]
    [Tooltip("Idle Default: Plays when story dialogue is locked (waiting for quest/flag). Repeats each time.")]
    [SerializeField] private TextAsset idleDefault;
    [Tooltip("Force camera to exit POI after idle default?")]
    [SerializeField] private bool forceExitAfterIdle = false;

    [Tooltip("Completed Default: Plays when all story dialogues are finished. Repeats each time.")]
    [SerializeField] private TextAsset completedDefault;
    [Tooltip("Force camera to exit POI after completed default?")]
    [SerializeField] private bool forceExitAfterCompleted = false;

    [Header("Settings")]
    [SerializeField] private float dialogueCooldown = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // State
    private NPCState currentState = NPCState.Locked;
    private int currentDialogueIndex = 0;
    private bool isWaitingAtPOI = false;
    private CameraMovement cameraMovement;
    private float lastDialogueEndTime = -999f;
    private bool currentDialogueShouldForceExit = false; // Track if current dialogue should force exit
    private bool hasPlayedDefaultThisEntry = false; // prevents repeat while inside POI
    private bool wasInPOI = false; // detects POI entry

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

        if (NPCManager.Instance != null)
        {
            NPCManager.Instance.RegisterNPC(this);
        }
    }

    private void OnDestroy()
    {
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
            currentState = newState;
            GameEvents.TriggerNPCStateChanged(npcID, newState);

            if (newState == NPCState.Available)
                GameEvents.TriggerNPCBecameAvailable(npcID);
            else if (currentState == NPCState.Available)
                GameEvents.TriggerNPCBecameUnavailable(npcID);
        }
    }

    private NPCState DetermineState()
    {
        // Currently talking?
        if (DialogueManager.Instance != null && DialogueManager.Instance.DialogueIsPlaying)
        {
            return NPCState.Talking;
        }

        // No dialogues?
        if (dialogues == null || dialogues.Count == 0)
        {
            return NPCState.Locked;
        }

        // All dialogues complete?
        if (currentDialogueIndex >= dialogues.Count)
        {
            // Always mark as completed when all dialogues are finished
            return NPCState.Completed;
        }

        // Check if current dialogue can play
        NPCDialogue currentDialogue = dialogues[currentDialogueIndex];

        if (currentDialogue.requirement != null && ProgressionManager.Instance != null)
        {
            bool canPlay = ProgressionManager.Instance.CanPlayDialogue(currentDialogue.requirement);

            if (!canPlay)
            {
                // Dialogue is locked, but check if we have idle default
                if (currentDialogue.customIdleDefault != null || idleDefault != null)
                {
                    return NPCState.Available; // Can play idle default
                }
                return NPCState.Locked; // No default, truly locked
            }
        }

        return NPCState.Available;
    }

    // ==================== INTERACTION ====================

    private void UpdateInteractionState()
    {
        if (cameraMovement == null || associatedPOI == null) return;

        bool atPOI = cameraMovement.IsInPOI;
        bool closeEnough = Vector3.Distance(
            cameraMovement.transform.position,
            associatedPOI.cameraTarget.position
        ) < interactionDistance;

        isWaitingAtPOI = atPOI && closeEnough;
    }

    private void HandleAutoDialogue()
    {
        if (cameraMovement == null || associatedPOI == null) return;

        bool atPOI = cameraMovement.IsInPOI;
        bool closeEnough = Vector3.Distance(
            cameraMovement.transform.position,
            associatedPOI.cameraTarget.position
        ) < interactionDistance;

        isWaitingAtPOI = atPOI && closeEnough;

        // --- Detect entering POI (only trigger once per entry) ---
        if (isWaitingAtPOI && !wasInPOI)
        {
            wasInPOI = true;
            hasPlayedDefaultThisEntry = false; // reset for new entry
            TryAutoPlayDialogueOnEntry();
        }
        else if (!isWaitingAtPOI)
        {
            wasInPOI = false; // reset when leaving POI
        }
    }

    /// <summary>
    /// Plays the correct dialogue automatically when player ENTERS the POI.
    /// </summary>
    private void TryAutoPlayDialogueOnEntry()
    {
        if (DialogueManager.Instance == null || DialogueManager.Instance.DialogueIsPlaying) return;

        if (Time.time - lastDialogueEndTime < dialogueCooldown)
            return;

        // Skip if we already played this entry (safety)
        if (hasPlayedDefaultThisEntry)
            return;

        hasPlayedDefaultThisEntry = true;

        // --- 1. If story dialogue is available, play that ---
        if (IsAvailable && currentDialogueIndex < dialogues.Count)
        {
            NPCDialogue currentDialogue = dialogues[currentDialogueIndex];

            bool locked = currentDialogue.requirement != null &&
                          ProgressionManager.Instance != null &&
                          !ProgressionManager.Instance.CanPlayDialogue(currentDialogue.requirement);

            if (!locked)
            {
                StartCurrentDialogue();
                return;
            }
        }

        // --- 2. Otherwise, play idle or completed defaults ---
        if (currentDialogueIndex >= dialogues.Count)
        {
            if (completedDefault != null)
            {
                StartCompletedDefaultDialogue();
                return;
            }
        }

        // If story dialogue is locked → use idle default
        if (idleDefault != null)
        {
            StartIdleDefaultDialogue();
        }
    }

    /// <summary>
    /// NEW: Plays idle default dialogue when no story dialogue is available.
    /// </summary>
    private void StartIdleDefaultDialogue()
    {
        if (idleDefault == null || DialogueManager.Instance == null)
            return;

        currentDialogueShouldForceExit = forceExitAfterIdle;

        DialogueManager.Instance.OnDialogueEnded += OnDefaultDialogueEnded;

        DialogueManager.Instance.EnterDialogueMode(idleDefault, $"{npcID}_idle_default_entry", npcName);

        DebugLog("Playing idle default dialogue (on POI entry)");

        lastDialogueEndTime = Time.time;

        GameEvents.TriggerNPCInteracted(npcID);
    }


    /// <summary>
    /// NEW: Handles the auto-play of the "completed default" dialogue
    /// every time player enters POI after all story dialogues are done.
    /// </summary>
    private void StartCompletedDefaultDialogue()
    {
        if (completedDefault == null || DialogueManager.Instance == null)
            return;

        currentDialogueShouldForceExit = forceExitAfterCompleted;

        // Subscribe to OnDialogueEnded (cleanup + optional force exit)
        DialogueManager.Instance.OnDialogueEnded += OnDefaultDialogueEnded;

        DialogueManager.Instance.EnterDialogueMode(completedDefault, $"{npcID}_completed_default", npcName);

        DebugLog("Playing completed default dialogue (auto-triggered)");

        lastDialogueEndTime = Time.time;

        GameEvents.TriggerNPCInteracted(npcID);
    }



    public void TryInteract()
    {
        if (!IsAvailable || !isWaitingAtPOI)
        {
            return;
        }

        StartCurrentDialogue();
    }

    private void StartCurrentDialogue()
    {
        // Determine which dialogue to play (story, idle, or completed)
        TextAsset dialogueToPlay = null;
        string dialogueIDToUse = "";
        bool isDefaultDialogue = false;
        currentDialogueShouldForceExit = false; // Reset

        // Case 1: All dialogues complete → play completed default
        if (currentDialogueIndex >= dialogues.Count)
        {
            if (completedDefault != null)
            {
                dialogueToPlay = completedDefault;
                dialogueIDToUse = $"{npcID}_completed_default";
                isDefaultDialogue = true;
                currentDialogueShouldForceExit = forceExitAfterCompleted; // Check if should force exit
                DebugLog("Playing completed default dialogue");
            }
            else
            {
                // No completed default set - NPC has nothing to say
                Debug.LogWarning($"[NPCController] {npcID} has no completed default dialogue set. Consider adding one!");
                return;
            }
        }
        // Case 2: Current dialogue exists
        else
        {
            NPCDialogue currentDialogue = dialogues[currentDialogueIndex];

            // Check if current dialogue is locked
            bool isLocked = false;
            if (currentDialogue.requirement != null && ProgressionManager.Instance != null)
            {
                isLocked = !ProgressionManager.Instance.CanPlayDialogue(currentDialogue.requirement);
            }

            if (isLocked)
            {
                // Dialogue is locked → play idle default
                if (currentDialogue.customIdleDefault != null)
                {
                    dialogueToPlay = currentDialogue.customIdleDefault;
                    dialogueIDToUse = $"{npcID}_custom_idle_{currentDialogueIndex}";
                    isDefaultDialogue = true;
                    currentDialogueShouldForceExit = forceExitAfterIdle; // Use global idle setting
                    DebugLog($"Playing custom idle default for dialogue {currentDialogueIndex}");
                }
                else if (idleDefault != null)
                {
                    dialogueToPlay = idleDefault;
                    dialogueIDToUse = $"{npcID}_idle_default";
                    isDefaultDialogue = true;
                    currentDialogueShouldForceExit = forceExitAfterIdle; // Use global idle setting
                    DebugLog("Playing global idle default");
                }
                else
                {
                    // No idle default set - NPC is truly locked
                    Debug.LogWarning($"[NPCController] {npcID} dialogue {currentDialogueIndex} is locked but has no idle default. NPC will not respond.");
                    return;
                }
            }
            else
            {
                // Play normal story dialogue
                if (currentDialogue.inkJSON == null)
                {
                    Debug.LogError($"[NPCController] {npcID} dialogue {currentDialogueIndex} has no Ink JSON!");
                    return;
                }
                dialogueToPlay = currentDialogue.inkJSON;
                dialogueIDToUse = currentDialogue.requirement?.dialogueID;
                isDefaultDialogue = false;
                currentDialogueShouldForceExit = currentDialogue.forceExitPOI; // Check dialogue's force exit setting
                DebugLog($"Playing story dialogue {currentDialogueIndex}");
            }
        }

        // Play the determined dialogue
        if (dialogueToPlay != null && DialogueManager.Instance != null)
        {
            // Only subscribe to completion events for story dialogues, not defaults
            if (!isDefaultDialogue)
            {
                DialogueManager.Instance.OnDialogueEnded += OnDialogueComplete;
                DialogueManager.Instance.OnDialogueEndedWithoutSuccess += OnDialogueEndedWithoutSuccess;
            }
            else
            {
                // For defaults, just subscribe to ended event to clean up
                DialogueManager.Instance.OnDialogueEnded += OnDefaultDialogueEnded;
            }

            DialogueManager.Instance.EnterDialogueMode(dialogueToPlay, dialogueIDToUse, npcName);
        }

        GameEvents.TriggerNPCInteracted(npcID);
        UpdateState();
    }

    /// <summary>
    /// Called when a default dialogue ends (no progression logic needed)
    /// </summary>
    private void OnDefaultDialogueEnded(string dialogueID)
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= OnDefaultDialogueEnded;
        }

        lastDialogueEndTime = Time.time;

        // Check if this default dialogue should force exit POI
        if (currentDialogueShouldForceExit)
        {
            ForceExitPOI("Default dialogue marked to force exit POI");
        }

        UpdateState();
    }

    /// <summary>
    /// Helper method to force camera exit from POI
    /// </summary>
    private void ForceExitPOI(string reason)
    {
        if (cameraMovement != null && cameraMovement.IsInPOI)
        {
            DebugLog(reason);
            cameraMovement.ReturnToRail();
        }
    }

    private void OnDialogueEndedWithoutSuccess(string dialogueID)
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEndedWithoutSuccess -= OnDialogueEndedWithoutSuccess;
        }

        // Auto-exit POI when wrong choice is made (ALWAYS force exit on wrong choice)
        Debug.Log($"[NPC:{npcID}] Wrong choice detected in '{dialogueID}'");
        ForceExitPOI($"Wrong choice made in '{dialogueID}' - auto-exiting POI");
    }

    private void OnDialogueComplete(string dialogueID)
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= OnDialogueComplete;

            // If dialogue was successful, also unsubscribe from OnDialogueEndedWithoutSuccess
            // (since it won't fire for successful dialogues)
            if (GameManager.Instance != null && GameManager.Instance.IsDialogueComplete(dialogueID))
            {
                DialogueManager.Instance.OnDialogueEndedWithoutSuccess -= OnDialogueEndedWithoutSuccess;
            }
            // If not successful, OnDialogueEndedWithoutSuccess will fire and unsubscribe itself
        }

        lastDialogueEndTime = Time.time;

        if (currentDialogueIndex >= dialogues.Count)
        {
            UpdateState();
            return;
        }

        NPCDialogue currentDialogue = dialogues[currentDialogueIndex];

        // Always mark dialogue as attempted
        if (GameManager.Instance != null && !string.IsNullOrEmpty(dialogueID))
        {
            GameManager.Instance.MarkDialogueAttempted(dialogueID);
        }

        // Check if dialogue was completed (had #success tag)
        bool wasCompleted = false;
        if (GameManager.Instance != null)
        {
            wasCompleted = GameManager.Instance.IsDialogueComplete(dialogueID);
        }

        if (wasCompleted)
        {
            // Dialogue completed successfully!
            DebugLog($"Dialogue '{dialogueID}' completed successfully");

            // Check if should force exit POI (even on success)
            if (currentDialogueShouldForceExit)
            {
                ForceExitPOI("Dialogue marked to force exit POI");
            }

            // Skip all retry dialogues that come next
            int nextIndex = currentDialogueIndex + 1;
            while (nextIndex < dialogues.Count && dialogues[nextIndex].isRetryDialogue)
            {
                DebugLog($"Skipping retry dialogue at index {nextIndex}");
                nextIndex++;
            }
            currentDialogueIndex = nextIndex;
        }
        else if (currentDialogue.moveToNextAfterAttempt)
        {
            // Wrong choice, but this dialogue moves to next anyway (first-attempt dialogue)
            DebugLog($"Dialogue '{dialogueID}' NOT completed, but moving to next (moveToNextAfterAttempt = true)");
            currentDialogueIndex++;
        }
        else
        {
            // Wrong choice and should retry - stay on this dialogue
            DebugLog($"Dialogue '{dialogueID}' NOT completed - staying on index {currentDialogueIndex}");
            UpdateState();
            return;
        }

        // Check if this is the last dialogue and should repeat
        bool isLastDialogue = currentDialogueIndex >= dialogues.Count - 1;
        bool shouldRepeat = currentDialogue.requirement != null && !currentDialogue.requirement.oneTimeOnly;

        if (isLastDialogue && shouldRepeat)
        {
            UpdateState();
            return;
        }

        UpdateState();
        GameEvents.TriggerProgressionChanged();
    }

    // ==================== RELOCATION SUPPORT ====================

    public void RelocateToNewPOI(PointOfInterest newPOI, bool disableOldPOI = true)
    {
        if (newPOI == null)
        {
            Debug.LogWarning($"[NPCController] Cannot relocate {npcID} - newPOI is null");
            return;
        }

        PointOfInterest oldPOI = associatedPOI;
        associatedPOI = newPOI;

        if (newPOI.characterPosition != null)
        {
            transform.position = newPOI.characterPosition.position;
            transform.rotation = newPOI.characterPosition.rotation;
        }
        else
        {
            transform.position = newPOI.transform.position;
        }

        if (disableOldPOI && oldPOI != null)
        {
            oldPOI.gameObject.SetActive(false);
        }

        CharacterVisibility visibility = GetComponent<CharacterVisibility>();
        if (visibility != null)
        {
            visibility.poi = newPOI;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetFlag($"NPC_{npcID}_relocated", true);
        }

        lastDialogueEndTime = Time.time;
        UpdateState();
    }

    public bool HasBeenRelocated()
    {
        if (GameManager.Instance == null) return false;
        return GameManager.Instance.GetFlag($"NPC_{npcID}_relocated", false);
    }

    public PointOfInterest GetCurrentPOI()
    {
        return associatedPOI;
    }

    // ==================== PUBLIC API ====================

    public void SetDialogueIndex(int index)
    {
        if (index >= 0 && index < dialogues.Count)
        {
            currentDialogueIndex = index;
            UpdateState();
        }
    }

    public DialogueRequirement GetCurrentRequirement()
    {
        if (currentDialogueIndex >= dialogues.Count) return null;
        return dialogues[currentDialogueIndex].requirement;
    }

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

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, associatedPOI.transform.position);
        }
    }
#endif
}

/// <summary>
/// SIMPLIFIED: Just add your Ink file and optional requirements!
/// </summary>
[System.Serializable]
public class NPCDialogue
{
    [Tooltip("Your Ink dialogue file")]
    public TextAsset inkJSON;

    [Tooltip("Optional: Requirements for this dialogue to play")]
    public DialogueRequirement requirement;

    [Header("Retry System")]
    [Tooltip("Is this a retry dialogue? (Only plays if previous dialogue failed)")]
    public bool isRetryDialogue = false;

    [Tooltip("Move to next dialogue even if wrong choice? (For first-attempt dialogues)")]
    public bool moveToNextAfterAttempt = false;

    [Header("POI Behavior")]
    [Tooltip("Force camera to exit POI after this dialogue ends? (Happens after correct choice OR no choices)")]
    public bool forceExitPOI = false;

    [Header("Idle Default (Optional)")]
    [Tooltip("Custom idle dialogue if THIS specific dialogue is locked. Leave empty to use global idle default.")]
    public TextAsset customIdleDefault;
}