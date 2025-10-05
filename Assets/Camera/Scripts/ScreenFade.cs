using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFade : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 3f;
    private Coroutine _fadeRoutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(FadeIn());
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (_fadeRoutine != null)
                StopCoroutine(_fadeRoutine);

            _fadeRoutine = StartCoroutine(FadeIn());
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            if (_fadeRoutine != null)
                StopCoroutine(_fadeRoutine);

            _fadeRoutine = StartCoroutine(FadeOut());
        }
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
