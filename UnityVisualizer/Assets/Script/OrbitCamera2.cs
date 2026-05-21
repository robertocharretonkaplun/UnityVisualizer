using UnityEngine;
using UnityEngine.InputSystem;
public class OrbitCamera2 : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Orbit")]
    public float distance = 3f;
    public float autoRotateSpeed = 20f;
    public bool autoRotate = true;

    [Header("Mouse Rotation")]
    public float mouseSensitivity = 200f;
    public float minVerticalAngle = -20f;
    public float maxVerticalAngle = 70f;

    [Header("Zoom")]
    public float scrollSensitivity = 5f;
    public float minDistance = 1.5f;
    public float maxDistance = 8f;

    private float _yaw;
    private float _pitch = 20f;
    private bool _isDragging;
    private Vector2 _lastMousePos;

    private void Start() => UpdatePosition();

    private void LateUpdate()
    {
        HandleMouseInput();

        if (autoRotate && !_isDragging)
        {
            _yaw += autoRotateSpeed * Time.deltaTime;
        }

        UpdatePosition();
    }

    private void HandleMouseInput()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
            return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            _isDragging = true;
            _lastMousePos = mouse.position.ReadValue();
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            _isDragging = false;
        }

        if (_isDragging)
        {
            Vector2 currentMousePos = mouse.position.ReadValue();
            Vector2 delta = currentMousePos - _lastMousePos;

            _yaw += delta.x * mouseSensitivity * Time.deltaTime;
            _pitch -= delta.y * mouseSensitivity * Time.deltaTime;

            _pitch = Mathf.Clamp(_pitch, minVerticalAngle, maxVerticalAngle);

            _lastMousePos = currentMousePos;
        }

        float scroll = mouse.scroll.ReadValue().y;

        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * scrollSensitivity * 0.01f;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }

    private void UpdatePosition()
    {
        if (target == null)
            return;

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        transform.position = target.position + rotation * (Vector3.back * distance);
        transform.LookAt(target);
    }

    public void ToggleAutoRotate() => autoRotate = !autoRotate;

    public bool IsAutoRotating => autoRotate;

    public void ResetView()
    {
        _yaw = 0f;
        _pitch = 20f;
        distance = 3f;

        UpdatePosition();
    }
}