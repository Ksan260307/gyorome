using System.Collections;
using UnityEngine;

namespace Gyorome
{
    public static partial class ReactionCatalog
    {
        const string CatHold = "長押し";

        static void BuildHold()
        {
            R("hold_squish", "むぎゅ", CatHold, "目を長押し", ctx => HoldSquish(ctx), Zone.Eye, GestureType.LongPressBegin);
            R("hold_sleepy", "うとうと", CatHold, "目を長押しすると、ねむくなる", ctx => HoldSleepy(ctx), Zone.Eye, GestureType.LongPressBegin);
            R("hold_pressure", "ぷくー", CatHold, "目を長く長押し", ctx => HoldPressure(ctx), Zone.Eye, GestureType.LongPressBegin);
            R("hold_stare_contest", "にらめっこ", CatHold, "ひとみを長押しして、にらめっこ", ctx => HoldStare(ctx), Zone.Pupil, GestureType.LongPressBegin).WithPriority(2);
            R("hold_purr", "ごろごろ", CatHold, "目の上を長押し", ctx => HoldPurr(ctx), Zone.EyeTop, GestureType.LongPressBegin).WithPriority(2);
            R("hold_struggle", "じたばた", CatHold, "目をつかまえておく", ctx => HoldStruggle(ctx), Zone.Eye, GestureType.LongPressBegin);
            R("hold_release_pop", "ぽんっ", CatHold, "目を長押しして、ぱっとはなす", ctx => HoldReleasePop(ctx), Zone.Eye, GestureType.LongPressBegin).WithWeight(0.8f);
            R("bg_hold_suspicious", "じろじろ", CatHold, "目の外を長押し", ctx => BgHoldSuspicious(ctx), Zone.Background, GestureType.LongPressBegin);
            R("bg_hold_sleep", "すやすや", CatHold, "目の外を長く長押しすると、ねむる", ctx => BgHoldSleep(ctx), Zone.Background, GestureType.LongPressBegin);
            R("bg_hold_hypno", "ぐるぐる", CatHold, "目の外を長押しすると、目がまわる", ctx => BgHoldHypno(ctx), Zone.Background, GestureType.LongPressBegin).WithWeight(0.8f);
        }

        // ------------------------------------------------------------------
        static IEnumerator HoldSquish(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("squish", 0.9f, 0.8f);
            ctx.Haptic(25);
            e.ScaleSmooth = 0.06f;
            e.PupilSmooth = 0.06f;
            e.LidSmooth = 0.06f;
            e.Lids(0.6f, 0.8f);
            ctx.Other.LookAt(e.WorldCenter);
            yield return ctx.WhileHeld(() =>
            {
                Vector2 d = (ctx.Latest.pos - ctx.Eyes.PlacementOf(e).center) / Mathf.Max(1f, e.Radius);
                float push = Mathf.Clamp01(0.3f + ctx.Latest.duration * 0.2f);
                e.ScaleTarget = new Vector3(1f + push * 0.25f, 1f - push * 0.3f, 1f);
                e.OffsetSmooth = 0.08f;
                e.OffsetTarget = new Vector2(Mathf.Clamp(d.x, -1f, 1f) * 0.08f, -0.05f);
                e.PupilTarget = GyoConst.PupilNormal * (1f + push * 0.4f);
                e.LookAt(ctx.LatestWorld);
            });
            yield return ReleaseBounce(ctx, e);
            e.Lids(1f, 1f);
            yield return ctx.Wait(0.3f);
        }

        static IEnumerator HoldSleepy(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("yawn", 0.9f, 0.35f);
            e.LidSmooth = 0.4f;
            ctx.Other.LidSmooth = 0.6f;
            yield return ctx.WhileHeld(() =>
            {
                float k = Mathf.Clamp01(ctx.Latest.duration / 3f);
                e.Lids(1f - k, 1f - k * 0.4f);
                ctx.Other.Lids(1f - k * 0.8f, 1f - k * 0.3f);
                e.Look(new Vector2(0f, -10f * k));
                ctx.Other.Look(new Vector2(0f, -10f * k));
            }, 12f);
            yield return ctx.Wait(0.2f);
            ctx.Eyes.Blink(0.3f);
            ctx.BothLids(1f, 1f);
            ctx.Sfx("blink", 1f, 0.5f);
            yield return ctx.Wait(0.5f);
        }

        static IEnumerator HoldPressure(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("squish", 0.7f, 0.6f);
            e.ScaleSmooth = 0.3f;
            e.ColorSmooth = 0.3f;
            yield return ctx.WhileHeld(() =>
            {
                float k = Mathf.Clamp01(ctx.Latest.duration / 4f);
                e.ScaleTarget = Vector3.one * (1f + k * 0.28f);
                e.TintTarget = Color.Lerp(Color.white, new Color(1f, 0.6f, 0.6f), k);
                e.Lids(1f, 1f - k * 0.2f);
                e.PupilTarget = GyoConst.PupilNormal * (1f - k * 0.4f);
                if (k > 0.6f) ctx.Other.Lids(0.7f, 0.9f);
            }, 15f);
            ctx.Sfx("hiss", 1f, 0.8f);
            ctx.Haptic(30);
            e.ScaleSmooth = 0.1f;
            e.ScaleTarget = Vector3.one;
            e.TintTarget = Color.white;
            e.Kick(new Vector2(ctx.Rand(-1f, 1f), 1f) * 1.2f);
            ctx.Sweat(e);
            yield return ctx.Wait(0.9f);
        }

        static IEnumerator HoldStare(ReactionContext ctx)
        {
            var e = ctx.Target;
            e.GazeSmooth = 0.04f;
            e.LookAt(ctx.World);
            e.Lids(1f, 1f);
            e.PupilTarget = 0.3f;
            ctx.Other.LookAt(ctx.World);
            float t0 = Time.time;
            bool twitched = false;
            yield return ctx.WhileHeld(() =>
            {
                float held = Time.time - t0;
                if (held > 2.5f && !twitched)
                {
                    twitched = true;
                    ctx.Sfx("blink", 1.6f, 0.4f);
                }
                if (held > 2.5f) e.Lids(1f - Mathf.Abs(Mathf.Sin(held * 18f)) * 0.08f, 1f);
                if (held > 4f) e.TintTarget = new Color(1f, 0.85f, 0.85f);
            }, 10f);
            if (Time.time - t0 > 3f)
            {
                // まけた
                ctx.Sfx("drip", 1f, 0.6f);
                e.Blink(0.25f);
                ctx.Tear(e, 1);
                ctx.Emote("…", e, new Vector2(0.7f, 0.8f), Emotes.Style.Soft);
            }
            else
            {
                ctx.Sfx("pop", 1.2f, 0.6f);
                ctx.Emote("♪", e, new Vector2(0.7f, 0.85f));
            }
            yield return ctx.Wait(0.8f);
        }

        static IEnumerator HoldPurr(ReactionContext ctx)
        {
            ctx.Sfx("purr", 1f, 0.7f);
            ctx.SetMood(Mood.Happy, 6f);
            foreach (var e in ctx.Eyes.Both) { e.LidSmooth = 0.15f; e.Lids(0.2f, 0.55f); }
            float t0 = Time.time;
            float nextPulse = 0f;
            yield return ctx.WhileHeld(() =>
            {
                float s = Time.time - t0;
                foreach (var e in ctx.Eyes.Both)
                {
                    e.OffsetSmooth = 0.15f;
                    e.OffsetTarget = new Vector2(Mathf.Sin(s * 2.2f) * 0.03f, Mathf.Sin(s * 4.4f) * 0.015f);
                }
                if (s > nextPulse) { nextPulse = s + 0.35f; ctx.Haptic(10); }
                if (s > 1.2f && Mathf.Repeat(s, 1.3f) < Time.deltaTime) ctx.Sfx("purr", ctx.Rand(0.9f, 1.1f), 0.6f);
            }, 12f);
            ctx.Emote("♥", ctx.Target, new Vector2(0.65f, 0.9f));
            ctx.BothOffset(Vector2.zero);
            yield return ctx.Wait(0.7f);
        }

        static IEnumerator HoldStruggle(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("rattle", 0.9f, 0.6f);
            ctx.Other.TiltTarget = -12f;
            ctx.Other.Lids(0.8f, 1f);
            ctx.Other.LookAt(e.WorldCenter);
            float t0 = Time.time;
            yield return ctx.WhileHeld(() =>
            {
                float s = Time.time - t0;
                e.OffsetSmooth = 0.03f;
                e.OffsetTarget = new Vector2(Mathf.Sin(s * 19f) * 0.06f, Mathf.Abs(Mathf.Sin(s * 11f)) * 0.04f);
                e.GazeSmooth = 0.03f;
                e.Look(new Vector2(Mathf.Sin(s * 13f) * 30f, Mathf.Cos(s * 9f) * 15f));
                e.Lids(0.7f + Mathf.Sin(s * 15f) * 0.1f, 0.8f);
                if (Mathf.Repeat(s, 1.1f) < Time.deltaTime) ctx.Sfx("squish", ctx.Rand(1.1f, 1.4f), 0.4f);
            }, 10f);
            ctx.Sfx("whoosh", 1.2f, 0.5f);
            e.OffsetTarget = Vector2.zero;
            e.Kick(new Vector2(ctx.Rand(-1f, 1f), 1.2f) * 1.5f);
            e.Lids(1f, 1f);
            ctx.Sweat(e);
            yield return ctx.Wait(0.7f);
        }

        static IEnumerator HoldReleasePop(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("squish", 1.1f, 0.5f);
            e.ScaleSmooth = 0.2f;
            yield return ctx.WhileHeld(() =>
            {
                float k = Mathf.Clamp01(ctx.Latest.duration / 2f);
                e.ScaleTarget = new Vector3(1f + k * 0.12f, 1f - k * 0.18f, 1f);
                e.Lids(1f - k * 0.3f, 1f);
            }, 12f);
            ctx.Sfx("pop", 0.9f, 1f);
            ctx.Haptic(30);
            e.ScaleSmooth = 0.04f;
            e.ScaleTarget = Vector3.one;
            e.Kick(Vector2.up * 2.4f);
            e.Lids(1f, 1f);
            ctx.Other.Kick(Vector2.up * 0.6f);
            ctx.Emote("！", e, new Vector2(0.6f, 1.0f));
            yield return ctx.Wait(0.8f);
        }

        // ------------------------------------------------------------------
        static IEnumerator BgHoldSuspicious(ReactionContext ctx)
        {
            ctx.BothLids(0.55f, 1f);
            ctx.BothSmooth(0.3f);
            ctx.Sfx("blink", 0.8f, 0.4f);
            yield return ctx.WhileHeld(() => ctx.Eyes.LookAtScreen(ctx.Latest.pos), 12f);
            ctx.BothSmooth(0.09f);
            ctx.BothLids(1f, 1f);
            ctx.Emote("…", ctx.Target, new Vector2(0.7f, 0.8f), Emotes.Style.Soft);
            yield return ctx.Wait(0.6f);
        }

        static IEnumerator BgHoldSleep(ReactionContext ctx)
        {
            foreach (var e in ctx.Eyes.Both) { e.LidSmooth = 0.5f; }
            float t0 = Time.time;
            bool slept = false;
            yield return ctx.WhileHeld(() =>
            {
                float k = Mathf.Clamp01((Time.time - t0) / 3f);
                ctx.BothLids(1f - k, 1f - k * 0.4f);
                ctx.BothLook(new Vector2(0f, -8f * k));
                if (k >= 1f && !slept)
                {
                    slept = true;
                    ctx.Sfx("snore", 1f, 0.4f);
                    ctx.Emote("z", ctx.Target, new Vector2(0.7f, 0.7f), Emotes.Style.Soft);
                }
            }, 15f);
            if (slept)
            {
                ctx.SetMood(Mood.Asleep, 0f);
                ctx.Eyes.ResetToNeutral();
                ctx.Director?.MarkAsleep();
            }
            else
            {
                ctx.BothLids(1f, 1f);
            }
            yield return ctx.Wait(0.3f);
        }

        static IEnumerator BgHoldHypno(ReactionContext ctx)
        {
            ctx.Sfx("dizzy", 0.75f, 0.6f);
            ctx.BothShape(PupilShape.Spiral);
            ctx.BothPupil(0.6f);
            float t0 = Time.time;
            yield return ctx.WhileHeld(() =>
            {
                float s = Time.time - t0;
                ctx.BothSpin(s * 5f);
                ctx.BothLook(new Vector2(Mathf.Sin(s * 2f) * 12f, Mathf.Cos(s * 1.5f) * 8f));
            }, 12f);
            ctx.BothShape(PupilShape.Round);
            ctx.Eyes.Blink();
            yield return ctx.Wait(0.4f);
        }
    }
}
