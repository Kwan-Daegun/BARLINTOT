using UnityEngine;
using UnityEngine.AI;
using System;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCMovementController : MonoBehaviour
{
    public Action onReachedCounter;
    public Action onExited;

    public NPCData npcData;
    private Transform counterPosition;
    private Transform exitPosition;
    private NavMeshAgent agent;
    private bool isWaitingAtCounter;
    private bool hasBeenServed;

    private Animator npcAnimator;

    private void Awake()
    {
        /*agent = GetComponent<NavMeshAgent>();*/

        npcAnimator = GetComponent<Animator>();
        // Grab the NavMeshAgent from the parent
        agent = GetComponentInParent<NavMeshAgent>();

    }

    public void Initialize(NPCData data, Transform counter, Transform exit)
    {
        npcData = data;
        counterPosition = counter;
        exitPosition = exit;

        ApplyVisuals();
        WalkToCounter();
    }

    private void ApplyVisuals()
    {
        if (npcData.normalModelPrefab != null)
        {
            GameObject model = Instantiate(npcData.normalModelPrefab, transform.position, transform.rotation, transform);

            npcAnimator = model.GetComponent<Animator>();
            if (npcAnimator == null)
            {
                npcAnimator = model.GetComponentInChildren<Animator>();
            }
        }

        if (npcData.type == NPCType.Anomaly && npcData.anomalyType == AnomalyType.Physical)
        {
            if (npcData.physicalMorphPrefab != null)
            {
                Instantiate(npcData.physicalMorphPrefab, transform.position, transform.rotation, transform);
            }
        }
    }

    public void WalkToCounter()
    {
        isWaitingAtCounter = false;
        agent.SetDestination(counterPosition.position);
    }

    public void ServeOrReject()
    {
        hasBeenServed = true;
        isWaitingAtCounter = false;
        WalkToExit();
    }

    private void WalkToExit()
    {
        agent.SetDestination(exitPosition.position);
    }

    private void Update()
    {
        /*if (npcAnimator != null)
        {
            float speed = agent.velocity.magnitude;
            npcAnimator.SetFloat("Speed", speed);
*//*
            if (speed > 0.1f)
            {
                npcAnimator.Play("Walking");
            }
            else
            {
                npcAnimator.Play("Idle");
            }*//*
        }*/

        if (npcAnimator == null || agent == null) return;

        float speed = agent.velocity.magnitude;

        if (speed > 0.1f)
        {
            npcAnimator.Play("Walking");
        }
        else
        {
            npcAnimator.Play("Idle");
        }


        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                if (!isWaitingAtCounter && !hasBeenServed)
                {
                    isWaitingAtCounter = true;
                    onReachedCounter?.Invoke();
                }
                else if (hasBeenServed)
                {
                    TriggerDoorClose();
                    onExited?.Invoke();
                    Destroy(gameObject);
                }
            }
        }
    }

    private void TriggerDoorClose()
    {
        Debug.Log("Door Closing Event Triggered");
    }
}