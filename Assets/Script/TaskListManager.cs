using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sequential task list shown on the left side of the screen.
/// Add this to any empty GameObject (e.g. "GameManager").
/// Steps are completed in order, top to bottom. Add more steps later in the Inspector.
/// It starts automatically once the WakeUpIntro finishes.
/// </summary>
public class TaskListManager : MonoBehaviour
{
    public static TaskListManager Instance { get; private set; }

    [System.Serializable]
    public class TaskStep
    {
        public string title = "New task";

        [Tooltip("Items that must be collected for this step. Names must match the 'Item Name' on each PickupItem.")]
        public string[] requiredItems;

        [Tooltip("Where the path guide leads the player. Leave empty for no guide.")]
        public Transform target;

        [Tooltip("Complete this step when the player reaches the target.")]
        public bool completeOnReachTarget;

        public float arriveDistance = 3f;
    }

    [Header("Tasks (completed in order, top to bottom)")]
    public List<TaskStep> steps = new List<TaskStep>();

    [Header("Start")]
    public bool startAfterWakeUp = true;
    public string playerTag = "Player";

    [Header("Display (left side)")]
    [Tooltip("Show future tasks greyed out. Off = they appear only when they become active.")]
    public bool showUpcomingTasks = false;
    public int fontSize = 26;
    public float panelWidth = 460f;
    public float leftMargin = 40f;

    [Header("Sound")]
    public AudioClip stepCompleteSound;

    public bool Started { get; private set; }
    public bool AllDone { get; private set; }
    public int CurrentIndex { get; private set; }

    private readonly HashSet<string> inventory =
        new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    private PathGuide pathGuide;
    private GameObject canvasObj;
    private Text listText;
    private Transform player;
    private WakeUpIntro intro;

    // Pre-fills the list when you first add the component
    private void Reset()
    {
        steps = new List<TaskStep>
        {
            new TaskStep
            {
                title = "Gather your things",
                requiredItems = new[]
                {
                    "House Key", "Gate Key", "Shop Key",
                    "Wallet Bag", "Materials Bag", "Notebook"
                }
            },
            new TaskStep
            {
                title = "Go to the shop",
                completeOnReachTarget = true,
                arriveDistance = 3f
            }
        };
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        pathGuide = GetComponent<PathGuide>();
        if (pathGuide == null) pathGuide = gameObject.AddComponent<PathGuide>();

        BuildUI();
        canvasObj.SetActive(false);

        if (startAfterWakeUp)
        {
#if UNITY_2023_1_OR_NEWER
            intro = FindFirstObjectByType<WakeUpIntro>();
#else
            intro = FindObjectOfType<WakeUpIntro>();
#endif
        }

        if (intro != null) intro.OnFinished += BeginTasks;
        else BeginTasks();
    }

    private void OnDestroy()
    {
        if (intro != null) intro.OnFinished -= BeginTasks;
        if (Instance == this) Instance = null;
    }

    // ---------------------------------------------------------------- Flow

    public void BeginTasks()
    {
        if (Started) return;
        Started = true;
        canvasObj.SetActive(true);

        if (steps.Count == 0)
        {
            AllDone = true;
            RefreshUI();
            return;
        }

        ActivateStep(0);
    }

    private void Update()
    {
        if (!Started || AllDone || steps.Count == 0) return;

        TaskStep step = steps[CurrentIndex];
        bool hasItems = step.requiredItems != null && step.requiredItems.Length > 0;

        // A step with no items and no reach-target is completed manually
        if (!hasItems && !step.completeOnReachTarget) return;

        if (hasItems && !HasAllItems(step)) return;
        if (step.completeOnReachTarget && !ReachedTarget(step)) return;

        CompleteCurrentStep();
    }

    /// <summary>Call this yourself for steps that complete by some custom event.</summary>
    public void CompleteCurrentStep()
    {
        if (!Started || AllDone) return;

        if (stepCompleteSound != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(stepCompleteSound, Camera.main.transform.position);

        CurrentIndex++;

        if (CurrentIndex >= steps.Count)
        {
            AllDone = true;
            pathGuide.SetTarget(null);
            RefreshUI();
        }
        else
        {
            ActivateStep(CurrentIndex);
        }
    }

    private void ActivateStep(int index)
    {
        CurrentIndex = index;
        TaskStep step = steps[index];

        if (step.completeOnReachTarget && step.target == null)
            Debug.LogWarning($"TaskListManager: step '{step.title}' needs a Target assigned, otherwise it can't be completed.");

        pathGuide.SetTarget(step.target);
        RefreshUI();
    }

    // ---------------------------------------------------------------- Items

    public void AddItem(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName)) return;
        inventory.Add(itemName.Trim());
        RefreshUI();
    }

    public bool HasItem(string itemName)
    {
        return !string.IsNullOrWhiteSpace(itemName) && inventory.Contains(itemName.Trim());
    }

    private bool HasAllItems(TaskStep step)
    {
        foreach (string item in step.requiredItems)
        {
            if (!HasItem(item)) return false;
        }
        return true;
    }

    private bool ReachedTarget(TaskStep step)
    {
        if (step.target == null) return false;

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag(playerTag);
            if (p == null) return false;
            player = p.transform;
        }

        Vector3 flat = step.target.position - player.position;
        flat.y = 0f;
        return flat.magnitude <= step.arriveDistance;
    }

    // ---------------------------------------------------------------- UI

    private void RefreshUI()
    {
        if (listText == null) return;

        const string doneColor = "#8FD694";     // green-ish
        const string greyColor = "#8F8F8F";

        StringBuilder sb = new StringBuilder();
        sb.Append("<b>TASKS</b>\n\n");

        for (int i = 0; i < steps.Count; i++)
        {
            if (i > CurrentIndex && !showUpcomingTasks) break;

            TaskStep step = steps[i];
            bool done = AllDone || i < CurrentIndex;
            bool current = !AllDone && i == CurrentIndex;

            if (done)
            {
                sb.Append($"<color={doneColor}>[x] {step.title}</color>\n");
            }
            else if (current)
            {
                sb.Append($"<b>> {step.title}</b>\n");

                if (step.requiredItems != null)
                {
                    foreach (string item in step.requiredItems)
                    {
                        if (HasItem(item))
                            sb.Append($"      <color={greyColor}>[x] {item}</color>\n");
                        else
                            sb.Append($"      [ ] {item}\n");
                    }
                }
            }
            else
            {
                sb.Append($"<color={greyColor}>[ ] {step.title}</color>\n");
            }

            sb.Append("\n");
        }

        listText.text = sb.ToString().TrimEnd();
    }

    private void BuildUI()
    {
        canvasObj = new GameObject("TaskCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Panel on the left, vertically centered, grows with its content
        GameObject panel = new GameObject("TaskPanel");
        panel.transform.SetParent(canvasObj.transform, false);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.45f);
        bg.raycastTarget = false;

        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0f, 0.5f);
        prt.anchorMax = new Vector2(0f, 0.5f);
        prt.pivot = new Vector2(0f, 0.5f);
        prt.anchoredPosition = new Vector2(leftMargin, 0f);
        prt.sizeDelta = new Vector2(panelWidth, 100f);

        VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 14, 14);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        GameObject textObj = new GameObject("TaskText");
        textObj.transform.SetParent(panel.transform, false);

        listText = textObj.AddComponent<Text>();
        listText.fontSize = fontSize;
        listText.color = Color.white;
        listText.alignment = TextAnchor.UpperLeft;
        listText.horizontalOverflow = HorizontalWrapMode.Wrap;
        listText.verticalOverflow = VerticalWrapMode.Overflow;
        listText.supportRichText = true;
        listText.raycastTarget = false;

#if UNITY_2022_2_OR_NEWER
        listText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        listText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
    }
}