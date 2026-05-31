using UnityEngine;
using TMPro;

public class OrderTicket : MonoBehaviour, IInteractable
{
    public GameObject uiPaperCanvas;

    public TextMeshProUGUI nameText;
    public TextMeshProUGUI datesText;
    public TextMeshProUGUI epitaphText;

    private MeshRenderer paperMesh;
    private Collider paperCollider;
    private bool isUIVisible = false;
    private bool toggledThisFrame = false;

    private void Awake()
    {
        paperMesh = GetComponent<MeshRenderer>();
        paperCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        paperMesh.enabled = false;
        paperCollider.enabled = false;
        uiPaperCanvas.SetActive(false);
    }

    private void Update()
    {
        // When an order is taken, spawn the paper and auto-open the UI
        if (GameManager.Instance.hasActiveOrder && !paperMesh.enabled)
        {
            paperMesh.enabled = true;
            paperCollider.enabled = true;

            if (!isUIVisible)
            {
                ToggleUI();
                toggledThisFrame = true; // Prevents the interaction 'E' from closing it instantly
            }
        }
        // When lapida is delivered, remove the paper
        else if (!GameManager.Instance.hasActiveOrder && paperMesh.enabled)
        {
            paperMesh.enabled = false;
            paperCollider.enabled = false;
            if (isUIVisible) ToggleUI();
        }

        if (toggledThisFrame)
        {
            toggledThisFrame = false;
            return;
        }

        // Close UI if open
        if (isUIVisible && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)))
        {
            ToggleUI();
            toggledThisFrame = true;
        }
    }

    public string GetPromptText()
    {
        if (isUIVisible)
        {
            return "[E] Put Down Ticket";
        }
        return "[E] Read Ticket";
    }

    public void Interact()
    {
        if (!isUIVisible)
        {
            ToggleUI();
            toggledThisFrame = true;
        }
    }

    private void ToggleUI()
    {
        isUIVisible = !isUIVisible;
        uiPaperCanvas.SetActive(isUIVisible);

        if (isUIVisible && GameManager.Instance.currentOrderData != null)
        {
            nameText.text = GameManager.Instance.currentOrderData.orderDetails.deceasedName;
            datesText.text = GameManager.Instance.currentOrderData.orderDetails.birthDate + " - " + GameManager.Instance.currentOrderData.orderDetails.deathDate;
            epitaphText.text = GameManager.Instance.currentOrderData.orderDetails.message;
        }
    }
}