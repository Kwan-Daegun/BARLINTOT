using System;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCKarenderya : MonoBehaviour, IInteractable
{
    public Action ShowMenu;

    public enum AI_STATE
    {
        IDLING,
        WAITING,
        COOKING,
        SERVING
    }

    public AI_STATE current_state = AI_STATE.IDLING;
    private NavMeshAgent agent;

    public Timer updateTimer;
    public Transform standLocation, cookingLocation;
    public Transform[] roamLocations;
    public Area3D playerDetector;
    public float nearbyThreshold = 2f;

    private bool playerInside = false;

    [Header("Debug")]
    [SerializeField] private Canvas debugCanvas;
    [SerializeField] private bool showDebug = false;

    [SerializeField] private TextMeshProUGUI distanceLabel, stateLabel;

    private void Awake()
    {
        agent = gameObject.GetComponent<NavMeshAgent>();
        if (agent == null) Debug.LogError($"{gameObject.name} does not have a NavMeshAgent!");

        if (playerDetector)
        {
            playerDetector.onBodyEnter.AddListener(PlayerEntered);
            playerDetector.onBodyExit.AddListener(PlayerExited);
        }

        debugCanvas.gameObject.SetActive(false);
        if (showDebug)
        {
            debugCanvas.gameObject.SetActive(true);
        }

        if (updateTimer)
        {
            updateTimer.StartTime();
            updateTimer.timeout.AddListener(UpdateAI);
        }
    }

    private void Update()
    {
        // Debug Code
        distanceLabel.text = "Distance: " + Vector3.Distance(agent.destination, transform.position).ToString("F0");
        stateLabel.text = "State: " + current_state.ToString();
    }

    private void UpdateAI()
    {
        switch(current_state)
        {
            case AI_STATE.IDLING: Procrastinating(); break;
            case AI_STATE.WAITING: StandBy(); break;
            case AI_STATE.COOKING: Cooking(); break;
            case AI_STATE.SERVING: Serve(); break;
        }        
    }

    private void PlayerEntered(GameObject body)
    {
        if (!body.CompareTag("Player")) return;

        playerInside = true;
    }

    private void PlayerExited(GameObject body)
    {
        if (!body.CompareTag("Player")) return;

        playerInside = false;
    }

    private void Procrastinating()
    {
        // Roam around
        if (playerInside)
        {
            current_state = AI_STATE.WAITING;
        }
    }

    private void StandBy()
    {
        if (!playerInside)
        {
            current_state = AI_STATE.IDLING;
        }

        agent.SetDestination(standLocation.position);
    }

    private void Cooking()
    {
        agent.SetDestination(cookingLocation.position);

        if (InDestination())
        {
            // Add a timer or something
        }
    }

    private void Serve()
    {
        agent.SetDestination(standLocation.position);

        if (InDestination())
        {
            // Place the food
        }
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
        ShowMenu.Invoke();
    }

    public string GetPromptText()
    {
        return "[E] Buy Food";
    }
}
