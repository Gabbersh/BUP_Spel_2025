using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Rail Settings")]
    public Transform railStart;
    public Transform railEnd;

    [Header("Movement Settings")]
    public float buttonSpeed = 1f; // constant speed while holding button
    public float fixedY = 5f;

    [Header("Momentum Settings")]
    public float deceleration = 3f; // velocity decay after release

    [Header("POI Override")]
    public float overrideSpeed = 1f;
    public bool IsInPOI => overrideActive;
    private bool overrideActive = false;
    private Vector3 overrideTargetPos;
    private Quaternion overrideTargetRot;

    // Events
    public event System.Action OnLeftPOI;
    public event System.Action OnReturnedToRail;
    public event System.Action OnReachedPOI;
    public event System.Action OnLeftRail;

    private bool isTransitioning = false;
    public bool HasReachedPOI { get; private set; } = false;

    public bool IsIdleOnRail => !isTransitioning && !overrideActive && !returningToRail && Mathf.Abs(velocity) < 0.001f;

    private bool returningToRail = false;
    private Vector3 returnTargetPos;
    private Quaternion returnTargetRot;

    private float t = 0.5f; // position along rail [0,1]
    private float velocity = 0f; // current velocity along rail
    private float externalInput = 0f; // -1 = left, 1 = right

    private Quaternion railOriginalRot;

    void Start()
    {
        railOriginalRot = transform.rotation;
    }

    void Update()
    {
        if (railStart == null || railEnd == null) return;

        HandleRailMovement();
        ApplyCameraPosition();
    }

    private void HandleRailMovement()
    {
        if (!overrideActive && !returningToRail)
        {
            if (Mathf.Abs(externalInput) > 0.001f)
            {
                // Constant velocity while button is held
                velocity = externalInput * buttonSpeed;
            }
            else
            {
                // Decelerate velocity after button release
                velocity = Mathf.MoveTowards(velocity, 0f, deceleration * Time.deltaTime);
            }

            t += velocity * Time.deltaTime;
            t = Mathf.Clamp01(t);
        }
    }

    private void ApplyCameraPosition()
    {
        if (overrideActive || returningToRail)
            MoveTowardsTarget();
        else
            MoveAlongRail();
    }

    private void MoveAlongRail()
    {
        Vector3 railPos = Vector3.Lerp(railStart.position, railEnd.position, t);
        railPos.y = fixedY;
        transform.position = railPos;
        transform.rotation = railOriginalRot;
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
            {
                velocity = 0f;
                HasReachedPOI = true;
                OnReachedPOI?.Invoke();
            }
            else
            {
                returningToRail = false;
                HasReachedPOI = false;
                OnReturnedToRail?.Invoke();
            }

            isTransitioning = false;
        }
    }

    // ----- POI Interaction -----
    public void MoveToPOI(Vector3 targetPos, Quaternion targetRot)
    {
        if (isTransitioning || !IsIdleOnRail) return;

        OnLeftRail?.Invoke();

        returnTargetPos = transform.position;
        returnTargetRot = transform.rotation;

        overrideTargetPos = targetPos;
        overrideTargetRot = targetRot;
        overrideActive = true;
        returningToRail = false;
        isTransitioning = true;
        HasReachedPOI = false;
    }

    public void ReturnToRail()
    {
        if (isTransitioning) return;

        overrideActive = false;
        returningToRail = true;
        isTransitioning = true;
        HasReachedPOI = false;

        OnLeftPOI?.Invoke();
    }

    // ----- External Input for buttons -----
    public void SetExternalInput(float input) => externalInput = input;

    public void AddDeltaT(float delta)
    {
        t += delta;
        velocity = delta / Time.deltaTime;
        t = Mathf.Clamp01(t);
    }
}
