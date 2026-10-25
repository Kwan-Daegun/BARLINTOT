/*using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-by-one task system. Shows the CURRENT task only, with a live distance meter.
/// Uses YOUR UI: drag the objects from the Hierarchy into the slots below.
/// Add this to any empty GameObject (e.g. "GameManager").
/// Starts automatically after the WakeUpIntro finishes.
/// </summary>
public class TaskListManager : MonoBehaviour
{
    public static TaskListManager Instance { get; private set; }

    [System.Serializable]
    public class TaskStep
    {
        [Tooltip("Shown in the Title text, e.g. 'Gather your things'.")]
        public string title = "New task";

        [Tooltip("Shown in the Description text, e.g. 'Collect House Key'.")]
        public string description = "";

        [Tooltip("Optional. Item to collect (must match 'Item Name' on a PickupItem). " +
                 "The step completes when it's collected and the distance meter points to it.")]
        public string itemName;

        [Tooltip("Optional. A place to go. If empty and an item is set, the item's position is used.")]
        public Transform target;

        [Tooltip("Complete this step when the player gets close to the target.")]
        public bool completeOnReachTarget;
        public float arriveDistance = 3f;

        [Tooltip("Optional. The step completes when this door gets locked. " +
                 "Drag the object that has the Door script (not the empty target).")]
        public Door lockDoor;

        [Tooltip("Show the distance meter for this step.")]
        public bool showDistance = true;

        [Tooltip("Draw the path line on the ground to the target.")]
        public bool showPathGuide;
    }

    [Header("Your UI (drag from the Hierarchy)")]
    [Tooltip("The object that holds the whole task UI. It is hidden until the wake-up intro ends. " +
             "Best: the 'Canvas' object under HUD > Task.")]
    public GameObject panel;
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text distanceText;

    [Header("Tasks (completed in order, top to bottom)")]
    public List<TaskStep> steps = new List<TaskStep>();

    [Header("Start")]
    public bool startAfterWakeUp = true;
    public string playerTag = "Player";

    [Header("Distance meter")]
    [Tooltip("{0} = distance in meters.")]
    public string distanceFormat = "{0}m away";

    [Header("Feel")]
    public bool animateTransitions = true;
    public Color completedColor = new Color(0.55f, 0.9f, 0.6f);
    [Tooltip("How long the finished task stays green before the next one appears.")]
    public float completedHold = 0.7f;
    public float fadeTime = 0.25f;
    public bool hidePanelWhenDone = true;
    public AudioClip stepCompleteSound;

    [Header("Testing (skip task)")]
    [Tooltip("Lets you skip the current task while testing. Turn OFF before releasing the game.")]
    public bool enableSkip = true;
    public KeyCode skipKey = KeyCode.F9;
    [Tooltip("Optional. A UI Button that skips the current task. Item tasks give you the item automatically.")]
    public Button skipButton;

    public bool Started { get; private set; }
    public bool AllDone { get; private set; }
    public int CurrentIndex { get; private set; }

    private readonly HashSet<string> inventory =
        new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    private PathGuide pathGuide;
    private CanvasGroup canvasGroup;
    private Transform player;
    private Transform currentTarget;
    private WakeUpIntro intro;
    private Color descriptionColor = Color.white;
    private int lastShownMeters = -1;
    private bool transitioning;

    // Pre-fills the list when you add the component or press Reset
    private void Reset()
    {
        string[] items = { "House Key", "Gate Key", "Shop Key", "Wallet Bag", "Materials Bag", "Notebook" };

        steps = new List<TaskStep>();
        foreach (string item in items)
        {
            steps.Add(new TaskStep
            {
                title = "Gather your things",
                description = "Collect " + item,
                itemName = item
            });
        }

        steps.Add(new TaskStep
        {
            title = "Go to the shop",
            description = "Head to the shop",
            completeOnReachTarget = true,
            arriveDistance = 3f,
            showPathGuide = true
        });
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        pathGuide = GetComponent<PathGuide>();
        if (pathGuide == null) pathGuide = gameObject.AddComponent<PathGuide>();

        if (panel == null || titleText == null || descriptionText == null)
            Debug.LogWarning("TaskListManager: drag your Panel, Title Text and Description Text into the slots.");

        if (descriptionText != null) descriptionColor = descriptionText.color;

        if (panel != null)
        {
            canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = panel.AddComponent<CanvasGroup>();
            panel.SetActive(false); // hidden until the intro is over
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipCurrentTask);
            skipButton.gameObject.SetActive(enableSkip);
        }

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
        if (skipButton != null) skipButton.onClick.RemoveListener(SkipCurrentTask);
        if (Instance == this) Instance = null;
    }

    // ---------------------------------------------------------------- Flow

    public void BeginTasks()
    {
        if (Started) return;
        Started = true;

        if (panel != null) panel.SetActive(true);
        if (canvasGroup != null) canvasGroup.alpha = animateTransitions ? 0f : 1f;

        if (steps.Count == 0)
        {
            AllDone = true;
            return;
        }

        StartCoroutine(GoToStep(0, false));
    }

    private void Update()
    {
        if (enableSkip && Input.GetKeyDown(skipKey)) SkipCurrentTask();

        if (!Started || AllDone || transitioning || steps.Count == 0) return;

        EnsurePlayer();
        UpdateDistance();

        TaskStep step = steps[CurrentIndex];
        bool hasItem = !string.IsNullOrWhiteSpace(step.itemName);

        // A step with no item, no reach-target and no door is completed manually
        if (!hasItem && !step.completeOnReachTarget && step.lockDoor == null) return;

        if (hasItem && !HasItem(step.itemName)) return;
        if (step.lockDoor != null && !step.lockDoor.IsLocked) return;
        if (step.completeOnReachTarget && !ReachedTarget(step)) return;

        CompleteCurrentStep();
    }

    /// <summary>Call this yourself for steps that complete by some custom event.</summary>
    public void CompleteCurrentStep()
    {
        if (!Started || AllDone || transitioning) return;

        if (stepCompleteSound != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(stepCompleteSound, Camera.main.transform.position);

        StartCoroutine(GoToStep(CurrentIndex + 1, true));
    }

    /// <summary>
    /// For testing: completes the current task right away.
    /// If it's an item task, you automatically get the item (and it disappears from the world).
    /// </summary>
    public void SkipCurrentTask()
    {
        if (!enableSkip) return;
        if (!Started || AllDone || transitioning || steps.Count == 0) return;

        TaskStep step = steps[CurrentIndex];

        if (!string.IsNullOrWhiteSpace(step.itemName) && !HasItem(step.itemName))
        {
            EnsurePlayer();
            Vector3 from = player != null ? player.position : Vector3.zero;
            PickupItem item = PickupItem.FindByName(step.itemName, from);

            if (item != null) item.ForceCollect(); // adds the item and removes it from the scene
            else AddItem(step.itemName);
        }

        StartCoroutine(GoToStep(CurrentIndex + 1, false, true));
    }

    private IEnumerator GoToStep(int index, bool showCompleted, bool instant = false)
    {
        transitioning = true;

        // Little "done!" moment: the finished task turns green, then fades away
        if (showCompleted && animateTransitions)
        {
            if (descriptionText != null) descriptionText.color = completedColor;
            yield return new WaitForSeconds(completedHold);
            yield return Fade(1f, 0f);
        }

        if (index >= steps.Count)
        {
            AllDone = true;
            currentTarget = null;
            pathGuide.SetTarget(null);
            if (hidePanelWhenDone && panel != null) panel.SetActive(false);
            transitioning = false;
            yield break;
        }

        ApplyStep(index);

        if (animateTransitions && !instant) yield return Fade(0f, 1f);
        else if (canvasGroup != null) canvasGroup.alpha = 1f;

        transitioning = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (canvasGroup == null) yield break;

        float duration = Mathf.Max(0.01f, fadeTime);
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, time / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private void ApplyStep(int index)
    {
        CurrentIndex = index;
        TaskStep step = steps[index];

        if (titleText != null) titleText.text = step.title;
        if (descriptionText != null)
        {
            descriptionText.text = step.description;
            descriptionText.color = descriptionColor;
        }

        // Work out where this step points to
        EnsurePlayer();
        currentTarget = step.target;
        if (currentTarget == null && step.lockDoor != null) currentTarget = step.lockDoor.transform;

        bool hasItem = !string.IsNullOrWhiteSpace(step.itemName);
        if (currentTarget == null && hasItem && !HasItem(step.itemName))
        {
            Vector3 from = player != null ? player.position : Vector3.zero;
            PickupItem item = PickupItem.FindByName(step.itemName, from);

            if (item != null) currentTarget = item.transform;
            else Debug.LogWarning($"TaskListManager: no PickupItem named '{step.itemName}' found in the scene.");
        }

        if (step.completeOnReachTarget && currentTarget == null)
            Debug.LogWarning($"TaskListManager: step '{step.description}' needs a Target assigned, otherwise it can't be completed.");

        pathGuide.SetTarget(step.showPathGuide ? currentTarget : null);

        if (distanceText != null)
            distanceText.gameObject.SetActive(step.showDistance && currentTarget != null);

        lastShownMeters = -1;
        UpdateDistance();
    }

    // ---------------------------------------------------------------- Distance

    private void UpdateDistance()
    {
        if (distanceText == null || currentTarget == null || player == null) return;
        if (!steps[CurrentIndex].showDistance) return;

        Vector3 flat = currentTarget.position - player.position;
        flat.y = 0f;
        int meters = Mathf.RoundToInt(flat.magnitude);

        if (meters == lastShownMeters) return; // only touch the text when the number changes
        lastShownMeters = meters;
        distanceText.text = string.Format(distanceFormat, meters);
    }

    private bool ReachedTarget(TaskStep step)
    {
        if (currentTarget == null || player == null) return false;

        Vector3 flat = currentTarget.position - player.position;
        flat.y = 0f;
        return flat.magnitude <= step.arriveDistance;
    }

    private void EnsurePlayer()
    {
        if (player != null) return;
        GameObject p = GameObject.FindWithTag(playerTag);
        if (p != null) player = p.transform;
    }

    // ---------------------------------------------------------------- Items

    public void AddItem(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName)) return;
        inventory.Add(itemName.Trim());
    }

    public bool HasItem(string itemName)
    {
        return !string.IsNullOrWhiteSpace(itemName) && inventory.Contains(itemName.Trim());
    }
}*/


/*using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-by-one task system. Shows the CURRENT task only, with a live distance meter.
/// Uses YOUR UI: drag the objects from the Hierarchy into the slots below.
/// Add this to any empty GameObject (e.g. "GameManager").
/// Starts automatically after the WakeUpIntro finishes.
/// </summary>
public class TaskListManager : MonoBehaviour
{
    public static TaskListManager Instance { get; private set; }

    [System.Serializable]
    public class TaskStep
    {
        [Tooltip("Shown in the Title text, e.g. 'Gather your things'.")]
        public string title = "New task";

        [Tooltip("Shown in the Description text, e.g. 'Collect House Key'.")]
        public string description = "";

        [Tooltip("Optional. Item to collect (must match 'Item Name' on a PickupItem). " +
                 "The step completes when it's collected and the distance meter points to it.")]
        public string itemName;

        [Tooltip("Optional. A place to go. If empty and an item is set, the item's position is used.")]
        public Transform target;

        [Tooltip("Complete this step when the player gets close to the target.")]
        public bool completeOnReachTarget;
        public float arriveDistance = 3f;

        [Tooltip("Optional. The step completes when this door gets locked. " +
                 "Drag the object that has the Door script (not the empty target).")]
        public Door lockDoor;

        [Tooltip("Optional. The step completes when this shutter reaches the state below. " +
                 "Drag the object that has the ShutterButton script.")]
        public ShutterButton shutter;
        [Tooltip("Ticked = the shutter must be OPEN to complete. Unticked = it must be CLOSED.")]
        public bool shutterMustBeOpen = true;

        [Tooltip("Optional. The step completes when another script calls " +
                 "TaskListManager.Instance.SendSignal(\"this name\"), e.g. OrderTaken or Ate.")]
        public string signalName;

        [Tooltip("Show the distance meter for this step.")]
        public bool showDistance = true;

        [Tooltip("Draw the path line on the ground to the target.")]
        public bool showPathGuide;
    }

    [Header("Your UI (drag from the Hierarchy)")]
    [Tooltip("The object that holds the whole task UI. It is hidden until the wake-up intro ends. " +
             "Best: the 'Canvas' object under HUD > Task.")]
    public GameObject panel;
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text distanceText;

    [Header("Tasks (completed in order, top to bottom)")]
    public List<TaskStep> steps = new List<TaskStep>();

    [Header("Start")]
    public bool startAfterWakeUp = true;
    public string playerTag = "Player";

    [Header("Distance meter")]
    [Tooltip("{0} = distance in meters.")]
    public string distanceFormat = "{0}m away";

    [Header("Feel")]
    public bool animateTransitions = true;
    public Color completedColor = new Color(0.55f, 0.9f, 0.6f);
    [Tooltip("How long the finished task stays green before the next one appears.")]
    public float completedHold = 0.7f;
    public float fadeTime = 0.25f;
    public bool hidePanelWhenDone = true;
    public AudioClip stepCompleteSound;

    [Header("Testing (skip task)")]
    [Tooltip("Lets you skip the current task while testing. Turn OFF before releasing the game.")]
    public bool enableSkip = true;
    public KeyCode skipKey = KeyCode.F9;
    [Tooltip("Optional. A UI Button that skips the current task. Item tasks give you the item automatically.")]
    public Button skipButton;

    public bool Started { get; private set; }
    public bool AllDone { get; private set; }
    public int CurrentIndex { get; private set; }

    private readonly HashSet<string> inventory =
        new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> signals =
        new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    private PathGuide pathGuide;
    private CanvasGroup canvasGroup;
    private Transform player;
    private Transform currentTarget;
    private WakeUpIntro intro;
    private Color descriptionColor = Color.white;
    private int lastShownMeters = -1;
    private bool transitioning;

    // Pre-fills the list when you add the component or press Reset
    private void Reset()
    {
        string[] items = { "House Key", "Gate Key", "Shop Key", "Wallet Bag", "Materials Bag", "Notebook" };

        steps = new List<TaskStep>();
        foreach (string item in items)
        {
            steps.Add(new TaskStep
            {
                title = "Gather your things",
                description = "Collect " + item,
                itemName = item
            });
        }

        steps.Add(new TaskStep
        {
            title = "Go to the shop",
            description = "Head to the shop",
            completeOnReachTarget = true,
            arriveDistance = 3f,
            showPathGuide = true
        });
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        pathGuide = GetComponent<PathGuide>();
        if (pathGuide == null) pathGuide = gameObject.AddComponent<PathGuide>();

        if (panel == null || titleText == null || descriptionText == null)
            Debug.LogWarning("TaskListManager: drag your Panel, Title Text and Description Text into the slots.");

        if (descriptionText != null) descriptionColor = descriptionText.color;

        if (panel != null)
        {
            canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = panel.AddComponent<CanvasGroup>();
            panel.SetActive(false); // hidden until the intro is over
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipCurrentTask);
            skipButton.gameObject.SetActive(enableSkip);
        }

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
        if (skipButton != null) skipButton.onClick.RemoveListener(SkipCurrentTask);
        if (Instance == this) Instance = null;
    }

    // ---------------------------------------------------------------- Flow

    public void BeginTasks()
    {
        if (Started) return;
        Started = true;

        if (panel != null) panel.SetActive(true);
        if (canvasGroup != null) canvasGroup.alpha = animateTransitions ? 0f : 1f;

        if (steps.Count == 0)
        {
            AllDone = true;
            return;
        }

        StartCoroutine(GoToStep(0, false));
    }

    private void Update()
    {
        if (enableSkip && Input.GetKeyDown(skipKey)) SkipCurrentTask();

        if (!Started || AllDone || transitioning || steps.Count == 0) return;

        EnsurePlayer();
        UpdateDistance();

        TaskStep step = steps[CurrentIndex];
        bool hasItem = !string.IsNullOrWhiteSpace(step.itemName);

        bool hasSignal = !string.IsNullOrWhiteSpace(step.signalName);

        // A step with no condition at all is completed manually (CompleteCurrentStep)
        if (!hasItem && !step.completeOnReachTarget && step.lockDoor == null
            && step.shutter == null && !hasSignal) return;

        if (hasItem && !HasItem(step.itemName)) return;
        if (step.lockDoor != null && !step.lockDoor.IsLocked) return;
        if (step.shutter != null && step.shutter.IsOpen != step.shutterMustBeOpen) return;
        if (hasSignal && !HasSignal(step.signalName)) return;
        if (step.completeOnReachTarget && !ReachedTarget(step)) return;

        CompleteCurrentStep();
    }

    /// <summary>Call this yourself for steps that complete by some custom event.</summary>
    public void CompleteCurrentStep()
    {
        if (!Started || AllDone || transitioning) return;

        if (stepCompleteSound != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(stepCompleteSound, Camera.main.transform.position);

        StartCoroutine(GoToStep(CurrentIndex + 1, true));
    }

    /// <summary>
    /// For testing: completes the current task right away.
    /// If it's an item task, you automatically get the item (and it disappears from the world).
    /// </summary>
    public void SkipCurrentTask()
    {
        if (!enableSkip) return;
        if (!Started || AllDone || transitioning || steps.Count == 0) return;

        TaskStep step = steps[CurrentIndex];

        if (!string.IsNullOrWhiteSpace(step.itemName) && !HasItem(step.itemName))
        {
            EnsurePlayer();
            Vector3 from = player != null ? player.position : Vector3.zero;
            PickupItem item = PickupItem.FindByName(step.itemName, from);

            if (item != null) item.ForceCollect(); // adds the item and removes it from the scene
            else AddItem(step.itemName);
        }

        if (step.shutter != null) step.shutter.SetOpen(step.shutterMustBeOpen);
        if (!string.IsNullOrWhiteSpace(step.signalName)) SendSignal(step.signalName);

        StartCoroutine(GoToStep(CurrentIndex + 1, false, true));
    }

    private IEnumerator GoToStep(int index, bool showCompleted, bool instant = false)
    {
        transitioning = true;

        // Little "done!" moment: the finished task turns green, then fades away
        if (showCompleted && animateTransitions)
        {
            if (descriptionText != null) descriptionText.color = completedColor;
            yield return new WaitForSeconds(completedHold);
            yield return Fade(1f, 0f);
        }

        if (index >= steps.Count)
        {
            AllDone = true;
            currentTarget = null;
            pathGuide.SetTarget(null);
            if (hidePanelWhenDone && panel != null) panel.SetActive(false);
            transitioning = false;
            yield break;
        }

        ApplyStep(index);

        if (animateTransitions && !instant) yield return Fade(0f, 1f);
        else if (canvasGroup != null) canvasGroup.alpha = 1f;

        transitioning = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (canvasGroup == null) yield break;

        float duration = Mathf.Max(0.01f, fadeTime);
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, time / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private void ApplyStep(int index)
    {
        CurrentIndex = index;
        TaskStep step = steps[index];

        if (titleText != null) titleText.text = step.title;
        if (descriptionText != null)
        {
            descriptionText.text = step.description;
            descriptionText.color = descriptionColor;
        }

        // Work out where this step points to
        EnsurePlayer();
        currentTarget = step.target;
        if (currentTarget == null && step.lockDoor != null) currentTarget = step.lockDoor.transform;
        if (currentTarget == null && step.shutter != null) currentTarget = step.shutter.shutterPivot;

        bool hasItem = !string.IsNullOrWhiteSpace(step.itemName);
        if (currentTarget == null && hasItem && !HasItem(step.itemName))
        {
            Vector3 from = player != null ? player.position : Vector3.zero;
            PickupItem item = PickupItem.FindByName(step.itemName, from);

            if (item != null) currentTarget = item.transform;
            else Debug.LogWarning($"TaskListManager: no PickupItem named '{step.itemName}' found in the scene.");
        }

        if (step.completeOnReachTarget && currentTarget == null)
            Debug.LogWarning($"TaskListManager: step '{step.description}' needs a Target assigned, otherwise it can't be completed.");

        pathGuide.SetTarget(step.showPathGuide ? currentTarget : null);

        if (distanceText != null)
            distanceText.gameObject.SetActive(step.showDistance && currentTarget != null);

        lastShownMeters = -1;
        UpdateDistance();
    }

    // ---------------------------------------------------------------- Distance

    private void UpdateDistance()
    {
        if (distanceText == null || currentTarget == null || player == null) return;
        if (!steps[CurrentIndex].showDistance) return;

        Vector3 flat = currentTarget.position - player.position;
        flat.y = 0f;
        int meters = Mathf.RoundToInt(flat.magnitude);

        if (meters == lastShownMeters) return; // only touch the text when the number changes
        lastShownMeters = meters;
        distanceText.text = string.Format(distanceFormat, meters);
    }

    private bool ReachedTarget(TaskStep step)
    {
        if (currentTarget == null || player == null) return false;

        Vector3 flat = currentTarget.position - player.position;
        flat.y = 0f;
        return flat.magnitude <= step.arriveDistance;
    }

    private void EnsurePlayer()
    {
        if (player != null) return;
        GameObject p = GameObject.FindWithTag(playerTag);
        if (p != null) player = p.transform;
    }

    // ---------------------------------------------------------------- Items

    public void AddItem(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName)) return;
        inventory.Add(itemName.Trim());
    }

    public bool HasItem(string itemName)
    {
        return !string.IsNullOrWhiteSpace(itemName) && inventory.Contains(itemName.Trim());
    }

    // ---------------------------------------------------------------- Signals

    /// <summary>Other scripts call this to finish steps that wait for a signal, e.g. SendSignal("OrderTaken").</summary>
    public void SendSignal(string signalName)
    {
        if (string.IsNullOrWhiteSpace(signalName)) return;
        signals.Add(signalName.Trim());
    }

    public bool HasSignal(string signalName)
    {
        return !string.IsNullOrWhiteSpace(signalName) && signals.Contains(signalName.Trim());
    }

    /// <summary>Forget a signal (useful when the same task repeats on a new day).</summary>
    public void ClearSignal(string signalName)
    {
        if (!string.IsNullOrWhiteSpace(signalName)) signals.Remove(signalName.Trim());
    }
}*/


using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-by-one task system. Shows the CURRENT task only, with a live distance meter.
/// Uses YOUR UI: drag the objects from the Hierarchy into the slots below.
/// Add this to any empty GameObject (e.g. "GameManager").
/// Starts automatically after the WakeUpIntro finishes.
/// </summary>
public class TaskListManager : MonoBehaviour
{
    public static TaskListManager Instance { get; private set; }

    [System.Serializable]
    public class TaskStep
    {
        [Tooltip("Shown in the Title text, e.g. 'Gather your things'.")]
        public string title = "New task";

        [Tooltip("Shown in the Description text, e.g. 'Collect House Key'.")]
        public string description = "";

        [Tooltip("Optional. Item to collect (must match 'Item Name' on a PickupItem). " +
                 "The step completes when it's collected and the distance meter points to it.")]
        public string itemName;

        [Tooltip("Optional. A place to go. If empty and an item is set, the item's position is used.")]
        public Transform target;

        [Tooltip("Complete this step when the player gets close to the target.")]
        public bool completeOnReachTarget;
        public float arriveDistance = 3f;

        [Tooltip("Optional. The step completes when this door gets locked. " +
                 "Drag the object that has the Door script (not the empty target).")]
        public Door lockDoor;

        [Tooltip("Optional. The step completes when this shutter reaches the state below. " +
                 "Drag the object that has the ShutterButton script.")]
        public ShutterButton shutter;
        [Tooltip("Ticked = the shutter must be OPEN to complete. Unticked = it must be CLOSED.")]
        public bool shutterMustBeOpen = true;

        [Tooltip("Optional. The step completes when another script calls " +
                 "TaskListManager.Instance.SendSignal(\"this name\"), e.g. OrderTaken or Ate.")]
        public string signalName;

        [Tooltip("Show the distance meter for this step.")]
        public bool showDistance = true;

        [Tooltip("Draw the path line on the ground to the target.")]
        public bool showPathGuide;
    }

    [Header("Your UI (drag from the Hierarchy)")]
    [Tooltip("The object that holds the whole task UI. It is hidden until the wake-up intro ends. " +
             "Best: the 'Canvas' object under HUD > Task.")]
    public GameObject panel;
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text distanceText;

    [Header("Tasks (completed in order, top to bottom)")]
    public List<TaskStep> steps = new List<TaskStep>();

    [Tooltip("The daily routine. When a new day begins (or after the last step) the list jumps back to this step number, " +
             "where 0 = the first step. Put the steps that repeat every day AFTER your first-day steps. -1 = no loop.")]
    public int repeatFromStep = -1;

    [Header("Start")]
    public bool startAfterWakeUp = true;
    public string playerTag = "Player";

    [Header("Distance meter")]
    [Tooltip("{0} = distance in meters.")]
    public string distanceFormat = "{0}m away";

    [Header("Feel")]
    public bool animateTransitions = true;
    public Color completedColor = new Color(0.55f, 0.9f, 0.6f);
    [Tooltip("How long the finished task stays green before the next one appears.")]
    public float completedHold = 0.7f;
    public float fadeTime = 0.25f;
    public bool hidePanelWhenDone = true;
    public AudioClip stepCompleteSound;

    [Header("Testing (skip task)")]
    [Tooltip("Lets you skip the current task while testing. Turn OFF before releasing the game.")]
    public bool enableSkip = true;
    public KeyCode skipKey = KeyCode.F9;
    [Tooltip("Optional. A UI Button that skips the current task. Item tasks give you the item automatically.")]
    public Button skipButton;

    public bool Started { get; private set; }
    public bool AllDone { get; private set; }
    public int CurrentIndex { get; private set; }

    private readonly HashSet<string> inventory =
        new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> signals =
        new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    private PathGuide pathGuide;
    private CanvasGroup canvasGroup;
    private Transform player;
    private Transform currentTarget;
    private WakeUpIntro intro;
    private Color descriptionColor = Color.white;
    private int lastShownMeters = -1;
    private bool transitioning;
    private bool pendingDayRestart;

    // Pre-fills the list when you add the component or press Reset
    private void Reset()
    {
        string[] items = { "House Key", "Gate Key", "Shop Key", "Wallet Bag", "Materials Bag", "Notebook" };

        steps = new List<TaskStep>();
        foreach (string item in items)
        {
            steps.Add(new TaskStep
            {
                title = "Gather your things",
                description = "Collect " + item,
                itemName = item
            });
        }

        steps.Add(new TaskStep
        {
            title = "Go to the shop",
            description = "Head to the shop",
            completeOnReachTarget = true,
            arriveDistance = 3f,
            showPathGuide = true
        });
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        pathGuide = GetComponent<PathGuide>();
        if (pathGuide == null) pathGuide = gameObject.AddComponent<PathGuide>();

        if (panel == null || titleText == null || descriptionText == null)
            Debug.LogWarning("TaskListManager: drag your Panel, Title Text and Description Text into the slots.");

        if (descriptionText != null) descriptionColor = descriptionText.color;

        if (panel != null)
        {
            canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = panel.AddComponent<CanvasGroup>();
            panel.SetActive(false); // hidden until the intro is over
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipCurrentTask);
            skipButton.gameObject.SetActive(enableSkip);
        }

        if (startAfterWakeUp)
        {
#if UNITY_2023_1_OR_NEWER
            intro = FindFirstObjectByType<WakeUpIntro>();
#else
            intro = FindObjectOfType<WakeUpIntro>();
#endif
        }

        if (DayNightManager.Instance != null) DayNightManager.Instance.OnNewDay += HandleNewDay;

        if (intro != null) intro.OnFinished += BeginTasks;
        else BeginTasks();
    }

    private void OnDestroy()
    {
        if (intro != null) intro.OnFinished -= BeginTasks;
        if (skipButton != null) skipButton.onClick.RemoveListener(SkipCurrentTask);
        if (DayNightManager.Instance != null) DayNightManager.Instance.OnNewDay -= HandleNewDay;
        if (Instance == this) Instance = null;
    }

    // ---------------------------------------------------------------- Flow

    public void BeginTasks()
    {
        if (Started) return;
        Started = true;

        if (panel != null) panel.SetActive(true);
        if (canvasGroup != null) canvasGroup.alpha = animateTransitions ? 0f : 1f;

        if (steps.Count == 0)
        {
            AllDone = true;
            return;
        }

        StartCoroutine(GoToStep(0, false));
    }

    private void Update()
    {
        if (enableSkip && Input.GetKeyDown(skipKey)) SkipCurrentTask();

        // A new day began: start the daily routine again
        if (pendingDayRestart && Started && !transitioning && steps.Count > 0)
        {
            pendingDayRestart = false;
            ClearLoopSignals();
            AllDone = false;
            if (panel != null) panel.SetActive(true);
            StartCoroutine(GoToStep(repeatFromStep, false, true));
            return;
        }

        if (!Started || AllDone || transitioning || steps.Count == 0) return;

        EnsurePlayer();
        UpdateDistance();

        TaskStep step = steps[CurrentIndex];
        bool hasItem = !string.IsNullOrWhiteSpace(step.itemName);

        bool hasSignal = !string.IsNullOrWhiteSpace(step.signalName);

        // A step with no condition at all is completed manually (CompleteCurrentStep)
        if (!hasItem && !step.completeOnReachTarget && step.lockDoor == null
            && step.shutter == null && !hasSignal) return;

        if (hasItem && !HasItem(step.itemName)) return;
        if (step.lockDoor != null && !step.lockDoor.IsLocked) return;
        if (step.shutter != null && step.shutter.IsOpen != step.shutterMustBeOpen) return;
        if (hasSignal && !HasSignal(step.signalName)) return;
        if (step.completeOnReachTarget && !ReachedTarget(step)) return;

        CompleteCurrentStep();
    }

    /// <summary>Call this yourself for steps that complete by some custom event.</summary>
    public void CompleteCurrentStep()
    {
        if (!Started || AllDone || transitioning) return;

        if (stepCompleteSound != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(stepCompleteSound, Camera.main.transform.position);

        StartCoroutine(GoToStep(CurrentIndex + 1, true));
    }

    /// <summary>
    /// For testing: completes the current task right away.
    /// If it's an item task, you automatically get the item (and it disappears from the world).
    /// </summary>
    public void SkipCurrentTask()
    {
        if (!enableSkip) return;
        if (!Started || AllDone || transitioning || steps.Count == 0) return;

        TaskStep step = steps[CurrentIndex];

        if (!string.IsNullOrWhiteSpace(step.itemName) && !HasItem(step.itemName))
        {
            EnsurePlayer();
            Vector3 from = player != null ? player.position : Vector3.zero;
            PickupItem item = PickupItem.FindByName(step.itemName, from);

            if (item != null) item.ForceCollect(); // adds the item and removes it from the scene
            else AddItem(step.itemName);
        }

        if (step.shutter != null) step.shutter.SetOpen(step.shutterMustBeOpen);
        if (!string.IsNullOrWhiteSpace(step.signalName)) SendSignal(step.signalName);

        StartCoroutine(GoToStep(CurrentIndex + 1, false, true));
    }

    private IEnumerator GoToStep(int index, bool showCompleted, bool instant = false)
    {
        transitioning = true;

        // Little "done!" moment: the finished task turns green, then fades away
        if (showCompleted && animateTransitions)
        {
            if (descriptionText != null) descriptionText.color = completedColor;
            yield return new WaitForSeconds(completedHold);
            yield return Fade(1f, 0f);
        }

        if (index >= steps.Count)
        {
            if (repeatFromStep >= 0 && repeatFromStep < steps.Count)
            {
                // The last step is done: the same routine starts again
                ClearLoopSignals();
                index = repeatFromStep;
            }
            else
            {
                AllDone = true;
                currentTarget = null;
                pathGuide.SetTarget(null);
                if (hidePanelWhenDone && panel != null) panel.SetActive(false);
                transitioning = false;
                yield break;
            }
        }

        ApplyStep(index);

        if (animateTransitions && !instant) yield return Fade(0f, 1f);
        else if (canvasGroup != null) canvasGroup.alpha = 1f;

        transitioning = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        if (canvasGroup == null) yield break;

        float duration = Mathf.Max(0.01f, fadeTime);
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, time / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }

    private void ApplyStep(int index)
    {
        CurrentIndex = index;
        TaskStep step = steps[index];

        if (titleText != null) titleText.text = step.title;
        if (descriptionText != null)
        {
            descriptionText.text = step.description;
            descriptionText.color = descriptionColor;
        }

        // Work out where this step points to
        EnsurePlayer();
        currentTarget = step.target;
        if (currentTarget == null && step.lockDoor != null) currentTarget = step.lockDoor.transform;
        if (currentTarget == null && step.shutter != null) currentTarget = step.shutter.shutterPivot;

        bool hasItem = !string.IsNullOrWhiteSpace(step.itemName);
        if (currentTarget == null && hasItem && !HasItem(step.itemName))
        {
            Vector3 from = player != null ? player.position : Vector3.zero;
            PickupItem item = PickupItem.FindByName(step.itemName, from);

            if (item != null) currentTarget = item.transform;
            else Debug.LogWarning($"TaskListManager: no PickupItem named '{step.itemName}' found in the scene.");
        }

        if (step.completeOnReachTarget && currentTarget == null)
            Debug.LogWarning($"TaskListManager: step '{step.description}' needs a Target assigned, otherwise it can't be completed.");

        pathGuide.SetTarget(step.showPathGuide ? currentTarget : null);

        if (distanceText != null)
            distanceText.gameObject.SetActive(step.showDistance && currentTarget != null);

        lastShownMeters = -1;
        UpdateDistance();
    }

    // ---------------------------------------------------------------- Distance

    private void UpdateDistance()
    {
        if (distanceText == null || currentTarget == null || player == null) return;
        if (!steps[CurrentIndex].showDistance) return;

        Vector3 flat = currentTarget.position - player.position;
        flat.y = 0f;
        int meters = Mathf.RoundToInt(flat.magnitude);

        if (meters == lastShownMeters) return; // only touch the text when the number changes
        lastShownMeters = meters;
        distanceText.text = string.Format(distanceFormat, meters);
    }

    private bool ReachedTarget(TaskStep step)
    {
        if (currentTarget == null || player == null) return false;

        Vector3 flat = currentTarget.position - player.position;
        flat.y = 0f;
        return flat.magnitude <= step.arriveDistance;
    }

    private void EnsurePlayer()
    {
        if (player != null) return;
        GameObject p = GameObject.FindWithTag(playerTag);
        if (p != null) player = p.transform;
    }

    // ---------------------------------------------------------------- Items

    public void AddItem(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName)) return;
        inventory.Add(itemName.Trim());
    }

    public bool HasItem(string itemName)
    {
        return !string.IsNullOrWhiteSpace(itemName) && inventory.Contains(itemName.Trim());
    }

    // ---------------------------------------------------------------- Days

    private void HandleNewDay(int day)
    {
        if (repeatFromStep < 0 || repeatFromStep >= steps.Count) return;
        pendingDayRestart = true;
    }

    // Forget the signals of the repeating steps so they can be completed again today
    private void ClearLoopSignals()
    {
        if (repeatFromStep < 0) return;

        for (int i = repeatFromStep; i < steps.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(steps[i].signalName)) ClearSignal(steps[i].signalName);
        }
    }

    // ---------------------------------------------------------------- Signals

    /// <summary>Other scripts call this to finish steps that wait for a signal, e.g. SendSignal("OrderTaken").</summary>
    public void SendSignal(string signalName)
    {
        if (string.IsNullOrWhiteSpace(signalName)) return;
        signals.Add(signalName.Trim());
    }

    public bool HasSignal(string signalName)
    {
        return !string.IsNullOrWhiteSpace(signalName) && signals.Contains(signalName.Trim());
    }

    /// <summary>Forget a signal (useful when the same task repeats on a new day).</summary>
    public void ClearSignal(string signalName)
    {
        if (!string.IsNullOrWhiteSpace(signalName)) signals.Remove(signalName.Trim());
    }
}