using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using System.Collections;

public class WorldMoodController : MonoBehaviour
{
    [Header("Power Reference")]
    [SerializeField] private MayorsPower mayorsPower;

    [Header("Lighting")]
    [SerializeField] private Light mainLight;
    [SerializeField] private Color lightHappy = Color.white;
    [SerializeField] private Color lightDark = new Color(0.5f, 0.5f, 0.6f);
    [SerializeField] private float happyIntensity = 1.2f;
    [SerializeField] private float darkIntensity = 0.8f;

    [Header("Ambient Light")]
    [SerializeField] private Color ambientHappy = new Color(0.8f, 0.8f, 0.8f);
    [SerializeField] private Color ambientDark = new Color(0.2f, 0.2f, 0.25f);

    [Header("Post Processing")]
    [SerializeField] private PostProcessVolume postProcessVolume;
    private ColorGrading colorGrading;

    [Header("Transition Settings")]
    [SerializeField] private float transitionDuration = 1f;

    // Saturation and contrast ranges
    private const float minSaturation = -90f;
    private const float maxSaturation = 10f;
    private const float minContrast = 20f;
    private const float maxContrast = 0f;

    [Header("Power Mapping")]
    [SerializeField, Range(0f, 1f)] private float minVisualPower = 0.2f; // maps "full happiness" to 20% power

    private Coroutine moodRoutine;

    private void Start()
    {
        if (postProcessVolume != null && postProcessVolume.profile != null)
            postProcessVolume.profile.TryGetSettings(out colorGrading);

        // Initialize mood instantly
        ApplyMood(MapPower(mayorsPower.GetPowerPercentage()));
    }

    private void OnEnable()
    {
        MayorsPower.OnMayorPowerChanged += OnPowerChanged;
    }

    private void OnDisable()
    {
        MayorsPower.OnMayorPowerChanged -= OnPowerChanged;
    }

    private void OnPowerChanged(float powerPercent)
    {
        if (moodRoutine != null)
            StopCoroutine(moodRoutine);

        // Remap power so visuals reach full happiness at minVisualPower
        moodRoutine = StartCoroutine(SmoothMoodTransition(MapPower(powerPercent)));
    }

    // Maps actual power (0–1) to visual range so happiness maxes out at minVisualPower
    private float MapPower(float actualPower)
    {
        return Mathf.InverseLerp(minVisualPower, 1f, actualPower);
    }

    private IEnumerator SmoothMoodTransition(float targetPower)
    {
        Color startLightColor = mainLight.color;
        float startLightIntensity = mainLight.intensity;
        Color startAmbient = RenderSettings.ambientLight;
        float startSaturation = colorGrading != null ? colorGrading.saturation.value : 0f;
        float startContrast = colorGrading != null ? colorGrading.contrast.value : 0f;

        Color targetLightColor = Color.Lerp(lightHappy, lightDark, targetPower);
        float targetLightIntensity = Mathf.Lerp(happyIntensity, darkIntensity, targetPower);
        Color targetAmbient = Color.Lerp(ambientHappy, ambientDark, targetPower);
        float targetSaturation = Mathf.Lerp(minSaturation, maxSaturation, 1f - targetPower);
        float targetContrast = Mathf.Lerp(minContrast, maxContrast, 1f - targetPower);

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);

            mainLight.color = Color.Lerp(startLightColor, targetLightColor, t);
            mainLight.intensity = Mathf.Lerp(startLightIntensity, targetLightIntensity, t);
            RenderSettings.ambientLight = Color.Lerp(startAmbient, targetAmbient, t);

            if (colorGrading != null)
            {
                colorGrading.saturation.value = Mathf.Lerp(startSaturation, targetSaturation, t);
                colorGrading.contrast.value = Mathf.Lerp(startContrast, targetContrast, t);
            }

            yield return null;
        }

        // Final values
        mainLight.color = targetLightColor;
        mainLight.intensity = targetLightIntensity;
        RenderSettings.ambientLight = targetAmbient;

        if (colorGrading != null)
        {
            colorGrading.saturation.value = targetSaturation;
            colorGrading.contrast.value = targetContrast;
        }
    }

    private void ApplyMood(float powerPercent)
    {
        mainLight.color = Color.Lerp(lightHappy, lightDark, powerPercent);
        mainLight.intensity = Mathf.Lerp(happyIntensity, darkIntensity, powerPercent);
        RenderSettings.ambientLight = Color.Lerp(ambientHappy, ambientDark, powerPercent);

        if (colorGrading != null)
        {
            colorGrading.saturation.value = Mathf.Lerp(minSaturation, maxSaturation, 1f - powerPercent);
            colorGrading.contrast.value = Mathf.Lerp(minContrast, maxContrast, 1f - powerPercent);
        }
    }
}
