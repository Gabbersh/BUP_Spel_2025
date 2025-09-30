using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Ink JSON")]
    [SerializeField] private TextAsset inkJSON;

    [Header("POI Reference")]
    [SerializeField] private PointOfInterest associatedPOI;

    [Header("Trigger Settings")]
    [SerializeField] private float activationDistance = 0.5f;
    [SerializeField] private bool requirePOIActive = true;
    [SerializeField] private bool autoTriggerOnArrival = false;
    [SerializeField] private bool allowRepeat = false;
    [SerializeField] private float cameraStopThreshold = 0.02f; // Smaller threshold for precision

    private CameraMovement cameraMovement;
    private bool canInteract = false;
    private bool hasTriggered = false;
    private bool cameraHasArrived = false;

    private void Awake()
    {
        cameraMovement = Camera.main?.GetComponent<CameraMovement>();
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
                // NEW: Snap camera to exact position to prevent drift
                cameraMovement.transform.position = associatedPOI.cameraTarget.position;
                cameraMovement.transform.rotation = associatedPOI.cameraTarget.rotation;
                cameraHasArrived = true;
            }
            else
            {
                return;
            }
        }

        if (canInteract && cameraHasArrived && !DialogueManager.GetInstance().dialogueIsPlaying)
        {
            if (hasTriggered && !allowRepeat)
            {
                return;
            }

            if (autoTriggerOnArrival && !hasTriggered)
            {
                DialogueManager.GetInstance().EnterDialogueMode(inkJSON);
                hasTriggered = true;
            }
            else if (!autoTriggerOnArrival && InputManager.GetInstance().GetInteractPressed())
            {
                DialogueManager.GetInstance().EnterDialogueMode(inkJSON);
                hasTriggered = true;
            }
        }

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
}