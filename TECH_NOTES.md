# しくみ（実装メモ）

[README](README.md) から分けた、しくみのメモです。反応の一覧は [REACTIONS.md](REACTIONS.md)、テストの中身は [TESTING.md](TESTING.md)、ストアに出す手順は [STORE.md](STORE.md)。

## 目玉（`unity/Assets/Scripts/Eye`）

部品は Blender（`blender/scripts/build_eye_kit.py`）でプロシージャルに作った 5 つの FBX です：白目 `Eyeball`（半径 1 の球）・虹彩 `Iris`（球面のふた）・上下のまぶた `LidUpper` / `LidLower`（厚みのある球の殻）・しずく `Tear`。
色は頂点カラーで、白目とまぶたはほぼ白で作り、Unity 側でテーマの色をかけます。

`EyeRig` がひとつの目です。root（画面の置き場所、カメラの方を向く）→ `Ball`（視線 yaw / pitch と回転 roll）→ `Iris`（Ball の子）、root → `LidUpper` / `LidLower`（回らない）。
反応や idle は「目標の値」（視線・まぶたのひらき・かたむき・ひとみの大きさ・つぶれ・ずれ・色）を入れるだけで、`EyeRig` がばね（`SmoothDamp`）でなめらかに追いつきます。つついたときの「ぷるん」は減衰振動のばね（`Jiggle`）です。

- **視線**: yaw は画面の右が正、pitch は上が正。root のローカル X は画面の左なので、回すときに符号を反転しています（`Quaternion.Euler(-pitch, -yaw, 0)`）。最大 ±62° / ±52°（これより外はひとみがまぶたにかくれる）
- **まぶた**: ひらいた姿勢で作ってあり、閉じるときは目の中心を軸に X まわりに回します（上 95°・下 30°）。怒り・悲しみは Z まわりのかたむきで、左右の目で鏡うつしにします（内がわが下がる = 怒り）
- **まばたき**: ひらき具合に「閉じる 35% → 止まる 10% → ひらく 55%」の曲線をかけます（0.15〜0.22 秒）
- **ひとみ**: メッシュではなく、虹彩の UV（中心からの距離・角度）から `Gyorome/Iris` シェーダーが描きます。大きさ `_PupilSize` と形 `_PupilShape`（まる・ねこ・ハート・星・ぐるぐる・×・輪）を `MaterialPropertyBlock` で目ごとに変えます
- **つや**: 本物の目の映りこみと同じく、カメラから見た向きで決める（`GyoGlint`）ので、目玉が回ってもハイライトは同じ所に出ます。白目と虹彩で同じ式を使うので、つなぎ目で切れません

## 画面の割り方（`ScreenLayout`・`EyePair`）

画面を長い辺で半分に割り、横長なら左右、縦長なら上下に目を置きます。半径は半分の領域の短い辺の 40%。
カメラは縦の画角 30° の少し遠近感のある透視投影で、距離を `(高さ/2) / tan(15°)` にして **目の面で 1 ワールド単位 = 1 ピクセル** にしています。
目の中心は画面の面（z = 0）の少しうしろ（z = 半径）に置くので、指でさわった点（z = 0）を見ると、自分のひとみのふちをさわられたときに 45° 横を見る、という自然な角度になります。ふたつの目は同じ点を見るので、まん中をさわると より目になります。
画面の向きが変わると `GameManager.Layout` が置き直します。

## 入力とジェスチャー（`unity/Assets/Scripts/Core`）

`GyoInput` が Input System の EnhancedTouch（指）とマウス、加速度センサーを読み、`GestureRecognizer`（Unity に依存しない純粋なクラス）に押す・動く・はなす を流します。
`GestureRecognizer` が見分けるもの：タップ（すばやく続けた回数つき）・長押し（はじめ / つづき / 終わり）・スワイプ / はやいスワイプ・ドラッグ・こする（行ったり来たり 3 回）・円（時計回り / 反時計回り）・ピンチ・2 本指タップ / スワイプ・ひねる。
しきい値は端末の DPI から決めます（タップとみなす距離 = 4mm など）。
ふる（加速度の急な変化）・かたむける（重力の画面に沿った成分。`ScreenGravity` が画面の向きごとに軸を合わせる）は `GyoInput` が直接イベントにします。PC ではスペースキーがふる、矢印キーがかたむける、ホイールがピンチです。

UI Toolkit のボタンや窓の上の指は `GameUI.IsPointerOverUI`（`panel.Pick`）で目に届かないようにします。

## 反応（`unity/Assets/Scripts/Reactions`）

`ReactionCatalog`（partial クラス：Tap / Swipe / Hold / Multi / Device / Idle / Combo）に、反応をひとつずつ定義しています：id・名前（オノマトペ）・図鑑のヒント・分類・きっかけ（ジェスチャーと場所）・条件・優先度・重み・演じ方（コルーチン）。
`ReactionDirector` がジェスチャーを受けると、合う反応の中から優先度の高いものだけを残し、重みつきで 1 つ選びます（まだ見つけていないものは 2.5 倍、直前と同じものは 0.3 倍）。
指をはなすまでつづく操作（長押し・ドラッグ・ピンチ・ひねる・かたむける）は、演じている反応に `Latest` / `StreamActive` として流れ、反応は `WhileHeld` で追いかけます。あとから来たジェスチャーは、同じか高い優先度なら今の反応を止めて始めます。
反応が終わると `EyePair.ResetToNeutral` が目を今の気分の姿勢にもどします。気分（うれしい・おこる・かなしい・ねむい・めまい・うっとり…）はしばらく残り、次にどの反応が出るかを変えます（`combo_*`）。
演じた反応は `SaveSystem`（PlayerPrefs に JSON）に記録され、図鑑と「見つけた数」に出ます。

`ReactionContext` が演じ方の道具箱です：さわった目 `Target` ともう片方 `Other`、さわった場所、`Over(秒, t => ...)`（時間をかけて値を変える）、`Wait`、`WhileHeld`、音 `Sfx`、ふるえ `Haptic`、記号 `Emote`、なみだ・あせ `Tear` / `Sweat`、両目まとめての操作 `Both*`。

## ひとりでに（`EyeBehaviour`）

さわっていないとき：まばたき（2.2〜6.5 秒ごと、15% で 2 回つづけて）・きょろきょろ（1.4〜4.5 秒ごと。半分はこちらを見る、たまにおたがいを見る、はしをのぞく）・小さな目の動き・見えないハエを目で追う・あくび・ウインク・くしゃみ。
15 秒でたいくつ、30 秒でうとうと、45 秒でねむる（いびき・z・夢でまぶたの下の目が動く）。夜（23〜5 時）はひとみが大きく、はやくねむくなります。さわると `combo_wake_startle` でびっくりして起きます。
乱数は `Seed` で固定できます。

## 絵づくり（`unity/Assets/Shaders`）

URP の Forward、MSAA 4、HDR・影・ポストエフェクトなし（スマホで軽く）。
`Gyorome/Toon`（白目・まぶた・しずく：頂点カラー × 色、やわらかい 2 階調、ふちの光、つや）、`Gyorome/Iris`（上述）、`Gyorome/Background`（上下のグラデーション + まん中の明るさ + 四すみの暗さ。色はまぶたのテーマで変わる）。

## UI（`unity/Assets/UI`）

UI Toolkit（UXML / USS）。基準 1080×1920・`MatchWidthOrHeight` 0.5 なので、たてでも横でも同じ大きさに見えます。
反応の名前のふきだし・記号（♥ ★ ？ ！ ♪ z …）・見つけた数（本のボタン）・はんのう図鑑（分類ごと。見つけていないものは「？？？」とヒント）・せってい（音・ふるえ・名前・ヒント・目の色 8 色・まぶたの色 8 色・リセット）・しばらくさわらないと出るヒント。
フォントは Noto Sans JP のサブセット（`tools/make_fonts.py`。コードと UXML の文字を集めて作る）。ノッチなどの安全な余白は `Screen.safeArea`（WebGL では jslib の `env(safe-area-inset-*)`）。

## 音（`tools/make_audio.py`）

効果音 27 しゅと小さな環境音のループを numpy で合成しています（まばたき・ぼいん・ぷにっ・くすくす・めまい・いびき・あくび・びっくり・うなる・きらきら・ひゅっ・ちりん・ぽちゃん・くしゃみ・ごろごろ・がたがた・コンコン・ぐるん・ぼよよん・笑い・音符・ジャックポット・ぷしゅー・ちゅっ・ぴょん・ひっく）。`AudioManager.Play(名前, 高さ, 大きさ)` で鳴らします。

## ビルド（`unity/Assets/Editor`）

`ProjectSetup`（`Gyorome/Setup Project`）がプレイヤー設定・URP・マテリアル・`EyeAssets`・UI の設定・シーン・アイコン・`REACTIONS.md` を作ります。何度実行しても同じ結果です。
`BuildScript.Build` は game-ci と同じ引数（`-customBuildTarget` / `-customBuildPath` / `-buildVersion` / `-androidVersionCode` / `-androidExportType` / `-androidKeystore*`）を読むので、手元でも CI でも同じ方法でビルドできます。対象ごとの設定（AAB・ARM64・署名・macOS Universal・出力の拡張子）は `PrepareOutput` にまとめてあります。
