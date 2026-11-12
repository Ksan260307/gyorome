using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gyorome
{
    /// <summary>
    /// 画面の上にのるもの（UI Toolkit）：はじめのタイトル・記号の層・せってい（音・ふるえ・まぶたの色）。
    /// 目へのタッチは GyoInput が直接読むので、ここはボタンと窓の上かどうか（IsPointerOverUI）だけ答える。
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public Font bodyFont;
        public Font titleFont;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern float GyoSafeInset(int side);
#else
        static float GyoSafeInset(int side) => 0f;
#endif

        GameManager _gm;
        UIDocument _doc;
        VisualElement _root, _hud, _hudTop, _settings, _emoteLayer, _lidSwatches;
        Label _title, _version;
        Button _settingsButton, _settingsClose;
        Toggle _toggleSound, _toggleHaptics;
        readonly List<Button> _lidButtons = new List<Button>();
        float _titleUntil;

        public bool SettingsOpen => _settings != null && !_settings.ClassListContains("hidden");
        public VisualElement Root => _root;

        void OnEnable()
        {
            _doc = GetComponent<UIDocument>();
            _root = _doc != null ? _doc.rootVisualElement : null;
            if (_root == null) return;
            _root = _root.Q("root") ?? _root;
            if (bodyFont != null) _root.style.unityFontDefinition = FontDefinition.FromFont(bodyFont);
            _hud = _root.Q("hud");
            _hudTop = _root.Q("hud-top");
            _settings = _root.Q("settings");
            _emoteLayer = _root.Q("emotes");
            _title = _root.Q<Label>("title");
            _version = _root.Q<Label>("version");
            _settingsButton = _root.Q<Button>("settings-button");
            _settingsClose = _root.Q<Button>("settings-close");
            _toggleSound = _root.Q<Toggle>("toggle-sound");
            _toggleHaptics = _root.Q<Toggle>("toggle-haptics");
            _lidSwatches = _root.Q("lid-swatches");
            if (_title != null && titleFont != null) _title.style.unityFontDefinition = FontDefinition.FromFont(titleFont);

            _settingsButton?.RegisterCallback<ClickEvent>(_ => { Touch(); OpenSettings(); });
            _settingsClose?.RegisterCallback<ClickEvent>(_ => { Touch(); CloseSettings(); });
            // 窓の外をタップしたら閉じる
            _settings?.RegisterCallback<ClickEvent>(e => { if (e.target == _settings) CloseSettings(); });

            var s = SaveSystem.Settings;
            Bind(_toggleSound, s.sound, v => { s.sound = v; SaveSystem.SaveSettings(); });
            Bind(_toggleHaptics, s.haptics, v => { s.haptics = v; SaveSystem.SaveSettings(); });
            if (_version != null) _version.text = "ver " + Application.version;

            _titleUntil = Time.time + 2.4f;
        }

        static void Bind(Toggle t, bool value, System.Action<bool> onChange)
        {
            if (t == null) return;
            t.SetValueWithoutNotify(value);
            t.RegisterValueChangedCallback(e => onChange(e.newValue));
        }

        /// <summary>GameManager が起動時に呼ぶ。</summary>
        public void Bind(GameManager gm)
        {
            _gm = gm;
            if (gm.emotes != null) gm.emotes.layer = _emoteLayer;
            if (gm.input != null) gm.input.IsOverUI = IsPointerOverUI;
            if (bodyFont == null && gm.assets != null) bodyFont = gm.assets.bodyFont;
            if (titleFont == null && gm.assets != null) titleFont = gm.assets.titleFont;
            if (_root != null && bodyFont != null) _root.style.unityFontDefinition = FontDefinition.FromFont(bodyFont);
            if (_title != null && titleFont != null) _title.style.unityFontDefinition = FontDefinition.FromFont(titleFont);
            BuildSwatches();
        }

        void Touch() => _gm?.input?.NoteInput();

        // ------------------------------------------------------------------
        public Vector2 ScreenToPanel(Vector2 screen)
        {
            if (_root == null || _root.panel == null) return screen;
            var vt = _root.panel.visualTree;
            float pw = vt.layout.width, ph = vt.layout.height;
            if (float.IsNaN(pw) || pw <= 0f) pw = Screen.width;
            if (float.IsNaN(ph) || ph <= 0f) ph = Screen.height;
            return new Vector2(screen.x / Mathf.Max(1f, Screen.width) * pw, (1f - screen.y / Mathf.Max(1f, Screen.height)) * ph);
        }

        /// <summary>画面の点（左下原点）が、ボタンや窓の上か。</summary>
        public bool IsPointerOverUI(Vector2 screen)
        {
            if (_root == null || _root.panel == null) return false;
            if (SettingsOpen) return true;
            var picked = _root.panel.Pick(ScreenToPanel(screen));
            return picked != null && picked != _root && picked != _root.panel.visualTree;
        }

        /// <summary>画面の大きさ・向きが変わったとき（安全な余白を入れ直す）。</summary>
        public void OnLayout()
        {
            if (_hud == null) return;
            Rect sa = Screen.safeArea;
            float w = Mathf.Max(1f, Screen.width), h = Mathf.Max(1f, Screen.height);
            float top = Mathf.Max((h - sa.yMax) / h, GyoSafeInset(1));
            float right = Mathf.Max((w - sa.xMax) / w, GyoSafeInset(2));
            var vt = _root.panel != null ? _root.panel.visualTree : null;
            float pw = vt != null && vt.layout.width > 0f ? vt.layout.width : w;
            float ph = vt != null && vt.layout.height > 0f ? vt.layout.height : h;
            if (_hudTop != null)
            {
                _hudTop.style.top = 24f + top * ph;
                _hudTop.style.right = 24f + right * pw;
            }
        }

        // ------------------------------------------------------------------
        public void OpenSettings()
        {
            if (_settings == null) return;
            RefreshSwatches();
            _settings.RemoveFromClassList("hidden");
        }

        public void CloseSettings() => _settings?.AddToClassList("hidden");

        void BuildSwatches()
        {
            if (_lidSwatches == null) return;
            _lidSwatches.Clear();
            _lidButtons.Clear();
            for (int i = 0; i < Themes.Lid.Length; i++)
            {
                int idx = i;
                var b = new Button(() => { Touch(); _gm?.SetLidTheme(idx); RefreshSwatches(); _gm?.audioManager?.Play("pop", 1f, 0.5f); });
                b.AddToClassList("swatch");
                b.style.backgroundColor = Themes.Lid[i].lid;
                b.tooltip = Themes.Lid[i].name;
                _lidSwatches.Add(b);
                _lidButtons.Add(b);
            }
            RefreshSwatches();
        }

        void RefreshSwatches()
        {
            var s = SaveSystem.Settings;
            for (int i = 0; i < _lidButtons.Count; i++) _lidButtons[i].EnableInClassList("swatch--selected", i == s.lidTheme);
        }

        // ------------------------------------------------------------------
        void Update()
        {
            if (_title != null && _titleUntil > 0f && Time.time >= _titleUntil)
            {
                _titleUntil = -1f;
                _title.AddToClassList("title--hidden");
            }
        }
    }
}
