using System.Collections.Generic;
using UnityEngine;

/// @file BackgroundController.cs
/// @brief Runtime controller for backdrop and ground colour presets.
/// @author Roberto Charreton
/// @date 2026

/// @class BackgroundPreset
/// @brief Serialisable data record that defines one studio backdrop look.
///
/// Each preset drives two renderers through MaterialPropertyBlock overrides:
/// - The **backdrop quad**, which uses the custom GradientBackground shader.
/// - The **ground quad**, which uses HDRP/Lit.
[System.Serializable]
public class BackgroundPreset
{
    /// @brief Human-readable name shown in the on-screen UI (e.g. "Dark Studio").
    public string presetName = "Dark Studio";

    /// @brief Top gradient colour fed into the GradientBackground shader's @c _TopColor.
    public Color topColor = new(0.06f, 0.06f, 0.18f, 1f);

    /// @brief Bottom gradient colour fed into @c _BottomColor.
    public Color bottomColor = new(0.01f, 0.01f, 0.04f, 1f);

    /// @brief Radial vignette intensity fed into @c _VignetteAmount (0 = off, 4 = heavy).
    public float vignette = 1.2f;

    /// @brief Tint applied to the ground plane's @c _BaseColor.
    public Color groundTint = new(0.10f, 0.10f, 0.12f, 1f);
}

/// @class BackgroundController
/// @brief Cycles through BackgroundPreset entries by writing to MaterialPropertyBlocks.
///
/// Using MaterialPropertyBlock means the shared Material assets are **never
/// modified**, so changes are not serialised to disk and do not break prefabs.
///
/// @par Scene wiring
/// ProductVisualizerSetup assigns @ref backdropRenderer and @ref groundRenderer
/// automatically. Add further presets in the Inspector at any time; they become
/// available immediately in Play mode.
///
/// @par Preset list (defaults)
/// | # | Name           | Mood                     |
/// |---|----------------|--------------------------|
/// | 0 | Dark Studio    | Classic product render    |
/// | 1 | Warm Amber     | Cozy / sunset             |
/// | 2 | Deep Space     | Dark sci-fi               |
/// | 3 | Emerald Night  | Nature / energy drink     |
public class BackgroundController : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Inspector fields
    // ------------------------------------------------------------------

    /// @name References
    /// @{

    /// @brief Renderer of the large backdrop quad (uses GradientBackground shader).
    [Header("References")]
    public Renderer backdropRenderer;

    /// @brief Renderer of the ground plane quad (uses HDRP/Lit).
    public Renderer groundRenderer;

    /// @}

    /// @brief Ordered list of available studio presets.
    [Header("Presets")]
    public List<BackgroundPreset> presets = new();

    // ------------------------------------------------------------------
    // Events
    // ------------------------------------------------------------------

    /// @brief Fired after a preset change with <c>(newIndex, newPreset)</c>.
    public System.Action<int, BackgroundPreset> OnPresetChanged;

    // ------------------------------------------------------------------
    // Private state
    // ------------------------------------------------------------------

    private int _currentIndex;
    private MaterialPropertyBlock _mpb;

    // ------------------------------------------------------------------
    // Unity lifecycle
    // ------------------------------------------------------------------

    void Awake() => _mpb = new MaterialPropertyBlock();

    void Start()
    {
        if (presets.Count > 0) Apply(0);
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    /// @brief Advances to the next preset, wrapping around the list.
    public void NextPreset()
    {
        _currentIndex = (_currentIndex + 1) % presets.Count;
        Apply(_currentIndex);
        OnPresetChanged?.Invoke(_currentIndex, presets[_currentIndex]);
    }

    /// @brief Jumps directly to a preset by index.
    /// @param index Zero-based index into @ref presets. Out-of-range values are ignored.
    public void SetPreset(int index)
    {
        if (index < 0 || index >= presets.Count) return;
        _currentIndex = index;
        Apply(index);
        OnPresetChanged?.Invoke(_currentIndex, presets[_currentIndex]);
    }

    /// @brief The currently active BackgroundPreset, or @c null if the list is empty.
    public BackgroundPreset CurrentPreset     => presets.Count > 0 ? presets[_currentIndex] : null;

    /// @brief Zero-based index of the active preset.
    public int              CurrentIndex      => _currentIndex;

    /// @brief Display name of the active preset (empty string if none).
    public string           CurrentPresetName => CurrentPreset?.presetName ?? "";

    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    /// @brief Pushes the preset's values to the backdrop and ground via MaterialPropertyBlock.
    /// @param index Index into @ref presets.
    void Apply(int index)
    {
        BackgroundPreset p = presets[index];

        if (backdropRenderer != null)
        {
            backdropRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_TopColor",       p.topColor);
            _mpb.SetColor("_BottomColor",    p.bottomColor);
            _mpb.SetFloat("_VignetteAmount", p.vignette);
            backdropRenderer.SetPropertyBlock(_mpb);
        }

        if (groundRenderer != null)
        {
            groundRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", p.groundTint);
            groundRenderer.SetPropertyBlock(_mpb);
        }
    }
}
