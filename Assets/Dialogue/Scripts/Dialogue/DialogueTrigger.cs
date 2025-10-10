using UnityEngine;

/// <summary>
/// SIMPLIFIED DialogueTrigger - Just bridges input to NPCController.
/// All logic is now in NPCController, making this much simpler.
/// </summary>
public class DialogueTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NPCController npcController;
    [SerializeField] private PointOfInterest associatedPOI;

    [Header("Settings")]
    [SerializeField] private float interactionDistance = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private CameraMovement cameraMovement;
    private bool canInteract = false;

    private void Awake()
    {
        cameraMovement = Camera.main?.GetComponent<CameraMovement>();

        // Auto-find NPCController if not assigned
        if (npcController == null)
        {
            npcController = GetComponent<NPCController>();
        }

        if (npcController == null)
        {
            Debug.LogError($"[DialogueTrigger] No NPCController found on {gameObject.name}!");
        }
    }

    private void Update()
    {
        UpdateInteractionState();
        HandleInput();
    }

    private void UpdateInteractionState()
    {
        if (cameraMovement == null || associatedPOI == null || npcController == null)
        {
            canInteract = false;
            return;
        }

        // Don't allow interaction during dialogue
        if (DialogueManager.Instance != null && DialogueManager.Instance.DialogueIsPlaying)
        {
            canInteract = false;
            return;
        }

        // Check if at POI and NPC is available
        bool atPOI = cameraMovement.IsInPOI;
        bool closeEnough = Vector3.Distance(
            cameraMovement.transform.position,
            associatedPOI.cameraTarget.position
        ) < interactionDistance;

        canInteract = atPOI && closeEnough && npcController.IsAvailable;
    }

    private void HandleInput()
    {
        if (!canInteract) return;

        if (InputManager.Instance != null && InputManager.Instance.GetInteractPressed())
        {
            DebugLog("Interaction triggered");
            npcController.TryInteract();
        }
    }

    // ==================== PUBLIC API ====================

    /// <summary>
    /// Manually trigger interaction (for UI buttons, etc.)
    /// </summary>
    public void TriggerInteraction()
    {
        if (npcController != null && npcController.IsAvailable)
        {
            npcController.TryInteract();
        }
    }

    /// <summary>
    /// Check if currently can interact
    /// </summary>
    public bool CanInteract()
    {
        return canInteract;
    }

    // ==================== UTILITY ====================

    private void DebugLog(string message)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[DialogueTrigger] {message}");
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (associatedPOI != null && associatedPOI.cameraTarget != null)
        {
            Gizmos.color = canInteract ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(associatedPOI.cameraTarget.position, interactionDistance);
        }
    }
#endif
}

/// <summary>
/// Alternative: UI Button trigger for mobile-friendly interaction
/// Attach to a UI button and assign NPCController
/// </summary>
public class UIDialogueTrigger : MonoBehaviour
{
    [SerializeField] private NPCController npcController;

    public void OnButtonClick()
    {
        if (npcController != null && npcController.IsAvailable)
        {
            npcController.TryInteract();
        }
    }
}