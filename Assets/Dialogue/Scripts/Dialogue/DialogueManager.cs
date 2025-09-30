using Ink.Runtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject continueButton;

    [Header("Typewriter Effect")]
    [SerializeField] private float typingSpeed = 0.04f;
    [SerializeField] private bool canSkipTyping = true;

    [Header("Auto Exit Settings")]
    [SerializeField] private float autoExitDelay = 0.1f;

    [Header("Choices UI")]
    [SerializeField] private GameObject[] choices;
    private TextMeshProUGUI[] choicesText;

    private Story currentStory;
    private static DialogueManager instance;

    public bool dialogueIsPlaying { get; private set; }
    private bool dialogueHasEnded = false;

    private bool isTyping = false;
    private Coroutine typingCoroutine;

    private void Start()
    {
        dialogueIsPlaying = false;
        dialoguePanel.SetActive(false);

        choicesText = new TextMeshProUGUI[choices.Length];
        int index = 0;
        foreach (GameObject choice in choices)
        {
            choicesText[index] = choice.GetComponentInChildren<TextMeshProUGUI>();
            index++;
        }

        if (continueButton != null)
        {
            Button btn = continueButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => ContinueStory());
            }
            continueButton.SetActive(false);
        }
    }

    private void Update()
    {
        if (!dialogueIsPlaying || dialogueHasEnded) return;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            {
                HandleDialogueInput();
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                HandleDialogueInput();
            }
        }
    }

    private void HandleDialogueInput()
    {
        if (isTyping && canSkipTyping)
        {
            SkipTyping();
        }
        else if (!isTyping && currentStory.currentChoices.Count == 0)
        {
            ContinueStory();
        }
    }

    private void Awake()
    {
        if (instance != null)
            Debug.LogError("Found more than one Dialogue Manager in the scene");

        instance = this;
    }

    public static DialogueManager GetInstance()
    {
        return instance;
    }

    public void EnterDialogueMode(TextAsset inkJSON)
    {
        currentStory = new Story(inkJSON.text);
        dialogueIsPlaying = true;
        dialogueHasEnded = false;
        dialoguePanel.SetActive(true);

        ContinueStory();
    }

    private IEnumerator ExitDialogueMode()
    {
        yield return new WaitForSeconds(autoExitDelay);

        dialogueIsPlaying = false;
        dialogueHasEnded = false;
        dialoguePanel?.SetActive(false);
        dialogueText.text = "";

        if (continueButton != null)
            continueButton.SetActive(false);

        foreach (GameObject choice in choices)
        {
            choice.SetActive(false);
        }
    }

    public void ForceExitDialogue()
    {
        StopAllCoroutines();

        dialogueIsPlaying = false;
        dialogueHasEnded = false;
        dialoguePanel?.SetActive(false);
        dialogueText.text = "";
        isTyping = false;

        if (continueButton != null)
            continueButton.SetActive(false);

        foreach (GameObject choice in choices)
        {
            choice.SetActive(false);
        }
    }

    private void ContinueStory()
    {
        if (dialogueHasEnded || isTyping) return;

        if (currentStory.canContinue)
        {
            string nextLine = currentStory.Continue();
            HideAllChoices();

            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeText(nextLine));
        }
        else
        {
            dialogueHasEnded = true;
            if (continueButton != null)
                continueButton.SetActive(false);
            StartCoroutine(ExitDialogueMode());
        }
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        DisplayChoices();

        if (!currentStory.canContinue && currentStory.currentChoices.Count == 0)
        {
            dialogueHasEnded = true;
            if (continueButton != null)
                continueButton.SetActive(false);
            StartCoroutine(ExitDialogueMode());
        }
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        dialogueText.text = currentStory.currentText;
        isTyping = false;
        DisplayChoices();

        if (!currentStory.canContinue && currentStory.currentChoices.Count == 0)
        {
            dialogueHasEnded = true;
            if (continueButton != null)
                continueButton.SetActive(false);
            StartCoroutine(ExitDialogueMode());
        }
    }

    private void DisplayChoices()
    {
        if (isTyping) return;

        List<Choice> currentChoices = currentStory.currentChoices;

        if (currentChoices.Count > choices.Length)
            Debug.LogError("More choices were given than the UI can support. Number of choices given: " + currentChoices.Count);

        int index = 0;
        foreach (Choice choice in currentChoices)
        {
            choices[index].gameObject.SetActive(true);
            choicesText[index].text = choice.text;
            index++;
        }

        for (int i = index; i < choices.Length; i++)
        {
            choices[i].gameObject.SetActive(false);
        }

        if (continueButton != null)
            continueButton.SetActive(currentChoices.Count == 0 && !dialogueHasEnded);
    }

    private void HideAllChoices()
    {
        foreach (GameObject choice in choices)
        {
            choice.SetActive(false);
        }

        if (continueButton != null)
            continueButton.SetActive(false);
    }

    public void MakeChoice(int choiceIndex)
    {
        if (dialogueHasEnded || isTyping) return;

        currentStory.ChooseChoiceIndex(choiceIndex);
        ContinueStory();
    }
}