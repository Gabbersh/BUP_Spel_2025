using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class HidingObjects : MonoBehaviour
{
    [Header("Objects to Toggle")]
    [SerializeField] private List<GameObject> objectsToToggle = new List<GameObject>();

    [Header("Objects Hidden Only During Intro")]
    [SerializeField] private List<GameObject> hideDuringIntro = new List<GameObject>();

    [Header("References")]
    [SerializeField] private CameraMovement cameraMovement;

    private void Start()
    {
        if (cameraMovement == null)
        {
            Debug.LogWarning($"{nameof(HidingObjects)} on {gameObject.name} has no CameraMovement assigned.");
            return;
        }


        // Hide intro-only objects immediately
        SetActiveState(hideDuringIntro, false);

        InitializeVisibility();
        SubscribeToCameraEvents();

        cameraMovement.OnReachedPOI += HandleIntroEnd;
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

    private void ShowObjects() => SetActiveState(objectsToToggle, true);
    private void HideObjects() => SetActiveState(objectsToToggle, false);

    private void SetActiveState(List<GameObject> list, bool active)
    {
        foreach (var obj in list)
        {
            if (obj == null) continue;

            var pickedMarker = obj.GetComponent<PickedUpMarker>();
            if (pickedMarker != null && pickedMarker.pickedUp)
                continue;

            obj.SetActive(active);
        }
    }

    public void RegisterObject(GameObject obj)
    {
        if (!objectsToToggle.Contains(obj))
            objectsToToggle.Add(obj);

        if (cameraMovement != null && cameraMovement.IsInPOI)
            obj.SetActive(false);
    }

    private void HandleIntroEnd()
    {
        SetActiveState(hideDuringIntro, true);
    }
}
