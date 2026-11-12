using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gyorome
{
    /// <summary>
    /// ジェスチャーを受けて、カタログから反応をえらんで演じる。
    ///  - 同じきっかけに複数の反応があるときは、優先度の高いものの中から、重みつきで選ぶ（まだ見つけていないものを出やすく）
    ///  - 指をはなすまでつづく操作（長押し・ドラッグ・ピンチ・回転・かたむき）は、演じている反応に流す
    ///  - あとから来たジェスチャーは、同じか高い優先度なら今の反応を止めて始める
    ///  - 演じた反応は図鑑（SaveSystem）に記録する
    /// </summary>
    public class ReactionDirector : MonoBehaviour
    {
        public EyePair eyes;
        public GyoInput input;
        public AudioManager audioManager;
        public Emotes emotes;
        public EyeBehaviour behaviour;

        public Reaction Current { get; private set; }
        public bool Busy => Current != null;
        public int PlayedCount { get; private set; }
        public string LastId { get; private set; }
        public float LastPlayedTime { get; private set; } = -100f;
        public readonly List<string> History = new List<string>();
        public event Action<Reaction, bool> Played;
        public event Action<string, EyeRig> NameShown;
        public System.Random Rng = new System.Random();
        /// <summary>テスト用：反応をえらぶ乱数を固定する。</summary>
        public int Seed { set => Rng = new System.Random(value); }

        readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();
        readonly List<Reaction> _candidates = new List<Reaction>();
        Coroutine _routine;
        ReactionContext _ctx;
        GestureType _streamBegin;

        // 文脈
        readonly List<(float time, int eye, GestureType type)> _recent = new List<(float, int, GestureType)>();
        int _tickles, _rubs, _shakes;
        float _lastIdleBeforeTouch;

        void OnEnable()
        {
            if (input != null) input.Gesture += Handle;
        }

        void OnDisable()
        {
            if (input != null) input.Gesture -= Handle;
        }

        // ------------------------------------------------------------------
        public void Handle(GestureEvent e)
        {
            if (eyes == null || eyes.A == null) return;
            // 演じている反応に、つづきの操作を流す
            if (_ctx != null && Current != null)
            {
                if (IsStreamUpdate(e.type, _streamBegin))
                {
                    _ctx.Latest = e;
                    return;
                }
                if (IsStreamEnd(e.type, _streamBegin))
                {
                    _ctx.Latest = e;
                    _ctx.StreamActive = false;
                    return;
                }
            }
            if (IsStreamUpdateOnly(e.type) || IsStreamEndOnly(e.type)) return;

            var ctx = BuildContext(e);
            if (behaviour != null && !ctx.Asleep) behaviour.WakeUp();
            _candidates.Clear();
            int best = int.MinValue;
            foreach (var r in ReactionCatalog.All)
            {
                if (!r.Matches(ctx)) continue;
                if (r.CooldownSeconds > 0f && _lastPlayed.TryGetValue(r.Id, out var t) && Time.time - t < r.CooldownSeconds) continue;
                if (r.Priority > best)
                {
                    best = r.Priority;
                    _candidates.Clear();
                }
                if (r.Priority == best) _candidates.Add(r);
            }
            if (_candidates.Count == 0) return;
            if (Current != null && best < Current.Priority && _ctx != null && _ctx.StreamActive) return;
            var chosen = Pick(_candidates);
            Play(chosen, ctx);
        }

        Reaction Pick(List<Reaction> list)
        {
            if (list.Count == 1) return list[0];
            float total = 0f;
            var weights = new float[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                var r = list[i];
                float w = r.Weight * (SaveSystem.IsDiscovered(r.Id) ? 1f : 2.5f);
                if (r.Id == LastId) w *= 0.3f;
                weights[i] = w;
                total += w;
            }
            double x = Rng.NextDouble() * total;
            for (int i = 0; i < list.Count; i++)
            {
                x -= weights[i];
                if (x <= 0) return list[i];
            }
            return list[list.Count - 1];
        }

        static bool IsStreamUpdate(GestureType t, GestureType begin)
        {
            switch (begin)
            {
                case GestureType.LongPressBegin: return t == GestureType.LongPressHold || t == GestureType.Drag;
                case GestureType.DragBegin: return t == GestureType.Drag;
                case GestureType.PinchBegin: return t == GestureType.Pinch || t == GestureType.Rotate || t == GestureType.RotateBegin;
                case GestureType.RotateBegin: return t == GestureType.Rotate || t == GestureType.Pinch || t == GestureType.PinchBegin;
                case GestureType.TiltBegin: return t == GestureType.Tilt;
                default: return false;
            }
        }

        static bool IsStreamEnd(GestureType t, GestureType begin)
        {
            switch (begin)
            {
                case GestureType.LongPressBegin: return t == GestureType.LongPressEnd || t == GestureType.DragEnd;
                case GestureType.DragBegin: return t == GestureType.DragEnd;
                case GestureType.PinchBegin: return t == GestureType.PinchEnd;
                case GestureType.RotateBegin: return t == GestureType.RotateEnd;
                case GestureType.TiltBegin: return t == GestureType.TiltEnd;
                default: return false;
            }
        }

        static bool IsStreamUpdateOnly(GestureType t) => t == GestureType.LongPressHold || t == GestureType.Drag || t == GestureType.Pinch || t == GestureType.Rotate || t == GestureType.Tilt;
        static bool IsStreamEndOnly(GestureType t) => t == GestureType.LongPressEnd || t == GestureType.DragEnd || t == GestureType.PinchEnd || t == GestureType.RotateEnd || t == GestureType.TiltEnd;
        public static bool IsStreamBegin(GestureType t) => t == GestureType.LongPressBegin || t == GestureType.DragBegin || t == GestureType.PinchBegin || t == GestureType.RotateBegin || t == GestureType.TiltBegin;

        /// <summary>ねむった・目がさめた（EyeBehaviour の idle の進み具合に合わせる）。</summary>
        public void MarkAsleep() => behaviour?.ForceSleep();
        public void MarkAwake() => behaviour?.WakeUp();

        // ------------------------------------------------------------------
        public ReactionContext BuildContext(GestureEvent e)
        {
            var ctx = new ReactionContext
            {
                Eyes = eyes, Event = e, Latest = e, Director = this, Audio = audioManager, Fx = emotes, Rng = Rng,
                Screen = e.pos, World = eyes.ScreenToWorld(e.pos),
                IdleSeconds = input != null ? Time.time - input.LastInputTime : 0f,
                LastReactionId = LastId, SinceLastReaction = Time.time - LastPlayedTime,
            };
            int hit = eyes.Hit(e.pos);
            int hit2 = e.fingers >= 2 ? eyes.Hit(e.pos2) : -2;
            if (e.type == GestureType.Shake || e.type == GestureType.TiltBegin || e.type == GestureType.Tilt || e.type == GestureType.TiltEnd)
            {
                ctx.Zone = Zone.Any;
                ctx.Target = eyes.A;
                ctx.Other = eyes.B;
            }
            else if (hit >= 0)
            {
                ctx.Target = hit == 0 ? eyes.A : eyes.B;
                ctx.Other = eyes.Other(ctx.Target);
                var p = eyes.PlacementOf(ctx.Target);
                Vector2 local = (e.pos - p.center) / Mathf.Max(1f, p.radius);
                if (hit2 >= 0 && hit2 != hit) ctx.Zone = Zone.BothEyes;
                else if (local.magnitude < 0.3f) ctx.Zone = Zone.Pupil;
                else if (local.y > 0.4f) ctx.Zone = Zone.EyeTop;
                else if (local.y < -0.4f) ctx.Zone = Zone.EyeBottom;
                else ctx.Zone = ctx.Target.isLeft ? Zone.LeftEye : Zone.RightEye;
            }
            else
            {
                ctx.Zone = Zone.Background;
                ctx.Target = eyes.Nearest(e.pos);
                ctx.Other = eyes.Other(ctx.Target);
            }

            // 文脈：最近のタップ
            float now = Time.time;
            _recent.RemoveAll(r => now - r.time > 6f);
            int streak = 0;
            float prev = now;
            for (int i = _recent.Count - 1; i >= 0; i--)
            {
                if (_recent[i].type != GestureType.Tap || _recent[i].eye != hit) break;
                if (prev - _recent[i].time > 1.5f) break;   // 1.5 秒以上あくと、つづけてタップしたことにならない
                prev = _recent[i].time;
                streak++;
            }
            ctx.SameEyeTapStreak = e.type == GestureType.Tap ? streak + 1 : streak;
            if (e.type == GestureType.Tap)
            {
                ctx.Rhythmic = IsRhythmic(now);
                if (hit >= 0)
                    for (int i = _recent.Count - 1; i >= 0; i--)
                    {
                        if (now - _recent[i].time > 0.45f) break;
                        if (_recent[i].type == GestureType.Tap && _recent[i].eye >= 0 && _recent[i].eye != hit) { ctx.DoublePoke = true; break; }
                    }
            }
            ctx.TicklesInRow = _tickles;
            ctx.RubsInRow = e.type == GestureType.Rub ? _rubs + 1 : _rubs;
            ctx.ShakesInRow = e.type == GestureType.Shake ? _shakes + 1 : _shakes;
            ctx.FirstTouchAfterLongIdle = ctx.IdleSeconds > 60f || _lastIdleBeforeTouch > 60f;
            if (e.type == GestureType.Tap || e.type == GestureType.Swipe || e.type == GestureType.Flick || e.type == GestureType.LongPressBegin || e.type == GestureType.Rub || e.type == GestureType.Shake)
                _recent.Add((now, hit, e.type));
            bool tilt = e.type == GestureType.TiltBegin || e.type == GestureType.Tilt || e.type == GestureType.TiltEnd;
            if (e.type == GestureType.Rub) _rubs++; else if (!tilt && e.type != GestureType.Shake) _rubs = 0;
            if (e.type == GestureType.Shake) _shakes++; else if (!tilt) _shakes = 0;
            if (!tilt) _lastIdleBeforeTouch = ctx.IdleSeconds;
            return ctx;
        }

        bool IsRhythmic(float now)
        {
            var times = new List<float>();
            for (int i = _recent.Count - 1; i >= 0 && times.Count < 4; i--)
                if (_recent[i].type == GestureType.Tap) times.Add(_recent[i].time);
            if (times.Count < 3) return false;
            times.Add(now);
            times.Sort();
            float a = times[times.Count - 1] - times[times.Count - 2];
            float b = times[times.Count - 2] - times[times.Count - 3];
            float c = times[times.Count - 3] - times[times.Count - 4];
            if (a < 0.25f || a > 1.2f) return false;
            float tol = Mathf.Max(0.12f, a * 0.3f);   // テンポの 3 割までのゆれは同じテンポ
            return Mathf.Abs(a - b) < tol && Mathf.Abs(b - c) < tol;
        }

        /// <summary>こちょこちょの回数を記録する（反応から呼ぶ）。</summary>
        public void NoteTickle() => _tickles++;
        public void ResetTickles() => _tickles = 0;

        // ------------------------------------------------------------------
        public void Play(Reaction r, ReactionContext ctx)
        {
            Stop();
            Current = r;
            _ctx = ctx;
            _streamBegin = ctx.Event.type;
            ctx.StreamActive = IsStreamBegin(ctx.Event.type);
            _lastPlayed[r.Id] = Time.time;
            LastId = r.Id;
            LastPlayedTime = Time.time;
            PlayedCount++;
            History.Add(r.Id);
            if (History.Count > 20) History.RemoveAt(0);
            bool isNew = SaveSystem.Record(r.Id);
            if (!string.IsNullOrEmpty(r.Name) && !r.IsIdle) ShowName(r.Name, ctx.Target ?? eyes.A);
            Played?.Invoke(r, isNew);
            _routine = StartCoroutine(Run(r, ctx));
        }

        IEnumerator Run(Reaction r, ReactionContext ctx)
        {
            IEnumerator it = null;
            try { it = r.Play(ctx); }
            catch (Exception ex) { Debug.LogException(ex); }
            if (it != null)
            {
                while (true)
                {
                    object cur;
                    try
                    {
                        if (!it.MoveNext()) break;
                        cur = it.Current;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        break;
                    }
                    yield return cur;
                }
            }
            if (Current == r)
            {
                Current = null;
                _ctx = null;
                _routine = null;
                eyes.ResetToNeutral();
            }
        }

        public void Stop()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            if (Current != null)
            {
                Current = null;
                _ctx = null;
                eyes.ResetToNeutral();
            }
        }

        /// <summary>id の反応を、今のジェスチャーで演じる（テスト・デバッグ用）。</summary>
        public bool PlayById(string id, GestureEvent e)
        {
            var r = ReactionCatalog.Find(id);
            if (r == null) return false;
            var ctx = BuildContext(e);
            Play(r, ctx);
            return true;
        }

        /// <summary>idle のふるまいを反応として演じる（EyeBehaviour から）。</summary>
        public void PlayIdle(string id)
        {
            if (Busy) return;
            var r = ReactionCatalog.Find(id);
            if (r == null) return;
            var e = new GestureEvent { type = GestureType.Idle, pos = eyes.PlacementA.center, time = Time.time };
            var ctx = BuildContext(e);
            ctx.Zone = Zone.Any;
            Play(r, ctx);
        }

        /// <summary>idle のふるまい（まばたきなど、演じるのは EyeBehaviour）を図鑑に記録する。</summary>
        public void ReportIdle(string id)
        {
            var r = ReactionCatalog.Find(id);
            if (r == null) return;
            if (SaveSystem.IsDiscovered(id)) return;   // まばたきのたびに保存しない
            SaveSystem.Record(id);
            History.Add(id);
            Played?.Invoke(r, true);
        }

        public void ShowName(string text, EyeRig at) => NameShown?.Invoke(text, at);

        /// <summary>今の反応をすぐ終える（テスト用）。</summary>
        public void Interrupt() => Stop();

        void Update()
        {
            // 反応が終わったあと、指がはなれたのに stream が残っていたら片づける
            if (Current == null && _ctx != null) _ctx = null;
        }
    }
}
