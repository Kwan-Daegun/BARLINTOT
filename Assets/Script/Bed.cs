using UnityEngine;

/// <summary>
/// Put this on your bed (it needs a Collider, like your other interactables).
/// At night, press E to sleep: the screen goes black, the next day begins, and you wake up.
/// </summary>
public class Bed : MonoBehaviour, IInteractable
{
    public string sleepPrompt = "[E] Sleep";
    [Tooltip("Shown while it's still daytime. Leave empty to show nothing.")]
    public string notTiredPrompt = "Not tired yet";

    public string GetPromptText()
    {
        DayNightManager days = DayNightManager.Instance;
        if (days == null || days.IsSleeping || GameEndManager.GameIsOver) return string.Empty;
        if (DialogueUI.IsOpen || OrderChoiceUI.IsOpen || DocumentCheckUI.IsOpen) return string.Empty;

        return days.CanSleep ? sleepPrompt : notTiredPrompt;
    }

    public void Interact()
    {
        if (DialogueUI.IsOpen || OrderChoiceUI.IsOpen || DocumentCheckUI.IsOpen) return;

        if (DayNightManager.Instance != null)
            DayNightManager.Instance.TrySleep();
    }
}