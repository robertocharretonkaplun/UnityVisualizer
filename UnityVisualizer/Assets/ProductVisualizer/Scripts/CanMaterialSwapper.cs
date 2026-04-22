using System.Collections.Generic;
using UnityEngine;

/// @file CanMaterialSwapper.cs
/// @brief Full material-set swapper for the two-slot soda can FBX.
/// @author Roberto Charreton
/// @date 2026

/// @class CanVariant
/// @brief Data container that pairs a brand label material with an aluminium cap material.
///
/// Instances of this class are authored in the Inspector or populated at
/// edit-time by ProductVisualizerSetup. Each variant corresponds to one
/// physical brand (Coca-Cola, Pepsi, etc.).
[System.Serializable]
public class CanVariant
{
    /// @brief Human-readable brand name shown in the on-screen UI.
    public string   variantName;

    /// @brief HDRP/Lit material applied to slot 0 (the printable label area).
    /// @details Typically has @c _BaseColorMap set to the brand artwork texture.
    public Material labelMaterial;

    /// @brief HDRP/Lit material applied to slot 1 (the aluminium top and bottom caps).
    /// @details Shared across all variants — high metallic, high smoothness.
    public Material capMaterial;
}

/// @class CanMaterialSwapper
/// @brief Cycles through brand variants by replacing the full material array on the can renderer.
///
/// The soda can FBX (Resources/Prefabs/Can) exposes exactly two material slots:
/// | Slot | Region         | Changed per variant? |
/// |------|----------------|----------------------|
/// | 0    | Label / body   | Yes                  |
/// | 1    | Aluminium caps | No (shared)          |
///
/// @par Why material swap instead of MaterialPropertyBlock?
/// The brand textures require entirely separate material assets because
/// @c _BaseColorMap cannot be overridden per-renderer without creating a
/// material instance anyway. A full swap is simpler and equally performant
/// for a single-object visualizer.
///
/// @par Integration
/// Wire @ref canRenderer in the Inspector (or let Start() auto-detect it),
/// then populate @ref variants. VisualizerUI calls NextVariant() / PreviousVariant()
/// in response to UI button presses.
public class CanMaterialSwapper : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Inspector fields
    // ------------------------------------------------------------------

    /// @brief The Renderer whose material array will be swapped.
    /// @details Auto-detected via GetComponentInChildren if left null.
    [Header("Can Renderer")]
    public Renderer canRenderer;

    /// @brief Ordered list of brand variants available in the visualizer.
    /// @details Populated automatically by ProductVisualizerSetup from the
    ///          textures found in <c>Assets/Resources/textures/</c>.
    [Header("Variants (one per brand)")]
    public List<CanVariant> variants = new();

    // ------------------------------------------------------------------
    // Events
    // ------------------------------------------------------------------

    /// @brief Fired after a variant change with <c>(newIndex, newVariant)</c>.
    /// @details Subscribe in VisualizerUI to update on-screen labels.
    public System.Action<int, CanVariant> OnVariantChanged;

    // ------------------------------------------------------------------
    // Private state
    // ------------------------------------------------------------------

    private int _currentIndex;

    // ------------------------------------------------------------------
    // Unity lifecycle
    // ------------------------------------------------------------------

    void Start()
    {
        if (canRenderer == null)
            canRenderer = GetComponentInChildren<Renderer>();

        if (variants.Count > 0) Apply(0);
    }

    // ------------------------------------------------------------------
    // Public API
    // ------------------------------------------------------------------

    /// @brief Advances to the next variant, wrapping around the list.
    public void NextVariant()
    {
        _currentIndex = (_currentIndex + 1) % variants.Count;
        Apply(_currentIndex);
        OnVariantChanged?.Invoke(_currentIndex, variants[_currentIndex]);
    }

    /// @brief Steps back to the previous variant, wrapping around the list.
    public void PreviousVariant()
    {
        _currentIndex = (_currentIndex - 1 + variants.Count) % variants.Count;
        Apply(_currentIndex);
        OnVariantChanged?.Invoke(_currentIndex, variants[_currentIndex]);
    }

    /// @brief Jumps directly to a variant by index.
    /// @param index Zero-based index into @ref variants. Out-of-range values are ignored.
    public void SetVariantIndex(int index)
    {
        if (index < 0 || index >= variants.Count) return;
        _currentIndex = index;
        Apply(index);
        OnVariantChanged?.Invoke(_currentIndex, variants[_currentIndex]);
    }

    /// @brief The currently active CanVariant, or @c null if the list is empty.
    public CanVariant CurrentVariant     => variants.Count > 0 ? variants[_currentIndex] : null;

    /// @brief Zero-based index of the active variant.
    public int        CurrentIndex       => _currentIndex;

    /// @brief Total number of registered variants.
    public int        VariantCount       => variants.Count;

    /// @brief Display name of the active variant (empty string if none).
    public string     CurrentVariantName => CurrentVariant?.variantName ?? "";

    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    /// @brief Pushes the material array of the given variant to @ref canRenderer.
    /// @param index Index into @ref variants.
    void Apply(int index)
    {
        if (canRenderer == null || index >= variants.Count) return;
        CanVariant v = variants[index];
        canRenderer.materials = new[] { v.labelMaterial, v.capMaterial };
    }
}
