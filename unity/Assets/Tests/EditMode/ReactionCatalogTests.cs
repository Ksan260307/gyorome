using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Gyorome.Tests
{
    /// <summary>単体テスト：反応のカタログ（100 しゅ以上・id の重複なし・きっかけのもれなし）。</summary>
    public class ReactionCatalogTests
    {
        [Test]
        public void HasAtLeastOneHundredReactions()
        {
            Assert.GreaterOrEqual(ReactionCatalog.Count, GyoConst.ReactionGoal, "反応は 100 しゅ以上");
        }

        [Test]
        public void IdsAreUniqueAndWellFormed()
        {
            var ids = ReactionCatalog.All.Select(r => r.Id).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count(), "id が重複");
            foreach (var id in ids) Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-z0-9_]+$"), id);
        }

        [Test]
        public void EveryReactionHasNameHintCategoryAndPerformer()
        {
            foreach (var r in ReactionCatalog.All)
            {
                Assert.IsFalse(string.IsNullOrEmpty(r.Name), r.Id + " に名前がない");
                Assert.IsFalse(string.IsNullOrEmpty(r.Hint), r.Id + " にヒントがない");
                Assert.IsFalse(string.IsNullOrEmpty(r.Category), r.Id + " に分類がない");
                Assert.IsNotNull(r.Play, r.Id + " に演技がない");
                Assert.GreaterOrEqual(r.Priority, 1, r.Id);
                Assert.Greater(r.Weight, 0f, r.Id);
                if (r.IsIdle) Assert.IsTrue(r.AcceptsGesture(GestureType.Idle), r.Id);
                else Assert.Greater(r.Gestures.Length, 0, r.Id + " にきっかけがない");
                Assert.IsNotNull(ReactionCatalog.Find(r.Id));
            }
        }

        [Test]
        public void NamesAreMostlyUnique()
        {
            var names = ReactionCatalog.All.Select(r => r.Name).ToList();
            int dup = names.Count - names.Distinct().Count();
            Assert.LessOrEqual(dup, 3, "同じ名前の反応が多すぎる: " + string.Join(", ", names.GroupBy(n => n).Where(g => g.Count() > 1).Select(g => g.Key)));
        }

        [Test]
        public void EveryTriggerGestureHasAReaction()
        {
            var triggers = new[]
            {
                GestureType.Tap, GestureType.LongPressBegin, GestureType.Swipe, GestureType.Flick, GestureType.DragBegin, GestureType.Rub,
                GestureType.Circle, GestureType.PinchBegin, GestureType.TwoFingerTap, GestureType.TwoFingerSwipe, GestureType.RotateBegin,
                GestureType.Shake, GestureType.TiltBegin, GestureType.Idle,
            };
            foreach (var g in triggers)
                Assert.IsTrue(ReactionCatalog.All.Any(r => r.AcceptsGesture(g)), g + " に反応がない");
        }

        [Test]
        public void NoReactionListensToStreamUpdatesOrEnds()
        {
            var bad = new[] { GestureType.LongPressHold, GestureType.LongPressEnd, GestureType.Drag, GestureType.DragEnd, GestureType.Pinch, GestureType.PinchEnd, GestureType.Rotate, GestureType.RotateEnd, GestureType.Tilt, GestureType.TiltEnd };
            foreach (var r in ReactionCatalog.All)
                foreach (var g in bad)
                    Assert.IsFalse(r.AcceptsGesture(g), $"{r.Id} は {g} ではなく、はじまりのジェスチャーで始める");
        }

        [Test]
        public void EachCategoryHasEnoughVariety()
        {
            var groups = ReactionCatalog.All.GroupBy(r => r.Category).ToDictionary(g => g.Key, g => g.Count());
            foreach (var kv in groups) Assert.GreaterOrEqual(kv.Value, 5, kv.Key);
            Assert.GreaterOrEqual(groups.Count, 6);
        }

        [Test]
        public void TapsOnEyeAndBackgroundAlwaysHaveACandidate()
        {
            // 条件つき（気分・回数）でない、いつでも出る反応が、目にも外にもある
            Assert.IsTrue(ReactionCatalog.All.Any(r => r.AcceptsGesture(GestureType.Tap) && r.Zone == Zone.Eye && r.Priority == 1));
            Assert.IsTrue(ReactionCatalog.All.Any(r => r.AcceptsGesture(GestureType.Tap) && r.Zone == Zone.Background && r.Priority == 1));
            Assert.IsTrue(ReactionCatalog.All.Any(r => r.AcceptsGesture(GestureType.Swipe) && r.Zone == Zone.Background));
            Assert.IsTrue(ReactionCatalog.All.Any(r => r.AcceptsGesture(GestureType.LongPressBegin) && r.Zone == Zone.Background));
            Assert.IsTrue(ReactionCatalog.All.Any(r => r.AcceptsGesture(GestureType.DragBegin) && r.Zone == Zone.Background));
        }

        [Test]
        public void WakeStartle_BeatsEverythingWhileAsleep()
        {
            var wake = ReactionCatalog.Find("combo_wake_startle");
            Assert.IsNotNull(wake);
            foreach (var r in ReactionCatalog.All)
            {
                if (r == wake || r.IsIdle) continue;
                if (r.Gestures.Any(g => wake.AcceptsGesture(g)))
                    Assert.LessOrEqual(r.Priority, wake.Priority, r.Id + " はねむっているときの「びっくり」より強くてはいけない");
            }
        }

        [Test]
        public void IdleReactions_AreNotPickedByGestures()
        {
            var ctx = new ReactionContext { Event = new GestureEvent { type = GestureType.Tap }, Zone = Zone.Eye };
            foreach (var r in ReactionCatalog.All.Where(r => r.IsIdle))
                Assert.IsFalse(r.Matches(ctx), r.Id);
        }

        [Test]
        public void ZoneSpecificReactions_DoNotMatchOtherZones()
        {
            var ctxBg = new ReactionContext { Event = new GestureEvent { type = GestureType.Tap, count = 1 }, Zone = Zone.Background };
            var pupil = ReactionCatalog.Find("tap_center_bullseye");
            Assert.IsFalse(pupil.Matches(ctxBg));
            var ctxPupil = new ReactionContext { Event = new GestureEvent { type = GestureType.Tap, count = 1 }, Zone = Zone.Pupil };
            Assert.IsTrue(pupil.Matches(ctxPupil));
            Assert.IsTrue(ReactionCatalog.Find("tap_flinch").Matches(ctxPupil), "ひとみも目のうち");
            Assert.IsFalse(ReactionCatalog.Find("tap_flinch").Matches(ctxBg));
        }

        [Test]
        public void HundredthReaction_ExistsAndGoalIsHonest()
        {
            Assert.AreEqual(100, GyoConst.ReactionGoal);
            Assert.GreaterOrEqual(ReactionCatalog.All.Count(r => !r.IsIdle), 90, "さわって出る反応だけでもほぼ 100");
        }
    }
}
