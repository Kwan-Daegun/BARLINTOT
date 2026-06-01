using UnityEngine;
using System.Collections;

public class DayNightManager : MonoBehaviour
{
    public static DayNightManager Instance;

    [Header("References")]
    public NPCSpawner npcSpawner;

    public Material daySkybox;
    public Material nightSkybox;
    public Light directionalLight;

    [Header("Lighting Settings")]
    public float dayLightIntensity = 1f;
    public float nightLightIntensity = 0f;
    public float dayAmbientIntensity = 1f;
    public float nightAmbientIntensity = 0f;
    public float dayReflectionIntensity = 1f;
    public float nightReflectionIntensity = 0f;

    [Header("Time & Transitions")]
    public float dayShiftDuration = 120f;
    public float nightShiftDuration = 60f;
    public float transitionDuration = 5f;

    private float currentShiftTimer;
    public bool isNightTime = false;
    private Coroutine transitionRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        isNightTime = false;
        currentShiftTimer = dayShiftDuration;
        RenderSettings.skybox = daySkybox;
        RenderSettings.ambientIntensity = dayAmbientIntensity;
        RenderSettings.reflectionIntensity = dayReflectionIntensity;

        if (directionalLight != null)
        {
            directionalLight.intensity = dayLightIntensity;
        }
        DynamicGI.UpdateEnvironment();
    }

    private void Update()
    {
        currentShiftTimer -= Time.deltaTime;

        if (currentShiftTimer <= 0)
        {
            if (!isNightTime)
            {
                StartNightShift();
            }
            else
            {
                StartDayShift();
            }
        }
    }

    public void StartNightShift()
    {
        if (isNightTime) return;
        isNightTime = true;
        currentShiftTimer = nightShiftDuration;

        if (GameManager.Instance != null && GameManager.Instance.hasActiveOrder)
        {
            GameManager.Instance.RejectCustomer();
        }

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionLighting(nightSkybox, nightLightIntensity, nightAmbientIntensity, nightReflectionIntensity));
    }

    public void StartDayShift()
    {
        if (!isNightTime) return;
        isNightTime = false;
        currentShiftTimer = dayShiftDuration;

        if (npcSpawner != null)
        {
            npcSpawner.ResumeSpawning();
        }

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionLighting(daySkybox, dayLightIntensity, dayAmbientIntensity, dayReflectionIntensity));
    }

    private IEnumerator TransitionLighting(Material targetSkybox, float targetLight, float targetAmbient, float targetReflection)
    {
        RenderSettings.skybox = targetSkybox;

        float startLight = directionalLight != null ? directionalLight.intensity : 0f;
        float startAmbient = RenderSettings.ambientIntensity;
        float startReflection = RenderSettings.reflectionIntensity;

        float timeElapsed = 0f;

        while (timeElapsed < transitionDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / transitionDuration;

            if (directionalLight != null)
            {
                directionalLight.intensity = Mathf.Lerp(startLight, targetLight, t);
            }
            RenderSettings.ambientIntensity = Mathf.Lerp(startAmbient, targetAmbient, t);
            RenderSettings.reflectionIntensity = Mathf.Lerp(startReflection, targetReflection, t);

            yield return null;
        }

        if (directionalLight != null)
        {
            directionalLight.intensity = targetLight;
        }
        RenderSettings.ambientIntensity = targetAmbient;
        RenderSettings.reflectionIntensity = targetReflection;

        DynamicGI.UpdateEnvironment();
    }
}