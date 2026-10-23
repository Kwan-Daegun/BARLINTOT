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


using System.Collections.Generic;
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
    private bool isPickupVisit;   // true when the customer comes back to collect a finished lapida

    /// <summary>
    /// Fires after the player has marked all the documents.
    /// (customer data, number of mistakes: 0 = perfect). Used later for sanity / ghost reactions.
    /// </summary>
    public static event System.Action<NPCData, int> OnVerdict;

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

    /// <summary>Called by the spawner when this customer is coming back for their finished lapida.</summary>
    public void MarkAsPickup()
    {
        isPickupVisit = true;
        docsChecked = true;
    }

    private void HandleReachedCounter()
    {
        if (isPickupVisit) movementController.SetPresentingDocs(); // waiting to receive the lapida
        else movementController.SetAskingOrder();
    }

    public string GetPromptText()
    {
        // No prompt while one of the popups is open
        if (OrderChoiceUI.IsOpen || DocumentCheckUI.IsOpen) return string.Empty;

        switch (movementController.CurrentState)
        {
            case NPCMovementController.NPCState.AskingOrder:
                return "[E] Take Order";

            case NPCMovementController.NPCState.PresentingDocs:
                if (isPickupVisit) return "[E] Give Lapida";   // only when they come back for it
                return docsChecked ? string.Empty : "[E] Check Documents";

            default:
                return string.Empty;
        }
    }

    public void Interact()
    {
        if (OrderChoiceUI.IsOpen || DocumentCheckUI.IsOpen) return;

        switch (movementController.CurrentState)
        {
            case NPCMovementController.NPCState.AskingOrder:
                if (OrderChoiceUI.Instance == null)
                {
                    // No popup set up in the scene: take the order right away
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
                if (isPickupVisit)
                {
                    DeliverLapida();
                    return;
                }

                if (docsChecked) return;

                if (DocumentCheckUI.Instance == null)
                {
                    // No document screen set up in the scene: treat every document as approved
                    Verdict(new[] { true, true, true, true });
                    return;
                }

                DocumentCheckUI.Instance.Show(
                    movementController.npcData,
                    Verdict,
                    () => movementController != null &&
                          movementController.CurrentState == NPCMovementController.NPCState.PresentingDocs &&
                          !docsChecked);
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

    // The player marked every document. marks[i] = true means "Yes, it's right", false means "No, it's wrong".
    // Order: customer ID, deceased ID, photo, death document.
    private void Verdict(bool[] marks)
    {
        if (docsChecked) return;
        docsChecked = true;

        NPCData data = movementController.npcData;
        bool[] truth = data.GetDocumentTruth();

        bool approved = true;   // the order only goes ahead if EVERY document was marked Yes
        int mistakes = 0;
        List<string> notes = new List<string>();

        for (int i = 0; i < truth.Length && i < marks.Length; i++)
        {
            if (!marks[i]) approved = false;

            if (marks[i] != truth[i])
            {
                mistakes++;
                notes.Add(NPCData.DocumentNames[i] + (truth[i] ? " was fine" : " was wrong"));
            }
        }

        string result = mistakes == 0
            ? "All documents checked correctly."
            : "Mistakes: " + string.Join(", ", notes) + ".";

        // The player let something wrong through: explain what it was
        if (approved && !data.DocumentsMatch()) result += "\n" + data.DescribeMismatch();

        if (OnVerdict != null) OnVerdict.Invoke(data, mistakes);
        if (TaskListManager.Instance != null) TaskListManager.Instance.SendSignal("DocsChecked");

        if (!approved)
        {
            // At least one document was marked No: the customer leaves and the order is cancelled
            HintMessage.Show(result);
            GameManager.Instance.RejectCustomer();
            return;
        }

        // Everything marked Yes: the customer pays and leaves. The order stays, so the lapida can be started.
        int payment = data.payment;
        if (PlayerStats.Instance != null) PlayerStats.Instance.AddMoney(payment);

        GameManager.Instance.MarkOrderPaid();
        GameManager.Instance.RegisterNPC(null);   // the order stays, the customer doesn't
        movementController.ServeOrReject();

        HintMessage.Show(result + "\nPayment received: " + PlayerStats.FormatMoney(payment));

        if (TaskListManager.Instance != null) TaskListManager.Instance.SendSignal("OrderPaid");
    }

    // The customer came back and the lapida is finished
    private void DeliverLapida()
    {
        if (GameManager.Instance.TryDeliverLapida())
            HintMessage.Show("Thank you. It's perfect.");
        else
            HintMessage.Show("The lapida isn't finished yet.");
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