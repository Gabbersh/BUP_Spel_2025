using UnityEngine;

public class MovementButton : MonoBehaviour
{
    [Header("References")]
    public CameraMovement cameraMovement;

    [Header("UI Buttons")]
    public GameObject leftButtonObject;
    public GameObject rightButtonObject;

    [Header("Movement Settings")]
    public float buttonSpeed = 1f;
    [Tooltip("How close to the end (0..1) before hiding the button")]
    public float hideThreshold = 0.01f;

    private int direction = 0;
    private bool buttonsEnabled = false;

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

    void Start()
    {
        SetButtonsActive(false);

        if (cameraMovement != null && cameraMovement.IsIdleOnRail)
            ShowButtons();
    }

    void Update()
    {
        if (cameraMovement == null) return;

        // Apply movement input
        cameraMovement.SetExternalInput(buttonsEnabled ? direction * buttonSpeed : 0f);

        if (!buttonsEnabled) return;

        // Update button visibility based on rail position
        float pos = cameraMovement.RailPosition01;
        SetButtonVisibility(leftButtonObject, pos > hideThreshold);
        SetButtonVisibility(rightButtonObject, pos < 1f - hideThreshold);
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

    private void ShowButtons()
    {
        buttonsEnabled = true;
        SetButtonsActive(true);
    }

    private void HideButtons()
    {
        buttonsEnabled = false;
        SetButtonsActive(false);
    }

    private void SetButtonsActive(bool state)
    {
        SetButtonVisibility(leftButtonObject, state);
        SetButtonVisibility(rightButtonObject, state);
    }

    // --- UI Button Callbacks ---
    public void StartMoveLeft() => direction = -1;
    public void StartMoveRight() => direction = 1;
    public void StopMove() => direction = 0;
}
