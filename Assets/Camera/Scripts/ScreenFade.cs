using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFade : MonoBehaviour
{
    public static ScreenFade Instance { get; private set; }

    public Image fadeImage;
    public GameObject inputBlocker;
    public GameObject nextDayText;
    public float fadeDuration = 1.5f;

    private Coroutine fadeRoutine;

    public bool IsFading {  get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(FadeIn());
    }

    public void FadeOutThenIn()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeSequence());
    }

    private IEnumerator FadeSequence()
    {
        if (inputBlocker != null)
            inputBlocker.SetActive(true);

        if (nextDayText != null)
            nextDayText.SetActive(false);

        IsFading = true;

        yield return StartCoroutine(FadeOut());

        yield return new WaitForSeconds(.5f);

        // Skärmen är nu helt svart
        if (nextDayText != null)
            nextDayText.SetActive(true);

        yield return new WaitForSeconds(2f);

        if (nextDayText != null)
            nextDayText.SetActive(false);

        yield return new WaitForSeconds(.5f);

        yield return StartCoroutine(FadeIn());

        IsFading = false;

        if (inputBlocker != null)
            inputBlocker.SetActive(false);
    }

    public IEnumerator FadeIn()
    {
        float t = 0;
        Color color = fadeImage.color;

        color.a = 1;
        fadeImage.color = color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            color.a = 1 - (t / fadeDuration);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 0;
        fadeImage.color = color;
    }

    public IEnumerator FadeOut()
    {
        float t = 0;
        Color color = fadeImage.color;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            color.a = t / fadeDuration;
            fadeImage.color = color;
            yield return null;
        }

        color.a = 1;
        fadeImage.color = color;
    }
}