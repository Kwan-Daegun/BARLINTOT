using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "Item", menuName = "ScriptableObjects/Item")]
public class Item : ScriptableObject
{
    public UnityEvent itemBought = new();

    public string itemName;
    public Sprite itemSprite;
}