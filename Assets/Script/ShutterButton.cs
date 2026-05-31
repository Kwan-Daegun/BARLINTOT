using UnityEngine;
using System.Collections;

public class ShutterButton : MonoBehaviour, IInteractable
{
    public Transform shutterPivot;
    public float openYScale = 0.05f;
    public float closedYScale = 1.0f;
    public float squishSpeed = 7f;

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
        StartCoroutine(SquishShutter());
    }

    private IEnumerator SquishShutter()
    {
        isMoving = true;
        isOpen = !isOpen;

        if (!isOpen)
        {
            GameManager.Instance.RejectCustomer();
        }

        float targetY = isOpen ? openYScale : closedYScale;
        Vector3 targetScale = new Vector3(shutterPivot.localScale.x, targetY, shutterPivot.localScale.z);

        while (Mathf.Abs(shutterPivot.localScale.y - targetY) > 0.01f)
        {
            shutterPivot.localScale = Vector3.Lerp(shutterPivot.localScale, targetScale, squishSpeed * Time.deltaTime);
            yield return null;
        }

        shutterPivot.localScale = targetScale;
        isMoving = false;
    }
}