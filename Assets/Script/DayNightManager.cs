/*using UnityEngine;
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

    [Header("Audio")]
    public AudioSource ambientAudioSource;
    public AudioClip dayAmbientSound;
    public AudioClip[] nightAmbientSounds;
    public float ambientDelay = 3f;

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
            directionalLight.intensity = dayLightIntensity;

        DynamicGI.UpdateEnvironment();
        StartCoroutine(PlayAmbientWithDelay(dayAmbientSound));
    }

    private void Update()
    {
        currentShiftTimer -= Time.deltaTime;

        if (currentShiftTimer <= 0)
        {
            if (!isNightTime)
                StartNightShift();
            else
                StartDayShift();
        }
    }

    public void StartNightShift()
    {
        if (isNightTime) return;
        isNightTime = true;
        currentShiftTimer = nightShiftDuration;

        if (GameManager.Instance != null && GameManager.Instance.hasActiveOrder)
            GameManager.Instance.RejectCustomer();

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionLighting(nightSkybox, nightLightIntensity, nightAmbientIntensity, nightReflectionIntensity));
        StartCoroutine(PlayNightAmbientLoop());
    }

    public void StartDayShift()
    {
        if (!isNightTime) return;
        isNightTime = false;
        currentShiftTimer = dayShiftDuration;

        if (npcSpawner != null)
            npcSpawner.ResumeSpawning();

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionLighting(daySkybox, dayLightIntensity, dayAmbientIntensity, dayReflectionIntensity));
        StartCoroutine(PlayAmbientWithDelay(dayAmbientSound));
    }

    private IEnumerator PlayAmbientWithDelay(AudioClip clip)
    {
        if (ambientAudioSource == null || clip == null) yield break;

        ambientAudioSource.Stop();
        yield return new WaitForSeconds(ambientDelay);

        ambientAudioSource.clip = clip;
        ambientAudioSource.loop = true;
        ambientAudioSource.Play();
    }

    private IEnumerator PlayNightAmbientLoop()
    {
        if (ambientAudioSource == null || nightAmbientSounds.Length == 0) yield break;

        ambientAudioSource.Stop();
        yield return new WaitForSeconds(ambientDelay);

        while (isNightTime)
        {
            AudioClip randomNight = nightAmbientSounds[Random.Range(0, nightAmbientSounds.Length)];
            ambientAudioSource.clip = randomNight;
            ambientAudioSource.loop = false;
            ambientAudioSource.Play();

            yield return new WaitForSeconds(randomNight.length);
        }
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
                directionalLight.intensity = Mathf.Lerp(startLight, targetLight, t);

            RenderSettings.ambientIntensity = Mathf.Lerp(startAmbient, targetAmbient, t);
            RenderSettings.reflectionIntensity = Mathf.Lerp(startReflection, targetReflection, t);

            yield return null;
        }

        if (directionalLight != null)
            directionalLight.intensity = targetLight;

        RenderSettings.ambientIntensity = targetAmbient;
        RenderSettings.reflectionIntensity = targetReflection;

        DynamicGI.UpdateEnvironment();
    }
}*/


using UnityEngine;
using System.Collections;

/// <summary>
/// Day / night cycle plus the day counter.
/// A day lasts "Day Shift Duration", then night falls. Go to bed at night to skip to the next morning
/// (like Minecraft). If you never go to bed, the night ends by itself and you pass out.
/// </summary>
public class DayNightManager : MonoBehaviour
{
    public static DayNightManager Instance;

    [Header("References")]
    public NPCSpawner npcSpawner;
    [Tooltip("The WakeUpIntro on your player. Used for the 'eyes close / eyes open' screen between days. " +
             "Leave empty to find it automatically.")]
    public WakeUpIntro wakeUp;

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

    [Header("Days")]
    [Tooltip("The current day. Starts at 1.")]
    public int dayNumber = 1;
    [Tooltip("Normally you can only sleep once night has fallen. Tick this to allow sleeping any time (for testing).")]
    public bool canSleepAnytime = false;
    [Tooltip("HP you get back from a night in bed.")]
    public float sleepHpGain = 25f;
    [Tooltip("HP you lose if the night ends and you never went to bed (you pass out).")]
    public float passOutHpLoss = 15f;
    [Tooltip("At the end of every day all your money goes to the family.")]
    public bool giveMoneyToFamily = true;

    [Header("Audio")]
    public AudioSource ambientAudioSource;
    public AudioClip dayAmbientSound;
    public AudioClip[] nightAmbientSounds;
    public float ambientDelay = 3f;

    /// <summary>Fires when a new day begins (the new day number).</summary>
    public event System.Action<int> OnNewDay;

    /// <summary>Fires when night falls.</summary>
    public event System.Action OnNightStarted;

    public bool IsSleeping { get; private set; }

    /// <summary>True when the player is allowed to go to bed right now.</summary>
    public bool CanSleep => !IsSleeping && (isNightTime || canSleepAnytime);

    private float currentShiftTimer;
    public bool isNightTime = false;
    private Coroutine transitionRoutine;

    private bool sleptInBed;
    private string morningMessage;

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
            directionalLight.intensity = dayLightIntensity;

        DynamicGI.UpdateEnvironment();
        StartCoroutine(PlayAmbientWithDelay(dayAmbientSound));
    }

    private void Update()
    {
        // The clock stops while sleeping, after the game ended, and during the wake-up intro
        if (IsSleeping || GameEndManager.GameIsOver) return;
        if (TaskListManager.Instance != null && !TaskListManager.Instance.Started) return;

        currentShiftTimer -= Time.deltaTime;

        if (currentShiftTimer <= 0)
        {
            if (!isNightTime)
                StartNightShift();
            else
                BeginSleep(false); // the night is over and you never went to bed: you pass out
        }
    }

    // ---------------------------------------------------------------- Sleeping

    /// <summary>Called by the bed. Returns true if you went to sleep.</summary>
    public bool TrySleep()
    {
        if (IsSleeping) return false;

        if (!CanSleep)
        {
            HintMessage.Show("You're not tired yet.");
            return false;
        }

        BeginSleep(true);
        return true;
    }

    private void BeginSleep(bool inBed)
    {
        if (IsSleeping) return;
        IsSleeping = true;
        sleptInBed = inBed;
        morningMessage = null;

        // Lets a task step finish with Signal Name = Slept
        if (inBed && TaskListManager.Instance != null) TaskListManager.Instance.SendSignal("Slept");

        if (wakeUp == null) wakeUp = FindWakeUp();

        if (wakeUp == null)
        {
            // No WakeUpIntro on the player: just skip the night
            FinishNight();
            HandleWokeUp();
            return;
        }

        // Eyes close -> FinishNight runs while the screen is black -> "Day N" -> eyes open -> HandleWokeUp
        wakeUp.OnFinished += HandleWokeUp;
        wakeUp.PlaySleep(dayNumber + 1, FinishNight);
    }

    // Runs while the screen is black
    private void FinishNight()
    {
        PlayerStats stats = PlayerStats.Instance;
        morningMessage = sleptInBed ? "You slept well." : "You passed out from exhaustion.";

        if (stats != null)
        {
            stats.ChangeHp(sleptInBed ? sleepHpGain : -passOutHpLoss);

            if (giveMoneyToFamily)
            {
                int given = stats.money;
                float change = stats.GiveMoneyToFamily();
                morningMessage += "\nYou gave the family " + PlayerStats.FormatMoney(given)
                                  + ". Their happiness " + (change >= 0f ? "+" : "") + Mathf.RoundToInt(change) + ".";
            }
        }

        // Sleeping in the daytime (testing): night has to fall first so the morning logic is the same
        if (!isNightTime) StartNightShift();

        StartDayShift();
    }

    private void HandleWokeUp()
    {
        if (wakeUp != null) wakeUp.OnFinished -= HandleWokeUp;

        IsSleeping = false;

        if (!string.IsNullOrEmpty(morningMessage))
        {
            HintMessage.Show(morningMessage);
            morningMessage = null;
        }
    }

    private WakeUpIntro FindWakeUp()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<WakeUpIntro>();
#else
        return FindObjectOfType<WakeUpIntro>();
#endif
    }

    // ---------------------------------------------------------------- Night / day

    public void StartNightShift()
    {
        if (isNightTime) return;
        isNightTime = true;
        currentShiftTimer = nightShiftDuration;

        // A customer at the counter leaves. A paid order keeps its progress.
        if (GameManager.Instance != null && GameManager.Instance.hasActiveOrder)
            GameManager.Instance.RejectCustomer();

        // Lets a task step finish with Signal Name = NightFell
        if (TaskListManager.Instance != null) TaskListManager.Instance.SendSignal("NightFell");
        if (OnNightStarted != null) OnNightStarted.Invoke();

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionLighting(nightSkybox, nightLightIntensity, nightAmbientIntensity, nightReflectionIntensity));
        StartCoroutine(PlayNightAmbientLoop());
    }

    public void StartDayShift()
    {
        if (!isNightTime) return;
        isNightTime = false;
        currentShiftTimer = dayShiftDuration;

        // A new day
        dayNumber++;
        if (TaskListManager.Instance != null)
        {
            TaskListManager.Instance.ClearSignal("NightFell");
            TaskListManager.Instance.ClearSignal("Slept");
        }
        if (OnNewDay != null) OnNewDay.Invoke(dayNumber);

        if (npcSpawner != null)
            npcSpawner.ResumeSpawning();

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionLighting(daySkybox, dayLightIntensity, dayAmbientIntensity, dayReflectionIntensity));
        StartCoroutine(PlayAmbientWithDelay(dayAmbientSound));
    }

    private IEnumerator PlayAmbientWithDelay(AudioClip clip)
    {
        if (ambientAudioSource == null || clip == null) yield break;

        ambientAudioSource.Stop();
        yield return new WaitForSeconds(ambientDelay);

        ambientAudioSource.clip = clip;
        ambientAudioSource.loop = true;
        ambientAudioSource.Play();
    }

    private IEnumerator PlayNightAmbientLoop()
    {
        if (ambientAudioSource == null || nightAmbientSounds.Length == 0) yield break;

        ambientAudioSource.Stop();
        yield return new WaitForSeconds(ambientDelay);

        while (isNightTime)
        {
            AudioClip randomNight = nightAmbientSounds[Random.Range(0, nightAmbientSounds.Length)];
            ambientAudioSource.clip = randomNight;
            ambientAudioSource.loop = false;
            ambientAudioSource.Play();

            yield return new WaitForSeconds(randomNight.length);
        }
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
                directionalLight.intensity = Mathf.Lerp(startLight, targetLight, t);

            RenderSettings.ambientIntensity = Mathf.Lerp(startAmbient, targetAmbient, t);
            RenderSettings.reflectionIntensity = Mathf.Lerp(startReflection, targetReflection, t);

            yield return null;
        }

        if (directionalLight != null)
            directionalLight.intensity = targetLight;

        RenderSettings.ambientIntensity = targetAmbient;
        RenderSettings.reflectionIntensity = targetReflection;

        DynamicGI.UpdateEnvironment();
    }
}