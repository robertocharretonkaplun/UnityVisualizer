#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One-click setup: Tools > Product Visualizer > Setup Scene
/// </summary>
public static class ProductVisualizerSetup
{
    const string ASSET_DIR = "Assets/ProductVisualizer/Generated";
    const string ROOT_NAME = "=== PRODUCT VISUALIZER ===";

    // -------------------------------------------------------------------------
    [MenuItem("Tools/Product Visualizer/Setup Scene", priority = 0)]
    public static void SetupScene()
    {
        EnsureDirectories();

        // Remove stale setup if it exists
        var old = GameObject.Find(ROOT_NAME);
        if (old != null) Object.DestroyImmediate(old);

        // Disable the original sun so it doesn't compete
        var sun = GameObject.Find("Sun") ?? GameObject.Find("Directional Light");
        if (sun != null) sun.SetActive(false);

        GameObject root = new(ROOT_NAME);

        // --- Sub-systems ---
        SetupPostProcess(root);
        SetupLights(root);
        var (backdropRenderer, groundRenderer) = SetupEnvironment(root);
        var (productRoot, swapper) = SetupProduct(root);
        SetupPedestal(root);
        var (cam, orbit)  = SetupCamera(root);
        var bgCtrl        = SetupBackgroundController(root, backdropRenderer, groundRenderer);
        SetupUI(root, orbit, swapper, bgCtrl);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root;
        Debug.Log("[ProductVisualizer] Scene ready. Press Play!");
    }

    // -------------------------------------------------------------------------
    // DIRECTORY HELPERS
    // -------------------------------------------------------------------------
    static void EnsureDirectories()
    {
        string[] parts = ASSET_DIR.Split('/');
        string path = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = path + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(path, parts[i]);
            path = next;
        }
    }

    static string SaveAsset(Object asset, string name)
    {
        string path = $"{ASSET_DIR}/{name}";
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        return path;
    }

    // -------------------------------------------------------------------------
    // POST-PROCESSING
    // -------------------------------------------------------------------------
    static void SetupPostProcess(GameObject root)
    {
        var go = new GameObject("PostProcess Volume");
        go.transform.SetParent(root.transform);

        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1f;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.85f);
        bloom.intensity.Override(0.4f);
        bloom.scatter.Override(0.55f);

        var color = profile.Add<ColorAdjustments>(true);
        color.contrast.Override(12f);
        color.saturation.Override(8f);

        var tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.ACES);

        var ao = profile.Add<ScreenSpaceAmbientOcclusion>(true);
        ao.intensity.Override(1.5f);
        ao.directLightingStrength.Override(0.25f);

        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.2f);
        vig.smoothness.Override(0.35f);

        profile.name = "ProductVisualizerProfile";
        string p = SaveAsset(profile, "ProductVisualizerProfile.asset");
        volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(p);
    }

    // -------------------------------------------------------------------------
    // LIGHTS  (3-point studio setup)
    // -------------------------------------------------------------------------
    static void SetupLights(GameObject root)
    {
        var lightRoot = new GameObject("Lights");
        lightRoot.transform.SetParent(root.transform);

        // Key  – warm, front-left
        MakeAreaLight(lightRoot, "Key Light",
            pos:       new Vector3(-1.6f, 2.8f, -1.2f),
            rot:       Quaternion.Euler(48f, 35f, 0f),
            color:     new Color(1f, 0.95f, 0.85f),
            intensity: 3500f,
            size:      new Vector2(0.8f, 1.4f));

        // Fill – cool, right-back
        MakeAreaLight(lightRoot, "Fill Light",
            pos:       new Vector3(2.2f, 1.8f, 1f),
            rot:       Quaternion.Euler(28f, -125f, 0f),
            color:     new Color(0.65f, 0.8f, 1f),
            intensity: 1200f,
            size:      new Vector2(1.8f, 0.9f));

        // Rim  – backlight highlight
        MakeAreaLight(lightRoot, "Rim Light",
            pos:       new Vector3(0f, 2.2f, 2f),
            rot:       Quaternion.Euler(18f, 180f, 0f),
            color:     new Color(0.9f, 0.95f, 1f),
            intensity: 2200f,
            size:      new Vector2(0.4f, 2f));

        // Bounce – subtle floor reflection
        MakeAreaLight(lightRoot, "Bounce Light",
            pos:       new Vector3(0f, -0.2f, 0f),
            rot:       Quaternion.Euler(-90f, 0f, 0f),
            color:     new Color(0.5f, 0.5f, 0.7f),
            intensity: 350f,
            size:      new Vector2(3f, 3f));
    }

    static void MakeAreaLight(GameObject parent, string name,
        Vector3 pos, Quaternion rot, Color color, float intensity, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;

        var light = go.AddComponent<Light>();
        light.type      = LightType.Rectangle;
        light.color     = color;
        light.intensity = intensity;
        light.shadows   = LightShadows.Soft;

        var hd = go.GetComponent<HDAdditionalLightData>();
        if (hd == null) hd = go.AddComponent<HDAdditionalLightData>();
        hd.shapeWidth  = size.x;
        hd.shapeHeight = size.y;
        hd.lightUnit   = LightUnit.Lumen;
        hd.intensity   = intensity;

        hd.normalBias = 0.3f;
    }

    // -------------------------------------------------------------------------
    // ENVIRONMENT  (backdrop + ground)
    // -------------------------------------------------------------------------
    static (Renderer backdrop, Renderer ground) SetupEnvironment(GameObject root)
    {
        var envRoot = new GameObject("Environment");
        envRoot.transform.SetParent(root.transform);

        // ---- Backdrop quad --------------------------------------------------
        var backdropGO = new GameObject("Backdrop");
        backdropGO.transform.SetParent(envRoot.transform);
        backdropGO.transform.localPosition = new Vector3(0f, 1.5f, 3.5f);
        backdropGO.transform.localScale    = new Vector3(9f, 7f, 1f);

        var bmf = backdropGO.AddComponent<MeshFilter>();
        bmf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        var bmr = backdropGO.AddComponent<MeshRenderer>();

        Shader gradShader = Shader.Find("Custom/GradientBackground")
                         ?? Shader.Find("HDRP/Unlit");

        var backdropMat = new Material(gradShader) { name = "Backdrop_Mat" };
        if (gradShader.name.Contains("Gradient"))
        {
            backdropMat.SetColor("_TopColor",    new Color(0.06f, 0.06f, 0.18f));
            backdropMat.SetColor("_BottomColor", new Color(0.01f, 0.01f, 0.04f));
            backdropMat.SetFloat("_VignetteAmount", 1.2f);
        }
        else
        {
            backdropMat.SetColor("_UnlitColor", new Color(0.03f, 0.03f, 0.1f));
        }
        bmr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            SaveAsset(backdropMat, "BackdropMat.mat"));

        // ---- Ground quad ----------------------------------------------------
        var groundGO = new GameObject("Ground");
        groundGO.transform.SetParent(envRoot.transform);
        groundGO.transform.localPosition = Vector3.zero;
        groundGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        groundGO.transform.localScale    = new Vector3(10f, 10f, 1f);

        var gmf = groundGO.AddComponent<MeshFilter>();
        gmf.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        var gmr = groundGO.AddComponent<MeshRenderer>();

        var groundMat = new Material(Shader.Find("HDRP/Lit")) { name = "Ground_Mat" };
        groundMat.SetColor("_BaseColor", new Color(0.08f, 0.08f, 0.1f));
        groundMat.SetFloat("_Metallic",   0.4f);
        groundMat.SetFloat("_Smoothness", 0.9f);
        gmr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            SaveAsset(groundMat, "GroundMat.mat"));

        return (bmr, gmr);
    }

    // -------------------------------------------------------------------------
    // PEDESTAL  — uses 64-sided smooth cylinder to avoid jagged shadow edges
    // -------------------------------------------------------------------------
    static void SetupPedestal(GameObject root)
    {
        var pedestalRoot = new GameObject("Pedestal");
        pedestalRoot.transform.SetParent(root.transform);
        pedestalRoot.transform.localPosition = Vector3.zero;

        var mat = new Material(Shader.Find("HDRP/Lit")) { name = "Pedestal_Mat" };
        mat.SetColor("_BaseColor", new Color(0.14f, 0.14f, 0.17f));
        mat.SetFloat("_Metallic",   0.85f);
        mat.SetFloat("_Smoothness", 0.92f);
        var savedMat = AssetDatabase.LoadAssetAtPath<Material>(SaveAsset(mat, "PedestalMat.mat"));

        PlaceSmoothCylinder(pedestalRoot.transform, "Pedestal_Body",
            pos: new Vector3(0f, 0.15f, 0f), scale: new Vector3(1.2f, 0.15f, 1.2f), mat: savedMat);

        PlaceSmoothCylinder(pedestalRoot.transform, "Pedestal_Top",
            pos: new Vector3(0f, 0.31f, 0f), scale: new Vector3(1.32f, 0.02f, 1.32f), mat: savedMat);
    }

    /// <summary>
    /// Creates a GameObject with a high-polygon-count (64 sides) cylinder mesh so
    /// shadow edges don't show the staircase artefact of Unity's default 24-sided cylinder.
    /// </summary>
    static void PlaceSmoothCylinder(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        const int sides = 64;
        var mesh = BuildCylinderMesh(sides);
        mesh.name = name + "_Mesh";
        string meshPath = $"{ASSET_DIR}/{name}_Mesh.asset";
        AssetDatabase.CreateAsset(mesh, meshPath);

        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;

        go.AddComponent<MeshFilter>().sharedMesh =
            AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.AddComponent<MeshCollider>().sharedMesh =
            AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
    }

    /// <summary>
    /// Procedurally builds a unit cylinder mesh with the given number of sides.
    /// Unity's built-in cylinder uses ~24 sides; 64 makes shadows smooth.
    /// </summary>
    static Mesh BuildCylinderMesh(int sides)
    {
        var mesh = new Mesh();

        int rings      = 2;          // top and bottom rings only
        int vertsPerRing = sides + 1;
        int totalVerts = vertsPerRing * rings + 2; // + 2 for cap centres

        var vertices  = new Vector3[totalVerts];
        var normals   = new Vector3[totalVerts];
        var uvs       = new Vector2[totalVerts];

        // Build side rings
        for (int ring = 0; ring < rings; ring++)
        {
            float y = ring == 0 ? -1f : 1f;
            for (int i = 0; i <= sides; i++)
            {
                float angle  = i / (float)sides * Mathf.PI * 2f;
                float x      = Mathf.Cos(angle);
                float z      = Mathf.Sin(angle);
                int   idx    = ring * vertsPerRing + i;
                vertices[idx] = new Vector3(x, y, z);
                normals[idx]  = new Vector3(x, 0f, z);
                uvs[idx]      = new Vector2(i / (float)sides, ring);
            }
        }

        // Cap centres
        int bottomCentre = vertsPerRing * rings;
        int topCentre    = bottomCentre + 1;
        vertices[bottomCentre] = new Vector3(0f, -1f, 0f);
        normals[bottomCentre]  = Vector3.down;
        uvs[bottomCentre]      = new Vector2(0.5f, 0f);
        vertices[topCentre]    = new Vector3(0f,  1f, 0f);
        normals[topCentre]     = Vector3.up;
        uvs[topCentre]         = new Vector2(0.5f, 1f);

        mesh.vertices = vertices;
        mesh.normals  = normals;
        mesh.uv       = uvs;

        // Triangles – sides
        var tris = new System.Collections.Generic.List<int>();
        for (int i = 0; i < sides; i++)
        {
            int b0 = i,           b1 = i + 1;
            int t0 = vertsPerRing + i, t1 = vertsPerRing + i + 1;
            tris.AddRange(new[] { b0, t0, b1, b1, t0, t1 });
        }

        // Triangles – bottom cap
        for (int i = 0; i < sides; i++)
            tris.AddRange(new[] { bottomCentre, i + 1, i });

        // Triangles – top cap
        for (int i = 0; i < sides; i++)
            tris.AddRange(new[] { topCentre, vertsPerRing + i, vertsPerRing + i + 1 });

        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();
        return mesh;
    }

    // -------------------------------------------------------------------------
    // PRODUCT  (real FBX can from Resources/Prefabs/Can)
    // -------------------------------------------------------------------------
    static (GameObject productRoot, CanMaterialSwapper swapper) SetupProduct(GameObject root)
    {
        var productRoot = new GameObject("Product");
        productRoot.transform.SetParent(root.transform);
        productRoot.transform.localPosition = new Vector3(0f, 0.72f, 0f);
        productRoot.AddComponent<ProductFloatAnimation>();

        // ---- Instantiate the real FBX can -----------------------------------
        var canPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Can.prefab");
        GameObject canGO;
        if (canPrefab != null)
        {
            canGO = (GameObject)PrefabUtility.InstantiatePrefab(canPrefab, productRoot.transform);
            canGO.transform.localPosition = Vector3.zero;
            canGO.transform.localRotation = Quaternion.identity;
            canGO.transform.localScale    = Vector3.one;
        }
        else
        {
            // Fallback: simple cylinder if prefab not found
            canGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            canGO.transform.SetParent(productRoot.transform);
            canGO.transform.localPosition = Vector3.zero;
            canGO.transform.localScale    = new Vector3(0.36f, 0.55f, 0.36f);
            Debug.LogWarning("[ProductVisualizer] Can.prefab not found at Assets/Resources/Prefabs/Can.prefab");
        }

        // ---- Shared aluminium cap material (slot 1) -------------------------
        var capMat = new Material(Shader.Find("HDRP/Lit")) { name = "Can_Cap_Mat" };
        capMat.SetColor("_BaseColor", new Color(0.76f, 0.76f, 0.80f));
        capMat.SetFloat("_Metallic",   0.96f);
        capMat.SetFloat("_Smoothness", 0.92f);
        var savedCapMat = AssetDatabase.LoadAssetAtPath<Material>(SaveAsset(capMat, "Can_Cap_Mat.mat"));

        // ---- One label material per brand (slot 0) --------------------------
        var brands = new (string name, string texPath)[]
        {
            ("Coca-Cola",    "Assets/Resources/textures/Coke Can.jpeg"),
            ("Pepsi",        "Assets/Resources/textures/Pepsi Can.jpeg"),
            ("Mountain Dew", "Assets/Resources/textures/Mountain Dew Can.jpeg"),
            ("Sprite",       "Assets/Resources/textures/Spritei Can.jpeg"),
        };

        var litShader = Shader.Find("HDRP/Lit");
        var swapper   = productRoot.AddComponent<CanMaterialSwapper>();
        swapper.canRenderer = canGO.GetComponent<Renderer>()
                           ?? canGO.GetComponentInChildren<Renderer>();

        foreach (var (brandName, texPath) in brands)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex == null)
            {
                Debug.LogWarning($"[ProductVisualizer] Texture not found: {texPath}");
                continue;
            }

            string safeName = brandName.Replace(" ", "_");
            var labelMat    = new Material(litShader) { name = $"Can_Label_{safeName}" };
            labelMat.SetTexture("_BaseColorMap", tex);
            labelMat.SetColor("_BaseColor",  Color.white);
            labelMat.SetFloat("_Metallic",   0.15f);
            labelMat.SetFloat("_Smoothness", 0.50f);
            var savedLabelMat = AssetDatabase.LoadAssetAtPath<Material>(
                SaveAsset(labelMat, $"Can_Label_{safeName}.mat"));

            swapper.variants.Add(new CanVariant
            {
                variantName   = brandName,
                labelMaterial = savedLabelMat,
                capMaterial   = savedCapMat,
            });
        }

        return (productRoot, swapper);
    }

    // -------------------------------------------------------------------------
    // CAMERA
    // -------------------------------------------------------------------------
    static (GameObject camGO, OrbitCamera orbit) SetupCamera(GameObject root)
    {
        // Target
        var target = new GameObject("Camera Target");
        target.transform.SetParent(root.transform);
        target.transform.localPosition = new Vector3(0f, 0.7f, 0f);

        // Reuse or create main camera
        Camera mainCam = Camera.main;
        GameObject camGO;
        if (mainCam == null)
        {
            camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            mainCam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
        }
        else
        {
            camGO = mainCam.gameObject;
        }

        camGO.transform.SetParent(root.transform);
        camGO.transform.localPosition = new Vector3(0f, 0.7f, -3f);
        mainCam.usePhysicalProperties = true;
        mainCam.nearClipPlane = 0.1f;
        mainCam.farClipPlane  = 50f;

        var hd = camGO.GetComponent<HDAdditionalCameraData>();
        if (hd == null) hd = camGO.AddComponent<HDAdditionalCameraData>();

        var orbit = camGO.AddComponent<OrbitCamera>();
        orbit.target          = target.transform;
        orbit.distance        = 3f;
        orbit.autoRotate      = true;
        orbit.autoRotateSpeed = 20f;

        // Reflection probe
        var probeGO = new GameObject("Reflection Probe");
        probeGO.transform.SetParent(root.transform);
        probeGO.transform.localPosition = new Vector3(0f, 1f, 0f);
        var probe = probeGO.AddComponent<ReflectionProbe>();
        probe.size        = new Vector3(10f, 6f, 10f);
        probe.resolution  = 256;
        probe.mode        = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.EveryFrame;

        return (camGO, orbit);
    }

    // -------------------------------------------------------------------------
    // BACKGROUND CONTROLLER
    // -------------------------------------------------------------------------
    static BackgroundController SetupBackgroundController(
        GameObject root, Renderer backdrop, Renderer ground)
    {
        var go = new GameObject("Background Controller");
        go.transform.SetParent(root.transform);

        var ctrl = go.AddComponent<BackgroundController>();
        ctrl.backdropRenderer = backdrop;
        ctrl.groundRenderer   = ground;

        ctrl.presets = new List<BackgroundPreset>
        {
            new() { presetName = "Dark Studio",
                    topColor = new Color(0.06f, 0.06f, 0.18f), bottomColor = new Color(0.01f, 0.01f, 0.04f),
                    vignette = 1.2f, groundTint = new Color(0.08f, 0.08f, 0.10f) },

            new() { presetName = "Warm Amber",
                    topColor = new Color(0.20f, 0.10f, 0.03f), bottomColor = new Color(0.04f, 0.02f, 0.01f),
                    vignette = 1.0f, groundTint = new Color(0.12f, 0.08f, 0.04f) },

            new() { presetName = "Deep Space",
                    topColor = new Color(0.01f, 0.02f, 0.08f), bottomColor = new Color(0.00f, 0.00f, 0.02f),
                    vignette = 1.5f, groundTint = new Color(0.03f, 0.03f, 0.06f) },

            new() { presetName = "Emerald Night",
                    topColor = new Color(0.02f, 0.10f, 0.06f), bottomColor = new Color(0.01f, 0.03f, 0.02f),
                    vignette = 1.1f, groundTint = new Color(0.04f, 0.08f, 0.05f) },
        };

        return ctrl;
    }

    // -------------------------------------------------------------------------
    // UI
    // -------------------------------------------------------------------------
    static void SetupUI(GameObject root,
        OrbitCamera orbit, CanMaterialSwapper swapper, BackgroundController bgCtrl)
    {
        // Canvas
        var canvasGO = new GameObject("UI Canvas");
        canvasGO.transform.SetParent(root.transform);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight   = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // EventSystem
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.transform.SetParent(root.transform);
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ---- Bottom control bar ---
        var bar = MakePanel(canvasGO.transform, "Controls Bar",
            anchorMin: new Vector2(0f, 0f), anchorMax: new Vector2(1f, 0f),
            pivot: new Vector2(0.5f, 0f), size: new Vector2(0f, 72f), pos: Vector2.zero);
        var barImg = bar.AddComponent<Image>();
        barImg.color = new Color(0f, 0f, 0f, 0.55f);

        float bw = 150f, bh = 48f, gap = 160f;
        float startX = -gap * 2f;

        var btnAutoRot  = MakeButton(bar.transform, "Auto-Rotate",  new Vector2(startX,         36f), new Vector2(bw, bh));
        var btnPrevCol  = MakeButton(bar.transform, "< Brand",      new Vector2(startX + gap,   36f), new Vector2(bw, bh));
        var btnNextCol  = MakeButton(bar.transform, "Brand >",      new Vector2(startX + gap*2, 36f), new Vector2(bw, bh));
        var btnBg       = MakeButton(bar.transform, "Background",   new Vector2(startX + gap*3, 36f), new Vector2(bw, bh));
        var btnReset    = MakeButton(bar.transform, "Reset Camera", new Vector2(startX + gap*4, 36f), new Vector2(bw, bh));

        // ---- Top-left info panel ---
        var info = MakePanel(canvasGO.transform, "Info Panel",
            anchorMin: new Vector2(0f, 1f), anchorMax: new Vector2(0f, 1f),
            pivot: new Vector2(0f, 1f), size: new Vector2(240f, 110f), pos: new Vector2(16f, -16f));
        var infoImg = info.AddComponent<Image>();
        infoImg.color = new Color(0f, 0f, 0f, 0.45f);

        var lblVariant = MakeLabel(info.transform, "VariantLabel",    "Brand: Coca-Cola",    new Vector2(12f, -12f));
        var lblBg      = MakeLabel(info.transform, "BgLabel",         "Scene: Dark Studio",  new Vector2(12f, -42f));
        var lblRot     = MakeLabel(info.transform, "RotLabel",        "Auto-Rotate: ON",     new Vector2(12f, -72f));

        // ---- Wire VisualizerUI ---
        var ui = canvasGO.AddComponent<VisualizerUI>();
        ui.orbitCamera          = orbit;
        ui.canSwapper           = swapper;
        ui.backgroundController = bgCtrl;
        ui.autoRotateButton     = btnAutoRot;
        ui.nextColorButton      = btnNextCol;
        ui.prevColorButton      = btnPrevCol;
        ui.nextBgButton         = btnBg;
        ui.resetCameraButton    = btnReset;
        ui.variantLabel         = lblVariant;
        ui.backgroundLabel      = lblBg;
        ui.rotationLabel        = lblRot;
    }

    // ---- UI Helpers ---
    static GameObject MakePanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin       = anchorMin;
        rt.anchorMax       = anchorMax;
        rt.pivot           = pivot;
        rt.sizeDelta       = size;
        rt.anchoredPosition = pos;
        return go;
    }

    static Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size)
    {
        var go = new GameObject($"Btn_{label}");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;

        var img = go.AddComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.18f, 0.92f);

        var btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(0.22f, 0.22f, 0.35f, 1f);
        cb.pressedColor     = new Color(0.08f, 0.08f, 0.12f, 1f);
        btn.colors = cb;
        btn.targetGraphic = img;

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 15f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        return btn;
    }

    static TextMeshProUGUI MakeLabel(Transform parent, string name, string text, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(1f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = new Vector2(-24f, 26f);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text     = text;
        tmp.fontSize = 15f;
        tmp.color    = Color.white;
        return tmp;
    }
}
#endif
