using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Shows YOUR lose screen (e.g. DeathScreen) when HP or family happiness hits the limit,
/// and an optional win screen when the customer's order is finished.
/// Add this to an object that is ALWAYS ACTIVE (e.g. GameManager or iGUI).
/// </summary>
public class GameEndManager : MonoBehaviour
{
    public static bool GameIsOver { get; private set; }

    [Header("Lose screen (your DeathScreen)")]
    public GameObject loseScreen;
    [Tooltip("Optional. A TextMeshPro text on the lose screen that says why you lost.")]
    public TMP_Text loseReasonText;
    public string hpReason = "You collapsed from hunger and exhaustion.";
    public string familyReason = "Your family lost hope in you.";

    [Header("Win screen (optional)")]
    public GameObject winScreen;
    [Tooltip("How many finished orders it takes to win.")]
    public int ordersToWin = 1;

    [Header("Restart (optional)")]
    [Tooltip("Buttons on your screens that restart the game. Leave empty if your screens already have their own.")]
    public Button[] restartButtons;

    [Header("When the game ends")]
    [Tooltip("Drag your player movement / mouse-look scripts here. They are switched off at the end.")]
    public Behaviour[] disableOnEnd;
    public bool freezeTime = true;
    public bool showCursor = true;

    private int deliveredOrders;

    private void Awake()
    {
        GameIsOver = false;
        Time.timeScale = 1f;

        if (loseScreen != null) loseScreen.SetActive(false);
        if (winScreen != null) winScreen.SetActive(false);

        foreach (Button button in restartButtons)
        {
            if (button != null) button.onClick.AddListener(Restart);
        }
    }

    private void OnEnable()
    {
        GameManager.OnOrderDelivered += HandleOrderDelivered;
    }

    private void OnDisable()
    {
        GameManager.OnOrderDelivered -= HandleOrderDelivered;
    }

    //private void Start()
    //{
    //    if (PlayerStats.Instance != null) PlayerStats.Instance.OnLose += HandleLose;
    //    else Debug.LogWarning("GameEndManager: no PlayerStats found. Add PlayerStats to your GameManager object.");
    //}

    //private void OnDestroy()
    //{
    //    if (PlayerStats.Instance != null) PlayerStats.Instance.OnLose -= HandleLose;
    //    Time.timeScale = 1f;
    //}

    //private void HandleLose(PlayerStats.LoseReason reason)
    //{
    //    if (loseReasonText != null)
    //        loseReasonText.text = reason == PlayerStats.LoseReason.Hp ? hpReason : familyReason;

    //    End(loseScreen);
    //}

    private void HandleOrderDelivered(NPCData data)
    {
        deliveredOrders++;
        if (deliveredOrders >= ordersToWin) End(winScreen);
    }

    private void End(GameObject screen)
    {
        if (GameIsOver) return;
        GameIsOver = true;

        if (screen != null) screen.SetActive(true);

        foreach (Behaviour behaviour in disableOnEnd)
        {
            if (behaviour != null) behaviour.enabled = false;
        }

        if (showCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (freezeTime) Time.timeScale = 0f;
    }

    /// <summary>Reloads the scene. (The scene must be listed in File > Build Profiles.)</summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        GameIsOver = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}