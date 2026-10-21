using UnityEngine;
using UnityEngine.UI;

public class FoodMenu : MonoBehaviour
{
    [SerializeField] private NPCKarenderya karenderyaNPC;

    [Header("Food Items")]
    [SerializeField] private GameObject foodItemContainer;
    public GameObject foodItemNode;
    public Item[] foods;

    [Header("UI")]
    [SerializeField] private Button exitBtn;

    private void Start()
    {
        ShowMenu(false);

        if (karenderyaNPC == null)
        {
            Debug.LogError($"{gameObject.name} has no Karenderya NPC!");
            return;
        }

        karenderyaNPC.ShowMenu += OpenMenu;

        if (exitBtn != null) exitBtn.onClick.AddListener(() => { ShowMenu(false); });

        // Initialize foods
        if (foodItemContainer == null)
        {
            Debug.LogError($"{gameObject} has no Container Transform!");
            return;
        }
        
        foreach(Item food in foods)
        {
            GameObject newFoodItem = Instantiate(foodItemNode, foodItemContainer.transform);
            ItemCard foodItem = newFoodItem.GetComponent<ItemCard>();

            if (foodItem)
            {
                foodItem.cardLabel.text = food.itemName;
                foodItem.cardImage.sprite = food.itemSprite;


            }
        }
    }

    private void OpenMenu()
    {
        ShowMenu(true);
    }

    public void ShowMenu(bool openMenu)
    {
        if (openMenu)
        {
            gameObject.SetActive(true);
            CamLook.SetUIOpen(true);
        }
        else
        {
            gameObject.SetActive(false);
            CamLook.SetUIOpen(false);
        }
    }
}
