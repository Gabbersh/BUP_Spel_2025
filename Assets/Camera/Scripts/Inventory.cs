using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    private List<Interactable> items = new List<Interactable>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void AddItem(Interactable item)
    {
        items.Add(item);
        Debug.Log($"Added {item.name} to inventory. Total items: {items.Count}");
    }
}
