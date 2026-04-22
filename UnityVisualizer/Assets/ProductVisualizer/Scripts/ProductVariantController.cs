using System.Collections.Generic;
using UnityEngine;

/// @file ProductVariantController.cs
/// @brief Colour-based variant system for primitive or custom mesh products.
/// @author Roberto Charreton
/// @date 2026

/// @class ProductVariant
/// @brief Serialisable PBR colour preset applied through MaterialPropertyBlock.
///
/// No new Material asset is created per variant; instead the values are pushed
/// directly to the GPU via Renderer.SetPropertyBlock, keeping the material
/// list in the project clean and avoiding unnecessary asset duplication.
[System.Serializable]
public class ProductVariant
{
    /// @brief Human-readable name displayed in the UI (e.g. "Ocean Blue").
    public string variantName = "Default";

    /// @brief Albedo colour mapped to the HDRP @c _BaseColor property.
    public Color baseColor = Color.white;

    /// @brief HDR emissive colour mapped to @c _EmissiveColor.
    /// @details Use the HDR picker (enabled by @c ColorUsage) to set intensity
    ///          in nits. Leave black to disable emission.
    [ColorUsage(false, true)]
    public Color emissiveColor = Color.black;

    /// @brief Metallic value in [0, 1] mapped to @c _Metallic.
    [Range(0f, 1f)] public float metallic = 0.9f;

    /// @brief Smoothness (gloss) value in [0, 1] mapped to @c _Smoothness.
    [Range(0f, 1f)] public float smoothness = 0.85f;
}

/// @class ProductVariantController
/// @brief Applies ProductVariant presets to a set of Renderers using MaterialPropertyBlock.
///
/// This component is the colour-only counterpart to CanMaterialSwapper.
/// Use it when the product mesh does not require texture swapping — for example,
/// to quickly prototype colour options without authoring separate material assets.
///
/// @par MaterialPropertyBlock vs. material swap
/// MaterialPropertyBlock writes properties to the GPU without creating a new
/// Material instance, so all renderers in @ref bodyRenderers share the same
/// draw-call state. This is the recommended approach for single-object
/// colour variation in Unity.
///
/// @see CanMaterialSwapper for texture-based brand swapping.
public class ProductVariantController : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Inspector fields
    // ------------------------------------------------------------------

    /// @brief Renderers whose material properties will be overridden.
    /// @details Typically the main body mesh(es) of the product.
    [Header("Body Renderers (color changes)")]
    public List<Renderer> bodyRenderers = new();

    /// @brief Ordered list of PBR colour presets available in the visualizer.
    [Header("Variants")]
    public List<ProductVariant> variants = new();

    // ------------------------------------------------------------------
    // Events
    // ------------------------------------------------------------------

    /// @brief Fired after a variant change with <c>(newIndex, newVariant)</c>.
    public System.Action<int, ProductVariant> OnVariantChanged;

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

    /// @brief The currently active ProductVariant, or @c null if the list is empty.
    public ProductVariant CurrentVariant     => variants.Count > 0 ? variants[_currentIndex] : null;

    /// @brief Zero-based index of the active variant.
    public int            CurrentIndex       => _currentIndex;

    /// @brief Total number of registered variants.
    public int            VariantCount       => variants.Count;

    /// @brief Display name of the active variant (empty string if none).
    public string         CurrentVariantName => CurrentVariant?.variantName ?? "";

    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    /// @brief Pushes the variant's PBR values to every renderer via MaterialPropertyBlock.
    /// @param index Index into @ref variants.
    void Apply(int index)
    {
        ProductVariant v = variants[index];
        foreach (Renderer r in bodyRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor",     v.baseColor);
            _mpb.SetFloat("_Metallic",      v.metallic);
            _mpb.SetFloat("_Smoothness",    v.smoothness);
            _mpb.SetColor("_EmissiveColor", v.emissiveColor);
            r.SetPropertyBlock(_mpb);
        }
    }
}
