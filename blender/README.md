# Blender アセット

`scripts/build_eye_kit.py` が、ゲームで使う目玉の部品をプロシージャルに作ります。

```bash
"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" -b --factory-startup --python blender/scripts/build_eye_kit.py
```

- 出力: `unity/Assets/Art/Models/*.fbx`（Unity 用）と `blender/eye_kit.blend`（編集・確認用）
- 部品: `Eyeball`（白目）・`Iris`（虹彩）・`LidUpper` / `LidLower`（まぶた）・`Tear`（なみだ・あせ）
- 色はすべて頂点カラー `Col`（sRGB）。Unity のシェーダーで色をかけます（白目・まぶたの色はテーマで変わる）
- 単位: 目の半径 = 1。見ている向きは Blender の -Y（Unity では +Z）
- ひとみ（瞳孔）はメッシュではなく、虹彩の UV（中心からの距離・角度）から Unity のシェーダーが描きます。
  大きさ・形（まる・ねこ・ハート・星・ぐるぐる・×・輪）を動かせるようにするためです
- まぶたは「ひらいた姿勢」で作り、Unity では目の中心を軸に回して閉じます（`EyeRig.cs`）

テスト: `blender -b --factory-startup --python-exit-code 1 --python blender/tests/test_eye_kit.py`
