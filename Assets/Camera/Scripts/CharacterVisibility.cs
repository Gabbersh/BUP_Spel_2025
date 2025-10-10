using UnityEngine;

public class CharacterVisibility : MonoBehaviour
{
    public PointOfInterest poi;
    public float activationDistance = 0.1f;

    private Renderer characterRenderer;

    void Start()
    {
        characterRenderer = GetComponent<Renderer>();
        characterRenderer.enabled = false; // start hidden
    }

    void Update()
    {
        if (characterRenderer == null) return;

        if (poi == null || poi.targetCamera == null) return;

        bool poiActive = poi.targetCamera.IsInPOI;

        // Just check if POI is active, don't worry about exact distance during dialogue
        // This way character shows as soon as camera starts moving to POI
        characterRenderer.enabled = poiActive;
    }
}
