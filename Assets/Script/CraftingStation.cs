using UnityEngine;

public class CraftingStation : MonoBehaviour, IInteractable
{
    public int stationStepNumber;
    public string stationName;
    private bool isCompleted = false;

    public string GetPromptText()
    {
        if (!GameManager.Instance.hasActiveOrder)
        {
            return "Need an order first";
        }
        if (isCompleted)
        {
            return stationName + " (Done)";
        }
        if (GameManager.Instance.currentCraftingStep != stationStepNumber - 1)
        {
            return "Finish previous step first";
        }

        return "[E] Use " + stationName;
    }

    public void Interact()
    {
        if (isCompleted) return;

        if (GameManager.Instance.TryStartCraftingStep(stationStepNumber))
        {
            GameManager.Instance.CompleteCraftingStep(stationStepNumber);
            isCompleted = true;
        }
    }

    public void ResetStation()
    {
        isCompleted = false;
    }
}