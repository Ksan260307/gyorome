using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gyorome
{
    /// <summary>
    /// 反応のカタログ。partial クラスに分けて、きっかけごとに定義する：
    ///   Tap / Swipe / Hold / Multi（2 本指・ピンチ・回転）/ Device（ふる・かたむける）/ Idle / Combo（文脈）
    /// </summary>
    public static partial class ReactionCatalog
    {
        static List<Reaction> _all;
        static Dictionary<string, Reaction> _byId;

        public static IReadOnlyList<Reaction> All
        {
            get
            {
                if (_all == null) Build();
                return _all;
            }
        }

        public static Reaction Find(string id)
        {
            if (_byId == null) Build();
            return _byId.TryGetValue(id, out var r) ? r : null;
        }

        public static int Count => All.Count;

        static void Build()
        {
            _all = new List<Reaction>();
            _byId = new Dictionary<string, Reaction>();
            BuildTap();
            BuildSwipe();
            BuildHold();
            BuildMulti();
            BuildDevice();
            BuildIdle();
            BuildCombo();
            foreach (var r in _all)
            {
                if (_byId.ContainsKey(r.Id)) throw new InvalidOperationException("反応の id が重複: " + r.Id);
                _byId[r.Id] = r;
            }
        }

        static Reaction R(string id, string name, string category, string hint, Func<ReactionContext, IEnumerator> play, Zone zone, params GestureType[] gestures)
        {
            var r = new Reaction { Id = id, Name = name, Category = category, Hint = hint, Play = play, Zone = zone, Gestures = gestures };
            _all.Add(r);
            return r;
        }

        static Reaction Idle(string id, string name, string hint, Func<ReactionContext, IEnumerator> play)
        {
            var r = new Reaction { Id = id, Name = name, Category = "ひとりでに", Hint = hint, Play = play, Zone = Zone.Any, Gestures = new[] { GestureType.Idle }, IsIdle = true };
            _all.Add(r);
            return r;
        }

        // ------------------------------------------------------------------
        // よく使う動きの部品
        static IEnumerator Nothing() { yield break; }

        /// <summary>目をちぢめて、ぷるんともどる。</summary>
        static IEnumerator Squash(ReactionContext ctx, EyeRig e, float amount, float dur)
        {
            e.ScaleSmooth = 0.03f;
            yield return ctx.Over(dur, t =>
            {
                float k = Ease.Bump(t) * amount;
                e.ScaleTarget = new Vector3(1f + k * 0.6f, 1f - k, 1f);
            });
            e.ScaleSmooth = 0.08f;
            e.ScaleTarget = Vector3.one;
        }

        /// <summary>ぴょんと上にはねる。</summary>
        static IEnumerator Hop(ReactionContext ctx, EyeRig e, float height, float dur)
        {
            e.OffsetSmooth = 0.03f;
            yield return ctx.Over(dur, t =>
            {
                e.OffsetTarget = new Vector2(0f, Ease.Bump(t) * height);
                float k = 1f + 0.12f * Mathf.Sin(t * Mathf.PI);
                e.ScaleTarget = new Vector3(1f / k, k, 1f);
            });
            e.OffsetSmooth = 0.1f;
            e.OffsetTarget = Vector2.zero;
            e.ScaleTarget = Vector3.one;
        }

        /// <summary>指をはなすまで待ち、はなしたら元にもどる（ばね）。</summary>
        static IEnumerator ReleaseBounce(ReactionContext ctx, EyeRig e)
        {
            e.ScaleSmooth = 0.06f;
            e.ScaleTarget = Vector3.one;
            e.OffsetTarget = Vector2.zero;
            e.Kick(new Vector2(0f, 0.6f));
            ctx.Sfx("pop", ctx.Rand(0.9f, 1.1f));
            yield return ctx.Wait(0.5f);
        }

        static Vector2 Jitter(ReactionContext ctx, float amp) => new Vector2(ctx.Rand(-amp, amp), ctx.Rand(-amp, amp));

        static IEnumerator LookAround(ReactionContext ctx, int times, float step)
        {
            ctx.BothSmooth(0.04f);
            for (int i = 0; i < times; i++)
            {
                ctx.BothLookDir(new Vector2(ctx.Rand(-0.9f, 0.9f), ctx.Rand(-0.6f, 0.7f)));
                yield return ctx.Wait(step);
            }
            ctx.BothSmooth(0.09f);
        }
    }
}
