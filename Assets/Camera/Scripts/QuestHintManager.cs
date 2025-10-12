using UnityEngine;
using System.Collections;

public class QuestHintManager : MonoBehaviour
{
    [Header("References")]
    public CameraMovement cameraMovement;
    public GameObject[] highlightObjects;
    public GameObject[] interactableObjects;

    [Header("Debug")]
    public KeyCode toggleQuestKey = KeyCode.B;
    public bool questActive = false;

    private bool questEnding = false;

    private void Awake()
    {
        SetActiveAll(highlightObjects, false);
        SetActiveAll(interactableObjects, false);
    }

    private void Start()
    {
        if (cameraMovement != null)
        {
            cameraMovement.OnLeftRail += OnOffRail;
            cameraMovement.OnReachedPOI += OnOffRail;
            cameraMovement.OnReturnedToRail += OnOnRail;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleQuestKey))
        {
            questActive = !questActive;
            UpdateQuestState();
        }
    }

    private void UpdateQuestState()
    {
        if (!questActive)
        {
            questEnding = true; // prevent flicker while deactivating
            SetActiveAll(highlightObjects, false);
            SetActiveAll(interactableObjects, false);
            StartCoroutine(ResetQuestEndingFlag());
            return;
        }

        // Subscribe to interactables once at the start of the quest
        SubscribeToInteractables();

        // Set quest mode based on camera
        if (cameraMovement != null && cameraMovement.IsIdleOnRail)
            SetQuestMode(onRail: true);
        else
            SetQuestMode(onRail: false);
    }

    private void SetQuestMode(bool onRail)
    {
        SetActiveAll(highlightObjects, onRail);

        // Only activate interactables if quest is active and not ending
        if (!questEnding)
            SetActiveAll(interactableObjects, !onRail);
    }

    // --- Camera Event Handlers ---
    private void OnOffRail()
    {
        if (!questActive) return;
        SetQuestMode(onRail: false);
    }

    private void OnOnRail()
    {
        if (!questActive) return;
        SetQuestMode(onRail: true);
    }

    // --- Interactable subscription ---
    private void SubscribeToInteractables()
    {
        foreach (var obj in interactableObjects)
        {
            if (obj == null) continue;

            var interactable = obj.GetComponent<Interactable>();
            if (interactable != null)
            {
                // Ensure only one subscription
                interactable.OnPickedUp -= HandleInteractiblePickedUp;
                interactable.OnPickedUp += HandleInteractiblePickedUp;
            }
        }
    }

    private void HandleInteractiblePickedUp(Interactable interactable)
    {
        StartCoroutine(EndQuestAfterDelay(interactable.DeactivateDelay + 0.1f, interactable));
    }

    private IEnumerator EndQuestAfterDelay(float delay, Interactable pickedUp)
    {
        yield return new WaitForSeconds(delay);
        EndQuest(pickedUp);
    }

    private void EndQuest(Interactable pickedUp)
    {
        questActive = false;
        questEnding = true;

        // Deactivate highlights and all interactables
        SetActiveAll(highlightObjects, false);
        SetActiveAll(interactableObjects, false);

        // Reset ending flag next frame
        StartCoroutine(ResetQuestEndingFlag());
    }

    private IEnumerator ResetQuestEndingFlag()
    {
        yield return null; // wait one frame
        questEnding = false;
    }

    private void SetActiveAll(GameObject[] objects, bool state)
    {
        foreach (var obj in objects)
            if (obj != null)
                obj.SetActive(state);
    }
}
