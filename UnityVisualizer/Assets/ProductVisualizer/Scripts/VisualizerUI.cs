using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// @file VisualizerUI.cs
/// @brief Screen-space UI controller that wires buttons and labels to scene systems.
/// @author Roberto Charreton
/// @date 2026

/// @class VisualizerUI
/// @brief Connects Unity UI buttons and TextMeshPro labels to the three scene controllers.
///
/// All button references and controller references are injected by
/// ProductVisualizerSetup at edit-time, so no manual Inspector wiring is needed
/// when running the setup tool.
///
/// @par Controlled systems
/// | Button          | Action                              |
/// |-----------------|-------------------------------------|
/// | autoRotateButton | OrbitCamera.ToggleAutoRotate()      |
/// | nextColorButton  | CanMaterialSwapper.NextVariant()    |
/// | prevColorButton  | CanMaterialSwapper.PreviousVariant()|
/// | nextBgButton     | BackgroundController.NextPreset()   |
/// | resetCameraButton| OrbitCamera.ResetView()             |
///
/// @par Label updates
/// Labels are refreshed reactively via the OnVariantChanged and OnPresetChanged
/// delegates exposed by CanMaterialSwapper and BackgroundController respectively.
public class VisualizerUI : MonoBehaviour
{
    // ------------------------------------------------------------------
    // Inspector fields — controllers
    // ------------------------------------------------------------------

    /// @name Scene Controllers
    /// @{

    /// @brief Reference to the orbital camera. Drives rotation toggle and reset.
    [Header("Scene Controllers")]
    public OrbitCamera orbitCamera;

    /// @brief Reference to the can material swapper. Drives brand cycling.
    public CanMaterialSwapper canSwapper;

    /// @brief Reference to the background controller. Drives scene preset cycling.
    public BackgroundController backgroundController;

    /// @}

    // ------------------------------------------------------------------
    // Inspector fields — buttons
    // ------------------------------------------------------------------

    /// @name Buttons
    /// @{

    /// @brief Toggles OrbitCamera auto-rotation on/off.
    [Header("Buttons")]
    public Button autoRotateButton;

    /// @brief Advances to the next can brand variant.
    public Button nextColorButton;

    /// @brief Steps back to the previous can brand variant.
    public Button prevColorButton;

    /// @brief Cycles to the next studio background preset.
    public Button nextBgButton;

    /// @brief Resets the camera to its default framing.
    public Button resetCameraButton;

    /// @}

    // ------------------------------------------------------------------
    // Inspector fields — labels
    // ------------------------------------------------------------------

    /// @name Labels
    /// @{

    /// @brief Displays the current brand name, e.g. "Brand: Coca-Cola".
    [Header("Labels")]
    public TextMeshProUGUI variantLabel;

    /// @brief Displays the current background preset name, e.g. "Scene: Dark Studio".
    public TextMeshProUGUI backgroundLabel;

    /// @brief Displays the current auto-rotation state, e.g. "Auto-Rotate: ON".
    public TextMeshProUGUI rotationLabel;

    /// @}

    // ------------------------------------------------------------------
    // Unity lifecycle
    // ------------------------------------------------------------------

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

    // ------------------------------------------------------------------
    // Private helpers
    // ------------------------------------------------------------------

    /// @brief Handler for the Auto-Rotate button; toggles rotation and refreshes the label.
    void OnToggleAutoRotate()
    {
        orbitCamera?.ToggleAutoRotate();
        RefreshRotationLabel();
    }

    /// @brief Pushes the current state of all three systems to their labels.
    void RefreshAllLabels()
    {
        SetLabel(variantLabel,    $"Brand: {canSwapper?.CurrentVariantName}");
        SetLabel(backgroundLabel, $"Scene: {backgroundController?.CurrentPresetName}");
        RefreshRotationLabel();
    }

    /// @brief Updates @ref rotationLabel to reflect OrbitCamera.IsAutoRotating.
    void RefreshRotationLabel()
    {
        if (orbitCamera != null)
            SetLabel(rotationLabel, orbitCamera.IsAutoRotating ? "Auto-Rotate: ON" : "Auto-Rotate: OFF");
    }

    /// @brief Null-safe helper to set a TextMeshProUGUI text value.
    /// @param label Target label. Silently ignored if null.
    /// @param text  New text content.
    static void SetLabel(TextMeshProUGUI label, string text)
    {
        if (label != null) label.text = text;
    }
}
