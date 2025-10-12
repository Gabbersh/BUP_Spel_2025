using UnityEngine;
using System;
using System.Collections;

public class Interactable : MonoBehaviour
{
    [Header("Pickup Settings")]
    public Transform pickupTarget;
    public float moveSpeed = 5f;

    private bool pickedUp = false;
    private bool finished = false;

    public event Action<Interactable> OnPickedUp;

    void Update()
    {
        if (!pickedUp || finished) return;
        if (pickupTarget == null) return;

        transform.position = Vector3.Lerp(transform.position, pickupTarget.position, Time.deltaTime * moveSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, pickupTarget.rotation, Time.deltaTime * moveSpeed);

        if (Vector3.Distance(transform.position, pickupTarget.position) < 0.01f)
        {
            finished = true;

            // Tell the quest system this item was picked up
            OnPickedUp?.Invoke(this);

            // Add to inventory if it exists
            Inventory.Instance?.AddItem(this);
        }
    }

    public void OnInteract()
    {
        if (pickedUp || finished) return;

        pickedUp = true;

        var highlight = GetComponentInChildren<HighlightDeactivator>();
        if (highlight != null)
            highlight.DeactivateHighlight();
    }
}