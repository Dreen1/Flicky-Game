using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// One click builds the whole game: menu bar → Flick Battle → Build Scene.
// Running it again wipes Assets/FlickBattle and rebuilds it from scratch.
public static class FlickBattleSetup
{
    const string Root = "Assets/FlickBattle";
    const string ScenePath = Root + "/FlickBattle.unity";

    static readonly Color BlueHot = new Color(0.3f, 0.6f, 1f);
    static readonly Color RedHot = new Color(1f, 0.35f, 0.25f);

    [MenuItem("Flick Battle/Build Scene")]
    public static void Build()
    {
#if !ENABLE_LEGACY_INPUT_MANAGER
        EditorUtility.DisplayDialog("One setting first",
            "Go to Edit → Project Settings → Player → Other Settings → Active Input Handling, " +
            "set it to \"Both\", let Unity restart, then click Flick Battle → Build Scene again.", "OK");
        return;
#else
        if (Resources.Load<TMP_Settings>("TMP Settings") == null)
        {
            EditorUtility.DisplayDialog("One setting first",
                "Go to Window → TextMeshPro → Import TMP Essential Resources, click Import, " +
                "then click Flick Battle → Build Scene again.", "OK");
            return;
        }
        if (!EditorUtility.DisplayDialog("Build Flick Battle",
                "This creates the game scene, ships, effects and sounds in Assets/FlickBattle. " +
                "If that folder already exists it will be replaced.", "Build it", "Cancel"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        MakeFolders();

        // Materials
        Material blueBody = Lit("BlueBody", new Color(0.12f, 0.2f, 0.4f), Color.black, 0.9f, 0.8f);
        Material blueAccent = Lit("BlueAccent", new Color(0.7f, 0.75f, 0.85f), Color.black, 0.8f, 0.7f);
        Material redBody = Lit("RedBody", new Color(0.7f, 0.08f, 0.05f), new Color(0.35f, 0.02f, 0.01f), 0.8f, 0.8f);
        Material redAccent = Lit("RedAccent", new Color(0.9f, 0.4f, 0.35f), Color.black, 0.6f, 0.9f);
        Material blueGlow = Glow("BlueGlow", BlueHot * 4f);
        Material redGlow = Glow("RedGlow", RedHot * 4f);
        Material redLights = Glow("RedLights", new Color(1f, 0.15f, 0.1f) * 2f);
        Material ringGlow = Glow("RingGlow", Color.white * 2f);
        Material sparkGlow = Glow("Sparks", Color.white * 2.5f);
        Material starGlow = Glow("Stars", Color.white * 1.5f);
        Material planetMat = Lit("Planet", new Color(0.25f, 0.12f, 0.4f), new Color(0.12f, 0.04f, 0.2f), 0f, 0.3f);

        // Prefabs
        GameObject blueBoom = MakeExplosion("BlueExplosion", BlueHot, Color.white, sparkGlow);
        GameObject redBoom = MakeExplosion("RedExplosion", RedHot, new Color(1f, 0.8f, 0.3f), sparkGlow);
        Ship bluePrefab = MakeShip("BlueShip", Team.Blue, blueBody, blueAccent, blueGlow, blueGlow, ringGlow, blueBoom);
        Ship redPrefab = MakeShip("RedShip", Team.Red, redBody, redAccent, redLights, redGlow, ringGlow, redBoom);

        // Scene
        CameraShake shake = MakeCamera();
        MakeLights();
        MakePostFX();
        MakeStarfield(starGlow);
        MakePlanet(planetMat);

        GameObject gmGo = new GameObject("GameManager");
        GameManager gm = gmGo.AddComponent<GameManager>();
        gmGo.AddComponent<FlickInput>();
        AudioSource sfx = gmGo.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        gm.blueShipPrefab = bluePrefab;
        gm.redShipPrefab = redPrefab;
        gm.cameraShake = shake;
        gm.sfx = sfx;
        gm.whooshClip = MakeWhoosh();
        gm.explosionClip = MakeBoomSound();
        gm.missClip = MakeMissSound();
        gm.winClip = MakeWinSound();
        gm.ui = MakeUI(gm);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        ConfigurePlayer();
        AssetDatabase.SaveAssets();

        Selection.activeGameObject = gmGo;
        EditorUtility.DisplayDialog("Flick Battle is ready",
            "Press the Play button at the top of Unity.\n\n" +
            "Click a ship, then quickly drag and let go to flick it.", "Let's play");
#endif
    }

    // ---------- Folders, materials ----------

    static void MakeFolders()
    {
        if (AssetDatabase.IsValidFolder(Root)) AssetDatabase.DeleteAsset(Root);
        AssetDatabase.CreateFolder("Assets", "FlickBattle");
        foreach (string sub in new[] { "Materials", "Prefabs", "Audio" })
            AssetDatabase.CreateFolder(Root, sub);
    }

    static Material Lit(string name, Color color, Color emission, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material m = new Material(shader);
        m.SetColor("_BaseColor", color);
        m.SetColor("_Color", color);
        m.SetFloat("_Metallic", metallic);
        m.SetFloat("_Smoothness", smoothness);
        if (emission.maxColorComponent > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
        }
        AssetDatabase.CreateAsset(m, $"{Root}/Materials/{name}.mat");
        return m;
    }

    // Additive, unlit, HDR: anything using this glows under Bloom
    static Material Glow(string name, Color hdrColor)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
        Material m = new Material(shader);
        m.SetColor("_BaseColor", hdrColor);
        m.SetColor("_Color", hdrColor);
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 2f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        AssetDatabase.CreateAsset(m, $"{Root}/Materials/{name}.mat");
        return m;
    }

    // ---------- Ships ----------

    static GameObject Part(PrimitiveType type, Transform parent, Material mat, Vector3 pos, Vector3 rot, Vector3 scale)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localEulerAngles = rot;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    // Placeholder models built from primitives. Swap the "Model" child for real art later.
    static Ship MakeShip(string name, Team team, Material body, Material accent, Material lights,
                         Material trailMat, Material ringMat, GameObject explosion)
    {
        GameObject root = new GameObject(name);
        Transform model = new GameObject("Model").transform;
        model.SetParent(root.transform, false);

        if (team == Team.Blue)
        {
            // Arrowhead fighter, nose along +X
            Part(PrimitiveType.Capsule, model, body, Vector3.zero, new Vector3(0, 0, 90), new Vector3(0.28f, 0.48f, 0.28f));
            Part(PrimitiveType.Cube, model, accent, new Vector3(-0.18f, 0.2f, 0), new Vector3(0, 0, -35), new Vector3(0.5f, 0.12f, 0.05f));
            Part(PrimitiveType.Cube, model, accent, new Vector3(-0.18f, -0.2f, 0), new Vector3(0, 0, 35), new Vector3(0.5f, 0.12f, 0.05f));
            Part(PrimitiveType.Sphere, model, lights, new Vector3(0.18f, 0, -0.12f), Vector3.zero, new Vector3(0.2f, 0.12f, 0.1f));
            Part(PrimitiveType.Sphere, model, lights, new Vector3(-0.5f, 0, 0), Vector3.zero, new Vector3(0.14f, 0.2f, 0.2f));
        }
        else
        {
            // Flying saucer seen from above
            Part(PrimitiveType.Cylinder, model, body, Vector3.zero, new Vector3(90, 0, 0), new Vector3(0.95f, 0.05f, 0.95f));
            Part(PrimitiveType.Sphere, model, accent, new Vector3(0, 0, -0.08f), Vector3.zero, new Vector3(0.48f, 0.48f, 0.3f));
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f;
                Vector3 p = new Vector3(Mathf.Cos(a) * 0.37f, Mathf.Sin(a) * 0.37f, -0.06f);
                Part(PrimitiveType.Sphere, model, lights, p, Vector3.zero, Vector3.one * 0.09f);
            }
        }

        GameObject ring = Part(PrimitiveType.Cylinder, root.transform, ringMat,
            new Vector3(0, 0, 0.1f), new Vector3(90, 0, 0), new Vector3(1.25f, 0.005f, 1.25f));
        ring.name = "SelectionRing";
        ring.SetActive(false);

        TrailRenderer trail = root.AddComponent<TrailRenderer>();
        trail.time = 0.45f;
        trail.minVertexDistance = 0.05f;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(1f, 0f));
        trail.sharedMaterial = trailMat;
        trail.shadowCastingMode = ShadowCastingMode.Off;

        Ship ship = root.AddComponent<Ship>();
        ship.team = team;
        ship.radius = 0.45f;
        ship.selectionRing = ring;
        ship.trail = trail;
        ship.explosionPrefab = explosion;

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, $"{Root}/Prefabs/{name}.prefab");
        Object.DestroyImmediate(root);
        return saved.GetComponent<Ship>();
    }

    static GameObject MakeExplosion(string name, Color a, Color b, Material mat)
    {
        GameObject go = new GameObject(name);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.22f);
        main.startColor = new ParticleSystem.MinMaxGradient(a, b);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.playOnAwake = true;
        main.maxParticles = 200;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)60) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = fade;

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, $"{Root}/Prefabs/{name}.prefab");
        Object.DestroyImmediate(go);
        return saved;
    }

    // ---------- Scene ----------

    static CameraShake MakeCamera()
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        go.transform.position = new Vector3(0, 0, -10);
        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.01f, 0.01f, 0.035f);
        go.AddComponent<AudioListener>();
        go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        return go.AddComponent<CameraShake>();
    }

    static void MakeLights()
    {
        Light key = new GameObject("Key Light").AddComponent<Light>();
        key.type = LightType.Directional;
        key.color = new Color(0.8f, 0.85f, 1f);
        key.intensity = 1.1f;
        key.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

        // Colored back light for the metallic edge glow
        Light rim = new GameObject("Rim Light").AddComponent<Light>();
        rim.type = LightType.Directional;
        rim.color = new Color(0.7f, 0.35f, 1f);
        rim.intensity = 2f;
        rim.transform.rotation = Quaternion.Euler(25f, 160f, 0f);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.06f, 0.06f, 0.12f);
    }

    static void MakePostFX()
    {
        Volume volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.isGlobal = true;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, $"{Root}/PostFX.asset");

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(1.5f);
        bloom.threshold.Override(0.9f);
        bloom.scatter.Override(0.7f);
        AssetDatabase.AddObjectToAsset(bloom, profile);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.3f);
        AssetDatabase.AddObjectToAsset(vignette, profile);

        volume.sharedProfile = profile;
    }

    static void MakeStarfield(Material mat)
    {
        GameObject go = new GameObject("Starfield");
        go.transform.position = new Vector3(0, 0, 6);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.duration = 10f;
        main.startLifetime = 40f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(0.7f, 0.8f, 1f));
        main.maxParticles = 600;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 12f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(24f, 13f, 1f);

        // Slow drift to the left
        ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.15f, -0.03f);
        vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
    }

    static void MakePlanet(Material mat)
    {
        GameObject planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        planet.name = "Gas Giant";
        Object.DestroyImmediate(planet.GetComponent<Collider>());
        planet.transform.position = new Vector3(3.5f, -2.5f, 12f);
        planet.transform.localScale = Vector3.one * 10f;
        planet.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // ---------- UI ----------

    static UIManager MakeUI(GameManager gm)
    {
        GameObject canvasGo = new GameObject("Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();

        Transform t = canvasGo.transform;
        UIManager ui = canvasGo.AddComponent<UIManager>();
        ui.blueCountText = Text(t, "BlueCount", "BLUE: 5", new Vector2(0, 1), new Vector2(40, -20), 48, TextAlignmentOptions.Left);
        ui.turnText = Text(t, "Turn", "BLUE'S TURN", new Vector2(0.5f, 1), new Vector2(0, -20), 52, TextAlignmentOptions.Center);
        ui.feedbackText = Text(t, "Feedback", "", new Vector2(0.5f, 1), new Vector2(0, -95), 56, TextAlignmentOptions.Center);
        ui.redCountText = Text(t, "RedCount", "RED: 5", new Vector2(1, 1), new Vector2(-40, -20), 48, TextAlignmentOptions.Right);
        ui.blueCountText.color = ui.blueColor;
        ui.redCountText.color = ui.redColor;

        // Bottom corners, out of the way of flicks
        MakeButton(t, "New Game", new Vector2(0, 0), new Vector2(24, 20), new Color(0.15f, 0.35f, 0.9f), gm.NewGame);
        ui.replayButton = MakeButton(t, "Watch Replay", new Vector2(1, 0), new Vector2(-24, 20), new Color(0.45f, 0.2f, 0.85f), gm.WatchReplay);
        return ui;
    }

    static TextMeshProUGUI Text(Transform parent, string name, string text, Vector2 anchor, Vector2 pos,
                                float size, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(700, 90);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = align;
        tmp.color = Color.white;
        return tmp;
    }

    static GameObject MakeButton(Transform parent, string label, Vector2 anchor, Vector2 pos, Color color, UnityAction onClick)
    {
        GameObject go = new GameObject(label + " Button", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(260, 70);
        go.AddComponent<Image>().color = color;
        Button button = go.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(button.onClick, onClick);

        TextMeshProUGUI tmp = Text(go.transform, "Label", label, new Vector2(0.5f, 0.5f), Vector2.zero, 38, TextAlignmentOptions.Center);
        ((RectTransform)tmp.transform).sizeDelta = rt.sizeDelta;
        return go;
    }

    // ---------- Player settings ----------

    static void ConfigurePlayer()
    {
        PlayerSettings.productName = "Flick Battle";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.dreen1.flickbattle");
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.dreen1.flickbattle");
    }

    // ---------- Sounds (generated, no downloads needed) ----------

    delegate float SampleFn(float time);

    static AudioClip Wav(string name, float seconds, SampleFn sample)
    {
        const int rate = 44100;
        int n = (int)(rate * seconds);
        string path = $"{Root}/Audio/{name}.wav";
        using (BinaryWriter w = new BinaryWriter(File.Create(path)))
        {
            w.Write(Encoding.ASCII.GetBytes("RIFF"));
            w.Write(36 + n * 2);
            w.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
            w.Write(16);
            w.Write((short)1);       // PCM
            w.Write((short)1);       // mono
            w.Write(rate);
            w.Write(rate * 2);
            w.Write((short)2);
            w.Write((short)16);
            w.Write(Encoding.ASCII.GetBytes("data"));
            w.Write(n * 2);
            for (int i = 0; i < n; i++)
                w.Write((short)(Mathf.Clamp(sample(i / (float)rate), -1f, 1f) * 32767f));
        }
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    static AudioClip MakeWhoosh()
    {
        const float d = 0.22f;
        System.Random rng = new System.Random(1);
        float phase = 0f, noise = 0f;
        return Wav("Whoosh", d, t =>
        {
            float k = t / d;
            float freq = 600f * Mathf.Pow(80f / 600f, k);
            phase = (phase + freq / 44100f) % 1f;
            noise += ((float)rng.NextDouble() * 2f - 1f - noise) * 0.2f;
            float env = (1f - k) * (1f - k);
            return ((phase * 2f - 1f) * 0.3f + noise * 0.8f) * env;
        });
    }

    static AudioClip MakeBoomSound()
    {
        const float d = 0.6f;
        System.Random rng = new System.Random(2);
        float low = 0f;
        return Wav("Explosion", d, t =>
        {
            low += ((float)rng.NextDouble() * 2f - 1f - low) * 0.06f;
            return low * 3f * Mathf.Pow(1f - t / d, 1.5f);
        });
    }

    static AudioClip MakeMissSound()
    {
        const float d = 0.15f;
        float phase = 0f;
        return Wav("Miss", d, t =>
        {
            float k = t / d;
            phase += 2f * Mathf.PI * (200f * Mathf.Pow(0.4f, k)) / 44100f;
            return Mathf.Sin(phase) * 0.4f * (1f - k);
        });
    }

    static AudioClip MakeWinSound()
    {
        float[] notes = { 523f, 659f, 784f, 1047f };
        return Wav("Win", 0.85f, t =>
        {
            int i = Mathf.Min((int)(t / 0.15f), notes.Length - 1);
            float local = t - i * 0.15f;
            float square = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * notes[i] * t));
            return square * 0.2f * Mathf.Exp(-local * 6f);
        });
    }
}
