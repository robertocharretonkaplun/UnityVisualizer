# Unity HDRP Product Visualizer

An interactive 3D product showcase built with **Unity 6** and the **High Definition Render Pipeline (HDRP)**.
Designed as a professional virtual catalogue experience for high-end consumer products.

---

## Features

| Feature | Description |
|---|---|
| **3-point studio lighting** | HDRP Rectangle Area Lights — Key, Fill, Rim, Bounce |
| **Orbital camera** | Auto-rotation + mouse drag orbit + scroll zoom |
| **Brand variants** | Swap Coca-Cola, Pepsi, Mountain Dew, Sprite labels in real time |
| **Background presets** | Dark Studio, Warm Amber, Deep Space, Emerald Night |
| **Floating animation** | Sinusoidal float + dual-axis tilt for a premium idle look |
| **Smooth shadows** | 64-sided procedural pedestal cylinder eliminates staircasing |
| **Post-processing** | ACES tonemapping, Bloom, SSAO, Vignette via HDRP Volume |
| **One-click setup** | Editor tool rebuilds the entire scene from scratch |

---

## Quick Start

1. Open the project in **Unity 6000.0.x** (HDRP 17).
2. In the menu bar: **Tools → Product Visualizer → Setup Scene**.
3. Press **Play**.

---

## Project Structure

```
Assets/ProductVisualizer/
├── Scripts/
│   ├── OrbitCamera.cs              Spherical-coordinate camera controller
│   ├── CanMaterialSwapper.cs       Swaps brand material sets on the FBX can
│   ├── BackgroundController.cs     Cycles studio backdrop presets
│   ├── ProductFloatAnimation.cs    Idle floating and tilting animation
│   ├── ProductVariantController.cs Colour-only variant system (MaterialPropertyBlock)
│   └── VisualizerUI.cs             UI buttons and labels wired to scene systems
├── Editor/
│   └── ProductVisualizerSetup.cs   One-click scene builder (Editor-only)
└── Shaders/
    └── GradientBackground.shader   HDRP custom unlit gradient + vignette

Assets/Resources/
├── Prefabs/Can.prefab              Soda can FBX (2 material slots)
└── textures/                       Brand label textures (Coke, Pepsi, Mountain Dew, Sprite)
```

---

## Runtime Controls

| Button | Action |
|---|---|
| **Auto-Rotate** | Toggle automatic yaw rotation |
| **< Brand / Brand >** | Cycle product label textures |
| **Background** | Cycle studio backdrop presets |
| **Reset Camera** | Return to default framing |
| Mouse drag | Orbit camera manually |
| Scroll wheel | Zoom in / out |

---

## Architecture

The scene is built around four decoupled runtime systems:

- **OrbitCamera** — drives the camera; controlled by VisualizerUI and mouse input.
- **CanMaterialSwapper** — owns brand variants; notifies VisualizerUI via `OnVariantChanged`.
- **BackgroundController** — owns background presets; notifies VisualizerUI via `OnPresetChanged`.
- **VisualizerUI** — subscriber only; never polls, always reacts to events.

All scene wiring is performed at edit-time by **ProductVisualizerSetup** so no
manual Inspector assignment is required after running the setup tool.

---

## Generating Documentation Locally

Requires [Doxygen](https://www.doxygen.nl/) and [Graphviz](https://graphviz.org/).

```bash
doxygen Doxyfile
# Open docs/html/index.html in a browser
```

Documentation is also automatically built and deployed to **GitHub Pages** on every
push to `main` via the workflow at `.github/workflows/docs.yml`.
