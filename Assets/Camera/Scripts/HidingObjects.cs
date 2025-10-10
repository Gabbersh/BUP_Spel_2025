using UnityEngine;

public class HidingObjects : MonoBehaviour
{
    [Header("Objects to Toggle")]
    [SerializeField] private GameObject[] objectsToToggle;

    [Header("References")]
    [SerializeField] private CameraMovement cameraMovement;

    private void Start()
    {
        if (cameraMovement == null)
        {
            Debug.LogWarning($"{nameof(HidingObjects)} on {gameObject.name} has no CameraMovement assigned.");
            return;
        }

        InitializeVisibility();
        SubscribeToCameraEvents();
    }

    private void OnDestroy()
    {
        if (cameraMovement != null)
            UnsubscribeFromCameraEvents();
    }

    private void SubscribeToCameraEvents()
    {
        cameraMovement.OnReturnedToRail += ShowObjects;
        cameraMovement.OnLeftRail += HideObjects;
    }

    private void UnsubscribeFromCameraEvents()
    {
        cameraMovement.OnReturnedToRail -= ShowObjects;
        cameraMovement.OnLeftRail -= HideObjects;
    }

    private void InitializeVisibility()
    {
        foreach (var obj in objectsToToggle)
        {
            if (obj == null) continue;

            var ignore = obj.GetComponent<IgnoreUntilActivated>();
            if (ignore != null && !ignore.isActive)
                obj.SetActive(false);
            else
                obj.SetActive(true);
        }
    }

    private void ShowObjects() => SetActiveState(true);
    private void HideObjects() => SetActiveState(false);

    private void SetActiveState(bool active)
    {
        foreach (var obj in objectsToToggle)
        {
            if (obj == null) continue;

            var pickedMarker = obj.GetComponent<PickedUpMarker>();
            if (pickedMarker != null && pickedMarker.pickedUp)
                continue;

            obj.SetActive(active);
        }
    }
}
