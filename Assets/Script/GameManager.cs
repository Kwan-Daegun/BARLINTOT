using UnityEngine;

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
}