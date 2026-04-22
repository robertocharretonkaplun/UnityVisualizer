using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ProductVariant
{
    public string variantName = "Default";
    public Color  baseColor   = Color.white;
    [ColorUsage(false, true)]
    public Color  emissiveColor = Color.black;
    [Range(0f, 1f)] public float metallic   = 0.9f;
    [Range(0f, 1f)] public float smoothness = 0.85f;
}

public class ProductVariantController : MonoBehaviour
{
    [Header("Body Renderers (color changes)")]
    public List<Renderer> bodyRenderers = new();

    [Header("Variants")]
    public List<ProductVariant> variants = new();

    public System.Action<int, ProductVariant> OnVariantChanged;

    private int _currentIndex;
    private MaterialPropertyBlock _mpb;

    void Awake() => _mpb = new MaterialPropertyBlock();

    void Start()
    {
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
        ProductVariant v = variants[index];
        foreach (Renderer r in bodyRenderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor",      v.baseColor);
            _mpb.SetFloat("_Metallic",       v.metallic);
            _mpb.SetFloat("_Smoothness",     v.smoothness);
            _mpb.SetColor("_EmissiveColor",  v.emissiveColor);
            r.SetPropertyBlock(_mpb);
        }
    }

    public ProductVariant CurrentVariant    => variants.Count > 0 ? variants[_currentIndex] : null;
    public int            CurrentIndex      => _currentIndex;
    public int            VariantCount      => variants.Count;
    public string         CurrentVariantName => CurrentVariant?.variantName ?? "";
}
