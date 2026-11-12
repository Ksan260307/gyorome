using UnityEngine;

namespace Gyorome
{
    /// <summary>画面の中の目の置き場所（画面座標・ピクセル。原点は左下）。</summary>
    public struct EyePlacement
    {
        public Vector2 center;
        public float radius;
        public Rect region;

        public bool Contains(Vector2 p, float margin = 1.12f) => (p - center).sqrMagnitude <= radius * radius * margin * margin;
    }

    /// <summary>
    /// 画面を長い方の辺で半分に割り、それぞれの半分に目を 1 つ置く。
    /// 横長なら左右（A = 左、B = 右）、縦長なら上下（A = 上、B = 下）。
    /// 目の半径は、半分の領域の短い方の辺の半分 × EyeFill。
    /// </summary>
    public static class ScreenLayout
    {
        public static bool IsPortrait(float w, float h) => h > w;

        public static void Compute(float w, float h, out EyePlacement a, out EyePlacement b)
        {
            if (IsPortrait(w, h))
            {
                a.region = new Rect(0f, h * 0.5f, w, h * 0.5f);   // 上
                b.region = new Rect(0f, 0f, w, h * 0.5f);         // 下
            }
            else
            {
                a.region = new Rect(0f, 0f, w * 0.5f, h);         // 左
                b.region = new Rect(w * 0.5f, 0f, w * 0.5f, h);   // 右
            }
            a.center = a.region.center;
            b.center = b.region.center;
            a.radius = Radius(a.region);
            b.radius = Radius(b.region);
        }

        public static float Radius(Rect region) => Mathf.Min(region.width, region.height) * 0.5f * GyoConst.EyeFill;

        /// <summary>画面の中の点が、どちらの目のそばか（0 = A, 1 = B, -1 = どちらでもない）。</summary>
        public static int Hit(Vector2 p, in EyePlacement a, in EyePlacement b, float margin = 1.12f)
        {
            bool inA = a.Contains(p, margin);
            bool inB = b.Contains(p, margin);
            if (inA && inB) return (p - a.center).sqrMagnitude <= (p - b.center).sqrMagnitude ? 0 : 1;
            if (inA) return 0;
            if (inB) return 1;
            return -1;
        }
    }
}
