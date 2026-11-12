using System;
using System.Collections;
using UnityEngine;

namespace Gyorome
{
    /// <summary>
    /// 反応を演じるときの道具箱。さわった目（Target）・もう片方（Other）・さわった場所・音・エフェクトと、
    /// 時間をかけて値を変える Over / Wait。指をはなすまでつづく操作は Latest / StreamActive で追う。
    /// </summary>
    public class ReactionContext
    {
        public EyePair Eyes;
        public EyeRig Target;
        public EyeRig Other;
        public GestureEvent Event;
        public GestureEvent Latest;
        public bool StreamActive;
        public Zone Zone;
        public Vector2 Screen;
        public Vector3 World;
        public ReactionDirector Director;
        public AudioManager Audio;
        public Emotes Fx;
        public System.Random Rng = new System.Random();

        // 文脈
        public Mood Mood => Eyes != null ? Eyes.Mood : Mood.Normal;
        public bool Asleep => Mood == Mood.Asleep;
        public float IdleSeconds;
        public int TapCount => Event.count;
        public int Fingers => Mathf.Max(1, Event.fingers);
        public Vector2 Dir => Event.dir;
        public float Magnitude => Event.magnitude;
        public string LastReactionId;
        public float SinceLastReaction;
        public int SameEyeTapStreak;
        public bool DoublePoke;
        public bool Rhythmic;
        public int TicklesInRow;
        public int RubsInRow;
        public int ShakesInRow;
        public bool FirstTouchAfterLongIdle;
        public int Hour = DateTime.Now.Hour;

        public EyeRig Left => Eyes.Left;
        public EyeRig Right => Eyes.Right;
        public Vector3 LatestWorld => Eyes.ScreenToWorld(Latest.pos);
        public Vector3 LatestWorld2 => Eyes.ScreenToWorld(Latest.pos2);
        public bool TargetIsLeft => Target != null && Target.isLeft;
        /// <summary>目の中心から見た、さわった場所（半径 1 の単位。右・上が正）。</summary>
        public Vector2 LocalOnTarget
        {
            get
            {
                if (Target == null) return Vector2.zero;
                var p = Eyes.PlacementOf(Target);
                return (Screen - p.center) / Mathf.Max(1f, p.radius);
            }
        }
        /// <summary>スワイプなどの向き（画面の右・上が正）。横向きか。</summary>
        public bool Horizontal => Mathf.Abs(Dir.x) >= Mathf.Abs(Dir.y);
        public bool Up => Dir.y > 0.5f;
        public bool Down => Dir.y < -0.5f;
        public bool RightDir => Dir.x > 0.5f;
        public bool LeftDir => Dir.x < -0.5f;
        public float Rand(float a, float b) => (float)(a + (b - a) * Rng.NextDouble());
        public bool Chance(float p) => Rng.NextDouble() < p;

        // ------------------------------------------------------------------
        public IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>dur 秒かけて f(t) を 0..1 で呼ぶ（最後に必ず 1 で呼ぶ）。</summary>
        public IEnumerator Over(float dur, Action<float> f)
        {
            float t = 0f;
            if (dur <= 0f) { f(1f); yield break; }
            while (t < dur)
            {
                f(Mathf.Clamp01(t / dur));
                t += Time.deltaTime;
                yield return null;
            }
            f(1f);
        }

        /// <summary>指をはなすまで f を毎フレーム呼ぶ（最長 maxSeconds）。</summary>
        public IEnumerator WhileHeld(Action f, float maxSeconds = 20f)
        {
            float t = 0f;
            while (StreamActive && t < maxSeconds)
            {
                f?.Invoke();
                t += Time.deltaTime;
                yield return null;
            }
        }

        public IEnumerator Until(Func<bool> cond, float timeout = 5f)
        {
            float t = 0f;
            while (!cond() && t < timeout)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        // ------------------------------------------------------------------
        public void Sfx(string name, float pitch = 1f, float volume = 1f) => Audio?.Play(name, pitch, volume);
        public void Haptic(int ms = 30) => Haptics.Pulse(ms);
        public void Emote(string text, EyeRig at, Vector2 offsetRadii, Emotes.Style style = Emotes.Style.Pop) => Fx?.Spawn(text, at, offsetRadii, style);
        public void Emote(string text, EyeRig at) => Emote(text, at, new Vector2(0.75f, 0.75f));
        public void Tear(EyeRig eye, int side = 1) => Fx?.Tear(eye, side);
        public void Sweat(EyeRig eye) => Fx?.Sweat(eye);
        public void Say(string text) => Director?.ShowName(text, Target ?? Eyes.A);

        // 両目にまとめて
        public void BothLook(Vector2 yawPitch) { foreach (var e in Eyes.Both) e.Look(yawPitch); }
        public void BothLookDir(Vector2 dir01) { foreach (var e in Eyes.Both) e.LookDir(dir01); }
        public void BothLids(float upper, float lower) { foreach (var e in Eyes.Both) e.Lids(upper, lower); }
        public void BothPupil(float size) { foreach (var e in Eyes.Both) e.PupilTarget = size; }
        public void BothShape(PupilShape s) { foreach (var e in Eyes.Both) e.Shape = s; }
        public void BothScale(Vector3 s) { foreach (var e in Eyes.Both) e.ScaleTarget = s; }
        public void BothTilt(float t) { foreach (var e in Eyes.Both) e.TiltTarget = t; }
        public void BothTint(Color c) { foreach (var e in Eyes.Both) e.TintTarget = c; }
        public void BothKick(Vector2 impulse) { foreach (var e in Eyes.Both) e.Kick(impulse); }
        public void BothShake(float amp, float dur) { foreach (var e in Eyes.Both) e.Shake(amp, dur); }
        public void BothSmooth(float gaze) { foreach (var e in Eyes.Both) e.GazeSmooth = gaze; }
        public void BothSpin(float spin) { foreach (var e in Eyes.Both) e.Spin = spin; }
        public void BothRoll(float deg) { foreach (var e in Eyes.Both) e.RollTarget = deg; }
        public void BothSparkle(float v) { foreach (var e in Eyes.Both) e.Sparkle = v; }
        public void BothOffset(Vector2 o) { foreach (var e in Eyes.Both) e.OffsetTarget = o; }
        public void SetMood(Mood m, float seconds) => Eyes.SetMood(m, seconds);
    }
}
