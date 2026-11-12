using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gyorome
{
    /// <summary>セットアップ（Editor/ProjectSetup.cs）が集める、目のメッシュ・マテリアル・音・フォント。</summary>
    [CreateAssetMenu(menuName = "Gyorome/Eye Assets")]
    public class EyeAssets : ScriptableObject
    {
        [Serializable]
        public class NamedClip
        {
            public string name;
            public AudioClip clip;
        }

        public Mesh eyeball;
        public Mesh iris;
        public Mesh lidUpper;
        public Mesh lidLower;
        public Mesh tear;

        public Material sclera;
        public Material irisMaterial;
        public Material lid;
        public Material tearMaterial;
        public Material background;

        public List<NamedClip> clips = new List<NamedClip>();
        public AudioClip ambience;

        public Font bodyFont;
        public Font titleFont;
        public Texture2D appIcon;

        public AudioClip Clip(string name)
        {
            foreach (var c in clips)
                if (c.name == name) return c.clip;
            return null;
        }
    }
}
