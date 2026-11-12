using System.Collections;
using UnityEngine;

namespace Gyorome
{
    public static partial class ReactionCatalog
    {
        static void BuildIdle()
        {
            // 演じるのは EyeBehaviour（記録だけ）
            Idle("idle_blink", "まばたき", "しばらく見ていると、まばたきをする", ctx => Nothing());
            Idle("idle_look_around", "きょろきょろ", "ときどき、あちこちを見る", ctx => Nothing());
            Idle("idle_sync_check", "ちら", "ふたつの目が、おたがいを見る", ctx => Nothing());
            Idle("idle_focus_bug", "ふよふよ", "見えない何かを目で追う", ctx => Nothing());
            Idle("idle_drowsy", "うとうと…", "30 秒くらい、さわらないでいる", ctx => Nothing());
            Idle("idle_sleep", "すやすや", "45 秒くらい、さわらないでいる", ctx => Nothing());
            Idle("idle_night_mode", "よるのめ", "夜おそく（23 時〜5 時）にひらく", ctx => Nothing());

            // 演じるのは Director（EyeBehaviour が頼む）
            Idle("idle_yawn", "ふぁ〜", "しばらく見ていると、あくびをする", ctx => IdleYawn(ctx));
            Idle("idle_bored", "ひま〜", "15 秒くらい、さわらないでいる", ctx => IdleBored(ctx));
            Idle("idle_dream", "むにゃむにゃ", "ねむっている目を見ていると、夢を見る", ctx => IdleDream(ctx));
            Idle("idle_wink_at_you", "ね？", "ずっと見ていると、たまにウインクしてくれる", ctx => IdleWink(ctx));
            Idle("idle_sneeze_random", "へくしっ", "ずっと見ていると、たまにくしゃみ", ctx => IdleSneeze(ctx));
            Idle("idle_morning_stretch", "おはよう", "その日はじめてひらく", ctx => IdleMorning(ctx));
        }

        // ------------------------------------------------------------------
        static IEnumerator IdleYawn(ReactionContext ctx)
        {
            ctx.Sfx("yawn", ctx.Rand(0.9f, 1.05f), 0.6f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.LidSmooth = 0.25f;
                e.Lids(0.1f, 0.6f);
                e.Look(new Vector2(0f, 12f));
                e.ScaleSmooth = 0.3f;
                e.ScaleTarget = new Vector3(0.97f, 1.05f, 1f);
            }
            yield return ctx.Wait(1.1f);
            ctx.Tear(ctx.Eyes.A, -1);
            foreach (var e in ctx.Eyes.Both)
            {
                e.LidSmooth = 0.3f;
                e.Lids(1f, 1f);
                e.ScaleTarget = Vector3.one;
                e.LookForward();
            }
            yield return ctx.Wait(0.9f);
        }

        static IEnumerator IdleBored(ReactionContext ctx)
        {
            ctx.SetMood(Mood.Bored, 10f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.GazeSmooth = 0.3f;
                e.Look(new Vector2(ctx.Rand(-15f, 15f), 40f));
                e.LidSmooth = 0.3f;
                e.Lids(0.55f, 1f);
            }
            ctx.Sfx("yawn", 1.3f, 0.25f);
            ctx.Emote("…", ctx.Eyes.B, new Vector2(0.7f, 0.8f), Emotes.Style.Soft);
            yield return ctx.Wait(1.5f);
            foreach (var e in ctx.Eyes.Both) e.Look(new Vector2(-e.GazeTarget.x, 35f));
            yield return ctx.Wait(1.2f);
        }

        static IEnumerator IdleDream(ReactionContext ctx)
        {
            // まぶたの下で目が動く
            foreach (var e in ctx.Eyes.Both) { e.GazeSmooth = 0.04f; e.Lids(0f, 0.6f); }
            for (int i = 0; i < 5; i++)
            {
                ctx.BothLook(new Vector2(ctx.Rand(-30f, 30f), ctx.Rand(-15f, 15f)));
                yield return ctx.Wait(ctx.Rand(0.12f, 0.3f));
            }
            ctx.Sfx("purr", 1.4f, 0.2f);
            ctx.Emote("…", ctx.Eyes.A, new Vector2(0.7f, 0.7f), Emotes.Style.Soft);
            ctx.BothLook(Vector2.zero);
            yield return ctx.Wait(0.4f);
        }

        static IEnumerator IdleWink(ReactionContext ctx)
        {
            var e = ctx.Chance(0.5f) ? ctx.Eyes.A : ctx.Eyes.B;
            ctx.Sfx("sparkle", 1.3f, 0.4f);
            e.LidSmooth = 0.04f;
            e.Lids(0f, 0.5f);
            ctx.Eyes.Other(e).Sparkle = 1f;
            ctx.Emote("★", e, new Vector2(0.7f, 0.8f));
            yield return ctx.Wait(0.5f);
            e.Lids(1f, 1f);
            yield return ctx.Wait(0.6f);
            ctx.Eyes.Other(e).Sparkle = 0f;
        }

        static IEnumerator IdleSneeze(ReactionContext ctx)
        {
            foreach (var e in ctx.Eyes.Both) { e.LidSmooth = 0.12f; e.Lids(0.5f, 0.7f); e.Look(new Vector2(0f, 20f)); }
            yield return ctx.Wait(0.6f);
            ctx.Sfx("sneeze", ctx.Rand(1.0f, 1.2f), 0.7f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.LidSmooth = 0.02f;
                e.Lids(0f, 0.3f);
                e.Kick(Vector2.down * 2.2f);
            }
            yield return ctx.Wait(0.35f);
            foreach (var e in ctx.Eyes.Both) { e.Lids(1f, 1f); e.LookForward(); }
            yield return ctx.Wait(0.5f);
        }

        static IEnumerator IdleMorning(ReactionContext ctx)
        {
            ctx.Sfx("yawn", 1.1f, 0.5f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.LidSmooth = 0.3f;
                e.Lids(0.1f, 0.6f);
                e.ScaleSmooth = 0.3f;
                e.ScaleTarget = new Vector3(0.96f, 1.06f, 1f);
            }
            yield return ctx.Wait(1.0f);
            ctx.Sfx("sparkle", 1f, 0.6f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.LidSmooth = 0.06f;
                e.Lids(1f, 1f);
                e.ScaleTarget = Vector3.one;
                e.Sparkle = 1f;
                e.Kick(Vector2.up * 1f);
            }
            ctx.Emote("☀", ctx.Eyes.A, new Vector2(0.6f, 1f));
            yield return ctx.Wait(1.2f);
            ctx.BothSparkle(0f);
        }
    }
}
