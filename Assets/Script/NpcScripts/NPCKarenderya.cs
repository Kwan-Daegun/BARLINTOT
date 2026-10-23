using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class NPCKarenderya : NPCBase<NPCKarenderya.AI_STATE>, IInteractable
{
    public enum AI_STATE
    {
        LOITERING,
        WAITING,
        COOKING,
        SERVING
    }

    [Header("NPC Karenderya")]
    public KarenderyaShop shopMenu;
    private Item food;

    public Transform standLocation, cookingLocation;
    public Transform[] roamLocations;
    public Area3D playerDetector;

    [Header("Roaming")]
    private Timer roamTimer;
    public float roamTimeMin = 3f;
    public float roamTimeMax = 5f;

    private bool playerInside = false;

    [Header("Cooking & Serving")]
    public float cookTimeMin = 4f;
    public float cookTimeMax = 8f;
    public Transform foodPlacement; // where the food appears (counter, tray, etc.)
    public UnityEvent<Item> foodServed; // hook up spawning/giving the food
    private Timer cookTimer;

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

        cookTimer = gameObject.AddComponent<Timer>();
        cookTimer.oneShot = true;
        cookTimer.timeout.AddListener(OnCookingFinished);

        shopMenu.foodBought.AddListener(OrderFood);
    }

    private void Update()
    {
        // Debug Code
        distanceLabel.text = "Distance: " + Vector3.Distance(agent.destination, transform.position).ToString("F0");
        stateLabel.text = "State: " + currentState.ToString();
    }

    protected override void UpdateAI()
    {
        switch (currentState)
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
            currentState = AI_STATE.WAITING;
            roamTimer.StopTimer();
            return;
        }

        if (roamTimer.IsActive()) return;

        if (InDestination())
        {
            roamTimer.StartTime(Random.Range(roamTimeMin, roamTimeMax));
        }
    }

    private void GoToNextRoamLocation()
    {
        if (roamLocations == null || roamLocations.Length == 0) return;

        Transform roamTarget = roamLocations[Random.Range(0, roamLocations.Length)];
        MoveTo(roamTarget);
    }

    private void StandBy()
    {
        if (!playerInside)
        {
            currentState = AI_STATE.LOITERING;
        }

        MoveTo(standLocation);
    }

    private void Cooking()
    {
        MoveTo(cookingLocation);

        // Only start once we've arrived, and only once
        if (InDestination() && !cookTimer.IsActive())
        {
            cookTimer.StartTime(Random.Range(cookTimeMin, cookTimeMax));
        }
    }

    private void OnCookingFinished()
    {
        currentState = AI_STATE.SERVING;
    }

    private void Serve()
    {
        MoveTo(standLocation);

        if (InDestination())
        {
            PlaceFood();
            food = null;
            currentState = AI_STATE.WAITING;
        }
    }

    private void PlaceFood()
    {
        // Todo: Spawn Prefab here
        foodServed.Invoke(food);
    }

    private void OrderFood(Item foodOrder)
    {
        if (currentState != AI_STATE.WAITING) return;

        food = foodOrder;
        currentState = AI_STATE.COOKING;
    }

    public void Interact()
    {
        if (!currentState.Equals(AI_STATE.WAITING)) return;
        shopMenu.OpenMenu(true);

        // Wait for player to buy food, then switch to Cooking
    }

    public string GetPromptText()
    {
        return "[E] Buy Food";
    }
}