using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Game.Scripts.Editor.Dungeon
{
    /// Rooms are laid out at run time, so a scene bake cannot reach them: each room prefab is baked alone in a scene of its own, inside
    /// a shell of its cell, and keeps its lightmap in a RoomLightmapComponent. Only kit stone is lightmapped; props cast baked shadows
    /// and are lit with bodies by the lamp's real-time twin, which the bake leaves on the default layer only.
    internal static class CatacombLightBaker
    {
        public const string Folder = "Assets/Game/Lightmaps";

        private const string ScenePath = Folder + "/Bake.unity";
        private const string BakeOutput = Folder + "/Bake";
        private const string SettingsPath = Folder + "/Bake.lighting";
        private const string PrefabsRoot = "Assets/Game/Prefabs";
        private const string ShellSide = "CatacombSide3";
        private const string UnusedHall = "GreatHall";
        private const float TexelsPerMeter = 4f;
        private const float Half = DungeonMapBuilder.Pitch * 0.5f;

        [MenuItem("Tools/Game/Dungeon/Catacombs/Bake Room Lightmaps")]
        public static void BakeAll()
        {
            BakeRooms(null);
        }

        /// A light of a prefab reaches lightmapped stone only on its layer: torches in hands, spells, orbs, the lamps of the kit.
        [MenuItem("Tools/Game/Dungeon/Catacombs/Let Prefab Lights Reach Baked Stone")]
        public static void ReachBakedStone()
        {
            HashSet<string> rooms = RoomPaths().ToHashSet();

            foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabsRoot }).Select(AssetDatabase.GUIDToAssetPath).Where(path => !rooms.Contains(path)))
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentInChildren<Light>(true) == null)
                    continue;

                GameObject prefab = PrefabUtility.LoadPrefabContents(path);

                foreach (Light light in prefab.GetComponentsInChildren<Light>(true))
                    DungeonLightLayers.Set(light, DungeonLightLayers.Everything);

                PrefabUtility.SaveAsPrefabAsset(prefab, path);
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            Debug.Log("Prefab lights reach baked stone");
        }

        /// names null for every room; rooms already holding a lightmap are baked again only when asked by name.
        public static void BakeRooms(string[] names)
        {
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                Debug.LogError("Save the open scene before baking");

                return;
            }

            string opened = EditorSceneManager.GetActiveScene().path;
            BattleEditorUtility.EnsureFolder(Folder);
            System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
            List<string> paths = RoomPaths().Where(path => names == null ? !Holds(path) : names.Contains(Path.GetFileNameWithoutExtension(path))).ToList();

            for (int i = 0; i < paths.Count; i++)
            {
                Bake(paths[i]);
                Debug.Log($"Baked {Path.GetFileNameWithoutExtension(paths[i])} ({i + 1}/{paths.Count}, {watch.Elapsed.TotalMinutes:0.0} min)");
            }

            AssetDatabase.DeleteAsset(BakeOutput);
            AssetDatabase.DeleteAsset(ScenePath);

            if (!string.IsNullOrEmpty(opened))
                EditorSceneManager.OpenScene(opened);

            Debug.Log($"Room lightmaps baked: {paths.Count}");
        }

        public static IEnumerable<string> RoomPaths()
        {
            return AssetDatabase.FindAssets("t:Prefab", new[] { CatacombRoomBuilder.Folder }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => Path.GetDirectoryName(path)?.Replace('\\', '/') == CatacombRoomBuilder.Folder
                               && Path.GetFileNameWithoutExtension(path) != UnusedHall
                               && AssetDatabase.LoadAssetAtPath<GameObject>(path).TryGetComponent(out CatacombRoomComponent _))
                .OrderBy(path => path);
        }

        private static bool Holds(string path)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).TryGetComponent(out RoomLightmapComponent _);
        }

        private static void Bake(string path)
        {
            string room = Path.GetFileNameWithoutExtension(path);
            UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetAir();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Lightmapping.lightingSettings = Settings();

            GameObject instance = (GameObject)Object.Instantiate((Object)AssetDatabase.LoadAssetAtPath<GameObject>(path));
            instance.name = room;
            BuildShell();
            List<MeshRenderer> renderers = instance.GetComponentsInChildren<MeshRenderer>(true).ToList();
            List<MeshRenderer> stone = renderers.Where(IsStone).ToList();
            renderers.Except(stone).ToList().ForEach(MarkOccluder);
            stone.ForEach(MarkStone);
            instance.GetComponentsInChildren<Light>(true).Where(light => !IsRuntimeOnly(light)).ToList().ForEach(MarkLamp);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (!Lightmapping.Bake())
            {
                Debug.LogError($"{room}: bake failed");

                return;
            }

            Texture2D[] maps = Keep(room);
            List<int> indices = stone.Select(renderer => renderers.IndexOf(renderer)).ToList();
            Store(path, maps, indices, stone);
        }

        private static void SetAir()
        {
            RenderSettings.skybox = null;
            RenderSettings.sun = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.4f, 0.4f, 0.46f) * 0.025f;
            RenderSettings.ambientEquatorColor = new Color(0.32f, 0.31f, 0.33f) * 0.025f;
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.2f, 0.18f) * 0.025f;
        }

        private static LightingSettings Settings()
        {
            LightingSettings settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(SettingsPath);

            if (settings != null)
                return settings;

            settings = new LightingSettings
            {
                bakedGI = true,
                realtimeGI = false,
                lightmapper = LightingSettings.Lightmapper.ProgressiveGPU,
                directionalityMode = LightmapsMode.NonDirectional,
                lightmapResolution = TexelsPerMeter,
                lightmapMaxSize = 1024,
                lightmapPadding = 3,
                lightmapCompression = LightmapCompression.NormalQuality,
                maxBounces = 2,
                directSampleCount = 32,
                indirectSampleCount = 200,
                environmentSampleCount = 64,
                filteringMode = LightingSettings.FilterMode.Auto,
                ao = false
            };
            AssetDatabase.CreateAsset(settings, SettingsPath);

            return settings;
        }

        /// The cell's sides with both doorways open, so that light leaves by them and nothing comes in.
        private static void BuildShell()
        {
            GameObject side = AssetDatabase.LoadAssetAtPath<GameObject>($"{CatacombRoomBuilder.Folder}/{ShellSide}.prefab");
            Transform shell = new GameObject("Shell").transform;

            for (int i = 0; i < 4; i++)
            {
                Quaternion turn = Quaternion.Euler(0f, i * 90f, 0f);
                Object.Instantiate(side, turn * Vector3.forward * Half, turn, shell);
            }

            foreach (MeshRenderer renderer in shell.GetComponentsInChildren<MeshRenderer>(true))
                MarkOccluder(renderer);

            foreach (Light light in shell.GetComponentsInChildren<Light>(true))
                light.gameObject.SetActive(false);
        }

        private static bool IsStone(MeshRenderer renderer)
        {
            return renderer.enabled && renderer.gameObject.activeInHierarchy
                && (renderer.TryGetComponent(out KitBlockComponent _) || renderer.TryGetComponent(out KitStairsComponent _))
                && renderer.sharedMaterial != null && renderer.sharedMaterial.shader.name is "Universal Render Pipeline/Lit" or "Game/WorldStone"
                && renderer.sharedMaterial.renderQueue < (int)RenderQueue.Transparent
                && (!renderer.sharedMaterial.HasProperty("_EmissionColor") || renderer.sharedMaterial.GetColor("_EmissionColor").maxColorComponent <= 0f);
        }

        private static bool IsRuntimeOnly(Light light)
        {
            return light.GetComponentInParent<CatacombLampComponent>(true) != null;
        }

        private static void MarkStone(MeshRenderer renderer)
        {
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.ContributeGI);
            renderer.receiveGI = ReceiveGI.Lightmaps;
            Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;

            if (mesh.uv2.Length > 0 || !AssetDatabase.GetAssetPath(mesh).EndsWith(".asset"))
                return;

            Unwrapping.GenerateSecondaryUVSet(mesh);
            EditorUtility.SetDirty(mesh);
        }

        private static void MarkOccluder(MeshRenderer renderer)
        {
            bool isSolid = renderer.sharedMaterial != null && renderer.sharedMaterial.renderQueue < (int)RenderQueue.Transparent
                && renderer.GetComponent<MeshFilter>()?.sharedMesh != null;
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, isSolid ? StaticEditorFlags.ContributeGI : default);
            renderer.receiveGI = ReceiveGI.LightProbes;
        }

        private static void MarkLamp(Light light)
        {
            light.lightmapBakeType = LightmapBakeType.Baked;
            light.shadows = LightShadows.Soft;
            light.shapeRadius = 0.08f;
        }

        /// The maps the bake wrote are renamed for the room: the next bake empties the scene's folder.
        private static Texture2D[] Keep(string room)
        {
            LightmapData[] maps = LightmapSettings.lightmaps;
            Texture2D[] kept = new Texture2D[maps.Length];

            for (int i = 0; i < maps.Length; i++)
            {
                string source = AssetDatabase.GetAssetPath(maps[i].lightmapColor);
                string target = $"{Folder}/{room}-{i}.exr";
                AssetDatabase.DeleteAsset(target);
                AssetDatabase.CopyAsset(source, target);
                kept[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(target);
            }

            return kept;
        }

        private static void Store(string path, Texture2D[] maps, List<int> indices, List<MeshRenderer> stone)
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            List<MeshRenderer> renderers = prefab.GetComponentsInChildren<MeshRenderer>(true).ToList();
            List<RoomLightmapComponent.Entry> entries = new();

            for (int i = 0; i < indices.Count; i++)
            {
                if (stone[i].lightmapIndex < 0 || stone[i].lightmapIndex >= maps.Length)
                    continue;

                MeshRenderer renderer = renderers[indices[i]];
                renderer.renderingLayerMask = DungeonLightLayers.BakedStone;
                entries.Add(new RoomLightmapComponent.Entry { Renderer = renderer, Map = stone[i].lightmapIndex, ScaleOffset = stone[i].lightmapScaleOffset });
            }

            foreach (Light light in prefab.GetComponentsInChildren<Light>(true))
            {
                DungeonLightLayers.Set(light, DungeonLightLayers.Default);

                if (light.TryGetComponent(out FlickerLightComponent flicker))
                    Object.DestroyImmediate(flicker);
            }

            RoomLightmapComponent component = prefab.TryGetComponent(out RoomLightmapComponent found) ? found : prefab.AddComponent<RoomLightmapComponent>();
            component.Set(maps, entries.ToArray());
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }
}
