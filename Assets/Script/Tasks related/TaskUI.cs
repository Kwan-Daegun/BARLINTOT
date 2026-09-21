using UnityEngine;
using TMPro; // Assuming TextMeshPro

public class TaskUI : MonoBehaviour
{
    public TextMeshProUGUI taskTextDisplay;

    void Start()
    {
        TaskManager.Instance.OnTaskUpdated += RefreshUI;
        RefreshUI(); // Initial draw
    }

    void OnDestroy()
    {
        if (TaskManager.Instance != null)
            TaskManager.Instance.OnTaskUpdated -= RefreshUI;
    }

    void RefreshUI()
    {
        taskTextDisplay.text = "Current Tasks:\n";
        foreach (Task t in TaskManager.Instance.activeTasks)
        {
            string checkbox = t.isCompleted ? "[X]" : "[ ]";
            taskTextDisplay.text += $"{checkbox} {t.description}\n";
        }
    }
}