using UnityEngine;

/// <summary>
/// Attach to the door's HINGE PIVOT object (door mesh = child of it).
/// No trigger collider needed. Detection uses a fixed point at the center of the doorway,
/// so it works from BOTH sides, whether the door is open or closed.
/// The door should be CLOSED when the game starts.
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

    [Header("Sound")]
    public AudioClip openSound;
    public AudioClip closeSound;

    [Header("UI")]
    [Tooltip("Your existing 'Press E' UI object.")]
    public GameObject promptUI;

    // The one door the player can currently interact with (nearest eligible door)
    private static Door activeDoor;

    private AudioSource audioSource;
    private Quaternion closedRotation;
    private Quaternion targetRotation;
    private Transform player;
    private Vector3 localPoint;   // doorway center, stored relative to the parent
    private float lastDistance;
    private bool wasActive;
    private bool isOpen;
    private bool warnedNoPlayer;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        closedRotation = transform.localRotation;
        targetRotation = closedRotation;

        // Snapshot the doorway center while the door is closed
        Vector3 worldPoint = interactPoint != null ? interactPoint.position : CalculateDoorCenter();
        localPoint = transform.parent != null ? transform.parent.InverseTransformPoint(worldPoint) : worldPoint;

        if (promptUI != null) promptUI.SetActive(false);
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

        if (activeDoor == this && Input.GetKeyDown(interactKey))
            Toggle();

        // Smooth rotation
        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation, targetRotation, rotateSpeed * Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (promptUI == null) return;

        bool active = activeDoor == this;

        if (active)
        {
            promptUI.SetActive(true);
        }
        else if (wasActive)
        {
            // Only hide if nobody else is using the same UI object
            if (activeDoor == null || activeDoor.promptUI != promptUI)
                promptUI.SetActive(false);
        }

        wasActive = active;
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
}