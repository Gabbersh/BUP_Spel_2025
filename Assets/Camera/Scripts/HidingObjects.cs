using UnityEngine;

public class HidingObjects : MonoBehaviour
{
    [SerializeField] private GameObject[] objectsToToggle;
    [SerializeField] private CameraMovement cameraMovement; // assign in inspector

    void Start()
    {
        // Subscribe to camera events
        cameraMovement.OnReachedPOI += HideObjects;      // hide on POI
        cameraMovement.OnLeftPOI += ShowObjects;        // show when leaving POI
        cameraMovement.OnReturnedToRail += ShowObjects; // show when back on rail
        cameraMovement.OnLeftRail += HideObjects;       // hide immediately when leaving rail
    }

    private void ShowObjects()
    {
        foreach (var obj in objectsToToggle)
        {
            if (obj != null) obj.SetActive(true);
        }
    }

    private void HideObjects()
    {
        foreach (var obj in objectsToToggle)
        {
            if (obj != null) obj.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Always unsubscribe when destroyed
        cameraMovement.OnReachedPOI -= HideObjects;
        cameraMovement.OnLeftPOI -= ShowObjects;
        cameraMovement.OnReturnedToRail -= ShowObjects;
    }
}
