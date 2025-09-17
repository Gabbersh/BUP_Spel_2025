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
        if (poi == null || poi.targetCamera == null) return;

        bool poiActive = poi.targetCamera.IsOverridingToPOI(poi.cameraTarget.position);

        bool cameraAtTarget = Vector3.Distance(poi.targetCamera.transform.position, poi.cameraTarget.position) <= activationDistance;

        characterRenderer.enabled = poiActive && cameraAtTarget;
    }
}
