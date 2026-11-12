using System.Collections;
using UnityEngine;

namespace Gyorome
{
    public static partial class ReactionCatalog
    {
        const string CatDevice = "ふる・かたむける";

        static void BuildDevice()
        {
            R("shake_rattle", "がたがた", CatDevice, "端末をふる（PC はスペースキー）", ctx => ShakeRattle(ctx), Zone.Any, GestureType.Shake).When(c => !c.Asleep);
            R("shake_dizzy_after", "くらっ", CatDevice, "端末をふると、目がまわる", ctx => ShakeDizzy(ctx), Zone.Any, GestureType.Shake).When(c => !c.Asleep);
            R("shake_wake", "はっ！", CatDevice, "ねむっているときに、端末をふる", ctx => ShakeWake(ctx), Zone.Any, GestureType.Shake).When(c => c.Asleep).WithPriority(5);
            R("shake_angry", "もう！", CatDevice, "何度もふる", ctx => ShakeAngry(ctx), Zone.Any, GestureType.Shake).When(c => c.ShakesInRow >= 3 && !c.Asleep).WithPriority(2);
            R("tilt_roll_gravity", "ころころ", CatDevice, "端末をかたむける（PC は矢印キー）", ctx => TiltRoll(ctx), Zone.Any, GestureType.TiltBegin).When(c => c.Magnitude < 0.75f && !c.Asleep);
            R("tilt_slide", "ずるっ", CatDevice, "端末を大きくかたむける", ctx => TiltSlide(ctx), Zone.Any, GestureType.TiltBegin).When(c => c.Magnitude >= 0.75f && !c.Asleep).WithPriority(2);
            R("tilt_upside_down", "あれれ", CatDevice, "端末をさかさまにする", ctx => TiltUpsideDown(ctx), Zone.Any, GestureType.TiltBegin).When(c => c.Dir.y > 0.9f && c.Magnitude > 0.8f && !c.Asleep).WithPriority(3);
        }

        // ------------------------------------------------------------------
        static IEnumerator ShakeRattle(ReactionContext ctx)
        {
            ctx.Sfx("rattle", 1f, 0.9f);
            ctx.Haptic(30);
            foreach (var e in ctx.Eyes.Both)
            {
                e.GazeSmooth = 0.02f;
                e.Kick(Jitter(ctx, 3f));
            }
            yield return ctx.Over(0.9f, t =>
            {
                foreach (var e in ctx.Eyes.Both)
                {
                    if (Mathf.Repeat(t * 12f, 1f) < 0.5f) e.Look(new Vector2(ctx.Rand(-1f, 1f) * 60f, ctx.Rand(-1f, 1f) * 45f));
                    e.OffsetSmooth = 0.02f;
                    e.OffsetTarget = Jitter(ctx, 0.05f) * (1f - t);
                }
            });
            ctx.BothOffset(Vector2.zero);
            ctx.BothSmooth(0.09f);
            ctx.Eyes.LookAtViewer();
            yield return ctx.Wait(0.3f);
        }

        static IEnumerator ShakeDizzy(ReactionContext ctx)
        {
            ctx.Sfx("rattle", 1.1f, 0.6f);
            foreach (var e in ctx.Eyes.Both) e.Kick(Jitter(ctx, 2.5f));
            yield return ctx.Wait(0.4f);
            ctx.Sfx("dizzy", 1f, 0.7f);
            ctx.SetMood(Mood.Dizzy, 4f);
            ctx.BothShape(PupilShape.Spiral);
            yield return ctx.Over(1.6f, t =>
            {
                foreach (var e in ctx.Eyes.Both)
                {
                    e.Spin = t * 10f;
                    e.OffsetSmooth = 0.05f;
                    e.OffsetTarget = new Vector2(Mathf.Sin(t * 18f), Mathf.Cos(t * 14f)) * 0.06f * (1f - t);
                }
            });
            ctx.BothOffset(Vector2.zero);
        }

        static IEnumerator ShakeWake(ReactionContext ctx)
        {
            ctx.Sfx("surprise", 1.2f, 0.9f);
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
                e.Kick(Vector2.up * 2f);
            }
            ctx.Emote("！", ctx.Eyes.A, new Vector2(0.6f, 1f), Emotes.Style.Big);
            yield return LookAround(ctx, 4, 0.15f);
            ctx.BothPupil(GyoConst.PupilNormal);
            yield return ctx.Wait(0.5f);
        }

        static IEnumerator ShakeAngry(ReactionContext ctx)
        {
            ctx.Sfx("growl", 0.9f, 1f);
            ctx.Haptic(60);
            ctx.SetMood(Mood.Angry, 8f);
            ctx.Eyes.ResetToNeutral();
            foreach (var e in ctx.Eyes.Both)
            {
                e.TiltSmooth = 0.04f;
                e.TintTarget = new Color(1f, 0.8f, 0.78f);
                e.LookForward();
            }
            ctx.BothShake(0.03f, 0.6f);
            ctx.Emote("！", ctx.Eyes.A, new Vector2(0.7f, 0.95f), Emotes.Style.Angry);
            ctx.Emote("！", ctx.Eyes.B, new Vector2(-0.7f, 0.95f), Emotes.Style.Angry);
            yield return ctx.Wait(1.4f);
        }

        static IEnumerator TiltRoll(ReactionContext ctx)
        {
            ctx.Sfx("knock", 1.5f, 0.2f);
            ctx.BothSmooth(0.12f);
            yield return ctx.WhileHeld(() =>
            {
                Vector2 g = ctx.Latest.dir * ctx.Latest.magnitude;
                ctx.BothLookDir(new Vector2(g.x * 1.2f, g.y * 1.2f));
            }, 30f);
            ctx.Eyes.LookAtViewer();
            yield return ctx.Wait(0.3f);
        }

        static IEnumerator TiltSlide(ReactionContext ctx)
        {
            ctx.Sfx("whoosh", 0.7f, 0.5f);
            foreach (var e in ctx.Eyes.Both) { e.OffsetSmooth = 0.2f; e.GazeSmooth = 0.1f; }
            yield return ctx.WhileHeld(() =>
            {
                Vector2 g = ctx.Latest.dir * ctx.Latest.magnitude;
                ctx.BothOffset(g * 0.22f);
                ctx.BothLookDir(-g * 0.6f);
                ctx.BothLids(0.8f, 1f);
                ctx.BothTilt(-10f);
            }, 30f);
            ctx.Sfx("pop", 0.9f, 0.5f);
            ctx.BothOffset(Vector2.zero);
            ctx.BothTilt(0f);
            ctx.BothLids(1f, 1f);
            foreach (var e in ctx.Eyes.Both) e.Kick(Vector2.up * 0.8f);
            yield return ctx.Wait(0.6f);
        }

        static IEnumerator TiltUpsideDown(ReactionContext ctx)
        {
            ctx.Sfx("surprise", 0.7f, 0.6f);
            foreach (var e in ctx.Eyes.Both) { e.OffsetSmooth = 0.25f; e.RollSmooth = 0.3f; }
            ctx.Emote("？", ctx.Eyes.A, new Vector2(0.6f, -1.0f));
            yield return ctx.WhileHeld(() =>
            {
                ctx.BothOffset(new Vector2(0f, 0.25f));
                ctx.BothRoll(180f);
                ctx.BothLook(new Vector2(0f, 30f));
            }, 30f);
            ctx.BothOffset(Vector2.zero);
            ctx.BothRoll(0f);
            ctx.Eyes.Blink();
            yield return ctx.Wait(0.5f);
        }
    }
}
