using System.Collections;
using UnityEngine;

public class FollowTargetSpawner : MonoBehaviour
{
    [SerializeField] private Transform target;      // Assign in Inspector
    [SerializeField] private GameObject prefab;     // Object to spawn
    [SerializeField] private float height = 60f;    // Height above target
    [SerializeField] private float fixedZ = 12f;    // Fixed Z position
    [SerializeField] private float spawnDelay = 5f; // Delay in seconds before spawning

    private GameObject spawnedObject;

    void Start()
    {
        StartCoroutine(SpawnAfterDelay());
    }

    private IEnumerator SpawnAfterDelay()
    {
        yield return new WaitForSeconds(spawnDelay);

        if (target != null && prefab != null)
        {
            Vector3 spawnPos = target.position + Vector3.up * height;
            spawnPos.z = fixedZ;
            spawnedObject = Instantiate(prefab, spawnPos, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning($"{nameof(FollowTargetSpawner)} missing target or prefab on {gameObject.name}");
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
