using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gyorome.Tests
{
    /// <summary>
    /// 総合テスト用の操作ハーネス。実際のシーン（Main）を読み込み、
    /// 指と同じ経路（GyoInput の仮想ポインター → GestureRecognizer → ReactionDirector）で目をさわる。
    /// </summary>
    public static class GameHarness
    {
        public static GameManager GM { get; private set; }
        public static EyePair Eyes => GM.eyes;
        public static ReactionDirector Director => GM.director;
        public static GyoInput GyoIn => GM.input;
        public static GameUI UI => GM.ui;
        public static EyeBehaviour Brain => GM.behaviour;
        const int Finger = 9;
        const int Finger2 = 10;

        public static IEnumerator Boot(float timeScale = 1f)
        {
            SaveSystem.MemoryOnly = true;
            SaveSystem.ResetAll();
            Time.timeScale = 1f;
            SceneManager.LoadScene(GyoConst.SceneName, LoadSceneMode.Single);
            yield return null;
            yield return null;
            float t = 0f;
            while (true)
            {
                GM = UnityEngine.Object.FindAnyObjectByType<GameManager>();
                if (GM != null && GM.Ready) break;
                t += Time.unscaledDeltaTime;
                if (t > 60f) Assert.Fail("シーンの起動がタイムアウトしました");
                yield return null;
            }
            GyoIn.sensorsEnabled = false;
            GM.CancelInvoke();   // その日はじめての「おはよう」はテストでは出さない
            Director.Seed = 1234;
            Director.Stop();
            Time.timeScale = timeScale;
            yield return null;
            yield return null;
        }

        public static void Shutdown()
        {
            Time.timeScale = 1f;
            SaveSystem.ResetAll();
            SaveSystem.MemoryOnly = false;
            SaveSystem.Reload();
        }

        public static Vector2 EyeA => Eyes.PlacementA.center;
        public static Vector2 EyeB => Eyes.PlacementB.center;
        /// <summary>目の外（画面のすみ）。</summary>
        public static Vector2 BgPoint => new Vector2(Screen.width * 0.03f, Screen.height * 0.03f);
        public static float RadiusA => Eyes.PlacementA.radius;

        public static IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        public static IEnumerator Seconds(float s)
        {
            float t = 0f;
            while (t < s)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        public static IEnumerator WaitUntil(Func<bool> cond, float timeout, string what)
        {
            float t = 0f;
            while (!cond())
            {
                t += Time.unscaledDeltaTime;
                if (t > timeout) Assert.Fail($"待ち時間切れ: {what}");
                yield return null;
            }
        }

        public static IEnumerator WaitIdle(float timeout = 15f)
        {
            yield return WaitUntil(() => !Director.Busy, timeout, "反応が終わる");
        }

        public static IEnumerator Tap(Vector2 p)
        {
            GyoIn.VirtualDown(Finger, p);
            yield return Frames(2);
            GyoIn.VirtualUp(Finger, p);
            yield return null;
        }

        public static IEnumerator Swipe(Vector2 from, Vector2 to, float seconds = 0.2f, int steps = 8, int finger = Finger)
        {
            GyoIn.VirtualDown(finger, from);
            float dt = seconds / steps;
            for (int i = 1; i <= steps; i++)
            {
                yield return Seconds(dt);
                GyoIn.VirtualMove(finger, Vector2.Lerp(from, to, i / (float)steps));
            }
            GyoIn.VirtualUp(finger, to);
            yield return null;
        }

        public static void Down(Vector2 p, int finger = Finger) => GyoIn.VirtualDown(finger, p);
        public static void Move(Vector2 p, int finger = Finger) => GyoIn.VirtualMove(finger, p);
        public static void Up(Vector2 p, int finger = Finger) => GyoIn.VirtualUp(finger, p);

        public static IEnumerator Pinch(Vector2 center, float fromDist, float toDist, float seconds = 0.3f, int steps = 6)
        {
            Vector2 a0 = center - Vector2.right * fromDist * 0.5f, b0 = center + Vector2.right * fromDist * 0.5f;
            Vector2 a1 = center - Vector2.right * toDist * 0.5f, b1 = center + Vector2.right * toDist * 0.5f;
            GyoIn.VirtualDown(Finger, a0);
            GyoIn.VirtualDown(Finger2, b0);
            for (int i = 1; i <= steps; i++)
            {
                yield return Seconds(seconds / steps);
                float t = i / (float)steps;
                GyoIn.VirtualMove(Finger, Vector2.Lerp(a0, a1, t));
                GyoIn.VirtualMove(Finger2, Vector2.Lerp(b0, b1, t));
            }
            GyoIn.VirtualUp(Finger2, b1);
            GyoIn.VirtualUp(Finger, a1);
            yield return null;
        }

        public static IEnumerator TwoFingerTap(Vector2 a, Vector2 b)
        {
            GyoIn.VirtualDown(Finger, a);
            GyoIn.VirtualDown(Finger2, b);
            yield return Frames(2);
            GyoIn.VirtualUp(Finger, a);
            GyoIn.VirtualUp(Finger2, b);
            yield return null;
        }

        /// <summary>はじまりのジェスチャーに対応する、終わりのジェスチャー。</summary>
        public static GestureEvent EndOf(GestureEvent e)
        {
            var end = e;
            switch (e.type)
            {
                case GestureType.LongPressBegin: end.type = GestureType.LongPressEnd; break;
                case GestureType.DragBegin: end.type = GestureType.DragEnd; break;
                case GestureType.PinchBegin: end.type = GestureType.PinchEnd; break;
                case GestureType.RotateBegin: end.type = GestureType.RotateEnd; break;
                case GestureType.TiltBegin: end.type = GestureType.TiltEnd; break;
            }
            return end;
        }

        /// <summary>反応を演じるための、それらしいジェスチャーを作る。</summary>
        public static GestureEvent EventFor(Reaction r)
        {
            var type = r.IsIdle ? GestureType.Idle : r.Gestures[0];
            Vector2 pos = r.Zone == Zone.Background ? BgPoint : EyeA;
            Vector2 pos2 = r.Zone == Zone.BothEyes ? EyeB : pos + new Vector2(80f, 0f);
            if (r.Zone == Zone.EyeTop) pos = EyeA + new Vector2(0f, RadiusA * 0.6f);
            if (r.Zone == Zone.EyeBottom) pos = EyeA + new Vector2(0f, -RadiusA * 0.6f);
            var e = new GestureEvent
            {
                type = type, pos = pos, pos2 = pos2, dir = new Vector2(1f, 0f), delta = new Vector2(100f, 0f), magnitude = 1.5f, count = 1, duration = 1f,
                fingers = type == GestureType.PinchBegin || type == GestureType.RotateBegin || type == GestureType.TwoFingerTap || type == GestureType.TwoFingerSwipe ? 2 : 1,
                time = Time.time,
            };
            if (type == GestureType.Swipe || type == GestureType.Flick) { e.magnitude = 200f; e.pos2 = pos + new Vector2(200f, 0f); }
            if (type == GestureType.Circle) e.magnitude = 1f;
            if (type == GestureType.TiltBegin) { e.dir = new Vector2(0.6f, -0.8f); e.magnitude = 0.5f; }
            if (type == GestureType.Shake) e.magnitude = 1f;
            if (type == GestureType.RotateBegin) e.magnitude = 40f;
            return e;
        }
    }
}
