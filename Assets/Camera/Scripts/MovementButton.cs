using UnityEngine;

public class CameraButtonController : MonoBehaviour
{
    public CameraMovement cameraMovement;
    public float buttonSpeed = 1f;

    private int direction = 0;

    void Update()
    {
        cameraMovement.SetExternalInput(direction);
    }

    public void StartMoveLeft() => direction = -1;
    public void StartMoveRight() => direction = 1;
    public void StopMove() => direction = 0;
}
