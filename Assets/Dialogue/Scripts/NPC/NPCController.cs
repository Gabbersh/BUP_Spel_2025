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
            return NPCState.Completed;
        }

        // Check if current dialogue can play
        NPCDialogue currentDialogue = dialogues[currentDialogueIndex];

        if (currentDialogue.requirement != null && ProgressionManager.Instance != null)
        {
            bool canPlay = ProgressionManager.Instance.CanPlayDialogue(currentDialogue.requirement);

            if (!canPlay)
            {
                return NPCState.Locked;
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
        if (!isWaitingAtPOI || !IsAvailable) return;
        if (DialogueManager.Instance == null || DialogueManager.Instance.DialogueIsPlaying) return;

        // Simple cooldown to prevent immediate re-trigger
        if (Time.time - lastDialogueEndTime < dialogueCooldown)
        {
            return;
        }

        StartCurrentDialogue();
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
        if (currentDialogueIndex >= dialogues.Count)
        {
            return;
        }

        NPCDialogue dialogue = dialogues[currentDialogueIndex];

        if (dialogue.inkJSON == null)
        {
            Debug.LogError($"[NPCController] {npcID} dialogue {currentDialogueIndex} has no Ink JSON!");
            return;
        }

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded += OnDialogueComplete;
            DialogueManager.Instance.OnDialogueEndedWithoutSuccess += OnDialogueEndedWithoutSuccess;
            DialogueManager.Instance.EnterDialogueMode(dialogue.inkJSON, dialogue.requirement?.dialogueID, npcName);
        }

        GameEvents.TriggerNPCInteracted(npcID);
        UpdateState();
    }

    private void OnDialogueEndedWithoutSuccess(string dialogueID)
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEndedWithoutSuccess -= OnDialogueEndedWithoutSuccess;
        }

        // Auto-exit POI when wrong choice is made
        Debug.Log($"[NPC:{npcID}] Wrong choice detected in '{dialogueID}'");

        if (cameraMovement == null)
        {
            Debug.LogError($"[NPC:{npcID}] CameraMovement is NULL! Cannot exit POI.");
            return;
        }

        Debug.Log($"[NPC:{npcID}] CameraMovement found. IsInPOI = {cameraMovement.IsInPOI}");

        if (cameraMovement.IsInPOI)
        {
            Debug.Log($"[NPC:{npcID}] Calling ReturnToRail()...");
            cameraMovement.ReturnToRail();
        }
        else
        {
            Debug.LogWarning($"[NPC:{npcID}] Camera is not in POI, cannot return to rail");
        }
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
            // Dialogue completed successfully - advance to next
            DebugLog($"Dialogue '{dialogueID}' completed successfully - advancing");
        }
        else if (currentDialogue.moveToNextAfterAttempt)
        {
            // Wrong choice, but this dialogue moves to next anyway (first-attempt dialogue)
            DebugLog($"Dialogue '{dialogueID}' NOT completed, but moving to next (moveToNextAfterAttempt = true)");
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

        // Move to next dialogue
        currentDialogueIndex++;

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

    [Tooltip("Move to next dialogue even if wrong choice? (For first-attempt dialogues that have a retry dialogue)")]
    public bool moveToNextAfterAttempt = false;
}