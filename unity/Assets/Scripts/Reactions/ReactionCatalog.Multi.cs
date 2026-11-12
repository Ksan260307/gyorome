using System.Collections;
using UnityEngine;

namespace Gyorome
{
    public static partial class ReactionCatalog
    {
        const string CatMulti = "2 本指";

        static void BuildMulti()
        {
            R("pinch_in_squint", "にゅっ", CatMulti, "目の外で、2 本指をせばめる", ctx => PinchSquint(ctx), Zone.Background, GestureType.PinchBegin).When(c => c.Magnitude < 1f);
            R("pinch_out_wide", "ぱっちり！", CatMulti, "目の外で、2 本指をひろげる", ctx => PinchWide(ctx), Zone.Background, GestureType.PinchBegin).When(c => c.Magnitude > 1f);
            R("pinch_in_shrink", "ちっちゃ", CatMulti, "目の上で、2 本指をせばめる", ctx => PinchShrink(ctx), Zone.Eye, GestureType.PinchBegin).When(c => c.Magnitude < 1f);
            R("pinch_out_grow", "でっか", CatMulti, "目の上で、2 本指をひろげる", ctx => PinchGrow(ctx), Zone.Eye, GestureType.PinchBegin).When(c => c.Magnitude > 1f);
            R("two_tap_both_wink", "ウインク×2", CatMulti, "2 本指で同時にタップ", ctx => TwoTapWink(ctx), Zone.Any, GestureType.TwoFingerTap);
            R("two_tap_salute", "ビシッ", CatMulti, "2 本指で同時にタップすると、きりっ", ctx => TwoTapSalute(ctx), Zone.Any, GestureType.TwoFingerTap);
            R("two_tap_both_squish", "ぎゅー", CatMulti, "両方の目を、同時にタップ", ctx => TwoTapBothSquish(ctx), Zone.BothEyes, GestureType.TwoFingerTap).WithPriority(2);
            R("two_tap_chord", "ぽぽん", CatMulti, "2 本指でタップすると、2 つの音", ctx => TwoTapChord(ctx), Zone.Any, GestureType.TwoFingerTap).WithWeight(0.8f);
            R("two_swipe_close_both", "すーっと", CatMulti, "2 本指で下にスワイプ（上で、ひらく）", ctx => TwoSwipeClose(ctx), Zone.Any, GestureType.TwoFingerSwipe).When(c => !c.Horizontal);
            R("two_swipe_sideways_roll", "ごろごろ", CatMulti, "2 本指で横にスワイプ", ctx => TwoSwipeRoll(ctx), Zone.Any, GestureType.TwoFingerSwipe).When(c => c.Horizontal);
            R("rotate_tilt_head", "かくん", CatMulti, "2 本指でひねる", ctx => RotateTilt(ctx), Zone.Any, GestureType.RotateBegin);
            R("rotate_dizzy", "ぐるぐる〜", CatMulti, "2 本指で、大きくひねる", ctx => RotateDizzy(ctx), Zone.Any, GestureType.RotateBegin).When(c => Mathf.Abs(c.Magnitude) > 60f).WithPriority(2);
            R("pinch_rotate_cross", "××", CatMulti, "目の上で、2 本指をせばめながらひねる", ctx => PinchRotateCross(ctx), Zone.Eye, GestureType.RotateBegin).WithWeight(0.6f);
            R("combo_stare_back", "みつめあい", CatMulti, "両方の目に指をおいて、少し動かす", ctx => StareBack(ctx), Zone.BothEyes, GestureType.PinchBegin, GestureType.RotateBegin).WithPriority(3);
        }

        // ------------------------------------------------------------------
        static IEnumerator PinchSquint(ReactionContext ctx)
        {
            ctx.Sfx("squish", 1.2f, 0.4f);
            foreach (var e in ctx.Eyes.Both) { e.LidSmooth = 0.05f; e.PupilSmooth = 0.05f; }
            yield return ctx.WhileHeld(() =>
            {
                float k = Mathf.Clamp01((1f - ctx.Latest.magnitude) * 1.6f);
                ctx.BothLids(1f - k * 0.75f, 1f - k * 0.5f);
                ctx.BothPupil(GyoConst.PupilNormal * (1f - k * 0.5f));
            }, 10f);
            ctx.BothLids(1f, 1f);
            yield return ctx.Wait(0.4f);
        }

        static IEnumerator PinchWide(ReactionContext ctx)
        {
            ctx.Sfx("pop", 1.3f, 0.6f);
            foreach (var e in ctx.Eyes.Both) { e.LidSmooth = 0.05f; e.PupilSmooth = 0.05f; }
            yield return ctx.WhileHeld(() =>
            {
                float k = Mathf.Clamp01((ctx.Latest.magnitude - 1f) * 1.2f);
                ctx.BothLids(1f, 1f);
                ctx.BothPupil(Mathf.Lerp(GyoConst.PupilNormal, GyoConst.PupilBig, k));
                ctx.BothScale(Vector3.one * (1f + k * 0.08f));
            }, 10f);
            ctx.Emote("！", ctx.Target, new Vector2(0.7f, 1.0f));
            ctx.BothScale(Vector3.one);
            yield return ctx.Wait(0.5f);
        }

        static IEnumerator PinchShrink(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("squish", 1.5f, 0.5f);
            e.ScaleSmooth = 0.06f;
            ctx.Other.LookAt(e.WorldCenter);
            yield return ctx.WhileHeld(() =>
            {
                float k = Mathf.Clamp01((1f - ctx.Latest.magnitude) * 1.5f);
                e.ScaleTarget = Vector3.one * (1f - k * 0.45f);
                e.Lids(1f - k * 0.3f, 1f);
                e.TiltTarget = -k * 14f;
                e.LookAt(ctx.Other.WorldCenter);
            }, 10f);
            ctx.Sfx("pop", 0.8f, 0.8f);
            e.ScaleSmooth = 0.05f;
            e.ScaleTarget = Vector3.one;
            e.Kick(Vector2.up * 1.6f);
            e.TiltTarget = 0f;
            yield return ctx.Wait(0.7f);
        }

        static IEnumerator PinchGrow(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("spin", 0.6f, 0.5f);
            e.ScaleSmooth = 0.08f;
            ctx.Other.TiltTarget = 12f;
            ctx.Other.LookAt(e.WorldCenter);
            yield return ctx.WhileHeld(() =>
            {
                float k = Mathf.Clamp01((ctx.Latest.magnitude - 1f) * 1.2f);
                e.ScaleTarget = Vector3.one * (1f + k * 0.4f);
                e.PupilTarget = Mathf.Lerp(GyoConst.PupilNormal, GyoConst.PupilBig, k);
                e.Lids(1f, 1f);
            }, 10f);
            ctx.Sfx("hiss", 1.2f, 0.6f);
            e.ScaleSmooth = 0.2f;
            e.ScaleTarget = Vector3.one;
            ctx.Other.TiltTarget = 0f;
            ctx.Other.Kick(Vector2.up * 0.5f);
            yield return ctx.Wait(0.8f);
        }

        static IEnumerator TwoTapWink(ReactionContext ctx)
        {
            ctx.Sfx("pop", 1.2f, 0.6f);
            ctx.Eyes.A.Lids(0f, 0.5f);
            yield return ctx.Wait(0.35f);
            ctx.Eyes.A.Lids(1f, 1f);
            ctx.Sfx("pop", 1.4f, 0.6f);
            ctx.Eyes.B.Lids(0f, 0.5f);
            yield return ctx.Wait(0.35f);
            ctx.Eyes.B.Lids(1f, 1f);
            ctx.Emote("♪", ctx.Eyes.B, new Vector2(0.7f, 0.8f));
            yield return ctx.Wait(0.4f);
        }

        static IEnumerator TwoTapSalute(ReactionContext ctx)
        {
            ctx.Sfx("surprise", 1.1f, 0.6f);
            ctx.Haptic(25);
            foreach (var e in ctx.Eyes.Both)
            {
                e.GazeSmooth = 0.03f;
                e.LookForward();
                e.LidSmooth = 0.03f;
                e.Lids(0.8f, 0.9f);
                e.TiltSmooth = 0.04f;
                e.TiltTarget = 12f;
                e.PupilTarget = 0.3f;
                e.ScaleTarget = new Vector3(0.97f, 1.05f, 1f);
            }
            yield return ctx.Wait(0.9f);
            ctx.BothScale(Vector3.one);
            yield return ctx.Wait(0.3f);
        }

        static IEnumerator TwoTapBothSquish(ReactionContext ctx)
        {
            ctx.Sfx("squish", 0.8f, 0.9f);
            ctx.Haptic(35);
            foreach (var e in ctx.Eyes.Both)
            {
                e.ScaleSmooth = 0.03f;
                e.ScaleTarget = new Vector3(1.2f, 0.7f, 1f);
                e.Lids(0.3f, 0.6f);
            }
            yield return ctx.Wait(0.3f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.ScaleSmooth = 0.06f;
                e.ScaleTarget = Vector3.one;
                e.Kick(Vector2.up * 2f);
                e.Lids(1f, 1f);
            }
            ctx.Sfx("pop", 1f, 0.8f);
            ctx.Emote("！", ctx.Eyes.A, new Vector2(0.6f, 1f));
            ctx.Emote("！", ctx.Eyes.B, new Vector2(0.6f, 1f));
            yield return ctx.Wait(0.7f);
        }

        static IEnumerator TwoTapChord(ReactionContext ctx)
        {
            ctx.Sfx("note", 1f, 0.8f);
            ctx.Eyes.A.Kick(Vector2.up * 1.4f);
            ctx.Emote("♪", ctx.Eyes.A, new Vector2(0.6f, 0.9f));
            yield return ctx.Wait(0.18f);
            ctx.Sfx("note", 1.26f, 0.8f);
            ctx.Eyes.B.Kick(Vector2.up * 1.4f);
            ctx.Emote("♫", ctx.Eyes.B, new Vector2(0.6f, 0.9f));
            yield return ctx.Wait(0.6f);
        }

        static IEnumerator TwoSwipeClose(ReactionContext ctx)
        {
            bool close = ctx.Dir.y < 0f;
            ctx.Sfx(close ? "blink" : "pop", 0.9f, 0.6f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.LidSmooth = 0.12f;
                e.Lids(close ? 0f : 1f, close ? 0.6f : 1f);
                if (!close) { e.PupilTarget = GyoConst.PupilBig; e.ScaleTarget = Vector3.one * 1.06f; }
            }
            yield return ctx.Wait(close ? 1.6f : 1.0f);
            ctx.BothLids(1f, 1f);
            ctx.BothScale(Vector3.one);
            yield return ctx.Wait(0.4f);
        }

        static IEnumerator TwoSwipeRoll(ReactionContext ctx)
        {
            ctx.Sfx("spin", 0.8f, 0.7f);
            float dir = -Mathf.Sign(ctx.Dir.x);
            foreach (var e in ctx.Eyes.Both) { e.RollSmooth = 0.1f; e.GazeSmooth = 0.04f; }
            yield return ctx.Over(1.3f, t =>
            {
                float k = Ease.OutQuad(t);
                ctx.BothRoll(dir * 540f * k);
                ctx.BothLook(new Vector2(ctx.Dir.x * 40f * (1f - k), 0f));
            });
            ctx.BothRoll(0f);
            ctx.Eyes.Blink();
            yield return ctx.Wait(0.4f);
        }

        static IEnumerator RotateTilt(ReactionContext ctx)
        {
            ctx.Sfx("knock", 1.3f, 0.3f);
            foreach (var e in ctx.Eyes.Both) { e.TiltSmooth = 0.05f; e.OffsetSmooth = 0.06f; }
            yield return ctx.WhileHeld(() =>
            {
                float a = Mathf.Clamp(ctx.Latest.magnitude, -45f, 45f);
                float k = a / 45f;
                // 頭をかしげる：目が上下にずれ、まぶたがかたむく
                ctx.Eyes.A.OffsetTarget = new Vector2(0f, -k * 0.12f);
                ctx.Eyes.B.OffsetTarget = new Vector2(0f, k * 0.12f);
                ctx.Eyes.A.TiltTarget = -k * 20f;
                ctx.Eyes.B.TiltTarget = k * 20f;
                ctx.BothLook(new Vector2(0f, k * 10f));
            }, 10f);
            ctx.BothOffset(Vector2.zero);
            ctx.BothTilt(0f);
            ctx.Emote("？", ctx.Target, new Vector2(0.7f, 0.9f));
            yield return ctx.Wait(0.6f);
        }

        static IEnumerator RotateDizzy(ReactionContext ctx)
        {
            ctx.Sfx("dizzy", 1f, 0.8f);
            ctx.SetMood(Mood.Dizzy, 5f);
            ctx.BothShape(PupilShape.Spiral);
            yield return ctx.WhileHeld(() =>
            {
                ctx.BothSpin(ctx.Latest.magnitude * 0.08f);
                ctx.BothRoll(ctx.Latest.magnitude * 0.5f);
            }, 10f);
            yield return ctx.Over(1.5f, t =>
            {
                foreach (var e in ctx.Eyes.Both)
                {
                    e.RollSmooth = 0.3f;
                    e.RollTarget = 0f;
                    e.OffsetSmooth = 0.05f;
                    e.OffsetTarget = new Vector2(Mathf.Sin(t * 22f), Mathf.Cos(t * 18f)) * 0.05f * (1f - t);
                }
            });
            ctx.BothOffset(Vector2.zero);
        }

        static IEnumerator PinchRotateCross(ReactionContext ctx)
        {
            ctx.Sfx("dizzy", 1.4f, 0.6f);
            ctx.BothShape(PupilShape.Cross);
            ctx.BothLids(0.6f, 0.85f);
            yield return ctx.WhileHeld(() => ctx.BothRoll(ctx.Latest.magnitude * 0.3f), 10f);
            yield return ctx.Wait(0.8f);
            ctx.BothShape(PupilShape.Round);
            ctx.BothRoll(0f);
            ctx.Eyes.Blink();
            yield return ctx.Wait(0.4f);
        }

        static IEnumerator StareBack(ReactionContext ctx)
        {
            ctx.Sfx("chime", 0.9f, 0.6f);
            ctx.BothSmooth(0.05f);
            ctx.Eyes.LookAtViewer();
            ctx.BothPupil(0.55f);
            yield return ctx.Wait(1.4f);
            // てれて、そっぽを向く
            ctx.Sfx("kiss", 1f, 0.5f);
            ctx.BothTint(new Color(1f, 0.86f, 0.9f));
            ctx.BothLookDir(new Vector2(ctx.Chance(0.5f) ? 0.8f : -0.8f, -0.3f));
            ctx.BothLids(0.75f, 0.7f);
            ctx.Emote("♥", ctx.Eyes.A, new Vector2(0.6f, 0.9f));
            ctx.Emote("♥", ctx.Eyes.B, new Vector2(-0.6f, 0.9f));
            ctx.SetMood(Mood.Love, 6f);
            yield return ctx.Wait(1.3f);
        }
    }
}
