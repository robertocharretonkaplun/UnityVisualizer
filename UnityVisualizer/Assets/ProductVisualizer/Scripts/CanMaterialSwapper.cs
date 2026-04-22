using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CanVariant
{
    public string   variantName;
    public Material labelMaterial;  // material slot 0 – can body / label artwork
    public Material capMaterial;    // material slot 1 – aluminium caps
}

/// <summary>
/// Swaps the full material set on the can's MeshRenderer.
/// Designed for the FBX model at Resources/Prefabs/Can which has exactly 2 material slots.
/// </summary>
public class CanMaterialSwapper : MonoBehaviour
{
    [Header("Can Renderer")]
    public Renderer canRenderer;

    [Header("Variants (one per brand)")]
    public List<CanVariant> variants = new();

    public System.Action<int, CanVariant> OnVariantChanged;

    private int _currentIndex;

    void Start()
    {
        if (canRenderer == null)
            canRenderer = GetComponentInChildren<Renderer>();

        if (variants.Count > 0) Apply(0);
    }

    public void NextVariant()
    {
        _currentIndex = (_currentIndex + 1) % variants.Count;
        Apply(_currentIndex);
        OnVariantChanged?.Invoke(_currentIndex, variants[_currentIndex]);
    }

    public void PreviousVariant()
    {
        _currentIndex = (_currentIndex - 1 + variants.Count) % variants.Count;
        Apply(_currentIndex);
        OnVariantChanged?.Invoke(_currentIndex, variants[_currentIndex]);
    }

    public void SetVariantIndex(int index)
    {
        if (index < 0 || index >= variants.Count) return;
        _currentIndex = index;
        Apply(index);
        OnVariantChanged?.Invoke(_currentIndex, variants[_currentIndex]);
    }

    void Apply(int index)
    {
        if (canRenderer == null || index >= variants.Count) return;
        CanVariant v = variants[index];
        canRenderer.materials = new[] { v.labelMaterial, v.capMaterial };
    }

    public CanVariant CurrentVariant     => variants.Count > 0 ? variants[_currentIndex] : null;
    public int        CurrentIndex       => _currentIndex;
    public int        VariantCount       => variants.Count;
    public string     CurrentVariantName => CurrentVariant?.variantName ?? "";
}
