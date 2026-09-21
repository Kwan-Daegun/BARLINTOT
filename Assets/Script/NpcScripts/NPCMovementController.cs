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

    private Animator childAnimator;

    private void Awake()
    {
        /*agent = GetComponent<NavMeshAgent>();*/

        childAnimator = GetComponent<Animator>();
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

    public void SetDestination(Vector3 target)
    {
        agent.SetDestination(target);
        childAnimator?.Play("Walking");
    }

    public bool InDestination()
    {
        return Vector3.Distance(transform.position, agent.destination) < 2f;
    }

    /* private void ApplyVisuals()
     {
         if (npcData.normalModelPrefab != null)
         {
             GameObject model = Instantiate(npcData.normalModelPrefab, transform.position, transform.rotation, transform);

             animator = model.GetComponent<Animator>();
             if (animator == null)
             {
                 animator = model.GetComponentInChildren<Animator>();
             }
         }

         if (npcData.type == NPCType.Anomaly && npcData.anomalyType == AnomalyType.Physical)
         {
             if (npcData.physicalMorphPrefab != null)
             {
                 Instantiate(npcData.physicalMorphPrefab, transform.position, transform.rotation, transform);
             }
         }
     }*/

    private void ApplyVisuals()
    {
        if (npcData.normalModelPrefab != null)
        {
            GameObject model = Instantiate(npcData.normalModelPrefab, transform.position, transform.rotation, transform);

            childAnimator = model.GetComponent<Animator>();
            if (childAnimator == null)
                childAnimator = model.GetComponentInChildren<Animator>();

            if (childAnimator != null && npcData.animatorController != null)
                childAnimator.runtimeAnimatorController = npcData.animatorController;
        }
    }

    /*public void WalkToCounter()
    {
        isWaitingAtCounter = false;
        agent.SetDestination(counterPosition.position);
    }*/

    public void WalkToCounter()
    {
        isWaitingAtCounter = false;
        agent.SetDestination(counterPosition.position);
        childAnimator?.Play("Walking"); // trigger here directly
    }

    public void ServeOrReject()
    {
        hasBeenServed = true;
        isWaitingAtCounter = false;
        WalkToExit();
    }

    /*private void WalkToExit()
    {
        agent.SetDestination(exitPosition.position);
    }*/

    private void WalkToExit()
    {
        agent.SetDestination(exitPosition.position);
        childAnimator?.Play("Walking"); // trigger here directly
    }

    /*private void Update()
    {
        *//*if (npcAnimator != null)
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
        }*//*

        if (animator == null || agent == null) return;

        float speed = agent.velocity.magnitude;

        if (speed > 0.1f)
        {
            animator.Play("Walking");
        }
        else
        {
            animator.Play("Idle");
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
    }*/


    private void Update()
    {
        // remove all animation logic from here, no more speed check

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                if (!isWaitingAtCounter && !hasBeenServed)
                {
                    isWaitingAtCounter = true;
                    childAnimator?.Play("Idle"); // arrived at counter
                    onReachedCounter?.Invoke();
                }
                else if (hasBeenServed)
                {
                    childAnimator?.Play("Idle");
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