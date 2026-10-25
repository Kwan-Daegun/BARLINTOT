using UnityEngine;
using UnityEngine.AI;
using System;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCMovementController : MonoBehaviour
{
    public enum NPCState { Walking, Standing, AskingOrder, PresentingDocs, Exiting }

    public Action onReachedCounter;
    public Action onExited;
    public Action<NPCState> onStateChanged;

    public NPCData npcData;
    public NPCState CurrentState { get; private set; } = NPCState.Walking;

    private Transform counterPosition;
    private Transform exitPosition;
    private NavMeshAgent agent;
    private bool isWaitingAtCounter;
    private bool hasBeenServed;
    private bool warnedNoNavMesh;

    private Animator[] npcAnimators;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    [Header("Audio")]
    public AudioClip walkingSound;
    private AudioSource audioSource;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
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
            Instantiate(npcData.normalModelPrefab, transform.position, transform.rotation, transform);
        }

        // Find every Animator on this NPC (the model from NPCData and/or one already inside Driver)
        npcAnimators = GetComponentsInChildren<Animator>();

        if (npcAnimators.Length == 0)
        {
            Debug.LogWarning($"NPC '{name}' has no Animator on it or its models.", this);
            return;
        }

        foreach (Animator a in npcAnimators)
            a.applyRootMotion = false; // the NavMeshAgent moves the NPC, not the animation
    }

    private void SetState(NPCState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        onStateChanged?.Invoke(CurrentState);
    }

    public void WalkToCounter()
    {
        SetState(NPCState.Walking);
        isWaitingAtCounter = false;
        SetDestinationSafe(counterPosition.position);
    }

    public void SetAskingOrder()
    {
        SetState(NPCState.AskingOrder);
    }

    public void SetPresentingDocs()
    {
        SetState(NPCState.PresentingDocs);
    }

    public void ServeOrReject()
    {
        hasBeenServed = true;
        isWaitingAtCounter = false;
        WalkToExit();
    }

    private void WalkToExit()
    {
        SetState(NPCState.Exiting);
        SetDestinationSafe(exitPosition.position);
    }

    // Avoids the "must be placed on a NavMesh" error flood and tells you the real problem once
    private void SetDestinationSafe(Vector3 target)
    {
        if (!agent.isOnNavMesh)
        {
            WarnNoNavMesh();
            return;
        }
        agent.SetDestination(target);
    }

    private void WarnNoNavMesh()
    {
        if (warnedNoNavMesh) return;
        warnedNoNavMesh = true;
        Debug.LogWarning($"NPC '{name}' is not on a NavMesh, so it can't walk. " +
                         "Bake a NavMesh that covers the spawn point, counter and exit.", this);
    }

    private void UpdateAnimators()
    {
        if (npcAnimators == null) return;

        float speed = agent.velocity.magnitude;
        foreach (Animator a in npcAnimators)
        {
            if (a != null) a.SetFloat(SpeedHash, speed);
        }
    }

    private void Update()
    {
        UpdateAnimators();
        HandleFootstepSound();

        if (!agent.isOnNavMesh)
        {
            WarnNoNavMesh();
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
            {
                if (!isWaitingAtCounter && !hasBeenServed)
                {
                    isWaitingAtCounter = true;
                    SetState(NPCState.Standing);
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

    private void HandleFootstepSound()
    {
        if (audioSource == null || walkingSound == null) return;
        if (npcData.type == NPCType.Anomaly && npcData.anomalyType != AnomalyType.Physical) return;

        bool isMoving = agent.velocity.magnitude > 0.1f;

        if (isMoving && !audioSource.isPlaying)
        {
            audioSource.clip = walkingSound;
            audioSource.loop = true;
            audioSource.Play();
        }
        else if (!isMoving && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    private void TriggerDoorClose()
    {
        Debug.Log("Door Closing Event Triggered");
    }
}