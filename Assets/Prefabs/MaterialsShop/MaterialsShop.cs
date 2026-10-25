using UnityEngine;

public class MaterialsShop : MonoBehaviour, IInteractable
{
    public Item[] materialItems;

    public string GetPromptText()
    {
        return "Buy materials";
    }

    public void Interact()
    {
        ShopMenu.Instance.OpenShop(materialItems);
    }
}
