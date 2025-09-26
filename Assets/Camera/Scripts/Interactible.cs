using UnityEngine;

public class Interactable : MonoBehaviour
{
    public Transform pickupTarget; // assign a point near the camera where it moves to
    public float moveSpeed = 5f;  // speed when moving to camera
    public bool pickedUp = false;

    private Vector3 startPos;
    private Quaternion startRot;

    void Start()
    {
        startPos = transform.position;
        startRot = transform.rotation;
    }

    void Update()
    {
        if (pickedUp)
        {
            // Smoothly move to pickup target
            transform.position = Vector3.Lerp(transform.position, pickupTarget.position, Time.deltaTime * moveSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, pickupTarget.rotation, Time.deltaTime * moveSpeed);

            // Optional: if close enough, finalize pickup
            if (Vector3.Distance(transform.position, pickupTarget.position) < 0.01f)
            {
                // You could disable or store the object here
                //gameObject.SetActive(false);
                Inventory.Instance.AddItem(this); // example inventory call
            }
        }
    }

    public void OnInteract()
    {
        pickedUp = true;
    }
}
