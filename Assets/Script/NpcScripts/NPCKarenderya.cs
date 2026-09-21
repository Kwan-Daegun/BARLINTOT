using UnityEngine;

public class NPCKarenderya : MonoBehaviour
{
    public enum AI_STATE
    {
        IDLING,
        COOKING,
        SERVING
    }

    public AI_STATE current_state = AI_STATE.IDLING;
    public NPCMovementController movementController;
    public Transform standLocation, cookingLocation;
    public NPCData npcData;

    private void Update()
    {
        switch(current_state)
        {
            case AI_STATE.IDLING: StandBy(); break;
            case AI_STATE.COOKING: Cooking(); break;
            case AI_STATE.SERVING: Serve(); break;
        }
    }

    private void StandBy()
    {
        if (movementController.InDestination())
        {
            return;
        }

        movementController.SetDestination(standLocation.position);
    }

    private void Cooking()
    {
        if (movementController.InDestination())
        {
            return;
        }

        movementController.SetDestination(cookingLocation.position);
    }

    private void Serve()
    {
        
    }
}
