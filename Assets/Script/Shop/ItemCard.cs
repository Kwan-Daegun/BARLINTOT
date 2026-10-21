using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles UI rendering and purchase logic for individual shop item cards.
/// </summary>
public class ItemCard : MonoBehaviour
{
    public TextMeshProUGUI cardLabel;
    public Image cardImage;
    public Button buyBtn;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip buySfx;

    private Item currentItem;

    /// <summary>
    /// Configures card display and binds the Item instance.
    /// </summary>
    public void SetupCard(Item item)
    {
        currentItem = item;

        if (cardLabel != null) cardLabel.text = item.itemName;
        if (cardImage != null) cardImage.sprite = item.itemSprite;

        if (buyBtn != null)
        {
            buyBtn.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        // Play purchase sound effect if available
        if (audioSource != null && buySfx != null)
        {
            audioSource.PlayOneShot(buySfx);
        }

        // Trigger item purchase listeners
        if (currentItem != null)
        {
            currentItem.itemBought?.Invoke();
        }
    }
}