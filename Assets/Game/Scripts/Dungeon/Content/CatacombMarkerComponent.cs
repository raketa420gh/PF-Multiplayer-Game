using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Game.Scripts.Dungeon
{
    public enum CatacombMarkerKind : byte
    {
        Monster,
        Container,
        Trap,
        Start
    }

    /// A point of a room the host spawns at: a monster, a container or a trap of the prefab, or where a team starts (its
    /// children are the spots). Drop a marker prefab of the kit into a room; the room finds its markers itself.
    public sealed class CatacombMarkerComponent : MonoBehaviour
    {
        public CatacombMarkerKind Kind => _kind;
        /// A monster marker without a prefab takes any monster of the floor.
        public NetworkObject Prefab => _prefab;

        [SerializeField]
        private CatacombMarkerKind _kind;

        [SerializeField]
        private NetworkObject _prefab;

        private static readonly Color[] s_colors = { new(1f, 0.25f, 0.2f), new(1f, 0.8f, 0.2f), new(0.9f, 0.4f, 1f), new(0.3f, 1f, 0.4f) };

        private readonly List<(Mesh mesh, Matrix4x4 matrix)> _preview = new();
        private NetworkObject _previewOf;

        private void OnDrawGizmos()
        {
            Color color = s_colors[(int)_kind];
            Gizmos.color = color;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, 0.35f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + transform.forward * 0.8f);

            if (_kind == CatacombMarkerKind.Start)
            {
                foreach (Transform spot in transform)
                    Gizmos.DrawWireCube(spot.position + Vector3.up * 0.9f, new Vector3(0.6f, 1.8f, 0.6f));

                return;
            }

            CachePreview();
            Gizmos.color = new Color(color.r, color.g, color.b, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;

            foreach ((Mesh mesh, Matrix4x4 matrix) part in _preview)
            {
                Gizmos.matrix = transform.localToWorldMatrix * part.matrix;
                Gizmos.DrawMesh(part.mesh);
            }

            Gizmos.matrix = Matrix4x4.identity;
        }

        /// Meshes of the prefab relative to its root, so the marker shows what will stand there.
        private void CachePreview()
        {
            if (_previewOf == _prefab)
                return;

            _previewOf = _prefab;
            _preview.Clear();

            if (_prefab == null)
                return;

            Matrix4x4 root = _prefab.transform.worldToLocalMatrix;

            foreach (MeshFilter filter in _prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh != null)
                    _preview.Add((filter.sharedMesh, root * filter.transform.localToWorldMatrix));
            }

            foreach (SkinnedMeshRenderer skin in _prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (skin.sharedMesh != null)
                    _preview.Add((skin.sharedMesh, root * skin.transform.localToWorldMatrix));
            }
        }
    }
}
