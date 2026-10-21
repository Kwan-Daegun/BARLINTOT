using UnityEngine;

/// <summary>
/// A ScriptableObject than holds an array of Items for shop owners have in the shop menu
/// </summary>
[CreateAssetMenu(fileName = "ShopInventory", menuName = "Shop/ShopInventory")]
public class ShopInventory : ScriptableObject
{
    public Item[] items;
}