using System;
using System.Collections;
using UnityEngine;

namespace Gyorome
{
    /// <summary>どこをさわったか。</summary>
    public enum Zone
    {
        Any,        // どこでも
        Eye,        // どちらかの目（ひとみ・上・下もふくむ）
        LeftEye,
        RightEye,
        Background, // 目の外
        Pupil,      // 目のまん中
        EyeTop,     // 目の上の方
        EyeBottom,  // 目の下の方
        BothEyes,   // 2 本の指が、べつべつの目に
    }

    /// <summary>反応ひとつ：きっかけ（ジェスチャー・場所・条件）と、見せ方（コルーチン）。</summary>
    public sealed class Reaction
    {
        public string Id;
        public string Name;         // オノマトペ（画面に出す）
        public string Hint;         // 図鑑のヒント（まだ見つけていないとき）
        public string Category;     // 図鑑の見出し
        public GestureType[] Gestures = Array.Empty<GestureType>();
        public Zone Zone = Zone.Any;
        public Func<ReactionContext, bool> Condition;
        public int Priority = 1;
        public float CooldownSeconds;
        public float Weight = 1f;
        public bool IsIdle;
        public Func<ReactionContext, IEnumerator> Play;

        public Reaction When(Func<ReactionContext, bool> cond) { Condition = cond; return this; }
        public Reaction WithPriority(int p) { Priority = p; return this; }
        public Reaction Cooldown(float s) { CooldownSeconds = s; return this; }
        public Reaction WithWeight(float w) { Weight = w; return this; }

        public bool AcceptsGesture(GestureType g)
        {
            foreach (var x in Gestures) if (x == g) return true;
            return false;
        }

        public bool Matches(ReactionContext ctx)
        {
            if (IsIdle) return false;
            if (!AcceptsGesture(ctx.Event.type)) return false;
            if (!ZoneMatches(Zone, ctx.Zone)) return false;
            if (Condition != null && !Condition(ctx)) return false;
            return true;
        }

        public static bool ZoneMatches(Zone wanted, Zone actual)
        {
            switch (wanted)
            {
                case Zone.Any: return true;
                case Zone.Eye: return actual == Zone.Eye || actual == Zone.LeftEye || actual == Zone.RightEye || actual == Zone.Pupil || actual == Zone.EyeTop || actual == Zone.EyeBottom || actual == Zone.BothEyes;
                case Zone.Background: return actual == Zone.Background;
                default: return actual == wanted;
            }
        }

        public override string ToString() => $"{Id} ({Name})";
    }
}
