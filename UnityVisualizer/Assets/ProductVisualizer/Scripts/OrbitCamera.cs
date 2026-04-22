using UnityEngine;

/// @file OrbitCamera.cs
/// @brief Orbital camera controller for the product visualizer.
/// @author Roberto Charreton
/// @date 2026

/// @class OrbitCamera
/// @brief Spherical-coordinate camera that orbits around a target Transform.
///
/// Supports three interaction modes:
/// - **Auto-rotation**: constant yaw increment each frame (idle showcase).
/// - **Mouse drag**: left-button drag overrides auto-rotation while held.
/// - **Scroll zoom**: mouse wheel adjusts the orbital radius.
///
/// Attach to the Main Camera. Assign @ref target in the Inspector or via
/// ProductVisualizerSetup, which creates a dedicated *Camera Target* object
/// at the product's centre of mass.
///
/// @par Usage example
/// @code
/// // Toggle auto-rotation from UI
/// GetComponent<OrbitCamera>().ToggleAutoRotate();
///
/// // Reset to default framing
/// GetComponent<OrbitCamera>().ResetView();
/// @endcode
public class OrbitCamera : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Inspector fields
    // ------------------------------------------------------------------

    /// @name Target
    /// @{

    /// @brief The point the camera orbits around.
    /// @details Typically the geometric centre of the displayed product.
    ///          Created automatically by ProductVisualizerSetup.
    public Transform target;

    /// @brief Current distance from target in world units.
    /// @details Clamped at runtime between @ref minDistance and @ref maxDistance.
    public float distance = 3f;

    /// @}

    /// @name Auto Rotation
    /// @{

    /// @brief Degrees per second of automatic yaw rotation.
    public float autoRotateSpeed = 20f;

    /// @brief Whether the camera is currently auto-rotating.
    /// @details Set to @c false while the user drags; restored on mouse-up.
    public bool autoRotate = true;

    /// @}

    /// @name Manual Orbit
    /// @{

    /// @brief Mouse sensitivity in degrees per pixel-second.
    public float mouseSensitivity = 200f;

    /// @brief Minimum pitch (vertical) angle in degrees.
    public float minVerticalAngle = -20f;

    /// @brief Maximum pitch (vertical) angle in degrees.
    public float maxVerticalAngle = 70f;

    /// @}

    /// @name Zoom
    /// @{

    /// @brief Scroll-wheel zoom sensitivity (world units per scroll unit).
    public float scrollSensitivity = 5f;

    /// @brief Closest allowed distance to the target.
    public float minDistance = 1.5f;

    /// @brief Farthest allowed distance from the target.
    public float maxDistance = 8f;

    /// @}

    // ------------------------------------------------------------------
    // Private state
    // ------------------------------------------------------------------

    private float   _yaw;             ///< @brief Current horizontal angle (degrees).
    private float   _pitch = 20f;     ///< @brief Current vertical angle (degrees).
    private bool    _isDragging;      ///< @brief True while left mouse button is held.
    private Vector2 _lastMousePos;    ///< @brief Mouse position on the previous frame.

    // ------------------------------------------------------------------
    // Unity lifecycle
    // ------------------------------------------------------------------

    void Start() => UpdatePosition();

    /// @brief Processes input then repositions the camera every frame.
    /// @details Runs in LateUpdate so it executes after any animation
    ///          that might move the target Transform.
    void LateUpdate()
    {
        HandleMouseInput();

        if (autoRotate && !_isDragging)
            _yaw += autoRotateSpeed * Time.deltaTime;

        UpdatePosition();
    }

    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    /// @brief Reads mouse drag and scroll-wheel input and updates yaw/pitch/distance.
    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _isDragging   = true;
            _lastMousePos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
            _isDragging = false;

        if (_isDragging)
        {
            Vector2 delta = (Vector2)Input.mousePosition - _lastMousePos;
            _yaw   += delta.x * mouseSensitivity * Time.deltaTime;
            _pitch -= delta.y * mouseSensitivity * Time.deltaTime;
            _pitch  = Mathf.Clamp(_pitch, minVerticalAngle, maxVerticalAngle);
            _lastMousePos = Input.mousePosition;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            distance -= scroll * scrollSensitivity;
            distance  = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }

    /// @brief Moves the camera to the spherical coordinate defined by yaw/pitch/distance.
    void UpdatePosition()
    {
        if (target == null) return;
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.position  = target.position + rotation * (Vector3.back * distance);
        transform.LookAt(target.position);
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    /// @brief Flips the auto-rotation state.
    /// @details Called by VisualizerUI when the user presses the Auto-Rotate button.
    public void ToggleAutoRotate() => autoRotate = !autoRotate;

    /// @brief Returns @c true when auto-rotation is active.
    public bool IsAutoRotating => autoRotate;

    /// @brief Resets yaw, pitch and distance to their default showcase values.
    public void ResetView()
    {
        _yaw     = 0f;
        _pitch   = 20f;
        distance = 3f;
    }
}
