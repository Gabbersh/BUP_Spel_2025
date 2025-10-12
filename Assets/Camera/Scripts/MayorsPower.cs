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

    [Header("Core Settings")]
    [SerializeField] private Image bar;
    [SerializeField] private int current = 100;
    [SerializeField] private int max = 100;

    [Header("Animation Speed")]
    [SerializeField, Range(0, 0.5f)] private float animationTime = 0.5f;
    private Coroutine _fillRoutine;

    [Header("Gradient Settings")]
    [SerializeField] private bool useGradient;
    [SerializeField] private Gradient barGradient;

    [Header("Choice Integration")]
    [SerializeField] private List<ChoicePowerEffect> choicePowerEffects = new List<ChoicePowerEffect>();

    private void Start()
    {
        UpdateBar();
        UseGradient();

        // Subscribe to choice events from GameEvents
        GameEvents.OnChoiceMade += OnChoiceMade;
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        GameEvents.OnChoiceMade -= OnChoiceMade;
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

    // ==================== POWER BAR LOGIC ====================

    public bool ChangeBarByAmount(int amount)
    {
        if (current + amount < 0)
        {
            return false;
        }

        current += amount;
        current = Mathf.Clamp(current, 0, max);

        UpdateBar();

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
}