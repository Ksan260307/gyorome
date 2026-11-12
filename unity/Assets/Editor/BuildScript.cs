using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Gyorome.EditorTools
{
    /// <summary>
    /// ビルド。ローカルでも CI（game-ci/unity-builder）でも同じメソッドを使う。
    ///   Unity -batchmode -quit -projectPath unity -executeMethod Gyorome.EditorTools.BuildScript.Build
    ///         -customBuildTarget Android -customBuildPath ../Builds/Android/Gyorome.aab
    ///         [-buildVersion 1.2.0] [-androidVersionCode 12] [-androidExportType androidAppBundle|androidPackage]
    ///         [-androidKeystoreName x.keystore -androidKeystorePass *** -androidKeyaliasName a -androidKeyaliasPass ***]
    ///         [-development]
    /// 対象ごとの入口: BuildWebGL / BuildAndroid / BuildIOS / BuildWindows / BuildMacOS / BuildLinux
    /// </summary>
    public static class BuildScript
    {
        [MenuItem("Gyorome/Build/WebGL", priority = 20)] public static void MenuWebGL() => Menu(BuildTarget.WebGL, "../WebGLBuild");
        [MenuItem("Gyorome/Build/Windows (Steam)", priority = 21)] public static void MenuWindows() => Menu(BuildTarget.StandaloneWindows64, "../Builds/Windows/Gyorome.exe");
        [MenuItem("Gyorome/Build/macOS (Steam)", priority = 22)] public static void MenuMac() => Menu(BuildTarget.StandaloneOSX, "../Builds/macOS/Gyorome.app");
        [MenuItem("Gyorome/Build/Linux (Steam)", priority = 23)] public static void MenuLinux() => Menu(BuildTarget.StandaloneLinux64, "../Builds/Linux/Gyorome.x86_64");
        [MenuItem("Gyorome/Build/Android (AAB)", priority = 24)] public static void MenuAndroid() => Menu(BuildTarget.Android, "../Builds/Android/Gyorome.aab");
        [MenuItem("Gyorome/Build/iOS (Xcode project)", priority = 25)] public static void MenuIOS() => Menu(BuildTarget.iOS, "../Builds/iOS");

        static void Menu(BuildTarget target, string output)
        {
            var report = Run(target, output);
            if (report != null && report.summary.result == BuildResult.Succeeded)
                EditorUtility.RevealInFinder(Path.GetFullPath(output));
        }

        // ---- バッチの入口 ----
        public static void Build() => Batch(ParseTarget(Arg("-customBuildTarget")) ?? EditorUserBuildSettings.activeBuildTarget, null);
        public static void BuildWebGL() => Batch(BuildTarget.WebGL, "../WebGLBuild");
        public static void BuildAndroid() => Batch(BuildTarget.Android, "../Builds/Android/Gyorome.aab");
        public static void BuildIOS() => Batch(BuildTarget.iOS, "../Builds/iOS");
        public static void BuildWindows() => Batch(BuildTarget.StandaloneWindows64, "../Builds/Windows/Gyorome.exe");
        public static void BuildMacOS() => Batch(BuildTarget.StandaloneOSX, "../Builds/macOS/Gyorome.app");
        public static void BuildLinux() => Batch(BuildTarget.StandaloneLinux64, "../Builds/Linux/Gyorome.x86_64");

        static void Batch(BuildTarget target, string defaultOutput)
        {
            string output = Arg("-customBuildPath") ?? Arg("-buildPath") ?? defaultOutput ?? DefaultOutput(target);
            BuildReport report;
            try
            {
                report = Run(target, output);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Exit(1);
                return;
            }
            Exit(report != null && report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        static void Exit(int code)
        {
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }

        public static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }

        static bool Flag(string name)
        {
            foreach (var a in Environment.GetCommandLineArgs())
                if (string.Equals(a, name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static BuildTarget? ParseTarget(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            if (Enum.TryParse<BuildTarget>(s, true, out var t)) return t;
            switch (s.ToLowerInvariant())
            {
                case "windows": case "win64": return BuildTarget.StandaloneWindows64;
                case "mac": case "macos": case "osx": return BuildTarget.StandaloneOSX;
                case "linux": return BuildTarget.StandaloneLinux64;
                case "ios": return BuildTarget.iOS;
                case "android": return BuildTarget.Android;
                case "webgl": case "web": return BuildTarget.WebGL;
            }
            return null;
        }

        public static string DefaultOutput(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.WebGL: return "../WebGLBuild";
                case BuildTarget.Android: return "../Builds/Android/Gyorome.aab";
                case BuildTarget.iOS: return "../Builds/iOS";
                case BuildTarget.StandaloneWindows64: return "../Builds/Windows/Gyorome.exe";
                case BuildTarget.StandaloneOSX: return "../Builds/macOS/Gyorome.app";
                case BuildTarget.StandaloneLinux64: return "../Builds/Linux/Gyorome.x86_64";
                default: return "../Builds/" + target;
            }
        }

        public static NamedBuildTarget Named(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android: return NamedBuildTarget.Android;
                case BuildTarget.iOS: return NamedBuildTarget.iOS;
                case BuildTarget.WebGL: return NamedBuildTarget.WebGL;
                default: return NamedBuildTarget.Standalone;
            }
        }

        // ------------------------------------------------------------------
        /// <summary>対象ごとの設定を入れてビルドする。</summary>
        public static BuildReport Run(BuildTarget target, string output)
        {
            var group = BuildPipeline.GetBuildTargetGroup(target);
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
                throw new Exception($"このエディターには {target} のモジュールが入っていません（Unity Hub で追加してください）");
            if (EditorUserBuildSettings.activeBuildTarget != target)
                EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);

            ApplyVersion();
            bool development = Flag("-development");
            output = PrepareOutput(target, output);

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { GyoConst.ScenePath },
                locationPathName = output,
                target = target,
                targetGroup = group,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var s = report.summary;
            Debug.Log($"[Build] {target} {s.result}  size={s.totalSize / (1024 * 1024f):0.0}MB  time={s.totalTime}  errors={s.totalErrors}  -> {Path.GetFullPath(output)}");
            return report;
        }

        static void ApplyVersion()
        {
            string version = Arg("-buildVersion");
            if (!string.IsNullOrEmpty(version) && version != "none") PlayerSettings.bundleVersion = version;
            string code = Arg("-androidVersionCode");
            if (!string.IsNullOrEmpty(code) && int.TryParse(code, out int vc) && vc > 0)
            {
                PlayerSettings.Android.bundleVersionCode = vc;
                PlayerSettings.iOS.buildNumber = vc.ToString();
                PlayerSettings.macOS.buildNumber = vc.ToString();
            }
        }

        static string PrepareOutput(BuildTarget target, string output)
        {
            switch (target)
            {
                case BuildTarget.Android:
                {
                    string export = (Arg("-androidExportType") ?? "").ToLowerInvariant();
                    bool aab = export == "androidappbundle" || (string.IsNullOrEmpty(export) && output.EndsWith(".aab", StringComparison.OrdinalIgnoreCase));
                    EditorUserBuildSettings.buildAppBundle = aab;
                    EditorUserBuildSettings.exportAsGoogleAndroidProject = export == "androidstudioproject";
                    if (!EditorUserBuildSettings.exportAsGoogleAndroidProject)
                    {
                        string ext = aab ? ".aab" : ".apk";
                        if (!output.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) output = Path.ChangeExtension(output, ext);
                    }
                    PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                    string keystore = Arg("-androidKeystoreName");
                    if (!string.IsNullOrEmpty(keystore) && File.Exists(keystore))
                    {
                        PlayerSettings.Android.useCustomKeystore = true;
                        PlayerSettings.Android.keystoreName = keystore;
                        PlayerSettings.Android.keystorePass = Arg("-androidKeystorePass") ?? "";
                        PlayerSettings.Android.keyaliasName = Arg("-androidKeyaliasName") ?? "";
                        PlayerSettings.Android.keyaliasPass = Arg("-androidKeyaliasPass") ?? "";
                        Debug.Log("[Build] Android: 署名に " + keystore + " を使います");
                    }
                    else
                    {
                        PlayerSettings.Android.useCustomKeystore = false;
                        Debug.Log("[Build] Android: デバッグ用の署名です（ストアに出すときは keystore を指定してください）");
                    }
                    break;
                }
                case BuildTarget.StandaloneWindows64:
                    if (!output.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) output = Path.Combine(output, "Gyorome.exe");
                    break;
                case BuildTarget.StandaloneOSX:
                    if (!output.EndsWith(".app", StringComparison.OrdinalIgnoreCase)) output = Path.Combine(output, "Gyorome.app");
                    try { EditorUserBuildSettings.SetPlatformSettings("Standalone", "OSXUniversal", "Architecture", "x64ARM64"); }
                    catch (Exception e) { Debug.LogWarning("[Build] macOS universal の設定ができませんでした: " + e.Message); }
                    break;
                case BuildTarget.StandaloneLinux64:
                    if (!output.EndsWith(".x86_64", StringComparison.OrdinalIgnoreCase)) output = Path.Combine(output, "Gyorome.x86_64");
                    break;
            }
            string dir = target == BuildTarget.WebGL || target == BuildTarget.iOS || target == BuildTarget.StandaloneOSX ? output : Path.GetDirectoryName(output);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            return output;
        }
    }
}
