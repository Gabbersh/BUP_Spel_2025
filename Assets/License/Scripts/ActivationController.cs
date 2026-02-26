using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ActivationController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField codeInput;
    [SerializeField] private Button activateButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Navigation")]
    [SerializeField] private string nextSceneName = "Start";

    [Header("DEV")]
    [SerializeField] private bool resetLicenseOnStart = false;

    private void Awake()
    {
#if UNITY_EDITOR
        if (resetLicenseOnStart)
        {
            LicenseManager.ClearLicense();
            Debug.Log("License reset via Inspector toggle.");
        }
#endif

        if (LicenseManager.IsActivated())
        {
            LoadNext();
            return;
        }

        statusText.text = "";
        activateButton.onClick.AddListener(OnActivateClicked);
    }

    private void OnActivateClicked()
    {
        var code = codeInput.text;

        if (LicenseManager.TryActivateMock(code, out var error))
        {
            statusText.text = "Activated! Starting...";
            LoadNext();
        }
        else
        {
            statusText.text = error;
        }
    }

    private void LoadNext()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}