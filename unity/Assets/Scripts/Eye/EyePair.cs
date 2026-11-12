using System.Collections.Generic;
using UnityEngine;

namespace Gyorome
{
    /// <summary>ふたつの目と、画面・カメラとの対応。A = 左（縦長では上）、B = 右（縦長では下）。</summary>
    public class EyePair : MonoBehaviour
    {
        public EyeRig A;
        public EyeRig B;
        public Camera cam;
        public Transform stage;

        public EyePlacement PlacementA;
        public EyePlacement PlacementB;
        public float CameraDistance { get; private set; }
        public int LayoutWidth { get; private set; }
        public int LayoutHeight { get; private set; }

        public EyeRig Left => A;
        public EyeRig Right => B;
        public IEnumerable<EyeRig> Both { get { yield return A; yield return B; } }
        public EyeRig Other(EyeRig e) => e == A ? B : A;
        public Mood Mood => A != null ? A.Mood : Mood.Normal;
        public bool Asleep => Mood == Mood.Asleep;

        /// <summary>画面の大きさに合わせて目を置く。1 ワールド単位 = 1 ピクセル（目の面で）。</summary>
        public void Layout(int width, int height)
        {
            LayoutWidth = width;
            LayoutHeight = height;
            ScreenLayout.Compute(width, height, out PlacementA, out PlacementB);
            float fov = GyoConst.CameraFov;
            CameraDistance = (height * 0.5f) / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            if (cam != null)
            {
                cam.fieldOfView = fov;
                cam.transform.position = new Vector3(0f, 0f, -CameraDistance);
                cam.transform.rotation = Quaternion.identity;
                cam.nearClipPlane = Mathf.Max(1f, CameraDistance * 0.1f);
                cam.farClipPlane = CameraDistance * 3f;
            }
            Place(A, PlacementA);
            Place(B, PlacementB);
        }

        void Place(EyeRig e, EyePlacement p)
        {
            if (e == null) return;
            e.Radius = p.radius;
            // 画面の中心を原点に。目の中心は、画面の面（z = 0）の少しうしろ（z = 半径）なので、
            // 遠近で内側に見えないよう、その深さで画面の置き場所にちょうど映る位置に置く
            float k = (CameraDistance + p.radius) / Mathf.Max(1f, CameraDistance);
            e.BasePosition = new Vector3((p.center.x - LayoutWidth * 0.5f) * k, (p.center.y - LayoutHeight * 0.5f) * k, p.radius);
            e.transform.localPosition = e.BasePosition;
            Vector3 camPos = cam != null ? cam.transform.position : new Vector3(0f, 0f, -CameraDistance);
            e.transform.rotation = Quaternion.LookRotation((camPos - e.transform.position).normalized, Vector3.up);
            e.Apply();
        }

        public EyePlacement PlacementOf(EyeRig e) => e == A ? PlacementA : PlacementB;

        /// <summary>画面の点（左下原点）→ 目の面（z = 0）のワールドの点。</summary>
        public Vector3 ScreenToWorld(Vector2 screen)
        {
            return new Vector3(screen.x - LayoutWidth * 0.5f, screen.y - LayoutHeight * 0.5f, 0f);
        }

        /// <summary>目の中心の画面の位置（ゆれ・ずれもふくむ）。</summary>
        public Vector2 ScreenOf(EyeRig e)
        {
            var p = PlacementOf(e);
            return p.center + e.Offset * e.Radius;
        }

        public EyeRig Nearest(Vector2 screen)
        {
            return (screen - PlacementA.center).sqrMagnitude <= (screen - PlacementB.center).sqrMagnitude ? A : B;
        }

        /// <summary>画面の点が、どちらの目か（0 = A、1 = B、-1 = 背景）。</summary>
        public int Hit(Vector2 screen) => ScreenLayout.Hit(screen, PlacementA, PlacementB);

        public void LookAtScreen(Vector2 screen)
        {
            Vector3 w = ScreenToWorld(screen);
            A.LookAt(w);
            B.LookAt(w);
        }

        public void LookAtViewer()
        {
            A.LookForward();
            B.LookForward();
        }

        public void Blink(float duration = 0.18f)
        {
            A.Blink(duration);
            B.Blink(duration);
        }

        public void SetMood(Mood mood, float seconds)
        {
            A.SetMood(mood, seconds);
            B.SetMood(mood, seconds);
        }

        public void ResetToNeutral()
        {
            A.ResetToNeutral();
            B.ResetToNeutral();
        }

        public void ApplyTheme(int irisTheme, int lidTheme, bool instant = false)
        {
            var it = Themes.IrisOf(irisTheme);
            var lt = Themes.LidOf(lidTheme);
            A.ApplyTheme(it, lt, instant);
            B.ApplyTheme(it, lt, instant);
        }
    }
}
