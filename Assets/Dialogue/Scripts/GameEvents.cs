using System;
using UnityEngine;

/// <summary>
/// Static event system for decoupling game systems.
/// Makes it easy to listen for game events without tight coupling.
/// </summary>
public static class GameEvents
{
    // ==================== DIALOGUE EVENTS ====================

    /// <summary>Fired when any dialogue starts. Args: dialogueID</summary>
    public static event Action<string> OnDialogueStarted;

    /// <summary>Fired when any dialogue ends. Args: dialogueID</summary>
    public static event Action<string> OnDialogueEnded;

    /// <summary>Fired when a choice is made. Args: dialogueID, choiceIndex</summary>
    public static event Action<string, int> OnChoiceMade;

    // ==================== NPC EVENTS ====================

    /// <summary>Fired when NPC state changes. Args: npcID, newState</summary>
    public static event Action<string, NPCState> OnNPCStateChanged;

    /// <summary>Fired when player interacts with NPC. Args: npcID</summary>
    public static event Action<string> OnNPCInteracted;

    /// <summary>Fired when NPC becomes available. Args: npcID</summary>
    public static event Action<string> OnNPCBecameAvailable;

    /// <summary>Fired when NPC becomes unavailable. Args: npcID</summary>
    public static event Action<string> OnNPCBecameUnavailable;

    // ==================== PROGRESSION EVENTS ====================

    /// <summary>Fired when game progression updates</summary>
    public static event Action OnProgressionChanged;

    /// <summary>Fired when a flag is set. Args: flagName, value</summary>
    public static event Action<string, string> OnFlagChanged;

    /// <summary>Fired when entering a new game phase. Args: phaseName</summary>
    public static event Action<string> OnGamePhaseChanged;

    // ==================== CAMERA EVENTS ====================

    /// <summary>Fired when camera moves to POI. Args: poiName</summary>
    public static event Action<string> OnCameraMovedToPOI;

    /// <summary>Fired when camera returns to rail</summary>
    public static event Action OnCameraReturnedToRail;

    // ==================== TRIGGER METHODS ====================

    public static void TriggerDialogueStarted(string dialogueID)
    {
        OnDialogueStarted?.Invoke(dialogueID);
    }

    public static void TriggerDialogueEnded(string dialogueID)
    {
        OnDialogueEnded?.Invoke(dialogueID);
    }

    public static void TriggerChoiceMade(string dialogueID, int choiceIndex)
    {
        OnChoiceMade?.Invoke(dialogueID, choiceIndex);
    }

    public static void TriggerNPCStateChanged(string npcID, NPCState newState)
    {
        OnNPCStateChanged?.Invoke(npcID, newState);
    }

    public static void TriggerNPCInteracted(string npcID)
    {
        OnNPCInteracted?.Invoke(npcID);
    }

    public static void TriggerNPCBecameAvailable(string npcID)
    {
        OnNPCBecameAvailable?.Invoke(npcID);
    }

    public static void TriggerNPCBecameUnavailable(string npcID)
    {
        OnNPCBecameUnavailable?.Invoke(npcID);
    }

    public static void TriggerProgressionChanged()
    {
        OnProgressionChanged?.Invoke();
    }

    public static void TriggerFlagChanged(string flagName, string value)
    {
        OnFlagChanged?.Invoke(flagName, value);
    }

    public static void TriggerGamePhaseChanged(string phaseName)
    {
        OnGamePhaseChanged?.Invoke(phaseName);
    }

    public static void TriggerCameraMovedToPOI(string poiName)
    {
        OnCameraMovedToPOI?.Invoke(poiName);
    }

    public static void TriggerCameraReturnedToRail()
    {
        OnCameraReturnedToRail?.Invoke();
    }

    // ==================== CLEANUP ====================

    /// <summary>
    /// Clear all event subscriptions (useful for scene changes)
    /// </summary>
    public static void ClearAllEvents()
    {
        OnDialogueStarted = null;
        OnDialogueEnded = null;
        OnChoiceMade = null;
        OnNPCStateChanged = null;
        OnNPCInteracted = null;
        OnNPCBecameAvailable = null;
        OnNPCBecameUnavailable = null;
        OnProgressionChanged = null;
        OnFlagChanged = null;
        OnGamePhaseChanged = null;
        OnCameraMovedToPOI = null;
        OnCameraReturnedToRail = null;
    }
}

/// <summary>
/// Possible states for NPCs
/// </summary>
public enum NPCState
{
    Locked,         // Not yet available
    Available,      // Can interact
    Talking,        // Currently in dialogue
    Completed,      // Finished their story
    WaitingForChoice // Needs correct choice to progress (deprecated - kept for compatibility)
}