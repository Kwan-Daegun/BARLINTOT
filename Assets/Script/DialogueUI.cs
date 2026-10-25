using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows what the customers say: a speaker name and one line at a time, typed out letter by letter.
/// Press E / Space / click to finish a line, and again for the next line.
/// Uses YOUR UI: drag the objects into the slots (only Panel and Line Text are needed).
/// Put this script on an object that is ALWAYS ACTIVE (e.g. your HUD or iGUI), NOT on the panel itself.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.open;

    [Header("Your UI (drag from the Hierarchy)")]
    [Tooltip("The whole dialogue box, e.g. 'Dialogue'. It is hidden until someone speaks.")]
    public GameObject panel;
    [Tooltip("Optional. Shows who is talking.")]
    public TMP_Text speakerText;
    public TMP_Text lineText;
    [Tooltip("Optional. A small arrow / 'press E' icon shown when the line is fully typed.")]
    public GameObject continueIndicator;

    [Header("Typing")]
    [Tooltip("Letters per second. 0 = show the whole line at once.")]
    public float charactersPerSecond = 45f;

    [Header("Advance")]
    public KeyCode advanceKey = KeyCode.E;
    public KeyCode advanceKey2 = KeyCode.Space;
    public bool mouseClickAdvances = true;

    [Header("While open")]
    [Tooltip("Drag your player movement / mouse-look scripts here. They are switched off while someone is talking.")]
    public Behaviour[] disableWhileOpen;

    private bool open;
    private string[] lines;
    private int lineIndex;
    private Action onFinished;
    private Func<bool> isStillValid;

    private float visibleChars;
    private int totalChars;
    private bool typing;
    private int openedFrame = -1;
    private bool[] wasEnabled;

    private void Awake()
    {
        Instance = this;

        if (panel != null) panel.SetActive(false);
        if (continueIndicator != null) continueIndicator.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Plays the lines one after another. finished is called after the last line.
    /// If there is no DialogueUI in the scene, finished is called right away so the game never gets stuck.
    /// </summary>
    public static void Say(string speaker, string[] lines, Action finished = null, Func<bool> isValid = null)
    {
        if (Instance == null)
        {
            if (finished != null) finished.Invoke();
            return;
        }

        Instance.Play(speaker, lines, finished, isValid);
    }

    public void Play(string speaker, string[] newLines, Action finished = null, Func<bool> isValid = null)
    {
        if (newLines == null || newLines.Length == 0)
        {
            if (finished != null) finished.Invoke();
            return;
        }

        if (open) Close(false); // replace whatever was being said

        lines = newLines;
        lineIndex = 0;
        onFinished = finished;
        isStillValid = isValid;
        open = true;
        openedFrame = Time.frameCount; // ignore the key press that opened the dialogue

        if (speakerText != null) speakerText.text = speaker;
        if (panel != null) panel.SetActive(true);

        // Freeze player controls
        wasEnabled = new bool[disableWhileOpen.Length];
        for (int i = 0; i < disableWhileOpen.Length; i++)
        {
            if (disableWhileOpen[i] == null) continue;
            wasEnabled[i] = disableWhileOpen[i].enabled;
            disableWhileOpen[i].enabled = false;
        }

        ShowLine(0);
    }

    private void Update()
    {
        if (!open) return;

        // Stop by itself if the speaker left in the meantime
        if (isStillValid != null && !isStillValid())
        {
            Close(false);
            return;
        }

        // Typewriter effect
        if (typing)
        {
            visibleChars += charactersPerSecond * Time.unscaledDeltaTime;
            if (lineText != null) lineText.maxVisibleCharacters = Mathf.FloorToInt(visibleChars);
            if (visibleChars >= totalChars) FinishTyping();
        }

        if (Time.frameCount == openedFrame) return;

        bool pressed = Input.GetKeyDown(advanceKey)
                       || Input.GetKeyDown(advanceKey2)
                       || (mouseClickAdvances && Input.GetMouseButtonDown(0));
        if (!pressed) return;

        if (typing) FinishTyping();   // first press: show the whole line
        else NextLine();              // next press: go on
    }

    private void ShowLine(int index)
    {
        string line = lines[index] ?? string.Empty;

        visibleChars = 0f;
        totalChars = line.Length;
        typing = true;
        if (continueIndicator != null) continueIndicator.SetActive(false);

        if (lineText != null)
        {
            lineText.text = line;
            lineText.maxVisibleCharacters = 0;
            lineText.ForceMeshUpdate();
            totalChars = lineText.textInfo.characterCount;
        }

        if (charactersPerSecond <= 0f || lineText == null || totalChars == 0) FinishTyping();
    }

    private void FinishTyping()
    {
        typing = false;
        if (lineText != null) lineText.maxVisibleCharacters = 99999;
        if (continueIndicator != null) continueIndicator.SetActive(true);
    }

    private void NextLine()
    {
        lineIndex++;
        if (lineIndex >= lines.Length) Close(true);
        else ShowLine(lineIndex);
    }

    private void Close(bool callFinished)
    {
        if (!open) return;
        open = false;
        typing = false;

        if (panel != null) panel.SetActive(false);
        if (continueIndicator != null) continueIndicator.SetActive(false);

        // Give the player their controls back (only the ones that were on)
        for (int i = 0; i < disableWhileOpen.Length; i++)
        {
            if (disableWhileOpen[i] != null && wasEnabled != null && wasEnabled[i])
                disableWhileOpen[i].enabled = true;
        }

        Action callback = onFinished;
        onFinished = null;
        isStillValid = null;
        lines = null;

        if (callFinished && callback != null) callback.Invoke();
    }
}