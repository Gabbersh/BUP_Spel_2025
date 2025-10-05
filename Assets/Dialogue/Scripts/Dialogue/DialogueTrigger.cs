using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enhanced DialogueTrigger that integrates with GameManager for progression-based dialogue.
/// Replace your current DialogueTrigger with this version.
/// </summary>
public class DialogueTrigger : MonoBehaviour
{
    [Header("Ink JSON")]
    [SerializeField] private TextAsset inkJSON;

    [Header("Dialogue Identity")]
    [Tooltip("Unique ID for this dialogue (e.g., 'intro_sarah', 'chapter1_reveal')")]
    [SerializeField] private string dialogueID = "";

    [Header("POI Reference")]
    [SerializeField] private PointOfInterest associatedPOI;

    [Header("Trigger Settings")]
    [SerializeField] private float activationDistance = 0.5f;
    [SerializeField] private bool requirePOIActive = true;
    [SerializeField] private bool autoTriggerOnArrival = false;
    [SerializeField] private float cameraStopThreshold = 0.02f;

    [Header("Progression Settings")]
    [Tooltip("If true, this dialogue can only play once. Tracked by dialogueID.")]
    [SerializeField] private bool oneTimeOnly = true;

    [Tooltip("If true, this dialogue can repeat even after completion")]
    [SerializeField] private bool allowRepeat = false;

    [Header("Requirements (AND Logic)")]
    [Tooltip("Dialogues that must be complete before this one is available")]
    [SerializeField] private List<string> requiredDialogues = new List<string>();

    [Tooltip("Story flags that must be true (format: 'flagName' or 'flagName:value')")]
    [SerializeField] private List<string> requiredFlags = new List<string>();

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private CameraMovement cameraMovement;
    private bool canInteract = false;
    private bool hasTriggered = false;
    private bool cameraHasArrived = false;

    private void Awake()
    {
        cameraMovement = Camera.main?.GetComponent<CameraMovement>();

        // Generate ID if empty (not recommended for production)
        if (string.IsNullOrEmpty(dialogueID))
        {
            dialogueID = $"{gameObject.name}_{GetInstanceID()}";
            Debug.LogWarning($"[DialogueTrigger] No dialogueID set for {gameObject.name}. Auto-generated: {dialogueID}");
        }
    }

    private void OnEnable()
    {
        // Subscribe to progression changes
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnProgressionChanged += CheckRequirements;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnProgressionChanged -= CheckRequirements;
        }
    }

    private void Update()
    {
        bool wasAbleToInteract = canInteract;
        UpdateInteractionAvailability();

        // Check if camera has finished moving to POI
        if (canInteract && associatedPOI != null && !cameraHasArrived)
        {
            float distanceToTarget = Vector3.Distance(
                cameraMovement.transform.position,
                associatedPOI.cameraTarget.position
            );

            if (distanceToTarget <= cameraStopThreshold)
            {
                cameraMovement.transform.position = associatedPOI.cameraTarget.position;
                cameraMovement.transform.rotation = associatedPOI.cameraTarget.rotation;
                cameraHasArrived = true;
            }
            else
            {
                return;
            }
        }

        // Try to trigger dialogue
        if (canInteract && cameraHasArrived && !DialogueManager.GetInstance().dialogueIsPlaying)
        {
            // Check if already triggered and shouldn't repeat
            if (hasTriggered && !allowRepeat)
            {
                return;
            }

            // Check progression requirements
            if (!MeetsRequirements())
            {
                if (showDebugInfo)
                    Debug.Log($"[DialogueTrigger] '{dialogueID}' requirements not met");
                return;
            }

            // Check if this is a one-time dialogue that's already been completed
            if (oneTimeOnly && GameManager.Instance != null &&
                GameManager.Instance.IsDialogueComplete(dialogueID))
            {
                if (showDebugInfo)
                    Debug.Log($"[DialogueTrigger] '{dialogueID}' already completed (one-time only)");
                return;
            }

            // Trigger dialogue
            if (autoTriggerOnArrival && !hasTriggered)
            {
                StartDialogue();
            }
            else if (!autoTriggerOnArrival && InputManager.GetInstance().GetInteractPressed())
            {
                StartDialogue();
            }
        }

        // Reset when leaving POI
        if (!canInteract && wasAbleToInteract)
        {
            hasTriggered = false;
            cameraHasArrived = false;
        }
    }

    private void UpdateInteractionAvailability()
    {
        if (cameraMovement == null)
        {
            canInteract = false;
            return;
        }

        if (DialogueManager.GetInstance().dialogueIsPlaying)
        {
            return;
        }

        if (requirePOIActive && associatedPOI != null)
        {
            bool poiActive = associatedPOI.targetCamera.IsInPOI;
            bool cameraClose = Vector3.Distance(
                cameraMovement.transform.position,
                associatedPOI.cameraTarget.position) <= activationDistance;

            canInteract = poiActive && cameraClose;
        }
        else
        {
            canInteract = Vector3.Distance(
                cameraMovement.transform.position,
                transform.position) <= activationDistance;
        }
    }

    private void StartDialogue()
    {
        if (DialogueManager.GetInstance() != null)
        {
            DialogueManager.GetInstance().EnterDialogueMode(inkJSON, dialogueID);  // FIXED: Pass dialogueID!
            hasTriggered = true;

            // Mark as complete in GameManager if one-time only
            if (oneTimeOnly && GameManager.Instance != null)
            {
                GameManager.Instance.MarkDialogueComplete(dialogueID);
            }

            if (showDebugInfo)
                Debug.Log($"[DialogueTrigger] Started dialogue: {dialogueID}");
        }
    }

    /// <summary>
    /// Check if all progression requirements are met.
    /// </summary>
    private bool MeetsRequirements()
    {
        if (GameManager.Instance == null) return true; // No game manager = no restrictions

        // Check required dialogues
        foreach (string requiredDialogue in requiredDialogues)
        {
            if (!GameManager.Instance.IsDialogueComplete(requiredDialogue))
            {
                if (showDebugInfo)
                    Debug.Log($"[DialogueTrigger] Missing required dialogue: {requiredDialogue}");
                return false;
            }
        }

        // Check required flags
        foreach (string flagRequirement in requiredFlags)
        {
            if (!CheckFlagRequirement(flagRequirement))
            {
                if (showDebugInfo)
                    Debug.Log($"[DialogueTrigger] Flag requirement not met: {flagRequirement}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Check a single flag requirement.
    /// Format: "flagName" (checks if true) or "flagName:value" (checks if equals value)
    /// </summary>
    private bool CheckFlagRequirement(string requirement)
    {
        if (string.IsNullOrEmpty(requirement)) return true;

        string[] parts = requirement.Split(':');
        string flagName = parts[0].Trim();

        if (parts.Length == 1)
        {
            // Just check if flag is true
            return GameManager.Instance.GetFlag<bool>(flagName, false);
        }
        else if (parts.Length == 2)
        {
            // Check if flag equals specific value
            string expectedValue = parts[1].Trim();

            // Try as bool
            if (bool.TryParse(expectedValue, out bool boolValue))
            {
                return GameManager.Instance.CheckFlag(flagName, boolValue);
            }
            // Try as int
            else if (int.TryParse(expectedValue, out int intValue))
            {
                return GameManager.Instance.CheckFlag(flagName, intValue);
            }
            // Treat as string
            else
            {
                return GameManager.Instance.CheckFlag(flagName, expectedValue);
            }
        }

        return false;
    }

    /// <summary>
    /// Force check requirements (useful for debugging).
    /// </summary>
    private void CheckRequirements()
    {
        // This gets called when GameManager.OnProgressionChanged fires
        // You could disable/enable visual indicators here
    }

    // ==================== PUBLIC UTILITY METHODS ====================

    /// <summary>
    /// Manually trigger this dialogue (bypasses requirements).
    /// </summary>
    public void ForceStartDialogue()
    {
        StartDialogue();
    }

    /// <summary>
    /// Check if this dialogue is currently available.
    /// </summary>
    public bool IsAvailable()
    {
        return MeetsRequirements() &&
               (!oneTimeOnly || !GameManager.Instance.IsDialogueComplete(dialogueID));
    }
}