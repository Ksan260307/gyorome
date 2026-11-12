using UnityEngine;

namespace Gyorome
{
    /// <summary>目の色・まぶたの色・背景のテーマ。</summary>
    public static class Themes
    {
        public struct IrisTheme
        {
            public string name;
            public Color outer;
            public Color inner;
        }

        public struct LidTheme
        {
            public string name;
            public Color lid;
            public Color backgroundTop;
            public Color backgroundBottom;
        }

        static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString(h, out var c);
            return c;
        }

        /// <summary>黒目の色。模様のない黒だけ（反応で一時的に金色・虹色になる）。</summary>
        public static readonly IrisTheme[] Iris =
        {
            new IrisTheme { name = "くろ", outer = Hex("#141217"), inner = Hex("#141217") },
        };

        public static readonly LidTheme[] Lid =
        {
            new LidTheme { name = "たまご", lid = Hex("#ffd670"), backgroundTop = Hex("#fff0c8"), backgroundBottom = Hex("#ffcc8a") },
            new LidTheme { name = "もも", lid = Hex("#ffb7a8"), backgroundTop = Hex("#ffe6e0"), backgroundBottom = Hex("#ffc2b3") },
            new LidTheme { name = "みんと", lid = Hex("#9fe3c4"), backgroundTop = Hex("#e4fbf0"), backgroundBottom = Hex("#b3ecd2") },
            new LidTheme { name = "らべんだー", lid = Hex("#cdb6f2"), backgroundTop = Hex("#f1e9ff"), backgroundBottom = Hex("#d9c6f7") },
            new LidTheme { name = "そらいろ", lid = Hex("#9fd3f5"), backgroundTop = Hex("#e6f5ff"), backgroundBottom = Hex("#b6def7") },
            new LidTheme { name = "はだいろ", lid = Hex("#f3c9a3"), backgroundTop = Hex("#fdeedd"), backgroundBottom = Hex("#f3d4b6") },
            new LidTheme { name = "ねずみ", lid = Hex("#b9bcc4"), backgroundTop = Hex("#eceef2"), backgroundBottom = Hex("#cfd3da") },
            new LidTheme { name = "よる", lid = Hex("#5b5e8a"), backgroundTop = Hex("#2d2f52"), backgroundBottom = Hex("#1a1b33") },
        };

        public static IrisTheme IrisOf(int i) => Iris[Mathf.Clamp(i, 0, Iris.Length - 1)];
        public static LidTheme LidOf(int i) => Lid[Mathf.Clamp(i, 0, Lid.Length - 1)];
    }
}
