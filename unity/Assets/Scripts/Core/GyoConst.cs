using UnityEngine;

namespace Gyorome
{
    /// <summary>ゲーム全体で使う定数。</summary>
    public static class GyoConst
    {
        public const string SceneName = "Main";
        public const string ScenePath = "Assets/Scenes/Main.unity";
        public const string GameObjectName = "Game";

        /// <summary>目が向ける最大の角度（度）。これより外は、ひとみがまぶたにかくれる。</summary>
        public const float MaxYawDeg = 62f;
        public const float MaxPitchDeg = 52f;

        // まぶたの形（blender/scripts/build_eye_kit.py と同じ値）
        public const float UpperCapDeg = 100f;
        public const float UpperPoleOpenDeg = 155f;
        /// <summary>上まぶたが閉じるときに回る角度。閉じたふちの仰角 = 155 - 95 - 100 = -40 度。</summary>
        public const float UpperTravelDeg = 95f;
        public const float LowerCapDeg = 70f;
        public const float LowerPoleOpenDeg = -125f;
        /// <summary>下まぶたが閉じるときに回る角度。閉じたふちの仰角 = -125 + 30 + 70 = -25 度。</summary>
        public const float LowerTravelDeg = 30f;

        /// <summary>ひとみ（黒目）の大きさ。反応はこの目盛りで書く。シェーダーには PupilToShader 倍して渡す（1 で虹彩のふたいっぱい）。</summary>
        public const float PupilToShader = 1.75f;
        public const float PupilNormal = 0.42f;
        public const float PupilSmall = 0.22f;
        public const float PupilBig = 0.64f;

        /// <summary>「反応 100 しゅ」の目標。図鑑の分母に使う（実際の数はカタログの方が多い）。</summary>
        public const int ReactionGoal = 100;

        /// <summary>画面の高さに対する、目の半径の割合（半分の画面の短い方の辺に対して）。</summary>
        public const float EyeFill = 0.80f;

        /// <summary>カメラの縦の画角（度）。少しだけ遠近感をつける。</summary>
        public const float CameraFov = 30f;

        public static readonly Color DefaultBackgroundTop = new Color(1f, 0.93f, 0.78f);
        public static readonly Color DefaultBackgroundBottom = new Color(1f, 0.80f, 0.56f);
    }

    /// <summary>目の気分（反応のあとにしばらく残る）。</summary>
    public enum Mood
    {
        Normal,
        Happy,
        Angry,
        Sad,
        Sleepy,
        Asleep,
        Dizzy,
        Scared,
        Love,
        Bored,
    }

    /// <summary>ひとみの形（Gyorome/Iris シェーダーの _PupilShape）。</summary>
    public enum PupilShape
    {
        Round = 0,
        Slit = 1,
        Heart = 2,
        Star = 3,
        Spiral = 4,
        Cross = 5,
        Ring = 6,
    }
}
