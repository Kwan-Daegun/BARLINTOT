/*using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int currentCraftingStep = 0;
    public int totalSteps = 3;
    public bool hasFinishedLapida = false;
    public bool hasActiveOrder = false;
    public NPCData currentOrderData;

    private NPCMovementController currentNPC;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterNPC(NPCMovementController npc)
    {
        currentNPC = npc;
    }

    public void AcceptOrder(NPCData data)
    {
        hasActiveOrder = true;
        currentOrderData = data;
    }

    public bool TryStartCraftingStep(int stepNumber)
    {
        if (!hasActiveOrder) return false;

        if (stepNumber == currentCraftingStep + 1)
        {
            return true;
        }
        return false;
    }

    public void CompleteCraftingStep(int stepNumber)
    {
        currentCraftingStep = stepNumber;
        if (currentCraftingStep >= totalSteps)
        {
            hasFinishedLapida = true;
        }
    }

    public bool TryDeliverLapida()
    {
        if (hasFinishedLapida && currentNPC != null)
        {
            currentNPC.ServeOrReject();
            currentNPC = null;
            ResetCrafting();
            return true;
        }
        return false;
    }

    public void RejectCustomer()
    {
        if (currentNPC != null)
        {
            currentNPC.ServeOrReject();
            currentNPC = null;
        }
        ResetCrafting();
    }

    private void ResetCrafting()
    {
        currentCraftingStep = 0;
        hasFinishedLapida = false;
        hasActiveOrder = false;
        currentOrderData = null;

        CraftingStation[] stations = FindObjectsByType<CraftingStation>(FindObjectsSortMode.None);
        foreach (CraftingStation station in stations)
        {
            station.ResetStation();
        }
    }
}*/


using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int currentCraftingStep = 0;
    public int totalSteps = 3;
    public bool hasFinishedLapida = false;
    public bool hasActiveOrder = false;
    public NPCData currentOrderData;

    /// <summary>
    /// True once the customer has paid. A paid order keeps its crafting progress after the customer
    /// leaves, so the lapida can take as long as it needs (even several days).
    /// </summary>
    public bool orderPaid = false;

    /// <summary>Crafting is only allowed once the order has been accepted AND paid.</summary>
    public bool CanCraft => hasActiveOrder && orderPaid;

    public bool HasCustomerPresent => currentNPC != null;

    /// <summary>Fires when the finished lapida is handed to the customer.</summary>
    public static event System.Action<NPCData> OnOrderDelivered;

    private NPCMovementController currentNPC;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void RegisterNPC(NPCMovementController npc)
    {
        currentNPC = npc;
    }

    public void AcceptOrder(NPCData data)
    {
        hasActiveOrder = true;
        currentOrderData = data;
        orderPaid = false;
    }

    /// <summary>Called when the customer hands over the payment. The order now survives the customer leaving.</summary>
    public void MarkOrderPaid()
    {
        orderPaid = true;
    }

    public bool TryStartCraftingStep(int stepNumber)
    {
        if (!CanCraft) return false;

        if (stepNumber == currentCraftingStep + 1)
        {
            return true;
        }
        return false;
    }

    public void CompleteCraftingStep(int stepNumber)
    {
        currentCraftingStep = stepNumber;
        if (currentCraftingStep >= totalSteps)
        {
            hasFinishedLapida = true;

            if (TaskListManager.Instance != null)
                TaskListManager.Instance.SendSignal("LapidaFinished");
        }
    }

    public bool TryDeliverLapida()
    {
        if (hasFinishedLapida && currentNPC != null)
        {
            NPCData delivered = currentOrderData;

            currentNPC.ServeOrReject();
            currentNPC = null;
            ResetCrafting();

            if (TaskListManager.Instance != null)
                TaskListManager.Instance.SendSignal("LapidaDelivered");
            if (OnOrderDelivered != null) OnOrderDelivered.Invoke(delivered);

            return true;
        }
        return false;
    }

    /// <summary>
    /// The current customer leaves (declined, rejected, shutter closed, night time...).
    /// An order that was NOT paid yet is cancelled. A PAID order keeps its crafting progress.
    /// </summary>
    public void RejectCustomer()
    {
        if (currentNPC != null)
        {
            currentNPC.ServeOrReject();
            currentNPC = null;
        }

        if (!orderPaid) ResetCrafting();
    }

    private void ResetCrafting()
    {
        currentCraftingStep = 0;
        hasFinishedLapida = false;
        hasActiveOrder = false;
        orderPaid = false;
        currentOrderData = null;

        CraftingStation[] stations = FindObjectsByType<CraftingStation>(FindObjectsSortMode.None);
        foreach (CraftingStation station in stations)
        {
            station.ResetStation();
        }
    }
}