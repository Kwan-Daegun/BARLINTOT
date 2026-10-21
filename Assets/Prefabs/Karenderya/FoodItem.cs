using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FoodItem : MonoBehaviour
{
    public TextMeshProUGUI foodLabel;
    public Image foodImage;
    public Button buyBtn;

    [Space]
    [SerializeField] private AudioSource audioSource;
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

[CreateAssetMenu(fileName = "FoodItem", menuName = "ScriptableObjects/Food")]
public class Food : ScriptableObject
{
    public string foodName;
    public Sprite foodSprite;
}