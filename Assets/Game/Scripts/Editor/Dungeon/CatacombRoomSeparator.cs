using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Dungeon;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Kit blocks overlap at their joints and carry their own UVs, so two faces in one plane flicker. Where such a pair can be
    /// seen the smaller block's face steps back a centimetre: too little to notice, plenty for the depth buffer.
    internal static class CatacombRoomSeparator
    {
        private const float Step = 0.01f;
        private const float Tolerance = 0.002f;
        private const float MinOverlap = 0.02f;
        private const int MaxPasses = 8;
        private const int ProbeRows = 5;
        /// A wall that runs past both corners of its cell would end in the plane of the neighbouring room's wall.
        private const float CornerToCorner = DungeonMapBuilder.Pitch + DungeonPropBuilder.WallThickness;

        private sealed class Box
        {
            public KitBlockComponent Block;
            public Vector3 Min;
            public Vector3 Max;
            public bool IsTurned;
            public bool IsChanged;

            public float FaceArea(int axis)
            {
                Vector3 size = Max - Min;

                return size[(axis + 1) % 3] * size[(axis + 2) % 3];
            }
        }

        [MenuItem("Tools/Game/Dungeon/Catacombs/Separate Coplanar Faces")]
        public static void SeparateAll()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { CatacombRoomBuilder.Folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (!AssetDatabase.LoadAssetAtPath<GameObject>(path).TryGetComponent(out CatacombRoomComponent _))
                    continue;

                GameObject room = PrefabUtility.LoadPrefabContents(path);
                int moved = Separate(room.transform);

                if (moved > 0)
                    PrefabUtility.SaveAsPrefabAsset(room, path);

                PrefabUtility.UnloadPrefabContents(room);
                Debug.Log($"{path}: {moved} kit blocks separated");
            }
        }

        private static int Separate(Transform room)
        {
            List<Box> boxes = room.GetComponentsInChildren<KitBlockComponent>(true).Where(IsAxisAligned).Select(block => ToBox(block, room)).ToList();

            foreach (Box box in boxes)
                TrimEnds(box);

            for (int pass = 0; pass < MaxPasses; pass++)
            {
                bool isClean = true;

                for (int a = 0; a < boxes.Count; a++)
                {
                    for (int b = a + 1; b < boxes.Count; b++)
                        isClean &= !SeparatePair(boxes, boxes[a], boxes[b]);
                }

                if (isClean)
                    break;
            }

            List<Box> changed = boxes.Where(box => box.IsChanged).ToList();

            foreach (Box box in changed)
                Write(box, room);

            return changed.Count;
        }

        private static bool SeparatePair(List<Box> boxes, Box a, Box b)
        {
            bool isMoved = false;

            for (int axis = 0; axis < 3; axis++)
            {
                foreach (int sign in new[] { -1, 1 })
                {
                    float face = sign < 0 ? a.Min[axis] : a.Max[axis];

                    if (Mathf.Abs(face - (sign < 0 ? b.Min[axis] : b.Max[axis])) > Tolerance || axis == 1 && sign < 0 && face <= 0f)
                        continue;

                    Vector3 from = Vector3.Max(a.Min, b.Min);
                    Vector3 to = Vector3.Min(a.Max, b.Max);
                    int u = (axis + 1) % 3;
                    int v = (axis + 2) % 3;

                    if (to[u] - from[u] < MinOverlap || to[v] - from[v] < MinOverlap)
                        continue;

                    if (!IsSeen(boxes, from, to, axis, face + sign * Step))
                        continue;

                    Box smaller = a.FaceArea(axis) <= b.FaceArea(axis) ? a : b;

                    if (sign < 0)
                        smaller.Min[axis] += Step;
                    else
                        smaller.Max[axis] -= Step;

                    smaller.IsChanged = true;
                    isMoved = true;
                }
            }

            return isMoved;
        }

        private static void TrimEnds(Box box)
        {
            foreach (int axis in new[] { 0, 2 })
            {
                if (Mathf.Abs(box.Max[axis] - box.Min[axis] - CornerToCorner) > Tolerance)
                    continue;

                box.Min[axis] += Step;
                box.Max[axis] -= Step;
                box.IsChanged = true;
            }
        }

        /// A grid over the shared part of the plane, a step in front of it: a joint is often covered in its middle only.
        private static bool IsSeen(List<Box> boxes, Vector3 from, Vector3 to, int axis, float plane)
        {
            int u = (axis + 1) % 3;
            int v = (axis + 2) % 3;

            for (int i = 0; i < ProbeRows; i++)
            {
                for (int j = 0; j < ProbeRows; j++)
                {
                    Vector3 probe = default;
                    probe[axis] = plane;
                    probe[u] = Mathf.Lerp(from[u], to[u], (i + 0.5f) / ProbeRows);
                    probe[v] = Mathf.Lerp(from[v], to[v], (j + 0.5f) / ProbeRows);

                    if (!boxes.Any(box => Contains(box, probe)))
                        return true;
                }
            }

            return false;
        }

        private static bool IsAxisAligned(KitBlockComponent block)
        {
            Vector3 angles = block.transform.eulerAngles;

            return !block.IsRound && Mathf.Abs(Mathf.DeltaAngle(angles.x, 0f)) < 0.01f && Mathf.Abs(Mathf.DeltaAngle(angles.z, 0f)) < 0.01f && Mathf.Abs(Mathf.DeltaAngle(angles.y, Mathf.Round(angles.y / 90f) * 90f)) < 0.01f;
        }

        private static Box ToBox(KitBlockComponent block, Transform room)
        {
            bool isTurned = Mathf.RoundToInt(block.transform.eulerAngles.y / 90f) % 2 != 0;
            Vector3 size = isTurned ? new Vector3(block.Size.z, block.Size.y, block.Size.x) : block.Size;
            Vector3 center = room.InverseTransformPoint(block.transform.position) + Vector3.up * size.y * 0.5f;

            return new Box { Block = block, Min = center - size * 0.5f, Max = center + size * 0.5f, IsTurned = isTurned };
        }

        private static bool Contains(Box box, Vector3 point)
        {
            return point.x > box.Min.x && point.x < box.Max.x && point.y > box.Min.y && point.y < box.Max.y && point.z > box.Min.z && point.z < box.Max.z;
        }

        private static void Write(Box box, Transform room)
        {
            Vector3 size = box.Max - box.Min;
            Vector3 bottom = (box.Min + box.Max) * 0.5f - Vector3.up * size.y * 0.5f;
            box.Block.transform.position = room.TransformPoint(bottom);
            box.Block.SetSize(box.IsTurned ? new Vector3(size.z, size.y, size.x) : size);
            EditorUtility.SetDirty(box.Block);
            CatacombKit.Apply(box.Block);
        }
    }
}
