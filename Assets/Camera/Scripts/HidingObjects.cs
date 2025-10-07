using UnityEngine;

public class HidingObjects : MonoBehaviour
{
    [Header("Object Groups")]
    [SerializeField] private GameObject[] objectsToToggle;
    [SerializeField] private GameObject[] inverseObjects;

    [Header("References")]
    [SerializeField] private CameraMovement cameraMovement;

    [Header("Input")]
    [SerializeField] private KeyCode activateKey = KeyCode.B;

    private void Start()
    {
        if (cameraMovement == null)
        {
            Debug.LogWarning($"{nameof(HidingObjects)} on {gameObject.name} has no CameraMovement assigned.");
            return;
        }

        SubscribeToCameraEvents();
        InitializeVisibility();
    }

    private void Update()
    {
        if (Input.GetKeyDown(activateKey))
        {
            ActivateAllIgnoredObjects();
            // Refresh visibility right after activating them
            ShowObjects();
        }
    }

    private void OnDestroy()
    {
        if (cameraMovement == null) return;
        UnsubscribeFromCameraEvents();
    }

    private void SubscribeToCameraEvents()
    {
        cameraMovement.OnReachedPOI += HideObjects;
        cameraMovement.OnLeftPOI += ShowObjects;
        cameraMovement.OnReturnedToRail += ShowObjects;
        cameraMovement.OnLeftRail += HideObjects;
    }

    private void UnsubscribeFromCameraEvents()
    {
        cameraMovement.OnReachedPOI -= HideObjects;
        cameraMovement.OnLeftPOI -= ShowObjects;
        cameraMovement.OnReturnedToRail -= ShowObjects;
        cameraMovement.OnLeftRail -= HideObjects;
    }

    private bool CanToggle(GameObject obj)
    {
        if (obj == null) return false;

        var pickedMarker = obj.GetComponent<PickedUpMarker>();
        if (pickedMarker != null && pickedMarker.pickedUp)
            return false;

        var ignore = obj.GetComponent<IgnoreUntilActivated>();
        if (ignore != null && !ignore.isActive)
            return false;

        return true;
    }

    private void InitializeVisibility()
    {
        foreach (var obj in objectsToToggle)
        {
            if (obj == null) continue;

            var ignore = obj.GetComponent<IgnoreUntilActivated>();
            if (ignore != null && !ignore.isActive)
            {
                obj.SetActive(false);
                continue;
            }

            obj.SetActive(true);
        }

        foreach (var obj in inverseObjects)
        {
            if (obj == null) continue;

            var ignore = obj.GetComponent<IgnoreUntilActivated>();
            if (ignore != null && !ignore.isActive)
            {
                obj.SetActive(false);
                continue;
            }

            obj.SetActive(false);
        }
    }

    private void ShowObjects()
    {
        SetActiveState(objectsToToggle, true);
        SetActiveState(inverseObjects, false);
    }

    private void HideObjects()
    {
        SetActiveState(objectsToToggle, false);
        SetActiveState(inverseObjects, true);
    }

    private void SetActiveState(GameObject[] objects, bool active)
    {
        foreach (var obj in objects)
        {
            if (CanToggle(obj))
                obj.SetActive(active);
        }
    }

    private void ActivateAllIgnoredObjects()
    {
        // Reactivate all objects that were ignored before
        foreach (var obj in objectsToToggle)
        {
            var ignore = obj?.GetComponent<IgnoreUntilActivated>();
            if (ignore != null && !ignore.isActive)
                ignore.isActive = true;
        }

        foreach (var obj in inverseObjects)
        {
            var ignore = obj?.GetComponent<IgnoreUntilActivated>();
            if (ignore != null && !ignore.isActive)
                ignore.isActive = true;
        }

        Debug.Log("All previously ignored objects are now active in logic!");
    }
}
