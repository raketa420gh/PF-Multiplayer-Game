using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Scripts.Editor.Dungeon
{
    /// Builds the hub blockout from the mockup export (Docs/02_Gameplay/HubMockup/export/hub.json, written by tools/export.js):
    /// one prefab with a group per element ID of the mockup, every piece a primitive (cube, cylinder, sphere) or a raw mesh in a
    /// flat colour, fires as point lights, plus the stations of the UI with their models, the arrival points, the dummy spots and the fencing hall.
    /// The scene build takes the prefab as it is, so its pieces can be swapped for models by hand; only Apply Mockup rebuilds it.
    internal static class HubBlockoutBuilder
    {
        [Serializable]
        private sealed class Export
        {
            public Piece[] meshes;
            public Geometry[] geos;
            public Paint[] mats;
            public Fire[] lights;
            public Marker[] markers;
            public Marker[] spawns;
            public Hall hall;
        }

        [Serializable]
        private sealed class Piece
        {
            public string t;
            public string m;
            public string k;
            public float[] p;
            public float[] q;
            public float[] s;
            public int g;
            /// Seen but not felt: the spandrels of an arch would catch a head in a narrow doorway.
            public int nc;
        }

        [Serializable]
        private sealed class Geometry
        {
            public float[] v;
            public int[] i;
            public int flat;
        }

        [Serializable]
        private sealed class Paint
        {
            public string n;
            public float[] c;
            public int glow;
            public float metal;
            public float rough;
            public float a;
            public int ds;
        }

        [Serializable]
        private sealed class Fire
        {
            public string t;
            public float[] p;
            public float[] c;
            public float i;
            public float d;
        }

        [Serializable]
        private sealed class Marker
        {
            public string id;
            public string n;
            public float[] p;
            public float yaw;
        }

        [Serializable]
        private sealed class Hall
        {
            public float[] p;
            public float yaw;
            public float[] s;
        }

        public const string PrefabPath = "Assets/Game/Prefabs/Dungeon/Hub/HubBlockout.prefab";
        public const string StationLayer = "Interactable";
        public const string Stations = "Stations";
        public const string Spawns = "Spawns";
        public const string Dummies = "Dummies";
        public const string FencingHall = "FencingHall";
        private const string JsonPath = "Docs/02_Gameplay/HubMockup/export/hub.json";
        private const string MeshesPath = DungeonMeshBuilder.Folder + "/HubBlockout.asset";
        private const string MaterialsFolder = "Assets/Game/Materials/Hub";
        /// Pieces smaller than this neither stop a body nor cast a shadow.
        private const float ClutterSize = 0.3f;
        /// three.js candela to a URP point light: the Lambert 1/π, a little under for the brighter grade of the game.
        private const float LightScale = 0.6f / Mathf.PI;

        /// Marker of the mockup → what pressing F there opens, and the elements of the mockup that are its model: the crosshair
        /// on any piece of them finds the station. A merchant behind a window or a counter is the figure and the counter.
        /// A grille is mostly gaps: with the fill one box over the whole model catches the look between the bars.
        private static readonly (string id, HubStation station, int index, string title, string prompt, string[] models, bool fill)[] s_stations =
        {
            ("A3", HubStation.Descent, 0, "Book of Descents", "Open the Book of Descents", new[] { "A3" }, false),
            ("B2", HubStation.Party, 0, "Party Board", "Read the party board", new[] { "B2" }, false),
            ("E6", HubStation.Skills, 0, "Skill Rack", "Change perks, skills and spells", new[] { "E6" }, false),
            ("E7", HubStation.Stash, 0, "Weapon Rack · Stash", "Open the stash", new[] { "E7" }, false),
            ("F2", HubStation.Stash, 0, "Veinworker's Chest", "Open the stash", new[] { "F2" }, false),
            ("F3", HubStation.SharedStash, 0, "Shared Chest", "Open the shared chest", new[] { "F3" }, false),
            ("F6", HubStation.Character, 0, "Kit", "Look over the kit", new[] { "F6" }, false),
            ("G1", HubStation.Road, 0, string.Empty, "Leave for the character select", new[] { "G1" }, true),
            ("C4.1", HubStation.Merchant, 0, "Halvard Rik · Quartermaster", "Trade with Halvard Rik", new[] { "M1", "C4.1" }, false),
            ("M2", HubStation.Merchant, 1, "Jutta Kolb · Weaponsmith", "Trade with Jutta Kolb", new[] { "M2" }, false),
            ("C3.4", HubStation.Merchant, 2, "Oswin Tarn · Armourer", "Trade with Oswin Tarn", new[] { "M3", "C3.4" }, false),
            ("M4", HubStation.Merchant, 3, "Sister Agneta · Cathedral of Repose", "Trade with Sister Agneta", new[] { "M4" }, false),
            ("C1.1", HubStation.Appraiser, 0, "Cassian Veit · Appraiser", "Sell to Cassian Veit", new[] { "M5", "C1.1" }, false)
        };

        /// Arrival points by the marker they stand at, named after HubArrival (and the gate of the guests).
        private static readonly (string id, string name)[] s_spawns = { ("F1", "Home"), ("A4", "Anchor"), ("D2", "Infirmary"), ("G2", "Gate") };
        /// Static dummies of the mockup: the game spawns networked ones on their spots.
        private static readonly string[] s_dummies = { "E2", "E3" };

        public static GameObject Load()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            return prefab != null ? prefab : Build();
        }

        [MenuItem("Tools/Game/Dungeon/Hub/Apply Mockup")]
        public static GameObject Build()
        {
            Export data = JsonUtility.FromJson<Export>(File.ReadAllText(JsonPath));
            BattleEditorUtility.EnsureFolder(MaterialsFolder);
            BattleEditorUtility.EnsureFolder(Path.GetDirectoryName(PrefabPath).Replace('\\', '/'));
            BattleEditorUtility.EnsureFolder(DungeonMeshBuilder.Folder);
            Dictionary<string, Material> materials = data.mats.ToDictionary(paint => paint.n, CreateMaterial);
            Mesh[] meshes = CreateMeshes(data.geos);

            GameObject root = new GameObject("HubBlockout");
            Transform geometry = BattleEditorUtility.CreateChild("Geometry", root.transform).transform;
            Dictionary<string, Marker> markers = data.markers.ToDictionary(marker => marker.id);
            Dictionary<string, Transform> groups = new();
            // The model of a station lies under the station itself: whatever is put there in its place is found the same way.
            Dictionary<string, Transform> models = new();
            Transform stations = BattleEditorUtility.CreateChild(Stations, root.transform).transform;

            foreach ((string id, HubStation station, int index, string title, string prompt, string[] tags, bool _) in s_stations)
            {
                GameObject go = BattleEditorUtility.CreateChild(id, stations, ToVector(markers[id].p));
                SerializedObject so = new SerializedObject(go.AddComponent<HubStationComponent>());
                so.FindProperty("_station").enumValueIndex = (int)station;
                BattleEditorUtility.Set(so, "_index", index);
                BattleEditorUtility.Set(so, "_title", title);
                BattleEditorUtility.Set(so, "_prompt", prompt);
                so.ApplyModifiedPropertiesWithoutUndo();

                foreach (string tag in tags)
                    models.Add(tag, go.transform);
            }

            Transform Group(string tag)
            {
                if (groups.TryGetValue(tag, out Transform group))
                    return group;

                groups[tag] = group = BattleEditorUtility.CreateChild(tag, models.GetValueOrDefault(tag, geometry)).transform;
                group.position = Vector3.zero;

                return group;
            }

            foreach (Piece piece in data.meshes.Where(piece => !s_dummies.Contains(piece.t)))
                CreatePiece(piece, Group(piece.t), materials[piece.m], meshes, models.ContainsKey(piece.t));

            foreach ((string id, HubStation _, int _, string _, string _, string[] _, bool fill) in s_stations)
            {
                Transform station = stations.Find(id);

                if (fill)
                    Fill(station);

                if (station.GetComponentInChildren<Collider>() == null)
                    Debug.LogError($"[{nameof(HubBlockoutBuilder)}] Station {id} has no model in the export: nothing to aim at");
            }

            foreach (Fire fire in data.lights)
                DungeonPropBuilder.PointLight(Group(fire.t), ToVector(fire.p), new Color(fire.c[0], fire.c[1], fire.c[2]), fire.d, fire.i * LightScale, true);

            Transform markerRoot = BattleEditorUtility.CreateChild("Markers", root.transform).transform;

            foreach (Marker marker in data.markers)
                BattleEditorUtility.CreateChild($"{marker.id} {marker.n}", markerRoot, ToVector(marker.p));

            Dictionary<string, Marker> arrivals = data.spawns.ToDictionary(spawn => spawn.id);
            Transform spawns = BattleEditorUtility.CreateChild(Spawns, root.transform).transform;

            foreach ((string id, string name) in s_spawns)
                BattleEditorUtility.CreateChild(name, spawns, ToVector(arrivals[id].p)).transform.localRotation = Quaternion.Euler(0f, arrivals[id].yaw, 0f);

            // Dummies stand at the far wall of the hall and look down its length.
            Transform dummies = BattleEditorUtility.CreateChild(Dummies, root.transform).transform;

            foreach (string id in s_dummies)
                BattleEditorUtility.CreateChild(id, dummies, ToVector(markers[id].p)).transform.localRotation = Quaternion.Euler(0f, data.hall.yaw + 180f, 0f);

            GameObject hall = BattleEditorUtility.CreateChild(FencingHall, root.transform, ToVector(data.hall.p));
            hall.transform.localRotation = Quaternion.Euler(0f, data.hall.yaw, 0f);
            // Only the box is read (HubDirector.IsInHall). Switched off: a live trigger on the Ignore Raycast layer is standing water to a body and slows it.
            BoxCollider area = hall.AddComponent<BoxCollider>();
            area.isTrigger = true;
            area.enabled = false;
            area.size = ToVector(data.hall.s);
            area.center = Vector3.up * data.hall.s[1] * 0.5f;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[{nameof(HubBlockoutBuilder)}] Blockout built: {data.meshes.Length} pieces, {data.lights.Length} lights, {s_stations.Length} stations");

            return prefab;
        }

        private static void CreatePiece(Piece piece, Transform parent, Material material, Mesh[] meshes, bool isStation)
        {
            bool isRaw = piece.k == "raw";
            GameObject go = isRaw ? new GameObject() : GameObject.CreatePrimitive(piece.k == "box" ? PrimitiveType.Cube : piece.k == "cyl" ? PrimitiveType.Cylinder : PrimitiveType.Sphere);
            go.name = isRaw ? "Mesh" : go.name;
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(ToVector(piece.p), new Quaternion(piece.q[0], piece.q[1], piece.q[2], piece.q[3]));
            go.transform.localScale = ToVector(piece.s);
            go.isStatic = true;

            // The rim of a station is drawn renderer by renderer: its pieces stay out of the static batch.
            if (isStation)
                GameObjectUtility.SetStaticEditorFlags(go, GameObjectUtility.GetStaticEditorFlags(go) & ~StaticEditorFlags.BatchingStatic);

            if (isRaw)
            {
                go.AddComponent<MeshFilter>().sharedMesh = meshes[piece.g];
                go.AddComponent<MeshRenderer>();
            }

            MeshRenderer renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            Mesh mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Vector3 size = Vector3.Scale(mesh.bounds.size, go.transform.localScale);
            bool isClutter = Mathf.Max(size.x, size.y, size.z) < ClutterSize || material.IsKeywordEnabled("_EMISSION");

            if (isClutter)
                renderer.shadowCastingMode = ShadowCastingMode.Off;

            // A capsule would round the ends of a post or a drum: cylinders collide as what they are.
            if ((isClutter && !isStation) || piece.k == "cyl")
                UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());

            bool isSolid = !isClutter && piece.nc == 0;

            if (!isSolid && !isStation)
                return;

            if (isRaw || piece.k == "cyl")
            {
                MeshCollider hull = go.AddComponent<MeshCollider>();
                hull.sharedMesh = mesh;
                hull.convex = !isRaw || !isSolid;
            }

            if (isSolid)
                return;

            // Too small to stop a body, but still a part of what the crosshair rests on.
            go.GetComponent<Collider>().isTrigger = true;
            go.layer = LayerMask.NameToLayer(StationLayer);
        }

        /// One trigger box over the whole model of a station, turned as its first box is.
        private static void Fill(Transform station)
        {
            Quaternion rotation = station.GetComponentInChildren<BoxCollider>().transform.rotation;
            Quaternion inverse = Quaternion.Inverse(rotation);
            Bounds bounds = default;
            bool isEmpty = true;

            foreach (MeshFilter filter in station.GetComponentsInChildren<MeshFilter>())
            {
                Bounds local = filter.sharedMesh.bounds;

                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 sign = new Vector3((corner & 1) * 2 - 1, (corner >> 1 & 1) * 2 - 1, (corner >> 2 & 1) * 2 - 1);
                    Vector3 point = inverse * filter.transform.TransformPoint(local.center + Vector3.Scale(local.extents, sign));

                    if (isEmpty)
                        bounds = new Bounds(point, Vector3.zero);
                    else
                        bounds.Encapsulate(point);

                    isEmpty = false;
                }
            }

            GameObject go = BattleEditorUtility.CreateChild("Fill", station);
            go.layer = LayerMask.NameToLayer(StationLayer);
            go.transform.SetPositionAndRotation(rotation * bounds.center, rotation);
            BoxCollider volume = go.AddComponent<BoxCollider>();
            volume.isTrigger = true;
            volume.size = bounds.size;
        }

        private static Mesh[] CreateMeshes(Geometry[] geometries)
        {
            Mesh[] meshes = new Mesh[geometries.Length];
            AssetDatabase.DeleteAsset(MeshesPath);

            for (int index = 0; index < geometries.Length; index++)
            {
                Geometry geometry = geometries[index];
                Vector3[] vertices = new Vector3[geometry.v.Length / 3];

                for (int i = 0; i < vertices.Length; i++)
                    vertices[i] = new Vector3(geometry.v[i * 3], geometry.v[i * 3 + 1], geometry.v[i * 3 + 2]);

                int[] triangles = geometry.i;

                // Flat shading: every triangle gets its own corners.
                if (geometry.flat != 0)
                {
                    vertices = triangles.Select(i => vertices[i]).ToArray();
                    triangles = Enumerable.Range(0, vertices.Length).ToArray();
                }

                Mesh mesh = new Mesh { name = $"Hub{index}", indexFormat = vertices.Length > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                mesh.SetVertices(vertices);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                meshes[index] = mesh;

                if (index == 0)
                    AssetDatabase.CreateAsset(mesh, MeshesPath);
                else
                    AssetDatabase.AddObjectToAsset(mesh, MeshesPath);
            }

            return meshes;
        }

        private static Material CreateMaterial(Paint paint)
        {
            Color color = new Color(paint.c[0], paint.c[1], paint.c[2], paint.a);

            if (paint.a < 1f)
                return DungeonPropBuilder.TransparentUnlit($"Hub_{paint.n}", color);

            string path = $"{MaterialsFolder}/Hub_{paint.n}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", paint.metal);
            material.SetFloat("_Smoothness", Mathf.Min(0.6f, 1f - paint.rough));
            material.SetFloat("_Cull", paint.ds != 0 ? (float)CullMode.Off : (float)CullMode.Back);

            if (paint.glow != 0)
            {
                material.SetColor("_EmissionColor", color * 2.5f);
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            EditorUtility.SetDirty(material);

            return material;
        }

        private static Vector3 ToVector(float[] values)
        {
            return new Vector3(values[0], values[1], values[2]);
        }
    }
}
