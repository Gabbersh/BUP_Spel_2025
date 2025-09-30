using UnityEngine;

public class InteractableTrigger : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main; // Cache main camera
    }

    void Update()
    {
        // ----- PC Mouse Input -----
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            CheckRaycast(ray);
        }

        // ----- Touch Input -----
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = mainCamera.ScreenPointToRay(touch.position);
                CheckRaycast(ray);
            }
        }
    }

    private void CheckRaycast(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Interactable interactable = hit.collider.GetComponent<Interactable>();
            if (interactable != null)
                interactable.OnInteract();
        }
        else
        {
            Debug.Log("Raycast hit nothing");
        }
    }
}