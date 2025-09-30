using UnityEngine;
using UnityEngine.UI;

public class ReturnButtonController : MonoBehaviour
{
    public CameraMovement cameraMovement; 
    public Button returnButton;           

    void Start()
    {
        // Hide button at start
        returnButton.gameObject.SetActive(false);

        // Register click event
        returnButton.onClick.AddListener(OnReturnPressed);
    }

    void Update()
    {
        // Only show button when camera is in a POI (override active)
        if (cameraMovement.IsInPOI) 
        {
            if (!returnButton.gameObject.activeSelf)
                returnButton.gameObject.SetActive(true);
        }
        else
        {
            if (returnButton.gameObject.activeSelf)
                returnButton.gameObject.SetActive(false);
        }
    }

    void OnReturnPressed()
    {
        if (DialogueManager.GetInstance() != null && DialogueManager.GetInstance().dialogueIsPlaying)
        {
            DialogueManager.GetInstance().ForceExitDialogue();
        }

        cameraMovement.ReturnToRail();
    }
}
