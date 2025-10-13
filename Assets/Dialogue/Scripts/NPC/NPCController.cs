using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls individual NPC behavior, dialogues, and state.
/// Handles all dialogue progression for a single character.
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
    [SerializeField] private List<NPCDialogue> dialogues = new List<NPCDialogue>();

    [Header("Visual Feedback")]
    [SerializeField] private GameObject availableIndicator;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // State
    private NPCState currentState = NPCState.Locked;
    private int currentDialogueIndex = 0;
    private bool isWaitingAtPOI = false;
    private CameraMovement cameraMovement;
    private float lastDialogueEndTime = -999f;
    private float dialogueCooldown = 0.5f;
    private bool hasLeftPOISinceLastDialogue = true;

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

            UpdateVisualIndicators();
        }
    }

    private NPCState DetermineState()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.DialogueIsPlaying)
        {
            return NPCState.Talking;
        }

        if (dialogues == null || dialogues.Count == 0)
        {
            return NPCState.Locked;
        }

        if (currentDialogueIndex >= dialogues.Count)
        {
            return NPCState.Completed;
        }

        NPCDialogue currentDialogue = dialogues[currentDialogueIndex];

        if (currentDialogue.requirement != null && ProgressionManager.Instance != null)
        {
            bool canPlay = ProgressionManager.Instance.CanPlayDialogue(currentDialogue.requirement);

            if (!canPlay)
            {
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

        bool atPOI = cameraMovement.IsInPOI;
        bool closeEnough = Vector3.Distance(
            cameraMovement.transform.position,
            associatedPOI.cameraTarget.position
        ) < interactionDistance;

        bool wasWaitingAtPOI = isWaitingAtPOI;
        isWaitingAtPOI = atPOI && closeEnough;

        if (wasWaitingAtPOI && !isWaitingAtPOI)
        {
            hasLeftPOISinceLastDialogue = true;
        }
    }

    private void HandleAutoDialogue()
    {
        if (!isWaitingAtPOI || !IsAvailable) return;
        if (DialogueManager.Instance == null || DialogueManager.Instance.DialogueIsPlaying) return;

        if (Time.time - lastDialogueEndTime < dialogueCooldown)
        {
            return;
        }

        if (!hasLeftPOISinceLastDialogue)
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

        hasLeftPOISinceLastDialogue = false;

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
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueEnded -= OnDialogueComplete;
        }

        lastDialogueEndTime = Time.time;

        if (currentDialogueIndex >= dialogues.Count)
        {
            UpdateState();
            return;
        }

        NPCDialogue currentDialogue = dialogues[currentDialogueIndex];

        // FIXED: Check if dialogue should repeat based on completion status
        // If repeatUntilCorrectChoice is true, only advance if dialogue was actually marked complete
        if (currentDialogue.repeatUntilCorrectChoice && GameManager.Instance != null)
        {
            bool wasCompleted = GameManager.Instance.IsDialogueComplete(dialogueID);

            if (!wasCompleted)
            {
                // Dialogue was NOT marked complete (wrong choice or cancelled)
                // Stay on this dialogue index - player must try again
                Debug.Log($"[NPC:{npcID}] Dialogue '{dialogueID}' NOT completed - staying on index {currentDialogueIndex}");
                UpdateState();
                return;
            }
            else
            {
                // Dialogue was marked complete (correct choice with #success tag)
                Debug.Log($"[NPC:{npcID}] Dialogue '{dialogueID}' completed successfully - advancing");
            }
        }

        bool isLastDialogue = currentDialogueIndex >= dialogues.Count - 1;
        bool shouldRepeat = currentDialogue.requirement != null && !currentDialogue.requirement.oneTimeOnly;

        if (isLastDialogue && shouldRepeat)
        {
            UpdateState();
            return;
        }

        currentDialogueIndex++;
        hasLeftPOISinceLastDialogue = true;

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
        UpdateVisualIndicators();
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

[System.Serializable]
public class NPCDialogue
{
    public TextAsset inkJSON;
    public DialogueRequirement requirement;
    public bool repeatUntilCorrectChoice = false;
}