using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFade : MonoBehaviour
{
    public static ScreenFade Instance { get; private set; }

    public Image fadeImage;
    public GameObject inputBlocker;
    public GameObject nextDayText;
    public GameObject outroText;
    public float fadeInDuration = 5f;
    public float fadeOutDuration = 5f;

    [Header("UI To Hide During Fade")]
    public GameObject leftArrow;
    public GameObject rightArrow;
    public GameObject handbookIcon;

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
        if (inputBlocker != null) inputBlocker.SetActive(true);
        if (nextDayText != null) nextDayText.SetActive(false);

        if (leftArrow != null) leftArrow.SetActive(false);
        if (rightArrow != null) rightArrow.SetActive(false);
        if (handbookIcon != null) handbookIcon.SetActive(false);

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

        if (inputBlocker != null) inputBlocker.SetActive(false);

        if (leftArrow != null) leftArrow.SetActive(true);
        if (rightArrow != null) rightArrow.SetActive(true);
        if (handbookIcon != null) handbookIcon.SetActive(true);
    }

    public void FadeOutOutro()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeOutroSequence());
    }

    private IEnumerator FadeOutroSequence()
    {
        if (inputBlocker != null)
            inputBlocker.SetActive(true);

        if (nextDayText != null)
            nextDayText.SetActive(false);

        if (outroText != null)
            outroText.SetActive(false);

        IsFading = true;

        yield return new WaitForSeconds(2.5f);

        yield return StartCoroutine(FadeOut());

        yield return new WaitForSeconds(.5f);

        // Visa outrotext
        if (outroText != null)
            outroText.SetActive(true);

        yield return new WaitForSeconds(3f);

        // Här kan du ladda huvudmenyn
        UnityEngine.SceneManagement.SceneManager.LoadScene("Start");
    }

    public IEnumerator FadeIn()
    {
        float t = 0;
        Color color = fadeImage.color;

        color.a = 1;
        fadeImage.color = color;

        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            color.a = 1 - (t / fadeInDuration);
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

        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            color.a = t / fadeOutDuration;
            fadeImage.color = color;
            yield return null;
        }

        color.a = 1;
        fadeImage.color = color;
    }
}