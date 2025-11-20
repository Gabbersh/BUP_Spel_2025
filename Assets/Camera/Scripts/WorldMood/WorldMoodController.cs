using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using System.Collections;
using System.Collections.Generic;

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

    [Header("Cloud Pooling")]
    [SerializeField] private GameObject cloudPrefab;
    [SerializeField] private Transform cloudSpawnArea;
    [SerializeField] private Vector3 cloudSpawnBoxSize = new Vector3(100f, 30f, 100f);
    [SerializeField] private int maxClouds = 1000;
    [SerializeField] private float spawnInterval = 5f; // update frequency

    private List<GameObject> cloudPool = new List<GameObject>();
    private int currentActiveClouds = 0;

    [Header("Car Groups (Enable based on mood)")]
    [SerializeField] private List<GameObject> carGroups = new List<GameObject>();
    [SerializeField] private AnimationCurve carActivationCurve = AnimationCurve.Linear(0, 0, 1, 1);

    private Coroutine moodRoutine;
    private Coroutine spawnRoutine;

    // -----------------------
    // --- Initialization ---
    // -----------------------
    private void Start()
    {
        if (postProcessVolume != null && postProcessVolume.profile != null)
            postProcessVolume.profile.TryGetSettings(out colorGrading);

        // Pre-create all clouds once (object pooling)
        InitializeCloudPool();

        // Initialize mood instantly
        ApplyMood(MapPower(mayorsPower.GetPowerPercentage()));

        // Start cloud updater
        spawnRoutine = StartCoroutine(SpawnController());
    }

    private void OnEnable()
    {
        MayorsPower.OnMayorPowerChanged += OnPowerChanged;
    }

    private void OnDisable()
    {
        MayorsPower.OnMayorPowerChanged -= OnPowerChanged;
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);
    }

    // -----------------------
    // --- Power Changes ---
    // -----------------------
    private void OnPowerChanged(float powerPercent)
    {
        if (moodRoutine != null)
            StopCoroutine(moodRoutine);

        moodRoutine = StartCoroutine(SmoothMoodTransition(MapPower(powerPercent)));
    }

    private float MapPower(float actualPower)
    {
        return Mathf.InverseLerp(minVisualPower, 1f, actualPower);
    }

    // -----------------------
    // --- Mood Transition ---
    // -----------------------
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

        // Final apply
        mainLight.color = targetLightColor;
        mainLight.intensity = targetLightIntensity;
        RenderSettings.ambientLight = targetAmbient;

        if (colorGrading != null)
        {
            colorGrading.saturation.value = targetSaturation;
            colorGrading.contrast.value = targetContrast;
        }

        // Update cars at end
        UpdateCarGroups(targetPower);
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

        UpdateCarGroups(powerPercent);
    }

    // -----------------------
    // --- Cloud Pooling ---
    // -----------------------
    private void InitializeCloudPool()
    {
        for (int i = 0; i < maxClouds; i++)
        {
            Vector3 pos = RandomPointInBox(cloudSpawnArea, cloudSpawnBoxSize);
            GameObject cloud = Instantiate(cloudPrefab, pos, Quaternion.identity, cloudSpawnArea);
            cloud.SetActive(false);
            cloudPool.Add(cloud);
        }
    }

    private IEnumerator SpawnController()
    {
        while (true)
        {
            float power = MapPower(mayorsPower.GetPowerPercentage());

            // Clouds: more when power is HIGH
            int targetClouds = Mathf.RoundToInt(Mathf.Lerp(0, maxClouds, power));
            ManageCloudPool(targetClouds);

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void ManageCloudPool(int targetCount)
    {
        if (targetCount == currentActiveClouds)
            return; // no change

        for (int i = 0; i < cloudPool.Count; i++)
        {
            bool shouldBeActive = i < targetCount;
            if (cloudPool[i].activeSelf != shouldBeActive)
            {
                cloudPool[i].SetActive(shouldBeActive);

                if (shouldBeActive)
                    cloudPool[i].transform.position = RandomPointInBox(cloudSpawnArea, cloudSpawnBoxSize);
            }
        }

        currentActiveClouds = targetCount;
    }

    private Vector3 RandomPointInBox(Transform center, Vector3 size)
    {
        return center.position + new Vector3(
            Random.Range(-size.x / 2f, size.x / 2f),
            Random.Range(-size.y / 2f, size.y / 2f),
            Random.Range(-size.z / 2f, size.z / 2f)
        );
    }

    // -----------------------
    // --- Car Groups ---
    // -----------------------
    private void UpdateCarGroups(float power)
    {
        // Cars active when mood is LOW
        int activeCount = Mathf.RoundToInt(carActivationCurve.Evaluate(1f - power) * carGroups.Count);

        for (int i = 0; i < carGroups.Count; i++)
        {
            bool shouldBeActive = i < activeCount;
            if (carGroups[i] != null && carGroups[i].activeSelf != shouldBeActive)
                carGroups[i].SetActive(shouldBeActive);
        }
    }
}