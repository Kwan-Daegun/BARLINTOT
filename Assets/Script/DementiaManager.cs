using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The dementia twist. Add this to your GameManager object.
/// 1) Memory lapses: now and then your task text turns into "..." and the distance meter and path line disappear.
/// 2) Lost items: every morning you forget where you left some of your things. They are back in the world
///    at new spots, and you have to find them again.
/// The longer the game goes on, the more forgetful you get.
/// </summary>
public class DementiaManager : MonoBehaviour
{
    public static DementiaManager Instance { get; private set; }

    [Header("How forgetful")]
    [Range(0f, 1f)] public float startLevel = 0.2f;
    [Range(0f, 1f)]
    [Tooltip("How much worse it gets every day.")]
    public float levelGrowthPerDay = 0.15f;

    [Header("Memory lapses (the task turns into '...')")]
    public bool enableLapses = true;
    [Tooltip("Seconds before the very first lapse.")]
    public float firstLapseDelay = 60f;
    [Tooltip("Seconds between lapses when you are not very forgetful yet.")]
    public float maxSecondsBetweenLapses = 120f;
    [Tooltip("Seconds between lapses when you are very forgetful.")]
    public float minSecondsBetweenLapses = 35f;
    public float minLapseSeconds = 6f;
    public float maxLapseSeconds = 16f;

    [Header("Lost items (every morning)")]
    public bool enableLostItems = true;
    [Tooltip("Nothing is lost before this day.")]
    public int startLosingOnDay = 2;
    public int minItemsLost = 1;
    public int maxItemsLost = 3;
    [Tooltip("Every item that can get lost: the PickupItem objects in your scene (drag them from under 'Items').")]
    public PickupItem[] items;
    [Tooltip("Optional extra hiding spots (empty objects). Lost items can also end up where other items started.")]
    public Transform[] extraHidingSpots;
    [Tooltip("{0} = the lost items, e.g. 'House Key, Notebook'")]
    public string lostMessage = "You can't remember where you left: {0}.";

    /// <summary>True during a memory lapse.</summary>
    public bool IsForgetting { get; private set; }

    public int Day => DayNightManager.Instance != null ? DayNightManager.Instance.dayNumber : 1;
    public float Level => Mathf.Clamp01(startLevel + levelGrowthPerDay * (Day - 1));

    private float timer;
    private Vector3[] originalPositions;

    private void Awake()
    {
        Instance = this;
        timer = firstLapseDelay;

        originalPositions = new Vector3[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null) originalPositions[i] = items[i].transform.position;
        }
    }

    private void Start()
    {
        if (DayNightManager.Instance != null) DayNightManager.Instance.OnNewDay += HandleNewDay;
    }

    private void OnDestroy()
    {
        if (DayNightManager.Instance != null) DayNightManager.Instance.OnNewDay -= HandleNewDay;
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!enableLapses || GameEndManager.GameIsOver) return;

        // Nothing happens during the intro or while sleeping
        if (TaskListManager.Instance == null || !TaskListManager.Instance.Started) return;
        if (DayNightManager.Instance != null && DayNightManager.Instance.IsSleeping) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        if (IsForgetting) EndLapse();
        else StartLapse();
    }

    // ---------------------------------------------------------------- Memory lapses

    private void StartLapse()
    {
        IsForgetting = true;
        timer = Mathf.Lerp(minLapseSeconds, maxLapseSeconds, Level);
    }

    private void EndLapse()
    {
        IsForgetting = false;
        timer = Mathf.Lerp(maxSecondsBetweenLapses, minSecondsBetweenLapses, Level);
    }

    // ---------------------------------------------------------------- Lost items

    private void HandleNewDay(int day)
    {
        if (IsForgetting) EndLapse();
        if (enableLostItems && day >= startLosingOnDay) LoseItems();
    }

    private void LoseItems()
    {
        TaskListManager tasks = TaskListManager.Instance;
        if (tasks == null) return;

        // Items you own right now (you picked them up, so they are switched off)
        List<PickupItem> owned = new List<PickupItem>();
        foreach (PickupItem item in items)
        {
            if (item != null && !item.gameObject.activeSelf && tasks.HasItem(item.itemName)) owned.Add(item);
        }

        int count = Mathf.RoundToInt(Mathf.Lerp(minItemsLost, maxItemsLost, Level));
        count = Mathf.Clamp(count, 0, owned.Count);
        if (count == 0) return;

        Shuffle(owned);

        // Spots that are already taken by items lying around
        List<Vector3> used = new List<Vector3>();
        foreach (PickupItem item in items)
        {
            if (item != null && item.gameObject.activeSelf) used.Add(item.transform.position);
        }

        List<string> lostNames = new List<string>();
        for (int i = 0; i < count; i++)
        {
            PickupItem item = owned[i];

            Vector3 spot = PickFreeSpot(used);
            used.Add(spot);

            tasks.RemoveItem(item.itemName);          // you no longer have it
            item.transform.position = spot;           // it's lying somewhere again
            item.gameObject.SetActive(true);
            lostNames.Add(item.itemName);
        }

        StartCoroutine(AnnounceLostItems(string.Join(", ", lostNames)));
    }

    private Vector3 PickFreeSpot(List<Vector3> used)
    {
        List<Vector3> pool = new List<Vector3>();
        for (int i = 0; i < originalPositions.Length; i++)
        {
            if (items[i] != null) pool.Add(originalPositions[i]);
        }
        foreach (Transform spot in extraHidingSpots)
        {
            if (spot != null) pool.Add(spot.position);
        }

        if (pool.Count == 0) return Vector3.zero;

        Shuffle(pool);
        foreach (Vector3 candidate in pool)
        {
            bool free = true;
            foreach (Vector3 taken in used)
            {
                if ((candidate - taken).sqrMagnitude < 0.25f) { free = false; break; }
            }
            if (free) return candidate;
        }

        return pool[0]; // everything is taken: overlap rather than lose the item
    }

    // The hint waits until you are awake, and a bit longer so it doesn't replace the morning message
    private IEnumerator AnnounceLostItems(string names)
    {
        while (DayNightManager.Instance != null && DayNightManager.Instance.IsSleeping) yield return null;
        yield return new WaitForSeconds(3.5f);

        HintMessage.Show(string.Format(lostMessage, names));
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    // ---------------------------------------------------------------- Testing (right-click the component title while playing)

    [ContextMenu("Test: start a memory lapse")]
    private void TestLapse() { StartLapse(); }

    [ContextMenu("Test: lose items now")]
    private void TestLoseItems() { LoseItems(); }
}