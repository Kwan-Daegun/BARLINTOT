using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TeleportTrigger : MonoBehaviour
{
    [Tooltip("Where the player will be teleported to.")]
    public Transform destination;

    [Tooltip("Only objects with this tag will be teleported.")]
    public string targetTag = "Player";

    private void Reset()
    {
        // Make sure the collider on this object is a trigger.
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(targetTag)) return;
        if (destination == null) return;

        // CharacterController overrides transform changes, so disable it briefly.
        CharacterController cc = other.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        other.transform.position = destination.position;

        Rigidbody rb = other.attachedRigidbody;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.position = destination.position;
        }

        if (cc != null) cc.enabled = true;
    }
}