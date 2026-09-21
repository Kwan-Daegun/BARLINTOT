using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCKarenderya : MonoBehaviour, IInteractable
{
    public enum AI_STATE
    {
        IDLING,
        COOKING,
        SERVING
    }

    public AI_STATE current_state = AI_STATE.IDLING;
    private NavMeshAgent agent;

    public Transform standLocation, cookingLocation;

    private void Awake()
    {
        agent = gameObject.GetComponent<NavMeshAgent>();
        if (agent == null) Debug.LogError($"{gameObject.name} does not have a NavMeshAgent!");
    }

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
        if (InDestination())
        {
            return;
        }

        agent.SetDestination(standLocation.position);
    }

    private void Cooking()
    {
        if (InDestination())
        {
            return;
        }

        agent.SetDestination(cookingLocation.position);
    }

    private void Serve()
    {
        
    }

    public void SetDestination(Vector3 target)
    {
        agent.SetDestination(target);
    }

    public bool InDestination()
    {
        return Vector3.Distance(transform.position, agent.destination) < 2f;
    }

    public void Interact()
    {
        // Show food menu
    }

    public string GetPromptText()
    {
        return "[E] Buy Food";
    }
}
