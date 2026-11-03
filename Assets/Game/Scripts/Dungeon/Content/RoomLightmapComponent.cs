using System;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// The lightmap baked for a room prefab, joined to the scene's lightmaps when the room is laid out. It holds irradiance only,
    /// with no direction, so it stays true in any of the room's four turns.
    public sealed class RoomLightmapComponent : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public Renderer Renderer;
            public int Map;
            public Vector4 ScaleOffset;
        }

        [SerializeField]
        private Texture2D[] _maps;

        [SerializeField]
        private Entry[] _entries;

        private void Awake()
        {
            if (Application.isBatchMode)
                return;

            // A scene without its own bake takes lightmaps for directional ones and divides by a direction map nobody gave it.
            LightmapSettings.lightmapsMode = LightmapsMode.NonDirectional;
            int[] indices = Register();

            foreach (Entry entry in _entries)
            {
                entry.Renderer.lightmapIndex = indices[entry.Map];
                entry.Renderer.lightmapScaleOffset = entry.ScaleOffset;
            }
        }

        public void Set(Texture2D[] maps, Entry[] entries)
        {
            _maps = maps;
            _entries = entries;
        }

        /// Rooms of a kind share their maps: a second instance finds them already there.
        private int[] Register()
        {
            LightmapData[] known = LightmapSettings.lightmaps;
            int[] indices = new int[_maps.Length];
            int added = 0;

            for (int i = 0; i < _maps.Length; i++)
            {
                indices[i] = Array.FindIndex(known, data => data.lightmapColor == _maps[i]);

                if (indices[i] < 0)
                    indices[i] = known.Length + added++;
            }

            if (added == 0)
                return indices;

            LightmapData[] all = new LightmapData[known.Length + added];
            known.CopyTo(all, 0);

            for (int i = 0; i < _maps.Length; i++)
            {
                if (all[indices[i]] == null)
                    all[indices[i]] = new LightmapData { lightmapColor = _maps[i] };
            }

            LightmapSettings.lightmaps = all;

            return indices;
        }
    }
}
