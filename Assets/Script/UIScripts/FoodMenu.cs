using UnityEngine;
using UnityEngine.UI;

public class FoodMenu : MonoBehaviour
{
    [SerializeField] private NPCKarenderya karenderyaNPC;

    [Header("UI")]
    [SerializeField] private Button exitBtn;

    private void Start()
    {
        ShowMenu(false);

        if (karenderyaNPC == null) return;
        karenderyaNPC.ShowMenu += OpenMenu;

        if (exitBtn != null) exitBtn.onClick.AddListener(() => { ShowMenu(false); });
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
