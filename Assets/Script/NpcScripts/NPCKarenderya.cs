using TMPro;
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
    public float nearbyThreshold = 2f;

    [Header("Debug")]
    [SerializeField] private Canvas debugCanvas;
    [SerializeField] private bool showDebug = false;

    [SerializeField] private TextMeshProUGUI distanceLabel;

    private void Awake()
    {
        agent = gameObject.GetComponent<NavMeshAgent>();
        if (agent == null) Debug.LogError($"{gameObject.name} does not have a NavMeshAgent!");

        debugCanvas.gameObject.SetActive(false);
        if (showDebug)
        {
            debugCanvas.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        switch(current_state)
        {
            case AI_STATE.IDLING: StandBy(); break;
            case AI_STATE.COOKING: Cooking(); break;
            case AI_STATE.SERVING: Serve(); break;
        }

        // Debug Code
        distanceLabel.text = "Distance: " + Vector3.Distance(agent.destination, transform.position).ToString("F0");
    }

    private void StandBy()
    {
        agent.SetDestination(standLocation.position);
    }

    private void Cooking()
    {
        agent.SetDestination(cookingLocation.position);

        if (InDestination())
        {
            
        }
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
        return Vector3.Distance(agent.destination, transform.position) < nearbyThreshold;
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
