using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Gyorome.EditorTools
{
    /// <summary>
    /// プロジェクトを一発で組み立てるセットアップ。
    /// プレイヤー設定（Android / iOS / Steam / WebGL）・URP・マテリアル・EyeAssets・UI の設定・シーン（Main.unity）を作る。
    ///   メニュー: Gyorome/Setup Project
    ///   バッチ  : Unity -batchmode -executeMethod Gyorome.EditorTools.ProjectSetup.RunBatch
    /// 何度実行しても同じ結果になる（既存のアセットは GUID を保ったまま更新）。
    /// </summary>
    public static class ProjectSetup
    {
        const string Settings = "Assets/Settings";
        const string Materials = "Assets/Art/Materials";
        const string Models = "Assets/Art/Models";
        const string Icons = "Assets/UI/Icons";
        public const string BundleId = "com.ksan.gyorome";

        [MenuItem("Gyorome/Setup Project", priority = 0)]
        public static void Run()
        {
            EnsureFolder(Settings);
            EnsureFolder(Materials);
            EnsureFolder("Assets/Scenes");
            SetupPlayerSettings();
            SetupURP();
            SetupTextures();
            SetupAudioImport();
            SetupFonts();
            ReimportModels();
            var assets = SetupAssets();
            var panel = SetupPanelSettings();
            BuildScene(assets, panel);
            SetupIcons(assets);
            try { ReactionDocs.Export(ReactionDocs.DefaultPath); }
            catch (Exception e) { Debug.LogWarning("[Setup] REACTIONS.md を書けませんでした: " + e.Message); }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Setup] 完了しました: " + GyoConst.ScenePath + " / 反応 " + ReactionCatalog.Count + " しゅ");
        }

        public static void RunBatch()
        {
            try
            {
                Run();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static T LoadOrCreate<T>(string path, Func<T> create) where T : UnityEngine.Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = create();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        // ------------------------------------------------------------------
        public static void SetupPlayerSettings()
        {
            PlayerSettings.companyName = "Ksan";
            PlayerSettings.productName = "Gyorome";
            if (string.IsNullOrEmpty(PlayerSettings.bundleVersion) || PlayerSettings.bundleVersion == "0.1") PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.statusBarHidden = true;
            PlayerSettings.gcIncremental = true;

            foreach (var t in new[] { NamedBuildTarget.Android, NamedBuildTarget.iOS, NamedBuildTarget.Standalone })
                PlayerSettings.SetApplicationIdentifier(t, BundleId);

            // ---- Android（Google Play: AAB / 64bit / IL2CPP）----
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            if (PlayerSettings.Android.bundleVersionCode < 1) PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.forceSDCardPermission = false;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Medium);
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android, Il2CppCompilerConfiguration.Release);

            // ---- iOS（iPhone と iPad / IL2CPP / iOS 15 以上）----
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.requiresFullScreen = true;
            if (string.IsNullOrEmpty(PlayerSettings.iOS.buildNumber) || PlayerSettings.iOS.buildNumber == "0") PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.iOS.appInBackgroundBehavior = iOSAppInBackgroundBehavior.Suspend;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Medium);

            // ---- Steam（Windows / macOS / Linux）----
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.useMacAppStoreValidation = false;
            PlayerSettings.macRetinaSupport = true;
            PlayerSettings.usePlayerLog = true;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Standalone, ManagedStrippingLevel.Low);

            // ---- WebGL（GitHub Pages）----
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.template = "PROJECT:Gyorome";
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.FullWithoutStacktrace;
            PlayerSettings.WebGL.nameFilesAsHashes = false;
            PlayerSettings.WebGL.showDiagnostics = false;
            PlayerSettings.defaultWebScreenWidth = 1280;
            PlayerSettings.defaultWebScreenHeight = 720;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Minimal);

            // 新しい Input System のみを使う
            var ps = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = ps.FindProperty("activeInputHandler");
            if (input != null) input.intValue = 1;
            ps.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------
        public static UniversalRenderPipelineAsset SetupURP()
        {
            var postData = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            var rd = LoadOrCreate($"{Settings}/GyoRenderer.asset", () =>
            {
                var r = ScriptableObject.CreateInstance<UniversalRendererData>();
                r.postProcessData = postData;
                return r;
            });
            if (rd.postProcessData == null) rd.postProcessData = postData;
            rd.renderingMode = RenderingMode.Forward;
            EditorUtility.SetDirty(rd);

            var urp = LoadOrCreate($"{Settings}/GyoURP.asset", () => UniversalRenderPipelineAsset.Create(rd));
            var so = new SerializedObject(urp);
            void B(string n, bool v) { var p = so.FindProperty(n); if (p != null) p.boolValue = v; else Debug.LogWarning("URP field not found: " + n); }
            void I(string n, int v) { var p = so.FindProperty(n); if (p != null) p.intValue = v; else Debug.LogWarning("URP field not found: " + n); }
            void F(string n, float v) { var p = so.FindProperty(n); if (p != null) p.floatValue = v; else Debug.LogWarning("URP field not found: " + n); }
            B("m_RequireDepthTexture", false);
            B("m_RequireOpaqueTexture", false);
            B("m_SupportsHDR", false);
            I("m_MSAA", 4);
            F("m_RenderScale", 1f);
            B("m_MainLightShadowsSupported", false);
            I("m_AdditionalLightsRenderingMode", 0);
            B("m_AdditionalLightShadowsSupported", false);
            B("m_SoftShadowsSupported", false);
            B("m_UseSRPBatcher", true);
            B("m_SupportsDynamicBatching", false);
            B("m_SupportsLightCookies", false);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(urp);

            GraphicsSettings.defaultRenderPipeline = urp;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urp;
                QualitySettings.vSyncCount = 1;
            }
            QualitySettings.SetQualityLevel(current, false);
            return urp;
        }

        // ------------------------------------------------------------------
        static void SetupTextures()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Icons }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var imp = (TextureImporter)AssetImporter.GetAtPath(p);
                if (imp == null) continue;
                bool appIcon = Path.GetFileName(p).StartsWith("app_icon");
                imp.textureType = TextureImporterType.Default;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.isReadable = appIcon;
                imp.maxTextureSize = appIcon ? 1024 : 128;
                imp.SaveAndReimport();
            }
        }

        static void SetupAudioImport()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var imp = (AudioImporter)AssetImporter.GetAtPath(p);
                if (imp == null) continue;
                var s = imp.defaultSampleSettings;
                bool longClip = p.Contains("ambience");
                s.loadType = longClip ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = longClip ? 0.4f : 0.6f;
                imp.defaultSampleSettings = s;
                imp.loadInBackground = longClip;
                imp.SaveAndReimport();
            }
        }

        static void SetupFonts()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Font", new[] { "Assets/UI/Fonts" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(p) is TrueTypeFontImporter imp)
                {
                    imp.fontTextureCase = FontTextureCase.Dynamic;
                    imp.includeFontData = true;
                    imp.SaveAndReimport();
                }
            }
        }

        static void ReimportModels()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Models }))
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
        }

        // ------------------------------------------------------------------
        static Material Mat(string name, string shader, Action<Material> setup)
        {
            string path = $"{Materials}/{name}.mat";
            var sh = Shader.Find(shader);
            if (sh == null) throw new Exception("Shader not found: " + shader);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(sh);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = sh;
            setup(m);
            m.enableInstancing = false;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Mesh LoadMesh(string name)
        {
            string p = $"{Models}/{name}.fbx";
            var mesh = AssetDatabase.LoadAllAssetsAtPath(p).OfType<Mesh>().FirstOrDefault();
            if (mesh == null) throw new Exception($"モデルがありません: {p}（blender/scripts/build_eye_kit.py を実行してください）");
            return mesh;
        }

        public static EyeAssets SetupAssets()
        {
            var assets = LoadOrCreate($"{Settings}/EyeAssets.asset", () => ScriptableObject.CreateInstance<EyeAssets>());
            assets.eyeball = LoadMesh("Eyeball");
            assets.iris = LoadMesh("Iris");
            assets.lidUpper = LoadMesh("LidUpper");
            assets.lidLower = LoadMesh("LidLower");
            assets.tear = LoadMesh("Tear");

            assets.sclera = Mat("M_Sclera", "Gyorome/Toon", m =>
            {
                m.SetColor("_Color", Color.white);
                m.SetColor("_ShadowColor", new Color(0.78f, 0.80f, 0.88f));
                m.SetFloat("_ShadowThreshold", 0.4f);
                m.SetFloat("_ShadowSoftness", 0.2f);
                m.SetFloat("_RimStrength", 0.12f);
                m.SetFloat("_Glint", 1f);
            });
            assets.lid = Mat("M_Lid", "Gyorome/Toon", m =>
            {
                m.SetColor("_Color", Themes.Lid[0].lid);
                m.SetColor("_ShadowColor", new Color(0.74f, 0.66f, 0.72f));
                m.SetFloat("_ShadowThreshold", 0.45f);
                m.SetFloat("_ShadowSoftness", 0.15f);
                m.SetFloat("_RimStrength", 0.3f);
                m.SetFloat("_Glint", 0f);
            });
            assets.tearMaterial = Mat("M_Tear", "Gyorome/Toon", m =>
            {
                m.SetColor("_Color", new Color(0.78f, 0.92f, 1f));
                m.SetColor("_ShadowColor", new Color(0.6f, 0.72f, 0.95f));
                m.SetFloat("_RimStrength", 0.6f);
                m.SetFloat("_Glint", 1f);
                m.SetFloat("_GlintSize", 0.9f);
            });
            assets.irisMaterial = Mat("M_Iris", "Gyorome/Iris", m =>
            {
                m.SetColor("_IrisColor", Themes.Iris[0].outer);
                m.SetColor("_IrisInner", Themes.Iris[0].inner);
                m.SetFloat("_PupilSize", GyoConst.PupilNormal);
            });
            assets.background = Mat("M_Background", "Gyorome/Background", m =>
            {
                m.SetColor("_ColorTop", Themes.Lid[0].backgroundTop);
                m.SetColor("_ColorBottom", Themes.Lid[0].backgroundBottom);
            });

            assets.clips.Clear();
            assets.ambience = null;
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
                string n = Path.GetFileNameWithoutExtension(p);
                if (n == "ambience") assets.ambience = clip;
                else assets.clips.Add(new EyeAssets.NamedClip { name = n, clip = clip });
            }
            assets.bodyFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/GyoromeSans-Bold.ttf");
            assets.titleFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/GyoromeSans-Black.ttf");
            assets.appIcon = AssetDatabase.LoadAssetAtPath<Texture2D>($"{Icons}/app_icon.png");
            EditorUtility.SetDirty(assets);
            return assets;
        }

        static PanelSettings SetupPanelSettings()
        {
            string tssPath = "Assets/UI/GyoromeTheme.tss";
            if (!File.Exists(tssPath))
            {
                File.WriteAllText(tssPath, "@import url(\"unity-theme://default\");\n");
                AssetDatabase.ImportAsset(tssPath);
            }
            var panel = LoadOrCreate("Assets/UI/GamePanel.asset", () => ScriptableObject.CreateInstance<PanelSettings>());
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(tssPath);
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1080, 1920);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            panel.sortingOrder = 0;
            EditorUtility.SetDirty(panel);
            return panel;
        }

        static void SetupIcons(EyeAssets assets)
        {
            if (assets.appIcon == null) return;
            try
            {
                var sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Unknown, IconKind.Any);
                var icons = new Texture2D[sizes.Length];
                for (int i = 0; i < icons.Length; i++) icons[i] = assets.appIcon;
                PlayerSettings.SetIcons(NamedBuildTarget.Unknown, icons, IconKind.Any);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Setup] アイコンを設定できませんでした: " + e.Message);
            }
        }

        // ------------------------------------------------------------------
        static void BuildScene(EyeAssets assets, PanelSettings panel)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.6f);
            RenderSettings.fog = false;

            var lightGo = new GameObject("Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.98f, 0.94f);
            light.intensity = 1.05f;
            light.shadows = LightShadows.None;
            lightGo.transform.rotation = Quaternion.Euler(38f, -28f, 0f);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Themes.Lid[0].backgroundBottom;
            cam.fieldOfView = GyoConst.CameraFov;
            cam.nearClipPlane = 10f;
            cam.farClipPlane = 20000f;
            cam.allowHDR = false;
            cam.allowMSAA = true;
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = false;
            camData.antialiasing = AntialiasingMode.None;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 0f, -2000f);

            var game = new GameObject(GyoConst.GameObjectName);
            var gm = game.AddComponent<GameManager>();
            var pair = game.AddComponent<EyePair>();
            var input = game.AddComponent<GyoInput>();
            var director = game.AddComponent<ReactionDirector>();
            var behaviour = game.AddComponent<EyeBehaviour>();
            var audio = game.AddComponent<AudioManager>();
            var emotes = game.AddComponent<Emotes>();
            gm.assets = assets;
            gm.cam = cam;
            gm.eyes = pair;
            gm.input = input;
            gm.director = director;
            gm.behaviour = behaviour;
            gm.audioManager = audio;
            gm.emotes = emotes;
            pair.cam = cam;
            audio.assets = assets;
            emotes.assets = assets;

            var uiGo = new GameObject("UI");
            var doc = uiGo.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/GameUI.uxml");
            var ui = uiGo.AddComponent<GameUI>();
            ui.bodyFont = assets.bodyFont;
            ui.titleFont = assets.titleFont;
            gm.ui = ui;

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, GyoConst.ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(GyoConst.ScenePath, true) };
        }
    }
}
