using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static Gyorome.Tests.GameHarness;

namespace Gyorome.Tests
{
    /// <summary>総合テスト：さわらないでいると、まばたき・きょろきょろ・たいくつ・うとうと・ねむる。さわると目がさめる。</summary>
    public class IdlePlayTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return Boot(4f);
        }

        [TearDown]
        public void TearDown() => Shutdown();

        [UnityTest, Timeout(120000)]
        public IEnumerator BlinksAndLooksAroundOnItsOwn()
        {
            yield return Seconds(14f);
            Assert.GreaterOrEqual(Brain.IdleBlinks, 1, "14 秒のうちに 1 回はまばたき");
            Assert.GreaterOrEqual(Brain.GazeChanges, 1, "きょろきょろ");
            Assert.IsTrue(SaveSystem.IsDiscovered("idle_blink"), "まばたきは図鑑に記録される");
            yield return WaitUntil(() => !Eyes.A.Blinking, 3f, "まばたきが終わる");
            Assert.Greater(Eyes.A.Openness, 0.6f, "ふだんは目をあけている");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator GetsBoredDrowsyAndFallsAsleep_ThenWakesStartled()
        {
            Brain.BoredAfter = 2f;
            Brain.DrowsyAfter = 4f;
            Brain.SleepAfter = 6f;
            yield return WaitUntil(() => Eyes.Mood == Mood.Bored || SaveSystem.IsDiscovered("idle_bored"), 20f, "たいくつ");
            yield return WaitUntil(() => Eyes.Mood == Mood.Asleep, 30f, "ねむる");
            Assert.IsTrue(SaveSystem.IsDiscovered("idle_drowsy"));
            Assert.IsTrue(SaveSystem.IsDiscovered("idle_sleep"));
            yield return Seconds(1.5f);
            Assert.Less(Eyes.A.Openness, 0.2f, "ねむっているときは目を閉じる");
            // さわると、びっくりして目がさめる（そのあとすぐ また たいくつにならないように、時間をもどす）
            Brain.BoredAfter = 15f;
            Brain.DrowsyAfter = 30f;
            Brain.SleepAfter = 45f;
            yield return Tap(EyeA);
            yield return WaitUntil(() => Director.LastId == "combo_wake_startle", 3f, "びっくり");
            yield return WaitIdle();
            Assert.AreNotEqual(Mood.Asleep, Eyes.Mood);
            yield return Seconds(1f);
            yield return WaitUntil(() => !Eyes.A.Blinking, 3f, "まばたきが終わる");
            Assert.Greater(Eyes.A.Openness, 0.6f, "目がさめた");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator Director_PlaysIdleReactionsThroughTheBehaviour()
        {
            Brain.Paused = true;
            Director.PlayIdle("idle_yawn");
            Assert.IsTrue(Director.Busy);
            Assert.AreEqual("idle_yawn", Director.LastId);
            yield return WaitIdle();
            Assert.IsTrue(SaveSystem.IsDiscovered("idle_yawn"));
            Assert.AreEqual(1f, Eyes.A.UpperTarget, "終わるともどる");
        }
    }
}
