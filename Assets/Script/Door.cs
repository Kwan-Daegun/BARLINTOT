/*using System.Reflection;
using UnityEngine;

/// <summary>
/// Attach to the door's HINGE PIVOT object (door mesh = child of it).
/// No trigger collider needed. Detection uses a fixed point at the center of the doorway,
/// so it works from BOTH sides, whether the door is open or closed.
/// The door should be CLOSED when the game starts.
///
/// E = open / close.   L = lock / unlock (needs the key item, and the door must be closed).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class Door : MonoBehaviour
{
    [Header("Detection")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("How close the player must be (horizontal distance to the doorway center).")]
    public float interactDistance = 2.5f;
    public bool requireFacingDoor = true;
    [Range(0f, 1f)]
    [Tooltip("1 = must look straight at the door, 0 = anywhere within 90 degrees.")]
    public float facingThreshold = 0.4f;
    [Tooltip("Optional. If empty, the doorway center is calculated from the door mesh automatically.")]
    public Transform interactPoint;

    [Header("Rotation")]
    public float openAngle = 90f;
    [Tooltip("Degrees per second.")]
    public float rotateSpeed = 180f;
    public bool openAwayFromPlayer = true;

    [Header("Lock")]
    [Tooltip("Turn OFF for doors that should never be locked (no L prompt, L does nothing).")]
    public bool canBeLocked = true;
    public bool startsLocked = false;
    [Tooltip("Item needed to lock/unlock this door (must match a PickupItem's Item Name). Leave empty = no key needed.")]
    public string requiredKey = "House Key";
    public KeyCode lockKey = KeyCode.L;

    [Header("Sound")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockSound;
    public AudioClip unlockSound;
    [Tooltip("Played when you try to open a locked door.")]
    public AudioClip lockedSound;

    [Header("UI")]
    [Tooltip("Your existing 'Press E' UI object.")]
    public GameObject promptUI;
    [Tooltip("Optional. The text inside the E prompt. If empty, the first Text / TextMeshPro under Prompt UI is used.")]
    public GameObject promptText;
    public string openLabel = "Open";
    public string closeLabel = "Close";
    [Tooltip("Shown instead of Open while the door is locked.")]
    public string lockedLabel = "Locked";
    [Tooltip("{0} = key, {1} = Open / Close / Locked label")]
    public string promptFormat = "[{0}] {1}";
    [Tooltip("Your 'Press L' UI object. Only shown when the player can lock/unlock this door (has the key, door is closed).")]
    public GameObject lockPromptUI;
    [Tooltip("Optional. The text inside the L prompt. If empty, the first Text / TextMeshPro under Lock Prompt UI is used.")]
    public GameObject lockPromptText;
    public string lockLabel = "Lock";
    public string unlockLabel = "Unlock";
    [Tooltip("{0} = key, {1} = Lock / Unlock label")]
    public string lockPromptFormat = "[{0}] {1}";

    /// <summary>True while the door is locked. Locked doors can't be opened.</summary>
    public bool IsLocked { get; private set; }
    public bool IsOpen => isOpen;

    // The one door the player can currently interact with (nearest eligible door)
    private static Door activeDoor;

    private AudioSource audioSource;
    private Quaternion closedRotation;
    private Quaternion targetRotation;
    private Transform player;
    private Vector3 localPoint;   // doorway center, stored relative to the parent
    private float lastDistance;
    private bool wasActive;
    private bool wasShowingLock;
    private int lastPromptState = -1;
    private Component promptTextComponent;   // works with UI Text and TextMeshPro
    private PropertyInfo promptTextProperty;
    private bool lastLockedState;
    private string lastLockText;
    private Component lockTextComponent;   // works with UI Text and TextMeshPro
    private PropertyInfo lockTextProperty;
    private bool isOpen;
    private bool warnedNoPlayer;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        closedRotation = transform.localRotation;
        targetRotation = closedRotation;
        IsLocked = startsLocked;

        // Snapshot the doorway center while the door is closed
        Vector3 worldPoint = interactPoint != null ? interactPoint.position : CalculateDoorCenter();
        localPoint = transform.parent != null ? transform.parent.InverseTransformPoint(worldPoint) : worldPoint;

        if (promptUI != null)
        {
            FindTextComponent(promptText != null ? promptText : promptUI,
                out promptTextComponent, out promptTextProperty);
            promptUI.SetActive(false);
        }

        if (lockPromptUI != null)
        {
            CacheLockText();
            lockPromptUI.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (activeDoor == this) activeDoor = null;
    }

    private void Update()
    {
        if (player == null) FindPlayer();

        // Work out if THIS door is the one the player can use
        bool eligible = IsEligible(out float distance);
        lastDistance = distance;

        if (eligible)
        {
            if (activeDoor == null || activeDoor == this || distance < activeDoor.lastDistance)
                activeDoor = this;
        }
        else if (activeDoor == this)
        {
            activeDoor = null;
        }

        // Items take priority, so E doesn't open a door while you grab a key next to it
        if (activeDoor == this && !PickupItem.IsFocusingItem)
        {
            if (Input.GetKeyDown(interactKey)) TryOpenOrClose();
            if (Input.GetKeyDown(lockKey)) TryToggleLock();
        }

        // Smooth rotation
        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation, targetRotation, rotateSpeed * Time.deltaTime);
    }

    private void LateUpdate()
    {
        bool active = activeDoor == this;

        // "Press E" prompt
        if (promptUI != null)
        {
            if (active)
            {
                // Refresh the Open / Close / Locked label when the prompt appears or the state changes
                int state = (IsLocked && !isOpen) ? 2 : (isOpen ? 1 : 0);
                if (!wasActive || state != lastPromptState)
                {
                    lastPromptState = state;
                    string label = state == 2 ? lockedLabel : (state == 1 ? closeLabel : openLabel);
                    SetText(promptTextComponent, promptTextProperty, string.Format(promptFormat, interactKey, label));
                }

                promptUI.SetActive(true);
            }
            else if (wasActive)
            {
                // Only hide if nobody else is using the same UI object
                if (activeDoor == null || activeDoor.promptUI != promptUI)
                    promptUI.SetActive(false);
            }
        }

        // "Press L" prompt: only while the player can actually lock/unlock this door
        if (lockPromptUI != null)
        {
            bool showLock = CanShowLockPrompt();

            if (showLock)
            {
                // Refresh the "Lock" / "Unlock" label when it appears or the lock state changes
                if (!wasShowingLock || lastLockText == null || lastLockedState != IsLocked)
                {
                    lastLockedState = IsLocked;
                    lastLockText = string.Format(lockPromptFormat, lockKey, IsLocked ? unlockLabel : lockLabel);
                    SetLockText(lastLockText);
                }

                lockPromptUI.SetActive(true);
            }
            else if (wasShowingLock)
            {
                // Don't hide it if another door is about to show the same UI object
                if (activeDoor == null || activeDoor.lockPromptUI != lockPromptUI || !activeDoor.CanShowLockPrompt())
                    lockPromptUI.SetActive(false);
            }

            wasShowingLock = showLock;
        }

        wasActive = active;
    }

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

    private static void SetText(Component comp, PropertyInfo prop, string value)
    {
        if (comp != null && prop != null) prop.SetValue(comp, value);
    }

    private void CacheLockText()
    {
        GameObject root = lockPromptText != null ? lockPromptText : lockPromptUI;
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
                    lockTextComponent = c;
                    lockTextProperty = prop;
                    return;
                }
            }
        }

        Debug.LogWarning($"Door '{name}': no Text or TextMeshPro found under the Lock Prompt UI. " +
                         "Assign the text object in 'Lock Prompt Text'.");
    }

    private void SetLockText(string value)
    {
        if (lockTextProperty != null && lockTextComponent != null)
            lockTextProperty.SetValue(lockTextComponent, value);
    }

    private bool CanShowLockPrompt()
    {
        return canBeLocked
            && activeDoor == this
            && !PickupItem.IsFocusingItem
            && !isOpen
            && Quaternion.Angle(transform.localRotation, closedRotation) <= 1f
            && HasKey();
    }

    // ---------------------------------------------------------------- Open / close / lock

    private void TryOpenOrClose()
    {
        if (IsLocked && !isOpen)
        {
            PlaySound(lockedSound);

            if (HasKey()) HintMessage.Show($"Locked. Press [{lockKey}] to unlock.");
            else HintMessage.Show($"Locked. You need the {requiredKey}.");
            return;
        }

        Toggle();
    }

    private void TryToggleLock()
    {
        if (!canBeLocked) return;

        // Can't lock a door that's open (or still swinging shut)
        if (isOpen || Quaternion.Angle(transform.localRotation, closedRotation) > 1f)
        {
            HintMessage.Show("Close the door first.");
            return;
        }

        if (!HasKey())
        {
            PlaySound(lockedSound);
            HintMessage.Show($"You need the {requiredKey}.");
            return;
        }

        IsLocked = !IsLocked;
        PlaySound(IsLocked ? lockSound : unlockSound);
        HintMessage.Show(IsLocked ? "Door locked." : "Door unlocked.");
    }

    private bool HasKey()
    {
        if (string.IsNullOrWhiteSpace(requiredKey)) return true;
        return TaskListManager.Instance != null && TaskListManager.Instance.HasItem(requiredKey);
    }

    private bool IsEligible(out float distance)
    {
        distance = float.MaxValue;
        if (player == null) return false;

        Vector3 point = GetDoorPoint();

        // Horizontal distance only
        Vector3 flatToDoor = point - player.position;
        flatToDoor.y = 0f;
        distance = flatToDoor.magnitude;
        if (distance > interactDistance) return false;

        if (!requireFacingDoor) return true;

        Transform looker = Camera.main != null ? Camera.main.transform : player;

        Vector3 forward = looker.forward;
        forward.y = 0f;
        Vector3 toDoor = point - looker.position;
        toDoor.y = 0f;

        if (forward.sqrMagnitude < 0.0001f || toDoor.sqrMagnitude < 0.0001f) return true;

        return Vector3.Dot(forward.normalized, toDoor.normalized) >= facingThreshold;
    }

    private void Toggle()
    {
        if (!isOpen)
        {
            float direction = 1f;

            if (openAwayFromPlayer && player != null)
            {
                Vector3 toPlayer = player.position - transform.position;
                direction = Vector3.Dot(transform.forward, toPlayer) > 0f ? -1f : 1f;
            }

            targetRotation = closedRotation * Quaternion.Euler(0f, openAngle * direction, 0f);
            isOpen = true;
            PlaySound(openSound);
        }
        else
        {
            targetRotation = closedRotation;
            isOpen = false;
            PlaySound(closeSound);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null) audioSource.PlayOneShot(clip);
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindWithTag(playerTag);
        if (p != null)
        {
            player = p.transform;
        }
        else if (!warnedNoPlayer)
        {
            Debug.LogWarning($"Door '{name}': no object with tag '{playerTag}' found. Tag your player!");
            warnedNoPlayer = true;
        }
    }

    private Vector3 GetDoorPoint()
    {
        return transform.parent != null ? transform.parent.TransformPoint(localPoint) : localPoint;
    }

    private Vector3 CalculateDoorCenter()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return transform.position;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b.center;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 p = Application.isPlaying
            ? GetDoorPoint()
            : (interactPoint != null ? interactPoint.position : CalculateDoorCenter());

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(p, interactDistance);
    }
}*/


using System.Reflection;
using UnityEngine;

/// <summary>
/// Attach to the door's HINGE PIVOT object (door mesh = child of it).
/// No trigger collider needed. Detection uses a fixed point at the center of the doorway,
/// so it works from BOTH sides, whether the door is open or closed.
/// The door should be CLOSED when the game starts.
///
/// E = open / close.   L = lock / unlock (needs the key item, and the door must be closed).
/// Motion: Hinge = swings like a normal door. Slide = moves in a straight line (shutters, sliding gates).
/// Teleport = for doors that can't open (fixed door models): E moves the player to the other side.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class Door : MonoBehaviour
{
    public enum MotionType { Hinge, Slide, Teleport }

    [Header("Detection")]
    public string playerTag = "Player";
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("How close the player must be (horizontal distance to the doorway center).")]
    public float interactDistance = 2.5f;
    public bool requireFacingDoor = true;
    [Range(0f, 1f)]
    [Tooltip("1 = must look straight at the door, 0 = anywhere within 90 degrees.")]
    public float facingThreshold = 0.4f;
    [Tooltip("Optional. If empty, the doorway center is calculated from the door mesh automatically.")]
    public Transform interactPoint;

    [Header("Motion")]
    [Tooltip("Hinge = swings open like a normal door. Slide = moves in a straight line (shutters, sliding doors, gates). Teleport = for fixed door models: E moves the player to the Teleport Target.")]
    public MotionType motion = MotionType.Hinge;

    [Header("Hinge settings (Motion = Hinge)")]
    public float openAngle = 90f;
    [Tooltip("Degrees per second.")]
    public float rotateSpeed = 180f;
    public bool openAwayFromPlayer = true;

    [Header("Slide settings (Motion = Slide)")]
    [Tooltip("How far the door moves when open, in the PARENT object's local axes. E.g. (0, 6.5, 0) lifts a shutter up.")]
    public Vector3 slideOffset = new Vector3(0f, 3f, 0f);
    [Tooltip("Meters per second.")]
    public float slideSpeed = 2f;

    [Header("Teleport settings (Motion = Teleport)")]
    [Tooltip("An empty object on the other side of the door. The player ends up here, facing the way the object's blue arrow points.")]
    public Transform teleportTarget;
    public bool matchTargetRotation = true;
    [Tooltip("Quick fade to black while moving.")]
    public bool fadeScreen = true;
    [Tooltip("Played when you go through. If empty, the Open Sound is used.")]
    public AudioClip teleportSound;
    [Tooltip("Optional. Typing a name here lets a task step finish with Signal Name = that name, e.g. EnteredHouse.")]
    public string signalOnUse;
    [Tooltip("Drag your player movement / mouse-look scripts here so you can't move during the fade.")]
    public Behaviour[] freezeWhileTeleporting;

    [Header("Lock")]
    [Tooltip("Turn OFF for doors that should never be locked (no L prompt, L does nothing).")]
    public bool canBeLocked = true;
    public bool startsLocked = false;
    [Tooltip("Item needed to lock/unlock this door (must match a PickupItem's Item Name). Leave empty = no key needed.")]
    public string requiredKey = "House Key";
    public KeyCode lockKey = KeyCode.L;

    [Header("Sound")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockSound;
    public AudioClip unlockSound;
    [Tooltip("Played when you try to open a locked door.")]
    public AudioClip lockedSound;

    [Header("UI")]
    [Tooltip("Your existing 'Press E' UI object.")]
    public GameObject promptUI;
    [Tooltip("Optional. The text inside the E prompt. If empty, the first Text / TextMeshPro under Prompt UI is used.")]
    public GameObject promptText;
    public string openLabel = "Open";
    public string closeLabel = "Close";
    [Tooltip("Shown instead of Open while the door is locked.")]
    public string lockedLabel = "Locked";
    [Tooltip("{0} = key, {1} = Open / Close / Locked label")]
    public string promptFormat = "[{0}] {1}";
    [Tooltip("Your 'Press L' UI object. Only shown when the player can lock/unlock this door (has the key, door is closed).")]
    public GameObject lockPromptUI;
    [Tooltip("Optional. The text inside the L prompt. If empty, the first Text / TextMeshPro under Lock Prompt UI is used.")]
    public GameObject lockPromptText;
    public string lockLabel = "Lock";
    public string unlockLabel = "Unlock";
    [Tooltip("{0} = key, {1} = Lock / Unlock label")]
    public string lockPromptFormat = "[{0}] {1}";

    /// <summary>True while the door is locked. Locked doors can't be opened.</summary>
    public bool IsLocked { get; private set; }
    public bool IsOpen => isOpen;

    // The one door the player can currently interact with (nearest eligible door)
    private static Door activeDoor;

    private AudioSource audioSource;
    private Quaternion closedRotation;
    private Quaternion targetRotation;
    private Vector3 closedPosition;
    private Vector3 targetPosition;
    private Transform player;
    private Vector3 localPoint;   // doorway center, stored relative to the parent
    private float lastDistance;
    private bool wasActive;
    private bool wasShowingLock;
    private int lastPromptState = -1;
    private Component promptTextComponent;   // works with UI Text and TextMeshPro
    private PropertyInfo promptTextProperty;
    private bool lastLockedState;
    private string lastLockText;
    private Component lockTextComponent;   // works with UI Text and TextMeshPro
    private PropertyInfo lockTextProperty;
    private bool isOpen;
    private bool teleporting;
    private bool warnedNoPlayer;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        closedRotation = transform.localRotation;
        targetRotation = closedRotation;
        closedPosition = transform.localPosition;
        targetPosition = closedPosition;
        IsLocked = startsLocked;

        // Snapshot the doorway center while the door is closed
        Vector3 worldPoint = interactPoint != null ? interactPoint.position : CalculateDoorCenter();
        localPoint = transform.parent != null ? transform.parent.InverseTransformPoint(worldPoint) : worldPoint;

        if (promptUI != null)
        {
            FindTextComponent(promptText != null ? promptText : promptUI,
                out promptTextComponent, out promptTextProperty);
            promptUI.SetActive(false);
        }

        if (lockPromptUI != null)
        {
            CacheLockText();
            lockPromptUI.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (activeDoor == this) activeDoor = null;
    }

    private void Update()
    {
        if (player == null) FindPlayer();

        // Work out if THIS door is the one the player can use
        bool eligible = IsEligible(out float distance);
        lastDistance = distance;

        if (eligible)
        {
            if (activeDoor == null || activeDoor == this || distance < activeDoor.lastDistance)
                activeDoor = this;
        }
        else if (activeDoor == this)
        {
            activeDoor = null;
        }

        // Items take priority, so E doesn't open a door while you grab a key next to it
        if (activeDoor == this && !PickupItem.IsFocusingItem)
        {
            if (Input.GetKeyDown(interactKey)) TryOpenOrClose();
            if (Input.GetKeyDown(lockKey)) TryToggleLock();
        }

        // Smooth movement
        if (motion == MotionType.Hinge)
        {
            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation, targetRotation, rotateSpeed * Time.deltaTime);
        }
        else if (motion == MotionType.Slide)
        {
            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition, targetPosition, slideSpeed * Time.deltaTime);
        }
    }

    private void LateUpdate()
    {
        bool active = activeDoor == this;

        // "Press E" prompt
        if (promptUI != null)
        {
            if (active)
            {
                // Refresh the Open / Close / Locked label when the prompt appears or the state changes
                int state = (IsLocked && !isOpen) ? 2 : (isOpen ? 1 : 0);
                if (!wasActive || state != lastPromptState)
                {
                    lastPromptState = state;
                    string label = state == 2 ? lockedLabel : (state == 1 ? closeLabel : openLabel);
                    SetText(promptTextComponent, promptTextProperty, string.Format(promptFormat, interactKey, label));
                }

                promptUI.SetActive(true);
            }
            else if (wasActive)
            {
                // Only hide if nobody else is using the same UI object
                if (activeDoor == null || activeDoor.promptUI != promptUI)
                    promptUI.SetActive(false);
            }
        }

        // "Press L" prompt: only while the player can actually lock/unlock this door
        if (lockPromptUI != null)
        {
            bool showLock = CanShowLockPrompt();

            if (showLock)
            {
                // Refresh the "Lock" / "Unlock" label when it appears or the lock state changes
                if (!wasShowingLock || lastLockText == null || lastLockedState != IsLocked)
                {
                    lastLockedState = IsLocked;
                    lastLockText = string.Format(lockPromptFormat, lockKey, IsLocked ? unlockLabel : lockLabel);
                    SetLockText(lastLockText);
                }

                lockPromptUI.SetActive(true);
            }
            else if (wasShowingLock)
            {
                // Don't hide it if another door is about to show the same UI object
                if (activeDoor == null || activeDoor.lockPromptUI != lockPromptUI || !activeDoor.CanShowLockPrompt())
                    lockPromptUI.SetActive(false);
            }

            wasShowingLock = showLock;
        }

        wasActive = active;
    }

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

    private static void SetText(Component comp, PropertyInfo prop, string value)
    {
        if (comp != null && prop != null) prop.SetValue(comp, value);
    }

    private void CacheLockText()
    {
        GameObject root = lockPromptText != null ? lockPromptText : lockPromptUI;
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
                    lockTextComponent = c;
                    lockTextProperty = prop;
                    return;
                }
            }
        }

        Debug.LogWarning($"Door '{name}': no Text or TextMeshPro found under the Lock Prompt UI. " +
                         "Assign the text object in 'Lock Prompt Text'.");
    }

    private void SetLockText(string value)
    {
        if (lockTextProperty != null && lockTextComponent != null)
            lockTextProperty.SetValue(lockTextComponent, value);
    }

    private bool CanShowLockPrompt()
    {
        return canBeLocked
            && activeDoor == this
            && !PickupItem.IsFocusingItem
            && !isOpen
            && IsFullyClosed()
            && HasKey();
    }

    // ---------------------------------------------------------------- Open / close / lock

    private void TryOpenOrClose()
    {
        if (teleporting) return;

        if (IsLocked && !isOpen)
        {
            PlaySound(lockedSound);

            if (HasKey()) HintMessage.Show($"Locked. Press [{lockKey}] to unlock.");
            else HintMessage.Show($"Locked. You need the {requiredKey}.");
            return;
        }

        Toggle();
    }

    private void TryToggleLock()
    {
        if (!canBeLocked) return;

        // Can't lock a door that's open (or still swinging shut)
        if (isOpen || !IsFullyClosed())
        {
            HintMessage.Show("Close the door first.");
            return;
        }

        if (!HasKey())
        {
            PlaySound(lockedSound);
            HintMessage.Show($"You need the {requiredKey}.");
            return;
        }

        IsLocked = !IsLocked;
        PlaySound(IsLocked ? lockSound : unlockSound);
        HintMessage.Show(IsLocked ? "Door locked." : "Door unlocked.");
    }

    private bool HasKey()
    {
        if (string.IsNullOrWhiteSpace(requiredKey)) return true;
        return TaskListManager.Instance != null && TaskListManager.Instance.HasItem(requiredKey);
    }

    private bool IsEligible(out float distance)
    {
        distance = float.MaxValue;
        if (player == null) return false;

        Vector3 point = GetDoorPoint();

        // Horizontal distance only
        Vector3 flatToDoor = point - player.position;
        flatToDoor.y = 0f;
        distance = flatToDoor.magnitude;
        if (distance > interactDistance) return false;

        if (!requireFacingDoor) return true;

        Transform looker = Camera.main != null ? Camera.main.transform : player;

        Vector3 forward = looker.forward;
        forward.y = 0f;
        Vector3 toDoor = point - looker.position;
        toDoor.y = 0f;

        if (forward.sqrMagnitude < 0.0001f || toDoor.sqrMagnitude < 0.0001f) return true;

        return Vector3.Dot(forward.normalized, toDoor.normalized) >= facingThreshold;
    }

    private bool IsFullyClosed()
    {
        if (motion == MotionType.Teleport) return true; // a teleport door never "opens"

        if (motion == MotionType.Hinge)
            return Quaternion.Angle(transform.localRotation, closedRotation) <= 1f;

        return Vector3.Distance(transform.localPosition, closedPosition) <= 0.02f;
    }

    private void Toggle()
    {
        if (motion == MotionType.Teleport)
        {
            StartTeleport();
            return;
        }

        if (!isOpen)
        {
            if (motion == MotionType.Hinge)
            {
                float direction = 1f;

                if (openAwayFromPlayer && player != null)
                {
                    Vector3 toPlayer = player.position - transform.position;
                    direction = Vector3.Dot(transform.forward, toPlayer) > 0f ? -1f : 1f;
                }

                targetRotation = closedRotation * Quaternion.Euler(0f, openAngle * direction, 0f);
            }
            else
            {
                targetPosition = closedPosition + slideOffset;
            }

            isOpen = true;
            PlaySound(openSound);
        }
        else
        {
            targetRotation = closedRotation;
            targetPosition = closedPosition;
            isOpen = false;
            PlaySound(closeSound);
        }
    }

    // ---------------------------------------------------------------- Teleport

    private void StartTeleport()
    {
        if (teleporting) return;

        if (teleportTarget == null)
        {
            Debug.LogWarning($"Door '{name}': Motion is Teleport but no Teleport Target is set.");
            return;
        }

        teleporting = true;
        PlaySound(teleportSound != null ? teleportSound : openSound);

        // Freeze the player's controls while the screen fades
        Behaviour[] frozen = freezeWhileTeleporting ?? new Behaviour[0];
        bool[] wasOn = new bool[frozen.Length];
        for (int i = 0; i < frozen.Length; i++)
        {
            if (frozen[i] == null) continue;
            wasOn[i] = frozen[i].enabled;
            frozen[i].enabled = false;
        }

        System.Action finish = () =>
        {
            for (int i = 0; i < frozen.Length; i++)
            {
                if (frozen[i] != null && wasOn[i]) frozen[i].enabled = true;
            }
            teleporting = false;
        };

        if (fadeScreen)
        {
            ScreenFader.Run(MovePlayer, finish);
        }
        else
        {
            MovePlayer();
            finish();
        }
    }

    // Runs while the screen is black
    private void MovePlayer()
    {
        if (player == null || teleportTarget == null) return;

        Transform mover = GetPlayerMover();

        // A CharacterController would undo the move, so switch it off for a moment
        CharacterController controller = mover.GetComponent<CharacterController>();
        bool controllerWasOn = controller != null && controller.enabled;
        if (controllerWasOn) controller.enabled = false;

        mover.position = teleportTarget.position;
        if (matchTargetRotation)
            mover.rotation = Quaternion.Euler(0f, teleportTarget.eulerAngles.y, 0f);

        Rigidbody body = mover.GetComponent<Rigidbody>();
        if (body != null)
        {
#if UNITY_6000_0_OR_NEWER
            body.linearVelocity = Vector3.zero;
#else
            body.velocity = Vector3.zero;
#endif
        }

        if (controllerWasOn) controller.enabled = true;
        Physics.SyncTransforms();

        // Lets a task step finish with Signal Name = Signal On Use
        if (!string.IsNullOrWhiteSpace(signalOnUse) && TaskListManager.Instance != null)
            TaskListManager.Instance.SendSignal(signalOnUse);
    }

    // The object that really moves (the one with the CharacterController)
    private Transform GetPlayerMover()
    {
        CharacterController controller = player.GetComponentInParent<CharacterController>();
        if (controller == null) controller = player.GetComponentInChildren<CharacterController>();
        return controller != null ? controller.transform : player;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null) audioSource.PlayOneShot(clip);
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindWithTag(playerTag);
        if (p != null)
        {
            player = p.transform;
        }
        else if (!warnedNoPlayer)
        {
            Debug.LogWarning($"Door '{name}': no object with tag '{playerTag}' found. Tag your player!");
            warnedNoPlayer = true;
        }
    }

    private Vector3 GetDoorPoint()
    {
        return transform.parent != null ? transform.parent.TransformPoint(localPoint) : localPoint;
    }

    private Vector3 CalculateDoorCenter()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return transform.position;

        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b.center;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 p = Application.isPlaying
            ? GetDoorPoint()
            : (interactPoint != null ? interactPoint.position : CalculateDoorCenter());

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(p, interactDistance);
    }
}