using UnityEngine;

/// @file ProductFloatAnimation.cs
/// @brief Idle floating and tilting animation for the displayed product.
/// @author Roberto Charreton
/// @date 2026

/// @class ProductFloatAnimation
/// @brief Animates a GameObject with a gentle sinusoidal float and dual-axis tilt.
///
/// The animation runs entirely in @c Update() via @c Mathf.Sin / @c Mathf.Cos,
/// requiring no Animator or AnimationClip asset. A random phase offset is
/// assigned at Start so multiple products in the same scene do not move in sync.
///
/// @par Formula
/// @code
/// Y offset = sin(time * floatSpeed) * floatAmplitude
/// Pitch    = sin(time * tiltSpeed * 0.7) * tiltAmplitude
/// Roll     = cos(time * tiltSpeed)       * tiltAmplitude * 0.5
/// @endcode
///
/// @par Tuning tips
/// - Keep @ref floatAmplitude below 0.1 for a subtle, premium feel.
/// - Reduce @ref tiltAmplitude to 0 if the product should stay perfectly upright.
/// - Both @ref floatSpeed and @ref tiltSpeed are independent so the movement
///   never fully repeats, avoiding a mechanical look.
public class ProductFloatAnimation : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Inspector fields
    // ------------------------------------------------------------------

    /// @name Float
    /// @{

    /// @brief Peak vertical displacement from the rest position (metres).
    [Header("Float")]
    public float floatAmplitude = 0.05f;

    /// @brief Cycles per second of the vertical sine wave.
    public float floatSpeed = 1.2f;

    /// @}

    /// @name Tilt
    /// @{

    /// @brief Peak rotation on X and Z axes (degrees).
    [Header("Tilt")]
    public float tiltAmplitude = 2f;

    /// @brief Base frequency of the tilt oscillation. Z-axis uses this value,
    ///        X-axis uses 70% of it to avoid synchronised movement.
    public float tiltSpeed = 0.8f;

    /// @}

    // ------------------------------------------------------------------
    // Private state
    // ------------------------------------------------------------------

    private Vector3 _startPos; ///< @brief World position captured at Start.
    private float   _offset;   ///< @brief Random phase offset in radians.

    // ------------------------------------------------------------------
    // Unity lifecycle
    // ------------------------------------------------------------------

    void Start()
    {
        _startPos = transform.localPosition;
        _offset   = Random.Range(0f, Mathf.PI * 2f);
    }

    /// @brief Advances the float and tilt animation each frame.
    void Update()
    {
        float t = Time.time + _offset;

        transform.localPosition = _startPos + Vector3.up * (Mathf.Sin(t * floatSpeed) * floatAmplitude);

        float tiltX = Mathf.Sin(t * tiltSpeed * 0.7f) * tiltAmplitude;
        float tiltZ = Mathf.Cos(t * tiltSpeed)        * tiltAmplitude * 0.5f;
        transform.localRotation = Quaternion.Euler(tiltX, 0f, tiltZ);
    }
}
