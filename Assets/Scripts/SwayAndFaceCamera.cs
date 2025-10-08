using UnityEngine;

[RequireComponent(typeof(Transform))]
public class SwayAndFaceCamera : MonoBehaviour
{
    [Header("Sway Settings")]
    [Tooltip("Amount of sway in local units (pixels for UI)")]
    public float swayAmplitude = 3f;

    [Tooltip("Speed of sway (radians per second)")]
    public float swaySpeed = 2f;

    [Header("Camera Facing")]
    [Tooltip("Use main camera for facing")]
    public Camera targetCamera;

    private Vector3 baseLocalPosition;
    public bool working = false;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        //baseLocalPosition = transform.localPosition;
    }

    public void SetBasePos(Vector3 pos)
    {
        baseLocalPosition = pos;
    }

    private void Update()
    {
        if (!working) return;
        DoActions();
    }

    public void DoActions()
    {
        if (targetCamera == null) return;

        // --- 1) Face camera ---
        transform.forward = targetCamera.transform.forward;

        // --- 2) Sway ---
        float swayX = Mathf.Sin(Time.time * swaySpeed + GetInstanceID()) * swayAmplitude;
        float swayY = Mathf.Cos(Time.time * swaySpeed + GetInstanceID() * 1.5f) * swayAmplitude;

        transform.localPosition = baseLocalPosition + new Vector3(swayX, swayY, 0f);
    }

    /// <summary>
    /// Reset the base position, useful if the parent moves dynamically.
    /// </summary>
    public void ResetBasePosition()
    {
        baseLocalPosition = transform.localPosition;
    }
}
