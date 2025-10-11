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
            Debug.Log($"Quest {(questActive ? "Activated" : "Deactivated")}");
            UpdateQuestState();
        }
    }

    private void UpdateQuestState()
    {
        if (!questActive)
        {
            SetActiveAll(highlightObjects, false);
            SetActiveAll(interactableObjects, false);
            return;
        }

        if (cameraMovement != null && cameraMovement.IsIdleOnRail)
            SetQuestMode(onRail: true);
        else
            SetQuestMode(onRail: false);
    }

    private void SetQuestMode(bool onRail)
    {
        SetActiveAll(highlightObjects, onRail);
        SetActiveAll(interactableObjects, !onRail);

        if (!onRail)
            SubscribeToInteractables();
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
                interactable.OnPickedUp -= HandleInteractiblePickedUp;
                interactable.OnPickedUp += HandleInteractiblePickedUp;
            }
        }
    }

    private void HandleInteractiblePickedUp(Interactable interactable)
    {
        Debug.Log($"Picked up {interactable.name}, ending quest soon...");
        StartCoroutine(EndQuestAfterDelay(interactable.DeactivateDelay + 0.1f, interactable));
    }

    private IEnumerator EndQuestAfterDelay(float delay, Interactable pickedUp)
    {
        Debug.Log($"Quest will end in {delay:F1} seconds (waiting for {pickedUp.name} to finish deactivating).");
        yield return new WaitForSeconds(delay);
        EndQuest(pickedUp);
    }

    private void EndQuest(Interactable pickedUp)
    {
        questActive = false;
        SetActiveAll(highlightObjects, false);

        foreach (var obj in interactableObjects)
        {
            if (obj == null || obj == pickedUp.gameObject) continue;
            obj.SetActive(false);
        }

        Debug.Log($"Quest ended after picking up {pickedUp.name}.");
    }

    // --- Utility ---
    private void SetActiveAll(GameObject[] objects, bool state)
    {
        foreach (var obj in objects)
            if (obj != null)
                obj.SetActive(state);
    }
}
