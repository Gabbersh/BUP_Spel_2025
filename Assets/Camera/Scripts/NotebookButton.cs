using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class NotebookButton : MonoBehaviour
{
    [Header("References")]
    public CameraMovement cameraMovement;
    public GameObject handbook;
    public GameObject blockClick;
    public GameObject POI;

    [Header("UI Button")]
    public GameObject notebookIcon;
    public Button notebookButton;

    [Header("Auto Intro")]
    public float delayBeforeStart = 0f;
    private bool hasAutoOpened = false;

    private bool buttonEnabled = false;
    private bool notebookOpen = false;

    void Start()
    {
        handbook.SetActive(buttonEnabled);
        blockClick.SetActive(buttonEnabled);
        notebookButton.onClick.AddListener(OnNoteBookPressed);
    }

    void OnNoteBookPressed()
    {
        if (handbook.GetComponent<AutoFlip>().isFlipping)
            return;

        notebookOpen = !notebookOpen;
        handbook.SetActive(notebookOpen);
        blockClick.SetActive(notebookOpen);
        POI.SetActive(!notebookOpen);
    }

    void OnEnable()
    {
        if (cameraMovement == null) return;

        cameraMovement.OnLeftRail += HideButtons;
        cameraMovement.OnReachedPOI += HideButtons;
        cameraMovement.OnReturnedToRail += ShowButtons;
    }
    void OnDisable()
    {
        if (cameraMovement == null) return;

        cameraMovement.OnLeftRail -= HideButtons;
        cameraMovement.OnReachedPOI -= HideButtons;
        cameraMovement.OnReturnedToRail -= ShowButtons;
    }

    private void ShowButtons()
    {
        buttonEnabled = true;
        SetButtonsActive(buttonEnabled);

        if (!hasAutoOpened)
        {
            StartCoroutine(AutoIntroSequence());
            hasAutoOpened = true;
        }
    }

    private void HideButtons()
    {
        buttonEnabled = false;
        SetButtonsActive(buttonEnabled);
    }
    private void SetButtonsActive(bool state)
    {
        SetButtonVisibility(notebookIcon, state);
    }

    private void SetButtonVisibility(GameObject button, bool visible)
    {
        if (button == null) return;

        CanvasGroup cg = button.GetComponent<CanvasGroup>();
        if (cg == null) cg = button.AddComponent<CanvasGroup>();

        cg.alpha = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;
    }

    private IEnumerator AutoIntroSequence()
    {
        blockClick.SetActive(true); // Lås input
        POI.SetActive(false);
        notebookButton.interactable = false;

        yield return new WaitForSeconds(delayBeforeStart);

        CanvasGroup cg = notebookIcon.GetComponent<CanvasGroup>();

        float singleFadeTime = 0.5f; // tid 0 -> 1
        int blinkCount = 3;

        for (int i = 0; i < blinkCount; i++)
        {
            float timer = 0f;
            float totalDuration = singleFadeTime * 2f; // 0->1->0

            while (timer < totalDuration)
            {
                timer += Time.deltaTime;

                float alpha = 1f - Mathf.PingPong(timer / singleFadeTime, 1f);
                cg.alpha = alpha;

                yield return null;
            }
        }

        cg.alpha = 1f;

        // Öppna handboken
        if (!notebookOpen)
            OnNoteBookPressed();

        notebookButton.interactable = true;
    }

}
