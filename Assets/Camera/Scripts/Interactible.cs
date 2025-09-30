using UnityEngine;
using System;

public class Interactable : MonoBehaviour
{
    [Header("Pickup Settings")]
    public Transform pickupTarget;    // point near camera to move to
    public float moveSpeed = 5f;      // movement speed to target

    private bool pickedUp = false;    // object is moving to pickupTarget
    private bool finished = false;    // has finished moving
    private bool firstClickDone = false; // tracks first vs second click

    public event Action<Interactable> OnPickedUp; // optional event

    void Update()
    {
        if (pickedUp && !finished)
        {
            if (pickupTarget == null) return;

            // Smoothly move to pickup target
            transform.position = Vector3.Lerp(transform.position, pickupTarget.position, Time.deltaTime * moveSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, pickupTarget.rotation, Time.deltaTime * moveSpeed);

            // Check if close enough
            if (Vector3.Distance(transform.position, pickupTarget.position) < 0.01f)
            {
                finished = true; // stop further movement
                OnPickedUp?.Invoke(this);

                if (Inventory.Instance != null)
                    Inventory.Instance.AddItem(this);
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
        }
        else
        {
            // Second click: deactivate object
            gameObject.SetActive(false);
        }
    }
}
