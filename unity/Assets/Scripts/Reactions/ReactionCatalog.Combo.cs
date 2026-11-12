using System.Collections;
using UnityEngine;

namespace Gyorome
{
    public static partial class ReactionCatalog
    {
        const string CatCombo = "くみあわせ";

        static void BuildCombo()
        {
            R("combo_wake_startle", "びっくり", CatCombo, "ねむっているときに、さわる", ctx => WakeStartle(ctx), Zone.Any,
                GestureType.Tap, GestureType.Swipe, GestureType.Flick, GestureType.LongPressBegin, GestureType.DragBegin, GestureType.Rub, GestureType.Circle, GestureType.PinchBegin, GestureType.TwoFingerTap, GestureType.TwoFingerSwipe, GestureType.RotateBegin)
                .When(c => c.Asleep).WithPriority(10);
            R("combo_tap_after_angry", "ふん", CatCombo, "おこっているときに、タップ", ctx => TapAfterAngry(ctx), Zone.Any, GestureType.Tap).When(c => c.Mood == Mood.Angry && c.TapCount <= 2).WithPriority(4);
            R("combo_pet_after_angry", "ゆるす", CatCombo, "おこっているときに、目の上をゆっくりなでる", ctx => PetAfterAngry(ctx), Zone.Eye, GestureType.DragBegin).When(c => c.Mood == Mood.Angry).WithPriority(4);
            R("combo_poke_both_eyes_fast", "だぶるぱんち", CatCombo, "左右の目を、つづけてすばやくタップ", ctx => PokeBoth(ctx), Zone.Eye, GestureType.Tap).When(c => c.DoublePoke).WithPriority(4);
            R("combo_rhythm", "のりのり", CatCombo, "同じテンポで 4 回以上タップ", ctx => Rhythm(ctx), Zone.Any, GestureType.Tap).When(c => c.Rhythmic).WithPriority(4);
            R("combo_tap_10_times_same_eye", "もういいって", CatCombo, "同じ目を 10 回タップ", ctx => TooMany(ctx), Zone.Eye, GestureType.Tap).When(c => c.SameEyeTapStreak >= 10).WithPriority(5).Cooldown(8f);
            R("combo_long_idle_then_tap", "おかえり", CatCombo, "1 分以上あけてから、さわる", ctx => Welcome(ctx), Zone.Any, GestureType.Tap, GestureType.Swipe, GestureType.LongPressBegin, GestureType.DragBegin).When(c => c.FirstTouchAfterLongIdle && !c.Asleep).WithPriority(5).Cooldown(30f);
            R("combo_tickle_laugh_escalate", "げらげら", CatCombo, "つづけて 2 回こちょこちょ", ctx => TickleEscalate(ctx), Zone.EyeBottom, GestureType.DragBegin).When(c => c.TicklesInRow >= 1 && c.SinceLastReaction < 8f).WithPriority(4);
            R("combo_swipe_between", "ぴょん", CatCombo, "片方の目から、もう片方の目へスワイプ", ctx => SwipeBetween(ctx), Zone.Eye, GestureType.Swipe, GestureType.Flick).When(c => c.Eyes.Hit(c.Event.pos2) >= 0 && c.Eyes.Hit(c.Event.pos2) != c.Eyes.Hit(c.Event.pos)).WithPriority(5);
            R("tap_while_dizzy", "うぷっ", CatCombo, "目がまわっているときに、タップ", ctx => TapWhileDizzy(ctx), Zone.Any, GestureType.Tap).When(c => c.Mood == Mood.Dizzy).WithPriority(4);
            R("combo_tap_while_happy", "にこにこ", CatCombo, "うれしいときに、タップ", ctx => TapWhileHappy(ctx), Zone.Eye, GestureType.Tap).When(c => c.Mood == Mood.Happy && c.TapCount == 1).WithPriority(3).WithWeight(0.8f);
            R("combo_tap_while_sad", "なぐさめて", CatCombo, "かなしいときに、目の上をタップ", ctx => TapWhileSad(ctx), Zone.Eye, GestureType.Tap).When(c => c.Mood == Mood.Sad && c.TapCount == 1).WithPriority(3);
            R("combo_tap_while_love", "めろめろ", CatCombo, "うっとりしているときに、タップ", ctx => TapWhileLove(ctx), Zone.Any, GestureType.Tap).When(c => c.Mood == Mood.Love && c.TapCount == 1).WithPriority(3);
            R("combo_swipe_while_asleep_lid", "そーっと", CatCombo, "ねむっている目を、上にスワイプしてまぶたをあける", ctx => PeekAsleep(ctx), Zone.Eye, GestureType.Swipe).When(c => c.Asleep && c.Up).WithPriority(10);
            R("combo_night_tap", "ねむいよ", CatCombo, "夜おそくにタップ", ctx => NightTap(ctx), Zone.Any, GestureType.Tap).When(c => (c.Hour >= 23 || c.Hour < 5) && c.TapCount == 1 && c.Chance(0.35f)).WithPriority(2).Cooldown(20f);
        }

        // ------------------------------------------------------------------
        static IEnumerator WakeStartle(ReactionContext ctx)
        {
            ctx.Sfx("surprise", 1.1f, 0.9f);
            ctx.Haptic(40);
            ctx.SetMood(Mood.Normal, 0f);
            ctx.Eyes.ResetToNeutral();
            ctx.Director?.MarkAwake();
            foreach (var e in ctx.Eyes.Both)
            {
                e.LidSmooth = 0.02f;
                e.Lids(1f, 1f);
                e.PupilSmooth = 0.03f;
                e.PupilTarget = 0.14f;
                e.OffsetSmooth = 0.03f;
                e.OffsetTarget = new Vector2(0f, 0.15f);
                e.ScaleTarget = Vector3.one * 1.06f;
            }
            ctx.Emote("！", ctx.Target, new Vector2(0.6f, 1f), Emotes.Style.Big);
            yield return ctx.Wait(0.4f);
            ctx.BothOffset(Vector2.zero);
            ctx.BothScale(Vector3.one);
            yield return LookAround(ctx, 3, 0.2f);
            ctx.Eyes.LookAtScreen(ctx.Screen);
            ctx.BothPupil(GyoConst.PupilNormal);
            yield return ctx.Wait(0.7f);
        }

        static IEnumerator TapAfterAngry(ReactionContext ctx)
        {
            ctx.Sfx("growl", 1.3f, 0.5f);
            float side = ctx.LocalOnTarget.x >= 0f ? -1f : 1f;
            foreach (var e in ctx.Eyes.Both)
            {
                e.GazeSmooth = 0.05f;
                e.LookDir(new Vector2(side, 0.3f));
                e.Lids(0.5f, 1f);
            }
            ctx.Emote("…", ctx.Target, new Vector2(0.7f, 0.8f), Emotes.Style.Soft);
            yield return ctx.Wait(1.3f);
        }

        static IEnumerator PetAfterAngry(ReactionContext ctx)
        {
            ctx.Sfx("purr", 1f, 0.6f);
            foreach (var e in ctx.Eyes.Both) { e.TiltSmooth = 0.4f; e.LidSmooth = 0.3f; }
            yield return ctx.WhileHeld(() =>
            {
                float k = Mathf.Clamp01(ctx.Latest.duration / 2f);
                ctx.BothTilt(18f * (1f - k));
                ctx.BothTint(Color.Lerp(new Color(1f, 0.9f, 0.88f), Color.white, k));
                ctx.BothLids(0.72f + 0.1f * k, 1f - 0.4f * k);
            }, 8f);
            ctx.SetMood(Mood.Happy, 5f);
            ctx.Eyes.ResetToNeutral();
            ctx.Sfx("chime", 1f, 0.7f);
            ctx.Emote("♥", ctx.Target, new Vector2(0.6f, 0.9f));
            yield return ctx.Wait(0.8f);
        }

        static IEnumerator PokeBoth(ReactionContext ctx)
        {
            ctx.Sfx("bounce", 1.2f, 0.9f);
            ctx.Haptic(40);
            foreach (var e in ctx.Eyes.Both)
            {
                e.Kick(Vector2.down * 2f);
                e.Shape = PupilShape.Cross;
                e.Lids(0.4f, 0.7f);
            }
            ctx.Emote("×", ctx.Eyes.A, new Vector2(0.6f, 0.9f));
            ctx.Emote("×", ctx.Eyes.B, new Vector2(0.6f, 0.9f));
            yield return ctx.Wait(1.0f);
            ctx.BothShape(PupilShape.Round);
            ctx.BothLids(1f, 1f);
            yield return ctx.Wait(0.4f);
        }

        static IEnumerator Rhythm(ReactionContext ctx)
        {
            ctx.Sfx("note", ctx.Rand(0.9f, 1.3f), 0.8f);
            ctx.SetMood(Mood.Happy, 4f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.Kick(Vector2.up * 1.8f);
                e.Lids(0.9f, 0.5f);
            }
            ctx.Emote("♪", ctx.Target, new Vector2(ctx.Rand(-0.8f, 0.8f), 0.95f));
            yield return ctx.Wait(0.4f);
        }

        static IEnumerator TooMany(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("growl", 1.1f, 0.7f);
            ctx.Haptic(40);
            e.LidSmooth = 0.03f;
            e.Lids(0f, 0.4f);
            e.TintTarget = new Color(1f, 0.78f, 0.76f);
            ctx.Other.LookForward();
            ctx.Other.TiltTarget = 14f;
            ctx.Other.Lids(0.7f, 1f);
            ctx.Emote("…", e, new Vector2(0.7f, 0.8f), Emotes.Style.Soft);
            yield return ctx.Wait(2.5f);
            e.Lids(1f, 1f);
            e.TintTarget = Color.white;
            yield return ctx.Wait(0.5f);
        }

        static IEnumerator Welcome(ReactionContext ctx)
        {
            ctx.Sfx("sparkle", 1f, 0.8f);
            ctx.Haptic(25);
            ctx.SetMood(Mood.Happy, 5f);
            ctx.Eyes.ResetToNeutral();
            foreach (var e in ctx.Eyes.Both)
            {
                e.Sparkle = 1f;
                e.PupilTarget = 0.55f;
                e.LookAt(ctx.World);
            }
            yield return Hop(ctx, ctx.Eyes.A, 0.2f, 0.35f);
            ctx.Emote("♥", ctx.Eyes.A, new Vector2(0.6f, 0.95f));
            yield return Hop(ctx, ctx.Eyes.B, 0.2f, 0.35f);
            ctx.Emote("♥", ctx.Eyes.B, new Vector2(0.6f, 0.95f));
            yield return ctx.Wait(0.8f);
            ctx.BothSparkle(0f);
        }

        static IEnumerator TickleEscalate(ReactionContext ctx)
        {
            ctx.Director?.ResetTickles();
            ctx.Sfx("laugh", ctx.Rand(0.95f, 1.1f), 1f);
            ctx.Haptic(30);
            ctx.SetMood(Mood.Happy, 6f);
            foreach (var e in ctx.Eyes.Both) { e.LidSmooth = 0.03f; e.Lids(0.5f, 0.3f); }
            float t0 = Time.time;
            yield return ctx.WhileHeld(() =>
            {
                float s = Time.time - t0;
                foreach (var e in ctx.Eyes.Both)
                {
                    e.OffsetSmooth = 0.03f;
                    e.OffsetTarget = new Vector2(Mathf.Sin(s * 27f) * 0.05f, Mathf.Abs(Mathf.Sin(s * 18f)) * 0.1f);
                    e.ScaleTarget = new Vector3(1f + Mathf.Abs(Mathf.Sin(s * 18f)) * 0.06f, 1f - Mathf.Abs(Mathf.Sin(s * 18f)) * 0.06f, 1f);
                }
            }, 6f);
            ctx.Tear(ctx.Eyes.A, 1);
            ctx.Tear(ctx.Eyes.B, -1);
            ctx.BothOffset(Vector2.zero);
            ctx.BothScale(Vector3.one);
            yield return ctx.Wait(0.6f);
        }

        static IEnumerator SwipeBetween(ReactionContext ctx)
        {
            var from = ctx.Target;
            var to = ctx.Other;
            ctx.Sfx("hop", 1f, 0.8f);
            yield return Hop(ctx, from, 0.25f, 0.3f);
            from.LookAt(to.WorldCenter);
            ctx.Sfx("hop", 1.2f, 0.8f);
            yield return Hop(ctx, to, 0.3f, 0.35f);
            to.LookAt(from.WorldCenter);
            ctx.Emote("♪", to, new Vector2(0.6f, 0.9f));
            yield return ctx.Wait(0.6f);
        }

        static IEnumerator TapWhileDizzy(ReactionContext ctx)
        {
            ctx.Sfx("dizzy", 0.7f, 0.6f);
            ctx.BothTint(new Color(0.82f, 1f, 0.86f));
            ctx.BothLids(0.5f, 0.8f);
            yield return ctx.Over(1.4f, t =>
            {
                foreach (var e in ctx.Eyes.Both)
                {
                    e.Spin = t * 16f;
                    e.OffsetSmooth = 0.05f;
                    e.OffsetTarget = new Vector2(Mathf.Sin(t * 25f), Mathf.Cos(t * 19f)) * 0.08f * (1f - t);
                }
            });
            ctx.BothOffset(Vector2.zero);
        }

        static IEnumerator TapWhileHappy(ReactionContext ctx)
        {
            ctx.Sfx("giggle", 1.2f, 0.6f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.LidSmooth = 0.05f;
                e.Lids(0.8f, 0.3f);
                e.Kick(Vector2.up * 1.2f);
            }
            ctx.Emote("♪", ctx.Target, new Vector2(0.7f, 0.9f));
            yield return ctx.Wait(0.9f);
        }

        static IEnumerator TapWhileSad(ReactionContext ctx)
        {
            ctx.Sfx("chime", 0.8f, 0.6f);
            var e = ctx.Target;
            e.LookAt(ctx.World);
            yield return ctx.Wait(0.5f);
            ctx.SetMood(Mood.Normal, 0f);
            ctx.Eyes.ResetToNeutral();
            foreach (var x in ctx.Eyes.Both) { x.TiltSmooth = 0.4f; x.LidSmooth = 0.3f; x.Lids(1f, 0.7f); }
            ctx.Emote("♥", e, new Vector2(0.6f, 0.9f));
            yield return ctx.Wait(1.0f);
        }

        static IEnumerator TapWhileLove(ReactionContext ctx)
        {
            ctx.Sfx("kiss", 1f, 0.7f);
            ctx.BothShape(PupilShape.Heart);
            ctx.BothPupil(GyoConst.PupilBig);
            ctx.BothTint(new Color(1f, 0.88f, 0.92f));
            yield return ctx.Over(1.2f, t =>
            {
                float b = 1f + 0.06f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 4f));
                ctx.BothScale(new Vector3(b, b, b));
            });
            ctx.Emote("♥", ctx.Eyes.A, new Vector2(0.6f, 0.9f));
            ctx.Emote("♥", ctx.Eyes.B, new Vector2(-0.6f, 0.9f));
            ctx.BothScale(Vector3.one);
            yield return ctx.Wait(0.5f);
        }

        static IEnumerator PeekAsleep(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("blink", 0.6f, 0.5f);
            e.LidSmooth = 0.2f;
            e.Lids(0.6f, 0.6f);
            e.Look(new Vector2(0f, 25f));
            yield return ctx.Wait(0.8f);
            e.Look(new Vector2(ctx.Rand(-20f, 20f), -10f));
            yield return ctx.Wait(0.5f);
            e.LidSmooth = 0.3f;
            e.Lids(0f, 0.6f);
            ctx.Sfx("snore", 1f, 0.3f);
            ctx.Emote("z", e, new Vector2(0.7f, 0.7f), Emotes.Style.Soft);
            yield return ctx.Wait(0.8f);
        }

        static IEnumerator NightTap(ReactionContext ctx)
        {
            ctx.Sfx("yawn", 0.8f, 0.5f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.LidSmooth = 0.3f;
                e.Lids(0.4f, 0.8f);
                e.PupilTarget = GyoConst.PupilBig;
                e.LookAt(ctx.World);
            }
            ctx.Emote("★", ctx.Target, new Vector2(0.7f, 0.9f), Emotes.Style.Soft);
            yield return ctx.Wait(1.6f);
        }
    }
}
