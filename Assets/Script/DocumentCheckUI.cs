using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The document check screen: customer ID, deceased ID, photo, death document, and the Approve / Reject buttons.
/// Uses YOUR UI: drag the objects into the slots. Every slot is optional.
/// Put this script on an object that is ALWAYS ACTIVE (e.g. your HUD or iGUI), NOT on the panel itself.
/// </summary>
public class DocumentCheckUI : MonoBehaviour
{
    public static DocumentCheckUI Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.open;

    [Header("Panel (hidden until needed)")]
    public GameObject panel;

    [Header("Customer ID card")]
    public TMP_Text customerIdName;

    [Header("Deceased ID card")]
    public TMP_Text deceasedIdName;
    public TMP_Text deceasedIdBirthDate;

    [Header("Photo of them together")]
    public Image photoImage;

    [Header("Death document")]
    public TMP_Text deathDocName;
    public TMP_Text deathDocDate;
    public TMP_Text deathPlace;

    [Header("Buttons (check mark / X)")]
    public Button approveButton;
    public Button rejectButton;

    [Header("Keyboard shortcuts")]
    public KeyCode approveKey = KeyCode.Y;
    public KeyCode rejectKey = KeyCode.N;
    [Tooltip("Close the screen without deciding, so you can look at your ticket / notebook first.")]
    public KeyCode closeKey = KeyCode.Escape;

    [Header("While open")]
    [Tooltip("Drag your player movement / mouse-look scripts here. They are switched off while this screen is open.")]
    public Behaviour[] disableWhileOpen;
    public bool showCursor = true;

    private bool open;
    private Action onApprove;
    private Action onReject;
    private Func<bool> isStillValid;

    private bool[] wasEnabled;
    private CursorLockMode previousLockState;
    private bool previousCursorVisible;

    private void Awake()
    {
        Instance = this;

        if (panel != null) panel.SetActive(false);
        if (approveButton != null) approveButton.onClick.AddListener(() => Decide(true));
        if (rejectButton != null) rejectButton.onClick.AddListener(() => Decide(false));
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

        if (Input.GetKeyDown(approveKey)) Decide(true);
        else if (Input.GetKeyDown(rejectKey)) Decide(false);
        else if (Input.GetKeyDown(closeKey)) Close();
    }

    /// <summary>Fills the cards from the customer's data and opens the screen.</summary>
    public void Show(NPCData data, Action approve, Action reject, Func<bool> isValid = null)
    {
        if (open || data == null) return;

        onApprove = approve;
        onReject = reject;
        isStillValid = isValid;
        open = true;

        Fill(data.GetDocuments());
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

    private void Fill(DocumentSet d)
    {
        SetText(customerIdName, d.customerIdName);

        SetText(deceasedIdName, d.deceasedIdName);
        SetText(deceasedIdBirthDate, d.deceasedIdBirthDate);

        SetText(deathDocName, d.deathDocName);
        SetText(deathDocDate, d.deathDocDate);
        SetText(deathPlace, d.deathPlace);

        // If no photo is set for this customer, the picture you placed in the UI stays as it is
        if (photoImage != null && d.togetherPhoto != null)
            photoImage.sprite = d.togetherPhoto;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null) text.text = value;
    }

    private void Decide(bool approve)
    {
        if (!open) return;

        Action callback = approve ? onApprove : onReject;
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

        onApprove = null;
        onReject = null;
        isStillValid = null;
    }
}