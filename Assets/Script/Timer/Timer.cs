using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Timer works almost exactly how Godot's Timer node works
public class Timer : MonoBehaviour
{
    public UnityEvent timeout = new UnityEvent();

    /// <summary>
    /// The time interval for the timer, waits for this amount of Seconds before Invoking timeout Event
    /// You need to reset the timer when assigning a new wait time while its running, else it will still use the previous wait time
    /// </summary>
    public float waitTime = 1.0f;
    /// <summary>
    /// If false the timer repeats the Coroutine after finishing, if true the Timer runs only once 
    /// </summary>
    public bool oneShot = false;
    /// <summary>
    /// If true the timer runs regardless of timeScale, if false the timer depends on the timeScale value
    /// </summary>
    public bool unscaledTime = false;

    private Coroutine activeTimer;

    public static Timer CreateTimer(GameObject target)
    {
        Timer newTimer = target.AddComponent<Timer>();
        return newTimer;
    }

    /// <summary>
    /// Starts a Coroutine timer based on the wait time assigned to the component
    /// </summary>
    public void StartTime() => StartTime(waitTime);

    /// <summary>
    /// Starts a Coroutine timer based with the assigned argument instead of the wait time
    /// </summary>
    /// <param name="time"></param>
    public void StartTime(float time)
    {
        StopTimer();
        activeTimer = StartCoroutine(timer(time));
    }

    /// <summary>
    /// Stops the timer's Coroutine
    /// </summary>
    public void StopTimer()
    {
        if (activeTimer != null)
        {
            StopCoroutine(activeTimer);
            activeTimer = null;
        }
    }

    /// <summary>
    /// Returns true if the Coroutine is still running, Returns false otherwise
    /// </summary>
    /// <returns></returns>
    public bool IsActive()
    {
        return activeTimer != null;
    }

    private IEnumerator timer(float time)
    {
        do
        {
            if (unscaledTime)
                yield return new WaitForSecondsRealtime(time);
            else
                yield return new WaitForSeconds(time);
            
            timeout.Invoke();

        } while (!oneShot);
        
        activeTimer = null;
    }
}