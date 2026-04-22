using UnityEngine;

public class ProductFloatAnimation : MonoBehaviour
{
    [Header("Float")]
    public float floatAmplitude = 0.05f;
    public float floatSpeed     = 1.2f;

    [Header("Tilt")]
    public float tiltAmplitude = 2f;
    public float tiltSpeed     = 0.8f;

    private Vector3 _startPos;
    private float   _offset;

    void Start()
    {
        _startPos = transform.localPosition;
        _offset   = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float t = Time.time + _offset;

        transform.localPosition = _startPos + Vector3.up * (Mathf.Sin(t * floatSpeed) * floatAmplitude);

        float tiltX = Mathf.Sin(t * tiltSpeed * 0.7f) * tiltAmplitude;
        float tiltZ = Mathf.Cos(t * tiltSpeed)        * tiltAmplitude * 0.5f;
        transform.localRotation = Quaternion.Euler(tiltX, 0f, tiltZ);
    }
}
