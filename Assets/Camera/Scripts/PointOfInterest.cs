using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PointOfInterest : MonoBehaviour
{
    public Transform cameraTarget;       // Camera target position/rotation
    public CameraMovement targetCamera;  // Reference to CameraMovement

    [Header("Character Placement")]
    public Transform characterPosition;

    [SerializeField] private LayerMask interactableLayers = ~0;

    private Camera mainCam;

    private void Awake()
    {
        mainCam = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TryHandleClick(Input.mousePosition);

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            TryHandleClick(Input.GetTouch(0).position);
    }

    private void TryHandleClick(Vector3 screenPos)
    {
        Ray ray = mainCam.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, interactableLayers))
        {
            PointOfInterest poi = hit.collider.GetComponent<PointOfInterest>();
            if (poi != null)
                poi.OnPOISelected();
        }
    }

    public void OnPOISelected()
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

        // block POI if camera is transitioning or returning to rail
        if (!targetCamera.CanInteractWithPOIs)
            return;

        targetCamera.MoveToPOI(cameraTarget.position, cameraTarget.rotation);
    }

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = false;
    }
}
