using UnityEngine;
using System.Collections;

public class CraftingStation : MonoBehaviour, IInteractable
{
    public int stationStepNumber;
    public string stationName;
    public float craftingDuration = 3f;
    public ParticleSystem craftingVFX;
    public MonoBehaviour playerMovementScript;
    public AudioClip[] craftingSounds;

    [Header("Horror Settings")]
    [Range(0f, 100f)]
    public float jumpscareChancePercent = 50f;

    private bool isCompleted = false;
    private bool isCrafting = false;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public string GetPromptText()
    {
        if (!GameManager.Instance.hasActiveOrder)
            return "Need an order first";
        if (isCompleted)
            return stationName + " (Done)";
        if (isCrafting)
            return "Crafting...";
        if (GameManager.Instance.currentCraftingStep != stationStepNumber - 1)
            return "Finish previous step first";

        return "[E] Use " + stationName;
    }

    public void Interact()
    {
        if (isCompleted || isCrafting) return;

        if (GameManager.Instance.currentOrderData != null &&
            GameManager.Instance.currentOrderData.type == NPCType.Anomaly)
        {
            float roll = Random.Range(0f, 100f);

            if (roll <= jumpscareChancePercent)
            {
                StartCoroutine(AnomalyInterruptRoutine());
                return;
            }
        }

        if (GameManager.Instance.TryStartCraftingStep(stationStepNumber))
            StartCoroutine(CraftRoutine());
    }

    private IEnumerator AnomalyInterruptRoutine()
    {
        isCrafting = true;
        if (playerMovementScript != null) playerMovementScript.enabled = false;

        if (stationStepNumber == 1)
            JumpscareManager.Instance.TriggerComputerScare();
        else
            JumpscareManager.Instance.TriggerPaintTableScare();

        yield return null;
    }

    private IEnumerator CraftRoutine()
    {
        isCrafting = true;

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
            playerMovementScript.GetComponent<AudioSource>().Stop();
        }
        if (craftingVFX != null) craftingVFX.Play();

        if (audioSource != null && craftingSounds.Length > 0)
        {
            audioSource.clip = craftingSounds[Random.Range(0, craftingSounds.Length)];
            audioSource.loop = true;
            audioSource.Play();
        }

        yield return new WaitForSeconds(craftingDuration);

        if (audioSource != null) audioSource.Stop();
        if (craftingVFX != null) craftingVFX.Stop();
        if (playerMovementScript != null) playerMovementScript.enabled = true;

        GameManager.Instance.CompleteCraftingStep(stationStepNumber);
        isCompleted = true;
        isCrafting = false;
    }

    public void ResetStation()
    {
        isCompleted = false;
        isCrafting = false;
    }
}