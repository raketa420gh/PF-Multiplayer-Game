using System;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Terrain ground: the strongest painted layer under the foot picks the surface, ground under the water line is water.
    [RequireComponent(typeof(Terrain))]
    public sealed class FootstepTerrainSurfaceTag : FootstepSurfaceTag
    {
        [SerializeField, Tooltip("Surface per terrain layer, in the order of the terrain layers")]
        private FootstepSurface[] _layers = Array.Empty<FootstepSurface>();

        [SerializeField]
        private float _waterLevel = float.MinValue;

        [SerializeField]
        private Terrain _terrain;

        public override FootstepSurface GetSurface(Vector3 point)
        {
            if (point.y < _waterLevel)
                return FootstepSurface.Water;

            TerrainData data = _terrain.terrainData;
            Vector3 local = point - _terrain.transform.position;
            int x = Mathf.Clamp((int)(local.x / data.size.x * data.alphamapWidth), 0, data.alphamapWidth - 1);
            int z = Mathf.Clamp((int)(local.z / data.size.z * data.alphamapHeight), 0, data.alphamapHeight - 1);
            float[,,] weights = data.GetAlphamaps(x, z, 1, 1);
            int strongest = 0;

            for (int i = 1; i < weights.GetLength(2); i++)
            {
                if (weights[0, 0, i] > weights[0, 0, strongest])
                    strongest = i;
            }

            return strongest < _layers.Length ? _layers[strongest] : base.GetSurface(point);
        }
    }
}
