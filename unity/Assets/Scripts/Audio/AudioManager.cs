using System.Collections.Generic;
using UnityEngine;

namespace Gyorome
{
    /// <summary>効果音（名前で鳴らす。音の高さを変えられる）と、小さな環境音のループ。設定で切れる。</summary>
    public class AudioManager : MonoBehaviour
    {
        public EyeAssets assets;
        public int voices = 6;
        public float sfxVolume = 0.9f;
        public float ambienceVolume = 0.35f;

        readonly List<AudioSource> _pool = new List<AudioSource>();
        AudioSource _ambience;
        int _next;
        float _lastPlay = -1f;
        string _lastName;

        public int PlayedCount { get; private set; }
        public string LastPlayed { get; private set; }

        void Awake()
        {
            for (int i = 0; i < voices; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                _pool.Add(s);
            }
            _ambience = gameObject.AddComponent<AudioSource>();
            _ambience.playOnAwake = false;
            _ambience.loop = true;
            _ambience.spatialBlend = 0f;
            _ambience.volume = 0f;
        }

        void Start()
        {
            if (assets != null && assets.ambience != null)
            {
                _ambience.clip = assets.ambience;
                _ambience.Play();
            }
        }

        void Update()
        {
            float target = SaveSystem.Settings.sound ? ambienceVolume : 0f;
            _ambience.volume = Mathf.MoveTowards(_ambience.volume, target, Time.unscaledDeltaTime * 0.3f);
        }

        public bool Play(string name, float pitch = 1f, float volume = 1f)
        {
            PlayedCount++;
            LastPlayed = name;
            if (!SaveSystem.Settings.sound || assets == null) return false;
            var clip = assets.Clip(name);
            if (clip == null) return false;
            // 同じ音がまったく同時に 2 回鳴らないように
            if (_lastName == name && Time.unscaledTime - _lastPlay < 0.03f) return false;
            _lastName = name;
            _lastPlay = Time.unscaledTime;
            var s = _pool[_next];
            _next = (_next + 1) % _pool.Count;
            s.pitch = pitch;
            s.volume = volume * sfxVolume;
            s.clip = clip;
            s.Play();
            return true;
        }

        public void StopAll()
        {
            foreach (var s in _pool) s.Stop();
        }
    }
}
