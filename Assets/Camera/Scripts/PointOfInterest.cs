using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PointOfInterest : MonoBehaviour
{
    public Transform cameraTarget;   // assign in inspector

    public CameraMovement targetCamera; // assign your camera here

    [Header("Character Placement")]
    public Transform characterPosition;

    private void OnMouseDown()
    {
        if (cameraTarget == null)
        {
            Debug.LogWarning($"POI {gameObject.name} has no cameraTarget assigned.");
            return;
        }

        if (targetCamera == null)
        {
            Debug.LogWarning("No CameraMovement assigned on POI.");
            return;
        }

        // Move the camera
        targetCamera.MoveToPOI(cameraTarget.position, cameraTarget.rotation);
    }

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = false;
    }
}