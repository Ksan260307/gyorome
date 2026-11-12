# ストアに出す（Android / iOS / Steam / Web）

ぎょろめを各ストアに出すときの手順と、リポジトリに用意してあるもののメモです。
ビルドそのものは `.github/workflows/release.yml`（CI）か `tools/build_release.ps1`（手元）で作ります。

## 共通：Unity の設定（`unity/Assets/Editor/ProjectSetup.cs` が入れる）

| 項目 | 値 |
| --- | --- |
| 会社名 / 製品名 | Ksan / Gyorome（画面の中のタイトルは「ぎょろめ」） |
| バンドル ID | `com.ksan.gyorome`（Android / iOS / macOS 共通） |
| バージョン | `ProjectSettings` の `bundleVersion`（CI ではタグ `v1.2.3` の数字を使う） |
| ビルド番号 | CI では実行番号（`github.run_number`）。Android の versionCode と iOS の build number に入る |
| 画面の向き | 自動回転（たて・横・さかさま すべて）。目は長い辺で半分ずつに置き直す |
| 入力 | Input System（タッチ・マウス・加速度センサー） |
| アイコン | `unity/Assets/UI/Icons/app_icon.png`（1024px、`tools/make_icons.py` が作る） |

CI でビルドするには、GitHub の **Settings → Secrets and variables → Actions** に Unity のライセンスを登録します（`README.md` の「公開（CI/CD）」）。

## Android（Google Play）

- 形式: **AAB**（`Gyorome-Android-aab` アーティファクト）。手元の端末で試す用に APK も作ります
- 設定: IL2CPP / ARM64 のみ / minSdk 26 / targetSdk は Unity が持つ最新 / 全画面・ノッチの外まで描く
- 署名: Secrets に次を登録すると、リリース用の keystore で署名します（無いときはデバッグ署名）
  - `ANDROID_KEYSTORE_BASE64` … `base64 -w0 your.keystore` の出力
  - `ANDROID_KEYSTORE_PASS` / `ANDROID_KEYALIAS_NAME` / `ANDROID_KEYALIAS_PASS`
  - keystore を作る: `keytool -genkey -v -keystore gyorome.keystore -alias gyorome -keyalg RSA -keysize 2048 -validity 10000`
- Play Console: 「アプリ バンドル」に AAB をアップロード。Play App Signing を使うなら、上の keystore はアップロード鍵になります
- アダプティブアイコン: `app_icon_fg.png`（前景・透明背景）と `app_icon_bg.png`（背景）を用意してあります。Unity Hub に Android モジュールを入れた環境で **Player Settings → Icon → Adaptive** に割り当ててください（モジュールが無い環境でも壊れないよう、セットアップでは既定のアイコンだけ入れています）
- 手元で: `tools/build_release.ps1 -Target Android [-Apk]`（Unity Hub で Android Build Support が必要）

## iOS / iPadOS（App Store）

- CI の `Gyorome-iOS-xcode` アーティファクトは **Xcode プロジェクト**です（Linux では .ipa まで作れないため）
- 設定: IL2CPP / iPhone と iPad / iOS 15.0 以上 / フルスクリーン必須 / ステータスバー非表示
- Mac で:
  1. アーティファクトを展開し、`Unity-iPhone.xcodeproj` を Xcode でひらく
  2. Signing & Capabilities で Team を選ぶ（自動署名）
  3. Product → Archive → Distribute App → App Store Connect
  - コマンドラインなら `xcodebuild -scheme Unity-iPhone -configuration Release -archivePath build/Gyorome.xcarchive archive` → `xcodebuild -exportArchive ...`
- 表示名を日本語にするには、Xcode の Info で `CFBundleDisplayName` を「ぎょろめ」にします（Android は `res/values/strings.xml` の `app_name`）
- センサー（ふる・かたむける）は許可が不要な `CMMotionManager` 経由（Unity の Accelerometer）なので、プライバシーの説明文は不要です
- 手元で: `tools/build_release.ps1 -Target iOS`（Unity Hub で iOS Build Support が必要。Windows でも Xcode プロジェクトまでは作れます）

## Steam（Windows / macOS / Linux）

- CI の `Gyorome-StandaloneWindows64` / `StandaloneOSX` / `StandaloneLinux64` が、そのまま各 depot の中身です
- 設定: Mono / フルスクリーンウィンドウ / 1280×720 から / ウィンドウの大きさ変更可 / macOS は Universal（Intel + Apple Silicon）
- Steamworks で AppID と depot（Windows = AppID+1、macOS = +2、Linux = +3 を想定）を作り、
  `tools/steam/app_build.vdf` と `depot_*.vdf` の `YOUR_APP_ID` を書きかえます
- 手元からアップロード: `steamcmd +login <account> +run_app_build <絶対パス>/tools/steam/app_build.vdf +quit`
- CI からアップロード: Secrets に `STEAM_USERNAME` と `STEAM_CONFIG_VDF`（[game-ci/steam-deploy](https://github.com/game-ci/steam-deploy) の手順で作る）、
  Variables に `STEAM_APP_ID`（と、任意で `STEAM_BRANCH`、既定は `beta`）を登録すると、タグを push したとき（または手動実行で「Steam にアップロード」にチェック）に送ります
- Steam Input / 実績 / オーバーレイなどは Steamworks SDK を入れていません（まずはそのまま出せます。入れるなら Facepunch.Steamworks か Steamworks.NET）
- macOS の公証（notarization）は Steam では不要です。Steam 外で配るときは Apple Developer の署名と公証が必要です

## Web（GitHub Pages）

`main` に push すると `.github/workflows/deploy-pages.yml` が公開します。くわしくは `README.md`。
iPhone の Safari では「ホーム画面に追加」すると全画面で遊べます（`manifest.webmanifest`）。ふる・かたむけるは、はじめてさわったときに許可を求めます。

## リリースの流れ（まとめ）

1. `ProjectSettings` のバージョンを上げる（または CI にまかせてタグの数字を使う）
2. `git tag v1.2.3 && git push origin v1.2.3`
3. Actions の **Release builds** が全対象をビルドし、GitHub Release に zip を添付する（Steam の Secrets があれば Steam にも送る）
4. Android は AAB を Play Console へ、iOS は Xcode プロジェクトを Mac でアーカイブして App Store Connect へ
