using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    [Header("Points")]
    public GameObject spawnPoint;
    public GameObject servingPoint;

    [Header("Customer Prefabs")]
    public GameObject humanPrefab;
    public GameObject ghoulPrefab;
    public GameObject ghostPrefab;

    [Header("Settings")]
    public float minSpawnTime = 3f;
    public float maxSpawnTime = 8f;

    private bool isWaitingForCustomer = false;
    private GameObject currentCustomer = null;

    void Start()
    {
        SpawnNextCustomer();
    }

    void SpawnNextCustomer()
    {
        float randomTime = Random.Range(minSpawnTime, maxSpawnTime);
        Invoke(nameof(SpawnCustomer), randomTime);
    }

    void SpawnCustomer()
    {
        if (currentCustomer != null) return;

        GameObject[] prefabs = { humanPrefab, ghoulPrefab, ghostPrefab };
        CustomerAI.CustomerType[] types = {
            CustomerAI.CustomerType.Human,
            CustomerAI.CustomerType.Ghoul,
            CustomerAI.CustomerType.Ghost
        };

        int index = Random.Range(0, prefabs.Length);

        currentCustomer = Instantiate(prefabs[index], spawnPoint.transform.position, spawnPoint.transform.rotation);
        CustomerAI ai = currentCustomer.GetComponent<CustomerAI>();
        ai.customerType = types[index];
        ai.servingPoint = servingPoint;
        ai.spawner = this;
    }

    public void CustomerLeft()
    {
        currentCustomer = null;
        SpawnNextCustomer();
    }
}