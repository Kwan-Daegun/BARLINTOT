using UnityEngine;
using TMPro;

[RequireComponent(typeof(NPCMovementController))]
public class NPCDebugOverlay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI overlayText;
    [SerializeField] private bool isEnabled = true;

    private NPCMovementController movementController;
    private Transform cameraTransform;

    private void Awake()
    {
        movementController = GetComponent<NPCMovementController>();
        cameraTransform = Camera.main != null ? Camera.main.transform : null;
    }

    private void Start()
    {
        movementController.onStateChanged += UpdateText;
        UpdateText(movementController.CurrentState);

        if (overlayText != null)
        {
            overlayText.gameObject.SetActive(isEnabled);
        }
    }

    private void OnDestroy()
    {
        if (movementController != null)
        {
            movementController.onStateChanged -= UpdateText;
        }
    }

    private void UpdateText(NPCMovementController.NPCState state)
    {
        if (overlayText == null) return;

        string npcName = movementController.npcData != null ? movementController.npcData.npcName : name;
        overlayText.text = $"{npcName}\n is {state}";
    }

    private void LateUpdate()
    {
        if (overlayText == null || cameraTransform == null) return;

        overlayText.transform.LookAt(cameraTransform);
        overlayText.transform.Rotate(0f, 180f, 0f);
    }
}