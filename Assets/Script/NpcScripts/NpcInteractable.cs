using UnityEngine;

public class NPCInteractable : MonoBehaviour, IInteractable
{
    private NPCMovementController movementController;
    private bool hasRegistered = false;

    private void Awake()
    {
        movementController = GetComponent<NPCMovementController>();
    }

    private void Update()
    {
        if (!hasRegistered && GameManager.Instance != null)
        {
            GameManager.Instance.RegisterNPC(movementController);
            hasRegistered = true;
        }
    }

    public string GetPromptText()
    {
        if (!GameManager.Instance.hasActiveOrder)
        {
            return "[E] Take Order";
        }

        if (GameManager.Instance.hasFinishedLapida)
        {
            return "[E] Give Lapida";
        }

        return "Crafting in progress...";
    }

    public void Interact()
    {
        if (!GameManager.Instance.hasActiveOrder)
        {
            GameManager.Instance.AcceptOrder(movementController.npcData);
        }
        else if (GameManager.Instance.hasFinishedLapida)
        {
            GameManager.Instance.TryDeliverLapida();
        }
    }
}