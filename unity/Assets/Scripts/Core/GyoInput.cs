using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Gyorome
{
    /// <summary>
    /// タッチ・マウス・センサー（ふる・かたむける）をまとめて GestureRecognizer に流す。
    /// テスト用に、画面をさわる操作をコードから入れられる（VirtualDown など）。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GyoInput : MonoBehaviour
    {
        public GestureRecognizer Recognizer { get; private set; } = new GestureRecognizer();
        public event Action<GestureEvent> Gesture;

        /// <summary>最後に入力があった時刻（Time.time）。</summary>
        public float LastInputTime { get; private set; }
        public bool AnyPointerDown => Recognizer.ActivePointers > 0;
        /// <summary>UI の上の指は目に届かない（GameUI が入れる）。</summary>
        public Func<Vector2, bool> IsOverUI;
        /// <summary>タッチ操作ができる端末か。</summary>
        public static bool TouchCapable => Touchscreen.current != null || Application.isMobilePlatform;

        /// <summary>画面上の指の位置（デバッグ・テスト用）。</summary>
        public readonly Dictionary<int, Vector2> Pointers = new Dictionary<int, Vector2>();

        readonly List<GestureEvent> _events = new List<GestureEvent>();
        bool _mouseDown;
        const int MousePointer = -1;

        // ふる・かたむける
        Vector3 _accelSmooth = new Vector3(0f, -1f, 0f);
        float _lastShake = -10f;
        bool _tilting;
        float _nextTilt;
        public float shakeThreshold = 2.2f;
        public float tiltThreshold = 0.28f;
        /// <summary>かたむきの入力を止める（テスト・設定）。</summary>
        public bool sensorsEnabled = true;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            // 加速度センサー：手に持つ端末だけ（PC のブラウザでは許可のエラーが出るだけなので使わない）
            var acc = Accelerometer.current;
            bool handheld = Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
            if (acc != null && !acc.enabled && handheld)
            {
                try { InputSystem.EnableDevice(acc); }
                catch (System.Exception e) { Debug.LogWarning("加速度センサーを使えません: " + e.Message); }
            }
            LastInputTime = Time.time;
            ApplyScreenScale();
        }

        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        /// <summary>画面の大きさに合わせて、タップとみなす距離などを決める。</summary>
        public void ApplyScreenScale()
        {
            float unit = Mathf.Min(Screen.width, Screen.height);
            float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
            float mm = dpi / 25.4f;   // 1mm のピクセル数
            Recognizer.tapMaxDist = Mathf.Clamp(mm * 4f, 10f, unit * 0.06f);
            Recognizer.swipeMinDist = Mathf.Clamp(mm * 12f, 40f, unit * 0.2f);
            Recognizer.circleMinSize = Mathf.Clamp(mm * 14f, 50f, unit * 0.3f);
            Recognizer.flickSpeed = Mathf.Max(900f, mm * 300f);
        }

        void Update()
        {
            float now = Time.time;
            bool touched = false;
            if (EnhancedTouchSupport.enabled)
            {
                foreach (var t in Touch.activeTouches)
                {
                    touched = true;
                    int id = t.touchId;
                    Vector2 p = t.screenPosition;
                    switch (t.phase)
                    {
                        case UnityEngine.InputSystem.TouchPhase.Began:
                            if (IsOverUI != null && IsOverUI(p)) break;
                            Pointers[id] = p;
                            Recognizer.Down(id, p, now);
                            LastInputTime = now;
                            break;
                        case UnityEngine.InputSystem.TouchPhase.Moved:
                        case UnityEngine.InputSystem.TouchPhase.Stationary:
                            if (!Pointers.ContainsKey(id)) break;
                            Pointers[id] = p;
                            Recognizer.Move(id, p, now);
                            LastInputTime = now;
                            break;
                        case UnityEngine.InputSystem.TouchPhase.Ended:
                            if (!Pointers.ContainsKey(id)) break;
                            Pointers.Remove(id);
                            Recognizer.Up(id, p, now);
                            LastInputTime = now;
                            break;
                        case UnityEngine.InputSystem.TouchPhase.Canceled:
                            if (!Pointers.ContainsKey(id)) break;
                            Pointers.Remove(id);
                            Recognizer.Cancel(id, now);
                            break;
                    }
                }
            }
            var mouse = Mouse.current;
            if (mouse != null && !touched)
            {
                Vector2 p = mouse.position.ReadValue();
                if (mouse.leftButton.wasPressedThisFrame && !(IsOverUI != null && IsOverUI(p)))
                {
                    _mouseDown = true;
                    Pointers[MousePointer] = p;
                    Recognizer.Down(MousePointer, p, now);
                    LastInputTime = now;
                }
                else if (_mouseDown && mouse.leftButton.isPressed)
                {
                    Pointers[MousePointer] = p;
                    Recognizer.Move(MousePointer, p, now);
                    LastInputTime = now;
                }
                else if (_mouseDown && !mouse.leftButton.isPressed)
                {
                    _mouseDown = false;
                    Pointers.Remove(MousePointer);
                    Recognizer.Up(MousePointer, p, now);
                    LastInputTime = now;
                }
                // ホイール = ピンチ（PC で試すため）
                float wheel = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) > 0.01f)
                {
                    float scale = wheel > 0f ? 1.5f : 0.6f;
                    Emit(new GestureEvent { type = GestureType.PinchBegin, pos = p, pos2 = p, magnitude = scale, fingers = 2, time = now });
                    Emit(new GestureEvent { type = GestureType.Pinch, pos = p, pos2 = p, magnitude = scale, fingers = 2, time = now });
                    Emit(new GestureEvent { type = GestureType.PinchEnd, pos = p, pos2 = p, magnitude = scale, fingers = 2, time = now });
                    LastInputTime = now;
                }
            }
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame) VirtualShake(1f);
                Vector2 tilt = Vector2.zero;
                if (kb.leftArrowKey.isPressed) tilt.x -= 1f;
                if (kb.rightArrowKey.isPressed) tilt.x += 1f;
                if (kb.upArrowKey.isPressed) tilt.y += 1f;
                if (kb.downArrowKey.isPressed) tilt.y -= 1f;
                if (tilt != Vector2.zero) VirtualTilt(tilt.normalized * 0.7f);
                else if (_tilting && _keyboardTilt) VirtualTilt(Vector2.zero);
            }
            UpdateSensors(now);
            Recognizer.Tick(now);
            Recognizer.Drain(_events);
            if (_events.Count > 0)
            {
                foreach (var e in _events) Gesture?.Invoke(e);
                _events.Clear();
            }
        }

        bool _keyboardTilt;

        void UpdateSensors(float now)
        {
            if (!sensorsEnabled) return;
            var acc = Accelerometer.current;
            if (acc == null) return;
            Vector3 a = acc.acceleration.ReadValue();
            if (a.sqrMagnitude < 1e-4f) return;
            _accelSmooth = Vector3.Lerp(_accelSmooth, a, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 6f));
            float jerk = (a - _accelSmooth).magnitude;
            if (jerk > shakeThreshold && now - _lastShake > 0.7f)
            {
                _lastShake = now;
                Emit(new GestureEvent { type = GestureType.Shake, magnitude = Mathf.Clamp01(jerk / 4f), time = now });
                LastInputTime = now;
            }
            // かたむき：重力の向きの、画面に沿った成分（右・上が正）。まっすぐ持つと (0, -1) なので、そこを 0 とする
            Vector2 g = ScreenGravity(_accelSmooth, Screen.orientation);
            Vector2 tilt = new Vector2(g.x, g.y + 1f);
            if (now >= _nextTilt)
            {
                _nextTilt = now + 0.1f;
                if (tilt.magnitude > tiltThreshold)
                {
                    _keyboardTilt = false;
                    Emit(new GestureEvent { type = _tilting ? GestureType.Tilt : GestureType.TiltBegin, dir = tilt.normalized, magnitude = Mathf.Clamp01(tilt.magnitude), time = now });
                    _tilting = true;
                }
                else if (_tilting && !_keyboardTilt)
                {
                    _tilting = false;
                    Emit(new GestureEvent { type = GestureType.TiltEnd, time = now });
                }
            }
        }

        /// <summary>
        /// 加速度センサーの値（端末に固定の軸）を、画面の右・上を正とする重力の向きにする。
        /// たてにまっすぐ持つと (0, -1)、右にかたむけると x が正、さかさまにすると (0, +1)。
        /// </summary>
        public static Vector2 ScreenGravity(Vector3 a, ScreenOrientation o)
        {
            switch (o)
            {
                case ScreenOrientation.LandscapeLeft: return new Vector2(-a.y, a.x);
                case ScreenOrientation.LandscapeRight: return new Vector2(a.y, -a.x);
                case ScreenOrientation.PortraitUpsideDown: return new Vector2(-a.x, -a.y);
                default: return new Vector2(a.x, a.y);
            }
        }

        void Emit(GestureEvent e) => _events.Add(e);

        // ------------------------------------------------------------------
        // テスト・PC 用の仮想入力
        public void VirtualDown(int id, Vector2 pos)
        {
            Pointers[id] = pos;
            Recognizer.Down(id, pos, Time.time);
            LastInputTime = Time.time;
        }

        public void VirtualMove(int id, Vector2 pos)
        {
            Pointers[id] = pos;
            Recognizer.Move(id, pos, Time.time);
            LastInputTime = Time.time;
        }

        public void VirtualUp(int id, Vector2 pos)
        {
            Pointers.Remove(id);
            Recognizer.Up(id, pos, Time.time);
            LastInputTime = Time.time;
        }

        public void VirtualShake(float strength = 1f)
        {
            Emit(new GestureEvent { type = GestureType.Shake, magnitude = strength, time = Time.time });
            LastInputTime = Time.time;
        }

        public void VirtualTilt(Vector2 tilt)
        {
            if (tilt.magnitude > 0.05f)
            {
                _keyboardTilt = true;
                Emit(new GestureEvent { type = _tilting ? GestureType.Tilt : GestureType.TiltBegin, dir = tilt.normalized, magnitude = Mathf.Clamp01(tilt.magnitude), time = Time.time });
                _tilting = true;
            }
            else if (_tilting)
            {
                _tilting = false;
                _keyboardTilt = false;
                Emit(new GestureEvent { type = GestureType.TiltEnd, time = Time.time });
            }
            LastInputTime = Time.time;
        }

        /// <summary>入力があった扱いにする（UI のボタンをおした、ページにもどった、など）。</summary>
        public void NoteInput() => LastInputTime = Time.time;

        /// <summary>直接イベントを流す（テスト用）。</summary>
        public void Inject(GestureEvent e)
        {
            e.time = Time.time;
            Gesture?.Invoke(e);
            LastInputTime = Time.time;
        }
    }
}
