using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The "Take this order?  Yes / No" popup. Uses YOUR UI: drag the objects into the slots.
/// Put this script on an object that is ALWAYS ACTIVE (for example your HUD or iGUI),
/// NOT on the popup panel itself, because the panel is hidden until it's needed.
/// </summary>
public class OrderChoiceUI : MonoBehaviour
{
    public static OrderChoiceUI Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.open;

    [Header("Your UI (drag from the Hierarchy)")]
    [Tooltip("The popup object that holds the question and the two buttons. It is hidden until needed.")]
    public GameObject panel;
    public Button yesButton;
    public Button noButton;
    [Tooltip("Optional. Text for the question. Leave empty to keep whatever you typed in the UI.")]
    public TMP_Text questionText;
    public string question = "Take this order?";

    [Header("Keyboard shortcuts")]
    public KeyCode yesKey = KeyCode.Y;
    public KeyCode noKey = KeyCode.N;

    [Header("While the popup is open")]
    [Tooltip("Drag your player movement / mouse-look scripts here. They are switched off while the popup is open.")]
    public Behaviour[] disableWhileOpen;
    public bool showCursor = true;

    private bool open;
    private Action onYes;
    private Action onNo;
    private Func<bool> isStillValid;

    private bool[] wasEnabled;
    private CursorLockMode previousLockState;
    private bool previousCursorVisible;

    private void Awake()
    {
        Instance = this;

        if (panel != null) panel.SetActive(false);
        if (yesButton != null) yesButton.onClick.AddListener(() => Choose(true));
        if (noButton != null) noButton.onClick.AddListener(() => Choose(false));
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!open) return;

        // Close by itself if the customer left in the meantime
        if (isStillValid != null && !isStillValid())
        {
            Close();
            return;
        }

        if (Input.GetKeyDown(yesKey)) Choose(true);
        else if (Input.GetKeyDown(noKey)) Choose(false);
    }

    /// <summary>Shows the popup. isValid (optional) lets the popup close itself when the choice no longer applies.</summary>
    public void Show(Action yes, Action no, Func<bool> isValid = null)
    {
        if (open) return;

        onYes = yes;
        onNo = no;
        isStillValid = isValid;
        open = true;

        if (questionText != null && !string.IsNullOrEmpty(question)) questionText.text = question;
        if (panel != null) panel.SetActive(true);

        // Freeze player controls
        wasEnabled = new bool[disableWhileOpen.Length];
        for (int i = 0; i < disableWhileOpen.Length; i++)
        {
            if (disableWhileOpen[i] == null) continue;
            wasEnabled[i] = disableWhileOpen[i].enabled;
            disableWhileOpen[i].enabled = false;
        }

        // Free the mouse so the buttons can be clicked
        if (showCursor)
        {
            previousLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Choose(bool yes)
    {
        if (!open) return;

        Action callback = yes ? onYes : onNo;
        Close();
        if (callback != null) callback.Invoke();
    }

    private void Close()
    {
        if (!open) return;
        open = false;

        if (panel != null) panel.SetActive(false);

        // Give the player their controls back (only the ones that were on)
        for (int i = 0; i < disableWhileOpen.Length; i++)
        {
            if (disableWhileOpen[i] != null && wasEnabled != null && wasEnabled[i])
                disableWhileOpen[i].enabled = true;
        }

        if (showCursor)
        {
            Cursor.lockState = previousLockState;
            Cursor.visible = previousCursorVisible;
        }

        onYes = null;
        onNo = null;
        isStillValid = null;
    }
}