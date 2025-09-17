using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Rail Settings")]
    public Transform railStart;
    public Transform railEnd;

    [Header("Movement Settings")]
    public float moveSpeed = 0.5f; // drag/swipe sensitivity
    public float fixedY = 5f;

    [Header("Momentum Settings")]
    public float deceleration = 5f;

    [Header("POI Override")]
    public float overrideSpeed = 1f;
    private bool overrideActive = false;
    private Vector3 overrideTargetPos;
    private Quaternion overrideTargetRot;

    [Header("Rail Return")]
    private bool returningToRail = false;
    private Vector3 returnTargetPos;
    private Quaternion returnTargetRot;

    private float t = 0.5f; // position along rail [0,1]
    private float velocity = 0f;
    private bool dragging = false;
    private Vector2 lastInputPos;
    private Quaternion railOriginalRot;

    void Start()
    {
        railOriginalRot = transform.rotation;
    }

    void Update()
    {
        if (railStart == null || railEnd == null) return;

        Vector2 inputDelta = Vector2.zero;

        // ----- Mouse Input (PC) -----
        if (Input.GetMouseButtonDown(0)) StartDragging(Input.mousePosition);
        if (Input.GetMouseButton(0))
        {
            inputDelta = (Vector2)Input.mousePosition - lastInputPos;
            lastInputPos = Input.mousePosition;
            dragging = true;
        }
        if (Input.GetMouseButtonUp(0)) dragging = false;

        // ----- Touch Input (Mobile) -----
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    StartDragging(touch.position);
                    break;
                case TouchPhase.Moved:
                    inputDelta = touch.deltaPosition;
                    dragging = true;
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    dragging = false;
                    break;
            }
        }

        if (!overrideActive && !returningToRail)
            HandleRailMovement(inputDelta);

        ApplyCameraPosition();
    }

    private void HandleRailMovement(Vector2 inputDelta)
    {
        if (dragging)
        {
            float deltaT = -inputDelta.x / Screen.width * moveSpeed;
            t += deltaT;
            velocity = deltaT / Time.deltaTime;
        }
        else if (Mathf.Abs(velocity) > 0.001f)
        {
            t += velocity * Time.deltaTime;
            velocity = Mathf.MoveTowards(velocity, 0f, deceleration * Time.deltaTime);
        }

        t = Mathf.Clamp01(t);
    }

    private void ApplyCameraPosition()
    {
        if (overrideActive || returningToRail)
            MoveTowardsTarget();
        else
            MoveAlongRail();
    }

    private void MoveTowardsTarget()
    {
        Vector3 targetPos = overrideActive ? overrideTargetPos : returnTargetPos;
        Quaternion targetRot = overrideActive ? overrideTargetRot : returnTargetRot;

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * overrideSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * overrideSpeed);

        if (Vector3.Distance(transform.position, targetPos) < 0.01f)
        {
            transform.position = targetPos;
            transform.rotation = targetRot;

            if (overrideActive)
                velocity = 0f;
            else
                returningToRail = false;
        }
    }

    private void MoveAlongRail()
    {
        Vector3 railPos = Vector3.Lerp(railStart.position, railEnd.position, t);
        railPos.y = fixedY;
        transform.position = railPos;
        transform.rotation = railOriginalRot;
    }

    private void StartDragging(Vector2 inputPos)
    {
        lastInputPos = inputPos;
        dragging = true;
        velocity = 0f;
    }

    // ----- POI Interaction -----
    public void MoveToPOI(Vector3 targetPos, Quaternion targetRot)
    {
        if (overrideActive)
        {
            // Start returning to rail using the same overrideSpeed
            overrideActive = false;
            returningToRail = true;
        }
        else
        {
            // Save current position as return target
            returnTargetPos = transform.position;
            returnTargetRot = transform.rotation;

            overrideTargetPos = targetPos;
            overrideTargetRot = targetRot;
            overrideActive = true;
        } 
    }

    // ----- PUBLIC METHODS FOR OTHER SCRIPTS -----
    public bool IsOverridingToPOI(Vector3 targetPos)
    {
        return overrideActive && overrideTargetPos == targetPos;
    }
}
