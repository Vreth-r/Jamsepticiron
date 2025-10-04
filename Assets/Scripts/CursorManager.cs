using UnityEngine;
using UnityEngine.InputSystem; // 👈 new input system

public class CursorManager : MonoBehaviour
{
    [Header("Cursor Settings")]
    [Tooltip("Optional custom cursor texture.")]
    public Texture2D customCursor;

    [Tooltip("Hotspot offset from the top-left corner of the cursor texture.")]
    public Vector2 cursorHotspot = Vector2.zero;

    [Tooltip("Cursor mode for setting custom cursor.")]
    public CursorMode cursorMode = CursorMode.Auto;

    [Header("Input")]
    [Tooltip("Input action reference for toggling the cursor.")]
    public InputActionReference toggleCursorAction;

    private bool cursorVisible = true;

    private void OnEnable()
    {
        // Subscribe to the input action if assigned
        if (toggleCursorAction != null)
        {
            toggleCursorAction.action.performed += OnToggleCursor;
            toggleCursorAction.action.Enable();
        }

        ApplyCursor();
    }

    private void OnDisable()
    {
        if (toggleCursorAction != null)
        {
            toggleCursorAction.action.performed -= OnToggleCursor;
            toggleCursorAction.action.Disable();
        }
    }

    private void OnToggleCursor(InputAction.CallbackContext ctx)
    {
        ToggleCursor();
    }

    public void ToggleCursor()
    {
        cursorVisible = !cursorVisible;
        ApplyCursor();
    }

    private void ApplyCursor()
    {
        if (cursorVisible)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (customCursor != null)
                Cursor.SetCursor(customCursor, cursorHotspot, cursorMode);
            else
                Cursor.SetCursor(null, Vector2.zero, cursorMode);
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void EnableCursor()
    {
        cursorVisible = true;
        ApplyCursor();
    }

    public void DisableCursor()
    {
        cursorVisible = false;
        ApplyCursor();
    }
}
