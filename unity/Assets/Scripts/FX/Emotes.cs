using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gyorome
{
    /// <summary>
    /// 目のそばに出る記号（♥ ★ ？ ！ ♪ z …）と、なみだ・あせのしずく。
    /// 記号は UI Toolkit のラベル（GameUI の層に置く）、しずくは小さなメッシュ（Blender の Tear）。
    /// </summary>
    public class Emotes : MonoBehaviour
    {
        public enum Style { Pop, Soft, Big, Angry }

        public EyePair eyes;
        public EyeAssets assets;
        /// <summary>記号を置く UI の層（GameUI が入れる）。null なら記号は数えるだけ。</summary>
        public VisualElement layer;

        class Floating
        {
            public Label label;
            public float born;
            public float life;
            public Vector2 start;
            public float rise;
            public float panelW, panelH;
        }

        class Drop
        {
            public Transform t;
            public Vector3 vel;
            public float born;
            public EyeRig eye;
        }

        readonly List<Floating> _floating = new List<Floating>();
        readonly List<Drop> _drops = new List<Drop>();
        readonly Stack<Transform> _dropPool = new Stack<Transform>();

        public int SpawnedEmotes { get; private set; }
        public int SpawnedDrops { get; private set; }
        public int ActiveEmotes => _floating.Count;
        public int ActiveDrops => _drops.Count;
        public string LastEmote { get; private set; }

        public void Spawn(string text, EyeRig at, Vector2 offsetRadii, Style style = Style.Pop)
        {
            SpawnedEmotes++;
            LastEmote = text;
            if (layer == null || eyes == null || at == null) return;
            Vector2 screen = eyes.ScreenOf(at) + offsetRadii * at.Radius;
            var l = new Label(text);
            l.pickingMode = PickingMode.Ignore;
            l.AddToClassList("emote");
            l.AddToClassList("emote--" + style.ToString().ToLowerInvariant());
            l.style.position = Position.Absolute;
            layer.Add(l);
            var f = new Floating { label = l, born = Time.time, life = style == Style.Soft ? 1.8f : 1.2f, start = screen, rise = style == Style.Big ? 0.5f : 0.35f };
            SetPanelPos(f, screen, at.Radius);
            _floating.Add(f);
        }

        void SetPanelPos(Floating f, Vector2 screen, float radius)
        {
            var panel = layer.panel;
            float pw = layer.resolvedStyle.width, ph = layer.resolvedStyle.height;
            if (float.IsNaN(pw) || pw <= 0f) pw = Screen.width;
            if (float.IsNaN(ph) || ph <= 0f) ph = Screen.height;
            f.panelW = pw;
            f.panelH = ph;
            float x = screen.x / Mathf.Max(1f, Screen.width) * pw;
            float y = (1f - screen.y / Mathf.Max(1f, Screen.height)) * ph;
            f.label.style.left = x - 30f;
            f.label.style.top = y - 30f;
            _ = panel;
        }

        public void Tear(EyeRig eye, int side = 1)
        {
            SpawnedDrops++;
            if (eyes == null || eye == null) return;
            var t = GetDrop();
            if (t == null) return;
            t.SetParent(eye.transform.parent, false);
            // 目の下のふち（ななめ下）から
            Vector3 local = new Vector3(-side * 0.35f, -0.62f, -0.75f) * eye.Radius;
            t.position = eye.transform.position + eye.transform.rotation * local;
            t.localScale = Vector3.one * eye.Radius * 0.1f;
            t.rotation = eye.transform.rotation * Quaternion.Euler(0f, 0f, side * 10f);
            t.gameObject.SetActive(true);
            _drops.Add(new Drop { t = t, vel = new Vector3(0f, -eye.Radius * 0.4f, 0f), born = Time.time, eye = eye });
        }

        public void Sweat(EyeRig eye)
        {
            SpawnedDrops++;
            if (eyes == null || eye == null) return;
            var t = GetDrop();
            if (t == null) return;
            t.SetParent(eye.transform.parent, false);
            int side = eye.isLeft ? -1 : 1;
            Vector3 local = new Vector3(side * 0.95f, 0.75f, -0.4f) * eye.Radius;
            t.position = eye.transform.position + eye.transform.rotation * local;
            t.localScale = Vector3.one * eye.Radius * 0.09f;
            t.rotation = eye.transform.rotation * Quaternion.Euler(0f, 0f, -side * 25f);
            t.gameObject.SetActive(true);
            _drops.Add(new Drop { t = t, vel = new Vector3(side * eye.Radius * 0.35f, eye.Radius * 0.9f, 0f), born = Time.time, eye = eye });
        }

        Transform GetDrop()
        {
            if (_dropPool.Count > 0) return _dropPool.Pop();
            if (assets == null || assets.tear == null) return null;
            var go = new GameObject("Drop");
            go.AddComponent<MeshFilter>().sharedMesh = assets.tear;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = assets.tearMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go.transform;
        }

        void Update()
        {
            float now = Time.time;
            for (int i = _floating.Count - 1; i >= 0; i--)
            {
                var f = _floating[i];
                float t = (now - f.born) / f.life;
                if (t >= 1f)
                {
                    f.label.RemoveFromHierarchy();
                    _floating.RemoveAt(i);
                    continue;
                }
                float y = (1f - f.start.y / Mathf.Max(1f, Screen.height)) * f.panelH - f.rise * f.panelH * 0.12f * Ease.OutQuad(t);
                f.label.style.top = y - 30f;
                f.label.style.opacity = t < 0.15f ? t / 0.15f : 1f - Ease.InQuad((t - 0.15f) / 0.85f);
                float s = 0.7f + 0.3f * Ease.OutBack(Mathf.Min(1f, t * 4f));
                f.label.style.scale = new Scale(new Vector2(s, s));
            }
            for (int i = _drops.Count - 1; i >= 0; i--)
            {
                var d = _drops[i];
                float age = now - d.born;
                float g = (d.eye != null ? d.eye.Radius : 100f) * 2.2f;
                d.vel += new Vector3(0f, -g * Time.deltaTime, 0f);
                d.t.position += d.vel * Time.deltaTime;
                if (age > 1.6f)
                {
                    d.t.gameObject.SetActive(false);
                    _dropPool.Push(d.t);
                    _drops.RemoveAt(i);
                }
            }
        }

        public void Clear()
        {
            foreach (var f in _floating) f.label.RemoveFromHierarchy();
            _floating.Clear();
            foreach (var d in _drops)
            {
                d.t.gameObject.SetActive(false);
                _dropPool.Push(d.t);
            }
            _drops.Clear();
        }
    }
}
