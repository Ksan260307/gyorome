using System.Runtime.InteropServices;
using UnityEngine;

namespace Gyorome
{
    /// <summary>ふるえ（スマホ・タブレット・ブラウザ）。設定で切れる。</summary>
    public static class Haptics
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void GyoVibrate(int ms);
#else
        static void GyoVibrate(int ms) { }
#endif
        public static int Pulses { get; private set; }

        public static void Pulse(int ms = 30)
        {
            if (!SaveSystem.Settings.haptics) return;
            Pulses++;
#if UNITY_WEBGL && !UNITY_EDITOR
            GyoVibrate(ms);
#elif (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (ms >= 20) Handheld.Vibrate();
#endif
        }
    }
}
