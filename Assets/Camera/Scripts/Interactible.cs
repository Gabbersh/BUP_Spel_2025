using UnityEngine;
using System;
using System.Collections;

public class Interactable : MonoBehaviour
{
    [Header("Pickup Settings")]
    public Transform pickupTarget;
    public float moveSpeed = 5f;
    public float deactivateDelay = 3f;

    private bool pickedUp = false;
    private bool finished = false;
    private bool firstClickDone = false;
    private bool isDeactivating = false;

    public event Action<Interactable> OnPickedUp;

    void Update()
    {
        if (pickedUp && !finished)
        {
            if (pickupTarget == null) return;

            transform.position = Vector3.Lerp(transform.position, pickupTarget.position, Time.deltaTime * moveSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, pickupTarget.rotation, Time.deltaTime * moveSpeed);

            if (Vector3.Distance(transform.position, pickupTarget.position) < 0.01f)
            {
                finished = true;
                OnPickedUp?.Invoke(this);

                if (Inventory.Instance != null)
                    Inventory.Instance.AddItem(this);

                // Start countdown to deactivate
                if (!isDeactivating)
                    StartCoroutine(DeactivateAfterDelay());
            }
        }
    }

    public void OnInteract()
    {
        if (!firstClickDone)
        {
            // First click: pick up
            pickedUp = true;
            finished = false;
            firstClickDone = true;

            // Immediately hide the highlight
            var highlight = GetComponentInChildren<HighlightDeactivator>();
            if (highlight != null)
                highlight.DeactivateHighlight();
        }
    }

    private IEnumerator DeactivateAfterDelay()
    {
        isDeactivating = true;
        yield return new WaitForSeconds(deactivateDelay);

        // mark so other systems know this object has been picked up
        var marker = GetComponent<PickedUpMarker>();
        if (marker == null) marker = gameObject.AddComponent<PickedUpMarker>();
        marker.pickedUp = true;

        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        gameObject.SetActive(false);
    }
}
