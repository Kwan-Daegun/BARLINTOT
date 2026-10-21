using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Is used for shop item cards in shop menus
/// </summary>
public class ItemCard : MonoBehaviour
{
    public TextMeshProUGUI cardLabel;
    public Image cardImage;
    public Button buyBtn;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip buySfx;

    private void Start()
    {
        if (buyBtn)
        {
            buyBtn.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (audioSource == null || buySfx == null) return;

        audioSource.clip = buySfx;
        audioSource.Play();
    }
}
