using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple mouse-drag camera panning for testing the tower map.
/// Left-click and drag to pan the camera up/down/left/right.
/// Scroll wheel to zoom in/out.
/// Uses the new Input System package.
/// </summary>
public class CameraPan : MonoBehaviour
{
    [Header("Pan Settings")]
    [SerializeField] private float panSpeed = 2f;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 25f;

    [Header("Arena Boundaries")]
    [SerializeField] private float minX = -12f;
    [SerializeField] private float maxX = 12f;
    [SerializeField] private float minY = -2f;
    [SerializeField] private float maxY = 42f;

    private Camera cam;
    private Vector2 lastMousePosition;
    private bool isDragging;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        HandlePan();
        HandleZoom();
    }

    private void HandlePan()
    {
        var mouse = Mouse.current;

        // Start drag on left mouse button press
        if (mouse.leftButton.wasPressedThisFrame)
        {
            isDragging = true;
            lastMousePosition = mouse.position.ReadValue();
        }

        // Stop drag on left mouse button release
        if (mouse.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }

        // Pan while dragging
        if (isDragging && mouse.leftButton.isPressed)
        {
            Vector2 currentMousePosition = mouse.position.ReadValue();
            Vector2 delta = currentMousePosition - lastMousePosition;
            lastMousePosition = currentMousePosition;

            // Convert screen delta to world delta
            float worldDeltaX = -delta.x * panSpeed * cam.orthographicSize / Screen.height;
            float worldDeltaY = -delta.y * panSpeed * cam.orthographicSize / Screen.height;

            Vector3 newPos = transform.position;
            newPos.x += worldDeltaX;
            newPos.y += worldDeltaY;

            // Clamp to boundaries
            newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
            newPos.y = Mathf.Clamp(newPos.y, minY, maxY);

            transform.position = newPos;
        }
    }

    private void HandleZoom()
    {
        var mouse = Mouse.current;
        float scroll = mouse.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) > 0.01f)
        {
            cam.orthographicSize -= scroll * zoomSpeed * Time.deltaTime;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
    }
}
