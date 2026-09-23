using UnityEngine;

public class CamLook : MonoBehaviour
{
    public float mouseSensitivity = 200f;
    public Transform playerBody;

    private static bool inputLocked = false;
    private float xRotation = 0f;

    private void Start()
    {
        SetCursorState(false);
    }

    private void Update()
    {
        if (inputLocked) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    /// <summary>
    /// Call this when opening/closing a UI panel.
    /// uiOpen = true  -> show cursor, stop camera movement
    /// uiOpen = false -> hide cursor, resume camera movement
    /// </summary>
    public static void SetUIOpen(bool uiOpen)
    {
        inputLocked = uiOpen;
        SetCursorState(uiOpen);
    }

    private static void SetCursorState(bool uiOpen)
    {
        if (uiOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}