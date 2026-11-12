using UnityEngine;

namespace Gyorome
{
    /// <summary>イージング（0..1 → 0..1）。反応の動きに使う。</summary>
    public static class Ease
    {
        public static float Linear(float t) => Mathf.Clamp01(t);
        public static float InQuad(float t) { t = Mathf.Clamp01(t); return t * t; }
        public static float OutQuad(float t) { t = Mathf.Clamp01(t); return 1f - (1f - t) * (1f - t); }
        public static float InOut(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
        public static float OutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
        public static float OutElastic(float t)
        {
            t = Mathf.Clamp01(t);
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            const float c4 = 2f * Mathf.PI / 3f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
        }
        public static float OutBounce(float t)
        {
            t = Mathf.Clamp01(t);
            const float n1 = 7.5625f, d1 = 2.75f;
            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
            if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }
        /// <summary>0 → 1 → 0 の山（なめらか）。</summary>
        public static float Bump(float t) { t = Mathf.Clamp01(t); return Mathf.Sin(t * Mathf.PI); }
        /// <summary>0 → 1 → 0 の山（はやく上がって、ゆっくり下りる）。</summary>
        public static float Flick(float t) { t = Mathf.Clamp01(t); return t < 0.2f ? OutQuad(t / 0.2f) : 1f - InOut((t - 0.2f) / 0.8f); }
        /// <summary>減衰する振動（1 から 0 へ）。</summary>
        public static float Wobble(float t, float cycles = 3f) { t = Mathf.Clamp01(t); return Mathf.Cos(t * Mathf.PI * 2f * cycles) * (1f - t) * (1f - t); }
    }

    /// <summary>ばねで目標に追いつく値（SmoothDamp の薄い包み）。</summary>
    public struct Spring
    {
        public float value;
        public float target;
        public float velocity;
        public float smoothTime;

        public Spring(float v, float smooth)
        {
            value = v;
            target = v;
            velocity = 0f;
            smoothTime = smooth;
        }

        public void Step(float dt)
        {
            if (dt <= 0f) return;
            value = Mathf.SmoothDamp(value, target, ref velocity, Mathf.Max(0.0001f, smoothTime), float.PositiveInfinity, dt);
        }

        public void Snap(float v)
        {
            value = target = v;
            velocity = 0f;
        }
    }

    /// <summary>減衰振動のばね（つついたときの「ぷるん」用）。</summary>
    public struct Jiggle
    {
        public Vector2 offset;
        public Vector2 velocity;
        public float stiffness;
        public float damping;

        public Jiggle(float stiffness, float damping)
        {
            offset = Vector2.zero;
            velocity = Vector2.zero;
            this.stiffness = stiffness;
            this.damping = damping;
        }

        public void Kick(Vector2 impulse) => velocity += impulse;

        public void Step(float dt)
        {
            if (dt <= 0f) return;
            dt = Mathf.Min(dt, 1f / 30f);
            Vector2 accel = -stiffness * offset - damping * velocity;
            velocity += accel * dt;
            offset += velocity * dt;
            if (offset.sqrMagnitude < 1e-8f && velocity.sqrMagnitude < 1e-8f)
            {
                offset = Vector2.zero;
                velocity = Vector2.zero;
            }
        }

        public bool IsStill => offset.sqrMagnitude < 1e-6f && velocity.sqrMagnitude < 1e-6f;
    }
}
