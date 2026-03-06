using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ReturnButtonController : MonoBehaviour
{
    public CameraMovement cameraMovement;
    public Button returnButton;

    private CanvasGroup canvasGroup;
    private bool dialogueActive = false;

    void Start()
    {
        canvasGroup = returnButton.GetComponent<CanvasGroup>();

        HideButton();

        returnButton.onClick.AddListener(OnReturnPressed);

        cameraMovement.OnReachedPOI += ShowButton;
        cameraMovement.OnLeftPOI += HideButton;
        cameraMovement.OnReturnedToRail += HideButton;

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
        StartCoroutine(DelayedUpdate());
    }

    private IEnumerator DelayedUpdate()
    {
        yield return null; // vänta en frame
        UpdateButtonState();
    }

    private void HideButton()
    {
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void OnDialogueStarted(string dialogueID)
    {
        dialogueActive = true;
        UpdateButtonState();
    }

    private void OnDialogueEnded(string dialogueID)
    {
        dialogueActive = false;
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        bool shouldShow =
            cameraMovement.IsInPOI &&
            !dialogueActive &&
            !ScreenFade.Instance.IsFading;

        canvasGroup.alpha = shouldShow ? 1 : 0;
        canvasGroup.interactable = shouldShow;
        canvasGroup.blocksRaycasts = shouldShow;
    }

    void OnReturnPressed()
    {
        cameraMovement.ReturnToRail();
    }
}