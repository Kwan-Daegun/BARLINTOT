/*using UnityEngine;
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
}*/


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

    [Header("Shop")]
    [Tooltip("Customers only come while this shutter is open. Drag the object that has your ShutterButton.")]
    public ShutterButton shutter;

    [Header("Pickup")]
    [Tooltip("After the lapida is finished, the customer comes back to collect it after this many seconds.")]
    public float pickupDelay = 3f;

    private NPCMovementController activeNPC; // only one customer at a time
    private float pickupTimer;

    private void OnEnable()
    {
        if (shutter != null) shutter.OnStateChanged += HandleShutterChanged;
    }

    private void OnDisable()
    {
        if (shutter != null) shutter.OnStateChanged -= HandleShutterChanged;
    }

    private void Start()
    {
        // Does nothing if the shop is closed; the shutter opening will start customers instead
        SpawnRandomNPC();
    }

    private void Update()
    {
        // A finished lapida: the same customer comes back to collect it
        GameManager gm = GameManager.Instance;
        bool wantPickup = gm != null && gm.hasFinishedLapida && gm.currentOrderData != null
                          && activeNPC == null && CanSpawnNow();

        if (!wantPickup)
        {
            pickupTimer = 0f;
            return;
        }

        pickupTimer += Time.deltaTime;
        if (pickupTimer >= pickupDelay)
        {
            pickupTimer = 0f;
            SpawnNPC(gm.currentOrderData, true);
        }
    }

    private bool CanSpawnNow()
    {
        if (shutter != null && !shutter.IsOpen) return false;                                   // shop is closed
        if (DayNightManager.Instance != null && DayNightManager.Instance.isNightTime) return false;
        return true;
    }

    public void SpawnRandomNPC()
    {
        if (activeNPC != null) return;                                                          // already a customer
        if (!CanSpawnNow()) return;

        // An order is in progress: no new customers until it's finished
        if (GameManager.Instance != null && GameManager.Instance.hasActiveOrder) return;

        if (allPossibleNPCs.Count == 0) return;

        int randomIndex = Random.Range(0, allPossibleNPCs.Count);
        SpawnNPC(allPossibleNPCs[randomIndex], false);
    }

    private void SpawnNPC(NPCData data, bool forPickup)
    {
        GameObject newNPC = Instantiate(baseNPCPrefab, spawnPoint.position, Quaternion.identity);

        if (!newNPC.TryGetComponent(out NPCMovementController controller))
        {
            Destroy(newNPC);
            return;
        }

        if (forPickup && newNPC.TryGetComponent(out NPCInteractable interactable))
            interactable.MarkAsPickup();

        activeNPC = controller;
        controller.Initialize(data, counterPoint, exitPoint);
        controller.onExited += HandleNPCExited;
    }

    private void HandleNPCExited()
    {
        activeNPC = null;
        Invoke(nameof(SpawnRandomNPC), spawnDelay);
    }

    private void HandleShutterChanged(bool isOpen)
    {
        if (isOpen)
        {
            // Shop just opened: the first customer shows up after a short delay
            if (!IsInvoking(nameof(SpawnRandomNPC)))
                Invoke(nameof(SpawnRandomNPC), spawnDelay);
        }
        else
        {
            // Shop closed: no new customers, and anyone on their way in leaves
            CancelInvoke(nameof(SpawnRandomNPC));
            if (activeNPC != null) activeNPC.ServeOrReject();
        }
    }

    public void ResumeSpawning()
    {
        if (!IsInvoking(nameof(SpawnRandomNPC)))
        {
            SpawnRandomNPC();
        }
    }
}