using System;
using TMPro;
using UnityEngine;

public class NPCKarenderya : NPCBase<NPCKarenderya.AI_STATE>, IInteractable
{
    public Action ShowMenu;

    public enum AI_STATE
    {
        LOITERING,
        WAITING,
        COOKING,
        SERVING
    }

    public Transform standLocation, cookingLocation;
    public Transform[] roamLocations;
    public Area3D playerDetector;

    [Header("Roaming")]
    private Timer roamTimer;
    public float roamTimeMin = 3f;
    public float roamTimeMax = 5f;

    private bool playerInside = false;

    [Header("Debug")]
    [SerializeField] private Canvas debugCanvas;
    [SerializeField] private bool showDebug = false;
    [SerializeField] private TextMeshProUGUI distanceLabel, stateLabel;

    protected override void Awake()
    {
        base.Awake();

        if (playerDetector)
        {
            playerDetector.onBodyEnter.AddListener(PlayerEntered);
            playerDetector.onBodyExit.AddListener(PlayerExited);
        }

        debugCanvas.gameObject.SetActive(false);
        if (showDebug) debugCanvas.gameObject.SetActive(true);

        roamTimer = gameObject.AddComponent<Timer>();
        roamTimer.oneShot = true;
        roamTimer.timeout.AddListener(GoToNextRoamLocation);
    }

    private void Update()
    {
        // Debug Code
        distanceLabel.text = "Distance: " + Vector3.Distance(agent.destination, transform.position).ToString("F0");
        stateLabel.text = "State: " + current_state.ToString();
    }

    protected override void UpdateAI()
    {
        switch (current_state)
        {
            case AI_STATE.LOITERING: Procrastinating(); break;
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
        if (playerInside)
        {
            current_state = AI_STATE.WAITING;
            roamTimer.StopTimer();
            return;
        }

        if (roamTimer.IsActive()) return;

        if (InDestination())
        {
            roamTimer.StartTime(UnityEngine.Random.Range(roamTimeMin, roamTimeMax));
        }
    }

    private void GoToNextRoamLocation()
    {
        if (roamLocations == null || roamLocations.Length == 0) return;

        Transform roamTarget = roamLocations[UnityEngine.Random.Range(0, roamLocations.Length)];
        MoveTo(roamTarget);
    }

    private void StandBy()
    {
        if (!playerInside)
        {
            current_state = AI_STATE.LOITERING;
        }

        MoveTo(standLocation);
    }

    private void Cooking()
    {
        MoveTo(cookingLocation);

        if (InDestination())
        {
            // Add a timer or something
        }
    }

    private void Serve()
    {
        MoveTo(standLocation);

        if (InDestination())
        {
            // Place the food
        }
    }

    public void Interact()
    {
        if (!current_state.Equals(AI_STATE.WAITING)) return;
        ShowMenu.Invoke();
    }

    public string GetPromptText()
    {
        return "[E] Buy Food";
    }
}