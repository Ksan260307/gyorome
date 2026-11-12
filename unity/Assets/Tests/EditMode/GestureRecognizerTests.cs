using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Gyorome.Tests
{
    /// <summary>単体テスト：指の動きから、タップ・長押し・スワイプ・こする・円・ピンチ・回転を見分ける。</summary>
    public class GestureRecognizerTests
    {
        GestureRecognizer _r;
        readonly List<GestureEvent> _e = new List<GestureEvent>();

        [SetUp]
        public void SetUp()
        {
            _r = new GestureRecognizer { tapMaxDist = 20f, swipeMinDist = 60f, flickSpeed = 1800f, circleMinSize = 70f };
            _e.Clear();
        }

        List<GestureEvent> Drain()
        {
            _e.Clear();
            _r.Drain(_e);
            return _e;
        }

        static Vector2 P(float x, float y) => new Vector2(x, y);

        [Test]
        public void Tap_ShortPressWithoutMovement()
        {
            _r.Down(0, P(100, 100), 0f);
            _r.Tick(0.05f);
            _r.Up(0, P(103, 101), 0.1f);
            var e = Drain();
            Assert.AreEqual(1, e.Count);
            Assert.AreEqual(GestureType.Tap, e[0].type);
            Assert.AreEqual(1, e[0].count);
            Assert.AreEqual(0, _r.ActivePointers);
        }

        [Test]
        public void DoubleAndTripleTap_CountUp()
        {
            _r.Down(0, P(100, 100), 0f); _r.Up(0, P(100, 100), 0.08f);
            _r.Down(0, P(104, 100), 0.25f); _r.Up(0, P(104, 100), 0.32f);
            _r.Down(0, P(100, 104), 0.5f); _r.Up(0, P(100, 104), 0.58f);
            var e = Drain().Where(x => x.type == GestureType.Tap).ToList();
            Assert.AreEqual(3, e.Count);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, e.Select(x => x.count).ToArray());
        }

        [Test]
        public void TapsFarApartInTime_ResetCount()
        {
            _r.Down(0, P(100, 100), 0f); _r.Up(0, P(100, 100), 0.08f);
            _r.Down(0, P(100, 100), 1.0f); _r.Up(0, P(100, 100), 1.08f);
            var e = Drain().Where(x => x.type == GestureType.Tap).ToList();
            Assert.AreEqual(2, e.Count);
            Assert.AreEqual(1, e[1].count);
        }

        [Test]
        public void LongPress_BeginHoldEnd()
        {
            _r.Down(0, P(50, 50), 0f);
            _r.Tick(0.3f);
            Assert.AreEqual(0, Drain().Count, "0.3 秒ではまだ長押しではない");
            _r.Tick(0.5f);
            var e = Drain();
            Assert.AreEqual(1, e.Count);
            Assert.AreEqual(GestureType.LongPressBegin, e[0].type);
            _r.Tick(0.66f);
            e = Drain();
            Assert.AreEqual(1, e.Count);
            Assert.AreEqual(GestureType.LongPressHold, e[0].type);
            Assert.AreEqual(0.66f, e[0].duration, 0.01f);
            _r.Up(0, P(52, 50), 1.0f);
            e = Drain();
            Assert.AreEqual(1, e.Count);
            Assert.AreEqual(GestureType.LongPressEnd, e[0].type);
            Assert.IsFalse(e.Any(x => x.type == GestureType.Tap), "長押しのあとはタップにならない");
        }

        [Test]
        public void Swipe_Right_HasDirectionAndLength()
        {
            _r.Down(0, P(0, 0), 0f);
            _r.Move(0, P(40, 0), 0.05f);
            _r.Move(0, P(90, 2), 0.1f);
            _r.Up(0, P(90, 2), 0.15f);
            var e = Drain();
            var swipe = e.FirstOrDefault(x => x.type == GestureType.Swipe);
            Assert.AreEqual(GestureType.Swipe, swipe.type);
            Assert.Greater(swipe.dir.x, 0.9f);
            Assert.AreEqual(90f, swipe.magnitude, 1f);
            Assert.IsFalse(e.Any(x => x.type == GestureType.Flick));
            Assert.IsFalse(e.Any(x => x.type == GestureType.Tap));
        }

        [Test]
        public void Flick_FastSwipeIsFlick()
        {
            _r.Down(0, P(0, 0), 0f);
            _r.Move(0, P(120, 0), 0.02f);
            _r.Up(0, P(120, 0), 0.04f);
            var e = Drain();
            Assert.IsTrue(e.Any(x => x.type == GestureType.Flick));
            Assert.IsFalse(e.Any(x => x.type == GestureType.Swipe));
        }

        [Test]
        public void SlowDrag_IsDragNotSwipe()
        {
            _r.Down(0, P(0, 0), 0f);
            for (int i = 1; i <= 12; i++) _r.Move(0, P(i * 15f, 0), i * 0.1f);
            _r.Up(0, P(180, 0), 1.3f);
            var e = Drain();
            Assert.AreEqual(1, e.Count(x => x.type == GestureType.DragBegin));
            Assert.Greater(e.Count(x => x.type == GestureType.Drag), 5);
            Assert.AreEqual(1, e.Count(x => x.type == GestureType.DragEnd));
            Assert.IsFalse(e.Any(x => x.type == GestureType.Swipe), "ゆっくりはスワイプではない");
            Assert.IsFalse(e.Any(x => x.type == GestureType.Tap));
        }

        [Test]
        public void Rub_BackAndForth()
        {
            _r.Down(0, P(100, 100), 0f);
            float t = 0f;
            int dir = 1;
            float x = 100f;
            for (int leg = 0; leg < 5; leg++)
            {
                for (int i = 0; i < 4; i++)
                {
                    t += 0.03f;
                    x += dir * 15f;
                    _r.Move(0, P(x, 100 + (i % 2)), t);
                }
                dir = -dir;
            }
            _r.Up(0, P(x, 100), t + 0.03f);
            var e = Drain();
            Assert.GreaterOrEqual(e.Count(v => v.type == GestureType.Rub), 1);
            Assert.IsFalse(e.Any(v => v.type == GestureType.Circle));
        }

        void Circle(bool clockwise, int steps = 26)
        {
            Vector2 c = P(200, 200);
            float r = 80f;
            float a0 = Mathf.PI / 2f;
            _r.Down(0, c + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * r, 0f);
            for (int i = 1; i <= steps; i++)
            {
                float a = a0 + (clockwise ? -1f : 1f) * i * 15f * Mathf.Deg2Rad;
                _r.Move(0, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, i * 0.04f);
            }
            _r.Up(0, c, steps * 0.04f + 0.04f);
        }

        [Test]
        public void Circle_Clockwise_IsPositive()
        {
            Circle(true);
            var e = Drain();
            var circ = e.Where(x => x.type == GestureType.Circle).ToList();
            Assert.GreaterOrEqual(circ.Count, 1);
            Assert.Greater(circ[0].magnitude, 0f);
        }

        [Test]
        public void Circle_CounterClockwise_IsNegative()
        {
            Circle(false);
            var circ = Drain().Where(x => x.type == GestureType.Circle).ToList();
            Assert.GreaterOrEqual(circ.Count, 1);
            Assert.Less(circ[0].magnitude, 0f);
        }

        [Test]
        public void PinchOut_BeginUpdateEnd_NoTapForRemainingFinger()
        {
            _r.Down(0, P(100, 100), 0f);
            _r.Down(1, P(140, 100), 0.01f);
            _r.Move(1, P(200, 100), 0.1f);
            _r.Move(1, P(240, 100), 0.15f);
            _r.Up(1, P(240, 100), 0.2f);
            _r.Up(0, P(100, 100), 0.25f);
            var e = Drain();
            Assert.AreEqual(1, e.Count(x => x.type == GestureType.PinchBegin));
            Assert.Greater(e.Count(x => x.type == GestureType.Pinch), 0);
            Assert.Greater(e.Last(x => x.type == GestureType.Pinch).magnitude, 2f);
            Assert.AreEqual(1, e.Count(x => x.type == GestureType.PinchEnd));
            Assert.IsFalse(e.Any(x => x.type == GestureType.Tap), "2 本指のあとの指をはなしてもタップにならない");
            Assert.IsFalse(e.Any(x => x.type == GestureType.Swipe));
            Assert.IsFalse(e.Any(x => x.type == GestureType.TwoFingerTap));
        }

        [Test]
        public void PinchIn_HasMagnitudeBelowOne()
        {
            _r.Down(0, P(100, 100), 0f);
            _r.Down(1, P(300, 100), 0.01f);
            _r.Move(1, P(200, 100), 0.1f);
            _r.Up(1, P(200, 100), 0.2f);
            _r.Up(0, P(100, 100), 0.22f);
            var e = Drain();
            Assert.Less(e.First(x => x.type == GestureType.PinchBegin).magnitude, 1f);
        }

        [Test]
        public void TwoFingerTap()
        {
            _r.Down(0, P(100, 100), 0f);
            _r.Down(1, P(300, 100), 0.02f);
            _r.Up(0, P(100, 100), 0.12f);
            _r.Up(1, P(300, 100), 0.14f);
            var e = Drain();
            Assert.AreEqual(1, e.Count(x => x.type == GestureType.TwoFingerTap));
            Assert.IsFalse(e.Any(x => x.type == GestureType.Tap));
            var t = e.First(x => x.type == GestureType.TwoFingerTap);
            Assert.AreEqual(2, t.fingers);
            Assert.AreNotEqual(t.pos, t.pos2);
        }

        [Test]
        public void TwoFingerSwipe()
        {
            _r.Down(0, P(100, 100), 0f);
            _r.Down(1, P(100, 200), 0.01f);
            for (int i = 1; i <= 4; i++)
            {
                _r.Move(0, P(100 + i * 30f, 100), i * 0.04f);
                _r.Move(1, P(100 + i * 30f, 200), i * 0.04f + 0.005f);
            }
            _r.Up(0, P(220, 100), 0.2f);
            _r.Up(1, P(220, 200), 0.21f);
            var e = Drain();
            var s = e.FirstOrDefault(x => x.type == GestureType.TwoFingerSwipe);
            Assert.AreEqual(GestureType.TwoFingerSwipe, s.type);
            Assert.Greater(s.dir.x, 0.9f);
        }

        [Test]
        public void Rotate_CounterClockwiseIsNegative()
        {
            _r.Down(0, P(100, 100), 0f);
            _r.Down(1, P(200, 100), 0.01f);
            _r.Move(1, P(190, 140), 0.1f);
            _r.Move(1, P(170, 170), 0.15f);
            _r.Up(1, P(170, 170), 0.3f);
            _r.Up(0, P(100, 100), 0.32f);
            var e = Drain();
            Assert.AreEqual(1, e.Count(x => x.type == GestureType.RotateBegin));
            Assert.Less(e.Last(x => x.type == GestureType.Rotate).magnitude, -30f);
            Assert.AreEqual(1, e.Count(x => x.type == GestureType.RotateEnd));
        }

        [Test]
        public void Cancel_EmitsNothing()
        {
            _r.Down(0, P(10, 10), 0f);
            _r.Cancel(0, 0.1f);
            Assert.AreEqual(0, Drain().Count);
            Assert.AreEqual(0, _r.ActivePointers);
        }

        [Test]
        public void SecondFinger_EndsOngoingDragAndLongPress()
        {
            _r.Down(0, P(0, 0), 0f);
            for (int i = 1; i <= 4; i++) _r.Move(0, P(i * 20f, 0), i * 0.1f);
            Drain();
            _r.Down(1, P(200, 200), 0.5f);
            var e = Drain();
            Assert.IsTrue(e.Any(x => x.type == GestureType.DragEnd));

            _r.Clear();
            _r.Down(0, P(0, 0), 0f);
            _r.Tick(0.6f);
            Drain();
            _r.Down(1, P(200, 200), 0.7f);
            e = Drain();
            Assert.IsTrue(e.Any(x => x.type == GestureType.LongPressEnd));
        }

        [Test]
        public void MovingAfterLongPress_IsDragUpdate()
        {
            _r.Down(0, P(0, 0), 0f);
            _r.Tick(0.6f);
            Drain();
            _r.Move(0, P(30, 0), 0.7f);
            var e = Drain();
            Assert.AreEqual(GestureType.Drag, e[0].type);
            _r.Up(0, P(30, 0), 0.8f);
            e = Drain();
            Assert.AreEqual(GestureType.LongPressEnd, e[0].type);
        }
    }
}
