using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gyorome
{
    /// <summary>
    /// 画面の文字と窓（UI Toolkit）：反応の名前のふきだし・見つけた数・はんのう図鑑・せってい・ヒント・タイトル。
    /// 目へのタッチは GyoInput が直接読むので、ここはボタンと窓の上かどうか（IsPointerOverUI）だけ答える。
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public Font bodyFont;
        public Font titleFont;
        public float hintInterval = 14f;
        public float hintDuration = 5f;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern float GyoSafeInset(int side);
#else
        static float GyoSafeInset(int side) => 0f;
#endif

        GameManager _gm;
        UIDocument _doc;
        VisualElement _root, _hud, _hudTop, _book, _settings, _popups, _emoteLayer, _irisSwatches, _lidSwatches;
        Label _counter, _toast, _hint, _title, _bookCount, _version;
        ScrollView _bookList;
        Button _bookButton, _settingsButton, _bookClose, _settingsClose, _reset;
        Toggle _toggleSound, _toggleHaptics, _toggleNames, _toggleHints;
        readonly List<Button> _irisButtons = new List<Button>();
        readonly List<Button> _lidButtons = new List<Button>();

        class Popup
        {
            public Label label;
            public float born;
            public Vector2 start;
            public float panelH;
        }

        readonly List<Popup> _popupList = new List<Popup>();
        float _toastUntil = -1f;
        float _hintUntil = -1f;
        float _nextHint;
        float _titleUntil;
        bool _resetArmed;
        bool _bookDirty = true;

        public bool BookOpen => _book != null && !_book.ClassListContains("hidden");
        public bool SettingsOpen => _settings != null && !_settings.ClassListContains("hidden");
        public int PopupCount { get; private set; }
        public string LastPopup { get; private set; }
        public string LastToast { get; private set; }
        public string CounterText => _counter != null ? _counter.text : "";
        public int BookEntries { get; private set; }
        public int HintsShown { get; private set; }
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
            _book = _root.Q("book");
            _settings = _root.Q("settings");
            _popups = _root.Q("popups");
            _emoteLayer = _root.Q("emotes");
            _counter = _root.Q<Label>("counter");
            _toast = _root.Q<Label>("toast");
            _hint = _root.Q<Label>("hint");
            _title = _root.Q<Label>("title");
            _bookCount = _root.Q<Label>("book-count");
            _version = _root.Q<Label>("version");
            _bookList = _root.Q<ScrollView>("book-list");
            _bookButton = _root.Q<Button>("book-button");
            _settingsButton = _root.Q<Button>("settings-button");
            _bookClose = _root.Q<Button>("book-close");
            _settingsClose = _root.Q<Button>("settings-close");
            _reset = _root.Q<Button>("reset-button");
            _toggleSound = _root.Q<Toggle>("toggle-sound");
            _toggleHaptics = _root.Q<Toggle>("toggle-haptics");
            _toggleNames = _root.Q<Toggle>("toggle-names");
            _toggleHints = _root.Q<Toggle>("toggle-hints");
            _irisSwatches = _root.Q("iris-swatches");
            _lidSwatches = _root.Q("lid-swatches");
            if (_title != null && titleFont != null) _title.style.unityFontDefinition = FontDefinition.FromFont(titleFont);

            _bookButton?.RegisterCallback<ClickEvent>(_ => { Touch(); OpenBook(); });
            _settingsButton?.RegisterCallback<ClickEvent>(_ => { Touch(); OpenSettings(); });
            _bookClose?.RegisterCallback<ClickEvent>(_ => { Touch(); CloseBook(); });
            _settingsClose?.RegisterCallback<ClickEvent>(_ => { Touch(); CloseSettings(); });
            // 窓の外をタップしたら閉じる
            _book?.RegisterCallback<ClickEvent>(e => { if (e.target == _book) CloseBook(); });
            _settings?.RegisterCallback<ClickEvent>(e => { if (e.target == _settings) CloseSettings(); });
            _reset?.RegisterCallback<ClickEvent>(_ => { Touch(); OnReset(); });

            var s = SaveSystem.Settings;
            Bind(_toggleSound, s.sound, v => { s.sound = v; SaveSystem.SaveSettings(); });
            Bind(_toggleHaptics, s.haptics, v => { s.haptics = v; SaveSystem.SaveSettings(); });
            Bind(_toggleNames, s.names, v => { s.names = v; SaveSystem.SaveSettings(); });
            Bind(_toggleHints, s.hints, v => { s.hints = v; SaveSystem.SaveSettings(); });
            if (_version != null) _version.text = "ver " + Application.version;

            _titleUntil = Time.time + 2.4f;
            _nextHint = Time.time + 10f;
            UpdateCounter();
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
            if (gm.director != null)
            {
                gm.director.Played += OnPlayed;
                gm.director.NameShown += OnNameShown;
            }
            if (gm.emotes != null) gm.emotes.layer = _emoteLayer;
            if (gm.input != null) gm.input.IsOverUI = IsPointerOverUI;
            if (bodyFont == null && gm.assets != null) bodyFont = gm.assets.bodyFont;
            if (titleFont == null && gm.assets != null) titleFont = gm.assets.titleFont;
            if (_root != null && bodyFont != null) _root.style.unityFontDefinition = FontDefinition.FromFont(bodyFont);
            if (_title != null && titleFont != null) _title.style.unityFontDefinition = FontDefinition.FromFont(titleFont);
            BuildSwatches();
            UpdateCounter();
        }

        void OnDisable()
        {
            if (_gm != null && _gm.director != null)
            {
                _gm.director.Played -= OnPlayed;
                _gm.director.NameShown -= OnNameShown;
            }
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
            if (BookOpen || SettingsOpen) return true;
            var picked = _root.panel.Pick(ScreenToPanel(screen));
            return picked != null && picked != _root && picked != _root.panel.visualTree;
        }

        /// <summary>画面の大きさ・向きが変わったとき（安全な余白を入れ直す）。</summary>
        public void OnLayout()
        {
            if (_hud == null) return;
            Rect sa = Screen.safeArea;
            float w = Mathf.Max(1f, Screen.width), h = Mathf.Max(1f, Screen.height);
            float left = Mathf.Max(sa.xMin / w, GyoSafeInset(0));
            float top = Mathf.Max((h - sa.yMax) / h, GyoSafeInset(1));
            float right = Mathf.Max((w - sa.xMax) / w, GyoSafeInset(2));
            float bottom = Mathf.Max(sa.yMin / h, GyoSafeInset(3));
            var vt = _root.panel != null ? _root.panel.visualTree : null;
            float pw = vt != null && vt.layout.width > 0f ? vt.layout.width : w;
            float ph = vt != null && vt.layout.height > 0f ? vt.layout.height : h;
            _hud.style.paddingLeft = left * pw;
            _hud.style.paddingRight = right * pw;
            _hud.style.paddingTop = top * ph;
            _hud.style.paddingBottom = bottom * ph;
            if (_hudTop != null)
            {
                _hudTop.style.top = 24f + top * ph;
                _hudTop.style.right = 24f + right * pw;
            }
        }

        // ------------------------------------------------------------------
        void OnPlayed(Reaction r, bool isNew)
        {
            _bookDirty = true;
            UpdateCounter();
            if (isNew)
            {
                int n = SaveSystem.DiscoveredCount;
                string text = n >= ReactionCatalog.Count ? $"ぜんぶ見つけた！ {r.Name}" : (n == GyoConst.ReactionGoal ? $"100 しゅ たっせい！ {r.Name}" : $"あたらしい反応！ {r.Name}");
                ShowToast(text, true, 2.6f);
                if (n == GyoConst.ReactionGoal || n >= ReactionCatalog.Count) _gm?.audioManager?.Play("jackpot", 1f, 0.8f);
                else _gm?.audioManager?.Play("chime", 1.4f, 0.4f);
            }
        }

        void OnNameShown(string text, EyeRig at)
        {
            if (!SaveSystem.Settings.names) return;
            ShowPopup(text, at, !SaveSystem.IsDiscovered(_gm != null && _gm.director != null ? _gm.director.LastId : "") );
        }

        public void ShowPopup(string text, EyeRig at, bool isNew = false)
        {
            PopupCount++;
            LastPopup = text;
            if (_popups == null || _gm == null || at == null) return;
            var l = new Label(text);
            l.pickingMode = PickingMode.Ignore;
            l.AddToClassList("popup");
            if (isNew) l.AddToClassList("popup--new");
            _popups.Add(l);
            Vector2 screen = _gm.eyes.ScreenOf(at) + new Vector2(0f, at.Radius * 1.12f);
            screen.y = Mathf.Min(screen.y, Screen.height - 120f);
            var pp = ScreenToPanel(screen);
            var p = new Popup { label = l, born = Time.time, start = pp };
            l.style.left = pp.x - 250f;
            l.style.width = 500f;
            l.style.top = pp.y - 40f;
            _popupList.Add(p);
        }

        public void ShowToast(string text, bool highlight, float seconds)
        {
            LastToast = text;
            if (_toast == null) return;
            _toast.text = text;
            _toast.RemoveFromClassList("hidden");
            _toast.EnableInClassList("toast--new", highlight);
            _toastUntil = Time.time + seconds;
        }

        /// <summary>次のヒントの時刻を hintInterval 後にする（設定を変えたとき・テスト用）。</summary>
        public void ResetHintTimer() => _nextHint = Time.time + hintInterval;

        public void ShowHint(string text, float seconds)
        {
            HintsShown++;
            if (_hint == null) return;
            _hint.text = text;
            _hint.RemoveFromClassList("hidden");
            _hintUntil = Time.time + seconds;
        }

        void UpdateCounter()
        {
            if (_counter == null) return;
            int n = SaveSystem.DiscoveredCount;
            int total = ReactionCatalog.Count;
            _counter.text = $"{n} / {total}";
            _counter.EnableInClassList("counter--complete", n >= total);
            if (_bookCount != null) _bookCount.text = $"{n} / {total} 見つけた";
        }

        // ------------------------------------------------------------------
        public void OpenBook()
        {
            if (_book == null) return;
            CloseSettings();
            if (_bookDirty) BuildBook();
            _book.RemoveFromClassList("hidden");
            UpdateCounter();
        }

        public void CloseBook() => _book?.AddToClassList("hidden");

        public void OpenSettings()
        {
            if (_settings == null) return;
            CloseBook();
            _resetArmed = false;
            if (_reset != null)
            {
                _reset.text = "図鑑をリセット";
                _reset.RemoveFromClassList("text-button--armed");
            }
            _settings.RemoveFromClassList("hidden");
        }

        public void CloseSettings() => _settings?.AddToClassList("hidden");

        static readonly string[] CategoryOrder = { "タップ", "スワイプ・なでる", "長押し", "2 本指", "ふる・かたむける", "くみあわせ", "ひとりでに" };

        void BuildBook()
        {
            _bookDirty = false;
            if (_bookList == null) return;
            _bookList.Clear();
            BookEntries = 0;
            var cats = new List<string>(CategoryOrder);
            foreach (var r in ReactionCatalog.All) if (!cats.Contains(r.Category)) cats.Add(r.Category);
            foreach (var cat in cats)
            {
                int found = 0, total = 0;
                foreach (var r in ReactionCatalog.All) if (r.Category == cat) { total++; if (SaveSystem.IsDiscovered(r.Id)) found++; }
                if (total == 0) continue;
                var head = new Label($"{cat}　{found} / {total}");
                head.AddToClassList("book-category");
                head.pickingMode = PickingMode.Ignore;
                _bookList.Add(head);
                foreach (var r in ReactionCatalog.All)
                {
                    if (r.Category != cat) continue;
                    bool known = SaveSystem.IsDiscovered(r.Id);
                    var row = new VisualElement();
                    row.AddToClassList("entry");
                    row.EnableInClassList("entry--found", known);
                    row.pickingMode = PickingMode.Ignore;
                    var badge = new VisualElement();
                    badge.AddToClassList("entry-badge");
                    badge.pickingMode = PickingMode.Ignore;
                    row.Add(badge);
                    var texts = new VisualElement();
                    texts.AddToClassList("entry-texts");
                    texts.pickingMode = PickingMode.Ignore;
                    var name = new Label(known ? r.Name : "？？？");
                    name.AddToClassList("entry-name");
                    name.pickingMode = PickingMode.Ignore;
                    var hint = new Label(r.Hint);
                    hint.AddToClassList("entry-hint");
                    hint.pickingMode = PickingMode.Ignore;
                    texts.Add(name);
                    texts.Add(hint);
                    row.Add(texts);
                    if (known)
                    {
                        var count = new Label("×" + SaveSystem.CountOf(r.Id));
                        count.AddToClassList("entry-count");
                        count.pickingMode = PickingMode.Ignore;
                        row.Add(count);
                    }
                    _bookList.Add(row);
                    BookEntries++;
                }
            }
        }

        void BuildSwatches()
        {
            if (_irisSwatches == null || _lidSwatches == null) return;
            _irisSwatches.Clear();
            _lidSwatches.Clear();
            _irisButtons.Clear();
            _lidButtons.Clear();
            for (int i = 0; i < Themes.Iris.Length; i++)
            {
                int idx = i;
                var b = new Button(() => { Touch(); _gm?.SetIrisTheme(idx); RefreshSwatches(); _gm?.audioManager?.Play("pop", 1.2f, 0.5f); });
                b.AddToClassList("swatch");
                b.style.backgroundColor = Themes.Iris[i].outer;
                b.tooltip = Themes.Iris[i].name;
                _irisSwatches.Add(b);
                _irisButtons.Add(b);
            }
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
            for (int i = 0; i < _irisButtons.Count; i++) _irisButtons[i].EnableInClassList("swatch--selected", i == s.irisTheme);
            for (int i = 0; i < _lidButtons.Count; i++) _lidButtons[i].EnableInClassList("swatch--selected", i == s.lidTheme);
        }

        void OnReset()
        {
            if (!_resetArmed)
            {
                _resetArmed = true;
                _reset.text = "ほんとうにリセットする？（もう一度おす）";
                _reset.AddToClassList("text-button--armed");
                return;
            }
            _resetArmed = false;
            SaveSystem.ResetProgress();
            _bookDirty = true;
            _reset.text = "リセットしました";
            _reset.RemoveFromClassList("text-button--armed");
            UpdateCounter();
            ShowToast("図鑑をリセットしました", false, 2f);
        }

        // ------------------------------------------------------------------
        void Update()
        {
            float now = Time.time;
            if (_title != null && _titleUntil > 0f && now >= _titleUntil)
            {
                _titleUntil = -1f;
                _title.AddToClassList("title--hidden");
            }
            if (_toastUntil > 0f && now >= _toastUntil)
            {
                _toastUntil = -1f;
                _toast?.AddToClassList("hidden");
            }
            if (_hintUntil > 0f && now >= _hintUntil)
            {
                _hintUntil = -1f;
                _hint?.AddToClassList("hidden");
            }
            // ふきだしの動き
            for (int i = _popupList.Count - 1; i >= 0; i--)
            {
                var p = _popupList[i];
                float t = (now - p.born) / 1.5f;
                if (t >= 1f)
                {
                    p.label.RemoveFromHierarchy();
                    _popupList.RemoveAt(i);
                    continue;
                }
                float s = Ease.OutBack(Mathf.Min(1f, t * 5f));
                p.label.style.scale = new Scale(new Vector2(s, s));
                p.label.style.top = p.start.y - 40f - 60f * Ease.OutQuad(t);
                p.label.style.opacity = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
            }
            // ヒント
            if (_gm != null && _gm.input != null && SaveSystem.Settings.hints && now >= _nextHint && !BookOpen && !SettingsOpen)
            {
                _nextHint = now + hintInterval;
                float idle = now - _gm.input.LastInputTime;
                if (idle > 6f && SaveSystem.DiscoveredCount < ReactionCatalog.Count)
                {
                    var r = PickHint();
                    if (r != null) ShowHint("ためしに：" + r.Hint, hintDuration);
                }
            }
        }

        Reaction PickHint()
        {
            var list = new List<Reaction>();
            foreach (var r in ReactionCatalog.All)
                if (!SaveSystem.IsDiscovered(r.Id) && !r.IsIdle && r.Priority <= 2) list.Add(r);
            if (list.Count == 0) return null;
            return list[Random.Range(0, list.Count)];
        }
    }
}
