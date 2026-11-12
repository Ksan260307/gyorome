using System.Collections;
using UnityEngine;

namespace Gyorome
{
    public static partial class ReactionCatalog
    {
        const string CatTap = "タップ";

        static void BuildTap()
        {
            // ---------------- 目を 1 回タップ ----------------
            R("tap_flinch", "びくっ", CatTap, "目をひとつ、ちょんとタップ", ctx => TapFlinch(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount == 1);
            R("tap_look_at_finger", "じーっ", CatTap, "目をタップして、見つめられてみて", ctx => TapLookAtFinger(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount == 1);
            R("tap_wink", "パチッ", CatTap, "目をタップすると、たまにウインク", ctx => TapWink(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount == 1).WithWeight(0.8f);
            R("tap_pupil_shrink", "キュッ", CatTap, "タップされて、ひとみが小さく", ctx => TapPupilShrink(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount == 1);
            R("tap_jiggle", "ぷるん", CatTap, "目玉はゼリーみたいにゆれる", ctx => TapJiggle(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount == 1);
            R("tap_ouch", "いたっ", CatTap, "つつかれると、いたい", ctx => TapOuch(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount == 1).WithWeight(0.7f);
            R("tap_cross_eyed", "ギョロ", CatTap, "目をタップすると、より目に", ctx => TapCrossEyed(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount == 1).WithWeight(0.8f);
            R("tap_blink_double", "パチパチ", CatTap, "タップで、2 回まばたき", ctx => TapBlinkDouble(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount == 1);
            R("tap_look_other", "ちらっ", CatTap, "さわられた目を、もう片方が見る", ctx => TapLookOther(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount == 1);
            R("tap_center_bullseye", "ドキッ", CatTap, "ひとみのまん中を、ぴたりとタップ", ctx => TapBullseye(ctx), Zone.Pupil, GestureType.Tap).When(c => c.TapCount == 1).WithPriority(2);
            R("tap_top_half_pat", "ぽん", CatTap, "目の上の方を、なでるようにタップ", ctx => TapPat(ctx), Zone.EyeTop, GestureType.Tap).When(c => c.TapCount == 1).WithPriority(2).WithWeight(0.7f);
            R("tap_bottom_half_chin", "ん？", CatTap, "目の下の方をタップ", ctx => TapChin(ctx), Zone.EyeBottom, GestureType.Tap).When(c => c.TapCount == 1).WithPriority(2).WithWeight(0.7f);
            R("tap_rare_golden", "きらきら", CatTap, "とてもまれに、目が金色にきらめく", ctx => TapGolden(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount == 1 && c.Chance(0.02f)).WithPriority(3);

            // ---------------- 2 回タップ ----------------
            R("dtap_surprise", "！！", CatTap, "目をすばやく 2 回タップ", ctx => DtapSurprise(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount == 2);
            R("dtap_spin", "ぐるん", CatTap, "2 回タップで、目玉が一回転", ctx => DtapSpin(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount == 2);
            R("dtap_peekaboo", "いないいない…ばあ", CatTap, "2 回タップで、いないいないばあ", ctx => DtapPeekaboo(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount == 2);
            R("dtap_hiccup", "ひっく", CatTap, "2 回タップで、しゃっくり", ctx => DtapHiccup(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount == 2).WithWeight(0.8f);
            R("dtap_bg_jump", "ぴょこ", CatTap, "背景を 2 回タップ", ctx => DtapBgJump(ctx), Zone.Background, GestureType.Tap).When(c => c.TapCount == 2);
            R("bg_double_tap_zoom", "ずいっ", CatTap, "背景を 2 回タップすると、近づいてくる", ctx => BgZoom(ctx), Zone.Background, GestureType.Tap).When(c => c.TapCount == 2);

            // ---------------- 3 回タップ ----------------
            R("ttap_dizzy", "めまい", CatTap, "目を 3 回つづけてタップ", ctx => TtapDizzy(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount == 3);
            R("ttap_angry", "むっ", CatTap, "3 回もつつくと、おこる", ctx => TtapAngry(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount == 3);
            R("ttap_sneeze", "はくしょん", CatTap, "3 回タップで、くしゃみ", ctx => TtapSneeze(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount == 3);

            // ---------------- 連打 ----------------
            R("rapid_panic", "あわわ", CatTap, "目を連打", ctx => RapidPanic(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount >= 5);
            R("rapid_laugh", "あはは", CatTap, "連打すると、くすぐったい", ctx => RapidLaugh(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount >= 5);
            R("rapid_overload", "くらくら", CatTap, "もっと連打", ctx => RapidOverload(ctx), Zone.Eye, GestureType.Tap).When(c => c.TapCount >= 8).WithPriority(2);
            R("rapid_disco", "ピカピカ", CatTap, "すごく連打", ctx => RapidDisco(ctx), Zone.Any, GestureType.Tap).When(c => c.TapCount >= 12).WithPriority(3);

            // ---------------- 背景タップ ----------------
            R("bg_tap_look", "なになに？", CatTap, "目の外をタップ", ctx => BgTapLook(ctx), Zone.Background, GestureType.Tap).When(c => c.TapCount == 1);
            R("bg_tap_startle", "はっ", CatTap, "目の外をタップすると、はっとする", ctx => BgTapStartle(ctx), Zone.Background, GestureType.Tap).When(c => c.TapCount == 1);
            R("bg_tap_ignore", "ふーん", CatTap, "目の外をタップしても、きょうみがないことも", ctx => BgTapIgnore(ctx), Zone.Background, GestureType.Tap).When(c => c.TapCount == 1).WithWeight(0.6f);
            R("bg_tap_knock", "コンコン", CatTap, "ガラスをノックするみたいに、目の外をタップ", ctx => BgTapKnock(ctx), Zone.Background, GestureType.Tap).When(c => c.TapCount == 1);
        }

        // ------------------------------------------------------------------
        static IEnumerator TapFlinch(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("poke", ctx.Rand(0.95f, 1.1f));
            ctx.Haptic(20);
            e.LidSmooth = 0.03f;
            e.Lids(0.25f, 0.6f);
            e.Kick(-ctx.LocalOnTarget.normalized * 1.4f + Vector2.down * 0.4f);
            e.PupilTarget = GyoConst.PupilSmall;
            ctx.Other.GazeSmooth = 0.05f;
            ctx.Other.LookAt(e.WorldCenter);
            yield return ctx.Wait(0.25f);
            e.LidSmooth = 0.08f;
            e.Lids(1f, 1f);
            e.PupilTarget = GyoConst.PupilNormal;
            e.LookAt(ctx.World);
            yield return ctx.Wait(0.6f);
        }

        static IEnumerator TapLookAtFinger(ReactionContext ctx)
        {
            ctx.Sfx("blink", 1.2f, 0.5f);
            ctx.BothSmooth(0.05f);
            ctx.Eyes.LookAtScreen(ctx.Screen);
            ctx.BothPupil(0.5f);
            yield return ctx.Wait(1.1f);
            ctx.Target.Blink();
            yield return ctx.Wait(0.3f);
        }

        static IEnumerator TapWink(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("pop", 1.3f, 0.7f);
            e.LidSmooth = 0.035f;
            e.Lids(0f, 0.5f);
            ctx.Other.PupilTarget = 0.5f;
            ctx.Emote("♪", e, new Vector2(0.8f, 0.7f));
            yield return ctx.Wait(0.45f);
            e.Lids(1f, 1f);
            yield return ctx.Wait(0.3f);
        }

        static IEnumerator TapPupilShrink(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("blink", 1.6f, 0.6f);
            e.PupilSmooth = 0.03f;
            e.PupilTarget = 0.16f;
            e.LookAt(ctx.World);
            yield return ctx.Wait(0.5f);
            e.PupilSmooth = 0.6f;
            e.PupilTarget = GyoConst.PupilNormal;
            yield return ctx.Wait(1.0f);
        }

        static IEnumerator TapJiggle(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("squish", ctx.Rand(0.9f, 1.15f));
            ctx.Haptic(15);
            Vector2 dir = ctx.LocalOnTarget;
            if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;
            e.Kick(dir.normalized * 2.2f);
            yield return Squash(ctx, e, 0.22f, 0.35f);
            yield return ctx.Wait(0.5f);
        }

        static IEnumerator TapOuch(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("surprise", 0.8f);
            ctx.Haptic(40);
            e.LidSmooth = 0.03f;
            e.Lids(0f, 0.3f);
            e.Kick(Vector2.down * 1.2f);
            yield return ctx.Wait(0.4f);
            ctx.Tear(e, ctx.LocalOnTarget.x >= 0f ? 1 : -1);
            ctx.Sfx("drip", 1f, 0.6f);
            e.Lids(0.7f, 1f);
            e.TiltTarget = 16f;
            e.LookAt(ctx.World);
            ctx.Other.LookAt(ctx.World);
            ctx.Other.TiltTarget = 14f;
            yield return ctx.Wait(1.2f);
        }

        static IEnumerator TapCrossEyed(ReactionContext ctx)
        {
            ctx.Sfx("spin", 0.8f, 0.6f);
            ctx.BothSmooth(0.05f);
            // おたがいの方（内がわ）を見て、より目に
            ctx.Eyes.A.LookDir(new Vector2(ctx.Eyes.A.isLeft ? 1f : -1f, -0.15f));
            ctx.Eyes.B.LookDir(new Vector2(ctx.Eyes.B.isLeft ? 1f : -1f, -0.15f));
            yield return ctx.Wait(0.9f);
            ctx.Eyes.Blink();
            yield return ctx.Wait(0.25f);
        }

        static IEnumerator TapBlinkDouble(ReactionContext ctx)
        {
            ctx.Sfx("blink", 1f, 0.5f);
            ctx.Target.Blink(0.14f);
            yield return ctx.Wait(0.22f);
            ctx.Sfx("blink", 1.1f, 0.5f);
            ctx.Target.Blink(0.14f);
            ctx.Other.LookAt(ctx.World);
            yield return ctx.Wait(0.5f);
        }

        static IEnumerator TapLookOther(ReactionContext ctx)
        {
            ctx.BothSmooth(0.05f);
            ctx.Other.LookAt(ctx.Target.WorldCenter);
            ctx.Target.LookAt(ctx.World);
            yield return ctx.Wait(0.45f);
            ctx.Target.LookAt(ctx.Other.WorldCenter);
            ctx.Sfx("pop", 1.1f, 0.4f);
            yield return ctx.Wait(0.6f);
        }

        static IEnumerator TapBullseye(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("chime", 1f, 0.9f);
            ctx.Haptic(25);
            e.PupilSmooth = 0.05f;
            e.PupilTarget = GyoConst.PupilBig;
            e.Sparkle = 1f;
            ctx.Emote("♥", e, new Vector2(0.6f, 0.8f));
            ctx.Other.PupilTarget = 0.55f;
            yield return ctx.Wait(1.2f);
            e.Sparkle = 0f;
            yield return ctx.Wait(0.3f);
        }

        static IEnumerator TapPat(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("pop", 0.8f, 0.7f);
            e.Kick(Vector2.down * 1.0f);
            e.Lids(0.85f, 0.6f);
            yield return Squash(ctx, e, 0.15f, 0.3f);
            e.Lids(0.9f, 0.5f);
            ctx.Emote("♪", e, new Vector2(0.7f, 0.9f));
            yield return ctx.Wait(0.8f);
        }

        static IEnumerator TapChin(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.BothSmooth(0.05f);
            e.LookAt(ctx.World);
            ctx.Sfx("blink", 1.3f, 0.4f);
            yield return ctx.Wait(0.5f);
            e.LookForward();
            e.PupilTarget = 0.5f;
            ctx.Emote("？", e, new Vector2(0.75f, 0.8f));
            yield return ctx.Wait(0.7f);
        }

        static IEnumerator TapGolden(ReactionContext ctx)
        {
            ctx.Sfx("jackpot", 1f, 1f);
            ctx.Haptic(60);
            Color gold = new Color(1f, 0.82f, 0.25f), goldIn = new Color(1f, 0.95f, 0.6f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.ColorSmooth = 0.08f;
                e.IrisTarget = gold;
                e.IrisInnerTarget = goldIn;
                e.Sparkle = 1f;
                e.Shape = PupilShape.Star;
                e.PupilTarget = 0.5f;
            }
            for (int i = 0; i < 6; i++)
            {
                ctx.Emote("★", i % 2 == 0 ? ctx.Eyes.A : ctx.Eyes.B, new Vector2(ctx.Rand(-0.8f, 0.8f), ctx.Rand(0.2f, 1f)));
                yield return ctx.Wait(0.15f);
            }
            yield return ctx.Wait(1.2f);
            var it = Themes.IrisOf(SaveSystem.Settings.irisTheme);
            foreach (var e in ctx.Eyes.Both)
            {
                e.ColorSmooth = 0.4f;
                e.IrisTarget = it.outer;
                e.IrisInnerTarget = it.inner;
                e.Sparkle = 0f;
            }
            yield return ctx.Wait(0.4f);
        }

        // ------------------------------------------------------------------
        static IEnumerator DtapSurprise(ReactionContext ctx)
        {
            ctx.Sfx("surprise", 1f);
            ctx.Haptic(40);
            foreach (var e in ctx.Eyes.Both)
            {
                e.LidSmooth = 0.03f;
                e.PupilSmooth = 0.03f;
                e.Lids(1f, 1f);
                e.PupilTarget = 0.14f;
                e.OffsetSmooth = 0.03f;
                e.OffsetTarget = new Vector2(0f, 0.12f);
                e.ScaleTarget = new Vector3(1.08f, 1.08f, 1.08f);
            }
            ctx.Emote("！", ctx.Target, new Vector2(0.6f, 1.0f), Emotes.Style.Big);
            yield return ctx.Wait(0.55f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.OffsetSmooth = 0.1f;
                e.OffsetTarget = Vector2.zero;
                e.ScaleTarget = Vector3.one;
                e.PupilSmooth = 0.4f;
                e.PupilTarget = GyoConst.PupilNormal;
            }
            yield return ctx.Wait(0.8f);
        }

        static IEnumerator DtapSpin(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("spin", 1f);
            e.GazeSmooth = 0.02f;
            yield return ctx.Over(0.7f, t =>
            {
                float a = t * Mathf.PI * 2f;
                e.Look(new Vector2(Mathf.Sin(a) * 55f, Mathf.Cos(a) * 45f));
            });
            e.GazeSmooth = 0.09f;
            e.LookForward();
            e.Blink();
            yield return ctx.Wait(0.4f);
        }

        static IEnumerator DtapPeekaboo(ReactionContext ctx)
        {
            ctx.BothLids(0f, 0.6f);
            ctx.Sfx("blink", 0.8f, 0.6f);
            yield return ctx.Wait(0.8f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.LidSmooth = 0.025f;
                e.Lids(1f, 1f);
                e.PupilTarget = GyoConst.PupilBig;
                e.ScaleTarget = new Vector3(1.1f, 1.1f, 1.1f);
                e.Kick(Vector2.up * 1.2f);
            }
            ctx.Sfx("surprise", 1.3f);
            ctx.Emote("ばあ", ctx.Target, new Vector2(0f, 1.05f), Emotes.Style.Big);
            yield return ctx.Wait(0.5f);
            ctx.BothScale(Vector3.one);
            yield return ctx.Wait(0.6f);
        }

        static IEnumerator DtapHiccup(ReactionContext ctx)
        {
            for (int i = 0; i < 2; i++)
            {
                ctx.Sfx("hiccup", ctx.Rand(0.95f, 1.1f));
                foreach (var e in ctx.Eyes.Both)
                {
                    e.Kick(Vector2.up * 1.6f);
                    e.Blink(0.12f);
                }
                yield return ctx.Wait(0.5f);
            }
            yield return ctx.Wait(0.3f);
        }

        static IEnumerator DtapBgJump(ReactionContext ctx)
        {
            ctx.Sfx("hop", 1f);
            yield return Hop(ctx, ctx.Eyes.A, 0.22f, 0.35f);
            ctx.Sfx("hop", 1.15f);
            yield return Hop(ctx, ctx.Eyes.B, 0.22f, 0.35f);
        }

        static IEnumerator BgZoom(ReactionContext ctx)
        {
            ctx.Sfx("whoosh", 1f, 0.7f);
            ctx.BothSmooth(0.05f);
            ctx.Eyes.LookAtScreen(ctx.Screen);
            foreach (var e in ctx.Eyes.Both)
            {
                e.ScaleSmooth = 0.12f;
                e.ScaleTarget = new Vector3(1.22f, 1.22f, 1.22f);
                var p = ctx.Eyes.PlacementOf(e);
                Vector2 toward = (ctx.Screen - p.center) / Mathf.Max(1f, p.radius);
                e.OffsetSmooth = 0.15f;
                e.OffsetTarget = Vector2.ClampMagnitude(toward, 1f) * 0.18f;
            }
            yield return ctx.Wait(1.2f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.ScaleTarget = Vector3.one;
                e.OffsetTarget = Vector2.zero;
            }
            yield return ctx.Wait(0.5f);
        }

        // ------------------------------------------------------------------
        static IEnumerator TtapDizzy(ReactionContext ctx)
        {
            ctx.Sfx("dizzy", 1f);
            ctx.SetMood(Mood.Dizzy, 5f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.Shape = PupilShape.Spiral;
                e.Lids(0.8f, 0.9f);
            }
            ctx.Emote("@", ctx.Target, new Vector2(0.7f, 0.9f));
            yield return ctx.Over(2.2f, t =>
            {
                float a = t * 14f;
                foreach (var e in ctx.Eyes.Both)
                {
                    e.Spin = a * 1.2f;
                    e.GazeSmooth = 0.03f;
                    e.Look(new Vector2(Mathf.Sin(a) * 18f, Mathf.Cos(a * 1.3f) * 12f));
                    e.OffsetSmooth = 0.05f;
                    e.OffsetTarget = new Vector2(Mathf.Sin(a * 0.7f), Mathf.Cos(a * 0.9f)) * 0.05f * (1f - t);
                }
            });
            ctx.BothOffset(Vector2.zero);
        }

        static IEnumerator TtapAngry(ReactionContext ctx)
        {
            ctx.Sfx("growl", 1f);
            ctx.Haptic(50);
            ctx.SetMood(Mood.Angry, 8f);
            ctx.Eyes.ResetToNeutral();
            foreach (var e in ctx.Eyes.Both)
            {
                e.LidSmooth = 0.04f;
                e.TiltSmooth = 0.06f;
                e.LookAt(ctx.World);
            }
            ctx.Emote("！", ctx.Target, new Vector2(0.8f, 0.9f), Emotes.Style.Angry);
            yield return ctx.Wait(0.4f);
            ctx.BothShake(0.02f, 0.3f);
            yield return ctx.Wait(1.0f);
        }

        static IEnumerator TtapSneeze(ReactionContext ctx)
        {
            // ため → くしゃみ
            foreach (var e in ctx.Eyes.Both)
            {
                e.LidSmooth = 0.1f;
                e.Lids(0.55f, 0.7f);
                e.Look(new Vector2(0f, 18f));
                e.ScaleTarget = new Vector3(0.96f, 1.04f, 1f);
            }
            yield return ctx.Wait(0.7f);
            ctx.Sfx("sneeze", ctx.Rand(0.95f, 1.1f));
            ctx.Haptic(40);
            foreach (var e in ctx.Eyes.Both)
            {
                e.LidSmooth = 0.02f;
                e.Lids(0f, 0.3f);
                e.Kick(Vector2.down * 2.6f);
                e.ScaleTarget = new Vector3(1.12f, 0.9f, 1f);
            }
            yield return ctx.Wait(0.35f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.Lids(1f, 1f);
                e.ScaleTarget = Vector3.one;
                e.LookForward();
            }
            ctx.Sweat(ctx.Target);
            yield return ctx.Wait(0.6f);
        }

        // ------------------------------------------------------------------
        static IEnumerator RapidPanic(ReactionContext ctx)
        {
            ctx.Sfx("rattle", 1.2f, 0.8f);
            ctx.BothPupil(GyoConst.PupilSmall);
            ctx.Sweat(ctx.Target);
            yield return LookAround(ctx, 7, 0.12f);
            ctx.Sweat(ctx.Other);
            yield return ctx.Wait(0.3f);
        }

        static IEnumerator RapidLaugh(ReactionContext ctx)
        {
            ctx.Sfx("giggle", ctx.Rand(0.95f, 1.1f));
            ctx.SetMood(Mood.Happy, 4f);
            foreach (var e in ctx.Eyes.Both) { e.LidSmooth = 0.04f; e.Lids(0.9f, 0.35f); }
            yield return ctx.Over(1.2f, t =>
            {
                float b = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 5f));
                foreach (var e in ctx.Eyes.Both)
                {
                    e.OffsetSmooth = 0.03f;
                    e.OffsetTarget = new Vector2(0f, b * 0.08f);
                    e.ScaleTarget = new Vector3(1f + b * 0.05f, 1f - b * 0.05f, 1f);
                }
            });
            ctx.BothOffset(Vector2.zero);
            ctx.BothScale(Vector3.one);
        }

        static IEnumerator RapidOverload(ReactionContext ctx)
        {
            ctx.Sfx("dizzy", 1.3f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.Shape = PupilShape.Cross;
                e.Lids(0.5f, 0.8f);
                e.TintTarget = new Color(0.9f, 1f, 0.92f);
            }
            ctx.Emote("×", ctx.Target, new Vector2(0.7f, 0.9f));
            ctx.BothShake(0.03f, 1.2f);
            yield return ctx.Wait(1.6f);
            ctx.BothShape(PupilShape.Round);
            ctx.BothShake(0.06f, 0.3f);
            ctx.Sfx("pop", 1f);
            yield return ctx.Wait(0.5f);
        }

        static IEnumerator RapidDisco(ReactionContext ctx)
        {
            ctx.Sfx("sparkle", 1f);
            ctx.Haptic(30);
            ctx.BothShape(PupilShape.Star);
            ctx.BothSparkle(1f);
            yield return ctx.Over(2.4f, t =>
            {
                float h = (t * 3f) % 1f;
                Color c = Color.HSVToRGB(h, 0.75f, 0.95f);
                Color cin = Color.HSVToRGB((h + 0.1f) % 1f, 0.4f, 1f);
                foreach (var e in ctx.Eyes.Both)
                {
                    e.ColorSmooth = 0.03f;
                    e.IrisTarget = c;
                    e.IrisInnerTarget = cin;
                    e.OffsetSmooth = 0.04f;
                    e.OffsetTarget = new Vector2(0f, Mathf.Abs(Mathf.Sin(t * Mathf.PI * 8f)) * 0.06f);
                }
            });
            var it = Themes.IrisOf(SaveSystem.Settings.irisTheme);
            foreach (var e in ctx.Eyes.Both)
            {
                e.ColorSmooth = 0.4f;
                e.IrisTarget = it.outer;
                e.IrisInnerTarget = it.inner;
                e.OffsetTarget = Vector2.zero;
            }
            ctx.BothSparkle(0f);
            yield return ctx.Wait(0.5f);
        }

        // ------------------------------------------------------------------
        static IEnumerator BgTapLook(ReactionContext ctx)
        {
            ctx.Sfx("blink", 1.4f, 0.4f);
            ctx.BothSmooth(0.05f);
            ctx.Eyes.LookAtScreen(ctx.Screen);
            ctx.Emote("？", ctx.Target, new Vector2(0.7f, 0.85f));
            yield return ctx.Wait(1.0f);
        }

        static IEnumerator BgTapStartle(ReactionContext ctx)
        {
            ctx.Eyes.Blink(0.12f);
            ctx.Sfx("surprise", 1.4f, 0.5f);
            ctx.BothKick(((Vector2)(ctx.Target.WorldCenter - ctx.World)).normalized * 0.5f);
            yield return ctx.Wait(0.15f);
            ctx.BothSmooth(0.04f);
            ctx.Eyes.LookAtScreen(ctx.Screen);
            ctx.BothPupil(GyoConst.PupilSmall);
            yield return ctx.Wait(0.8f);
        }

        static IEnumerator BgTapIgnore(ReactionContext ctx)
        {
            ctx.BothSmooth(0.08f);
            ctx.Eyes.LookAtScreen(ctx.Screen);
            yield return ctx.Wait(0.35f);
            ctx.BothLids(0.6f, 1f);
            ctx.Eyes.LookAtViewer();
            ctx.Sfx("yawn", 1.5f, 0.3f);
            ctx.Emote("…", ctx.Target, new Vector2(0.75f, 0.7f), Emotes.Style.Soft);
            yield return ctx.Wait(1.0f);
        }

        static IEnumerator BgTapKnock(ReactionContext ctx)
        {
            ctx.Sfx("knock", 1f);
            ctx.Haptic(20);
            ctx.BothSmooth(0.04f);
            ctx.Eyes.LookAtScreen(ctx.Screen);
            foreach (var e in ctx.Eyes.Both)
            {
                e.Lids(0.7f, 0.85f);
                var p = ctx.Eyes.PlacementOf(e);
                Vector2 toward = (ctx.Screen - p.center) / Mathf.Max(1f, p.radius);
                e.OffsetSmooth = 0.08f;
                e.OffsetTarget = Vector2.ClampMagnitude(toward, 1f) * 0.1f;
            }
            yield return ctx.Wait(1.1f);
            ctx.BothOffset(Vector2.zero);
            yield return ctx.Wait(0.3f);
        }
    }
}
