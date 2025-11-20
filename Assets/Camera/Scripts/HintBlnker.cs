using UnityEngine;

public class HintBlinker : MonoBehaviour
{
    [Header("Timing")]
    public float delayBeforeBlink = 2f;   
    public float blinkInterval = 0.5f;    

    private Renderer rend;
    private float timer;
    private bool isBlinking = false;
    private bool isVisible = true;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        timer = delayBeforeBlink;
    }

    private void Update()
    {
        if (!isBlinking)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                isBlinking = true;
                timer = blinkInterval;
            }
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            isVisible = !isVisible;
            rend.enabled = isVisible;  
            timer = blinkInterval;
        }
    }

    public void Reactivate()
    {
        rend.enabled = true;
        isVisible = true;
        isBlinking = false;
        timer = delayBeforeBlink;
    }
}
