using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Gyorome.Tests
{
    /// <summary>単体テスト：画面の割り方・イージング・ばね・保存・テーマ・かたむきの軸。</summary>
    public class ScreenLayoutTests
    {
        [Test]
        public void Landscape_SplitsLeftRight()
        {
            ScreenLayout.Compute(1920, 1080, out var a, out var b);
            Assert.AreEqual(0f, a.region.xMin);
            Assert.AreEqual(960f, a.region.xMax);
            Assert.AreEqual(960f, b.region.xMin);
            Assert.AreEqual(new Vector2(480f, 540f), a.center);
            Assert.AreEqual(new Vector2(1440f, 540f), b.center);
            Assert.AreEqual(960f * 0.5f * GyoConst.EyeFill, a.radius, 0.01f);
            Assert.AreEqual(a.radius, b.radius);
        }

        [Test]
        public void Portrait_SplitsTopBottom()
        {
            ScreenLayout.Compute(1080, 1920, out var a, out var b);
            Assert.Greater(a.center.y, b.center.y, "A は上");
            Assert.AreEqual(a.center.x, b.center.x);
            Assert.AreEqual(960f * 0.5f * GyoConst.EyeFill, a.radius, 0.01f, "半分の領域（1080×960）の短い辺で決まる");
            Assert.IsTrue(ScreenLayout.IsPortrait(1080, 1920));
            Assert.IsFalse(ScreenLayout.IsPortrait(1920, 1080));
        }

        [Test]
        public void Square_IsLandscapeAndEyesFit()
        {
            ScreenLayout.Compute(1000, 1000, out var a, out var b);
            Assert.AreEqual(500f * 0.5f * GyoConst.EyeFill, a.radius, 0.01f);
            Assert.Less(a.center.x + a.radius, 500f, "左の目は左半分におさまる");
            Assert.Greater(b.center.x - b.radius, 500f);
        }

        [Test]
        public void Hit_FindsTheNearestEyeOrBackground()
        {
            ScreenLayout.Compute(1920, 1080, out var a, out var b);
            Assert.AreEqual(0, ScreenLayout.Hit(new Vector2(480, 540), a, b));
            Assert.AreEqual(1, ScreenLayout.Hit(new Vector2(1440, 540), a, b));
            Assert.AreEqual(-1, ScreenLayout.Hit(new Vector2(960, 60), a, b));
            Assert.AreEqual(0, ScreenLayout.Hit(new Vector2(480 + a.radius * 1.1f, 540), a, b), "少し外でも目あつかい");
            Assert.AreEqual(-1, ScreenLayout.Hit(new Vector2(480 + a.radius * 1.3f, 540), a, b));
        }
    }

    public class TweenTests
    {
        [Test]
        public void Easings_StartAtZeroAndEndAtOne()
        {
            foreach (var f in new System.Func<float, float>[] { Ease.Linear, Ease.InQuad, Ease.OutQuad, Ease.InOut, Ease.OutBack, Ease.OutElastic, Ease.OutBounce })
            {
                Assert.AreEqual(0f, f(0f), 1e-4f);
                Assert.AreEqual(1f, f(1f), 1e-4f);
                Assert.AreEqual(0f, f(-1f), 1e-4f, "0 より下は 0");
                Assert.AreEqual(1f, f(2f), 1e-4f, "1 より上は 1");
            }
        }

        [Test]
        public void OutBack_Overshoots()
        {
            float max = 0f;
            for (float t = 0f; t <= 1f; t += 0.01f) max = Mathf.Max(max, Ease.OutBack(t));
            Assert.Greater(max, 1.05f);
        }

        [Test]
        public void Bump_PeaksInTheMiddleAndReturns()
        {
            Assert.AreEqual(0f, Ease.Bump(0f), 1e-4f);
            Assert.AreEqual(1f, Ease.Bump(0.5f), 1e-4f);
            Assert.AreEqual(0f, Ease.Bump(1f), 1e-4f);
            Assert.AreEqual(0f, Ease.Flick(1f), 1e-3f);
            Assert.AreEqual(1f, Ease.Wobble(0f), 1e-4f);
            Assert.AreEqual(0f, Ease.Wobble(1f), 1e-4f);
        }

        [Test]
        public void Spring_ConvergesToTarget()
        {
            var s = new Spring(0f, 0.1f) { target = 1f };
            for (int i = 0; i < 120; i++) s.Step(1f / 60f);
            Assert.AreEqual(1f, s.value, 0.02f);
            s.Snap(5f);
            Assert.AreEqual(5f, s.value);
            Assert.AreEqual(5f, s.target);
        }

        [Test]
        public void Jiggle_DecaysToStill()
        {
            var j = new Jiggle(260f, 9f);
            j.Kick(new Vector2(2f, 0f));
            float peak = 0f;
            for (int i = 0; i < 300; i++)
            {
                j.Step(1f / 60f);
                peak = Mathf.Max(peak, j.offset.magnitude);
            }
            Assert.Greater(peak, 0.05f, "つつくとゆれる");
            Assert.Less(j.offset.magnitude, 0.01f, "5 秒でおさまる");
            for (int i = 0; i < 600; i++) j.Step(1f / 60f);
            Assert.IsTrue(j.IsStill);
        }
    }

    public class SaveSystemTests
    {
        [SetUp]
        public void SetUp()
        {
            SaveSystem.MemoryOnly = true;
            SaveSystem.ResetAll();
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.ResetAll();
            SaveSystem.MemoryOnly = false;
            SaveSystem.Reload();
        }

        [Test]
        public void Record_CountsNewAndRepeats()
        {
            Assert.AreEqual(0, SaveSystem.DiscoveredCount);
            Assert.IsTrue(SaveSystem.Record("a"));
            Assert.IsFalse(SaveSystem.Record("a"));
            Assert.IsTrue(SaveSystem.Record("b"));
            Assert.AreEqual(2, SaveSystem.DiscoveredCount);
            Assert.AreEqual(2, SaveSystem.CountOf("a"));
            Assert.AreEqual(1, SaveSystem.CountOf("b"));
            Assert.AreEqual(0, SaveSystem.CountOf("c"));
            Assert.AreEqual(3, SaveSystem.Progress.totalReactions);
            Assert.IsTrue(SaveSystem.IsDiscovered("a"));
            Assert.IsFalse(SaveSystem.IsDiscovered("c"));
        }

        [Test]
        public void ResetProgress_KeepsSettings()
        {
            SaveSystem.Settings.sound = false;
            SaveSystem.Record("a");
            SaveSystem.ResetProgress();
            Assert.AreEqual(0, SaveSystem.DiscoveredCount);
            Assert.IsFalse(SaveSystem.Settings.sound);
        }

        [Test]
        public void RegisterLaunch_FirstOfTheDay()
        {
            var d = new System.DateTime(2026, 11, 11, 9, 0, 0);
            Assert.IsTrue(SaveSystem.RegisterLaunch(d));
            Assert.IsFalse(SaveSystem.RegisterLaunch(d.AddHours(3)));
            Assert.IsTrue(SaveSystem.RegisterLaunch(d.AddDays(1)));
            Assert.AreEqual(3, SaveSystem.Progress.launches);
        }

        [Test]
        public void Progress_RoundTripsThroughJson()
        {
            SaveSystem.Record("x");
            SaveSystem.Record("x");
            SaveSystem.Record("y");
            string json = JsonUtility.ToJson(SaveSystem.Progress);
            var back = JsonUtility.FromJson<ProgressData>(json);
            CollectionAssert.AreEquivalent(new[] { "x", "y" }, back.discovered);
            Assert.AreEqual(back.countIds.Count, back.counts.Count);
            Assert.AreEqual(2, back.counts[back.countIds.IndexOf("x")]);
        }
    }

    public class ThemeAndSensorTests
    {
        [Test]
        public void Themes_HaveSeveralChoicesAndClamp()
        {
            Assert.AreEqual(1, Themes.Iris.Length, "黒目は黒だけ（模様も色えらびもない）");
            Assert.Less(Themes.Iris[0].outer.maxColorComponent, 0.15f);
            Assert.GreaterOrEqual(Themes.Lid.Length, 6);
            Assert.AreEqual(Themes.Iris[0].name, Themes.IrisOf(-5).name);
            Assert.AreEqual(Themes.Lid[Themes.Lid.Length - 1].name, Themes.LidOf(99).name);
            foreach (var t in Themes.Iris) Assert.IsFalse(string.IsNullOrEmpty(t.name));
            foreach (var t in Themes.Lid) Assert.IsFalse(string.IsNullOrEmpty(t.name));
        }

        [Test]
        public void ScreenGravity_IsDownAtRestInEveryOrientation()
        {
            // たてにまっすぐ持ったとき、センサーは端末の -Y に重力。横にしたときは端末の ±X
            Assert.AreEqual(new Vector2(0f, -1f), GyoInput.ScreenGravity(new Vector3(0f, -1f, 0f), ScreenOrientation.Portrait));
            Assert.AreEqual(new Vector2(0f, -1f), GyoInput.ScreenGravity(new Vector3(0f, 1f, 0f), ScreenOrientation.PortraitUpsideDown));
            Assert.AreEqual(new Vector2(0f, -1f), GyoInput.ScreenGravity(new Vector3(-1f, 0f, 0f), ScreenOrientation.LandscapeLeft));
            Assert.AreEqual(new Vector2(0f, -1f), GyoInput.ScreenGravity(new Vector3(1f, 0f, 0f), ScreenOrientation.LandscapeRight));
        }

        [Test]
        public void ScreenGravity_RightTiltIsPositiveX()
        {
            // たて：右を下げると端末の +X に重力
            Assert.Greater(GyoInput.ScreenGravity(new Vector3(0.5f, -0.86f, 0f), ScreenOrientation.Portrait).x, 0.4f);
            // 横（ホームボタン右 = LandscapeLeft）：画面の右 = 端末の -Y
            Assert.Greater(GyoInput.ScreenGravity(new Vector3(-0.86f, -0.5f, 0f), ScreenOrientation.LandscapeLeft).x, 0.4f);
            Assert.Greater(GyoInput.ScreenGravity(new Vector3(0.86f, 0.5f, 0f), ScreenOrientation.LandscapeRight).x, 0.4f);
        }

        [Test]
        public void Zones_MatchAsDocumented()
        {
            Assert.IsTrue(Reaction.ZoneMatches(Zone.Any, Zone.Background));
            Assert.IsTrue(Reaction.ZoneMatches(Zone.Eye, Zone.Pupil));
            Assert.IsTrue(Reaction.ZoneMatches(Zone.Eye, Zone.EyeTop));
            Assert.IsTrue(Reaction.ZoneMatches(Zone.Eye, Zone.LeftEye));
            Assert.IsTrue(Reaction.ZoneMatches(Zone.Eye, Zone.BothEyes));
            Assert.IsFalse(Reaction.ZoneMatches(Zone.Eye, Zone.Background));
            Assert.IsFalse(Reaction.ZoneMatches(Zone.Background, Zone.Eye));
            Assert.IsFalse(Reaction.ZoneMatches(Zone.Pupil, Zone.LeftEye));
            Assert.IsTrue(Reaction.ZoneMatches(Zone.Pupil, Zone.Pupil));
        }
    }
}
