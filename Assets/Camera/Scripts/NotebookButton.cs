using UnityEngine;
using UnityEngine.UI;

public class NotebookButton : MonoBehaviour
{
    [Header("References")]
    public CameraMovement cameraMovement;
    public GameObject handbook;
    public GameObject blockClick;
    public GameObject POI;

    [Header("UI Button")]
    public GameObject notebookIcon;
    public Button notebookButton;

    private bool buttonEnabled = false;
    private bool notebookOpen = false;

    void Start()
    {
        handbook.SetActive(buttonEnabled);
        blockClick.SetActive(buttonEnabled);
        notebookButton.onClick.AddListener(OnNoteBookPressed);
    }

    void OnNoteBookPressed()
    {
        if (handbook.GetComponent<AutoFlip>().isFlipping)
            return;

        notebookOpen = !notebookOpen;
        handbook.SetActive(notebookOpen);
        blockClick.SetActive(notebookOpen);
        POI.SetActive(!notebookOpen);
    }

    void OnEnable()
    {
        if (cameraMovement == null) return;

        cameraMovement.OnLeftRail += HideButtons;
        cameraMovement.OnReachedPOI += HideButtons;
        cameraMovement.OnReturnedToRail += ShowButtons;
    }
    void OnDisable()
    {
        if (cameraMovement == null) return;

        cameraMovement.OnLeftRail -= HideButtons;
        cameraMovement.OnReachedPOI -= HideButtons;
        cameraMovement.OnReturnedToRail -= ShowButtons;
    }

    private void ShowButtons()
    {
        buttonEnabled = true;
        SetButtonsActive(buttonEnabled);
    }

    private void HideButtons()
    {
        buttonEnabled = false;
        SetButtonsActive(buttonEnabled);
    }
    private void SetButtonsActive(bool state)
    {
        SetButtonVisibility(notebookIcon, state);
    }

    private void SetButtonVisibility(GameObject button, bool visible)
    {
        if (button == null) return;

        CanvasGroup cg = button.GetComponent<CanvasGroup>();
        if (cg == null) cg = button.AddComponent<CanvasGroup>();

        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }
}
