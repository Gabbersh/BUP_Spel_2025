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

    public float DeactivateDelay => deactivateDelay; 

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

                if (!isDeactivating)
                    StartCoroutine(DeactivateAfterDelay());
            }
        }
    }

    public void OnInteract()
    {
        if (pickedUp || finished) return; // prevent double pickup

        pickedUp = true;
        finished = false;

        var highlight = GetComponentInChildren<HighlightDeactivator>();
        if (highlight != null)
            highlight.DeactivateHighlight();
    }

    private IEnumerator DeactivateAfterDelay()
    {
        isDeactivating = true;

        yield return new WaitForSeconds(deactivateDelay);

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
