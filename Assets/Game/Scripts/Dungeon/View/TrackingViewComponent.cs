using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    /// Tracking of the huntsman, seen only by him: silhouettes of foes within the effect's range glow through the walls.
    public sealed class TrackingViewComponent : MonoBehaviour
    {
        [SerializeField]
        private AdventurerComponent _adventurer;

        [SerializeField]
        private Material _material;

        private readonly Dictionary<Transform, SkinnedMeshRenderer[]> _renderers = new();
        private readonly List<Mesh> _meshes = new();
        private readonly List<Transform> _targets = new();
        private NetworkObject _object;

        private void Awake()
        {
            _object = GetComponent<NetworkObject>();
        }

        private void LateUpdate()
        {
            if (_material == null || _object == null || !_object.IsValid || !_object.HasInputAuthority)
                return;

            float range = _adventurer.Effects.GetMagnitude(StatusEffectKind.Tracking);

            if (range <= 0f)
                return;

            CollectTargets(range);
            int used = 0;
            RenderParams parameters = new RenderParams(_material) { layer = gameObject.layer };

            foreach (Transform target in _targets)
            {
                foreach (SkinnedMeshRenderer skin in GetRenderers(target))
                {
                    if (skin == null || !skin.gameObject.activeInHierarchy)
                        continue;

                    if (used == _meshes.Count)
                        _meshes.Add(new Mesh());

                    Mesh mesh = _meshes[used++];
                    skin.BakeMesh(mesh, true);
                    Transform bone = skin.transform;
                    Matrix4x4 matrix = Matrix4x4.TRS(bone.position, bone.rotation, Vector3.one);

                    for (int sub = 0; sub < mesh.subMeshCount; sub++)
                        Graphics.RenderMesh(parameters, mesh, sub, matrix);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (Mesh mesh in _meshes)
                Destroy(mesh);
        }

        /// Living monsters and adventurers of other parties within the range.
        private void CollectTargets(float range)
        {
            _targets.Clear();
            Vector3 position = transform.position;

            foreach (MonsterComponent monster in MonsterComponent.All)
            {
                if (monster.Fighter.Health.IsAlive && (monster.transform.position - position).sqrMagnitude <= range * range)
                    _targets.Add(monster.transform);
            }

            foreach (Game.Scripts.Battle.FighterComponent fighter in Game.Scripts.Battle.FighterComponent.All)
            {
                if (fighter.TryGetComponent(out AdventurerComponent other) && !_adventurer.IsAllyOf(other) && other.State == AdventurerState.Alive
                    && (fighter.transform.position - position).sqrMagnitude <= range * range)
                    _targets.Add(fighter.transform);
            }
        }

        private SkinnedMeshRenderer[] GetRenderers(Transform target)
        {
            if (!_renderers.TryGetValue(target, out SkinnedMeshRenderer[] renderers) || renderers.Length == 0 || renderers[0] == null)
            {
                renderers = target.GetComponentsInChildren<SkinnedMeshRenderer>();
                _renderers[target] = renderers;
            }

            return renderers;
        }
    }
}
