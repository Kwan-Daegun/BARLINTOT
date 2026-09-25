using UnityEngine;

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
}