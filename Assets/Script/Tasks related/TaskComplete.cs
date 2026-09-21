using UnityEngine;

public class TaskComplete : MonoBehaviour
{
    public enum CompletionType
    {
        Trigger,
        Pickup,
        Interact,
        Custom
    }

    public CompletionType completionType;

    public string taskID;

    public bool destroyOnComplete = false;

    private bool playerInside = false;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (completionType == CompletionType.Trigger ||
            completionType == CompletionType.Pickup)
        {
            Complete();

            if (completionType == CompletionType.Pickup && destroyOnComplete)
            {
                Destroy(gameObject);
            }
        }

        if (completionType == CompletionType.Interact)
        {
            playerInside = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (completionType == CompletionType.Interact)
        {
            playerInside = false;
        }
    }

    public void Interact()
    {
        if (completionType != CompletionType.Interact)
            return;

        if (!playerInside)
            return;

        Complete();
    }

    public void Complete()
    {
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.CompleteTask(taskID);
        }
    }
}