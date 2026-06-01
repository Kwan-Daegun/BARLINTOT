using UnityEngine;
using System.Collections;

public class JumpscareManager : MonoBehaviour
{
    public static JumpscareManager Instance;

    public GameObject jumpscareImageUI;
    public GameObject deathScreenUI;
    public AudioSource audioSource;
    public AudioClip scareSound;

    public MonoBehaviour playerLookScript;
    public MonoBehaviour playerMovementScript;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (deathScreenUI != null) deathScreenUI.SetActive(false);
        if (jumpscareImageUI != null) jumpscareImageUI.SetActive(false);
    }

    public void TriggerComputerScare()
    {
        StartCoroutine(ComputerScareRoutine());
    }

    public void TriggerPaintTableScare()
    {
        StartCoroutine(PaintTableScareRoutine());
    }

    private IEnumerator ComputerScareRoutine()
    {
        if (playerLookScript != null) playerLookScript.enabled = false;
        if (playerMovementScript != null) playerMovementScript.enabled = false;

        if (scareSound != null) audioSource.PlayOneShot(scareSound);
        if (jumpscareImageUI != null) jumpscareImageUI.SetActive(true);

        yield return new WaitForSeconds(1.5f);
        ShowDeathScreen();
    }

    private IEnumerator PaintTableScareRoutine()
    {
        if (playerLookScript != null) playerLookScript.enabled = false;
        if (playerMovementScript != null) playerMovementScript.enabled = false;

        yield return new WaitForSeconds(2.5f);

        if (scareSound != null) audioSource.PlayOneShot(scareSound);
        if (jumpscareImageUI != null) jumpscareImageUI.SetActive(true);

        yield return new WaitForSeconds(1.0f);
        ShowDeathScreen();
    }

    private void ShowDeathScreen()
    {
        if (jumpscareImageUI != null) jumpscareImageUI.SetActive(false);

        deathScreenUI.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }
}