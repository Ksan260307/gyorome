using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Gyorome
{
    /// <summary>
    /// 起動時にふたつの目を組み立て、画面の大きさに合わせて置き、入力 → 反応 → 目 の流れをつなぐ。
    /// 画面の向きが変わったら置き直す。背景の色はテーマで変わる。
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class GameManager : MonoBehaviour
    {
        public EyeAssets assets;
        public Camera cam;
        public EyePair eyes;
        public GyoInput input;
        public ReactionDirector director;
        public EyeBehaviour behaviour;
        public AudioManager audioManager;
        public Emotes emotes;
        public GameUI ui;
        public MeshRenderer background;

        public static GameManager Instance { get; private set; }
        public bool Ready { get; private set; }
        public bool PageHidden { get; private set; }
        public bool FirstLaunchToday { get; private set; }
        int _w, _h;
        Color _bgTop, _bgBottom;
        MaterialPropertyBlock _bgMpb;
        static readonly int TopId = Shader.PropertyToID("_ColorTop");
        static readonly int BottomId = Shader.PropertyToID("_ColorBottom");

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void GyoRegisterPageEvents();
#else
        static void GyoRegisterPageEvents() { }
#endif

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            if (eyes == null) eyes = GetComponent<EyePair>();
            if (input == null) input = GetComponent<GyoInput>();
            if (director == null) director = GetComponent<ReactionDirector>();
            if (behaviour == null) behaviour = GetComponent<EyeBehaviour>();
            if (audioManager == null) audioManager = GetComponent<AudioManager>();
            if (emotes == null) emotes = GetComponent<Emotes>();
            if (cam == null) cam = Camera.main;
            BuildEyes();
            Wire();
        }

        void Start()
        {
            GyoRegisterPageEvents();
            if (ui != null) ui.Bind(this);   // UI の OnEnable のあと（すべてのオブジェクトの Awake / OnEnable が終わってから）
            FirstLaunchToday = SaveSystem.RegisterLaunch(DateTime.Now);
            ApplyTheme(true);
            Layout(true);
            Ready = true;
            if (FirstLaunchToday) Invoke(nameof(MorningStretch), 0.8f);
        }

        void MorningStretch()
        {
            if (director != null && !director.Busy) director.PlayIdle("idle_morning_stretch");
        }

        void BuildEyes()
        {
            if (eyes.A != null && eyes.B != null) return;
            var stage = new GameObject("Stage").transform;
            stage.SetParent(transform, false);
            eyes.stage = stage;
            eyes.cam = cam;
            eyes.A = EyeRig.Build("EyeA", assets, true, stage);
            eyes.B = EyeRig.Build("EyeB", assets, false, stage);
            if (background == null && assets != null && assets.background != null)
            {
                var bg = new GameObject("Background");
                bg.transform.SetParent(stage, false);
                bg.AddComponent<MeshFilter>().sharedMesh = QuadMesh();
                background = bg.AddComponent<MeshRenderer>();
                background.sharedMaterial = assets.background;
                background.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                background.receiveShadows = false;
            }
        }

        /// <summary>背景用の 1×1 の板（CreatePrimitive は当たり判定を足そうとするので使わない）。</summary>
        static Mesh QuadMesh()
        {
            var m = new Mesh { name = "BackgroundQuad" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) };
            m.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            m.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            m.RecalculateBounds();
            return m;
        }

        void Wire()
        {
            if (director != null)
            {
                director.eyes = eyes;
                director.input = input;
                director.audioManager = audioManager;
                director.emotes = emotes;
                director.behaviour = behaviour;
            }
            if (behaviour != null)
            {
                behaviour.eyes = eyes;
                behaviour.director = director;
                behaviour.input = input;
                behaviour.audioManager = audioManager;
                behaviour.emotes = emotes;
            }
            if (emotes != null)
            {
                emotes.eyes = eyes;
                emotes.assets = assets;
            }
            if (audioManager != null) audioManager.assets = assets;
        }

        void Update()
        {
            if (Screen.width != _w || Screen.height != _h) Layout(false);
        }

        /// <summary>画面の大きさに合わせて目を置く。</summary>
        public void Layout(bool snap)
        {
            _w = Mathf.Max(1, Screen.width);
            _h = Mathf.Max(1, Screen.height);
            eyes.Layout(_w, _h);
            if (background != null)
            {
                float z = eyes.CameraDistance * 1.2f;
                float scale = (z + eyes.CameraDistance) / eyes.CameraDistance * 1.05f;
                background.transform.localPosition = new Vector3(0f, 0f, z);
                background.transform.localScale = new Vector3(_w * scale, _h * scale, 1f);
            }
            if (input != null) input.ApplyScreenScale();
            if (snap) foreach (var e in eyes.Both) e.Snap();
            ui?.OnLayout();
        }

        public void ApplyTheme(bool instant = false)
        {
            var s = SaveSystem.Settings;
            eyes.ApplyTheme(s.irisTheme, s.lidTheme, instant);
            var lt = Themes.LidOf(s.lidTheme);
            _bgTop = lt.backgroundTop;
            _bgBottom = lt.backgroundBottom;
            if (background != null)
            {
                _bgMpb ??= new MaterialPropertyBlock();
                _bgMpb.SetColor(TopId, _bgTop);
                _bgMpb.SetColor(BottomId, _bgBottom);
                background.SetPropertyBlock(_bgMpb);
            }
            if (cam != null) cam.backgroundColor = _bgBottom;
        }

        public void SetLidTheme(int i)
        {
            SaveSystem.Settings.lidTheme = Mathf.Clamp(i, 0, Themes.Lid.Length - 1);
            SaveSystem.SaveSettings();
            ApplyTheme();
        }

        // ブラウザのタブが見えなくなった・もどった（jslib から SendMessage）
        public void OnPageHidden()
        {
            PageHidden = true;
            if (behaviour != null) behaviour.Paused = true;
        }

        public void OnPageVisible()
        {
            PageHidden = false;
            if (behaviour != null) behaviour.Paused = false;
            if (input != null) input.NoteInput();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveSystem.SaveProgress();
            else if (input != null) input.NoteInput();
        }

        void OnApplicationQuit()
        {
            SaveSystem.SaveProgress();
        }
    }
}
