using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The player's money, HP and the family's happiness.
/// Add this to the same object as your GameManager.
/// You lose when HP or family happiness drops to the "Lose At Percent" (25% by default).
/// </summary>
public class PlayerStats : MonoBehaviour
{
    public enum LoseReason { Hp, Family }

    public static PlayerStats Instance { get; private set; }

    [Header("Money")]
    public int money = 0;
    [Tooltip("Shown in front of amounts, e.g. P500.")]
    public string currencyPrefix = "P";

    [Header("HP")]
    public float maxHp = 100f;
    public float hp = 100f;
    [Tooltip("Seconds after a meal before hunger starts to hurt.")]
    public float hungerGraceSeconds = 90f;
    [Tooltip("HP lost every second once you are hungry. 0 = hunger never hurts.")]
    public float hungerDrainPerSecond = 0.4f;
    [Tooltip("HP lost for every mistake when checking documents (the ghost drains you). 0 = off.")]
    public float hpLossPerMistake = 5f;

    [Header("Family happiness")]
    public float maxHappiness = 100f;
    public float familyHappiness = 70f;
    [Tooltip("Money the family needs from you each day.")]
    public int familyDailyNeed = 300;
    [Tooltip("Every this much money above (or below) the daily need changes happiness by 1.")]
    public int moneyPerHappinessPoint = 20;
    [Tooltip("The most happiness can change from one day's money.")]
    public float maxHappinessChangePerDay = 30f;

    [Header("Lose")]
    [Range(0f, 100f)]
    [Tooltip("You lose when HP or happiness falls to this percent.")]
    public float loseAtPercent = 25f;

    [Header("Your UI (all optional)")]
    [Tooltip("A TextMeshPro text that always shows your money.")]
    public TMP_Text moneyText;
    [Tooltip("{0} = currency prefix, {1} = amount")]
    public string moneyFormat = "{0}{1}";

    [Tooltip("An Image set to Image Type = Filled. Its fill shows your HP.")]
    public Image hpFill;
    public Slider hpSlider;
    public TMP_Text hpText;

    [Tooltip("An Image set to Image Type = Filled. Its fill shows the family's happiness.")]
    public Image happinessFill;
    public Slider happinessSlider;
    public TMP_Text happinessText;

    public event System.Action<int> OnMoneyChanged;
    public event System.Action<float> OnHpChanged;
    public event System.Action<float> OnHappinessChanged;
    public event System.Action<LoseReason> OnLose;

    /// <summary>Fires after documents were checked with mistakes (number of mistakes). For ghost scares later.</summary>
    public event System.Action<int> OnMistakeMade;

    public float Hp01 => maxHp > 0f ? Mathf.Clamp01(hp / maxHp) : 0f;
    public float Happiness01 => maxHappiness > 0f ? Mathf.Clamp01(familyHappiness / maxHappiness) : 0f;
    public bool IsHungry => timeSinceMeal >= hungerGraceSeconds;
    public bool HasLost { get; private set; }

    private float timeSinceMeal;

    private void Awake()
    {
        Instance = this;
        hp = Mathf.Clamp(hp, 0f, maxHp);
        familyHappiness = Mathf.Clamp(familyHappiness, 0f, maxHappiness);
        RefreshUI();
    }

    private void OnEnable()
    {
        NPCInteractable.OnVerdict += HandleVerdict;
    }

    private void OnDisable()
    {
        NPCInteractable.OnVerdict -= HandleVerdict;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (HasLost || GameEndManager.GameIsOver) return;

        // Nothing happens during the wake-up intro
        if (TaskListManager.Instance != null && !TaskListManager.Instance.Started) return;

        // No meal for a while: hunger slowly drains HP
        timeSinceMeal += Time.deltaTime;
        if (timeSinceMeal >= hungerGraceSeconds && hungerDrainPerSecond > 0f)
            ChangeHp(-hungerDrainPerSecond * Time.deltaTime);
    }

    // ---------------------------------------------------------------- Money

    public void AddMoney(int amount)
    {
        money += amount;
        MoneyChanged();
    }

    /// <summary>Takes the money if you have enough (for food / materials). Returns false if you can't afford it.</summary>
    public bool TrySpend(int amount)
    {
        if (money < amount) return false;
        money -= amount;
        MoneyChanged();
        return true;
    }

    /// <summary>"P500" style text, works even if there's no PlayerStats in the scene.</summary>
    public static string FormatMoney(int amount)
    {
        string prefix = Instance != null ? Instance.currencyPrefix : "P";
        return prefix + amount;
    }

    // ---------------------------------------------------------------- HP

    public void ChangeHp(float amount)
    {
        if (HasLost) return;

        hp = Mathf.Clamp(hp + amount, 0f, maxHp);
        RefreshUI();
        if (OnHpChanged != null) OnHpChanged.Invoke(hp);
        CheckLose();
    }

    /// <summary>Eating: restores HP and resets the hunger timer.</summary>
    public void Eat(float hpGain)
    {
        timeSinceMeal = 0f;
        ChangeHp(hpGain);
    }

    // ---------------------------------------------------------------- Family

    public void ChangeHappiness(float amount)
    {
        if (HasLost) return;

        familyHappiness = Mathf.Clamp(familyHappiness + amount, 0f, maxHappiness);
        RefreshUI();
        if (OnHappinessChanged != null) OnHappinessChanged.Invoke(familyHappiness);
        CheckLose();
    }

    /// <summary>
    /// End of the day: you hand ALL your money to the family. Giving more than they need makes them happier,
    /// giving less makes them sadder. Returns how much the happiness changed.
    /// </summary>
    public float GiveMoneyToFamily()
    {
        int given = money;
        money = 0;
        MoneyChanged();

        float change = (given - familyDailyNeed) / (float)Mathf.Max(1, moneyPerHappinessPoint);
        change = Mathf.Clamp(change, -maxHappinessChangePerDay, maxHappinessChangePerDay);

        ChangeHappiness(change);
        return change;
    }

    // ---------------------------------------------------------------- Lose

    private void CheckLose()
    {
        if (HasLost) return;

        if (Hp01 * 100f <= loseAtPercent) Lose(LoseReason.Hp);
        else if (Happiness01 * 100f <= loseAtPercent) Lose(LoseReason.Family);
    }

    private void Lose(LoseReason reason)
    {
        HasLost = true;
        if (OnLose != null) OnLose.Invoke(reason);
    }

    // ---------------------------------------------------------------- Documents

    private void HandleVerdict(NPCData data, int mistakes)
    {
        if (mistakes <= 0) return;

        if (hpLossPerMistake > 0f) ChangeHp(-hpLossPerMistake * mistakes);
        if (OnMistakeMade != null) OnMistakeMade.Invoke(mistakes);
    }

    // ---------------------------------------------------------------- UI

    private void MoneyChanged()
    {
        RefreshUI();
        if (OnMoneyChanged != null) OnMoneyChanged.Invoke(money);
    }

    private void RefreshUI()
    {
        if (moneyText != null) moneyText.text = string.Format(moneyFormat, currencyPrefix, money);

        SetBar(hpFill, hpSlider, hpText, Hp01);
        SetBar(happinessFill, happinessSlider, happinessText, Happiness01);
    }

    private static void SetBar(Image fill, Slider slider, TMP_Text label, float value01)
    {
        if (fill != null) fill.fillAmount = value01;

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value01;
        }

        if (label != null) label.text = Mathf.RoundToInt(value01 * 100f) + "%";
    }

    // ---------------------------------------------------------------- Testing (right-click the component title while playing)

    [ContextMenu("Test: lower HP by 20")]
    private void TestLowerHp() { ChangeHp(-20f); }

    [ContextMenu("Test: lower family happiness by 20")]
    private void TestLowerHappiness() { ChangeHappiness(-20f); }

    [ContextMenu("Test: make me hungry now")]
    private void TestHungry() { timeSinceMeal = hungerGraceSeconds; }
}