using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Game.Scripts.Dungeon;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Writes the rooms as they stand in their prefabs into the data of the mockup atlas (Docs/LevelMockups/Atlas): Unity
    /// room-local metres, pivot at the bottom middle, size X × Y × Z, yaw in degrees (a quaternion when tilted). A room file
    /// marked "edited":true holds an unbuilt change of the mockup and is left alone.
    internal static partial class CatacombRoomBuilder
    {
        private const string MockupFolder = "Docs/LevelMockups/Atlas/data";
        private const string EditedMark = "\"edited\":true";
        private const string PropPrefix = "Wall Prop ";
        /// A trap of more pieces than this shows them one by one rather than as a single box.
        private const int MachineParts = 6;
        private const float BodyWidth = 0.7f;
        private const float BodyHeight = 1.8f;

        private static readonly CultureInfo s_invariant = CultureInfo.InvariantCulture;

        [MenuItem("Tools/Game/Dungeon/Catacombs/Export Mockups")]
        public static void ExportMockups()
        {
            string folder = Path.Combine(Application.dataPath, "..", MockupFolder);
            Directory.CreateDirectory(folder);
            List<Layout> layouts = Layouts().ToList();
            List<string> entries = layouts.Select(layout => Entry(layout.Name, layout.Title, layout.Weight, layout.Limit, layout.IsCorner, true)).ToList();
            entries.Add(Entry(HallName, "Great Hall", 0f, 0, false, true));
            int written = 0;

            foreach (string name in layouts.Select(layout => layout.Name).Append(HallName))
                written += WriteRoom(folder, name) ? 1 : 0;

            string sides = string.Join(",", Enumerable.Range(0, 4).Select(doorways => $"[{string.Join(",", Objects(Prefab($"CatacombSide{doorways}")))}]"));
            string post = string.Join(",", Objects(Prefab("CatacombPost")));
            string manifest = $"ATLAS.manifest={{\"pitch\":{Number(Pitch)},\"height\":{Number(Height)},\"doorOffset\":{Number(DoorOffset)},\"doorWidth\":{Number(DoorWidth)},\"doorHeight\":{Number(DoorHeight)},"
                + $"\"key\":\"{HallName}\",\"rooms\":[{string.Join(",", entries)}],\"sides\":[{sides}],\"post\":[{post}]}};\n";
            File.WriteAllText(Path.Combine(folder, "manifest.js"), manifest, new UTF8Encoding(false));
            Debug.Log($"Mockups exported: {written} of {entries.Count} rooms to {MockupFolder}");
        }

        private static string Entry(string name, string title, float weight, int limit, bool isCorner, bool isInPool)
        {
            return $"{{\"name\":\"{name}\",\"title\":\"{title.Replace("\"", "'")}\",\"weight\":{Number(weight)},\"limit\":{limit},\"corner\":{Lower(isCorner)},\"pool\":{Lower(isInPool)}}}";
        }

        private static GameObject Prefab(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/{name}.prefab");
        }

        /// False when the room file holds an edit of the mockup not yet built into the prefab.
        private static bool WriteRoom(string folder, string name)
        {
            string path = Path.Combine(folder, $"{name}.js");

            if (File.Exists(path) && File.ReadAllText(path).Contains(EditedMark))
            {
                Debug.LogWarning($"Mockup {name} holds unbuilt edits: left as it is");

                return false;
            }

            string objects = string.Join(",\n", Objects(Prefab(name)));
            File.WriteAllText(path, $"ATLAS.room(\"{name}\",[\n{objects}\n]);\n", new UTF8Encoding(false));

            return true;
        }

        private static List<string> Objects(GameObject prefab)
        {
            List<string> objects = new();
            Dictionary<string, int> counters = new();

            foreach (Transform part in prefab.transform)
                Walk(part, part.name, prefab.transform.worldToLocalMatrix, objects, counters);

            return objects;
        }

        private static void Walk(Transform part, string group, Matrix4x4 toRoot, List<string> objects, Dictionary<string, int> counters)
        {
            if (!part.gameObject.activeSelf)
                return;

            string cat = Category(group);
            string name = group.StartsWith(PropPrefix) ? group.Substring(PropPrefix.Length) : group;

            if (part.TryGetComponent(out CatacombMarkerComponent marker))
                AddMarker(marker, name, toRoot, objects, counters);
            else if (part.TryGetComponent(out KitBlockComponent block))
                objects.Add(Box(cat, name, toRoot.MultiplyPoint3x4(part.position), toRoot.rotation * part.rotation, block.Size, block.IsRound, counters));
            else if (part.TryGetComponent(out KitStairsComponent stairs))
                objects.Add(Box("stairs", name, toRoot.MultiplyPoint3x4(part.position), toRoot.rotation * part.rotation, stairs.Size, false, counters));
            else if (group.StartsWith("Light") || group.StartsWith("Shaft Light"))
                AddLights(part, group, toRoot, objects, counters);
            else if (group.StartsWith(PropPrefix) || group == "Bars" || group == "Chandelier")
            {
                AddBounds(part, part, cat, name, toRoot, objects, counters);
                AddLights(part, group, toRoot, objects, counters);
            }
            else
            {
                if (part.TryGetComponent(out MeshRenderer _))
                    AddBounds(part, part, cat, name, toRoot, objects, counters);

                foreach (Transform child in part)
                    Walk(child, group, toRoot, objects, counters);
            }
        }

        private static string Category(string group)
        {
            return group switch
            {
                "Floor" => "floor",
                "Platform" => "platform",
                "Pit Wall" => "pit",
                "Wall Column" => "column",
                "Wall Upper" => "upper",
                "Water" => "water",
                "Acid" => "acid",
                "Bars" => "bars",
                "Rune" or "Vein" or "Glow" or "Logs" => "fx",
                "Chandelier" => "prop",
                _ when group.StartsWith("Ceiling") => "ceiling",
                _ when group.StartsWith(PropPrefix) => "prop",
                _ when group.StartsWith("Wall Doorways") => "shell",
                _ when group.StartsWith("Wall") || group == "Lintel" || group == "Arch" => group == "Wall" ? "wall" : "shell",
                _ => "wall"
            };
        }

        private static void AddMarker(CatacombMarkerComponent marker, string name, Matrix4x4 toRoot, List<string> objects, Dictionary<string, int> counters)
        {
            Transform point = marker.transform;
            Vector3 position = toRoot.MultiplyPoint3x4(point.position);
            Quaternion rotation = toRoot.rotation * point.rotation;
            string title = name.Substring(name.IndexOf(' ') + 1);

            switch (marker.Kind)
            {
                case CatacombMarkerKind.Monster:
                    objects.Add(Box("monster", title, position, rotation, new Vector3(BodyWidth, BodyHeight, BodyWidth), true, counters));

                    break;
                case CatacombMarkerKind.Start:
                    objects.Add(Box("start", "Start", position, rotation, new Vector3(1f, 0.1f, 1f), true, counters));

                    foreach (Transform spot in point)
                        objects.Add(Box("start", "Spot", toRoot.MultiplyPoint3x4(spot.position), toRoot.rotation * spot.rotation, new Vector3(0.6f, BodyHeight, 0.6f), false, counters));

                    break;
                default:
                    AddSpawned(marker, marker.Kind == CatacombMarkerKind.Trap ? "trap" : "chest", title, position, rotation, toRoot, objects, counters);

                    break;
            }
        }

        /// What the host spawns at a marker: its box, or the pieces of a big machine at their places.
        private static void AddSpawned(CatacombMarkerComponent marker, string cat, string title, Vector3 position, Quaternion rotation, Matrix4x4 toRoot, List<string> objects, Dictionary<string, int> counters)
        {
            GameObject prefab = marker.Prefab != null ? marker.Prefab.gameObject : null;
            MeshRenderer[] renderers = prefab != null ? prefab.GetComponentsInChildren<MeshRenderer>(true) : new MeshRenderer[0];
            Matrix4x4 placed = toRoot * marker.transform.localToWorldMatrix * (prefab != null ? prefab.transform.worldToLocalMatrix : Matrix4x4.identity);

            if (renderers.Length > MachineParts)
            {
                objects.Add(Box(cat, title, position, rotation, Vector3.one * 0.4f, false, counters));

                foreach (MeshRenderer renderer in renderers)
                    AddBounds(renderer.transform, renderer.transform, "machine", $"{title}: {renderer.name}", placed, objects, counters);

                return;
            }

            Bounds bounds = prefab != null ? LocalBounds(prefab.transform, prefab.transform) : default;
            Vector3 size = bounds.size == Vector3.zero ? Vector3.one * 0.8f : bounds.size;
            Vector3 bottom = marker.transform.TransformPoint(bounds.center - Vector3.up * bounds.extents.y);
            objects.Add(Box(cat, title, toRoot.MultiplyPoint3x4(bottom), rotation, size, false, counters));
        }

        private static void AddBounds(Transform frame, Transform content, string cat, string name, Matrix4x4 toRoot, List<string> objects, Dictionary<string, int> counters)
        {
            Bounds bounds = LocalBounds(frame, content);

            if (bounds.size == Vector3.zero)
                return;

            Vector3 bottom = frame.TransformPoint(bounds.center - Vector3.up * bounds.extents.y);
            objects.Add(Box(cat, name, toRoot.MultiplyPoint3x4(bottom), toRoot.rotation * frame.rotation, Vector3.Scale(bounds.size, frame.lossyScale), false, counters));
        }

        /// Bounds of the meshes below content in the space of frame.
        private static Bounds LocalBounds(Transform frame, Transform content)
        {
            Bounds bounds = default;
            bool isFirst = true;

            foreach (Renderer renderer in content.GetComponentsInChildren<Renderer>(true))
            {
                Mesh mesh = renderer is SkinnedMeshRenderer skin ? skin.sharedMesh : renderer.TryGetComponent(out MeshFilter filter) ? filter.sharedMesh : null;

                if (mesh == null || renderer is ParticleSystemRenderer)
                    continue;

                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = mesh.bounds.center + Vector3.Scale(mesh.bounds.extents, new Vector3(i & 1, i >> 1 & 1, i >> 2 & 1) * 2f - Vector3.one);
                    Vector3 point = frame.InverseTransformPoint(renderer.transform.TransformPoint(corner));

                    if (isFirst)
                        bounds = new Bounds(point, Vector3.zero);
                    else
                        bounds.Encapsulate(point);

                    isFirst = false;
                }
            }

            return bounds;
        }

        private static void AddLights(Transform part, string group, Matrix4x4 toRoot, List<string> objects, Dictionary<string, int> counters)
        {
            foreach (Light light in part.GetComponentsInChildren<Light>(true))
            {
                string kind = group.Replace("Light ", string.Empty).Replace(" Light", string.Empty);
                Vector3 position = toRoot.MultiplyPoint3x4(light.transform.position);
                string color = ColorUtility.ToHtmlStringRGB(light.color);
                objects.Add($"{{\"id\":\"{NextId("light", counters)}\",\"cat\":\"light\",\"name\":\"{kind}\",\"p\":{Vector(position)},\"color\":\"#{color}\",\"i\":{Number(light.intensity)},\"r\":{Number(light.range)}}}");
            }
        }

        private static string Box(string cat, string name, Vector3 position, Quaternion rotation, Vector3 size, bool isRound, Dictionary<string, int> counters)
        {
            Vector3 euler = rotation.eulerAngles;
            bool isTilted = Mathf.Abs(Mathf.DeltaAngle(euler.x, 0f)) > 0.01f || Mathf.Abs(Mathf.DeltaAngle(euler.z, 0f)) > 0.01f;
            string turn = isTilted ? $"\"q\":[{Number(rotation.x, 4)},{Number(rotation.y, 4)},{Number(rotation.z, 4)},{Number(rotation.w, 4)}]" : $"\"ry\":{Number(Mathf.Repeat(euler.y, 360f))}";

            return $"{{\"id\":\"{NextId(cat, counters)}\",\"cat\":\"{cat}\",\"name\":\"{name.Replace("\"", "'")}\",\"p\":{Vector(position)},{turn},\"s\":{Vector(size)}{(isRound ? ",\"round\":true" : string.Empty)}}}";
        }

        private static string NextId(string cat, Dictionary<string, int> counters)
        {
            counters.TryGetValue(cat, out int count);
            counters[cat] = ++count;

            return $"{char.ToUpperInvariant(cat[0])}{cat[1]}{count}";
        }

        private static string Vector(Vector3 value)
        {
            return $"[{Number(value.x)},{Number(value.y)},{Number(value.z)}]";
        }

        private static string Number(float value, int digits = 3)
        {
            return System.Math.Round(value, digits).ToString("0.###", s_invariant);
        }

        private static string Lower(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
