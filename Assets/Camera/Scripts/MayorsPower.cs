using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MayorsPower : MonoBehaviour
{
    [Header("Core Settings")]
    [SerializeField] Image bar;
    [SerializeField] int current = 100;
    [SerializeField] int max = 100;

    [Header("Animation Speed")]
    [SerializeField, Range(0, 0.5f)] float animationTime = 0.5f;
    private Coroutine _fillRoutine;

    [Header("Gradient Settings")]
    [SerializeField] bool useGradient;
    [SerializeField] Gradient barGradient;

    private void Start()
    {
        UpdateBar();
        UseGradient();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            ChangeBarByAmount(10);
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            ChangeBarByAmount(-10);
        }
        //UpdateBar();
    }

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
        float targetfill = (float) current / max;

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
}
