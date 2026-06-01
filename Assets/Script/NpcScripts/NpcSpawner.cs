using UnityEngine;
using System.Collections.Generic;

public class NPCSpawner : MonoBehaviour
{
    public GameObject baseNPCPrefab;
    public Transform spawnPoint;
    public Transform counterPoint;
    public Transform exitPoint;
    public List<NPCData> allPossibleNPCs;
    public float spawnDelay = 2f;

    private void Start()
    {
        SpawnRandomNPC();
    }

    public void SpawnRandomNPC()
    {
        if (DayNightManager.Instance != null && DayNightManager.Instance.isNightTime) return;

        if (allPossibleNPCs.Count == 0) return;

        int randomIndex = Random.Range(0, allPossibleNPCs.Count);
        NPCData selectedData = allPossibleNPCs[randomIndex];

        GameObject newNPC = Instantiate(baseNPCPrefab, spawnPoint.position, Quaternion.identity);

        if (newNPC.TryGetComponent(out NPCMovementController controller))
        {
            controller.Initialize(selectedData, counterPoint, exitPoint);
            controller.onExited += HandleNPCExited;
        }
    }

    private void HandleNPCExited()
    {
        Invoke(nameof(SpawnRandomNPC), spawnDelay);
    }

    public void ResumeSpawning()
    {
        if (!IsInvoking(nameof(SpawnRandomNPC)))
        {
            SpawnRandomNPC();
        }
    }
}