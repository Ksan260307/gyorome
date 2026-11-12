# ぎょろめ: ストア向けのビルドをローカルで作る
#   powershell -ExecutionPolicy Bypass -File tools/build_release.ps1 -Target Windows [-Version 1.2.0] [-VersionCode 12] [-Apk] [-Development]
#     -Target : Windows | macOS | Linux | Android | iOS | WebGL（Unity Hub でそのモジュールを入れておく）
#     -Apk    : Android を AAB ではなく APK で（手元の端末で試すとき）
#   Android の署名は、環境変数 ANDROID_KEYSTORE_PATH / ANDROID_KEYSTORE_PASS / ANDROID_KEYALIAS_NAME / ANDROID_KEYALIAS_PASS で指定する
#   出力: Builds/<Target>/
param(
    [Parameter(Mandatory = $true)][ValidateSet("Windows", "macOS", "Linux", "Android", "iOS", "WebGL")][string]$Target,
    [string]$Version = "",
    [int]$VersionCode = 0,
    [switch]$Apk,
    [switch]$Development,
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.4f1\Editor\Unity.exe"
)
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$project = Join-Path $root "unity"
$logDir = Join-Path $root "build-tmp"
New-Item -ItemType Directory -Force $logDir | Out-Null

$map = @{
    Windows = @{ unity = "StandaloneWindows64"; out = "Builds/Windows/Gyorome.exe" }
    macOS   = @{ unity = "StandaloneOSX";       out = "Builds/macOS/Gyorome.app" }
    Linux   = @{ unity = "StandaloneLinux64";   out = "Builds/Linux/Gyorome.x86_64" }
    Android = @{ unity = "Android";             out = $(if ($Apk) { "Builds/Android/Gyorome.apk" } else { "Builds/Android/Gyorome.aab" }) }
    iOS     = @{ unity = "iOS";                 out = "Builds/iOS" }
    WebGL   = @{ unity = "WebGL";               out = "WebGLBuild" }
}
$t = $map[$Target]
$out = Join-Path $root $t.out
$log = Join-Path $logDir "build-$Target.log"

$unityArgs = @(
    "-batchmode", "-quit", "-nographics", "-projectPath", $project,
    "-buildTarget", $t.unity,
    "-executeMethod", "Gyorome.EditorTools.BuildScript.Build",
    "-customBuildTarget", $t.unity,
    "-customBuildPath", $out,
    "-logFile", $log
)
if ($Version) { $unityArgs += @("-buildVersion", $Version) }
if ($VersionCode -gt 0) { $unityArgs += @("-androidVersionCode", "$VersionCode") }
if ($Target -eq "Android") { $unityArgs += @("-androidExportType", $(if ($Apk) { "androidPackage" } else { "androidAppBundle" })) }
if ($Development) { $unityArgs += "-development" }
if ($env:ANDROID_KEYSTORE_PATH) {
    $unityArgs += @("-androidKeystoreName", $env:ANDROID_KEYSTORE_PATH, "-androidKeystorePass", $env:ANDROID_KEYSTORE_PASS,
                    "-androidKeyaliasName", $env:ANDROID_KEYALIAS_NAME, "-androidKeyaliasPass", $env:ANDROID_KEYALIAS_PASS)
}

Write-Host "== $Target をビルド -> $out"
$p = Start-Process -FilePath $Unity -ArgumentList $unityArgs -Wait -PassThru -NoNewWindow
if ($p.ExitCode -ne 0) {
    Get-Content $log -Tail 80
    throw "ビルドに失敗しました。ログ: $log"
}
Write-Host "完了: $out"
