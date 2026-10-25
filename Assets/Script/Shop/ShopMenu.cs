using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopMenu : MonoBehaviour
{
    public static ShopMenu Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI shopLabel;
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject itemCardPrefab;
    [SerializeField] private Button exitBtn;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (exitBtn != null) 
            exitBtn.onClick.AddListener(() => ShowMenu(false));

        ShowMenu(false);
    }

    public void OpenShop(Item[] itemsToDisplay, string shopName = "Shop")
    {
        shopLabel.text = shopName;
        
        PopulateShop(itemsToDisplay);
        ShowMenu(true);
    }

    private void PopulateShop(Item[] items)
    {
        // Clear previous UI nodes
        foreach (Transform child in itemContainer)
        {
            Destroy(child.gameObject);
        }

        if (items == null || itemCardPrefab == null || itemContainer == null) return;

        // Instantiate and initialize new item cards
        foreach (Item item in items)
        {
            if (item == null) continue;

            GameObject newItemNode = Instantiate(itemCardPrefab, itemContainer);
            ItemCard itemCard = newItemNode.GetComponent<ItemCard>();

            if (itemCard != null)
            {
                itemCard.SetupCard(item);
            }
        }
    }

    public void ShowMenu(bool openMenu)
    {
        gameObject.SetActive(openMenu);
        CamLook.SetUIOpen(openMenu);
    }
}