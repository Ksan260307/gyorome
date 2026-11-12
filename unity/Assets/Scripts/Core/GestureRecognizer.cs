using System.Collections.Generic;
using UnityEngine;

namespace Gyorome
{
    public enum GestureType
    {
        Tap,            // count = 1, 2, 3...（すばやく続けた回数）
        LongPressBegin,
        LongPressHold,  // duration = 押している秒数
        LongPressEnd,
        Swipe,          // dir = 向き、magnitude = 長さ（px）
        Flick,          // はやいスワイプ
        DragBegin,
        Drag,           // delta = 前回からの移動
        DragEnd,
        Rub,            // 行ったり来たり（count = 往復の数）
        Circle,         // magnitude = +1 時計回り / -1 反時計回り
        PinchBegin,
        Pinch,          // magnitude = 2 本の指の間の、はじめに対する倍率
        PinchEnd,
        TwoFingerTap,
        TwoFingerSwipe, // dir = 向き
        RotateBegin,
        Rotate,         // magnitude = はじめからの回転（度、時計回りが正）
        RotateEnd,
        Shake,          // magnitude = 強さ
        TiltBegin,      // かたむけはじめた。dir = かたむき（x: 右が正、y: 上が正）。magnitude = 大きさ 0..1
        Tilt,           // かたむいている間（0.1 秒ごと）
        TiltEnd,
        Idle,           // さわらない時間がつづいた（duration = 秒）
    }

    /// <summary>ひとつのジェスチャー。pos は画面座標（左下が原点）。</summary>
    public struct GestureEvent
    {
        public GestureType type;
        public Vector2 pos;
        public Vector2 pos2;       // 2 本目の指（2 本指のとき）
        public Vector2 delta;
        public Vector2 dir;
        public float magnitude;
        public int count;
        public float duration;
        public int fingers;
        public float time;
        public int pointerId;

        public override string ToString() => $"{type} pos={pos} dir={dir} mag={magnitude:0.00} count={count} dur={duration:0.00}";
    }

    /// <summary>
    /// ポインター（指・マウス）の押す・動く・はなす から、タップ・長押し・スワイプ・こする・円・ピンチ・回転などを見分ける。
    /// Unity に依存しない（Vector2 だけ）ので EditMode で単体テストできる。
    /// </summary>
    public class GestureRecognizer
    {
        public float tapMaxDist = 24f;        // これより動いたらタップではない（px）
        public float tapMaxTime = 0.32f;
        public float multiTapInterval = 0.36f;
        public float longPressTime = 0.45f;
        public float holdEventInterval = 0.1f;
        public float swipeMinDist = 60f;
        public float swipeMaxTime = 0.6f;
        public float flickSpeed = 1800f;      // px/s
        public float dragStartTime = 0.1f;
        public int rubReversals = 3;
        public float rubCooldown = 0.6f;
        public float circleMinSize = 70f;
        public float pinchThreshold = 0.1f;
        public float rotateThresholdDeg = 18f;

        class Track
        {
            public int id;
            public Vector2 start, last, prev;
            public float startTime, lastTime;
            public bool moved, dragging, longPress, inMulti, cancelled;
            public float nextHoldEvent;
            public float pathLength;
            public int reversals;
            public Vector2 lastDir;
            public float lastRub = -10f;
            public float angleAccum;
            public Vector2 bboxMin, bboxMax;
            public float lastAngle;
            public bool hasAngle;
            public Vector2 centroid;
            public int samples;
            public float lastCircle = -10f;
        }

        readonly Dictionary<int, Track> _tracks = new Dictionary<int, Track>();
        readonly List<int> _order = new List<int>();
        readonly List<GestureEvent> _out = new List<GestureEvent>();

        // 連続タップ
        int _tapCount;
        float _lastTapTime = -10f;
        Vector2 _lastTapPos;

        // 2 本指
        bool _multi;
        float _multiStartDist, _multiStartAngle, _multiStartTime;
        Vector2 _multiStartCentroid;
        bool _pinching, _rotating, _multiMoved;
        float _multiRotateAccum, _multiLastAngle;
        float _lastPinchScale = 1f;

        public int ActivePointers => _tracks.Count;
        public bool IsPinching => _pinching;
        public bool IsRotating => _rotating;

        /// <summary>たまったイベントを取り出す（呼ぶたびに空になる）。</summary>
        public void Drain(List<GestureEvent> into)
        {
            into.AddRange(_out);
            _out.Clear();
        }

        public void Clear()
        {
            _tracks.Clear();
            _order.Clear();
            _out.Clear();
            _multi = _pinching = _rotating = _multiMoved = false;
            _tapCount = 0;
        }

        void Emit(GestureType t, Vector2 pos, float time, Vector2 delta = default, Vector2 dir = default, float mag = 0f, int count = 0, float dur = 0f, int fingers = 1, Vector2 pos2 = default, int pointerId = 0)
        {
            _out.Add(new GestureEvent { type = t, pos = pos, pos2 = pos2, delta = delta, dir = dir, magnitude = mag, count = count, duration = dur, fingers = fingers, time = time, pointerId = pointerId });
        }

        // ------------------------------------------------------------------
        public void Down(int id, Vector2 pos, float time)
        {
            if (_tracks.ContainsKey(id)) Up(id, pos, time);
            var t = new Track
            {
                id = id, start = pos, last = pos, prev = pos, startTime = time, lastTime = time,
                nextHoldEvent = time + longPressTime, bboxMin = pos, bboxMax = pos, centroid = pos, samples = 1,
            };
            _tracks[id] = t;
            _order.Add(id);
            if (_tracks.Count == 2)
            {
                BeginMulti(time);
            }
            else if (_tracks.Count > 2)
            {
                t.inMulti = true;
            }
        }

        public void Move(int id, Vector2 pos, float time)
        {
            if (!_tracks.TryGetValue(id, out var t)) return;
            Vector2 d = pos - t.last;
            if (d.sqrMagnitude < 1e-6f) return;
            t.prev = t.last;
            t.last = pos;
            t.lastTime = time;
            t.pathLength += d.magnitude;
            t.bboxMin = Vector2.Min(t.bboxMin, pos);
            t.bboxMax = Vector2.Max(t.bboxMax, pos);
            t.samples++;
            t.centroid += (pos - t.centroid) / Mathf.Min(t.samples, 24);
            if (!t.moved && (pos - t.start).magnitude > tapMaxDist) t.moved = true;

            if (_multi)
            {
                UpdateMulti(time);
                return;
            }
            if (t.inMulti) return;

            if (t.moved && !t.longPress)
            {
                if (!t.dragging && time - t.startTime >= dragStartTime)
                {
                    t.dragging = true;
                    Emit(GestureType.DragBegin, pos, time, pointerId: id);
                }
                if (t.dragging) Emit(GestureType.Drag, pos, time, delta: d, dir: d.normalized, mag: d.magnitude, dur: time - t.startTime, pointerId: id);
                DetectRub(t, d, time);
                DetectCircle(t, pos, time);
            }
            else if (t.longPress)
            {
                // 長押しのまま動かすのは、ドラッグ扱い（押しながらなでる）
                Emit(GestureType.Drag, pos, time, delta: d, dir: d.normalized, mag: d.magnitude, dur: time - t.startTime, pointerId: id);
            }
        }

        public void Up(int id, Vector2 pos, float time)
        {
            if (!_tracks.TryGetValue(id, out var t)) return;
            _tracks.Remove(id);
            _order.Remove(id);
            if (_multi)
            {
                EndMulti(t, pos, time);
                return;
            }
            if (t.inMulti || t.cancelled) return;

            float dur = time - t.startTime;
            if (t.longPress)
            {
                Emit(GestureType.LongPressEnd, pos, time, dur: dur, pointerId: id);
                return;
            }
            if (!t.moved)
            {
                if (dur <= tapMaxTime)
                {
                    if (time - _lastTapTime <= multiTapInterval && (pos - _lastTapPos).magnitude <= tapMaxDist * 3f) _tapCount++;
                    else _tapCount = 1;
                    _lastTapTime = time;
                    _lastTapPos = pos;
                    Emit(GestureType.Tap, pos, time, count: _tapCount, dur: dur, pointerId: id);
                }
                return;
            }
            Vector2 disp = pos - t.start;
            float dist = disp.magnitude;
            float speed = dist / Mathf.Max(0.016f, dur);
            if (t.dragging)
            {
                Vector2 vel = (pos - t.prev) / Mathf.Max(0.016f, time - t.lastTime + 0.016f);
                Emit(GestureType.DragEnd, pos, time, delta: vel, dir: disp.normalized, mag: dist, dur: dur, pointerId: id);
            }
            if (dist >= swipeMinDist && dur <= swipeMaxTime)
            {
                Emit(speed >= flickSpeed ? GestureType.Flick : GestureType.Swipe, t.start, time, delta: disp, dir: disp.normalized, mag: dist, dur: dur, pointerId: id, pos2: pos);
            }
        }

        public void Cancel(int id, float time)
        {
            if (!_tracks.TryGetValue(id, out var t)) return;
            t.cancelled = true;
            Up(id, t.last, time);
        }

        /// <summary>毎フレーム呼ぶ：長押しの開始・継続を出す。</summary>
        public void Tick(float time)
        {
            if (_multi) return;
            foreach (var t in _tracks.Values)
            {
                if (t.inMulti || t.moved) continue;
                if (!t.longPress && time - t.startTime >= longPressTime)
                {
                    t.longPress = true;
                    t.nextHoldEvent = time + holdEventInterval;
                    Emit(GestureType.LongPressBegin, t.last, time, pointerId: t.id);
                }
                else if (t.longPress && time >= t.nextHoldEvent)
                {
                    t.nextHoldEvent = time + holdEventInterval;
                    Emit(GestureType.LongPressHold, t.last, time, dur: time - t.startTime, pointerId: t.id);
                }
            }
        }

        // ------------------------------------------------------------------
        void DetectRub(Track t, Vector2 d, float time)
        {
            if (d.magnitude < 2f) return;
            Vector2 dir = d.normalized;
            if (t.lastDir != Vector2.zero && Vector2.Dot(dir, t.lastDir) < -0.3f) t.reversals++;
            t.lastDir = dir;
            if (t.reversals >= rubReversals && t.pathLength >= tapMaxDist * 4f && time - t.lastRub >= rubCooldown)
            {
                t.lastRub = time;
                t.reversals = 0;
                Vector2 size = t.bboxMax - t.bboxMin;
                Emit(GestureType.Rub, t.centroid, time, dir: size.x >= size.y ? Vector2.right : Vector2.up, mag: t.pathLength, count: rubReversals, dur: time - t.startTime, pointerId: t.id);
            }
        }

        void DetectCircle(Track t, Vector2 pos, float time)
        {
            // 進む向きの変化（曲がった角度）を足していく：円を 1 周すると、どこを中心に描いても 360 度になる。
            // 行ったり来たりは ±180 度で打ち消し合うので、円にはならない
            Vector2 step = pos - t.prev;
            if (step.magnitude < 2f) return;
            float heading = Mathf.Atan2(step.y, step.x) * Mathf.Rad2Deg;
            if (t.hasAngle)
            {
                float da = Mathf.DeltaAngle(t.lastAngle, heading);
                if (Mathf.Abs(da) < 150f) t.angleAccum += da;
            }
            t.lastAngle = heading;
            t.hasAngle = true;
            Vector2 size = t.bboxMax - t.bboxMin;
            Vector2 c = (t.bboxMax + t.bboxMin) * 0.5f;
            if (Mathf.Abs(t.angleAccum) >= 300f && size.x >= circleMinSize && size.y >= circleMinSize && time - t.lastCircle >= 0.4f)
            {
                t.lastCircle = time;
                float sign = t.angleAccum > 0f ? -1f : 1f;   // 画面座標（y 上向き）で左に曲がりつづける = 反時計回り
                t.angleAccum = 0f;
                t.reversals = 0;
                Emit(GestureType.Circle, c, time, mag: sign, dur: time - t.startTime, pointerId: t.id);
            }
        }

        // ------------------------------------------------------------------
        void BeginMulti(float time)
        {
            _multi = true;
            _multiMoved = false;
            _pinching = _rotating = false;
            _multiRotateAccum = 0f;
            _lastPinchScale = 1f;
            var a = _tracks[_order[0]];
            var b = _tracks[_order[1]];
            // 1 本目がすでにドラッグ中なら、いったん終える
            if (a.dragging)
            {
                Emit(GestureType.DragEnd, a.last, time, pointerId: a.id);
                a.dragging = false;
            }
            if (a.longPress)
            {
                Emit(GestureType.LongPressEnd, a.last, time, dur: time - a.startTime, pointerId: a.id);
                a.longPress = false;
            }
            a.inMulti = b.inMulti = true;
            Vector2 d = b.last - a.last;
            _multiStartDist = Mathf.Max(1f, d.magnitude);
            _multiStartAngle = _multiLastAngle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            _multiStartCentroid = (a.last + b.last) * 0.5f;
            _multiStartTime = time;
        }

        void UpdateMulti(float time)
        {
            if (_order.Count < 2) return;
            var a = _tracks[_order[0]];
            var b = _tracks[_order[1]];
            Vector2 d = b.last - a.last;
            Vector2 centroid = (a.last + b.last) * 0.5f;
            float scale = Mathf.Max(1f, d.magnitude) / _multiStartDist;
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            _multiRotateAccum += Mathf.DeltaAngle(_multiLastAngle, ang);
            _multiLastAngle = ang;
            if ((centroid - _multiStartCentroid).magnitude > tapMaxDist) _multiMoved = true;

            if (!_pinching && Mathf.Abs(scale - 1f) >= pinchThreshold)
            {
                _pinching = true;
                Emit(GestureType.PinchBegin, centroid, time, mag: scale, fingers: 2, pos2: b.last);
            }
            if (_pinching)
            {
                Emit(GestureType.Pinch, centroid, time, mag: scale, delta: new Vector2(scale - _lastPinchScale, 0f), fingers: 2, pos2: b.last, dur: time - _multiStartTime);
                _lastPinchScale = scale;
            }
            float rot = -_multiRotateAccum;   // 時計回りを正に
            if (!_rotating && Mathf.Abs(rot) >= rotateThresholdDeg)
            {
                _rotating = true;
                Emit(GestureType.RotateBegin, centroid, time, mag: rot, fingers: 2, pos2: b.last);
            }
            if (_rotating) Emit(GestureType.Rotate, centroid, time, mag: rot, fingers: 2, pos2: b.last, dur: time - _multiStartTime);
        }

        void EndMulti(Track released, Vector2 pos, float time)
        {
            Track other = null;
            foreach (var t in _tracks.Values) { other = t; break; }
            Vector2 otherPos = other != null ? other.last : pos;
            Vector2 centroid = (pos + otherPos) * 0.5f;
            float dur = time - _multiStartTime;
            if (_pinching) Emit(GestureType.PinchEnd, centroid, time, mag: _lastPinchScale, fingers: 2, pos2: otherPos, dur: dur);
            if (_rotating) Emit(GestureType.RotateEnd, centroid, time, mag: -_multiRotateAccum, fingers: 2, pos2: otherPos, dur: dur);
            if (!_pinching && !_rotating)
            {
                Vector2 disp = centroid - _multiStartCentroid;
                if (!_multiMoved && dur <= tapMaxTime * 1.5f && !released.moved)
                    Emit(GestureType.TwoFingerTap, pos, time, fingers: 2, pos2: otherPos, dur: dur);
                else if (disp.magnitude >= swipeMinDist && dur <= swipeMaxTime * 1.5f)
                    Emit(GestureType.TwoFingerSwipe, _multiStartCentroid, time, delta: disp, dir: disp.normalized, mag: disp.magnitude, fingers: 2, pos2: otherPos, dur: dur);
            }
            _multi = false;
            _pinching = _rotating = false;
            // 残った指は、はなしてもタップにしない
            if (other != null) other.inMulti = true;
            _tapCount = 0;
        }
    }
}
