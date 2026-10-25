using UnityEngine;

public class KarenderyaTask : MonoBehaviour
{
    public KarenderyaShop shop;
    public Task task;

    private void Start()
    {
        shop.foodBought.AddListener(CompleteTask);
    }

    private void CompleteTask(Item _food)
    {
        TaskManager.Instance.CompleteTask(task.taskID);
    }
}
