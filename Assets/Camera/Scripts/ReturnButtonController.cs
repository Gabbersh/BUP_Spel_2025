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
    }

    private void ShowButton()
    {
        returnButton.gameObject.SetActive(true);
    }

    private void HideButton()
    {
        returnButton.gameObject.SetActive(false);
    }

    void OnReturnPressed()
    {
        cameraMovement.ReturnToRail();
    }
}