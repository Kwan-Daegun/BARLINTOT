/*using System;
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
}*/

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The document check screen. Click a document to ENLARGE it, then press Yes (it's right) or No (it's wrong).
/// When all four documents are marked, the check is finished.
/// Uses YOUR UI: drag the objects into the slots.
/// Put this script on an object that is ALWAYS ACTIVE (e.g. your HUD or iGUI), NOT on the panel itself.
/// </summary>
public class DocumentCheckUI : MonoBehaviour
{
    public static DocumentCheckUI Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.open;

    // Order of the marks that are sent back: customer ID, deceased ID, photo, death document
    private const int CardCount = 4;

    [Header("Panel (hidden until needed)")]
    [Tooltip("The root of the whole document screen, e.g. 'Document'.")]
    public GameObject panel;
    [Tooltip("Optional. Where a clicked document moves to when enlarged. If empty, the middle of the panel is used.")]
    public RectTransform zoomCenter;
    public float zoomScale = 1.6f;
    public float zoomTime = 0.15f;

    [Header("Document cards (the whole card object, clicking it enlarges it)")]
    public RectTransform customerIdCard;
    public RectTransform deceasedIdCard;
    public RectTransform photoCard;
    public RectTransform deathDocCard;

    [Header("Texts on the cards")]
    public TMP_Text customerIdName;
    public TMP_Text deceasedIdName;
    public TMP_Text deceasedIdBirthDate;
    public Image photoImage;
    public TMP_Text deathDocName;
    public TMP_Text deathDocDate;
    public TMP_Text deathPlace;

    [Header("Yes / No (shown for the enlarged document)")]
    public Button yesButton;
    public Button noButton;
    public Color rightTint = new Color(0.6f, 1f, 0.6f, 1f);
    public Color wrongTint = new Color(1f, 0.6f, 0.6f, 1f);

    [Header("Keyboard shortcuts")]
    public KeyCode yesKey = KeyCode.Y;
    public KeyCode noKey = KeyCode.N;
    [Tooltip("Shrinks the enlarged document. Pressing it again closes the screen without deciding.")]
    public KeyCode closeKey = KeyCode.Escape;

    [Header("While open")]
    [Tooltip("Drag your player movement / mouse-look scripts here. They are switched off while this screen is open.")]
    public Behaviour[] disableWhileOpen;
    public bool showCursor = true;

    private bool open;
    private Action<bool[]> onFinished;
    private Func<bool> isStillValid;

    private RectTransform[] cards;
    private Image[] cardImages;
    private Color[] baseColors;
    private Vector3[] basePositions;
    private Vector3[] baseScales;
    private int[] baseSiblings;
    private Coroutine[] moveRoutines;
    private int[] marks;     // -1 = not marked yet, 1 = Yes (right), 0 = No (wrong)
    private int zoomed = -1; // index of the enlarged card, -1 = none

    private bool[] wasEnabled;
    private CursorLockMode previousLockState;
    private bool previousCursorVisible;

    // Tiny helper so a card can be clicked without you adding a Button to it
    private class CardClick : MonoBehaviour, IPointerClickHandler
    {
        public Action onClick;
        public void OnPointerClick(PointerEventData eventData)
        {
            if (onClick != null) onClick.Invoke();
        }
    }

    private void Awake()
    {
        Instance = this;

        cards = new[] { customerIdCard, deceasedIdCard, photoCard, deathDocCard };
        cardImages = new Image[CardCount];
        baseColors = new Color[CardCount];
        basePositions = new Vector3[CardCount];
        baseScales = new Vector3[CardCount];
        baseSiblings = new int[CardCount];
        moveRoutines = new Coroutine[CardCount];
        marks = new int[CardCount];

        for (int i = 0; i < CardCount; i++)
        {
            if (cards[i] == null) continue;

            cardImages[i] = cards[i].GetComponent<Image>();
            if (cardImages[i] != null) baseColors[i] = cardImages[i].color;

            basePositions[i] = cards[i].position;
            baseScales[i] = cards[i].localScale;
            baseSiblings[i] = cards[i].GetSiblingIndex();

            int index = i;
            CardClick relay = cards[i].GetComponent<CardClick>();
            if (relay == null) relay = cards[i].gameObject.AddComponent<CardClick>();
            relay.onClick = () => OnCardClicked(index);
        }

        if (yesButton != null) yesButton.onClick.AddListener(() => Mark(true));
        if (noButton != null) noButton.onClick.AddListener(() => Mark(false));

        SetYesNoVisible(false);
        if (panel != null) panel.SetActive(false);
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

        if (zoomed >= 0)
        {
            if (Input.GetKeyDown(yesKey)) Mark(true);
            else if (Input.GetKeyDown(noKey)) Mark(false);
        }

        if (Input.GetKeyDown(closeKey))
        {
            if (zoomed >= 0) ZoomOut();   // first press: shrink the document
            else Close();                 // second press: leave the screen
        }
    }

    // ---------------------------------------------------------------- Open / close

    /// <summary>
    /// Opens the screen. onFinished gets four values (customer ID, deceased ID, photo, death document):
    /// true = the player marked it Yes (right), false = No (wrong).
    /// </summary>
    public void Show(NPCData data, Action<bool[]> finished, Func<bool> isValid = null)
    {
        if (open || data == null) return;

        onFinished = finished;
        isStillValid = isValid;
        open = true;
        zoomed = -1;

        Fill(data.GetDocuments());

        for (int i = 0; i < CardCount; i++)
        {
            marks[i] = cards[i] == null ? 1 : -1; // a card you didn't set up counts as "right"
            RestoreCard(i);
        }

        SetYesNoVisible(false);
        if (panel != null) panel.SetActive(true);

        // Freeze player controls
        wasEnabled = new bool[disableWhileOpen.Length];
        for (int i = 0; i < disableWhileOpen.Length; i++)
        {
            if (disableWhileOpen[i] == null) continue;
            wasEnabled[i] = disableWhileOpen[i].enabled;
            disableWhileOpen[i].enabled = false;
        }

        // Free the mouse so the cards and buttons can be clicked
        if (showCursor)
        {
            previousLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Close()
    {
        if (!open) return;
        open = false;

        for (int i = 0; i < CardCount; i++) RestoreCard(i);
        zoomed = -1;
        SetYesNoVisible(false);

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

        onFinished = null;
        isStillValid = null;
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

    // ---------------------------------------------------------------- Cards

    private void OnCardClicked(int index)
    {
        if (!open || zoomed >= 0 || cards[index] == null) return;

        zoomed = index;

        // Bring the card (and the Yes / No buttons) to the front
        cards[index].SetAsLastSibling();
        if (yesButton != null) yesButton.transform.SetAsLastSibling();
        if (noButton != null) noButton.transform.SetAsLastSibling();
        SetYesNoVisible(true);

        MoveCard(index, GetZoomPosition(), baseScales[index] * zoomScale, null);
    }

    private void Mark(bool right)
    {
        if (!open || zoomed < 0) return;

        int index = zoomed;
        marks[index] = right ? 1 : 0;

        if (cardImages[index] != null)
        {
            Color tint = right ? rightTint : wrongTint;
            cardImages[index].color = new Color(tint.r, tint.g, tint.b, baseColors[index].a);
        }

        ZoomOut();

        // All documents marked: the check is done
        for (int i = 0; i < CardCount; i++)
        {
            if (marks[i] < 0) return;
        }
        Finish();
    }

    private void ZoomOut()
    {
        if (zoomed < 0) return;

        int index = zoomed;
        zoomed = -1;
        SetYesNoVisible(false);

        MoveCard(index, basePositions[index], baseScales[index],
            () => { if (cards[index] != null) cards[index].SetSiblingIndex(baseSiblings[index]); });
    }

    private void Finish()
    {
        bool[] result = new bool[CardCount];
        for (int i = 0; i < CardCount; i++) result[i] = marks[i] == 1;

        Action<bool[]> callback = onFinished;
        Close();
        if (callback != null) callback.Invoke(result);
    }

    private Vector3 GetZoomPosition()
    {
        if (zoomCenter != null) return zoomCenter.position;

        RectTransform area = panel != null ? panel.GetComponent<RectTransform>() : null;
        if (area != null) return area.TransformPoint(area.rect.center);

        return transform.position;
    }

    private void RestoreCard(int index)
    {
        if (cards[index] == null) return;

        if (moveRoutines[index] != null)
        {
            StopCoroutine(moveRoutines[index]);
            moveRoutines[index] = null;
        }

        cards[index].position = basePositions[index];
        cards[index].localScale = baseScales[index];
        cards[index].SetSiblingIndex(baseSiblings[index]);
        if (cardImages[index] != null) cardImages[index].color = baseColors[index];
    }

    private void SetYesNoVisible(bool visible)
    {
        if (yesButton != null) yesButton.gameObject.SetActive(visible);
        if (noButton != null) noButton.gameObject.SetActive(visible);
    }

    private void MoveCard(int index, Vector3 targetPosition, Vector3 targetScale, Action done)
    {
        if (moveRoutines[index] != null) StopCoroutine(moveRoutines[index]);
        moveRoutines[index] = StartCoroutine(MoveRoutine(index, targetPosition, targetScale, done));
    }

    private IEnumerator MoveRoutine(int index, Vector3 targetPosition, Vector3 targetScale, Action done)
    {
        RectTransform card = cards[index];
        Vector3 startPosition = card.position;
        Vector3 startScale = card.localScale;

        float duration = Mathf.Max(0.01f, zoomTime);
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / duration);
            card.position = Vector3.Lerp(startPosition, targetPosition, t);
            card.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        card.position = targetPosition;
        card.localScale = targetScale;
        moveRoutines[index] = null;

        if (done != null) done.Invoke();
    }
}