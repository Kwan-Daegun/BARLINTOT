using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class NPCBase<TState> : MonoBehaviour where TState : struct, System.Enum
{
    public TState current_state;

    protected NavMeshAgent agent;
    protected Transform target;

    protected Timer updateTimer;
    public float updateInterval = 1f;
    public float nearbyThreshold = 2f;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) Debug.LogError($"{gameObject.name} does not have a NavMeshAgent!");

        SetupUpdateTimer();
    }

    protected virtual void SetupUpdateTimer()
    {
        updateTimer = gameObject.AddComponent<Timer>();
        updateTimer.waitTime = updateInterval;
        updateTimer.StartTime();
        updateTimer.timeout.AddListener(UpdateAI);
    }

    /// <summary>
    /// Called every updateInterval seconds. This is where you put your state switch here.
    /// </summary>
    protected abstract void UpdateAI();

    public virtual void MoveTo(Transform newTarget)
    {
        if (newTarget == null)
        {
            agent.SetDestination(transform.position);
            return;
        }

        target = newTarget;
        agent.SetDestination(target.position);
    }

    public virtual bool InDestination()
    {
        if (Vector3.Distance(agent.destination, transform.position) < nearbyThreshold)
        {
            target = null;
            return true;
        }

        return false;
    }
}