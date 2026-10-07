using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Scripts.Dungeon
{
    /// Renders only the floor the local adventurer stands on: renderers, terrains, lights and volumes of other floors stay off
    /// until a descent, and the floor's own air (ambient light, fog, view distance) is applied. Colliders are untouched, the host
    /// still simulates every floor.
    public sealed class FloorVisibilityView : MonoBehaviour
    {
        [Serializable]
        public struct Atmosphere
        {
            public Color Sky;
            public Color Equator;
            public Color Ground;
            public Color Fog;
            public float FogDensity;
            public float ViewDistance;
            public Cubemap Reflection;
        }

        [SerializeField]
        private DungeonContext _context;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private Transform[] _floors;

        [SerializeField]
        private Atmosphere[] _atmospheres;

        private Renderer[][] _renderers;
        private Light[][] _lights;
        private Terrain[][] _terrains;
        private Volume[][] _volumes;
        private int _shownFloor = -1;

        private void Awake()
        {
            _renderers = new Renderer[_floors.Length][];
            _lights = new Light[_floors.Length][];
            _terrains = new Terrain[_floors.Length][];
            _volumes = new Volume[_floors.Length][];

            for (int i = 0; i < _floors.Length; i++)
            {
                _renderers[i] = _floors[i].GetComponentsInChildren<Renderer>(true);
                _lights[i] = _floors[i].GetComponentsInChildren<Light>(true);
                _terrains[i] = _floors[i].GetComponentsInChildren<Terrain>(true);
                _volumes[i] = _floors[i].GetComponentsInChildren<Volume>(true);
            }
        }

        private void LateUpdate()
        {
            AdventurerComponent adventurer = _context.LocalAdventurer;
            bool isInside = adventurer != null && adventurer.Object != null && adventurer.Object.IsValid;
            int floor = isInside ? Mathf.Clamp(adventurer.Floor - 1, 0, _floors.Length - 1) : 0;

            if (floor == _shownFloor)
                return;

            _shownFloor = floor;

            for (int i = 0; i < _floors.Length; i++)
            {
                bool isShown = i == floor;

                foreach (Renderer renderer in _renderers[i])
                    renderer.enabled = isShown;

                foreach (Light light in _lights[i])
                    light.enabled = isShown;

                foreach (Terrain terrain in _terrains[i])
                    terrain.enabled = isShown;

                foreach (Volume volume in _volumes[i])
                    volume.enabled = isShown;
            }

            if (floor < _atmospheres.Length)
                Apply(_atmospheres[floor]);
        }

        private void Apply(Atmosphere atmosphere)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = atmosphere.Sky;
            RenderSettings.ambientEquatorColor = atmosphere.Equator;
            RenderSettings.ambientGroundColor = atmosphere.Ground;
            RenderSettings.fogColor = atmosphere.Fog;
            RenderSettings.fogDensity = atmosphere.FogDensity;
            RenderSettings.customReflectionTexture = atmosphere.Reflection;
            _camera.backgroundColor = atmosphere.Fog;
            _camera.farClipPlane = atmosphere.ViewDistance;
        }
    }
}
