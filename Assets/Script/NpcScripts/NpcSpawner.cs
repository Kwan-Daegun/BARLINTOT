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
}*/


using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Decides who visits the shop each day.
/// - A paid order that isn't finished: the SAME customer comes back each day to ask about it.
/// - A finished lapida: the customer comes back to collect it.
/// - Otherwise: a NEW customer who hasn't had their order completed yet.
/// Customers only come while the shop shutter is open and it's daytime.
/// </summary>
public class NPCSpawner : MonoBehaviour
{
    private enum VisitType { NewOrder, Progress, Pickup }

    [Tooltip("Used for every customer that has no 'Prefab Override' in their NPCData.")]
    public GameObject baseNPCPrefab;
    public Transform spawnPoint;
    public Transform counterPoint;
    public Transform exitPoint;
    [Tooltip("Your customers (the NPCData assets).")]
    public List<NPCData> allPossibleNPCs;
    public float spawnDelay = 2f;

    [Header("Shop")]
    [Tooltip("Customers only come while this shutter is open. Drag the object that has your ShutterButton.")]
    public ShutterButton shutter;

    [Header("Pickup")]
    [Tooltip("After the lapida is finished, the customer comes back to collect it after this many seconds.")]
    public float pickupDelay = 3f;

    /// <summary>Day number. Goes up each time DayNightManager starts a new day.</summary>
    public int CurrentDay { get; private set; } = 1;

    private NPCMovementController activeNPC; // only one customer at a time
    private float pickupTimer;
    private bool visitedToday;               // today's visitor has already reached the counter
    private readonly HashSet<NPCData> completedCustomers = new HashSet<NPCData>();

    private void OnEnable()
    {
        if (shutter != null) shutter.OnStateChanged += HandleShutterChanged;
        GameManager.OnOrderDelivered += HandleOrderDelivered;
    }

    private void OnDisable()
    {
        if (shutter != null) shutter.OnStateChanged -= HandleShutterChanged;
        GameManager.OnOrderDelivered -= HandleOrderDelivered;
    }

    private void Start()
    {
        // Does nothing if the shop is closed; opening the shutter will start the day's visitor instead
        TrySpawnTodaysVisitor();
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
            SpawnNPC(gm.currentOrderData, VisitType.Pickup);
        }
    }

    // ---------------------------------------------------------------- Who comes today

    /// <summary>DayNightManager calls this when a new day starts.</summary>
    public void ResumeSpawning()
    {
        CurrentDay++;
        visitedToday = false;
        TrySpawnTodaysVisitor();
    }

    public void TrySpawnTodaysVisitor()
    {
        if (activeNPC != null) return;     // already a customer
        if (visitedToday) return;          // today's visitor already came
        if (!CanSpawnNow()) return;

        GameManager gm = GameManager.Instance;

        if (gm != null && gm.hasActiveOrder)
        {
            // A paid order is still being worked on: the same customer comes back for it
            if (gm.orderPaid && gm.currentOrderData != null)
            {
                SpawnNPC(gm.currentOrderData, gm.hasFinishedLapida ? VisitType.Pickup : VisitType.Progress);
            }
            return;
        }

        // No order in progress: a new customer whose order isn't completed yet
        NPCData next = PickNewCustomer();
        if (next != null) SpawnNPC(next, VisitType.NewOrder);
    }

    private NPCData PickNewCustomer()
    {
        List<NPCData> candidates = new List<NPCData>();
        foreach (NPCData data in allPossibleNPCs)
        {
            if (data != null && !completedCustomers.Contains(data)) candidates.Add(data);
        }

        if (candidates.Count == 0) return null; // everyone is done
        return candidates[Random.Range(0, candidates.Count)];
    }

    private bool CanSpawnNow()
    {
        if (shutter != null && !shutter.IsOpen) return false;                                   // shop is closed
        if (DayNightManager.Instance != null && DayNightManager.Instance.isNightTime) return false;
        return true;
    }

    // ---------------------------------------------------------------- Spawning

    private void SpawnNPC(NPCData data, VisitType type)
    {
        // A customer can have their own prefab (their own model, props, sounds, scripts)
        GameObject prefab = data.prefabOverride != null ? data.prefabOverride : baseNPCPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("NPCSpawner: no prefab for " + data.npcName + ". Set 'Base NPC Prefab' or the customer's 'Prefab Override'.");
            return;
        }

        GameObject newNPC = Instantiate(prefab, spawnPoint.position, Quaternion.identity);

        if (!newNPC.TryGetComponent(out NPCMovementController controller))
        {
            Debug.LogWarning("NPCSpawner: the prefab for " + data.npcName + " has no NPCMovementController.");
            Destroy(newNPC);
            return;
        }

        if (newNPC.TryGetComponent(out NPCInteractable interactable))
        {
            if (type == VisitType.Pickup) interactable.MarkAsPickup();
            else if (type == VisitType.Progress) interactable.MarkAsProgressVisit();
        }

        activeNPC = controller;
        controller.Initialize(data, counterPoint, exitPoint);
        controller.onExited += HandleNPCExited;

        // The day's visit only counts once the customer actually reaches the counter
        controller.onReachedCounter += () =>
        {
            if (type != VisitType.Pickup) visitedToday = true;
        };
    }

    private void HandleNPCExited()
    {
        activeNPC = null;
        Invoke(nameof(TrySpawnTodaysVisitor), spawnDelay);
    }

    private void HandleOrderDelivered(NPCData data)
    {
        if (data != null) completedCustomers.Add(data);
    }

    private void HandleShutterChanged(bool isOpen)
    {
        if (isOpen)
        {
            // Shop just opened: today's visitor shows up after a short delay
            if (!IsInvoking(nameof(TrySpawnTodaysVisitor)))
                Invoke(nameof(TrySpawnTodaysVisitor), spawnDelay);
        }
        else
        {
            // Shop closed: nobody new comes, and anyone on their way in leaves
            CancelInvoke(nameof(TrySpawnTodaysVisitor));
            if (activeNPC != null) activeNPC.ServeOrReject();
        }
    }
}