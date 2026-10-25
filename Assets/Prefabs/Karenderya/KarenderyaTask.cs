using UnityEngine;

public class KarenderyaTask : MonoBehaviour
{
    public KarenderyaShop shop;
    public Task task;

    private void CompleteTask(Item _food)
    {
        TaskManager.Instance.CompleteTask(task.taskID);
    }

    public void StartTask()
    {
        TaskManager.Instance.activeTasks.Add(task);
        shop.foodBought.AddListener(CompleteTask);
    }
}
