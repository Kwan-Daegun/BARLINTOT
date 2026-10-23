/*using UnityEngine;

public class NPCInteractable : MonoBehaviour, IInteractable
{
    private NPCMovementController movementController;

    [Header("Table Props")]
    [SerializeField] private GameObject idPropInstance;
    [SerializeField] private GameObject[] pictureProps;
    [SerializeField] private Transform tableIdSlot;
    [SerializeField] private Transform[] tablePictureSlots;

    private void Awake()
    {
        movementController = GetComponent<NPCMovementController>();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterNPC(movementController);
        }

        movementController.onReachedCounter += HandleReachedCounter;
    }

    private void OnDestroy()
    {
        if (movementController != null)
        {
            movementController.onReachedCounter -= HandleReachedCounter;
        }
    }

    private void HandleReachedCounter()
    {
        movementController.SetAskingOrder();
    }

    public string GetPromptText()
    {
        return movementController.CurrentState switch
        {
            NPCMovementController.NPCState.AskingOrder => "[E] Take Order",
            NPCMovementController.NPCState.PresentingDocs => "[E] Give Lapida",
            _ => string.Empty
        };
    }

    public void Interact()
    {
        switch (movementController.CurrentState)
        {
            case NPCMovementController.NPCState.AskingOrder:
                GameManager.Instance.AcceptOrder(movementController.npcData);
                movementController.SetPresentingDocs();
                PlaceDocsOnTable();
                break;

            case NPCMovementController.NPCState.PresentingDocs:
                GameManager.Instance.TryDeliverLapida();
                movementController.ServeOrReject();
                break;
        }
    }

    private void PlaceDocsOnTable()
    {
        if (idPropInstance != null && tableIdSlot != null)
        {
            idPropInstance.SetActive(true);
            idPropInstance.transform.position = tableIdSlot.position;
            idPropInstance.transform.rotation = tableIdSlot.rotation;
        }

        for (int i = 0; i < pictureProps.Length && i < tablePictureSlots.Length; i++)
        {
            pictureProps[i].SetActive(true);
            pictureProps[i].transform.position = tablePictureSlots[i].position;
            pictureProps[i].transform.rotation = tablePictureSlots[i].rotation;
        }
    }
}*/


using UnityEngine;

public class NPCInteractable : MonoBehaviour, IInteractable
{
    private NPCMovementController movementController;

    [Header("Table Props")]
    [SerializeField] private GameObject idPropInstance;
    [SerializeField] private GameObject[] pictureProps;
    [SerializeField] private Transform tableIdSlot;
    [SerializeField] private Transform[] tablePictureSlots;

    private bool docsChecked;

    /// <summary>
    /// Fires after the player approves or rejects the documents.
    /// (customer data, was the player's decision correct?) Used later for sanity / ghost reactions.
    /// </summary>
    public static event System.Action<NPCData, bool> OnVerdict;

    private void Awake()
    {
        movementController = GetComponent<NPCMovementController>();
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterNPC(movementController);
        }

        movementController.onReachedCounter += HandleReachedCounter;
    }

    private void OnDestroy()
    {
        if (movementController != null)
        {
            movementController.onReachedCounter -= HandleReachedCounter;
        }
    }

    private void HandleReachedCounter()
    {
        movementController.SetAskingOrder();
    }

    public string GetPromptText()
    {
        // No prompt while one of the popups is open
        if (OrderChoiceUI.IsOpen || DocumentCheckUI.IsOpen) return string.Empty;

        return movementController.CurrentState switch
        {
            NPCMovementController.NPCState.AskingOrder => "[E] Take Order",
            NPCMovementController.NPCState.PresentingDocs => docsChecked ? "[E] Give Lapida" : "[E] Check Documents",
            _ => string.Empty
        };
    }

    public void Interact()
    {
        if (OrderChoiceUI.IsOpen || DocumentCheckUI.IsOpen) return;

        switch (movementController.CurrentState)
        {
            case NPCMovementController.NPCState.AskingOrder:
                if (OrderChoiceUI.Instance == null)
                {
                    // No popup set up in the scene: take the order right away, like before
                    AcceptCurrentOrder();
                    return;
                }

                // "Take this order?  Yes / No"
                OrderChoiceUI.Instance.Show(
                    AcceptCurrentOrder,
                    DeclineCurrentOrder,
                    () => movementController != null &&
                          movementController.CurrentState == NPCMovementController.NPCState.AskingOrder);
                break;

            case NPCMovementController.NPCState.PresentingDocs:
                // First E: look at the documents and decide. After that, E gives the lapida.
                if (!docsChecked && DocumentCheckUI.Instance != null)
                {
                    DocumentCheckUI.Instance.Show(
                        movementController.npcData,
                        () => Verdict(true),
                        () => Verdict(false),
                        () => movementController != null &&
                              movementController.CurrentState == NPCMovementController.NPCState.PresentingDocs &&
                              !docsChecked);
                    return;
                }

                GameManager.Instance.TryDeliverLapida();
                movementController.ServeOrReject();
                break;
        }
    }

    // YES: the customer hands over the documents
    private void AcceptCurrentOrder()
    {
        if (movementController.CurrentState != NPCMovementController.NPCState.AskingOrder) return;

        GameManager.Instance.AcceptOrder(movementController.npcData);
        movementController.SetPresentingDocs();
        PlaceDocsOnTable();

        // The customer says their name once. The player has to remember it to spot a wrong ID.
        HintMessage.Show($"\"I'm {movementController.npcData.npcName}.\"");

        // Lets a task step finish with Signal Name = OrderTaken
        if (TaskListManager.Instance != null) TaskListManager.Instance.SendSignal("OrderTaken");
    }

    // NO: the customer leaves
    private void DeclineCurrentOrder()
    {
        if (movementController.CurrentState != NPCMovementController.NPCState.AskingOrder) return;

        GameManager.Instance.RejectCustomer();
    }

    // The player pressed check mark (approve = true) or X (approve = false)
    private void Verdict(bool approved)
    {
        if (docsChecked) return;
        docsChecked = true;

        NPCData data = movementController.npcData;
        bool documentsAreFine = data.DocumentsMatch();
        bool correct = approved == documentsAreFine;

        if (correct)
        {
            HintMessage.Show(approved
                ? "Everything matches. Good."
                : "Good catch. Those documents were wrong.");
        }
        else
        {
            HintMessage.Show(approved
                ? "Wrong! " + data.DescribeMismatch()
                : "Wrong! Those documents were fine.");
        }

        if (OnVerdict != null) OnVerdict.Invoke(data, correct);
        if (TaskListManager.Instance != null) TaskListManager.Instance.SendSignal("DocsChecked");

        // X sends the customer away and cancels the order
        if (!approved) GameManager.Instance.RejectCustomer();
    }

    private void PlaceDocsOnTable()
    {
        if (idPropInstance != null && tableIdSlot != null)
        {
            idPropInstance.SetActive(true);
            idPropInstance.transform.position = tableIdSlot.position;
            idPropInstance.transform.rotation = tableIdSlot.rotation;
        }

        for (int i = 0; i < pictureProps.Length && i < tablePictureSlots.Length; i++)
        {
            pictureProps[i].SetActive(true);
            pictureProps[i].transform.position = tablePictureSlots[i].position;
            pictureProps[i].transform.rotation = tablePictureSlots[i].rotation;
        }
    }
}