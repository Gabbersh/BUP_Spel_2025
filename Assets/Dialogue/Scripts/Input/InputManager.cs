using UnityEngine;

public class InputManager : MonoBehaviour
{
    private bool interactPressed = false;
    private bool submitPressed = false;

    private static InputManager instance;

    private void Awake()
    {
        if (isntance != null)
            Debug.LogError("Found more than one Input Manager in the scene.");

        instance = this;
    }

    public static InputManager GetInstance()
    {
        return instance;
    }
}
