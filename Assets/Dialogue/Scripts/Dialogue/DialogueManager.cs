using Ink.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles all dialogue display and interaction.
/// Uses Ink for dialogue, supports smart completion:
/// - Story dialogues (no choices) = auto-complete
/// - Quiz dialogues (has choices) = need #success tag on correct answer
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject continueButton;

    [Header("Typewriter Effect")]
    [SerializeField] private float typingSpeed = 0.04f;
    [SerializeField] private bool canSkipTyping = true;

    [Header("Choices UI")]
    [SerializeField] private GameObject[] choiceButtons; // Set to 4 in inspector!
    private TextMeshProUGUI[] choiceTexts;

    [Header("Choice Settings")]
    [Tooltip("Randomize the order of choices? Prevents memorizing button positions.")]
    [SerializeField] private bool shuffleChoices = true;

    // Maps UI button index -> actual Ink choice index (after shuffling)
    private List<int> shuffledChoiceIndices = new List<int>();

    [Header("Settings")]
    [SerializeField] private float autoExitDelay = 0.1f;

    [Header("Characters")]
    [SerializeField] private SpriteRenderer alexPortrait;
    [SerializeField] private SpriteRenderer linaPortrait;
    [SerializeField] private SpriteRenderer mayorPortrait;

    [SerializeField] private float inactiveBrightness = 0.65f;

    // State
    private Story currentStory;
    private string currentDialogueID = "";
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private bool waitingForClickToClose = false;
    private bool dialogueCompletedNaturally = false;
    private bool dialogueHadChoices = false;

    public event Action<string> OnDialogueStarted;
    public event Action<string> OnDialogueEnded;
    public event Action<string> OnDialogueEndedWithoutSuccess; 

    public bool DialogueIsPlaying { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        InitializeUI();
        SubscribeToInput();
    }

    private void OnDestroy()
    {
        UnsubscribeFromInput();
    }

    // ==================== INITIALIZATION ====================

    private void InitializeUI()
    {
        DialogueIsPlaying = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        // Validate button count
        if (choiceButtons.Length < 4)
        {
            Debug.LogWarning($"[DialogueManager] Only {choiceButtons.Length} choice buttons assigned! Recommended: 4 for maximum flexibility.");
        }

        choiceTexts = new TextMeshProUGUI[choiceButtons.Length];
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            choiceTexts[i] = choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>();

            int index = i;
            Button btn = choiceButtons[i].GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => MakeChoice(index));
            }
        }

        if (continueButton != null)
        {
            Button btn = continueButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(ContinueStory);
            }
            continueButton.SetActive(false);
        }
    }

    private void SubscribeToInput()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnInteract += HandleInteractDuringDialogue;
        }
    }

    private void UnsubscribeFromInput()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnInteract -= HandleInteractDuringDialogue;
        }
    }

    // ==================== INPUT HANDLING ====================

    private void HandleInteractDuringDialogue()
    {
        if (!DialogueIsPlaying) return;

        if (waitingForClickToClose)
        {
            ExitDialogueMode();
            return;
        }

        if (isTyping && canSkipTyping)
        {
            SkipTyping();
        }
        else if (!isTyping && currentStory.currentChoices.Count == 0)
        {
            ContinueStory();
        }
    }

    // ==================== PUBLIC API ====================

    public void EnterDialogueMode(TextAsset inkJSON, string dialogueID = "", string npcName = "")
    {
        if (inkJSON == null)
        {
            Debug.LogError("[DialogueManager] Attempted to start dialogue with null Ink JSON");
            return;
        }

        try
        {
            currentStory = new Story(inkJSON.text);
            BindInkVariables();
        }
        catch (Exception e)
        {
            Debug.LogError($"[DialogueManager] Failed to create Ink story: {e.Message}");
            return;
        }

        currentDialogueID = dialogueID;
        DialogueIsPlaying = true;
        waitingForClickToClose = false;
        dialogueCompletedNaturally = false;
        dialogueHadChoices = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        HideChoices();
        dialogueText.text = string.Empty;

        if (nameText != null)
        {
            nameText.text = !string.IsNullOrEmpty(npcName) ? npcName : "";
        }

        OnDialogueStarted?.Invoke(dialogueID);
        GameEvents.TriggerDialogueStarted(dialogueID);

        ContinueStory();
    }

    public void ExitDialogueMode()
    {
        StartCoroutine(ExitDialogueRoutine());
    }

    public void MakeChoice(int uiButtonIndex)
    {
        if (!DialogueIsPlaying || isTyping) return;

        // Convert UI button index to actual Ink choice index (handles shuffle)
        if (uiButtonIndex < 0 || uiButtonIndex >= shuffledChoiceIndices.Count)
        {
            Debug.LogError($"[DialogueManager] Invalid UI button index: {uiButtonIndex}");
            return;
        }

        int actualInkChoiceIndex = shuffledChoiceIndices[uiButtonIndex];

        if (actualInkChoiceIndex < 0 || actualInkChoiceIndex >= currentStory.currentChoices.Count)
        {
            Debug.LogError($"[DialogueManager] Invalid Ink choice index: {actualInkChoiceIndex}");
            return;
        }

        if (GameManager.Instance != null && !string.IsNullOrEmpty(currentDialogueID))
        {
            GameManager.Instance.RecordChoice(currentDialogueID, actualInkChoiceIndex);
        }

        GameEvents.TriggerChoiceMade(currentDialogueID, actualInkChoiceIndex);

        currentStory.ChooseChoiceIndex(actualInkChoiceIndex);
        SyncInkVariablesToGameManager();

        ContinueStory();
    }

    // ==================== DIALOGUE FLOW ====================

    private void ContinueStory()
    {
        if (!DialogueIsPlaying || isTyping) return;

        if (waitingForClickToClose)
        {
            ExitDialogueMode();
            return;
        }

        HideChoices();

        if (currentStory.canContinue)
        {
            string nextLine = currentStory.Continue();
            SyncInkVariablesToGameManager();

            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeText(nextLine));
        }
        else
        {
            DisplayChoices();
        }
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;

        string processedText = ProcessTags(text);

        // Sätt hela texten direkt
        dialogueText.text = processedText;

        // Tvinga TMP att beräkna layouten
        dialogueText.ForceMeshUpdate();

        int totalCharacters = dialogueText.textInfo.characterCount;

        dialogueText.maxVisibleCharacters = 0;

        for (int i = 0; i <= totalCharacters; i++)
        {
            dialogueText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }

        dialogueText.maxVisibleCharacters = totalCharacters;

        isTyping = false;
        OnTypingComplete();
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        // visa hela texten direkt
        dialogueText.maxVisibleCharacters = dialogueText.textInfo.characterCount;

        isTyping = false;

        OnTypingComplete();
    }

    private void OnTypingComplete()
    {
        DisplayChoices();

        // Check if dialogue has ended
        if (!currentStory.canContinue && currentStory.currentChoices.Count == 0)
        {
            // SMART LOGIC:
            // Story dialogues (no choices) = auto-complete
            // Quiz dialogues (has choices) = need #success tag

            if (!dialogueHadChoices)
            {
                // Story dialogue - auto-complete
                dialogueCompletedNaturally = true;
            }
            else
            {
                // Quiz dialogue - check for #success tag
                bool hasSuccessTag = false;
                if (currentStory.currentTags != null)
                {
                    foreach (string tag in currentStory.currentTags)
                    {
                        if (tag.Trim().ToLower() == "success")
                        {
                            hasSuccessTag = true;
                            break;
                        }
                    }
                }
                dialogueCompletedNaturally = hasSuccessTag;
            }

            waitingForClickToClose = true;
        }
    }

    // ==================== CHOICES ====================

    private void DisplayChoices()
    {
        if (isTyping) return;

        List<Choice> currentChoices = currentStory.currentChoices;

        // Track if this dialogue has choices (for quiz detection)
        if (currentChoices.Count > 0)
        {
            dialogueHadChoices = true;
        }

        // Create shuffled indices if shuffle is enabled
        shuffledChoiceIndices.Clear();
        for (int i = 0; i < currentChoices.Count; i++)
        {
            shuffledChoiceIndices.Add(i);
        }

        // Shuffle the indices (randomize button order)
        if (shuffleChoices && currentChoices.Count > 1)
        {
            for (int i = 0; i < shuffledChoiceIndices.Count; i++)
            {
                int temp = shuffledChoiceIndices[i];
                int randomIndex = UnityEngine.Random.Range(i, shuffledChoiceIndices.Count);
                shuffledChoiceIndices[i] = shuffledChoiceIndices[randomIndex];
                shuffledChoiceIndices[randomIndex] = temp;
            }
        }

        // Display choices in shuffled order
        for (int uiIndex = 0; uiIndex < shuffledChoiceIndices.Count; uiIndex++)
        {
            if (uiIndex >= choiceButtons.Length)
            {
                Debug.LogError($"[DialogueManager] Too many choices! UI supports {choiceButtons.Length}, got {currentChoices.Count}");
                break;
            }

            int inkChoiceIndex = shuffledChoiceIndices[uiIndex];
            Choice choice = currentChoices[inkChoiceIndex];

            choiceButtons[uiIndex].SetActive(true);
            choiceTexts[uiIndex].text = choice.text;
        }

        // Hide unused buttons
        for (int i = shuffledChoiceIndices.Count; i < choiceButtons.Length; i++)
        {
            choiceButtons[i].SetActive(false);
        }

        if (continueButton != null)
        {
            continueButton.SetActive(currentChoices.Count == 0 && currentStory.canContinue);
        }
    }

    private void HideChoices()
    {
        foreach (GameObject choice in choiceButtons)
        {
            choice.SetActive(false);
        }

        if (continueButton != null)
            continueButton.SetActive(false);
    }

    // ==================== EXIT ====================

    private IEnumerator ExitDialogueRoutine()
    {
        yield return new WaitForSeconds(autoExitDelay);

        string endedDialogueID = currentDialogueID;
        bool wasCompletedSuccessfully = dialogueCompletedNaturally;

        // Mark dialogue as complete if it has #success tag
        if (GameManager.Instance != null && !string.IsNullOrEmpty(endedDialogueID) && wasCompletedSuccessfully)
        {
            GameManager.Instance.MarkDialogueComplete(endedDialogueID);
        }

        DialogueIsPlaying = false;
        isTyping = false;
        waitingForClickToClose = false;
        dialogueCompletedNaturally = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        dialogueText.text = "";
        if (nameText != null)
            nameText.text = "";

        HideChoices();

        currentDialogueID = "";

        OnDialogueEnded?.Invoke(endedDialogueID);
        GameEvents.TriggerDialogueEnded(endedDialogueID);

        // Fire special event if dialogue ended without success (wrong choice)
        if (!wasCompletedSuccessfully && !string.IsNullOrEmpty(endedDialogueID))
        {
            Debug.Log($"[DialogueManager] Dialogue '{endedDialogueID}' ended WITHOUT success - firing OnDialogueEndedWithoutSuccess event");
            OnDialogueEndedWithoutSuccess?.Invoke(endedDialogueID);
        }
    }

    // ==================== INK INTEGRATION ====================

    private void BindInkVariables()
    {
        if (currentStory == null || GameManager.Instance == null) return;

        List<string> variableNames = new List<string>();
        foreach (string varName in currentStory.variablesState)
        {
            variableNames.Add(varName);
        }

        foreach (string varName in variableNames)
        {
            object value = currentStory.variablesState[varName];

            if (value is bool boolVal)
            {
                bool savedValue = GameManager.Instance.GetFlag(varName, boolVal);
                currentStory.variablesState[varName] = savedValue;
            }
            else if (value is int intVal)
            {
                int savedValue = GameManager.Instance.GetFlag(varName, intVal);
                currentStory.variablesState[varName] = savedValue;
            }
            else if (value is float floatVal)
            {
                float savedValue = GameManager.Instance.GetFlag(varName, floatVal);
                currentStory.variablesState[varName] = savedValue;
            }
            else if (value is string stringVal)
            {
                string savedValue = GameManager.Instance.GetFlag(varName, stringVal);
                currentStory.variablesState[varName] = savedValue;
            }
        }
    }

    private void SyncInkVariablesToGameManager()
    {
        if (currentStory == null || GameManager.Instance == null) return;

        foreach (string varName in currentStory.variablesState)
        {
            object value = currentStory.variablesState[varName];

            if (value is bool boolVal)
                GameManager.Instance.SetFlag(varName, boolVal);
            else if (value is int intVal)
                GameManager.Instance.SetFlag(varName, intVal);
            else if (value is float floatVal)
                GameManager.Instance.SetFlag(varName, floatVal);
            else if (value is string stringVal)
                GameManager.Instance.SetFlag(varName, stringVal);
        }
    }

    // ==================== UTILITY ====================

    private string ProcessTags(string text)
    {
        if (currentStory == null) return text;

        foreach (string tag in currentStory.currentTags)
        {
            string[] parts = tag.Split(':');
            if (parts.Length >= 2)
            {
                string key = parts[0].Trim().ToLower();
                string value = parts[1].Trim();

                if (key == "speaker" && nameText != null)
                {
                    nameText.text = value;
                    UpdatePortraitFocus(value);
                }
            }
        }

        return text;
    }

    private void UpdatePortraitFocus(string speaker)
    {
        if (alexPortrait == null || linaPortrait == null || mayorPortrait == null)
            return;

        if (speaker.ToLower() == "alex")
        {
            alexPortrait.color = Color.white;
            linaPortrait.color = new Color(inactiveBrightness, inactiveBrightness, inactiveBrightness);
            mayorPortrait.color = new Color(inactiveBrightness, inactiveBrightness, inactiveBrightness);
        }
        else if (speaker.ToLower() == "lina")
        {
            linaPortrait.color = Color.white;
            alexPortrait.color = new Color(inactiveBrightness, inactiveBrightness, inactiveBrightness);
            mayorPortrait.color = new Color(inactiveBrightness, inactiveBrightness, inactiveBrightness);
        }
        else if (speaker.ToLower() == "tryggve ångström")
        {
            mayorPortrait.color = Color.white;
            alexPortrait.color = new Color(inactiveBrightness, inactiveBrightness, inactiveBrightness);
            linaPortrait.color = new Color(inactiveBrightness, inactiveBrightness, inactiveBrightness);
        }
    }

}