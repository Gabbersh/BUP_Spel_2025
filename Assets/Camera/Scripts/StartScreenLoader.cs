using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartScreenLoader : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string citySceneName = "MainScene";

    [Header("UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private CanvasGroup fadeCanvasGroup; // alpha 0 = synlig startskärm, alpha 1 = svart/vit overlay
    [SerializeField] private float fadeOutDuration = 0.35f;

    [Header("Loading")]
    [SerializeField] private bool preloadOnStart = true;

    private AsyncOperation _loadOp;
    private bool _startPressed;

    private void Awake()
    {
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.interactable = false;
        }
    }

    private void Start()
    {
        if (preloadOnStart)
            BeginPreloadCity();
    }

    public void BeginPreloadCity()
    {
        if (_loadOp != null) return;

        _loadOp = SceneManager.LoadSceneAsync(citySceneName, LoadSceneMode.Single);
        _loadOp.allowSceneActivation = false; // håll kvar på 90% tills vi vill byta
    }

    public void StartGame()
    {
        if (_startPressed) return;
        _startPressed = true;

        if (_loadOp == null)
            BeginPreloadCity();

        startButton.interactable = false;
        StartCoroutine(CoStartSequence());
    }

    private IEnumerator CoStartSequence()
    {
        // Vänta tills scenen är "färdigladdad" (0.9) för att undvika hitch vid activation
        while (_loadOp.progress < 0.9f)
            yield return null;

        // Fade ut startskärmen (overlay in)
        yield return FadeCanvasGroup(fadeCanvasGroup, 0f, 1f, fadeOutDuration);

        // Aktivera City-scenen
        _loadOp.allowSceneActivation = true;

        // Vänta tills City är aktiv
        while (!_loadOp.isDone)
            yield return null;
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null || duration <= 0f) yield break;

        cg.blocksRaycasts = true;

        float t = 0f;
        cg.alpha = from;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }

        cg.alpha = to;
    }
}
