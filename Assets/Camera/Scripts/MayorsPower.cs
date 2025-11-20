using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MayorsPower : MonoBehaviour
{
    [System.Serializable]
    public class ChoicePowerEffect
    {
        [Tooltip("Dialogue ID to watch")]
        public string dialogueID;

        [Tooltip("Choice index (0 = first choice, 1 = second, etc.)")]
        public int choiceIndex;

        [Tooltip("Power change amount (positive or negative)")]
        public int powerChange;
    }

    [System.Serializable]
    public class DialoguePowerEffect
    {
        [Tooltip("Dialogue ID to watch for completion")]
        public string dialogueID;

        [Tooltip("Power change when this dialogue completes (positive or negative)")]
        public int powerChange;

        [Tooltip("Optional: Description of what this represents")]
        public string description;
    }

    [Header("Core Settings")]
    [SerializeField] private Image bar;
    [SerializeField] private int current = 100;
    [SerializeField] private int max = 100;

    [Header("Animation Speed")]
    [SerializeField, Range(0, 1f)] private float animationTime = 1f;
    private Coroutine _fillRoutine;

    [Header("Gradient Settings")]
    [SerializeField] private bool useGradient;
    [SerializeField] private Gradient barGradient;

    [Header("Choice Integration")]
    [SerializeField] private List<ChoicePowerEffect> choicePowerEffects = new List<ChoicePowerEffect>();

    [Header("Dialogue Completion Integration")]
    [SerializeField] private List<DialoguePowerEffect> dialoguePowerEffects = new List<DialoguePowerEffect>();

    private void Start()
    {
        // Load saved power or use default
        if (GameManager.Instance != null)
        {
            current = GameManager.Instance.GetFlag("mayor_power", current);
        }

        UpdateBar();
        UseGradient();
        OnMayorPowerChanged?.Invoke(GetPowerPercentage());

        // Subscribe to game events
        GameEvents.OnChoiceMade += OnChoiceMade;
        GameEvents.OnDialogueEnded += OnDialogueEnded;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        GameEvents.OnChoiceMade -= OnChoiceMade;
        GameEvents.OnDialogueEnded -= OnDialogueEnded;
    }

    private void Update()
    {
        // Debug testing keys
        if (Input.GetKeyDown(KeyCode.M))
        {
            ChangeBarByAmount(10);
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            ChangeBarByAmount(-10);
        }
    }

    // ==================== CHOICE HANDLING ====================

    private void OnChoiceMade(string dialogueID, int choiceIndex)
    {
        // Find if this choice affects power
        foreach (var effect in choicePowerEffects)
        {
            if (effect.dialogueID == dialogueID && effect.choiceIndex == choiceIndex)
            {
                ChangeBarByAmount(effect.powerChange);
                Debug.Log($"[MayorsPower] Choice made: {effect.powerChange:+0;-0} power from {dialogueID}");
                break; // Only apply first matching effect
            }
        }
    }

    // ==================== DIALOGUE COMPLETION HANDLING ====================

    private void OnDialogueEnded(string dialogueID)
    {
        // Only trigger if dialogue was actually completed (not cancelled)
        if (GameManager.Instance != null && !GameManager.Instance.IsDialogueComplete(dialogueID))
        {
            return; // Dialogue was cancelled, don't apply power change
        }

        // Find if this dialogue completion affects power
        foreach (var effect in dialoguePowerEffects)
        {
            if (effect.dialogueID == dialogueID)
            {
                ChangeBarByAmount(effect.powerChange);
                Debug.Log($"[MayorsPower] Dialogue completed '{dialogueID}': {effect.powerChange:+0;-0} power change");

                if (!string.IsNullOrEmpty(effect.description))
                {
                    Debug.Log($"[MayorsPower] → {effect.description}");
                }

                break; // Only apply first matching effect
            }
        }
    }

    // ==================== POWER BAR LOGIC ====================

    public bool ChangeBarByAmount(int amount)
    {
        if (current + amount < 0)
        {
            return false;
        }

        current += amount;
        current = Mathf.Clamp(current, 0, max);

        // Save power to GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetFlag("mayor_power", current);
        }

        UpdateBar();
        OnMayorPowerChanged?.Invoke(GetPowerPercentage());

        return true;
    }

    private void UpdateBar()
    {
        if (current <= 10)
        {
            current = 10;
            TriggerFillAnimation();
            return;
        }

        TriggerFillAnimation();
    }

    private void TriggerFillAnimation()
    {
        float targetfill = (float)current / max;

        if (Mathf.Approximately(bar.fillAmount, targetfill))
            return;

        if (_fillRoutine != null)
            StopCoroutine(_fillRoutine);

        _fillRoutine = StartCoroutine(SmoothlyTransitionToNewValue(targetfill));
    }

    private IEnumerator SmoothlyTransitionToNewValue(float targetFill)
    {
        float originalFill = bar.fillAmount;
        float elapsedTime = 0.0f;

        while (elapsedTime < animationTime)
        {
            elapsedTime += Time.deltaTime;
            float time = elapsedTime / animationTime;
            bar.fillAmount = Mathf.Lerp(originalFill, targetFill, time);

            UseGradient();

            yield return null;
        }

        bar.fillAmount = targetFill;
    }

    private void UseGradient()
    {
        if (!useGradient)
            return;

        bar.color = barGradient.Evaluate(bar.fillAmount);
    }

    // ==================== PUBLIC API ====================

    public int GetCurrentPower()
    {
        return current;
    }

    public int GetMaxPower()
    {
        return max;
    }

    public float GetPowerPercentage()
    {
        return (float)current / max;
    }

    public void SetPower(int value)
    {
        current = Mathf.Clamp(value, 0, max);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetFlag("mayor_power", current);
        }

        UpdateBar();
    }

    public void ResetPower()
    {
        SetPower(max);
    }

    public static event System.Action<float> OnMayorPowerChanged;
}