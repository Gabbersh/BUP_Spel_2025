using UnityEngine;
using UnityEngine.UI;

public class ReturnButtonController : MonoBehaviour
{
    public CameraMovement cameraMovement;
    public Button returnButton;

    void Start()
    {
        returnButton.gameObject.SetActive(false);
        returnButton.onClick.AddListener(OnReturnPressed);

        // Subscribe to camera events
        cameraMovement.OnReachedPOI += ShowButton;
        cameraMovement.OnLeftPOI += HideButton;
        cameraMovement.OnReturnedToRail += HideButton;

        // Subscribe to dialogue events to disable button during dialogue
        GameEvents.OnDialogueStarted += OnDialogueStarted;
        GameEvents.OnDialogueEnded += OnDialogueEnded;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        GameEvents.OnDialogueStarted -= OnDialogueStarted;
        GameEvents.OnDialogueEnded -= OnDialogueEnded;
    }

    private void ShowButton()
    {
        returnButton.gameObject.SetActive(true);
    }

    private void HideButton()
    {
        returnButton.gameObject.SetActive(false);
    }

    private void OnDialogueStarted(string dialogueID)
    {
        // Disable button during dialogue
        returnButton.interactable = false;
        Debug.Log("[ReturnButton] Disabled during dialogue");
    }

    private void OnDialogueEnded(string dialogueID)
    {
        // Re-enable button after dialogue
        returnButton.interactable = true;
        Debug.Log("[ReturnButton] Re-enabled after dialogue");
    }

    void OnReturnPressed()
    {
        // No need to check DialogueIsPlaying anymore - button is disabled during dialogue
        cameraMovement.ReturnToRail();
    }
}