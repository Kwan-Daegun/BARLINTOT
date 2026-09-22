using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField] private bool lockYAxisOnly = true;
    private Transform mainCameraTransform;

    void Start()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (mainCameraTransform == null) return;

        if (lockYAxisOnly)
        {
            // Keep the object upright
            Vector3 targetPosition = mainCameraTransform.position;
            targetPosition.y = transform.position.y;
            
            transform.LookAt(targetPosition);
        }
        else
        {
            transform.rotation = mainCameraTransform.rotation;
        }
    }
}
