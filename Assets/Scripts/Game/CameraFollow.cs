using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Smooth camera follow for Vesper with mouse scroll zoom.
/// Stays within arena bounds. Replaces the old CameraPan approach
/// during gameplay while keeping the map centered and properly visible.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    private Transform target;
    private Camera cam;

    [Header("Follow Settings")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 3f, -10f);

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 18f;

    // Arena bounds
    private float arenaWidth;
    private float arenaHeight;

    public void Setup(Transform followTarget, float width, float height)
    {
        target = followTarget;
        arenaWidth = width;
        arenaHeight = height;
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (target == null || cam == null) return;

        // Smooth follow
        Vector3 desiredPosition = target.position + offset;

        // Clamp to arena bounds — allow camera to see the full arena width
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        // Don't clamp X too tightly — the arena is narrow, let camera center
        float maxX = Mathf.Max(0, arenaWidth / 2f - halfWidth);
        float clampedX = Mathf.Clamp(desiredPosition.x, -maxX, maxX);
        float clampedY = Mathf.Clamp(desiredPosition.y, -2f + halfHeight, arenaHeight + 2f - halfHeight);
        desiredPosition = new Vector3(clampedX, clampedY, offset.z);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Mouse scroll zoom
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                cam.orthographicSize -= scroll * zoomSpeed * Time.deltaTime;
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
            }
        }
    }
}
