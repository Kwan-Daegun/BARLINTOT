using UnityEngine;
using UnityEngine.AI;

public class CustomerAI : MonoBehaviour
{
    public enum CustomerType
    {
        Human,
        Ghoul,
        Ghost
    }

    [Header("Customer Settings")]
    public CustomerType customerType;

    [HideInInspector] public GameObject servingPoint;
    [HideInInspector] public CustomerSpawner spawner;

    private NavMeshAgent agent;
    private bool hasArrived = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.SetDestination(servingPoint.transform.position);
    }

    void Update()
    {
        if (!hasArrived && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            hasArrived = true;
            OnReachedCounter();
        }
    }

    void OnReachedCounter()
    {
        switch (customerType)
        {
            case CustomerType.Human:
                ServeHuman();
                break;
            case CustomerType.Ghoul:
                ServeGhoul();
                break;
            case CustomerType.Ghost:
                ServeGhost();
                break;
        }
    }

    void ServeHuman()
    {
        Debug.Log("Serving a Human customer");
        //ADD THE FUCKING SERVING LOGIC HERE, SAME GOES FOR EVERY OTHER CUSTOMER TYPE
        FinishAndLeave();
    }

    void ServeGhoul()
    {
        Debug.Log("Serving a Ghoul customer");
        FinishAndLeave();
    }

    void ServeGhost()
    {
        Debug.Log("Serving a Ghost customer");
        FinishAndLeave();
    }

    void FinishAndLeave()
    {
        spawner.CustomerLeft();
        Destroy(gameObject);
    }
}