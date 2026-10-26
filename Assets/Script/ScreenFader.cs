using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A quick fade to black and back, used when the player is teleported through a door.
/// It builds itself the first time it is used, so there is nothing to set up.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    private static ScreenFader instance;
    private Image overlay;

    /// <summary>Fades to black, runs atBlack while the screen is black, then fades back and runs done.</summary>
    public static void Run(Action atBlack, Action done = null, float fadeOut = 0.2f, float hold = 0.1f, float fadeIn = 0.3f)
    {
        if (instance == null) Create();
        instance.StartCoroutine(instance.Routine(atBlack, done, fadeOut, hold, fadeIn));
    }

    private static void Create()
    {
        GameObject obj = new GameObject("ScreenFader");
        instance = obj.AddComponent<ScreenFader>();

        Canvas canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 998; // above your UI, just under the wake-up screen

        GameObject imageObj = new GameObject("Black");
        imageObj.transform.SetParent(obj.transform, false);

        instance.overlay = imageObj.AddComponent<Image>();
        instance.overlay.color = new Color(0f, 0f, 0f, 0f);
        instance.overlay.raycastTarget = false;

        RectTransform rt = imageObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private IEnumerator Routine(Action atBlack, Action done, float fadeOut, float hold, float fadeIn)
    {
        yield return Fade(0f, 1f, fadeOut);

        if (atBlack != null) atBlack.Invoke();
        yield return new WaitForSecondsRealtime(hold);

        yield return Fade(1f, 0f, fadeIn);

        if (done != null) done.Invoke();
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        duration = Mathf.Max(0.01f, duration);
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, time / duration));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float alpha)
    {
        Color c = overlay.color;
        c.a = alpha;
        overlay.color = c;
    }
}