/*using UnityEngine;
using System.Collections;

public class ShutterButton : MonoBehaviour, IInteractable
{
    public Transform shutterPivot;
    public float openYScale = 0.05f;
    public float closedYScale = 1.0f;
    public float squishSpeed = 7f;
    public AudioClip shutterSound;
    public AudioSource shutterAudioSource;

    private bool isOpen = true;
    private bool isMoving = false;

    private void Start()
    {
        Vector3 startScale = shutterPivot.localScale;
        startScale.y = openYScale;
        shutterPivot.localScale = startScale;
        isOpen = true;
    }

    public string GetPromptText()
    {
        if (isMoving) return "Moving...";
        return isOpen ? "[E] Close Shutter" : "[E] Open Shutter";
    }

    public void Interact()
    {
        if (isMoving) return;
        AudioSource.PlayClipAtPoint(shutterSound, transform.position);
        StartCoroutine(SquishShutter());
    }

    private IEnumerator SquishShutter()
    {
        isMoving = true;
        isOpen = !isOpen;

        if (shutterAudioSource != null)
            shutterAudioSource.Play();

        if (!isOpen)
            GameManager.Instance.RejectCustomer();

        float targetY = isOpen ? openYScale : closedYScale;
        Vector3 targetScale = new Vector3(shutterPivot.localScale.x, targetY, shutterPivot.localScale.z);

        while (Mathf.Abs(shutterPivot.localScale.y - targetY) > 0.01f)
        {
            shutterPivot.localScale = Vector3.Lerp(shutterPivot.localScale, targetScale, squishSpeed * Time.deltaTime);
            yield return null;
        }

        shutterPivot.localScale = targetScale;

        if (shutterAudioSource != null)
            shutterAudioSource.Stop();

        isMoving = false;
    }
}*/


using UnityEngine;
using System.Collections;

public class ShutterButton : MonoBehaviour, IInteractable
{
    public Transform shutterPivot;
    public float openYScale = 0.05f;
    public float closedYScale = 1.0f;
    public float squishSpeed = 7f;
    public AudioClip shutterSound;
    public AudioSource shutterAudioSource;

    [Tooltip("Untick to start with the shutter CLOSED (e.g. so the player needs the Shop Key to get in).")]
    public bool startsOpen = true;

    private bool isOpen = true;
    private bool isMoving = false;

    // Other scripts (like ShutterAccess) can read the state
    public bool IsOpen => isOpen;
    public bool IsMoving => isMoving;

    private void Start()
    {
        Vector3 startScale = shutterPivot.localScale;
        startScale.y = startsOpen ? openYScale : closedYScale;
        shutterPivot.localScale = startScale;
        isOpen = startsOpen;
    }

    public string GetPromptText()
    {
        if (isMoving) return "Moving...";
        return isOpen ? "[E] Close Shutter" : "[E] Open Shutter";
    }

    // The button inside the shop
    public void Interact()
    {
        Toggle();
    }

    /// <summary>Open if closed, close if open. Used by the button AND by ShutterAccess.</summary>
    public void Toggle()
    {
        if (isMoving) return;

        if (shutterSound != null)
            AudioSource.PlayClipAtPoint(shutterSound, transform.position);

        StartCoroutine(SquishShutter());
    }

    /// <summary>Move the shutter to the given state (does nothing if it's already there or moving).</summary>
    public void SetOpen(bool open)
    {
        if (isMoving || isOpen == open) return;
        Toggle();
    }

    private IEnumerator SquishShutter()
    {
        isMoving = true;
        isOpen = !isOpen;

        if (shutterAudioSource != null)
            shutterAudioSource.Play();

        if (!isOpen)
            GameManager.Instance.RejectCustomer();

        float targetY = isOpen ? openYScale : closedYScale;
        Vector3 targetScale = new Vector3(shutterPivot.localScale.x, targetY, shutterPivot.localScale.z);

        while (Mathf.Abs(shutterPivot.localScale.y - targetY) > 0.01f)
        {
            shutterPivot.localScale = Vector3.Lerp(shutterPivot.localScale, targetScale, squishSpeed * Time.deltaTime);
            yield return null;
        }

        shutterPivot.localScale = targetScale;

        if (shutterAudioSource != null)
            shutterAudioSource.Stop();

        isMoving = false;
    }
}