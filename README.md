# ぎょろめ — Gyorome

画面いっぱいの **目玉ふたつ** が、ぎょろぎょろ動く。
ふだんはこちらを見ていて、ときどきまばたきしたり、きょろきょろしたり。
タップ・スワイプ・長押し・2 本指・ふる・かたむける で **100 しゅ以上の反応** をします。見つけた反応は図鑑にたまっていきます。
スマホ・iPad・PC で、ブラウザから遊べます（Android / iPhone / Steam 向けのビルドも用意してあります）。

**▶ あそぶ：<https://ksan260307.github.io/gyorome/>**

## もくじ

- [あそびかた](#あそびかた)
- [反応](#反応)
- [開発](#開発)
- [公開（CI/CD）](#公開cicd)
- [くわしい説明（別のページ）](#くわしい説明別のページ)
- [クレジット](#クレジット)

## あそびかた

目をさわるだけ。横に持てば左右に、たてに持てば上下に、画面いっぱいの目がふたつ。

| さわり方 | 目の反応（例） |
| --- | --- |
| 目をタップ | びくっ・じーっ・ウインク・ぷるん・いたっ・より目 … |
| すばやく 2 回・3 回タップ | びっくり・ぐるん・いないいないばあ / めまい・むっ・はくしょん |
| 連打 | あわわ・あはは・くらくら・ピカピカ |
| 目の外をタップ | なになに？・はっ・ふーん・コンコン |
| スワイプ（上・下・横・ななめ） | あきれ・しょんぼり・すいっ・ころん・ビュン … |
| ゆっくりなでる・こちょこちょ・こする | なでなで・こちょこちょ・かゆい・ぴかーん・やめて |
| 円をかく | ぐるーん・きゅるきゅる・うっとり |
| 長押し | むぎゅ・うとうと・ぷくー・にらめっこ・ごろごろ・じたばた |
| 2 本指（ピンチ・同時タップ・ひねる） | にゅっ・ぱっちり！・でっか・ウインク×2・かくん … |
| 端末をふる・かたむける | がたがた・くらっ・ころころ・ずるっ・あれれ |
| 何もしない | まばたき・きょろきょろ・あくび・ひま〜・うとうと・すやすや・むにゃむにゃ |
| 気分の組み合わせ | おこっているときになでると「ゆるす」、ねむっているときにさわると「びっくり」、同じテンポでたたくと「のりのり」… |

- 右上の **本のボタン** で、はんのう図鑑（見つけた数 / 全部）。まだ見つけていない反応もヒントだけ見えます
- **歯車** で、音・ふるえ・名前の表示・ヒント・目の色（8 色）・まぶたの色（8 色）
- PC では、クリック = タップ、ドラッグ = スワイプ / なでる、ホイール = ピンチ、スペース = ふる、矢印キー = かたむける
- 進み具合と設定は端末（ブラウザ）に自動で保存されます

## 反応

ぜんぶで **114 しゅ**。一覧は [REACTIONS.md](REACTIONS.md)（カタログから自動生成）。

| 分類 | 数 | 例 |
| --- | ---: | --- |
| タップ | 30 | びくっ・ドキッ（ひとみのまん中）・ぽん（目の上の方）・きらきら（とてもまれ） |
| スワイプ・なでる | 25 | ぱっちり（下から上へ）・おやすみ（上から下へ）・なでなで・もくもく（外をこする） |
| 長押し | 10 | むぎゅ・にらめっこ（3 秒こらえる）・すやすや（外を長押し） |
| 2 本指 | 14 | ちっちゃ・でっか・ぎゅー（両目を同時に）・みつめあい |
| ふる・かたむける | 7 | がたがた・はっ！（ねむっているときにふる）・あれれ（さかさま） |
| ひとりでに | 13 | まばたき・ふよふよ（見えないハエ）・むにゃむにゃ（夢）・よるのめ・おはよう |
| くみあわせ | 15 | びっくり・ゆるす・だぶるぱんち・のりのり・もういいって・おかえり・ぴょん |

## 開発

- **エンジン**: Unity 6 (6000.5.4f1) / URP / UI Toolkit / Input System
- **モデル**: Blender 5.2 の Python スクリプトでプロシージャル生成（`blender/scripts/build_eye_kit.py`）
- **音**: 効果音・環境音も Python (numpy) で合成（`tools/make_audio.py`）
- **公開**: GitHub Actions → GitHub Pages（WebGL）。ストア向け（Android AAB / iOS Xcode / Steam Windows・macOS・Linux）も GitHub Actions でビルド

### フォルダ構成

```
blender/scripts/       Blender で目玉の部品を生成するスクリプト（*.blend も出力）
blender/tests/         Blender のテスト
tools/                 アイコン・フォント・音の生成、ローカルビルドとテストのスクリプト、Steam の設定
unity/                 Unity プロジェクト
  Assets/Art/Models    Blender から出力した FBX
  Assets/Scripts       ゲームのコード（Core / Eye / Reactions / UI / Audio / FX）
  Assets/Shaders       目・虹彩・背景のシェーダー
  Assets/UI            UI Toolkit (UXML/USS)・アイコン・フォント
  Assets/Editor        プロジェクトの自動セットアップとビルド
  Assets/Tests         EditMode / PlayMode のテスト
docs/                  ビルド済み WebGL（GitHub Pages の公開用）
.github/workflows/     GitHub Pages への自動デプロイと、ストア向けビルド
```

### アセットを作り直す

```powershell
& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" -b --factory-startup --python blender/scripts/build_eye_kit.py
python tools/make_icons.py
python tools/make_fonts.py   # ゲーム内のテキストを増やしたら、もう一度
python tools/make_audio.py
```

### Unity で開く・ビルドする

- Unity Hub で `unity/` フォルダを開き、`Assets/Scenes/Main.unity` を再生します。
  メニュー **Gyorome > Setup Project** でマテリアル・シーン・`REACTIONS.md` を作り直せます（反応を足したら実行）。
- ローカルで WebGL をビルドして `docs/` を更新：`powershell -ExecutionPolicy Bypass -File tools/build_webgl.ps1`
  （`-Assets` でモデル・音などの生成から、`-Setup` で Unity のセットアップからやり直す）
- ストア向け：`powershell -ExecutionPolicy Bypass -File tools/build_release.ps1 -Target Windows`（`macOS` / `Linux` / `Android` / `iOS` / `WebGL`。Unity Hub にそのモジュールが必要）
- メニュー **Gyorome > Build** からも同じビルドができます

### テスト

```powershell
powershell -ExecutionPolicy Bypass -File tools/run_tests.ps1            # すべて
powershell -ExecutionPolicy Bypass -File tools/run_tests.ps1 -Only PlayMode
```

| 種類 | 件数 | 場所 |
| --- | ---: | --- |
| Unity EditMode（単体） | 79 | `unity/Assets/Tests/EditMode` |
| Unity PlayMode（総合） | 32 | `unity/Assets/Tests/PlayMode` |
| Blender（単体） | 6 | `blender/tests` |
| Python（単体） | 13 | `tools/tests` |

テストの中身は [TESTING.md](TESTING.md) へ。

## 公開（CI/CD）

`main` に push すると `.github/workflows/deploy-pages.yml` が動き、GitHub Pages に公開されます。

1. リポジトリ設定 **Settings → Pages → Build and deployment → Source** を **GitHub Actions** にする（初回のみ）
2. そのままでも、リポジトリに入っているビルド済みの `docs/` が公開されます
3. GitHub Actions 上で Unity からビルドしたい場合は、**Settings → Secrets and variables → Actions** に次を登録します
   - `UNITY_LICENSE` … Unity Hub でライセンスを有効化したときにできる `Unity_lic.ulf` の中身（Windows: `C:\ProgramData\Unity\Unity_lic.ulf`）
   - `UNITY_EMAIL` / `UNITY_PASSWORD` … Unity アカウント（Pro ライセンスの場合は `UNITY_SERIAL` も）

   登録すると、push のたびに [game-ci](https://game.ci/) でテストを走らせ、WebGL をビルドして公開します。
4. ストア向けのビルドは `.github/workflows/release.yml`。タグ `v1.2.3` を push すると Android（AAB / APK）・iOS（Xcode プロジェクト）・Windows / macOS / Linux をビルドして GitHub Release に添付します（「Run workflow」で対象をしぼって手動でも）。署名や Steam へのアップロードの Secrets は [STORE.md](STORE.md) へ
5. ワークフローの権限は、ふだんは読むだけ（`contents: read`）。公開のジョブだけが `pages: write` / `id-token: write`、リリースのジョブだけが `contents: write`、Unity のテストのジョブだけが `checks: write` を持ちます

## くわしい説明（別のページ）

- [REACTIONS.md](REACTIONS.md) — 反応の一覧（114 しゅ。きっかけ・場所・優先度）
- [TECH_NOTES.md](TECH_NOTES.md) — しくみ（目玉のリグ・画面の割り方・ジェスチャー判定・反応の選び方・シェーダー・UI・ビルド）
- [TESTING.md](TESTING.md) — テストの中身
- [STORE.md](STORE.md) — Android / iOS / Steam / Web に出す手順
- [blender/README.md](blender/README.md) — 目玉の部品の作り方

## クレジット

- 企画・制作: Ksan
- フォント: [Noto Sans JP](https://fonts.google.com/noto/specimen/Noto+Sans+JP)（SIL Open Font License 1.1）のサブセット
- モデル・音・絵はすべてこのリポジトリのスクリプトで生成しています
