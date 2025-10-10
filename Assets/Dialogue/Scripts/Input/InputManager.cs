using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Simplified input manager that handles both touch and mouse input.
/// Checks if input is over UI to prevent accidental world interactions.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // Events for decoupling
    public event System.Action OnInteract;
    public event System.Action OnSubmit;

    // State tracking
    private bool interactConsumed = false;
    private bool interactThisFrame = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("[InputManager] Multiple instances found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        // ALWAYS detect input, let subscribers decide what to do
        DetectInput();
    }

    private void LateUpdate()
    {
        // Reset flags at end of frame
        interactThisFrame = false;
        interactConsumed = false;
    }

    /// <summary>
    /// Detect touch or mouse input, ignoring UI elements.
    /// Now ALWAYS detects input - subscribers decide how to handle it.
    /// </summary>
    private void DetectInput()
    {
        bool inputDetected = false;

        // Touch input (mobile)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                inputDetected = !IsPointerOverUI(touch.fingerId);
            }
        }
        // Mouse input (editor/desktop)
        else if (Input.GetMouseButtonDown(0))
        {
            inputDetected = !IsPointerOverUI();
        }

        if (inputDetected)
        {
            interactThisFrame = true;
            OnInteract?.Invoke();
        }
    }

    /// <summary>
    /// Check if pointer is over UI element.
    /// </summary>
    private bool IsPointerOverUI(int touchId = -1)
    {
        if (EventSystem.current == null)
            return false;

        if (touchId >= 0)
            return EventSystem.current.IsPointerOverGameObject(touchId);
        else
            return EventSystem.current.IsPointerOverGameObject();
    }

    // ==================== PUBLIC API ====================

    /// <summary>
    /// Check if interact was pressed this frame. Consumes the input.
    /// </summary>
    public bool GetInteractPressed()
    {
        if (interactThisFrame && !interactConsumed)
        {
            interactConsumed = true;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Check if interact was pressed this frame without consuming it.
    /// </summary>
    public bool PeekInteractPressed()
    {
        return interactThisFrame && !interactConsumed;
    }

    /// <summary>
    /// Trigger submit event (for UI buttons to invoke).
    /// </summary>
    public void TriggerSubmit()
    {
        OnSubmit?.Invoke();
    }
}