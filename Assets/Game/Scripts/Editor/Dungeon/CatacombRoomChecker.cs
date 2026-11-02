using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Scripts.Dungeon;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.Editor.Dungeon
{
    /// Every room baked alone the way the generator bakes a floor, its sides around it with all doorways open: each doorway must
    /// be free and reach the others, each marker must be on the NavMesh and reached from a doorway.
    internal static class CatacombRoomChecker
    {
        private const float SideOffset = DungeonMapBuilder.Pitch * 0.5f;
        private const float DoorInside = 16.2f;
        private const int NotWalkable = 1;
        private static readonly Vector3 s_site = new(0f, -900f, 0f);

        [MenuItem("Tools/Game/Dungeon/Catacombs/Check Rooms")]
        public static void CheckRooms()
        {
            Debug.Log(CheckAll());
        }

        public static string CheckAll()
        {
            StringBuilder report = new StringBuilder();

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { CatacombRoomBuilder.Folder }))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));

                if (prefab.TryGetComponent(out CatacombRoomComponent room) && prefab.name != CatacombRoomBuilder.HallName && prefab.name != "GreatHall")
                    report.AppendLine(Check(prefab, room.IsCorner));
            }

            return report.ToString();
        }

        private static string Check(GameObject prefab, bool isCorner)
        {
            GameObject root = new GameObject("RoomCheck");
            root.transform.position = s_site;
            Object.Instantiate(prefab, s_site, Quaternion.identity, root.transform);
            GameObject open = LoadSide(3);
            GameObject blind = LoadSide(0);
            Object.Instantiate(open, s_site + Vector3.forward * SideOffset, Quaternion.identity, root.transform);
            Object.Instantiate(open, s_site + Vector3.right * SideOffset, Quaternion.Euler(0f, -90f, 0f), root.transform);
            Object.Instantiate(isCorner ? blind : open, s_site + Vector3.back * SideOffset, Quaternion.identity, root.transform);
            Object.Instantiate(isCorner ? blind : open, s_site + Vector3.left * SideOffset, Quaternion.Euler(0f, -90f, 0f), root.transform);
            NavMeshDataInstance instance = NavMesh.AddNavMeshData(Bake(root.transform));
            List<string> problems = new();
            List<Vector3> doors = Doors(isCorner).Select(door => s_site + door).ToList();
            NavMeshHit first = default;
            bool hasFirst = false;
            NavMeshPath path = new NavMeshPath();

            foreach (Vector3 door in doors)
            {
                if (!NavMesh.SamplePosition(door, out NavMeshHit hit, 0.8f, NavMesh.AllAreas))
                {
                    problems.Add($"doorway {door - s_site}: no NavMesh");

                    continue;
                }

                if (!hasFirst)
                    (first, hasFirst) = (hit, true);
                else if (!NavMesh.CalculatePath(first.position, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                    problems.Add($"doorway {door - s_site}: cut off");
            }

            int markers = 0;

            foreach (CatacombMarkerComponent marker in root.GetComponentsInChildren<CatacombMarkerComponent>().Where(m => m.Kind != CatacombMarkerKind.Trap))
            {
                markers++;

                if (!hasFirst || !NavMesh.SamplePosition(marker.transform.position, out NavMeshHit hit, 1.5f, NavMesh.AllAreas)
                    || !NavMesh.CalculatePath(first.position, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                    problems.Add($"{marker.name} {marker.transform.position - s_site}: unreachable");
            }

            // Interior doors are no part of the NavMesh: both sides of one must be reached.
            foreach (CatacombMarkerComponent door in root.GetComponentsInChildren<CatacombMarkerComponent>().Where(m => m.name.Contains("Door")))
            {
                foreach (float side in new[] { -1.2f, 1.2f })
                {
                    Vector3 point = door.transform.position + door.transform.forward * side;

                    if (!hasFirst || !NavMesh.SamplePosition(point, out NavMeshHit hit, 0.8f, NavMesh.AllAreas)
                        || !NavMesh.CalculatePath(first.position, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                        problems.Add($"{door.name} side {point - s_site}: unreachable");
                }
            }

            NavMesh.RemoveNavMeshData(instance);
            Object.DestroyImmediate(root);

            return $"{(problems.Count == 0 ? "OK  " : "FAIL")} {prefab.name}: {doors.Count} doorways, {markers} markers{(problems.Count == 0 ? string.Empty : " | " + string.Join("; ", problems.Take(6)) + (problems.Count > 6 ? $" … {problems.Count}" : string.Empty))}";
        }

        private static GameObject LoadSide(int doorways)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{CatacombRoomBuilder.Folder}/CatacombSide{doorways}.prefab");
        }

        private static IEnumerable<Vector3> Doors(bool isCorner)
        {
            foreach (float sign in new[] { -1f, 1f })
            {
                yield return new Vector3(sign * CatacombRoomBuilder.DoorOffset, 0f, DoorInside);
                yield return new Vector3(DoorInside, 0f, sign * CatacombRoomBuilder.DoorOffset);

                if (isCorner)
                    continue;

                yield return new Vector3(sign * CatacombRoomBuilder.DoorOffset, 0f, -DoorInside);
                yield return new Vector3(-DoorInside, 0f, sign * CatacombRoomBuilder.DoorOffset);
            }
        }

        private static NavMeshData Bake(Transform root)
        {
            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = 0.35f;
            settings.agentHeight = 1.8f;
            settings.overrideVoxelSize = true;
            settings.voxelSize = 0.2f;
            List<NavMeshBuildMarkup> markups = new();
            List<NavMeshBuildSource> sources = new();

            foreach (Transform child in root.GetComponentsInChildren<Transform>())
            {
                if (child.name.StartsWith("Wall") || child.name.StartsWith("Ceiling") || child.name.StartsWith("Catacomb"))
                    markups.Add(new NavMeshBuildMarkup { root = child, overrideArea = true, area = NotWalkable });
            }

            NavMeshBuilder.CollectSources(root, 1, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);

            return NavMeshBuilder.BuildNavMeshData(settings, sources, new Bounds(s_site + Vector3.up * 7.5f, new Vector3(36f, 37f, 36f)), Vector3.zero, Quaternion.identity);
        }
    }
}
