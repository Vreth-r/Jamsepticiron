using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIRaycastDebugger : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionReference clickAction;
    public InputActionReference pointerPosAction;

    [Header("References")]
    public Camera eventCamera; // The camera used for world-space UI raycasts
    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;

    private void OnEnable()
    {
        if (clickAction != null)
            clickAction.action.performed += OnClick;

        clickAction?.action.Enable();
        pointerPosAction?.action.Enable();
    }

    private void OnDisable()
    {
        if (clickAction != null)
            clickAction.action.performed -= OnClick;

        clickAction?.action.Disable();
        pointerPosAction?.action.Disable();
    }

    private void Start()
    {
        if (raycaster == null)
            raycaster = FindFirstObjectByType<GraphicRaycaster>();

        if (eventSystem == null)
            eventSystem = FindFirstObjectByType<EventSystem>();

        if (eventCamera == null)
            eventCamera = Camera.main;

        // Make sure the raycaster has the correct event camera
        //if (raycaster != null && raycaster.eventCamera == null)
           // raycaster.eventCamera = eventCamera;
    }

    private void OnClick(InputAction.CallbackContext ctx)
    {
        if (raycaster == null || eventSystem == null)
        {
            Debug.LogWarning("Missing raycaster or event system.");
            return;
        }

        Vector2 screenPos = pointerPosAction?.action.ReadValue<Vector2>() ?? Vector2.zero;

        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = screenPos
        };

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);

        if (results.Count == 0)
        {
            Debug.Log($"[UIRaycastDebugger] No UI elements hit at {screenPos}");
        }
        else
        {
            foreach (var result in results)
            {
                Debug.Log($"[UIRaycastDebugger] Hit: {result.gameObject.name} (Layer: {LayerMask.LayerToName(result.gameObject.layer)}) Canvas: {result.module.name}");
            }
        }
    }
}
