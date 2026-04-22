using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VisualizerUI : MonoBehaviour
{
    [Header("Scene Controllers")]
    public OrbitCamera          orbitCamera;
    public CanMaterialSwapper   canSwapper;
    public BackgroundController backgroundController;

    [Header("Buttons")]
    public Button autoRotateButton;
    public Button nextColorButton;
    public Button prevColorButton;
    public Button nextBgButton;
    public Button resetCameraButton;

    [Header("Labels")]
    public TextMeshProUGUI variantLabel;
    public TextMeshProUGUI backgroundLabel;
    public TextMeshProUGUI rotationLabel;

    void Start()
    {
        autoRotateButton?.onClick.AddListener(OnToggleAutoRotate);
        nextColorButton?.onClick.AddListener(() => canSwapper?.NextVariant());
        prevColorButton?.onClick.AddListener(() => canSwapper?.PreviousVariant());
        nextBgButton?.onClick.AddListener(() => backgroundController?.NextPreset());
        resetCameraButton?.onClick.AddListener(() => orbitCamera?.ResetView());

        if (canSwapper != null)
            canSwapper.OnVariantChanged += (_, v) => SetLabel(variantLabel, $"Brand: {v.variantName}");

        if (backgroundController != null)
            backgroundController.OnPresetChanged += (_, p) => SetLabel(backgroundLabel, $"Scene: {p.presetName}");

        RefreshAllLabels();
    }

    void OnToggleAutoRotate()
    {
        orbitCamera?.ToggleAutoRotate();
        RefreshRotationLabel();
    }

    void RefreshAllLabels()
    {
        SetLabel(variantLabel,    $"Brand: {canSwapper?.CurrentVariantName}");
        SetLabel(backgroundLabel, $"Scene: {backgroundController?.CurrentPresetName}");
        RefreshRotationLabel();
    }

    void RefreshRotationLabel()
    {
        if (orbitCamera != null)
            SetLabel(rotationLabel, orbitCamera.IsAutoRotating ? "Auto-Rotate: ON" : "Auto-Rotate: OFF");
    }

    static void SetLabel(TextMeshProUGUI label, string text)
    {
        if (label != null) label.text = text;
    }
}
