using UnityEngine;

public class FollowTargetSpawner : MonoBehaviour
{
    [SerializeField] private Transform target;      // Assign in Inspector
    [SerializeField] private GameObject prefab;     // Object to spawn
    [SerializeField] private float height = 2f;     // Height above target

    private GameObject spawnedObject;

    void Start()
    {
        if (target != null && prefab != null)
        {
            // Spawn the object above the target
            Vector3 spawnPos = target.position + Vector3.up * height;
            spawnedObject = Instantiate(prefab, spawnPos, Quaternion.identity);
        }
    }

    void LateUpdate()
    {
        if (target != null && spawnedObject != null)
        {
            // Keep spawned object above target
            spawnedObject.transform.position = target.position + Vector3.up * height;
        }
    }
}