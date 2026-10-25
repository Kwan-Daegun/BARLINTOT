using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using System.Collections;

public class CraftingStation : MonoBehaviour, IInteractable
{
    public int stationStepNumber;
    public string stationName;
    public float craftingDuration = 3f;
    public ParticleSystem craftingVFX;
    public MonoBehaviour playerMovementScript;
    public AudioClip[] craftingSounds;

    [Header("Optional Tool Animation (fill in on the stencil station only)")]
    [Tooltip("Your chisel .anim file.")]
    public AnimationClip toolClip;
    [Tooltip("The Animator on the object the clip animates (the chisel, or the player's arms).")]
    public Animator toolAnimator;
    [Tooltip("Optional: chisel model that is only visible while crafting.")]
    public GameObject toolObject;

    private bool isCompleted = false;
    private bool isCrafting = false;
    private AudioSource audioSource;
    private AudioSource playerAudio;
    private PlayableGraph toolGraph;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (playerMovementScript != null)
            playerAudio = playerMovementScript.GetComponent<AudioSource>();

        if (toolObject != null) toolObject.SetActive(false);
    }

    void OnDestroy()
    {
        StopToolGraph();
    }

    public string GetPromptText()
    {
        GameManager gm = GameManager.Instance;

        if (!gm.hasActiveOrder)
            return "Need an order first";
        if (!gm.orderPaid)
            return "Customer hasn't paid yet";
        if (isCompleted)
            return stationName + " (Done)";
        if (isCrafting)
            return "Crafting...";
        if (gm.currentCraftingStep != stationStepNumber - 1)
            return "Finish previous step first";

        return "[E] Use " + stationName;
    }

    public void Interact()
    {
        if (isCompleted || isCrafting) return;

        if (GameManager.Instance.TryStartCraftingStep(stationStepNumber))
            StartCoroutine(CraftRoutine());
    }

    private void SetToolAnimation(bool active)
    {
        if (toolObject != null) toolObject.SetActive(active);

        StopToolGraph();

        if (active && toolClip != null && toolAnimator != null)
            AnimationPlayableUtilities.PlayClip(toolAnimator, toolClip, out toolGraph);
    }

    private void StopToolGraph()
    {
        if (toolGraph.IsValid()) toolGraph.Destroy();
    }

    private IEnumerator CraftRoutine()
    {
        isCrafting = true;

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
            if (playerAudio != null) playerAudio.Stop();
        }
        if (craftingVFX != null) craftingVFX.Play();

        SetToolAnimation(true);

        if (audioSource != null && craftingSounds.Length > 0)
        {
            audioSource.clip = craftingSounds[Random.Range(0, craftingSounds.Length)];
            audioSource.loop = true;
            audioSource.Play();
        }

        yield return new WaitForSeconds(craftingDuration);

        if (audioSource != null) audioSource.Stop();
        if (craftingVFX != null) craftingVFX.Stop();
        SetToolAnimation(false);
        if (playerMovementScript != null) playerMovementScript.enabled = true;

        GameManager.Instance.CompleteCraftingStep(stationStepNumber);
        isCompleted = true;
        isCrafting = false;
    }

    public void ResetStation()
    {
        StopAllCoroutines();
        if (audioSource != null) audioSource.Stop();
        if (craftingVFX != null) craftingVFX.Stop();
        SetToolAnimation(false);
        if (playerMovementScript != null) playerMovementScript.enabled = true;

        isCompleted = false;
        isCrafting = false;
    }
}