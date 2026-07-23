using UnityEngine;

public class InfoPopup : MonoBehaviour
{
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private GameObject infoButton;

    private void Start()
    {
        infoPanel.SetActive(false);
    }

    public void OpenInfo()
    {
        infoPanel.SetActive(true);
        infoButton.SetActive(false);
    }

    public void CloseInfo()
    {
        infoPanel.SetActive(false);
        infoButton.SetActive(true);
    }
}