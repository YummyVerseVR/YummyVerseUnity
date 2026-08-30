using GLTFast;
using UnityEngine;

namespace YummyVerse.Scripts.Model.Struct
{
    public struct Food
    {
        public GltfImport GltfImport;

        /// <summary>
        /// この食品を噛んだときに鳴らす音。用意されていない食品では null になり、
        /// ChewingSensorConfig の既定音へフォールバックする。
        /// </summary>
        public AudioClip ChewSound;
    }
}