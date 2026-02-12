using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[DisallowMultipleComponent]
public class CloudFollowSpline : MonoBehaviour
{
    [Header("Spline")]
    [SerializeField] private SplineContainer spline;
    [SerializeField] private int splineIndex = 0;

    [Header("Movement")]
    [Tooltip("Units per second if Use World Distance = true. If false, 'speed' is normalized t per second.")]
    [SerializeField] private float speed = 4f;

    [Tooltip("Random multiplier applied to speed (each cloud gets its own).")]
    [SerializeField] private Vector2 speedRandomMultiplier = new Vector2(0.7f, 1.3f);

    [Tooltip("If true, movement uses world distance (meters). If false, uses normalized t (0..1).")]
    [SerializeField] private bool useWorldDistance = true;

    [Tooltip("If true, ignore Start Offset and pick a random start position along the spline.")]
    [SerializeField] private bool randomizeStartAlongSpline = true;

    [Tooltip("Used only if Randomize Start Along Spline = false. (meters if world distance; 0..1 if normalized).")]
    [SerializeField] private float startOffset = 0f;

    [Header("Per-cloud offsets (constant) - bigger defaults")]
    [SerializeField] private Vector2 upRange = new Vector2(-8f, 8f);       // Y
    [SerializeField] private Vector2 depthRange = new Vector2(-15f, 15f);  // depth in spline frame
    [SerializeField] private Vector2 sideRange = new Vector2(-10f, 10f);   // X

    [Header("Wobble (optional)")]
    [SerializeField] private float wobbleAmplitude = 1.2f;
    [SerializeField] private float wobbleFrequency = 0.15f;

    [Header("Look")]
    [SerializeField] private bool alignToSplineTangent = false;

    private float _tOrDistance;
    private Vector3 _constantOffset;
    private float _seed;
    private float _speedLocal;

    private void Reset()
    {
        if (spline == null) spline = FindFirstObjectByType<SplineContainer>();
    }

    private void Awake()
    {
        if (spline == null) spline = FindFirstObjectByType<SplineContainer>();
        if (spline == null) return;

        // Avoid UnityEngine.Random vs Unity.Mathematics.Random ambiguity:
        _seed = UnityEngine.Random.value * 1000f;

        // Bigger spacing offset per cloud
        _constantOffset = new Vector3(
            UnityEngine.Random.Range(sideRange.x, sideRange.y),
            UnityEngine.Random.Range(upRange.x, upRange.y),
            UnityEngine.Random.Range(depthRange.x, depthRange.y)
        );

        // Per-cloud speed variance (prevents sync)
        _speedLocal = speed * UnityEngine.Random.Range(speedRandomMultiplier.x, speedRandomMultiplier.y);

        // Randomize start position so clouds don't stack
        if (randomizeStartAlongSpline)
        {
            if (useWorldDistance)
            {
                float length = spline.CalculateLength(splineIndex);
                _tOrDistance = (length > 0.001f) ? UnityEngine.Random.Range(0f, length) : 0f;
            }
            else
            {
                _tOrDistance = UnityEngine.Random.Range(0f, 1f);
            }
        }
        else
        {
            _tOrDistance = startOffset;
        }

        // Place immediately on Awake so they don't "pop" at t=0 then jump
        ForceUpdatePosition();
    }

    private void Update()
    {
        if (spline == null) return;

        if (useWorldDistance)
        {
            float length = spline.CalculateLength(splineIndex);
            if (length <= 0.001f) return;

            _tOrDistance += _speedLocal * Time.deltaTime;

            float d = Mathf.Repeat(_tOrDistance, length);
            float t = d / length;

            MoveToT(t);
        }
        else
        {
            _tOrDistance += _speedLocal * Time.deltaTime;
            float t = Mathf.Repeat(_tOrDistance, 1f);

            MoveToT(t);
        }
    }

    private void ForceUpdatePosition()
    {
        if (spline == null) return;

        if (useWorldDistance)
        {
            float length = spline.CalculateLength(splineIndex);
            if (length <= 0.001f) return;

            float d = Mathf.Repeat(_tOrDistance, length);
            float t = d / length;
            MoveToT(t);
        }
        else
        {
            float t = Mathf.Repeat(_tOrDistance, 1f);
            MoveToT(t);
        }
    }

    private void MoveToT(float t)
    {
        // Unity Splines Evaluate returns float3
        float3 posF, tangentF, upF;
        spline.Evaluate(splineIndex, t, out posF, out tangentF, out upF);

        Vector3 pos = posF;
        Vector3 tangent = tangentF;
        Vector3 up = upF;

        // Build stable frame
        Vector3 forward = (tangent.sqrMagnitude > 0.0001f) ? tangent.normalized : Vector3.forward;
        Vector3 right = Vector3.Cross(up, forward).normalized;

        // Guard against degenerate cross products
        if (right.sqrMagnitude < 0.0001f)
            right = Vector3.Cross(Vector3.up, forward).normalized;

        Vector3 binormal = Vector3.Cross(forward, right).normalized;

        // Constant offset (side/up/depth) in spline frame
        Vector3 offset =
            right * _constantOffset.x +
            up * _constantOffset.y +
            binormal * _constantOffset.z;

        // Gentle noise wobble
        if (wobbleAmplitude > 0f && wobbleFrequency > 0f)
        {
            float w = (Time.time + _seed) * wobbleFrequency;

            float wobbleY = (Mathf.PerlinNoise(w, 0.123f) - 0.5f) * 2f;
            float wobbleX = (Mathf.PerlinNoise(0.456f, w) - 0.5f) * 2f;
            float wobbleZ = (Mathf.PerlinNoise(w, 0.789f) - 0.5f) * 2f;

            offset += up * (wobbleY * wobbleAmplitude);
            offset += right * (wobbleX * wobbleAmplitude * 0.5f);
            offset += binormal * (wobbleZ * wobbleAmplitude * 0.35f);
        }

        transform.position = pos + offset;

        if (alignToSplineTangent)
            transform.rotation = Quaternion.LookRotation(forward, up);
    }
}
