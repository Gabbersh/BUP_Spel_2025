using UnityEngine;

public class FollowTargetSpawner : MonoBehaviour
{
    [SerializeField] private Transform target;      // Assign in Inspector
    [SerializeField] private GameObject prefab;     // Object to spawn
    [SerializeField] private float height = 60f;    // Height above target
    [SerializeField] private float fixedZ = 12f;    // Fixed Z position

    private GameObject spawnedObject;

    void Start()
    {
        if (target != null && prefab != null)
        {
            // Spawn the object above the target, force Z to fixed value
            Vector3 spawnPos = target.position + Vector3.up * height;
            spawnPos.z = fixedZ;
            spawnedObject = Instantiate(prefab, spawnPos, Quaternion.identity);
        }
    }

    void LateUpdate()
    {
        if (target != null && spawnedObject != null)
        {
            // Keep spawned object above target, force Z to fixed value
            Vector3 newPos = target.position + Vector3.up * height;
            newPos.z = fixedZ;
            spawnedObject.transform.position = newPos;
        }
    }
}
