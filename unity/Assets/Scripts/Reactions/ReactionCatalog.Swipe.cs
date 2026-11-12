using System.Collections;
using UnityEngine;

namespace Gyorome
{
    public static partial class ReactionCatalog
    {
        const string CatSwipe = "スワイプ・なでる";

        static void BuildSwipe()
        {
            // ---------------- 目をスワイプ ----------------
            R("swipe_up_roll", "あきれ", CatSwipe, "目を上にスワイプ", ctx => SwipeUpRoll(ctx), Zone.Eye, GestureType.Swipe, GestureType.Flick).When(c => c.Up);
            R("swipe_lid_pull_up", "ぱっちり", CatSwipe, "目の下から上へスワイプして、まぶたをひらく", ctx => SwipeLidPullUp(ctx), Zone.Eye, GestureType.Swipe).When(c => c.Up && c.LocalOnTarget.y < 0f).WithPriority(2);
            R("swipe_down_sad", "しょんぼり", CatSwipe, "目を下にスワイプ", ctx => SwipeDownSad(ctx), Zone.Eye, GestureType.Swipe, GestureType.Flick).When(c => c.Down);
            R("swipe_lid_pull_down", "おやすみ", CatSwipe, "目の上から下へスワイプして、まぶたを閉じる", ctx => SwipeLidPullDown(ctx), Zone.Eye, GestureType.Swipe).When(c => c.Down && c.LocalOnTarget.y > 0f).WithPriority(2);
            R("swipe_side_follow", "すいっ", CatSwipe, "目を横にスワイプ", ctx => SwipeSideFollow(ctx), Zone.Eye, GestureType.Swipe).When(c => c.Horizontal);
            R("swipe_push_rotate", "ころん", CatSwipe, "目を横にはじくと、目玉が転がる", ctx => SwipePushRotate(ctx), Zone.Eye, GestureType.Swipe, GestureType.Flick).When(c => c.Horizontal);
            R("swipe_wipe_clean", "きゅっきゅ", CatSwipe, "目を横にスワイプして、みがく", ctx => SwipeWipeClean(ctx), Zone.Eye, GestureType.Swipe).When(c => c.Horizontal).WithWeight(0.7f);
            R("swipe_flick_fast", "ビュン", CatSwipe, "目を、はやくはじく", ctx => SwipeFlickFast(ctx), Zone.Eye, GestureType.Flick).WithPriority(2);
            R("swipe_diagonal_tumble", "ごろん", CatSwipe, "目をななめにスワイプ", ctx => SwipeDiagonal(ctx), Zone.Eye, GestureType.Swipe, GestureType.Flick).When(c => Mathf.Abs(c.Dir.x) > 0.4f && Mathf.Abs(c.Dir.y) > 0.4f).WithPriority(2);

            // ---------------- 背景をスワイプ ----------------
            R("bg_swipe_wind", "びゅー", CatSwipe, "目の外をスワイプすると、風がふく", ctx => BgSwipeWind(ctx), Zone.Background, GestureType.Swipe, GestureType.Flick);
            R("bg_swipe_track", "すーっ", CatSwipe, "目の外をスワイプすると、目で追う", ctx => BgSwipeTrack(ctx), Zone.Background, GestureType.Swipe);
            R("bg_swipe_dodge", "ひょい", CatSwipe, "目の外をはやくはじくと、よける", ctx => BgSwipeDodge(ctx), Zone.Background, GestureType.Flick, GestureType.Swipe).WithWeight(0.8f);
            R("bg_swipe_curtain", "しゃっ", CatSwipe, "目の外を横にスワイプすると、順にまばたき", ctx => BgSwipeCurtain(ctx), Zone.Background, GestureType.Swipe, GestureType.Flick).When(c => c.Horizontal);

            // ---------------- ドラッグ・こする・円 ----------------
            R("drag_follow", "ついてく", CatSwipe, "指をゆっくり動かすと、目で追う", ctx => DragFollow(ctx), Zone.Background, GestureType.DragBegin);
            R("drag_eye_move", "ずるずる", CatSwipe, "目をつかんで、ゆっくり動かす", ctx => DragEyeMove(ctx), Zone.Eye, GestureType.DragBegin);
            R("drag_tickle", "こちょこちょ", CatSwipe, "目の下の方を、こちょこちょ", ctx => DragTickle(ctx), Zone.EyeBottom, GestureType.DragBegin).WithPriority(2);
            R("drag_pet", "なでなで", CatSwipe, "目の上を、ゆっくりなでる", ctx => DragPet(ctx), Zone.EyeTop, GestureType.DragBegin).WithPriority(2);
            R("rub_itchy", "かゆい", CatSwipe, "目をこする", ctx => RubItchy(ctx), Zone.Eye, GestureType.Rub);
            R("rub_sparkle", "ぴかーん", CatSwipe, "目をこすって、みがく", ctx => RubSparkle(ctx), Zone.Eye, GestureType.Rub);
            R("rub_angry", "やめて", CatSwipe, "しつこくこする", ctx => RubAngry(ctx), Zone.Eye, GestureType.Rub).When(c => c.RubsInRow >= 3).WithPriority(2);
            R("rub_genie", "もくもく", CatSwipe, "目の外をこすると、魔法のランプみたいに", ctx => RubGenie(ctx), Zone.Background, GestureType.Rub);
            R("circle_cw_spin", "ぐるーん", CatSwipe, "目の上で、時計回りに円をかく", ctx => CircleSpin(ctx, 1f), Zone.Eye, GestureType.Circle).When(c => c.Magnitude > 0f);
            R("circle_ccw_rewind", "きゅるきゅる", CatSwipe, "目の上で、反時計回りに円をかく", ctx => CircleRewind(ctx), Zone.Eye, GestureType.Circle).When(c => c.Magnitude < 0f);
            R("circle_hypnotize", "うっとり", CatSwipe, "目の外で、円をかく", ctx => CircleHypno(ctx), Zone.Background, GestureType.Circle);
            R("circle_on_eye_dizzy", "ふらふら", CatSwipe, "目の上で、何度も円をかく", ctx => CircleDizzy(ctx), Zone.Eye, GestureType.Circle).WithWeight(0.7f);
        }

        // ------------------------------------------------------------------
        static IEnumerator SwipeUpRoll(ReactionContext ctx)
        {
            ctx.Sfx("whoosh", 1.2f, 0.4f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.GazeSmooth = 0.06f;
                e.Look(new Vector2(ctx.Rand(-8f, 8f), GyoConst.MaxPitchDeg));
                e.LidSmooth = 0.1f;
                e.Lids(0.55f, 1f);
            }
            ctx.Emote("…", ctx.Target, new Vector2(0.7f, 0.9f), Emotes.Style.Soft);
            yield return ctx.Wait(1.3f);
            ctx.BothLids(1f, 1f);
            ctx.Eyes.LookAtViewer();
            yield return ctx.Wait(0.3f);
        }

        static IEnumerator SwipeLidPullUp(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("pop", 1.4f, 0.6f);
            e.LidSmooth = 0.03f;
            e.Lids(1f, 1f);
            e.PupilSmooth = 0.03f;
            e.PupilTarget = 0.16f;
            e.ScaleTarget = new Vector3(1.08f, 1.1f, 1.08f);
            ctx.Other.PupilTarget = 0.3f;
            ctx.Emote("！", e, new Vector2(0.7f, 1.0f));
            yield return ctx.Wait(1.0f);
            e.ScaleTarget = Vector3.one;
            e.PupilSmooth = 0.4f;
            e.PupilTarget = GyoConst.PupilNormal;
            yield return ctx.Wait(0.4f);
        }

        static IEnumerator SwipeDownSad(ReactionContext ctx)
        {
            ctx.Sfx("drip", 0.7f, 0.6f);
            ctx.SetMood(Mood.Sad, 6f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.LidSmooth = 0.1f;
                e.TiltSmooth = 0.1f;
                e.Lids(0.75f, 1f);
                e.TiltTarget = -18f;
                e.Look(new Vector2(0f, -30f));
            }
            yield return ctx.Wait(0.8f);
            ctx.Tear(ctx.Target, 1);
            yield return ctx.Wait(0.5f);
            ctx.Tear(ctx.Other, -1);
            yield return ctx.Wait(0.8f);
        }

        static IEnumerator SwipeLidPullDown(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("blink", 0.7f, 0.5f);
            e.LidSmooth = 0.08f;
            e.Lids(0f, 0.7f);
            yield return ctx.Wait(0.4f);
            ctx.Other.LidSmooth = 0.15f;
            ctx.Other.Lids(0f, 0.7f);
            ctx.Sfx("snore", 1.1f, 0.3f);
            ctx.Emote("z", ctx.Other, new Vector2(0.7f, 0.7f), Emotes.Style.Soft);
            yield return ctx.Wait(2.2f);
            ctx.Eyes.Blink(0.3f);
            ctx.BothLids(1f, 1f);
            yield return ctx.Wait(0.4f);
        }

        static IEnumerator SwipeSideFollow(ReactionContext ctx)
        {
            ctx.Sfx("whoosh", 1f, 0.5f);
            Vector2 d = ctx.Dir;
            ctx.BothSmooth(0.035f);
            ctx.BothLookDir(new Vector2(Mathf.Sign(d.x) * 1.1f, d.y * 0.4f));
            yield return ctx.Wait(0.3f);
            ctx.BothSmooth(0.12f);
            ctx.BothLookDir(new Vector2(Mathf.Sign(d.x) * 0.75f, d.y * 0.3f));
            yield return ctx.Wait(0.8f);
            ctx.Eyes.LookAtViewer();
            yield return ctx.Wait(0.3f);
        }

        static IEnumerator SwipePushRotate(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("spin", 0.9f, 0.7f);
            float dir = -Mathf.Sign(ctx.Dir.x);
            float speed = Mathf.Clamp(ctx.Magnitude / 120f, 1f, 4f);
            e.RollSmooth = 0.08f;
            e.GazeSmooth = 0.03f;
            yield return ctx.Over(1.4f, t =>
            {
                float k = Ease.OutQuad(t);
                e.RollTarget = dir * 360f * speed * k;
                e.Look(new Vector2(ctx.Dir.x * 35f * (1f - k), 0f));
            });
            e.RollSmooth = 0.15f;
            e.RollTarget = 0f;
            e.Blink();
            yield return ctx.Wait(0.4f);
        }

        static IEnumerator SwipeWipeClean(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("squish", 1.4f, 0.5f);
            e.Lids(0.45f, 0.75f);
            e.Kick(ctx.Dir * 0.8f);
            yield return ctx.Wait(0.4f);
            e.Lids(1f, 1f);
            e.Sparkle = 1f;
            ctx.Sfx("sparkle", 1.2f, 0.6f);
            ctx.Emote("☆", e, new Vector2(0.6f, 0.85f));
            yield return ctx.Wait(1.0f);
            e.Sparkle = 0f;
            yield return ctx.Wait(0.3f);
        }

        static IEnumerator SwipeFlickFast(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("spin", 1.3f, 0.8f);
            ctx.Haptic(25);
            e.RollSmooth = 0.05f;
            e.RollTarget = -Mathf.Sign(ctx.Dir.x + 0.001f) * 1080f;
            e.Kick(ctx.Dir * 1.5f);
            yield return ctx.Wait(1.0f);
            e.Shape = PupilShape.Spiral;
            e.RollSmooth = 0.3f;
            e.RollTarget = 0f;
            yield return ctx.Over(1.2f, t => { e.Spin = t * 20f; e.Look(new Vector2(Mathf.Sin(t * 25f) * 10f * (1f - t), 0f)); });
            e.Shape = PupilShape.Round;
            yield return ctx.Wait(0.3f);
        }

        static IEnumerator SwipeDiagonal(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("bounce", 1f, 0.7f);
            Vector2 d = ctx.Dir;
            e.OffsetSmooth = 0.05f;
            e.RollSmooth = 0.1f;
            yield return ctx.Over(0.9f, t =>
            {
                float k = Ease.OutBounce(t);
                e.OffsetTarget = d * 0.25f * (1f - k) + d * 0.08f * Mathf.Sin(t * Mathf.PI);
                e.RollTarget = -d.x * 180f * Ease.OutQuad(t);
            });
            e.OffsetTarget = Vector2.zero;
            e.RollTarget = 0f;
            yield return ctx.Wait(0.5f);
        }

        // ------------------------------------------------------------------
        static IEnumerator BgSwipeWind(ReactionContext ctx)
        {
            ctx.Sfx("whoosh", 0.8f, 0.9f);
            Vector2 d = ctx.Dir;
            foreach (var e in ctx.Eyes.Both)
            {
                e.LidSmooth = 0.04f;
                e.Lids(0.35f, 0.7f);
                e.OffsetSmooth = 0.08f;
                e.OffsetTarget = d * 0.12f;
                e.GazeSmooth = 0.05f;
                e.LookDir(-d * 0.5f);
            }
            yield return ctx.Over(0.9f, t =>
            {
                float f = Mathf.Sin(t * 40f) * 0.1f * (1f - t);
                foreach (var e in ctx.Eyes.Both) e.Lids(0.35f + f, 0.7f);
            });
            foreach (var e in ctx.Eyes.Both)
            {
                e.Lids(1f, 1f);
                e.OffsetTarget = Vector2.zero;
            }
            yield return ctx.Wait(0.5f);
        }

        static IEnumerator BgSwipeTrack(ReactionContext ctx)
        {
            ctx.BothSmooth(0.06f);
            Vector2 from = ctx.Event.pos, to = ctx.Event.pos2;
            yield return ctx.Over(0.6f, t => ctx.Eyes.LookAtScreen(Vector2.Lerp(from, to, t)));
            yield return ctx.Wait(0.5f);
            ctx.Eyes.LookAtViewer();
            yield return ctx.Wait(0.3f);
        }

        static IEnumerator BgSwipeDodge(ReactionContext ctx)
        {
            ctx.Sfx("whoosh", 1.3f, 0.6f);
            Vector2 d = ctx.Dir;
            Vector2 side = new Vector2(-d.y, d.x);
            foreach (var e in ctx.Eyes.Both)
            {
                var p = ctx.Eyes.PlacementOf(e);
                float s = Mathf.Sign(Vector2.Dot(p.center - ctx.Screen, side) + 0.001f);
                e.OffsetSmooth = 0.04f;
                e.OffsetTarget = side * s * 0.2f;
                e.Lids(0.5f, 0.8f);
                e.LookDir(-side * s * 0.5f);
            }
            yield return ctx.Wait(0.5f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.OffsetSmooth = 0.15f;
                e.OffsetTarget = Vector2.zero;
                e.Lids(1f, 1f);
            }
            yield return ctx.Wait(0.5f);
        }

        static IEnumerator BgSwipeCurtain(ReactionContext ctx)
        {
            var first = ctx.Dir.x > 0f ? ctx.Eyes.A : ctx.Eyes.B;
            var second = ctx.Eyes.Other(first);
            if (ScreenLayout.IsPortrait(ctx.Eyes.LayoutWidth, ctx.Eyes.LayoutHeight)) { first = ctx.Dir.y < 0f ? ctx.Eyes.A : ctx.Eyes.B; second = ctx.Eyes.Other(first); }
            ctx.Sfx("blink", 0.9f, 0.5f);
            first.Blink(0.22f);
            yield return ctx.Wait(0.18f);
            ctx.Sfx("blink", 1.1f, 0.5f);
            second.Blink(0.22f);
            yield return ctx.Wait(0.5f);
        }

        // ------------------------------------------------------------------
        static IEnumerator DragFollow(ReactionContext ctx)
        {
            ctx.BothSmooth(0.07f);
            yield return ctx.WhileHeld(() => ctx.Eyes.LookAtScreen(ctx.Latest.pos));
            yield return ctx.Wait(0.4f);
        }

        static IEnumerator DragEyeMove(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("squish", 0.8f, 0.5f);
            var p = ctx.Eyes.PlacementOf(e);
            e.OffsetSmooth = 0.06f;
            yield return ctx.WhileHeld(() =>
            {
                Vector2 o = (ctx.Latest.pos - p.center) / Mathf.Max(1f, p.radius);
                e.OffsetTarget = Vector2.ClampMagnitude(o, 0.5f);
                e.LookAt(ctx.LatestWorld);
                ctx.Other.LookAt(e.WorldCenter);
            });
            ctx.Sfx("pop", 1f, 0.7f);
            e.OffsetSmooth = 0.05f;
            e.OffsetTarget = Vector2.zero;
            e.Kick(-e.Offset * 4f);
            yield return ctx.Wait(0.6f);
        }

        static IEnumerator DragTickle(ReactionContext ctx)
        {
            ctx.Director?.NoteTickle();
            ctx.Sfx("giggle", ctx.Rand(1f, 1.2f));
            ctx.Haptic(15);
            ctx.SetMood(Mood.Happy, 5f);
            foreach (var e in ctx.Eyes.Both) { e.LidSmooth = 0.04f; e.Lids(0.85f, 0.35f); }
            float t0 = Time.time;
            yield return ctx.WhileHeld(() =>
            {
                float b = Mathf.Abs(Mathf.Sin((Time.time - t0) * 16f));
                foreach (var e in ctx.Eyes.Both)
                {
                    e.OffsetSmooth = 0.03f;
                    e.OffsetTarget = new Vector2(Mathf.Sin((Time.time - t0) * 23f) * 0.03f, b * 0.07f);
                }
            }, 6f);
            ctx.BothOffset(Vector2.zero);
            yield return ctx.Wait(0.5f);
        }

        static IEnumerator DragPet(ReactionContext ctx)
        {
            ctx.Sfx("purr", 1f, 0.6f);
            ctx.SetMood(Mood.Happy, 6f);
            foreach (var e in ctx.Eyes.Both) { e.LidSmooth = 0.12f; e.Lids(0.15f, 0.5f); }
            ctx.Target.OffsetSmooth = 0.15f;
            yield return ctx.WhileHeld(() =>
            {
                Vector2 d = (ctx.Latest.pos - ctx.Screen) / Mathf.Max(1f, ctx.Target.Radius);
                ctx.Target.OffsetTarget = new Vector2(Mathf.Clamp(d.x, -0.2f, 0.2f) * 0.3f, 0.04f);
            }, 8f);
            ctx.Emote("♥", ctx.Target, new Vector2(0.6f, 0.9f));
            ctx.Target.OffsetTarget = Vector2.zero;
            ctx.Sfx("chime", 1.2f, 0.5f);
            yield return ctx.Wait(0.8f);
        }

        static IEnumerator RubItchy(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("squish", 1.1f, 0.6f);
            e.Lids(0.4f, 0.75f);
            yield return ctx.Over(0.8f, t => { e.Lids(0.4f + Mathf.Sin(t * 50f) * 0.08f, 0.75f); e.OffsetTarget = new Vector2(Mathf.Sin(t * 60f) * 0.015f, 0f); });
            e.OffsetTarget = Vector2.zero;
            e.Lids(1f, 1f);
            ctx.Sfx("sneeze", 1.3f, 0.6f);
            e.Kick(Vector2.down * 1.5f);
            e.Blink(0.2f);
            yield return ctx.Wait(0.6f);
        }

        static IEnumerator RubSparkle(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("sparkle", 1f, 0.8f);
            e.Sparkle = 1f;
            e.PupilTarget = 0.5f;
            ctx.Emote("★", e, new Vector2(0.55f, 0.85f));
            ctx.Emote("☆", e, new Vector2(-0.6f, 0.5f));
            yield return ctx.Wait(1.3f);
            e.Sparkle = 0f;
            yield return ctx.Wait(0.3f);
        }

        static IEnumerator RubAngry(ReactionContext ctx)
        {
            ctx.Sfx("growl", 1.1f);
            ctx.Haptic(50);
            ctx.SetMood(Mood.Angry, 8f);
            ctx.Eyes.ResetToNeutral();
            foreach (var e in ctx.Eyes.Both)
            {
                e.TiltSmooth = 0.05f;
                e.TintTarget = new Color(1f, 0.82f, 0.8f);
                e.LookAt(ctx.World);
            }
            ctx.Target.Kick(Vector2.up * 1.2f);
            ctx.Emote("！", ctx.Target, new Vector2(0.8f, 0.9f), Emotes.Style.Angry);
            yield return ctx.Wait(1.2f);
        }

        static IEnumerator RubGenie(ReactionContext ctx)
        {
            ctx.Sfx("sparkle", 0.8f, 0.9f);
            ctx.Haptic(30);
            Color gold = new Color(1f, 0.85f, 0.3f), goldIn = new Color(1f, 0.97f, 0.7f);
            foreach (var e in ctx.Eyes.Both)
            {
                e.ColorSmooth = 0.15f;
                e.IrisTarget = gold;
                e.IrisInnerTarget = goldIn;
                e.Sparkle = 1f;
                e.PupilTarget = 0.5f;
            }
            for (int i = 0; i < 4; i++)
            {
                ctx.Emote("★", i % 2 == 0 ? ctx.Eyes.A : ctx.Eyes.B, new Vector2(ctx.Rand(-0.7f, 0.7f), ctx.Rand(0.3f, 1f)), Emotes.Style.Soft);
                yield return ctx.Wait(0.2f);
            }
            yield return ctx.Wait(1.0f);
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

        static IEnumerator CircleSpin(ReactionContext ctx, float sign)
        {
            ctx.Sfx("spin", 1f, 0.7f);
            ctx.BothSmooth(0.025f);
            yield return ctx.Over(1.2f, t =>
            {
                float a = t * Mathf.PI * 4f * sign;
                ctx.BothLook(new Vector2(Mathf.Sin(a) * 50f, Mathf.Cos(a) * 42f));
            });
            ctx.BothSmooth(0.09f);
            ctx.Eyes.LookAtViewer();
            ctx.Eyes.Blink();
            yield return ctx.Wait(0.4f);
        }

        static IEnumerator CircleRewind(ReactionContext ctx)
        {
            ctx.Sfx("spin", 0.7f, 0.7f);
            ctx.BothSmooth(0.025f);
            yield return ctx.Over(1.2f, t =>
            {
                float a = -t * Mathf.PI * 4f;
                ctx.BothLook(new Vector2(Mathf.Sin(a) * 50f, Mathf.Cos(a) * 42f));
                ctx.BothPupil(0.25f + 0.35f * Mathf.Abs(Mathf.Sin(t * Mathf.PI * 3f)));
            });
            ctx.BothSmooth(0.09f);
            ctx.Eyes.LookAtViewer();
            ctx.Emote("←", ctx.Target, new Vector2(0.7f, 0.85f));
            yield return ctx.Wait(0.5f);
        }

        static IEnumerator CircleHypno(ReactionContext ctx)
        {
            ctx.Sfx("dizzy", 0.8f, 0.7f);
            ctx.BothShape(PupilShape.Spiral);
            ctx.BothPupil(0.6f);
            ctx.BothLids(0.75f, 1f);
            yield return ctx.Over(2.5f, t =>
            {
                float a = t * 9f;
                foreach (var e in ctx.Eyes.Both)
                {
                    e.Spin = a;
                    e.OffsetSmooth = 0.1f;
                    e.OffsetTarget = new Vector2(Mathf.Sin(a * 0.6f) * 0.05f, 0f);
                }
            });
            ctx.BothShape(PupilShape.Round);
            ctx.BothOffset(Vector2.zero);
            yield return ctx.Wait(0.3f);
        }

        static IEnumerator CircleDizzy(ReactionContext ctx)
        {
            var e = ctx.Target;
            ctx.Sfx("dizzy", 1.1f, 0.6f);
            e.Shape = PupilShape.Spiral;
            e.Lids(0.7f, 0.9f);
            yield return ctx.Over(1.8f, t =>
            {
                e.Spin = t * 12f;
                e.OffsetSmooth = 0.04f;
                e.OffsetTarget = new Vector2(Mathf.Sin(t * 20f), Mathf.Cos(t * 17f)) * 0.05f * (1f - t);
                e.GazeSmooth = 0.03f;
                e.Look(new Vector2(Mathf.Sin(t * 15f) * 15f, 0f));
            });
            e.OffsetTarget = Vector2.zero;
            e.Shape = PupilShape.Round;
            yield return ctx.Wait(0.3f);
        }
    }
}
