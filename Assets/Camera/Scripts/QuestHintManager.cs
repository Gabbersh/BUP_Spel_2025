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
        if (cameraMovement == null) return;

        cameraMovement.OnLeftRail += () => UpdateQuestMode(false);
        cameraMovement.OnReachedPOI += () => UpdateQuestMode(false);
        cameraMovement.OnReturnedToRail += () => UpdateQuestMode(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleQuestKey))
        {
            questActive = !questActive;
            HandleQuestToggle();
        }
    }

    private void HandleQuestToggle()
    {
        StopAllCoroutines();

        if (!questActive)
        {
            EndQuestInstant();
            return;
        }

        SubscribeToInteractables();

        bool onRail = cameraMovement == null || cameraMovement.IsIdleOnRail;
        UpdateQuestMode(onRail);
    }

    private void UpdateQuestMode(bool onRail)
    {
        if (!questActive || questEnding) return;

        SetActiveAll(highlightObjects, onRail);
        SetActiveAll(interactableObjects, !onRail);
    }

    private void SubscribeToInteractables()
    {
        foreach (var obj in interactableObjects)
        {
            var interactable = obj?.GetComponent<Interactable>();
            if (interactable == null) continue;

            interactable.OnPickedUp -= HandlePickedUp;
            interactable.OnPickedUp += HandlePickedUp;
        }
    }

    private void HandlePickedUp(Interactable interactable)
    {
        if (questEnding) return;

        questEnding = true;
        StartCoroutine(EndQuestAfterDelay(1.7f)); // consistent delay 
    }

    private IEnumerator EndQuestAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        EndQuestInstant();
    }

    private void EndQuestInstant()
    {
        questActive = false;
        questEnding = false;

        SetActiveAll(highlightObjects, false);
        SetActiveAll(interactableObjects, false);
    }

    private void SetActiveAll(GameObject[] objects, bool state)
    {
        foreach (var obj in objects)
            if (obj) obj.SetActive(state);
    }

    public void ActivateQuestMode()
    {
        if (questActive) return; // Already active
        questActive = true;
        HandleQuestToggle(); // Reuse the existing logic
    }

    public void DeactivateQuestMode()
    {
        if (!questActive) return; // Already inactive
        questActive = false;
        HandleQuestToggle(); // Reuse the existing logic
    }
}