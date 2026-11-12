using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using Gyorome.EditorTools;

namespace Gyorome.Tests
{
    /// <summary>セットアップで作られるプロジェクト設定・シーン・アセットの検証（ストアに出せる設定になっているか）。</summary>
    public class ProjectConfigTests
    {
        const string AssetsPath = "Assets/Settings/EyeAssets.asset";

        static EyeAssets LoadAssets()
        {
            var a = AssetDatabase.LoadAssetAtPath<EyeAssets>(AssetsPath);
            Assert.IsNotNull(a, "EyeAssets がありません。Gyorome/Setup Project を実行してください。");
            return a;
        }

        [Test]
        public void URP_IsConfiguredLightForMobile()
        {
            var urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            Assert.IsNotNull(urp, "URP が既定のレンダーパイプライン");
            Assert.IsFalse(urp.supportsHDR, "HDR は使わない（軽く）");
            Assert.AreEqual(4, urp.msaaSampleCount, "目のふちをなめらかに");
            Assert.IsFalse(urp.supportsMainLightShadows, "影は使わない");
            Assert.IsFalse(urp.supportsCameraDepthTexture);
            Assert.IsTrue(urp.useSRPBatcher);
        }

        [Test]
        public void PlayerSettings_AreReadyForStores()
        {
            Assert.AreEqual("Gyorome", PlayerSettings.productName);
            Assert.AreEqual("Ksan", PlayerSettings.companyName);
            Assert.AreEqual(ColorSpace.Linear, PlayerSettings.colorSpace);
            Assert.IsFalse(PlayerSettings.SplashScreen.show);
            Assert.AreEqual(UIOrientation.AutoRotation, PlayerSettings.defaultInterfaceOrientation, "スマホもタブレットも、たてでも横でも");
            Assert.IsTrue(PlayerSettings.allowedAutorotateToPortrait);
            Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeLeft);
            Assert.IsTrue(PlayerSettings.allowedAutorotateToLandscapeRight);
            foreach (var t in new[] { NamedBuildTarget.Android, NamedBuildTarget.iOS, NamedBuildTarget.Standalone })
                Assert.AreEqual(ProjectSetup.BundleId, PlayerSettings.GetApplicationIdentifier(t), t.TargetName);
            // Android（Google Play）
            Assert.AreEqual(ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android));
            Assert.AreEqual(AndroidArchitecture.ARM64, PlayerSettings.Android.targetArchitectures, "Play は 64bit が必須");
            Assert.GreaterOrEqual((int)PlayerSettings.Android.minSdkVersion, 26);
            Assert.AreEqual(AndroidSdkVersions.AndroidApiLevelAuto, PlayerSettings.Android.targetSdkVersion);
            // iOS（App Store）
            Assert.AreEqual(ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(NamedBuildTarget.iOS));
            Assert.AreEqual(iOSTargetDevice.iPhoneAndiPad, PlayerSettings.iOS.targetDevice, "iPhone と iPad");
            Assert.AreEqual("15.0", PlayerSettings.iOS.targetOSVersionString);
            Assert.IsTrue(PlayerSettings.iOS.requiresFullScreen);
            // Steam（PC）
            Assert.AreEqual(ScriptingImplementation.Mono2x, PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone));
            Assert.IsTrue(PlayerSettings.resizableWindow);
            // WebGL（GitHub Pages）
            Assert.AreEqual(WebGLCompressionFormat.Gzip, PlayerSettings.WebGL.compressionFormat);
            Assert.IsTrue(PlayerSettings.WebGL.decompressionFallback, "GitHub Pages は Content-Encoding を付けないので展開フォールバックが必要");
            Assert.AreEqual("PROJECT:Gyorome", PlayerSettings.WebGL.template);
            Assert.AreEqual(WebGLExceptionSupport.FullWithoutStacktrace, PlayerSettings.WebGL.exceptionSupport);
            Assert.IsTrue(File.Exists("Assets/WebGLTemplates/Gyorome/index.html"));
            Assert.IsTrue(File.Exists("Assets/WebGLTemplates/Gyorome/icon.png"));
            Assert.IsTrue(File.Exists("Assets/WebGLTemplates/Gyorome/manifest.webmanifest"));
            var icons = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
            Assert.IsTrue(icons.Length > 0 && icons[0] != null, "アプリのアイコン");
        }

        [Test]
        public void WebGLTemplate_UsesUnityPlaceholders()
        {
            string html = File.ReadAllText("Assets/WebGLTemplates/Gyorome/index.html");
            StringAssert.Contains("{{{ LOADER_FILENAME }}}", html);
            StringAssert.Contains("{{{ DATA_FILENAME }}}", html);
            StringAssert.Contains("{{{ FRAMEWORK_FILENAME }}}", html);
            StringAssert.Contains("createUnityInstance", html);
            StringAssert.Contains("DeviceMotionEvent", html, "iOS のセンサーの許可");
        }

        [Test]
        public void Scene_IsInBuildSettings()
        {
            Assert.IsTrue(EditorBuildSettings.scenes.Any(s => s.enabled && s.path == GyoConst.ScenePath));
        }

        [Test]
        public void Scene_HasAllSystemsWired()
        {
            EditorSceneManager.OpenScene(GyoConst.ScenePath, OpenSceneMode.Single);
            var gm = Object.FindAnyObjectByType<GameManager>();
            Assert.IsNotNull(gm);
            Assert.AreEqual(GyoConst.GameObjectName, gm.gameObject.name, "jslib の SendMessage の宛先");
            Assert.IsNotNull(gm.assets);
            Assert.IsNotNull(gm.cam);
            Assert.IsNotNull(gm.eyes);
            Assert.IsNotNull(gm.input);
            Assert.IsNotNull(gm.director);
            Assert.IsNotNull(gm.behaviour);
            Assert.IsNotNull(gm.audioManager);
            Assert.IsNotNull(gm.emotes);
            Assert.IsNotNull(gm.ui);
            var a = gm.assets;
            Assert.IsNotNull(a.eyeball);
            Assert.IsNotNull(a.iris);
            Assert.IsNotNull(a.lidUpper);
            Assert.IsNotNull(a.lidLower);
            Assert.IsNotNull(a.tear);
            Assert.IsNotNull(a.sclera);
            Assert.IsNotNull(a.irisMaterial);
            Assert.IsNotNull(a.lid);
            Assert.IsNotNull(a.tearMaterial);
            Assert.IsNotNull(a.background);
            Assert.IsNotNull(a.ambience);
            Assert.IsNotNull(a.bodyFont);
            Assert.IsNotNull(a.titleFont);
            Assert.Greater(a.clips.Count, 20);
            var doc = gm.ui.GetComponent<UIDocument>();
            Assert.IsNotNull(doc.panelSettings);
            Assert.IsNotNull(doc.visualTreeAsset);
            Assert.IsNotNull(Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>());
            Assert.IsNotNull(Camera.main);
            Assert.IsFalse(Camera.main.GetUniversalAdditionalCameraData().renderPostProcessing, "ポストエフェクトは使わない（軽く）");
            Assert.AreEqual(CameraClearFlags.SolidColor, Camera.main.clearFlags);
        }

        [TestCase("M_Sclera", "Gyorome/Toon")]
        [TestCase("M_Lid", "Gyorome/Toon")]
        [TestCase("M_Tear", "Gyorome/Toon")]
        [TestCase("M_Iris", "Gyorome/Iris")]
        [TestCase("M_Background", "Gyorome/Background")]
        public void Materials_UseExpectedShaders(string mat, string shader)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Art/Materials/{mat}.mat");
            Assert.IsNotNull(m, mat);
            Assert.AreEqual(shader, m.shader.name);
        }

        [Test]
        public void Materials_ScleraGlintsAndLidsDoNot()
        {
            var sclera = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_Sclera.mat");
            var lid = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/M_Lid.mat");
            Assert.AreEqual(1f, sclera.GetFloat("_Glint"));
            Assert.AreEqual(0f, lid.GetFloat("_Glint"));
        }

        [TestCase("Gyorome/Toon")]
        [TestCase("Gyorome/Iris")]
        [TestCase("Gyorome/Background")]
        public void Shaders_CompileWithoutErrors(string name)
        {
            var s = Shader.Find(name);
            Assert.IsNotNull(s, name);
            Assert.IsFalse(ShaderUtil.ShaderHasError(s), $"{name} にコンパイルエラー");
            Assert.IsTrue(s.isSupported, $"{name} がこの環境で使えない");
        }

        [Test]
        public void Models_FaceUnityForwardAndHaveVertexColors()
        {
            var a = LoadAssets();
            foreach (var m in new[] { a.eyeball, a.iris, a.lidUpper, a.lidLower, a.tear })
            {
                Assert.AreEqual(m.vertexCount, m.colors32.Length, m.name + " に頂点カラーがない");
                Assert.Greater(m.vertexCount, 100, m.name);
            }
            // 白目は半径 1
            var b = a.eyeball.bounds;
            Assert.AreEqual(2f, b.size.x, 0.05f);
            Assert.AreEqual(2f, b.size.y, 0.05f);
            Assert.AreEqual(2f, b.size.z, 0.05f);
            // 虹彩は Unity の +Z（Blender の -Y）を向く
            Vector3 c = Vector3.zero;
            foreach (var v in a.iris.vertices) c += v;
            c /= a.iris.vertexCount;
            Assert.Greater(c.z, 0.85f, "虹彩は +Z 側にある: " + c);
            Assert.AreEqual(0f, c.x, 0.02f);
            Assert.AreEqual(0f, c.y, 0.02f);
            // 虹彩の UV は (距離 0..1, 角度 0..1)
            var uv = a.iris.uv;
            Assert.AreEqual(0f, uv.Min(u => u.x), 1e-3f);
            Assert.AreEqual(1f, uv.Max(u => u.x), 1e-3f);
            Assert.LessOrEqual(uv.Max(u => u.y), 1f + 1e-3f);
            // 上まぶたは上（+Y）、下まぶたは下
            Vector3 cu = Vector3.zero, cl = Vector3.zero;
            foreach (var v in a.lidUpper.vertices) cu += v;
            foreach (var v in a.lidLower.vertices) cl += v;
            Assert.Greater(cu.y / a.lidUpper.vertexCount, 0.2f);
            Assert.Less(cl.y / a.lidLower.vertexCount, -0.2f);
            // まぶたは白目の外側
            Assert.Greater(a.lidUpper.vertices.Min(v => v.magnitude), 1.02f);
        }

        [Test]
        public void Audio_EveryClipUsedByTheCodeExists()
        {
            var a = LoadAssets();
            var names = new HashSet<string>(a.clips.Select(c => c.name));
            var used = new HashSet<string>();
            foreach (var f in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
            {
                string code = File.ReadAllText(f);
                foreach (Match m in Regex.Matches(code, @"\.(?:Sfx|Play)\(""([a-z_]+)""")) used.Add(m.Groups[1].Value);
            }
            Assert.Greater(used.Count, 15);
            foreach (var u in used) Assert.IsTrue(names.Contains(u), $"音 '{u}' がありません（tools/make_audio.py）");
            foreach (var c in a.clips) Assert.IsNotNull(c.clip, c.name);
        }

        [Test]
        public void UI_ContainsEveryElementTheCodeUses()
        {
            var vta = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/GameUI.uxml");
            Assert.IsNotNull(vta);
            var root = vta.Instantiate();
            string code = File.ReadAllText("Assets/Scripts/UI/GameUI.cs");
            var names = new HashSet<string>();
            foreach (Match m in Regex.Matches(code, @"Q<\w+>\(""([\w-]+)""\)")) names.Add(m.Groups[1].Value);
            foreach (Match m in Regex.Matches(code, @"\.Q\(""([\w-]+)""\)")) names.Add(m.Groups[1].Value);
            Assert.Greater(names.Count, 15);
            foreach (var n in names) Assert.IsNotNull(root.Q(n), $"UXML に '{n}' がありません");
        }

        [Test]
        public void UI_StyleSheetDefinesEveryClassTheCodeUses()
        {
            string uss = File.ReadAllText("Assets/UI/GameUI.uss");
            foreach (var file in new[] { "Assets/Scripts/UI/GameUI.cs", "Assets/Scripts/FX/Emotes.cs" })
            {
                string code = File.ReadAllText(file);
                foreach (Match m in Regex.Matches(code, @"(?:AddToClassList|EnableInClassList|RemoveFromClassList|ClassListContains)\(""([a-z][a-z-]*)"""))
                    StringAssert.Contains("." + m.Groups[1].Value, uss, $"USS に {m.Groups[1].Value} がない（{file}）");
            }
            foreach (var style in new[] { "pop", "soft", "big", "angry" })
                StringAssert.Contains(".emote--" + style, uss);
            foreach (Match m in Regex.Matches(uss, @"url\(""([^""]+)""\)"))
                Assert.IsTrue(File.Exists(Path.Combine("Assets/UI", m.Groups[1].Value)), $"USS が参照する {m.Groups[1].Value} がない");
        }

        [Test]
        public void Fonts_CoverAllJapaneseText()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/UI/Fonts/GyoromeSans-Bold.ttf");
            Assert.IsNotNull(font, "tools/make_fonts.py を実行してください");
            var text = new System.Text.StringBuilder();
            text.Append(File.ReadAllText("Assets/UI/GameUI.uxml"));
            foreach (var f in Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories))
                foreach (Match m in Regex.Matches(File.ReadAllText(f), "\"([^\"]*)\""))
                    text.Append(m.Groups[1].Value);
            var missing = new HashSet<char>();
            foreach (char ch in text.ToString())
            {
                if (ch < 0x2000) continue;   // 日本語・記号だけ確認
                if (!font.HasCharacter(ch)) missing.Add(ch);
            }
            Assert.IsEmpty(missing, "フォントにない文字: " + new string(missing.ToArray()) + "（tools/make_fonts.py を再実行）");
        }

        [Test]
        public void ReactionDocs_IsUpToDate()
        {
            string path = ReactionDocs.DefaultPath;
            Assert.IsTrue(File.Exists(path), "REACTIONS.md がありません（Gyorome/Setup Project）");
            string md = File.ReadAllText(path);
            foreach (var r in ReactionCatalog.All) StringAssert.Contains("`" + r.Id + "`", md, "REACTIONS.md が古い（Gyorome/Export Reaction List）");
            Assert.AreEqual(ReactionDocs.Build().Replace("\r\n", "\n"), md.Replace("\r\n", "\n"), "REACTIONS.md が古い（Gyorome/Export Reaction List）");
        }

        [Test]
        public void BuildScript_ParsesTargets()
        {
            Assert.AreEqual(BuildTarget.Android, BuildScript.ParseTarget("Android"));
            Assert.AreEqual(BuildTarget.StandaloneWindows64, BuildScript.ParseTarget("windows"));
            Assert.AreEqual(BuildTarget.StandaloneOSX, BuildScript.ParseTarget("StandaloneOSX"));
            Assert.AreEqual(BuildTarget.iOS, BuildScript.ParseTarget("iOS"));
            Assert.IsNull(BuildScript.ParseTarget("nope"));
            StringAssert.EndsWith(".aab", BuildScript.DefaultOutput(BuildTarget.Android));
            StringAssert.EndsWith(".exe", BuildScript.DefaultOutput(BuildTarget.StandaloneWindows64));
        }
    }
}
