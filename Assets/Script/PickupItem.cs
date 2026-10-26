/*using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on every collectible (House Key, Gate Key, Notebook, ...).
/// The item needs a normal Collider (NOT a trigger) so the player can aim at it.
/// Look at it, press E, and it is added to the task list.
/// While you look at it, a prompt shows e.g. "[E] Pick up House Key".
/// </summary>
public class PickupItem : MonoBehaviour
{
    [Tooltip("Must match the name in the TaskListManager step, e.g. 'House Key'.")]
    public string itemName = "House Key";

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 2.5f;
    [Tooltip("Thickness of the aiming ray. Makes thin items easier to target.")]
    public float aimRadius = 0.06f;
    [Tooltip("Prompt stays this long after you look away, which stops flickering.")]
    public float hideDelay = 0.2f;
    public string playerTag = "Player";

    [Header("Prompt")]
    [Tooltip("ON = builds its own prompt near the crosshair (no clashes with your other UI). OFF = uses the Prompt UI below.")]
    public bool useOwnPrompt = true;
    public int ownFontSize = 30;
    [Tooltip("{0} = key, {1} = item name")]
    public string promptFormat = "[{0}] Pick up {1}";

    [Header("Your own UI (only used when Use Own Prompt is OFF)")]
    public GameObject promptUI;
    [Tooltip("Optional. If empty, the first Text / TextMeshPro found under Prompt UI is used.")]
    public GameObject promptText;

    [Header("Sound")]
    public AudioClip pickupSound;

    [Header("Debug")]
    [Tooltip("Prints what the player is aiming at to the Console. Turn off when everything works.")]
    public bool debugLog = false;

    // ---- shared between all items ----
    private static PickupItem current;          // item whose prompt is showing
    private static PickupItem looked;           // item under the crosshair this frame
    private static int lookedFrame = -1;
    private static int lastPickupFrame = -1;
    private static string lastDebug;
    private static Transform playerRoot;
    private static readonly RaycastHit[] hits = new RaycastHit[32];

    private static GameObject ownRoot;
    private static Text ownText;

    /// <summary>Every active PickupItem in the scene (used by the task system).</summary>
    public static readonly List<PickupItem> All = new List<PickupItem>();

    /// <summary>Nearest active item with this name (case-insensitive), or null.</summary>
    public static PickupItem FindByName(string wanted, Vector3 from)
    {
        PickupItem best = null;
        float bestDistance = float.MaxValue;

        foreach (PickupItem item in All)
        {
            if (item == null) continue;
            if (!string.Equals(item.itemName, wanted, System.StringComparison.OrdinalIgnoreCase)) continue;

            float d = (item.transform.position - from).sqrMagnitude;
            if (d < bestDistance)
            {
                bestDistance = d;
                best = item;
            }
        }
        return best;
    }

    /// <summary>True while the player is aiming at an item (or just picked one up this frame).</summary>
    public static bool IsFocusingItem => current != null || lastPickupFrame == Time.frameCount;

    // Works with both UI Text and TextMeshPro without needing either package
    private Component textComponent;
    private PropertyInfo textProperty;
    private string originalText;
    private string promptString;
    private float lastSeen;

    private void Awake()
    {
        if (useOwnPrompt)
        {
            EnsureOwnPrompt();
            ownRoot.SetActive(false);
        }
        else
        {
            CacheTextComponent();
            if (promptUI != null) promptUI.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
        if (current == this) current = null;
    }

    private void Update()
    {
        // Work out ONCE per frame which item is under the crosshair
        if (lookedFrame != Time.frameCount)
        {
            lookedFrame = Time.frameCount;
            looked = FindLookedItem(this);
        }

        if (looked == this)
        {
            lastSeen = Time.time;

            if (current != this)
            {
                current = this;
                promptString = string.Format(promptFormat, interactKey, itemName);
            }

            if (Input.GetKeyDown(interactKey)) Pickup();
        }
        else if (current == this && Time.time - lastSeen > hideDelay)
        {
            HidePrompt();
        }
    }

    // Runs after every Update, so it wins if another script hides/changes the prompt
    private void LateUpdate()
    {
        if (current == this) ShowPrompt();
    }

    // ---------------------------------------------------------------- Aiming

    private static PickupItem FindLookedItem(PickupItem caller)
    {
        // Can't pick things up during the wake-up intro
        if (TaskListManager.Instance != null && !TaskListManager.Instance.Started) return null;

        Camera cam = Camera.main;
        if (cam == null && Camera.allCamerasCount > 0) cam = Camera.allCameras[0];
        if (cam == null)
        {
            Log(caller, "no camera found (tag your player camera 'MainCamera')");
            return null;
        }

        if (playerRoot == null)
        {
            GameObject p = GameObject.FindWithTag(caller.playerTag);
            if (p != null) playerRoot = p.transform;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        int count = Physics.SphereCastNonAlloc(ray, Mathf.Max(0.001f, caller.aimRadius), hits, 10f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        // Nearest hit that isn't part of the player's own body
        float best = float.MaxValue;
        int bestIndex = -1;
        for (int i = 0; i < count; i++)
        {
            if (playerRoot != null && hits[i].collider.transform.IsChildOf(playerRoot)) continue;
            if (hits[i].distance < best)
            {
                best = hits[i].distance;
                bestIndex = i;
            }
        }

        if (bestIndex < 0)
        {
            Log(caller, "aiming at nothing within 10 m");
            return null;
        }

        Collider col = hits[bestIndex].collider;
        PickupItem item = col.GetComponentInParent<PickupItem>();

        if (item == null)
        {
            Log(caller, $"aiming at '{col.name}' (it has no PickupItem)");
            return null;
        }

        if (best > item.interactDistance)
        {
            Log(caller, $"aiming at item '{item.itemName}' but too far (limit {item.interactDistance} m)");
            return null;
        }

        Log(caller, $"aiming at item '{item.itemName}' - OK");
        return item;
    }

    private static void Log(PickupItem caller, string message)
    {
        if (!caller.debugLog || message == lastDebug) return;
        lastDebug = message;
        Debug.Log("[PickupItem] " + message);
    }

    // ---------------------------------------------------------------- Actions

    private void Pickup()
    {
        lastPickupFrame = Time.frameCount;

        if (TaskListManager.Instance != null) TaskListManager.Instance.AddItem(itemName);

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        if (current == this) HidePrompt();

        Destroy(gameObject);
    }

    /// <summary>Used by the task skip button: gives you the item without aiming at it.</summary>
    public void ForceCollect()
    {
        if (TaskListManager.Instance != null) TaskListManager.Instance.AddItem(itemName);
        if (current == this) HidePrompt();
        Destroy(gameObject);
    }

    // ---------------------------------------------------------------- Prompt

    private void ShowPrompt()
    {
        if (useOwnPrompt)
        {
            EnsureOwnPrompt();
            ownText.text = promptString;
            if (!ownRoot.activeSelf) ownRoot.SetActive(true);
        }
        else
        {
            SetPromptText(promptString);
            if (promptUI != null && !promptUI.activeSelf) promptUI.SetActive(true);
        }
    }

    private void HidePrompt()
    {
        current = null;

        if (useOwnPrompt)
        {
            if (ownRoot != null) ownRoot.SetActive(false);
        }
        else
        {
            SetPromptText(originalText); // restore so doors etc. show their normal text again
            if (promptUI != null) promptUI.SetActive(false);
        }
    }

    // One shared prompt for all items, built at runtime
    private void EnsureOwnPrompt()
    {
        if (ownRoot != null) return;

        ownRoot = new GameObject("PickupPromptCanvas");
        Canvas canvas = ownRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        CanvasScaler scaler = ownRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(ownRoot.transform, false);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.5f);
        bg.raycastTarget = false;

        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 1f);
        prt.anchoredPosition = new Vector2(0f, -80f); // just below the crosshair

        HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 10, 10);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(panel.transform, false);

        ownText = textObj.AddComponent<Text>();
        ownText.fontSize = ownFontSize;
        ownText.color = Color.white;
        ownText.alignment = TextAnchor.MiddleCenter;
        ownText.horizontalOverflow = HorizontalWrapMode.Overflow;
        ownText.verticalOverflow = VerticalWrapMode.Overflow;
        ownText.raycastTarget = false;

#if UNITY_2022_2_OR_NEWER
        ownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        ownText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
    }

    // ---------------------------------------------------------------- Your-own-UI text helpers

    private void CacheTextComponent()
    {
        GameObject root = promptText != null ? promptText : promptUI;
        if (root == null) return;

        foreach (Component c in root.GetComponentsInChildren<Component>(true))
        {
            if (c == null) continue;

            string typeName = c.GetType().Name;
            if (typeName == "Text" || typeName == "TextMeshProUGUI" || typeName == "TextMeshPro")
            {
                PropertyInfo prop = c.GetType().GetProperty("text");
                if (prop != null && prop.PropertyType == typeof(string))
                {
                    textComponent = c;
                    textProperty = prop;
                    originalText = (string)prop.GetValue(c);
                    return;
                }
            }
        }

        Debug.LogWarning($"PickupItem '{name}': no Text or TextMeshPro found under the prompt UI. " +
                         "Assign the text object in 'Prompt Text'.");
    }

    private void SetPromptText(string value)
    {
        if (textProperty != null && textComponent != null)
            textProperty.SetValue(textComponent, value);
    }
}*/


using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Put this on every collectible (House Key, Gate Key, Notebook, ...).
/// The item needs a normal Collider (NOT a trigger) so the player can aim at it.
/// Look at it, press E, and it is added to the task list.
/// While you look at it, a prompt shows e.g. "[E] Pick up House Key".
/// </summary>
public class PickupItem : MonoBehaviour
{
    [Tooltip("Must match the name in the TaskListManager step, e.g. 'House Key'.")]
    public string itemName = "House Key";

    [Header("Interaction")]
    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 2.5f;
    [Tooltip("Thickness of the aiming ray. Makes thin items easier to target.")]
    public float aimRadius = 0.06f;
    [Tooltip("Prompt stays this long after you look away, which stops flickering.")]
    public float hideDelay = 0.2f;
    public string playerTag = "Player";

    [Header("Prompt")]
    [Tooltip("ON = builds its own prompt near the crosshair (no clashes with your other UI). OFF = uses the Prompt UI below.")]
    public bool useOwnPrompt = true;
    public int ownFontSize = 30;
    [Tooltip("{0} = key, {1} = item name")]
    public string promptFormat = "[{0}] Pick up {1}";

    [Header("Your own UI (only used when Use Own Prompt is OFF)")]
    public GameObject promptUI;
    [Tooltip("Optional. If empty, the first Text / TextMeshPro found under Prompt UI is used.")]
    public GameObject promptText;

    [Header("Sound")]
    public AudioClip pickupSound;

    [Header("Debug")]
    [Tooltip("Prints what the player is aiming at to the Console. Turn off when everything works.")]
    public bool debugLog = false;

    // ---- shared between all items ----
    private static PickupItem current;          // item whose prompt is showing
    private static PickupItem looked;           // item under the crosshair this frame
    private static int lookedFrame = -1;
    private static int lastPickupFrame = -1;
    private static string lastDebug;
    private static Transform playerRoot;
    private static readonly RaycastHit[] hits = new RaycastHit[32];

    private static GameObject ownRoot;
    private static Text ownText;

    /// <summary>Every active PickupItem in the scene (used by the task system).</summary>
    public static readonly List<PickupItem> All = new List<PickupItem>();

    /// <summary>Nearest active item with this name (case-insensitive), or null.</summary>
    public static PickupItem FindByName(string wanted, Vector3 from)
    {
        PickupItem best = null;
        float bestDistance = float.MaxValue;

        foreach (PickupItem item in All)
        {
            if (item == null) continue;
            if (!string.Equals(item.itemName, wanted, System.StringComparison.OrdinalIgnoreCase)) continue;

            float d = (item.transform.position - from).sqrMagnitude;
            if (d < bestDistance)
            {
                bestDistance = d;
                best = item;
            }
        }
        return best;
    }

    /// <summary>True while the player is aiming at an item (or just picked one up this frame).</summary>
    public static bool IsFocusingItem => current != null || lastPickupFrame == Time.frameCount;

    // Works with both UI Text and TextMeshPro without needing either package
    private Component textComponent;
    private PropertyInfo textProperty;
    private string originalText;
    private string promptString;
    private float lastSeen;

    private void Awake()
    {
        if (useOwnPrompt)
        {
            EnsureOwnPrompt();
            ownRoot.SetActive(false);
        }
        else
        {
            CacheTextComponent();
            if (promptUI != null) promptUI.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
        if (current == this) current = null;
    }

    private void Update()
    {
        // Work out ONCE per frame which item is under the crosshair
        if (lookedFrame != Time.frameCount)
        {
            lookedFrame = Time.frameCount;
            looked = FindLookedItem(this);
        }

        if (looked == this)
        {
            lastSeen = Time.time;

            if (current != this)
            {
                current = this;
                promptString = string.Format(promptFormat, interactKey, itemName);
            }

            if (Input.GetKeyDown(interactKey)) Pickup();
        }
        else if (current == this && Time.time - lastSeen > hideDelay)
        {
            HidePrompt();
        }
    }

    // Runs after every Update, so it wins if another script hides/changes the prompt
    private void LateUpdate()
    {
        if (current == this) ShowPrompt();
    }

    // ---------------------------------------------------------------- Aiming

    private static PickupItem FindLookedItem(PickupItem caller)
    {
        // Can't pick things up during the wake-up intro
        if (TaskListManager.Instance != null && !TaskListManager.Instance.Started) return null;

        Camera cam = Camera.main;
        if (cam == null && Camera.allCamerasCount > 0) cam = Camera.allCameras[0];
        if (cam == null)
        {
            Log(caller, "no camera found (tag your player camera 'MainCamera')");
            return null;
        }

        if (playerRoot == null)
        {
            GameObject p = GameObject.FindWithTag(caller.playerTag);
            if (p != null) playerRoot = p.transform;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        int count = Physics.SphereCastNonAlloc(ray, Mathf.Max(0.001f, caller.aimRadius), hits, 10f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        // Nearest hit that isn't part of the player's own body
        float best = float.MaxValue;
        int bestIndex = -1;
        for (int i = 0; i < count; i++)
        {
            if (playerRoot != null && hits[i].collider.transform.IsChildOf(playerRoot)) continue;
            if (hits[i].distance < best)
            {
                best = hits[i].distance;
                bestIndex = i;
            }
        }

        if (bestIndex < 0)
        {
            Log(caller, "aiming at nothing within 10 m");
            return null;
        }

        Collider col = hits[bestIndex].collider;
        PickupItem item = col.GetComponentInParent<PickupItem>();

        if (item == null)
        {
            Log(caller, $"aiming at '{col.name}' (it has no PickupItem)");
            return null;
        }

        if (best > item.interactDistance)
        {
            Log(caller, $"aiming at item '{item.itemName}' but too far (limit {item.interactDistance} m)");
            return null;
        }

        Log(caller, $"aiming at item '{item.itemName}' - OK");
        return item;
    }

    private static void Log(PickupItem caller, string message)
    {
        if (!caller.debugLog || message == lastDebug) return;
        lastDebug = message;
        Debug.Log("[PickupItem] " + message);
    }

    // ---------------------------------------------------------------- Actions

    private void Pickup()
    {
        lastPickupFrame = Time.frameCount;

        if (TaskListManager.Instance != null) TaskListManager.Instance.AddItem(itemName);

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        if (current == this) HidePrompt();

        gameObject.SetActive(false); // switched off, not destroyed, so a forgetful morning can bring it back
    }

    /// <summary>Used by the task skip button: gives you the item without aiming at it.</summary>
    public void ForceCollect()
    {
        if (TaskListManager.Instance != null) TaskListManager.Instance.AddItem(itemName);
        if (current == this) HidePrompt();
        gameObject.SetActive(false); // switched off, not destroyed, so a forgetful morning can bring it back
    }

    // ---------------------------------------------------------------- Prompt

    private void ShowPrompt()
    {
        if (useOwnPrompt)
        {
            EnsureOwnPrompt();
            ownText.text = promptString;
            if (!ownRoot.activeSelf) ownRoot.SetActive(true);
        }
        else
        {
            SetPromptText(promptString);
            if (promptUI != null && !promptUI.activeSelf) promptUI.SetActive(true);
        }
    }

    private void HidePrompt()
    {
        current = null;

        if (useOwnPrompt)
        {
            if (ownRoot != null) ownRoot.SetActive(false);
        }
        else
        {
            SetPromptText(originalText); // restore so doors etc. show their normal text again
            if (promptUI != null) promptUI.SetActive(false);
        }
    }

    // One shared prompt for all items, built at runtime
    private void EnsureOwnPrompt()
    {
        if (ownRoot != null) return;

        ownRoot = new GameObject("PickupPromptCanvas");
        Canvas canvas = ownRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        CanvasScaler scaler = ownRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(ownRoot.transform, false);

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.5f);
        bg.raycastTarget = false;

        RectTransform prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 1f);
        prt.anchoredPosition = new Vector2(0f, -80f); // just below the crosshair

        HorizontalLayoutGroup layout = panel.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 10, 10);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(panel.transform, false);

        ownText = textObj.AddComponent<Text>();
        ownText.fontSize = ownFontSize;
        ownText.color = Color.white;
        ownText.alignment = TextAnchor.MiddleCenter;
        ownText.horizontalOverflow = HorizontalWrapMode.Overflow;
        ownText.verticalOverflow = VerticalWrapMode.Overflow;
        ownText.raycastTarget = false;

#if UNITY_2022_2_OR_NEWER
        ownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        ownText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
    }

    // ---------------------------------------------------------------- Your-own-UI text helpers

    private void CacheTextComponent()
    {
        GameObject root = promptText != null ? promptText : promptUI;
        if (root == null) return;

        foreach (Component c in root.GetComponentsInChildren<Component>(true))
        {
            if (c == null) continue;

            string typeName = c.GetType().Name;
            if (typeName == "Text" || typeName == "TextMeshProUGUI" || typeName == "TextMeshPro")
            {
                PropertyInfo prop = c.GetType().GetProperty("text");
                if (prop != null && prop.PropertyType == typeof(string))
                {
                    textComponent = c;
                    textProperty = prop;
                    originalText = (string)prop.GetValue(c);
                    return;
                }
            }
        }

        Debug.LogWarning($"PickupItem '{name}': no Text or TextMeshPro found under the prompt UI. " +
                         "Assign the text object in 'Prompt Text'.");
    }

    private void SetPromptText(string value)
    {
        if (textProperty != null && textComponent != null)
            textProperty.SetValue(textComponent, value);
    }
}