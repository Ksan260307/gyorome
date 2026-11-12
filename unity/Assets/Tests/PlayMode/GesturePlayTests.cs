using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static Gyorome.Tests.GameHarness;

namespace Gyorome.Tests
{
    /// <summary>総合テスト：さわり方ごとに、それに合った反応が出る（指 → ジェスチャー → 反応 → 目）。</summary>
    public class GesturePlayTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return Boot();
            Brain.Paused = true;
        }

        [TearDown]
        public void TearDown() => Shutdown();

        static Reaction Last => ReactionCatalog.Find(Director.LastId);

        [UnityTest, Timeout(60000)]
        public IEnumerator TapOnEye_PlaysATapReactionOnThatEye()
        {
            yield return Tap(EyeA);
            Assert.AreEqual(1, Director.PlayedCount);
            Assert.IsTrue(Last.AcceptsGesture(GestureType.Tap));
            Assert.AreNotEqual(Zone.Background, Last.Zone);
            Assert.IsTrue(Director.Busy);
            Assert.IsTrue(SaveSystem.IsDiscovered(Last.Id), "見つけた反応は記録される");
            Assert.AreEqual(1, SaveSystem.DiscoveredCount);
            yield return WaitIdle();
            Assert.IsFalse(Director.Busy);
            Assert.AreEqual(Vector3.one, Eyes.A.ScaleTarget, "終わるともとの形");
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator TapOnBackground_PlaysABackgroundReaction()
        {
            yield return Tap(BgPoint);
            Assert.AreEqual(Zone.Background, Last.Zone);
            yield return WaitIdle();
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator DoubleTap_PlaysADoubleTapReaction()
        {
            yield return Tap(EyeB);
            yield return Seconds(0.12f);
            yield return Tap(EyeB);
            Assert.IsTrue(Last.Id.StartsWith("dtap_"), Last.Id);
            yield return WaitIdle();
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator LongPress_RunsWhileHeldAndEndsOnRelease()
        {
            Down(EyeA);
            yield return Seconds(0.7f);
            Assert.IsTrue(Director.Busy, "長押しの反応が始まる");
            Assert.IsTrue(Last.AcceptsGesture(GestureType.LongPressBegin), Last.Id);
            yield return Seconds(0.5f);
            Assert.IsTrue(Director.Busy, "押している間はつづく");
            Up(EyeA);
            yield return WaitIdle(10f);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator SwipeUpOnEye_PlaysASwipeReaction()
        {
            yield return Swipe(EyeA + new Vector2(0f, -RadiusA * 0.3f), EyeA + new Vector2(0f, RadiusA * 0.5f), 0.15f);
            Assert.IsTrue(Last.AcceptsGesture(GestureType.Swipe) || Last.AcceptsGesture(GestureType.Flick), Last.Id);
            yield return WaitIdle();
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator SwipeFromEyeToEye_Hops()
        {
            yield return Swipe(EyeA, EyeB, 0.1f, 4);
            Assert.AreEqual("combo_swipe_between", Director.LastId);
            yield return WaitIdle();
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator DragOnBackground_EyesFollowTheFinger()
        {
            Vector2 start = BgPoint;
            Down(start);
            Vector2 p = start;
            // 右へ、ゆっくり（スワイプにならない速さ）
            for (int i = 0; i < 12; i++)
            {
                yield return Seconds(0.08f);
                p += new Vector2(Screen.width * 0.04f, Screen.height * 0.02f);
                Move(p);
            }
            yield return Frames(3);
            Assert.AreEqual("drag_follow", Director.LastId);
            Vector2 expect = Eyes.A.YawPitchTo(Eyes.ScreenToWorld(p));
            Assert.AreEqual(expect.x, Eyes.A.GazeTarget.x, 2f, "指の方を見る");
            Up(p);
            yield return WaitIdle();
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator PinchOnBackground_PlaysAPinchReaction()
        {
            yield return Pinch(BgPoint + new Vector2(60f, 60f), 60f, 200f);
            Assert.IsTrue(Last.AcceptsGesture(GestureType.PinchBegin), Last.Id);
            Assert.AreEqual("pinch_out_wide", Director.LastId);
            yield return WaitIdle();
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator TwoFingerTapOnBothEyes_SquishesBoth()
        {
            yield return TwoFingerTap(EyeA, EyeB);
            Assert.AreEqual("two_tap_both_squish", Director.LastId);
            yield return WaitIdle();
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator ShakeAndTilt_PlayDeviceReactions()
        {
            GyoIn.VirtualShake(1f);
            yield return Frames(2);
            Assert.IsTrue(Last.AcceptsGesture(GestureType.Shake), Last.Id);
            yield return WaitIdle();
            GyoIn.VirtualTilt(new Vector2(1f, 0f) * 0.5f);
            yield return Frames(2);
            Assert.AreEqual("tilt_roll_gravity", Director.LastId);
            yield return Seconds(0.3f);
            Assert.IsTrue(Director.Busy, "かたむけている間はつづく");
            GyoIn.VirtualTilt(Vector2.zero);
            yield return WaitIdle(10f);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator MoodChangesWhichReactionPlays()
        {
            Eyes.SetMood(Mood.Angry, 30f);
            yield return Tap(EyeA);
            Assert.AreEqual("combo_tap_after_angry", Director.LastId);
            yield return WaitIdle();
            Eyes.SetMood(Mood.Normal, 0f);
            Eyes.ResetToNeutral();
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator NewGestureInterruptsTheCurrentReaction()
        {
            yield return Tap(EyeA);
            string first = Director.LastId;
            yield return Seconds(0.1f);
            yield return Swipe(BgPoint, BgPoint + new Vector2(Screen.width * 0.3f, 0f), 0.1f);
            Assert.AreNotEqual(first, Director.LastId);
            Assert.GreaterOrEqual(Director.PlayedCount, 2);
            yield return WaitIdle();
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator SameEyeTenTimes_GetsRefused()
        {
            for (int i = 0; i < 10; i++)
            {
                yield return Tap(EyeB);
                yield return Seconds(0.45f);
            }
            Assert.AreEqual("combo_tap_10_times_same_eye", Director.LastId);
            yield return WaitIdle();
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator RhythmicTaps_AreNoticed()
        {
            for (int i = 0; i < 6; i++)
            {
                yield return Tap(BgPoint);
                yield return Seconds(0.6f);
            }
            Assert.IsTrue(Director.History.Contains("combo_rhythm"), string.Join(",", Director.History));
            yield return WaitIdle();
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator UndiscoveredReactionsAreFavoured()
        {
            // 同じ目を何度もタップすると、いろいろな反応が出る（同じものばかりにならない）
            var seen = new System.Collections.Generic.HashSet<string>();
            Vector2 p = EyeA + new Vector2(RadiusA * 0.5f, 0f);   // ひとみのまん中（「ドキッ」）は外す
            for (int i = 0; i < 8; i++)
            {
                yield return Tap(p);
                seen.Add(Director.LastId);
                Director.Stop();
                yield return Seconds(i % 2 == 0 ? 0.6f : 0.95f);   // 同じテンポ（「のりのり」）にならないように
            }
            Assert.GreaterOrEqual(seen.Count, 3, string.Join(",", seen));
        }
    }
}
