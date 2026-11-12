using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gyorome
{
    [Serializable]
    public class SettingsData
    {
        public bool sound = true;
        public bool haptics = true;
        public bool names = true;      // 反応の名前（オノマトペ）を出す
        public bool hints = true;      // しばらくさわらないとヒントを出す
        public int irisTheme = 0;
        public int lidTheme = 0;
    }

    [Serializable]
    public class ProgressData
    {
        public List<string> discovered = new List<string>();
        public List<string> countIds = new List<string>();
        public List<int> counts = new List<int>();
        public int launches;
        public string lastLaunchDay = "";
        public int totalReactions;
    }

    /// <summary>PlayerPrefs に JSON で保存する（見つけた反応・設定）。</summary>
    public static class SaveSystem
    {
        public const string SettingsKey = "gyorome.settings";
        public const string ProgressKey = "gyorome.progress";

        static SettingsData _settings;
        static ProgressData _progress;
        static readonly HashSet<string> _discovered = new HashSet<string>();
        static readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

        /// <summary>テスト用：PlayerPrefs を使わず、メモリだけにする。</summary>
        public static bool MemoryOnly;

        public static SettingsData Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = null;
                    if (!MemoryOnly && PlayerPrefs.HasKey(SettingsKey))
                    {
                        try { _settings = JsonUtility.FromJson<SettingsData>(PlayerPrefs.GetString(SettingsKey)); }
                        catch (Exception) { _settings = null; }
                    }
                    _settings ??= new SettingsData();
                }
                return _settings;
            }
        }

        public static ProgressData Progress
        {
            get
            {
                if (_progress == null)
                {
                    if (!MemoryOnly && PlayerPrefs.HasKey(ProgressKey))
                    {
                        try { _progress = JsonUtility.FromJson<ProgressData>(PlayerPrefs.GetString(ProgressKey)); }
                        catch (Exception) { _progress = null; }
                    }
                    _progress ??= new ProgressData();
                    _discovered.Clear();
                    foreach (var id in _progress.discovered) _discovered.Add(id);
                    _counts.Clear();
                    for (int i = 0; i < _progress.countIds.Count && i < _progress.counts.Count; i++) _counts[_progress.countIds[i]] = _progress.counts[i];
                }
                return _progress;
            }
        }

        public static int DiscoveredCount { get { var _ = Progress; return _discovered.Count; } }
        public static bool IsDiscovered(string id) { var _ = Progress; return _discovered.Contains(id); }
        public static int CountOf(string id) { var _ = Progress; return _counts.TryGetValue(id, out var c) ? c : 0; }

        /// <summary>反応を見つけた・また見た。はじめてなら true。</summary>
        public static bool Record(string id)
        {
            var p = Progress;
            bool isNew = _discovered.Add(id);
            if (isNew) p.discovered.Add(id);
            _counts[id] = CountOf(id) + 1;
            p.totalReactions++;
            SaveProgress();
            return isNew;
        }

        /// <summary>今日はじめての起動か（起動回数も数える）。</summary>
        public static bool RegisterLaunch(DateTime now)
        {
            var p = Progress;
            string day = now.ToString("yyyy-MM-dd");
            bool first = p.lastLaunchDay != day;
            p.lastLaunchDay = day;
            p.launches++;
            SaveProgress();
            return first;
        }

        public static void SaveSettings()
        {
            if (MemoryOnly) return;
            PlayerPrefs.SetString(SettingsKey, JsonUtility.ToJson(Settings));
            PlayerPrefs.Save();
        }

        public static void SaveProgress()
        {
            var p = Progress;
            p.countIds.Clear();
            p.counts.Clear();
            foreach (var kv in _counts)
            {
                p.countIds.Add(kv.Key);
                p.counts.Add(kv.Value);
            }
            if (MemoryOnly) return;
            PlayerPrefs.SetString(ProgressKey, JsonUtility.ToJson(p));
            PlayerPrefs.Save();
        }

        public static void ResetProgress()
        {
            _progress = new ProgressData();
            _discovered.Clear();
            _counts.Clear();
            if (!MemoryOnly)
            {
                PlayerPrefs.DeleteKey(ProgressKey);
                PlayerPrefs.Save();
            }
        }

        public static void ResetAll()
        {
            ResetProgress();
            _settings = new SettingsData();
            if (!MemoryOnly)
            {
                PlayerPrefs.DeleteKey(SettingsKey);
                PlayerPrefs.Save();
            }
        }

        /// <summary>メモリの内容を捨てて、次に読むときに保存から読み直す（テスト用）。</summary>
        public static void Reload()
        {
            _settings = null;
            _progress = null;
        }
    }
}
