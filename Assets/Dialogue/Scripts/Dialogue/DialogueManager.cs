using Ink.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simplified DialogueManager with better integration with new systems.
/// Cleaner code, better event handling, and easier to maintain.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI nameText; // Optional: for character names
    [SerializeField] private GameObject continueButton;

    [Header("Typewriter Effect")]
    [SerializeField] private float typingSpeed = 0.04f;
    [SerializeField] private bool canSkipTyping = true;

    [Header("Choices UI")]
    [SerializeField] private GameObject[] choiceButtons;
    private TextMeshProUGUI[] choiceTexts;

    [Header("Settings")]
    [SerializeField] private float autoExitDelay = 0.1f;

    // State
    private Story currentStory;
    private string currentDialogueID = "";
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private bool waitingForClickToClose = false;
    private bool dialogueCompletedNaturally = false;

    // Events (kept for backwards compatibility, but also uses GameEvents)
    public event Action<string> OnDialogueStarted;
    public event Action<string> OnDialogueEnded;

    // Properties
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

        // Setup choice buttons
        choiceTexts = new TextMeshProUGUI[choiceButtons.Length];
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            choiceTexts[i] = choiceButtons[i].GetComponentInChildren<TextMeshProUGUI>();

            // Add click listeners
            int index = i; // Capture for closure
            Button btn = choiceButtons[i].GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => MakeChoice(index));
            }
        }

        // Setup continue button
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

        // If waiting for click to close, close now
        if (waitingForClickToClose)
        {
            Debug.Log("[DialogueManager] Player clicked to close dialogue");
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

    public void EnterDialogueMode(TextAsset inkJSON, string dialogueID = "")
    {
        if (inkJSON == null)
        {
            Debug.LogError("[DialogueManager] Attempted to start dialogue with null Ink JSON");
            return;
        }

        Debug.Log($"[DialogueManager] ▶ STARTING dialogue: '{dialogueID}', Ink file: {inkJSON.name}");

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

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        HideChoices();
        dialogueText.text = string.Empty;

        // Fire events
        OnDialogueStarted?.Invoke(dialogueID);
        GameEvents.TriggerDialogueStarted(dialogueID);

        ContinueStory();
    }

    public void ExitDialogueMode()
    {
        StartCoroutine(ExitDialogueRoutine());
    }

    public void MakeChoice(int choiceIndex)
    {
        if (!DialogueIsPlaying || isTyping) return;

        if (choiceIndex < 0 || choiceIndex >= currentStory.currentChoices.Count)
        {
            Debug.LogError($"[DialogueManager] Invalid choice index: {choiceIndex}");
            return;
        }

        Debug.Log($"[DialogueManager] Choice made: {choiceIndex} in dialogue '{currentDialogueID}'");

        // Record choice
        if (GameManager.Instance != null && !string.IsNullOrEmpty(currentDialogueID))
        {
            GameManager.Instance.RecordChoice(currentDialogueID, choiceIndex);
        }

        // Fire events
        GameEvents.TriggerChoiceMade(currentDialogueID, choiceIndex);

        currentStory.ChooseChoiceIndex(choiceIndex);
        SyncInkVariablesToGameManager();

        ContinueStory();
    }

    // ==================== DIALOGUE FLOW ====================

    private void ContinueStory()
    {
        if (!DialogueIsPlaying || isTyping) return;

        // If waiting to close, close now
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
        dialogueText.text = "";

        // Check for character name tags (optional feature)
        string processedText = ProcessTags(text);

        foreach (char letter in processedText)
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        OnTypingComplete();
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogueText.text = ProcessTags(currentStory.currentText);
        isTyping = false;
        OnTypingComplete();
    }

    private void OnTypingComplete()
    {
        DisplayChoices();

        // Check if dialogue is finished (no more content and no choices)
        if (!currentStory.canContinue && currentStory.currentChoices.Count == 0)
        {
            // Dialogue reached END naturally
            dialogueCompletedNaturally = true;
            waitingForClickToClose = true;
            Debug.Log($"[DialogueManager] ✓ Dialogue '{currentDialogueID}' reached END naturally. Waiting for click to close.");
        }
    }

    // ==================== CHOICES ====================

    private void DisplayChoices()
    {
        if (isTyping) return;

        List<Choice> currentChoices = currentStory.currentChoices;

        // Show choices that exist, hide the rest
        int index = 0;
        foreach (Choice choice in currentChoices)
        {
            if (index >= choiceButtons.Length)
            {
                Debug.LogError($"[DialogueManager] Too many choices! UI supports {choiceButtons.Length}, got {currentChoices.Count}");
                break;
            }

            choiceButtons[index].SetActive(true);
            choiceTexts[index].text = choice.text;
            index++;
        }

        // Hide unused choice buttons
        for (int i = index; i < choiceButtons.Length; i++)
        {
            choiceButtons[i].SetActive(false);
        }

        // Show continue button only if no choices available and story can continue
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

        // DEBUG: Check all conditions
        Debug.Log($"[DialogueManager] === EXIT ROUTINE DEBUG ===");
        Debug.Log($"[DialogueManager] GameManager.Instance != null? {GameManager.Instance != null}");
        Debug.Log($"[DialogueManager] endedDialogueID is not empty? {!string.IsNullOrEmpty(endedDialogueID)} (value: '{endedDialogueID}')");
        Debug.Log($"[DialogueManager] dialogueCompletedNaturally? {dialogueCompletedNaturally}");

        // IMPORTANT: Mark dialogue as complete BEFORE firing events
        // This ensures NPCController.OnDialogueComplete can check if it's complete
        if (GameManager.Instance != null && !string.IsNullOrEmpty(endedDialogueID) && dialogueCompletedNaturally)
        {
            GameManager.Instance.MarkDialogueComplete(endedDialogueID);
            Debug.Log($"[DialogueManager] ✓✓✓ Marked dialogue '{endedDialogueID}' as COMPLETE in GameManager");
        }
        else if (!dialogueCompletedNaturally)
        {
            Debug.Log($"[DialogueManager] ✗ Dialogue '{endedDialogueID}' was CANCELLED - NOT marked as complete");
        }
        else
        {
            Debug.LogError($"[DialogueManager] ✗✗✗ FAILED TO MARK COMPLETE - One of the conditions failed!");
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

        Debug.Log($"[DialogueManager] ■ ENDED dialogue: '{endedDialogueID}'");

        // Fire events AFTER marking complete
        OnDialogueEnded?.Invoke(endedDialogueID);
        GameEvents.TriggerDialogueEnded(endedDialogueID);
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

    /// <summary>
    /// Process Ink tags for character names, etc.
    /// Example: "speaker: John" tag sets the name text
    /// </summary>
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
                }
            }
        }

        return text;
    }
}