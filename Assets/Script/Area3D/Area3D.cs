using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))] 
public class Area3D : MonoBehaviour
{
    public UnityEvent<GameObject> onBodyEnter = new UnityEvent<GameObject>();
    public UnityEvent<GameObject> onBodyExit = new UnityEvent<GameObject>();

    private void Awake()
    {
        // Automatically ensure the attached collider is set as a trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        onBodyEnter.Invoke(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        onBodyExit.Invoke(other.gameObject);
    }
}