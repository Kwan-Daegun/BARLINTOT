using System.Collections.Generic;
using UnityEngine;
using System;

public class TaskManager : MonoBehaviour
{
    public static TaskManager Instance;

    public List<Task> activeTasks = new List<Task>();

    public event Action OnTaskUpdated;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CompleteTask(string id)
    {
        int currentTaskIndex = GetCurrentTaskIndex();

        if (currentTaskIndex == -1)
            return;

        Task currentTask = activeTasks[currentTaskIndex];

        if (currentTask.taskID == id)
        {
            currentTask.isCompleted = true;
            OnTaskUpdated?.Invoke();

            Debug.Log("Task completed: " + currentTask.description);
        }
        else
        {
            Debug.Log("This is not the current task.");
        }
    }

    public int GetCurrentTaskIndex()
    {
        for (int i = 0; i < activeTasks.Count; i++)
        {
            if (!activeTasks[i].isCompleted)
            {
                return i;
            }
        }

        return -1;
    }

    public Task GetCurrentTask()
    {
        int index = GetCurrentTaskIndex();

        if (index == -1)
            return null;

        return activeTasks[index];
    }
}