/*using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to your Player. On game start it shows "Day 1" on a black screen,
/// then the eyes open slowly with a couple of blinks, like waking up.
/// It builds all of its own UI at runtime, so there is nothing else to set up.
/// </summary>
public class WakeUpIntro : MonoBehaviour
{
    [Header("Day Title")]
    public bool showDayTitle = true;
    public int dayNumber = 1;
    public int fontSize = 72;
    public float titleFadeIn = 1f;
    public float titleHold = 1.5f;
    public float titleFadeOut = 0.8f;

    [Header("Timing")]
    public bool playOnStart = true;
    [Tooltip("Black screen time before anything happens.")]
    public float startDelay = 0.5f;
    [Tooltip("1 = normal, 2 = twice as fast, 0.5 = half speed.")]
    public float speed = 1f;

    [Header("Player Control")]
    [Tooltip("Drag your movement / mouse-look scripts here. They are disabled during the intro.")]
    public Behaviour[] disableDuringIntro;

    /// <summary>Fired when the eyes are fully open and the player has control again.</summary>
    public event System.Action OnFinished;

    private Canvas canvas;
    private RectTransform topLid;
    private RectTransform bottomLid;
    private Image dim;
    private Text titleText;
    private float open; // 0 = eyes closed, 1 = fully open

    private void Start()
    {
        if (playOnStart) PlayWakeUp();
    }

    public void PlayWakeUp()
    {
        StopAllCoroutines();
        StartCoroutine(Sequence());
    }

    // ---------------------------------------------------------------- Sequence

    private IEnumerator Sequence()
    {
        BuildUI();
        SetOpen(0f);
        SetControls(false);

        yield return Wait(startDelay);

        if (showDayTitle)
        {
            yield return FadeTitle(0f, 1f, titleFadeIn);
            yield return Wait(titleHold);
            yield return FadeTitle(1f, 0f, titleFadeOut);
            yield return Wait(0.5f);
        }

        // Eyes struggle open, blink, then open fully
        yield return MoveEyes(0.25f, 1.0f);
        yield return Wait(0.3f);
        yield return MoveEyes(0f, 0.5f);
        yield return Wait(0.4f);
        yield return MoveEyes(0.6f, 1.0f);
        yield return Wait(0.2f);
        yield return MoveEyes(0.1f, 0.4f);
        yield return MoveEyes(1f, 1.8f);

        Destroy(canvas.gameObject);
        SetControls(true);

        if (OnFinished != null) OnFinished.Invoke();
    }

    private IEnumerator Wait(float seconds)
    {
        yield return new WaitForSeconds(seconds / Mathf.Max(0.01f, speed));
    }

    private IEnumerator MoveEyes(float target, float duration)
    {
        float start = open;
        float time = 0f;
        duration /= Mathf.Max(0.01f, speed);

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / duration);
            SetOpen(Mathf.Lerp(start, target, t));
            yield return null;
        }
        SetOpen(target);
    }

    private IEnumerator FadeTitle(float from, float to, float duration)
    {
        float time = 0f;
        duration /= Mathf.Max(0.01f, speed);

        while (time < duration)
        {
            time += Time.deltaTime;
            SetTitleAlpha(Mathf.Lerp(from, to, time / duration));
            yield return null;
        }
        SetTitleAlpha(to);
    }

    // ---------------------------------------------------------------- Helpers

    private void SetControls(bool enabled)
    {
        foreach (Behaviour b in disableDuringIntro)
        {
            if (b != null) b.enabled = enabled;
        }
    }

    private void SetOpen(float value)
    {
        open = Mathf.Clamp01(value);

        // Eyelids meet in the middle when closed and slide apart when opening
        topLid.anchorMin = new Vector2(0f, Mathf.Lerp(0.49f, 1f, open));
        topLid.anchorMax = new Vector2(1f, 1f);
        topLid.offsetMin = Vector2.zero;
        topLid.offsetMax = Vector2.zero;

        bottomLid.anchorMin = new Vector2(0f, 0f);
        bottomLid.anchorMax = new Vector2(1f, Mathf.Lerp(0.51f, 0f, open));
        bottomLid.offsetMin = Vector2.zero;
        bottomLid.offsetMax = Vector2.zero;

        // Dark, dim vision that brightens as the eyes open
        Color c = dim.color;
        c.a = Mathf.Clamp01(1f - open * 1.5f);
        dim.color = c;
    }

    private void SetTitleAlpha(float a)
    {
        if (titleText == null) return;
        Color c = titleText.color;
        c.a = a;
        titleText.color = c;
    }

    // ---------------------------------------------------------------- UI building

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("WakeUpCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // draw on top of everything

        dim = CreateImage("Dim", canvasObj.transform, new Color(0f, 0f, 0f, 1f)).GetComponent<Image>();
        topLid = CreateImage("TopLid", canvasObj.transform, Color.black);
        bottomLid = CreateImage("BottomLid", canvasObj.transform, Color.black);

        if (showDayTitle) CreateTitle(canvasObj.transform);
    }

    private RectTransform CreateImage(string objName, Transform parent, Color color)
    {
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(parent, false);

        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    private void CreateTitle(Transform parent)
    {
        GameObject obj = new GameObject("DayTitle");
        obj.transform.SetParent(parent, false);

        titleText = obj.AddComponent<Text>();
        titleText.text = "Day " + dayNumber;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontSize = fontSize;
        titleText.color = new Color(1f, 1f, 1f, 0f);
        titleText.raycastTarget = false;

#if UNITY_2022_2_OR_NEWER
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}*/

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to your Player. On game start it shows "Day 1" on a black screen,
/// then the eyes open slowly with a couple of blinks, like waking up.
/// It builds all of its own UI at runtime, so there is nothing else to set up.
/// </summary>
public class WakeUpIntro : MonoBehaviour
{
    [Header("Day Title")]
    public bool showDayTitle = true;
    public int dayNumber = 1;
    public int fontSize = 72;
    public float titleFadeIn = 1f;
    public float titleHold = 1.5f;
    public float titleFadeOut = 0.8f;

    [Header("Timing")]
    public bool playOnStart = true;
    [Tooltip("Black screen time before anything happens.")]
    public float startDelay = 0.5f;
    [Tooltip("1 = normal, 2 = twice as fast, 0.5 = half speed.")]
    public float speed = 1f;

    [Header("Sleeping (between days)")]
    [Tooltip("How long the eyes take to close when you go to bed.")]
    public float sleepCloseTime = 1.2f;
    [Tooltip("How long the screen stays black while the night passes.")]
    public float sleepBlackTime = 1f;

    [Header("Player Control")]
    [Tooltip("Drag your movement / mouse-look scripts here. They are disabled during the intro.")]
    public Behaviour[] disableDuringIntro;

    /// <summary>Fired when the eyes are fully open and the player has control again.</summary>
    public event System.Action OnFinished;

    private Canvas canvas;
    private RectTransform topLid;
    private RectTransform bottomLid;
    private Image dim;
    private Text titleText;
    private float open; // 0 = eyes closed, 1 = fully open

    private void Start()
    {
        if (playOnStart) PlayWakeUp();
    }

    public void PlayWakeUp()
    {
        Restart();
        StartCoroutine(Sequence());
    }

    /// <summary>
    /// Going to bed: the eyes close, whileBlack runs while the screen is black (skip the night there),
    /// then "Day N" shows and the eyes open again. OnFinished fires at the end.
    /// </summary>
    public void PlaySleep(int newDayNumber, System.Action whileBlack)
    {
        Restart();
        dayNumber = newDayNumber;
        StartCoroutine(Sequence(true, whileBlack));
    }

    private void Restart()
    {
        StopAllCoroutines();
        if (canvas != null) Destroy(canvas.gameObject);
    }

    // ---------------------------------------------------------------- Sequence

    private IEnumerator Sequence(bool closeFirst = false, System.Action whileBlack = null)
    {
        BuildUI();
        SetControls(false);

        if (closeFirst)
        {
            // Going to sleep: start with open eyes and close them
            SetOpen(1f);
            yield return MoveEyes(0f, sleepCloseTime);
            yield return Wait(0.3f);
        }
        else
        {
            SetOpen(0f);
        }

        // The screen is black now: this is where the night gets skipped
        if (whileBlack != null)
        {
            whileBlack.Invoke();
            yield return Wait(sleepBlackTime);
        }

        yield return Wait(startDelay);

        if (showDayTitle)
        {
            yield return FadeTitle(0f, 1f, titleFadeIn);
            yield return Wait(titleHold);
            yield return FadeTitle(1f, 0f, titleFadeOut);
            yield return Wait(0.5f);
        }

        // Eyes struggle open, blink, then open fully
        yield return MoveEyes(0.25f, 1.0f);
        yield return Wait(0.3f);
        yield return MoveEyes(0f, 0.5f);
        yield return Wait(0.4f);
        yield return MoveEyes(0.6f, 1.0f);
        yield return Wait(0.2f);
        yield return MoveEyes(0.1f, 0.4f);
        yield return MoveEyes(1f, 1.8f);

        Destroy(canvas.gameObject);
        SetControls(true);

        if (OnFinished != null) OnFinished.Invoke();
    }

    private IEnumerator Wait(float seconds)
    {
        yield return new WaitForSeconds(seconds / Mathf.Max(0.01f, speed));
    }

    private IEnumerator MoveEyes(float target, float duration)
    {
        float start = open;
        float time = 0f;
        duration /= Mathf.Max(0.01f, speed);

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / duration);
            SetOpen(Mathf.Lerp(start, target, t));
            yield return null;
        }
        SetOpen(target);
    }

    private IEnumerator FadeTitle(float from, float to, float duration)
    {
        float time = 0f;
        duration /= Mathf.Max(0.01f, speed);

        while (time < duration)
        {
            time += Time.deltaTime;
            SetTitleAlpha(Mathf.Lerp(from, to, time / duration));
            yield return null;
        }
        SetTitleAlpha(to);
    }

    // ---------------------------------------------------------------- Helpers

    private void SetControls(bool enabled)
    {
        foreach (Behaviour b in disableDuringIntro)
        {
            if (b != null) b.enabled = enabled;
        }
    }

    private void SetOpen(float value)
    {
        open = Mathf.Clamp01(value);

        // Eyelids meet in the middle when closed and slide apart when opening
        topLid.anchorMin = new Vector2(0f, Mathf.Lerp(0.49f, 1f, open));
        topLid.anchorMax = new Vector2(1f, 1f);
        topLid.offsetMin = Vector2.zero;
        topLid.offsetMax = Vector2.zero;

        bottomLid.anchorMin = new Vector2(0f, 0f);
        bottomLid.anchorMax = new Vector2(1f, Mathf.Lerp(0.51f, 0f, open));
        bottomLid.offsetMin = Vector2.zero;
        bottomLid.offsetMax = Vector2.zero;

        // Dark, dim vision that brightens as the eyes open
        Color c = dim.color;
        c.a = Mathf.Clamp01(1f - open * 1.5f);
        dim.color = c;
    }

    private void SetTitleAlpha(float a)
    {
        if (titleText == null) return;
        Color c = titleText.color;
        c.a = a;
        titleText.color = c;
    }

    // ---------------------------------------------------------------- UI building

    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("WakeUpCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // draw on top of everything

        dim = CreateImage("Dim", canvasObj.transform, new Color(0f, 0f, 0f, 1f)).GetComponent<Image>();
        topLid = CreateImage("TopLid", canvasObj.transform, Color.black);
        bottomLid = CreateImage("BottomLid", canvasObj.transform, Color.black);

        if (showDayTitle) CreateTitle(canvasObj.transform);
    }

    private RectTransform CreateImage(string objName, Transform parent, Color color)
    {
        GameObject obj = new GameObject(objName);
        obj.transform.SetParent(parent, false);

        Image img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    private void CreateTitle(Transform parent)
    {
        GameObject obj = new GameObject("DayTitle");
        obj.transform.SetParent(parent, false);

        titleText = obj.AddComponent<Text>();
        titleText.text = "Day " + dayNumber;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontSize = fontSize;
        titleText.color = new Color(1f, 1f, 1f, 0f);
        titleText.raycastTarget = false;

#if UNITY_2022_2_OR_NEWER
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}