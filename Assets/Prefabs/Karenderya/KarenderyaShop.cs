using UnityEngine;
using UnityEngine.Events;

public class KarenderyaShop : MonoBehaviour
{
    public UnityEvent<Item> foodBought;

    public GameObject foodItemNode;
    public Transform container;

    [Space]
    public Item[] foods;

    private CanvasGroup canvasGroup;

    void Start()
    {
        foreach(Item food in foods)
        {
            ItemCard newFoodItem = Instantiate(foodItemNode, container).GetComponent<ItemCard>();
            newFoodItem.SetupCard(food);
            newFoodItem.buyBtn.onClick.AddListener(() => BuyFood(food));
        }

        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        OpenMenu(false);
    }

    private void BuyFood(Item food)
    {
        foodBought.Invoke(food);
        OpenMenu(false);
    }

    public void OpenMenu(bool open)
    {
        float alpha = open? 1.0f : 0.0f;

        canvasGroup.alpha = alpha;
        canvasGroup.interactable = open;
        canvasGroup.blocksRaycasts = open;

        if (open)
            CamLook.SetUIOpen(true);
        else
            CamLook.SetUIOpen(false);
    }
}
