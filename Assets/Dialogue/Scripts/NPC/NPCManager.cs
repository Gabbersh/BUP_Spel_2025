using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages all NPCs in the scene.
/// Handles NPC registration, state updates, and queries.
/// </summary>
public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool autoUpdateStates = true;
    [SerializeField] private float updateInterval = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    // NPC registry
    private Dictionary<string, NPCController> npcs = new Dictionary<string, NPCController>();
    private float lastUpdateTime;

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
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void Update()
    {
        if (autoUpdateStates && Time.time - lastUpdateTime > updateInterval)
        {
            UpdateAllNPCStates();
            lastUpdateTime = Time.time;
        }
    }

    // ==================== EVENT HANDLING ====================

    private void SubscribeToEvents()
    {
        GameEvents.OnDialogueEnded += OnDialogueEnded;
        GameEvents.OnChoiceMade += OnChoiceMade;

        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.OnProgressionUpdated += UpdateAllNPCStates;
        }
    }

    private void UnsubscribeFromEvents()
    {
        GameEvents.OnDialogueEnded -= OnDialogueEnded;
        GameEvents.OnChoiceMade -= OnChoiceMade;

        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.OnProgressionUpdated -= UpdateAllNPCStates;
        }
    }

    private void OnDialogueEnded(string dialogueID)
    {
        DebugLog($"Dialogue ended: {dialogueID}");
        UpdateAllNPCStates();
    }

    private void OnChoiceMade(string dialogueID, int choiceIndex)
    {
        DebugLog($"Choice made in {dialogueID}: {choiceIndex}");
        UpdateAllNPCStates();
    }

    // ==================== NPC REGISTRATION ====================

    public void RegisterNPC(NPCController npc)
    {
        if (npc == null || string.IsNullOrEmpty(npc.NPCID)) return;

        if (npcs.ContainsKey(npc.NPCID))
        {
            Debug.LogWarning($"[NPCManager] NPC {npc.NPCID} already registered! Replacing...");
            npcs[npc.NPCID] = npc;
        }
        else
        {
            npcs.Add(npc.NPCID, npc);
            DebugLog($"Registered NPC: {npc.NPCID}");
        }

        npc.UpdateState();
    }

    public void UnregisterNPC(NPCController npc)
    {
        if (npc == null || string.IsNullOrEmpty(npc.NPCID)) return;

        if (npcs.ContainsKey(npc.NPCID))
        {
            npcs.Remove(npc.NPCID);
            DebugLog($"Unregistered NPC: {npc.NPCID}");
        }
    }

    // ==================== STATE MANAGEMENT ====================

    public void UpdateAllNPCStates()
    {
        foreach (var npc in npcs.Values)
        {
            npc.UpdateState();
        }
    }

    public void UpdateNPCState(string npcID)
    {
        if (npcs.TryGetValue(npcID, out NPCController npc))
        {
            npc.UpdateState();
        }
    }

    // ==================== QUERIES ====================

    /// <summary>
    /// Get NPC by ID
    /// </summary>
    public NPCController GetNPC(string npcID)
    {
        npcs.TryGetValue(npcID, out NPCController npc);
        return npc;
    }

    /// <summary>
    /// Get all NPCs with specific state
    /// </summary>
    public List<NPCController> GetNPCsByState(NPCState state)
    {
        return npcs.Values.Where(npc => npc.CurrentState == state).ToList();
    }

    /// <summary>
    /// Get all available NPCs
    /// </summary>
    public List<NPCController> GetAvailableNPCs()
    {
        return GetNPCsByState(NPCState.Available);
    }

    /// <summary>
    /// Check if specific NPC is available
    /// </summary>
    public bool IsNPCAvailable(string npcID)
    {
        if (npcs.TryGetValue(npcID, out NPCController npc))
        {
            return npc.IsAvailable;
        }
        return false;
    }

    /// <summary>
    /// Get total NPC count
    /// </summary>
    public int GetNPCCount()
    {
        return npcs.Count;
    }

    /// <summary>
    /// Get count of NPCs in specific state
    /// </summary>
    public int GetNPCCountByState(NPCState state)
    {
        return npcs.Values.Count(npc => npc.CurrentState == state);
    }

    // ==================== ACTIONS ====================

    /// <summary>
    /// Attempt to interact with NPC by ID
    /// </summary>
    public void InteractWithNPC(string npcID)
    {
        if (npcs.TryGetValue(npcID, out NPCController npc))
        {
            npc.TryInteract();
        }
        else
        {
            Debug.LogWarning($"[NPCManager] NPC {npcID} not found");
        }
    }

    /// <summary>
    /// Reset specific NPC
    /// </summary>
    public void ResetNPC(string npcID)
    {
        if (npcs.TryGetValue(npcID, out NPCController npc))
        {
            npc.ResetNPC();
            DebugLog($"Reset NPC: {npcID}");
        }
    }

    /// <summary>
    /// Reset all NPCs
    /// </summary>
    public void ResetAllNPCs()
    {
        foreach (var npc in npcs.Values)
        {
            npc.ResetNPC();
        }
        DebugLog("Reset all NPCs");
    }

    // ==================== DEBUG ====================

    public void DebugPrintAllNPCs()
    {
        Debug.Log("=== ALL NPCs ===");
        foreach (var kvp in npcs)
        {
            var npc = kvp.Value;
            Debug.Log($"{npc.NPCID} ({npc.NPCName}) - State: {npc.CurrentState}");
        }
    }

    public Dictionary<NPCState, int> GetStateStatistics()
    {
        var stats = new Dictionary<NPCState, int>();

        foreach (NPCState state in System.Enum.GetValues(typeof(NPCState)))
        {
            stats[state] = GetNPCCountByState(state);
        }

        return stats;
    }

    private void DebugLog(string message)
    {
        if (showDebugInfo)
        {
            Debug.Log($"[NPCManager] {message}");
        }
    }
}