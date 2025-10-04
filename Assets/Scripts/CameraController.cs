using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;

public class CameraController : MonoBehaviour
{
    [Header("Mouse Look")]
    public InputActionReference lookAction; // Bind this to "Look" (Vector2)
    public float mouseSensitivity = 100f;
    public float verticalClampMin = 20f; // max degrees up/down
    public float verticalClampMax = 0f;

    [Header("Idle Sway")]
    public float swayAmplitude = 1f;   // how far camera sways
    public float swayFrequency = 1f;   // speed of sway

    [Header("Events")]
    public UnityEvent onReachedMaxClamp;
    public UnityEvent onLeaveMaxClamp;

    private float xRotation = 0f;      // vertical look value
    private Vector3 swayBasePos;   // starting camera position
    private float swayTimer = 0f;

    private bool hasTriggeredMaxClamp = false; // prevent spam
    private bool hasLeftMaxClamp = false;

    public bool enableControl = true;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        swayBasePos = transform.localPosition;

        gameObject.transform.localRotation = Quaternion.Euler(10, 0, 0);

        // Enable input action if not already enabled
        if (lookAction != null)
            lookAction.action.Enable();
    }

    void FixedUpdate()
    {
        if (enableControl && !Cursor.visible)
        {
            HandleMouseLook();
            HandleIdleSway();
        }
    }

    private void HandleMouseLook()
    {
        if (lookAction == null) return;

        // Read mouse delta (Vector2)
        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

        // Scale with sensitivity
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        // Vertical look (clamped)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, verticalClampMin, verticalClampMax);

        // Apply rotation (pitch only)
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        if (xRotation >= verticalClampMax - 1)
        {
            if (!hasTriggeredMaxClamp)
            {
                hasTriggeredMaxClamp = true;
                hasLeftMaxClamp = false;
                onReachedMaxClamp?.Invoke();
            }
        }
        else
        {
            if (hasTriggeredMaxClamp && !hasLeftMaxClamp)
            {
                hasLeftMaxClamp = true;
                hasTriggeredMaxClamp = false;
                onLeaveMaxClamp?.Invoke();
            }
        }
    }

    private void HandleIdleSway()
    {
        swayTimer += Time.deltaTime * swayFrequency;

        float swayY = Mathf.Sin(swayTimer) * swayAmplitude * 0.01f;
        float swayX = Mathf.Cos(swayTimer * 0.5f) * swayAmplitude * 0.01f;

        transform.localPosition = swayBasePos + new Vector3(swayX, swayY, 0f);
    }
    
    public IEnumerator MoveRoutine(Vector3 targetPos, Vector3 targetRot, float duration)
    {
        enableControl = false;
        Vector3 startPos = gameObject.transform.position;
        Quaternion startRot = gameObject.transform.rotation;

        Quaternion endRot = Quaternion.Euler(targetRot);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // smooth step curve
            float smoothT = t * t * (3f - 2f * t);

            swayBasePos = Vector3.Lerp(startPos, targetPos, smoothT);
            gameObject.transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            gameObject.transform.rotation = Quaternion.Slerp(startRot, endRot, smoothT);

            yield return null;
        }

        // ensure exact final values because fuck why would it just go to where i said to go
        swayBasePos = targetPos;
        gameObject.transform.position = targetPos;
        gameObject.transform.rotation = endRot;
        enableControl = true;
    }
}
