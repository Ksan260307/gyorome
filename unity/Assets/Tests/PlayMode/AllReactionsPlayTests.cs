using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static Gyorome.Tests.GameHarness;

namespace Gyorome.Tests
{
    /// <summary>総合テスト：カタログのすべての反応が、最後まで（例外なく）演じられ、図鑑に記録される。</summary>
    public class AllReactionsPlayTests
    {
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return Boot(8f);
            Brain.Paused = true;
        }

        [TearDown]
        public void TearDown() => Shutdown();

        [UnityTest, Timeout(900000)]
        public IEnumerator EveryReactionPlaysToTheEnd()
        {
            int n = 0;
            foreach (var r in ReactionCatalog.All)
            {
                var e = EventFor(r);
                Assert.IsTrue(Director.PlayById(r.Id, e), r.Id);
                Assert.AreEqual(r.Id, Director.LastId);
                Assert.IsTrue(SaveSystem.IsDiscovered(r.Id), r.Id + " が記録されない");
                yield return Seconds(0.4f);
                if (ReactionDirector.IsStreamBegin(e.type)) Director.Handle(EndOf(e));
                yield return WaitUntil(() => !Director.Busy, 60f, r.Id + " が終わらない");
                n++;
            }
            Assert.AreEqual(ReactionCatalog.Count, n);
            Assert.AreEqual(ReactionCatalog.Count, SaveSystem.DiscoveredCount, "ぜんぶ見つけた");
            Assert.GreaterOrEqual(n, GyoConst.ReactionGoal);
            // 終わったあとは、ふつうの目にもどっている
            yield return Seconds(1f);
            foreach (var eye in Eyes.Both)
            {
                Assert.AreEqual(Vector3.one, eye.ScaleTarget, eye.name);
                Assert.AreEqual(Vector2.zero, eye.OffsetTarget, eye.name);
            }
            Assert.GreaterOrEqual(GM.audioManager.PlayedCount, 50, "音も鳴る");
            Assert.GreaterOrEqual(GM.emotes.SpawnedEmotes, 20, "記号も出る");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator StopResetsTheEyes()
        {
            var e = EventFor(ReactionCatalog.Find("hold_pressure"));
            Director.PlayById("hold_pressure", e);
            yield return Seconds(2f);
            Assert.Greater(Eyes.A.ScaleTarget.x, 1.01f, "ふくらんでいる");
            Director.Stop();
            Assert.IsFalse(Director.Busy);
            Assert.AreEqual(Vector3.one, Eyes.A.ScaleTarget);
            Assert.AreEqual(Color.white, Eyes.A.TintTarget);
        }
    }
}
