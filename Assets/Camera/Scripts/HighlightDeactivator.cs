using UnityEngine;

public class HighlightDeactivator : MonoBehaviour
{
    private Interactable parentInteractable;

    void Start()
    {
        parentInteractable = GetComponentInParent<Interactable>();

        if (parentInteractable == null)
        {
            Debug.LogWarning($"{name}: No Interactable found in parent!");
        }
    }

    public void DeactivateHighlight()
    {
        gameObject.SetActive(false);
    }
}