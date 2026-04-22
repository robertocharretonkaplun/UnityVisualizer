using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BackgroundPreset
{
    public string presetName    = "Dark Studio";
    public Color  topColor      = new(0.06f, 0.06f, 0.18f, 1f);
    public Color  bottomColor   = new(0.01f, 0.01f, 0.04f, 1f);
    public float  vignette      = 1.2f;
    public Color  groundTint    = new(0.10f, 0.10f, 0.12f, 1f);
}

public class BackgroundController : MonoBehaviour
{
    [Header("References")]
    public Renderer backdropRenderer;
    public Renderer groundRenderer;

    [Header("Presets")]
    public List<BackgroundPreset> presets = new();

    public System.Action<int, BackgroundPreset> OnPresetChanged;

    private int _currentIndex;
    private MaterialPropertyBlock _mpb;

    void Awake() => _mpb = new MaterialPropertyBlock();

    void Start()
    {
        if (presets.Count > 0) Apply(0);
    }

    public void NextPreset()
    {
        _currentIndex = (_currentIndex + 1) % presets.Count;
        Apply(_currentIndex);
        OnPresetChanged?.Invoke(_currentIndex, presets[_currentIndex]);
    }

    public void SetPreset(int index)
    {
        if (index < 0 || index >= presets.Count) return;
        _currentIndex = index;
        Apply(index);
        OnPresetChanged?.Invoke(_currentIndex, presets[_currentIndex]);
    }

    void Apply(int index)
    {
        BackgroundPreset p = presets[index];

        if (backdropRenderer != null)
        {
            backdropRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_TopColor",      p.topColor);
            _mpb.SetColor("_BottomColor",   p.bottomColor);
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

    public BackgroundPreset CurrentPreset     => presets.Count > 0 ? presets[_currentIndex] : null;
    public int              CurrentIndex      => _currentIndex;
    public string           CurrentPresetName => CurrentPreset?.presetName ?? "";
}
