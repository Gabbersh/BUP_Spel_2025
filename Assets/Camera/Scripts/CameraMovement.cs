using System.Collections;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Rail Settings")]
    public Transform railStart;
    public Transform railEnd;

    [Header("Movement Settings")]
    public float buttonSpeed = 1f;
    public float fixedY = 5f;

    [Header("Momentum Settings")]
    public float deceleration = 3f;

    [Header("POI Override")]
    public float overrideSpeed = 1f;
    public bool IsInPOI => overrideActive;
    private bool overrideActive = false;
    private Vector3 overrideTargetPos;
    private Quaternion overrideTargetRot;

    [Header("Intro Settings")]
    public bool playIntroAtStart = true;
    public Transform introStartPoint;
    public Transform introTargetPoint;
    public float introDuration = 3f;
    public AnimationCurve introCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool introPlaying = false;

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

    private float t = 0.5f;
    private float velocity = 0f;
    private float externalInput = 0f;

    public float RailPosition01 => t;

    private Quaternion railOriginalRot;

    public bool CanInteractWithPOIs =>
        !isTransitioning && !returningToRail && !introPlaying;

    void Start()
    {
        railOriginalRot = transform.rotation;

        if (playIntroAtStart && introStartPoint != null && introTargetPoint != null)
        {
            transform.position = introStartPoint.position;
            transform.rotation = introStartPoint.rotation;
            StartCoroutine(PlayIntroSequence());
        }
    }

    void Update()
    {
        if (railStart == null || railEnd == null || introPlaying) return;

        HandleRailMovement();
        ApplyCameraPosition();
    }

    private void HandleRailMovement()
    {
        if (!overrideActive && !returningToRail)
        {
            if (Mathf.Abs(externalInput) > 0.001f)
                velocity = externalInput * buttonSpeed;
            else
                velocity = Mathf.MoveTowards(velocity, 0f, deceleration * Time.deltaTime);

            t = Mathf.Clamp01(t + velocity * Time.deltaTime);
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

    // ---- POI MOVEMENT ----
    public void MoveToPOI(Vector3 targetPos, Quaternion targetRot)
    {
        if (returningToRail || isTransitioning)
            return;

        if (overrideActive)
        {
            HasReachedPOI = false;
            OnLeftPOI?.Invoke();
        }

        OnLeftRail?.Invoke();

        isTransitioning = true;
        overrideActive = true;
        returningToRail = false;

        overrideTargetPos = targetPos;
        overrideTargetRot = targetRot;
    }

    public void ReturnToRail()
    {
        if (isTransitioning) return;

        t = GetClosestTOnRail(transform.position);

        Vector3 railPos = Vector3.Lerp(railStart.position, railEnd.position, t);
        railPos.y = fixedY;

        returnTargetPos = railPos;
        returnTargetRot = railOriginalRot;

        overrideActive = false;
        returningToRail = true;
        isTransitioning = true;
        HasReachedPOI = false;

        OnLeftPOI?.Invoke();
    }

    public void SetExternalInput(float input)
    {
        externalInput = introPlaying ? 0f : input;
    }

    public void AddDeltaT(float delta)
    {
        t = Mathf.Clamp01(t + delta);
        velocity = delta / Time.deltaTime;
    }

    private IEnumerator PlayIntroSequence()
    {
        introPlaying = true;
        OnLeftRail?.Invoke();
        float time = 0f;

        Vector3 startPos = introStartPoint.position;
        Quaternion startRot = introStartPoint.rotation;
        Vector3 endPos = introTargetPoint.position;
        Quaternion endRot = introTargetPoint.rotation;

        while (time < introDuration)
        {
            float curveValue = introCurve.Evaluate(time / introDuration);
            transform.position = Vector3.Lerp(startPos, endPos, curveValue);
            transform.rotation = Quaternion.Slerp(startRot, endRot, curveValue);
            time += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;

        introPlaying = false;
        overrideActive = true;
        overrideTargetPos = endPos;
        overrideTargetRot = endRot;
        HasReachedPOI = true;
        OnReachedPOI?.Invoke();

        t = GetClosestTOnRail(endPos);
    }

    private float GetClosestTOnRail(Vector3 position)
    {
        Vector3 a = railStart.position;
        Vector3 b = railEnd.position;
        Vector3 ab = b - a;
        Vector3 ap = position - a;
        return Mathf.Clamp01(Vector3.Dot(ap, ab) / ab.sqrMagnitude);
    }
}