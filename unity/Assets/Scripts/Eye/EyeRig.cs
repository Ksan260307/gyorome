using UnityEngine;

namespace Gyorome
{
    /// <summary>
    /// 目玉ひとつ。白目（回る）・虹彩（白目の子）・上下のまぶた（回らない）を持ち、
    /// 反応や idle の「目標の値」に、ばねでなめらかに追いつく。
    ///   root（この Transform）: 画面の置き場所にあり、カメラの方を向く。localScale = 半径 × つぶれ
    ///   ball: 視線（yaw / pitch）と回転（roll）。iris はその子
    ///   lidUpper / lidLower: ひらき具合で X 軸まわりに回り、かたむき（怒り・悲しみ）で Z 軸まわりに回る
    /// yaw は画面の右向きが正、pitch は上向きが正（度）。root のローカル X は画面の左なので、回すときに符号を反転する。
    /// </summary>
    public class EyeRig : MonoBehaviour
    {
        public bool isLeft;
        public Transform ball;
        public Transform irisT;
        public Transform lidUpper;
        public Transform lidLower;
        public MeshRenderer ballRenderer;
        public MeshRenderer irisRenderer;
        public MeshRenderer lidUpperRenderer;
        public MeshRenderer lidLowerRenderer;

        /// <summary>半径（ワールド単位 = 画面のピクセル）。レイアウトが入れる。</summary>
        public float Radius = 100f;
        /// <summary>置き場所（親からの位置）。ゆれやずれは、ここに足す。</summary>
        public Vector3 BasePosition;

        // ---- 目標の値（反応・idle が入れる） ----
        public Vector2 GazeTarget;
        public float GazeSmooth = 0.09f;
        public float UpperTarget = 1f;
        public float LowerTarget = 1f;
        public float LidSmooth = 0.06f;
        public float TiltTarget;
        public float TiltSmooth = 0.12f;
        public float PupilTarget = GyoConst.PupilNormal;
        public float PupilSmooth = 0.12f;
        public PupilShape Shape = PupilShape.Round;
        public float Spin;
        public float Sparkle;
        public Vector3 ScaleTarget = Vector3.one;
        public float ScaleSmooth = 0.08f;
        public Vector2 OffsetTarget;
        public float OffsetSmooth = 0.1f;
        public float RollTarget;
        public float RollSmooth = 0.15f;
        public Color TintTarget = Color.white;
        /// <summary>黒目の色（ふだんは黒。反応で金色や虹色になる）。</summary>
        public Color IrisTarget = new Color(0.08f, 0.07f, 0.09f);
        public Color IrisInnerTarget = new Color(0.08f, 0.07f, 0.09f);
        public float ColorSmooth = 0.25f;
        public Color LidColor = new Color(1f, 0.84f, 0.44f);
        public Mood Mood = Mood.Normal;
        public float MoodUntil;

        // ---- 今の値 ----
        Spring _yaw = new Spring(0f, 0.09f);
        Spring _pitch = new Spring(0f, 0.09f);
        Spring _upper = new Spring(1f, 0.06f);
        Spring _lower = new Spring(1f, 0.06f);
        Spring _tilt = new Spring(0f, 0.12f);
        Spring _pupil = new Spring(GyoConst.PupilNormal, 0.12f);
        Spring _roll = new Spring(0f, 0.15f);
        Vector3 _scale = Vector3.one;
        Vector3 _scaleVel;
        Vector2 _offset;
        Vector2 _offsetVel;
        Jiggle _jiggle = new Jiggle(260f, 9f);
        Color _tint = Color.white;
        Color _iris;
        Color _irisInner;
        float _shakeAmp;
        float _shakeEnd;
        float _shakeFreq = 28f;
        Vector2 _shakeOff;
        float _blinkT = -1f;
        float _blinkDur = 0.18f;
        float _blinkLowerAmount = 0.6f;
        MaterialPropertyBlock _mpb;
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int PupilSizeId = Shader.PropertyToID("_PupilSize");
        static readonly int PupilShapeId = Shader.PropertyToID("_PupilShape");
        static readonly int SpinId = Shader.PropertyToID("_Spin");
        static readonly int IrisColorId = Shader.PropertyToID("_IrisColor");
        static readonly int IrisInnerId = Shader.PropertyToID("_IrisInner");
        static readonly int ScleraColorId = Shader.PropertyToID("_ScleraColor");
        static readonly int SparkleId = Shader.PropertyToID("_Sparkle");

        public Vector2 Gaze => new Vector2(_yaw.value, _pitch.value);
        public float UpperOpen => _upper.value;
        public float LowerOpen => _lower.value;
        /// <summary>まばたきもふくめた、上まぶたのひらき具合（0 = 閉じた、1 = ひらいた）。</summary>
        public float Openness { get; private set; } = 1f;
        public float Pupil => _pupil.value;
        public float Tilt => _tilt.value;
        public float Roll => _roll.value;
        public Vector3 Scale => _scale;
        public Vector2 Offset => _offset + _jiggle.offset + _shakeOff;
        public bool Blinking => _blinkT >= 0f;
        public int Blinks { get; private set; }
        public Color Tint => _tint;
        public Vector3 WorldCenter => ball != null ? ball.position : transform.position;
        /// <summary>目の中心が見ている向き（ワールド）。</summary>
        public Vector3 GazeDirection => ball != null ? ball.forward : transform.forward;

        void Awake()
        {
            _mpb = new MaterialPropertyBlock();
            _iris = IrisTarget;
            _irisInner = IrisInnerTarget;
        }

        /// <summary>部品をそろえた目を作る。</summary>
        public static EyeRig Build(string name, EyeAssets assets, bool isLeft, Transform parent)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var rig = root.AddComponent<EyeRig>();
            rig.isLeft = isLeft;

            var ball = new GameObject("Ball");
            ball.transform.SetParent(root.transform, false);
            var bmf = ball.AddComponent<MeshFilter>();
            bmf.sharedMesh = assets != null ? assets.eyeball : null;
            var bmr = ball.AddComponent<MeshRenderer>();
            bmr.sharedMaterial = assets != null ? assets.sclera : null;
            Quiet(bmr);
            rig.ball = ball.transform;
            rig.ballRenderer = bmr;

            var iris = new GameObject("Iris");
            iris.transform.SetParent(ball.transform, false);
            var imf = iris.AddComponent<MeshFilter>();
            imf.sharedMesh = assets != null ? assets.iris : null;
            var imr = iris.AddComponent<MeshRenderer>();
            imr.sharedMaterial = assets != null ? assets.irisMaterial : null;
            Quiet(imr);
            rig.irisT = iris.transform;
            rig.irisRenderer = imr;

            rig.lidUpper = Lid("LidUpper", assets != null ? assets.lidUpper : null, assets, root.transform, out rig.lidUpperRenderer);
            rig.lidLower = Lid("LidLower", assets != null ? assets.lidLower : null, assets, root.transform, out rig.lidLowerRenderer);
            return rig;
        }

        static Transform Lid(string name, Mesh mesh, EyeAssets assets, Transform parent, out MeshRenderer mr)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = assets != null ? assets.lid : null;
            Quiet(mr);
            return go.transform;
        }

        static void Quiet(MeshRenderer mr)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            mr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        // ------------------------------------------------------------------
        // 操作
        public void Look(Vector2 yawPitch)
        {
            GazeTarget = new Vector2(Mathf.Clamp(yawPitch.x, -GyoConst.MaxYawDeg, GyoConst.MaxYawDeg), Mathf.Clamp(yawPitch.y, -GyoConst.MaxPitchDeg, GyoConst.MaxPitchDeg));
        }

        /// <summary>-1..1 の向き（画面の右・上が正）で見る。1 で最大の角度。</summary>
        public void LookDir(Vector2 dir01) => Look(new Vector2(dir01.x * GyoConst.MaxYawDeg, dir01.y * GyoConst.MaxPitchDeg));

        public void LookForward() => GazeTarget = Vector2.zero;

        /// <summary>ワールドの点を見る（目の中心から）。</summary>
        public void LookAt(Vector3 world)
        {
            Look(YawPitchTo(world));
        }

        /// <summary>ワールドの点を見るときの yaw / pitch（度）。</summary>
        public Vector2 YawPitchTo(Vector3 world)
        {
            Vector3 local = transform.InverseTransformDirection(world - WorldCenter);
            if (local.sqrMagnitude < 1e-6f) return Vector2.zero;
            local.Normalize();
            float yaw = -Mathf.Atan2(local.x, Mathf.Max(0.05f, local.z)) * Mathf.Rad2Deg;   // ローカル +X は画面の左
            float pitch = Mathf.Atan2(local.y, Mathf.Sqrt(local.x * local.x + local.z * local.z)) * Mathf.Rad2Deg;
            return new Vector2(yaw, pitch);
        }

        public void Lids(float upperOpen, float lowerOpen)
        {
            UpperTarget = Mathf.Clamp01(upperOpen);
            LowerTarget = Mathf.Clamp01(lowerOpen);
        }

        public void Blink(float duration = 0.18f, float lowerAmount = 0.6f)
        {
            if (_blinkT >= 0f) return;
            _blinkT = 0f;
            _blinkDur = Mathf.Max(0.05f, duration);
            _blinkLowerAmount = lowerAmount;
            Blinks++;
        }

        public void Kick(Vector2 impulse) => _jiggle.Kick(impulse);

        public void Shake(float amplitude, float duration, float frequency = 28f)
        {
            _shakeAmp = amplitude;
            _shakeEnd = Time.time + duration;
            _shakeFreq = frequency;
        }

        /// <summary>今の気分にもどす（反応のあとに呼ぶ）。</summary>
        public void ResetToNeutral()
        {
            GazeSmooth = 0.09f;
            LidSmooth = 0.06f;
            ScaleSmooth = 0.08f;
            OffsetSmooth = 0.1f;
            PupilSmooth = 0.12f;
            ScaleTarget = Vector3.one;
            OffsetTarget = Vector2.zero;
            RollTarget = 0f;
            TintTarget = Color.white;
            Sparkle = 0f;
            Shape = PupilShape.Round;
            TiltTarget = 0f;
            UpperTarget = 1f;
            LowerTarget = 1f;
            PupilTarget = GyoConst.PupilNormal;
            ApplyMoodBaseline();
        }

        public void ApplyMoodBaseline()
        {
            switch (Mood)
            {
                case Mood.Happy:
                    LowerTarget = 0.55f; UpperTarget = 0.95f; PupilTarget = 0.46f; break;
                case Mood.Angry:
                    UpperTarget = 0.72f; TiltTarget = 18f; PupilTarget = 0.3f; TintTarget = new Color(1f, 0.9f, 0.88f); break;
                case Mood.Sad:
                    UpperTarget = 0.8f; TiltTarget = -16f; GazeTarget = new Vector2(0f, -12f); break;
                case Mood.Sleepy:
                    UpperTarget = 0.5f; LowerTarget = 0.85f; GazeTarget = new Vector2(0f, -8f); break;
                case Mood.Asleep:
                    UpperTarget = 0f; LowerTarget = 0.6f; break;
                case Mood.Dizzy:
                    Shape = PupilShape.Spiral; UpperTarget = 0.8f; break;
                case Mood.Scared:
                    PupilTarget = GyoConst.PupilSmall; ScaleTarget = new Vector3(0.96f, 0.96f, 0.96f); break;
                case Mood.Love:
                    Shape = PupilShape.Heart; PupilTarget = 0.6f; LowerTarget = 0.7f; break;
                case Mood.Bored:
                    UpperTarget = 0.6f; GazeTarget = new Vector2(0f, 10f); break;
            }
        }

        public void SetMood(Mood mood, float seconds)
        {
            Mood = mood;
            MoodUntil = Time.time + seconds;
        }

        public void ApplyTheme(Themes.IrisTheme iris, Themes.LidTheme lid, bool instant = false)
        {
            IrisTarget = iris.outer;
            IrisInnerTarget = iris.inner;
            LidColor = lid.lid;
            if (instant)
            {
                _iris = iris.outer;
                _irisInner = iris.inner;
            }
        }

        /// <summary>すべての値を目標に合わせる（起動時・テスト用）。</summary>
        public void Snap()
        {
            _yaw.Snap(GazeTarget.x);
            _pitch.Snap(GazeTarget.y);
            _upper.Snap(UpperTarget);
            _lower.Snap(LowerTarget);
            _tilt.Snap(TiltTarget);
            _pupil.Snap(PupilTarget);
            _roll.Snap(RollTarget);
            _scale = ScaleTarget;
            _scaleVel = Vector3.zero;
            _offset = OffsetTarget;
            _offsetVel = Vector2.zero;
            _jiggle = new Jiggle(260f, 9f);
            _tint = TintTarget;
            _iris = IrisTarget;
            _irisInner = IrisInnerTarget;
            _blinkT = -1f;
            Apply();
        }

        // ------------------------------------------------------------------
        void LateUpdate()
        {
            Step(Time.deltaTime);
            Apply();
        }

        /// <summary>時間を進める（テストからも呼べる）。</summary>
        public void Step(float dt)
        {
            if (dt <= 0f) return;
            _yaw.target = GazeTarget.x; _yaw.smoothTime = GazeSmooth; _yaw.Step(dt);
            _pitch.target = GazeTarget.y; _pitch.smoothTime = GazeSmooth; _pitch.Step(dt);
            _upper.target = UpperTarget; _upper.smoothTime = LidSmooth; _upper.Step(dt);
            _lower.target = LowerTarget; _lower.smoothTime = LidSmooth; _lower.Step(dt);
            _tilt.target = TiltTarget; _tilt.smoothTime = TiltSmooth; _tilt.Step(dt);
            _pupil.target = PupilTarget; _pupil.smoothTime = PupilSmooth; _pupil.Step(dt);
            _roll.target = RollTarget; _roll.smoothTime = RollSmooth; _roll.Step(dt);
            _scale = Vector3.SmoothDamp(_scale, ScaleTarget, ref _scaleVel, Mathf.Max(0.0001f, ScaleSmooth), float.PositiveInfinity, dt);
            _offset = Vector2.SmoothDamp(_offset, OffsetTarget, ref _offsetVel, Mathf.Max(0.0001f, OffsetSmooth), float.PositiveInfinity, dt);
            _jiggle.Step(dt);
            float k = 1f - Mathf.Exp(-dt / Mathf.Max(0.01f, ColorSmooth));
            _tint = Color.Lerp(_tint, TintTarget, k);
            _iris = Color.Lerp(_iris, IrisTarget, k);
            _irisInner = Color.Lerp(_irisInner, IrisInnerTarget, k);
            if (Time.time < _shakeEnd)
            {
                float t = Time.time * _shakeFreq;
                _shakeOff = new Vector2(Mathf.Sin(t * 6.28f) , Mathf.Cos(t * 5.1f)) * _shakeAmp;
            }
            else _shakeOff = Vector2.zero;

            if (_blinkT >= 0f)
            {
                _blinkT += dt;
                if (_blinkT >= _blinkDur) _blinkT = -1f;
            }
            if (MoodUntil > 0f && Time.time >= MoodUntil && Mood != Mood.Normal && Mood != Mood.Asleep)
            {
                Mood = Mood.Normal;
                MoodUntil = 0f;
                ResetToNeutral();
            }
        }

        /// <summary>まばたきの閉じ具合（0..1）。</summary>
        public float BlinkClosure
        {
            get
            {
                if (_blinkT < 0f) return 0f;
                float p = Mathf.Clamp01(_blinkT / _blinkDur);
                if (p < 0.35f) return Ease.InQuad(p / 0.35f);
                if (p < 0.45f) return 1f;
                return 1f - Ease.OutQuad((p - 0.45f) / 0.55f);
            }
        }

        public void Apply()
        {
            float c = BlinkClosure;
            float uOpen = _upper.value * (1f - c);
            float lOpen = Mathf.Lerp(_lower.value, _lower.value * (1f - _blinkLowerAmount), c);
            Openness = uOpen;
            float sign = isLeft ? 1f : -1f;
            if (lidUpper != null) lidUpper.localRotation = Quaternion.Euler(GyoConst.UpperTravelDeg * (1f - uOpen), 0f, sign * _tilt.value);
            if (lidLower != null) lidLower.localRotation = Quaternion.Euler(-GyoConst.LowerTravelDeg * (1f - lOpen), 0f, sign * _tilt.value * 0.5f);
            if (ball != null) ball.localRotation = Quaternion.Euler(-_pitch.value, -_yaw.value, 0f) * Quaternion.AngleAxis(_roll.value, Vector3.forward);
            transform.localPosition = BasePosition + (Vector3)(Offset * Radius);
            transform.localScale = new Vector3(_scale.x, _scale.y, _scale.z) * Radius;

            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            if (ballRenderer != null)
            {
                _mpb.Clear();
                _mpb.SetColor(ColorId, _tint);
                ballRenderer.SetPropertyBlock(_mpb);
            }
            if (irisRenderer != null)
            {
                _mpb.Clear();
                _mpb.SetFloat(PupilSizeId, Mathf.Clamp(_pupil.value * GyoConst.PupilToShader, 0.05f, 1f));
                _mpb.SetColor(ScleraColorId, _tint);
                _mpb.SetFloat(PupilShapeId, (float)Shape);
                _mpb.SetFloat(SpinId, Spin);
                _mpb.SetColor(IrisColorId, _iris);
                _mpb.SetColor(IrisInnerId, _irisInner);
                _mpb.SetFloat(SparkleId, Sparkle);
                irisRenderer.SetPropertyBlock(_mpb);
            }
            if (lidUpperRenderer != null)
            {
                _mpb.Clear();
                _mpb.SetColor(ColorId, LidColor);
                lidUpperRenderer.SetPropertyBlock(_mpb);
                lidLowerRenderer.SetPropertyBlock(_mpb);
            }
        }
    }
}
