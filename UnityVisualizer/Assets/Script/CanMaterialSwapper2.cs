using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CanVariant2
{
    public string variantName;
    public Material lableMaterial;
    public Material capMaterial;
}

public class CanMaterialSwapper2 : MonoBehaviour
{
    [Header("Can Renderer")]
    public Renderer canRenderer;

    [Header("Can Variants")]
    public List<CanVariant2> variants = new();

    public System.Action<int, CanVariant2> OnVariantChange;

    private int _currentIndex;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (canRenderer == null)
        {
            canRenderer = GetComponentInChildren<Renderer>();
        }
        if (variants.Count > 0)
        {
            Apply(0);
        }
    }

    public void NextVariant()
    {
        _currentIndex = (_currentIndex + 1) % variants.Count;
        Apply(_currentIndex);
        OnVariantChange?.Invoke(_currentIndex, variants[_currentIndex]);
    }

    public void PreviousVariant()
    {
        _currentIndex = (_currentIndex - 1 + variants.Count) % variants.Count;
        Apply(_currentIndex);
        OnVariantChange?.Invoke(_currentIndex, variants[_currentIndex]);
    }

    public void SetVariantIndex(int index)
    {
        if (index < 00 || index >= variants.Count)
        {
            Debug.LogWarning("Index out of range");
            return;
        }
        _currentIndex = index;
        Apply(_currentIndex);
        OnVariantChange?.Invoke(_currentIndex, variants[_currentIndex]);
    }

    public CanVariant2 CurrentVariant => variants.Count > 0 ? variants[_currentIndex] : null;
    public int CurrentIndex => _currentIndex;
    public int VariantCount => variants.Count;
    public string CurrentVariantName => CurrentVariant?.variantName ?? "";

    void Apply(int index)
    {
        if (canRenderer == null || index >= variants.Count)
        {
            Debug.LogWarning("Renderer or variant index is invalid");
            return;
        }
        CanVariant2 v = variants[index];
        canRenderer.materials = new[] { v.lableMaterial, v.capMaterial };
    }
}
