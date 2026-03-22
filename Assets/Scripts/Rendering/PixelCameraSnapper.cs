using UnityEngine;

/// <summary>
/// Snaps camera position to the pixel grid each LateUpdate, eliminating
/// sub-pixel shimmer caused by fractional camera movement.
///
/// Supports both Orthographic and Perspective cameras.
/// Projects movement onto the camera's local right/up axes, so any camera
/// orientation (top-down, side-scroller, angled) is handled correctly.
///
/// [DefaultExecutionOrder(10000)] ensures this runs after all camera-follow
/// scripts, preventing them from overwriting the snapped position.
/// </summary>
[DefaultExecutionOrder(10000)]
[RequireComponent(typeof(Camera))]
public class PixelCameraSnapper : MonoBehaviour
{
    [Tooltip("Must match Pixel Width in PixelRenderFeature settings.")]
    [SerializeField, Min(1)] private int pixelWidth = 160;

    [Tooltip("Must match Pixel Height in PixelRenderFeature settings.")]
    [SerializeField, Min(1)] private int pixelHeight = 90;

    [Tooltip("Perspective camera only. Reference depth from camera to the ground plane.")]
    [SerializeField] private float referenceDepth = 25f;

    private Camera  _camera;
    private Vector3 _unsnappedPosition;

    private void Awake()
    {
        _camera            = GetComponent<Camera>();
        _unsnappedPosition = transform.position;
    }

    private void LateUpdate()
    {
        _unsnappedPosition = transform.position;
        SnapToPixelGrid();
    }

    /// <summary>Snaps the camera's world position to the pixel grid.</summary>
    private void SnapToPixelGrid()
    {
        float pixelSize = GetPixelWorldSize();
        if (pixelSize <= 0f) return;

        Vector3 pos   = _unsnappedPosition;
        Vector3 right = transform.right;
        Vector3 up    = transform.up;

        // Project position onto camera's local axes, snap, then reapply.
        // Using camera axes (not world X/Y) makes this work for any orientation.
        float dotRight = Vector3.Dot(pos, right);
        float dotUp    = Vector3.Dot(pos, up);

        float snappedRight = Mathf.Round(dotRight / pixelSize) * pixelSize;
        float snappedUp    = Mathf.Round(dotUp    / pixelSize) * pixelSize;

        transform.position = pos
            + right * (snappedRight - dotRight)
            + up    * (snappedUp    - dotUp);
    }

    /// <summary>Returns the world-space size of one pixel for the current camera settings.</summary>
    private float GetPixelWorldSize()
    {
        if (_camera.orthographic)
        {
            return _camera.orthographicSize * 2f / pixelHeight;
        }
        else
        {
            float halfFovRad  = _camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float worldHeight = 2f * referenceDepth * Mathf.Tan(halfFovRad);
            return worldHeight / pixelHeight;
        }
    }

    /// <summary>The unsnapped logical position. Use this for gameplay logic, not transform.position.</summary>
    public Vector3 UnsnappedPosition => _unsnappedPosition;
}
