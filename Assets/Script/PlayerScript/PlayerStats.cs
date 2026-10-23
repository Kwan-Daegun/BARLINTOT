using TMPro;
using UnityEngine;

/// <summary>
/// The player's money. (HP and family happiness get added here in the next part.)
/// Add this to the same object as your GameManager.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Money")]
    public int money = 0;
    [Tooltip("Shown in front of amounts, e.g. P500.")]
    public string currencyPrefix = "P";

    [Header("Your UI (optional)")]
    [Tooltip("A TextMeshPro text that always shows your money.")]
    public TMP_Text moneyText;
    [Tooltip("{0} = currency prefix, {1} = amount")]
    public string moneyFormat = "{0}{1}";

    public event System.Action<int> OnMoneyChanged;

    private void Awake()
    {
        Instance = this;
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void AddMoney(int amount)
    {
        money += amount;
        Changed();
    }

    /// <summary>Takes the money if you have enough (for buying materials later). Returns false if you can't afford it.</summary>
    public bool TrySpend(int amount)
    {
        if (money < amount) return false;
        money -= amount;
        Changed();
        return true;
    }

    /// <summary>"P500" style text, works even if there's no PlayerStats in the scene.</summary>
    public static string FormatMoney(int amount)
    {
        string prefix = Instance != null ? Instance.currencyPrefix : "P";
        return prefix + amount;
    }

    private void Changed()
    {
        RefreshUI();
        if (OnMoneyChanged != null) OnMoneyChanged.Invoke(money);
    }

    private void RefreshUI()
    {
        if (moneyText != null) moneyText.text = string.Format(moneyFormat, currencyPrefix, money);
    }
}