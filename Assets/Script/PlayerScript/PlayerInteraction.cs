using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    public float interactionDistance = 3f;
    public LayerMask interactableLayer;
    public GameObject promptUI;
    public TextMeshProUGUI promptText;

    private Camera playerCamera;
    private IInteractable currentInteractable;

    private void Awake()
    {
        playerCamera = Camera.main;
        if (promptUI != null) promptUI.SetActive(false);
    }

    private void Update()
    {
        CheckForInteractable();
        HandleInteraction();
    }

    private void CheckForInteractable()
    {
        Ray ray = new(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer))
        {
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                currentInteractable = interactable;

                if (promptUI != null) promptUI.SetActive(true);
                if (promptText != null) promptText.text = currentInteractable.GetPromptText();

                return;
            }
        }

        currentInteractable = null;
        if (promptUI != null) promptUI.SetActive(false);
    }

    private void HandleInteraction()
    {
        if (currentInteractable != null && Input.GetKeyDown(KeyCode.E))
        {
            currentInteractable.Interact();
        }
    }
}