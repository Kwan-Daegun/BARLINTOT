using System.Reflection;
using UnityEngine;

/// <summary>
/// Lets the player open / close the shop shutter by AIMING AT THE SHUTTER DOOR itself
/// (the inside button keeps working as before).
/// Opening needs the Shop Key. Put this on any object, e.g. on ShutterDoor.
/// The aim area is measured while the shutter is full size, so you can still aim at
/// the spot where the door is even after it has rolled up.
/// </summary>
public class ShutterAccess : MonoBehaviour
{
    [Header("Shutter")]
    [Tooltip("The object that has your ShutterButton script.")]
    public ShutterButton shutter;
    [Tooltip("Optional. An empty object placed just OUTSIDE the shop, in front of the shutter. " +
             "If set, this only works from the outside. If empty, it works from both sides.")]
    public Transform outsidePoint;

    [Header("Key")]
    [Tooltip("Item needed to open the shutter from outside (matches a PickupItem's Item Name). Empty = no key.")]
    public string requiredKey = "Shop Key";
    [Tooltip("Also need the key to close it from outside.")]
    public bool requireKeyToClose = false;

    [Header("Detection")]
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("How far you can be from the shutter (measured from the camera along where you look).")]
    public float interactDistance = 4f;
    [Tooltip("Makes the aim area a bit bigger around the shutter, per side.")]
    public float aimPadding = 0.5f;
    public string playerTag = "Player";

    [Header("Prompt (your own E prompt UI)")]
    public GameObject promptUI;
    [Tooltip("Optional. The text inside the prompt. If empty, the first Text / TextMeshPro under Prompt UI is used.")]
    public GameObject promptText;
    public string openLabel = "Open Shutter";
    public string closeLabel = "Close Shutter";
    [Tooltip("Shown while you're aiming at a locked shutter without the key.")]
    public string lockedLabel = "Locked";
    [Tooltip("{0} = key, {1} = label")]
    public string promptFormat = "[{0}] {1}";

    [Header("Sound")]
    [Tooltip("Played when you try without the key.")]
    public AudioClip lockedSound;

    private Bounds closedBounds;   // shutter area while fully closed (full size)
    private Vector3 sideNormal;    // direction pointing through the shutter (front to back)
    private bool hasOutside;
    private float outsideSign;
    private bool ready;

    private Transform player;
    private bool focused;
    private bool wasShowing;
    private int lastState = -1;
    private Component textComponent;   // works with UI Text and TextMeshPro
    private PropertyInfo textProperty;

    private void Awake()
    {
        if (shutter == null || shutter.shutterPivot == null)
        {
            Debug.LogWarning("ShutterAccess: drag the object with your ShutterButton into 'Shutter'.");
            return;
        }

        // Measure the shutter now, while it is still full size (ShutterButton squishes it later in Start)
        if (!TryGetShutterBounds(out Bounds b))
        {
            Debug.LogWarning("ShutterAccess: no renderers found under the shutter pivot.");
            return;
        }

        // The thin horizontal axis of the shutter is the direction you walk through it
        sideNormal = b.size.x < b.size.z ? Vector3.right : Vector3.forward;

        if (outsidePoint != null)
        {
            hasOutside = true;
            outsideSign = Mathf.Sign(Vector3.Dot(outsidePoint.position - b.center, sideNormal));
        }

        b.Expand(aimPadding * 2f);
        closedBounds = b;
        ready = true;

        if (promptUI != null)
        {
            FindTextComponent(promptText != null ? promptText : promptUI,
                out textComponent, out textProperty);
            promptUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (!ready) return;

        if (player == null)
        {
            GameObject p = GameObject.FindWithTag(playerTag);
            if (p != null) player = p.transform;
        }

        focused = IsAimedAt();

        if (focused && !shutter.IsMoving && !PickupItem.IsFocusingItem && Input.GetKeyDown(interactKey))
            TryInteract();
    }

    // Runs after every Update so it wins if another script hides the prompt
    private void LateUpdate()
    {
        if (!ready || promptUI == null) return;

        bool show = focused && !shutter.IsMoving && !PickupItem.IsFocusingItem;

        if (show)
        {
            int state = GetState();
            if (!wasShowing || state != lastState)
            {
                lastState = state;
                string label = state == 2 ? lockedLabel : (state == 1 ? closeLabel : openLabel);
                SetText(string.Format(promptFormat, interactKey, label));
            }

            promptUI.SetActive(true);
        }
        else if (wasShowing)
        {
            promptUI.SetActive(false);
        }

        wasShowing = show;
    }

    // ---------------------------------------------------------------- Logic

    private void TryInteract()
    {
        if (!shutter.IsOpen)
        {
            // Closed -> opening needs the key
            if (!HasKey())
            {
                Fail($"The shutter is locked. You need the {requiredKey}.");
                return;
            }

            shutter.SetOpen(true);
            HintMessage.Show("Shutter opened.");
        }
        else
        {
            // Open -> closing (key only if you turned that on)
            if (requireKeyToClose && !HasKey())
            {
                Fail($"You need the {requiredKey} to lock up.");
                return;
            }

            shutter.SetOpen(false);
            HintMessage.Show("Shutter closed.");
        }
    }

    private void Fail(string message)
    {
        if (lockedSound != null)
            AudioSource.PlayClipAtPoint(lockedSound, shutter.transform.position);

        HintMessage.Show(message);
    }

    // 0 = can open, 1 = can close, 2 = locked
    private int GetState()
    {
        if (!shutter.IsOpen) return HasKey() ? 0 : 2;
        return (requireKeyToClose && !HasKey()) ? 2 : 1;
    }

    private bool HasKey()
    {
        if (string.IsNullOrWhiteSpace(requiredKey)) return true;
        return TaskListManager.Instance != null && TaskListManager.Instance.HasItem(requiredKey);
    }

    private bool IsAimedAt()
    {
        Camera cam = Camera.main;
        if (cam == null || player == null) return false;

        // Only from the outside, if an outside point was given
        if (hasOutside)
        {
            float side = Mathf.Sign(Vector3.Dot(player.position - closedBounds.center, sideNormal));
            if (side != outsideSign) return false;
        }

        Vector3 origin = cam.transform.position;
        Vector3 forward = cam.transform.forward;

        // Standing inside the padded area: just check you're looking toward the shutter
        if (closedBounds.Contains(origin))
        {
            Vector3 toCenter = (closedBounds.center - origin).normalized;
            return Vector3.Dot(forward, toCenter) > 0.3f;
        }

        return closedBounds.IntersectRay(new Ray(origin, forward), out float distance)
               && distance <= interactDistance;
    }

    private bool TryGetShutterBounds(out Bounds bounds)
    {
        bounds = default;
        if (shutter == null || shutter.shutterPivot == null) return false;

        Renderer[] renderers = shutter.shutterPivot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return false;

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    // ---------------------------------------------------------------- Text helpers

    private static bool FindTextComponent(GameObject root, out Component comp, out PropertyInfo prop)
    {
        comp = null;
        prop = null;
        if (root == null) return false;

        foreach (Component c in root.GetComponentsInChildren<Component>(true))
        {
            if (c == null) continue;

            string typeName = c.GetType().Name;
            if (typeName == "Text" || typeName == "TextMeshProUGUI" || typeName == "TextMeshPro")
            {
                PropertyInfo p = c.GetType().GetProperty("text");
                if (p != null && p.PropertyType == typeof(string))
                {
                    comp = c;
                    prop = p;
                    return true;
                }
            }
        }
        return false;
    }

    private void SetText(string value)
    {
        if (textComponent != null && textProperty != null)
            textProperty.SetValue(textComponent, value);
    }

    // Shows the aim area in the Scene view when this object is selected
    private void OnDrawGizmosSelected()
    {
        Bounds b;
        if (ready) b = closedBounds;
        else if (TryGetShutterBounds(out b)) b.Expand(aimPadding * 2f);
        else return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(b.center, b.size);
    }
}