using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public float distance = 3f;

    [Header("Auto Rotation")]
    public float autoRotateSpeed = 20f;
    public bool autoRotate = true;

    [Header("Manual Orbit")]
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

    void Start() => UpdatePosition();

    void LateUpdate()
    {
        HandleMouseInput();

        if (autoRotate && !_isDragging)
            _yaw += autoRotateSpeed * Time.deltaTime;

        UpdatePosition();
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _isDragging = true;
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

    void UpdatePosition()
    {
        if (target == null) return;
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        transform.position = target.position + rotation * (Vector3.back * distance);
        transform.LookAt(target.position);
    }

    public void ToggleAutoRotate() => autoRotate = !autoRotate;
    public bool IsAutoRotating => autoRotate;

    public void ResetView()
    {
        _yaw   = 0f;
        _pitch = 20f;
        distance = 3f;
    }
}
