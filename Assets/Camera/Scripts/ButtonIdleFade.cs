using UnityEngine;

public class ButtonIdleFade : MonoBehaviour
{
    [Header("Idle Alpha")]
    [SerializeField] private float minAlpha = 0.92f;
    [SerializeField] private float maxAlpha = 1f;

    [Header("Idle Scale")]
    [SerializeField] private float minScale = 1f;
    [SerializeField] private float maxScale = 1.04f;

    [Header("Speed")]
    [SerializeField] private float pulseSpeed = 1.5f;

    private CanvasGroup _canvasGroup;
    private Vector3 _initialScale;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _initialScale = transform.localScale;
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

        // Alpha breathing
        _canvasGroup.alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

        // Scale breathing
        float scale = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = _initialScale * scale;
    }
}