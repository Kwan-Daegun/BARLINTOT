using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Short on-screen messages like "Door locked." or "You need the House Key."
/// Put this on an ACTIVE object in your HUD (e.g. on the text object itself)
/// and drag a TextMeshPro text into the Text slot. Only one is needed in the scene.
/// Anywhere in code:  HintMessage.Show("Hello");
/// </summary>
public class HintMessage : MonoBehaviour
{
    public static HintMessage Instance { get; private set; }

    [Tooltip("Your TextMeshPro text for the message.")]
    public TMP_Text text;
    public float showTime = 2f;
    public float fadeTime = 0.4f;

    private Coroutine routine;

    private void Awake()
    {
        Instance = this;
        if (text != null) text.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public static void Show(string message)
    {
        if (Instance != null) Instance.Display(message);
        else Debug.Log("[Hint] " + message);
    }

    private void Display(string message)
    {
        if (text == null) return;

        text.text = message;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        text.alpha = 1f;
        yield return new WaitForSeconds(showTime);

        float duration = Mathf.Max(0.01f, fadeTime);
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            text.alpha = Mathf.Lerp(1f, 0f, time / duration);
            yield return null;
        }

        text.alpha = 0f;
        routine = null;
    }
}