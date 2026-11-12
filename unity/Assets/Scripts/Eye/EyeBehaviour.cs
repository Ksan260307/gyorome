using System;
using UnityEngine;

namespace Gyorome
{
    /// <summary>
    /// さわっていないときの目のふるまい：まばたき・きょろきょろ・小さな目の動き・あくび・たいくつ・うとうと・ねむる・夢・
    /// ウインク・くしゃみ・見えないハエを目で追う。反応（ReactionDirector）が動いている間は休む。
    /// 乱数は Seed で固定できる（テスト用）。
    /// </summary>
    public class EyeBehaviour : MonoBehaviour
    {
        public EyePair eyes;
        public ReactionDirector director;
        public GyoInput input;
        public AudioManager audioManager;
        public Emotes emotes;

        public int Seed = 0;
        public bool Paused;
        /// <summary>たいくつ・うとうと・ねむりまでの秒数。</summary>
        public float BoredAfter = 15f;
        public float DrowsyAfter = 30f;
        public float SleepAfter = 45f;
        /// <summary>夜（23 時〜5 時）扱いにする（テスト用。null なら時計を見る）。</summary>
        public bool? NightOverride;

        System.Random _rng;
        float _nextBlink;
        float _nextGaze;
        float _gazeHoldUntil;
        float _nextMicro;
        float _nextYawn;
        float _nextWink;
        float _nextSneeze;
        float _nextFly;
        float _flyUntil;
        Vector2 _flyPos, _flyVel;
        float _nextSnore;
        float _nextDream;
        float _nextZzz;
        bool _boredDone, _drowsyDone, _sleepDone, _nightReported;
        float _wakeSparkleUntil;
        public float IdleSeconds => input != null ? Time.time - input.LastInputTime : 0f;
        public bool IsNight => NightOverride ?? (DateTime.Now.Hour >= 23 || DateTime.Now.Hour < 5);
        public int IdleBlinks { get; private set; }
        public int GazeChanges { get; private set; }
        public bool FollowingFly => Time.time < _flyUntil;
        public float IdleStart;

        void Start()
        {
            _rng = new System.Random(Seed == 0 ? Environment.TickCount : Seed);
            float now = Time.time;
            _nextBlink = now + Range(1.5f, 4f);
            _nextGaze = now + Range(1.5f, 3.5f);
            _nextMicro = now + 0.4f;
            _nextYawn = now + Range(40f, 90f);
            _nextWink = now + Range(60f, 120f);
            _nextSneeze = now + Range(90f, 200f);
            _nextFly = now + Range(20f, 40f);
            if (IsNight) _nightReported = false;
        }

        float Range(float a, float b) => (float)(a + (b - a) * _rng.NextDouble());
        bool Chance(float p) => _rng.NextDouble() < p;

        public void ResetIdle()
        {
            _boredDone = _drowsyDone = _sleepDone = false;
        }

        /// <summary>反応でねむった（bg_hold_sleep など）。idle の進み具合もねむった状態にそろえる。</summary>
        public void ForceSleep()
        {
            _boredDone = _drowsyDone = _sleepDone = true;
            float now = Time.time;
            _nextSnore = now + 1f;
            _nextDream = now + Range(6f, 12f);
            _nextZzz = now + 1.5f;
        }

        /// <summary>目をさます（入力があったとき GameManager が呼ぶ）。</summary>
        public void WakeUp()
        {
            if (eyes.Mood == Mood.Asleep || eyes.Mood == Mood.Sleepy || eyes.Mood == Mood.Bored)
            {
                eyes.SetMood(Mood.Normal, 0f);
                eyes.ResetToNeutral();
            }
            ResetIdle();
        }

        void Update()
        {
            if (Paused || eyes == null || eyes.A == null) return;
            float now = Time.time;
            float idle = IdleSeconds;
            if (idle < 1f) ResetIdle();
            bool busy = director != null && director.Busy;
            bool asleep = eyes.Mood == Mood.Asleep;

            // 夜はひとみが大きく、まばたきがゆっくり。1 度だけ図鑑に記録
            if (IsNight && !_nightReported && !busy)
            {
                _nightReported = true;
                Report("idle_night_mode");
            }

            if (!busy)
            {
                float sleepMul = IsNight ? 0.6f : 1f;
                if (!asleep)
                {
                    // ---- まばたき ----
                    if (now >= _nextBlink)
                    {
                        eyes.Blink(Range(0.15f, 0.22f));
                        IdleBlinks++;
                        _nextBlink = now + Range(2.2f, 6.5f) * (IsNight ? 1.4f : 1f);
                        if (Chance(0.15f)) _nextBlink = now + 0.35f;   // 2 回つづけて
                        Report("idle_blink");
                        audioManager?.Play("blink", 1f, 0.25f);
                    }
                    // ---- きょろきょろ ----
                    if (now >= _nextGaze && !FollowingFly)
                    {
                        PickGaze(now);
                        GazeChanges++;
                    }
                    else if (now >= _gazeHoldUntil && !FollowingFly)
                    {
                        // もどす
                        foreach (var e in eyes.Both) if (e.Mood == Mood.Normal) e.GazeSmooth = 0.12f;
                        if (eyes.Mood == Mood.Normal || eyes.Mood == Mood.Happy) eyes.LookAtViewer();
                        _gazeHoldUntil = float.MaxValue;
                    }
                    // ---- 小さな目の動き ----
                    if (now >= _nextMicro && !FollowingFly)
                    {
                        _nextMicro = now + Range(0.3f, 0.9f);
                        Vector2 j = new Vector2(Range(-1.5f, 1.5f), Range(-1f, 1f));
                        foreach (var e in eyes.Both) e.Look(e.GazeTarget + j);
                    }
                    // ---- 見えないハエ ----
                    if (now >= _nextFly && idle > 6f && !FollowingFly && eyes.Mood == Mood.Normal)
                    {
                        _nextFly = now + Range(25f, 50f);
                        if (Chance(0.6f))
                        {
                            _flyUntil = now + 4.5f;
                            _flyPos = new Vector2(Range(-0.5f, 0.5f), Range(-0.3f, 0.5f));
                            _flyVel = new Vector2(Range(-0.6f, 0.6f), Range(-0.4f, 0.4f));
                            Report("idle_focus_bug");
                        }
                    }
                    if (FollowingFly)
                    {
                        float dt = Time.deltaTime;
                        _flyVel += new Vector2(Range(-4f, 4f), Range(-3f, 3f)) * dt;
                        _flyVel = Vector2.ClampMagnitude(_flyVel, 1.2f);
                        _flyPos += _flyVel * dt;
                        _flyPos.x = Mathf.Clamp(_flyPos.x, -0.8f, 0.8f);
                        _flyPos.y = Mathf.Clamp(_flyPos.y, -0.6f, 0.7f);
                        foreach (var e in eyes.Both) { e.GazeSmooth = 0.05f; e.LookDir(_flyPos); }
                        if (now >= _flyUntil) { foreach (var e in eyes.Both) e.GazeSmooth = 0.09f; eyes.LookAtViewer(); }
                    }
                    // ---- あくび ----
                    if (now >= _nextYawn && idle > 8f)
                    {
                        _nextYawn = now + Range(40f, 90f);
                        director?.PlayIdle("idle_yawn");
                    }
                    // ---- ウインク ----
                    if (now >= _nextWink && idle > 3f)
                    {
                        _nextWink = now + Range(60f, 120f);
                        if (Chance(0.5f)) director?.PlayIdle("idle_wink_at_you");
                    }
                    // ---- くしゃみ ----
                    if (now >= _nextSneeze && idle > 3f)
                    {
                        _nextSneeze = now + Range(90f, 200f);
                        director?.PlayIdle("idle_sneeze_random");
                    }
                    // ---- たいくつ → うとうと → ねむる ----
                    if (idle > BoredAfter && !_boredDone)
                    {
                        _boredDone = true;
                        director?.PlayIdle("idle_bored");
                    }
                    if (idle > DrowsyAfter * sleepMul && !_drowsyDone)
                    {
                        _drowsyDone = true;
                        eyes.SetMood(Mood.Sleepy, 0f);
                        eyes.ResetToNeutral();
                        Report("idle_drowsy");
                    }
                    if (idle > SleepAfter * sleepMul && !_sleepDone)
                    {
                        _sleepDone = true;
                        eyes.SetMood(Mood.Asleep, 0f);
                        eyes.ResetToNeutral();
                        _nextSnore = now + 1f;
                        _nextDream = now + Range(6f, 12f);
                        _nextZzz = now + 1.5f;
                        Report("idle_sleep");
                    }
                }
                else
                {
                    // ---- ねむっている ----
                    float breath = 1f + 0.015f * Mathf.Sin(now * 1.6f);
                    foreach (var e in eyes.Both) e.ScaleTarget = new Vector3(breath, 1f / breath, 1f);
                    if (now >= _nextSnore)
                    {
                        _nextSnore = now + 3.2f;
                        audioManager?.Play("snore", Range(0.9f, 1.1f), 0.4f);
                    }
                    if (now >= _nextZzz)
                    {
                        _nextZzz = now + Range(1.6f, 2.6f);
                        emotes?.Spawn("z", eyes.B, new Vector2(0.7f, 0.6f), Emotes.Style.Soft);
                    }
                    if (now >= _nextDream)
                    {
                        _nextDream = now + Range(6f, 14f);
                        director?.PlayIdle("idle_dream");
                    }
                }
            }
        }

        void PickGaze(float now)
        {
            _nextGaze = now + Range(1.4f, 4.5f);
            float hold = Range(0.5f, 1.6f);
            double r = _rng.NextDouble();
            foreach (var e in eyes.Both) e.GazeSmooth = 0.06f;
            if (eyes.Mood == Mood.Sleepy)
            {
                foreach (var e in eyes.Both) e.Look(new Vector2(Range(-10f, 10f), Range(-18f, -5f)));
            }
            else if (r < 0.5)
            {
                eyes.LookAtViewer();
                _gazeHoldUntil = float.MaxValue;
                return;
            }
            else if (r < 0.82)
            {
                Vector2 d = new Vector2(Range(-0.7f, 0.7f), Range(-0.5f, 0.6f));
                foreach (var e in eyes.Both) e.LookDir(d);
                Report("idle_look_around");
            }
            else if (r < 0.92)
            {
                // おたがいをちらっと見る
                eyes.A.LookAt(eyes.B.WorldCenter);
                eyes.B.LookAt(eyes.A.WorldCenter);
                hold = Range(0.35f, 0.7f);
                Report("idle_sync_check");
            }
            else
            {
                // はしをのぞく
                float s = Chance(0.5f) ? 1f : -1f;
                foreach (var e in eyes.Both) e.LookDir(new Vector2(s, Range(-0.2f, 0.4f)));
                hold = Range(0.6f, 1.2f);
            }
            _gazeHoldUntil = now + hold;
        }

        void Report(string id) => director?.ReportIdle(id);
    }
}
