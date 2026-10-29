using System.Globalization;
using Game.Scripts.Dungeon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Editor side of the catacomb level kit. A kit block or flight scaled with the Scale tool or given a new size gets the
    /// scale turned into its size and a mesh and colliders of that size, so textures keep their scale. A room prefab saved in
    /// prefab mode gets its map re-rendered.
    [InitializeOnLoad]
    internal static class CatacombKit
    {
        private const float StepHeight = 0.25f;

        static CatacombKit()
        {
            ObjectChangeEvents.changesPublished += OnChangesPublished;
            PrefabStage.prefabSaved += OnPrefabSaved;
        }

        [MenuItem("Tools/Game/Dungeon/Catacombs/Refresh Room Maps")]
        public static void RefreshMaps()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { CatacombRoomBuilder.Folder }))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));

                if (prefab.TryGetComponent(out CatacombRoomComponent _))
                    RefreshMap(prefab);
            }
        }

        [MenuItem("Tools/Game/Dungeon/Catacombs/Rebuild Rooms And Kit From Layouts (overwrites edits)")]
        public static void RebuildFromLayouts()
        {
            if (!EditorUtility.DisplayDialog("Catacombs", "Rooms, the Great Hall and kit prefabs are rebuilt from CatacombRoomLayouts: your edits to them are lost.", "Rebuild", "Cancel"))
                return;

            CatacombRoomBuilder.IsOverwriting = true;

            try
            {
                DungeonBuildMenu.BuildScene();
            }
            finally
            {
                CatacombRoomBuilder.IsOverwriting = false;
            }
        }

        /// Renders the room alone, far below the scene, into its map and points the prefab at it.
        public static void RefreshMap(GameObject prefab)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = CatacombRoomBuilder.Workshop;
            Texture2D map = prefab.name == CatacombRoomBuilder.HallName ? CatacombRoomBuilder.RenderHallMap(instance.transform)
                : DungeonMinimapBuilder.Render(instance.transform, CatacombRoomBuilder.Workshop, DungeonMapBuilder.Pitch * 0.5f, CatacombRoomBuilder.MapPixels, $"Catacomb{prefab.name}", true);
            Object.DestroyImmediate(instance);
            SerializedObject so = new SerializedObject(prefab.GetComponent<CatacombRoomComponent>());

            if (so.FindProperty("_map").objectReferenceValue == map)
                return;

            so.FindProperty("_map").objectReferenceValue = map;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        public static void Apply(KitBlockComponent block)
        {
            Vector3 size = Vector3.Max(Vector3.Scale(block.Size, block.transform.localScale), Vector3.one * 0.01f);
            string key = $"Kit{(block.IsRound ? "Round" : "Box")}_{Format(size)}";
            MeshFilter filter = block.GetComponent<MeshFilter>();
            BoxCollider collider = block.GetComponent<BoxCollider>();

            if (block.transform.localScale == Vector3.one && filter.sharedMesh != null && filter.sharedMesh.name == key)
                return;

            Undo.RecordObjects(new Object[] { block, block.transform, filter, collider != null ? collider : block }, "Resize kit block");
            block.SetSize(size);
            block.transform.localScale = Vector3.one;
            filter.sharedMesh = Load(key) ?? (block.IsRound
                ? new DungeonMeshBuilder(0.5f).Cylinder(Vector3.up * size.y * 0.5f, size.x * 0.5f, size.y, 16)
                : new DungeonMeshBuilder(0.5f).Box(Vector3.up * size.y * 0.5f, size)).Save(key);

            if (collider != null)
            {
                collider.center = Vector3.up * size.y * 0.5f;
                collider.size = size;
            }
        }

        /// Steps to see; a convex ramp to walk (first mesh collider) and the steps again to fill the space under it (second one),
        /// so the NavMesh finds no floor below the flight.
        public static void Apply(KitStairsComponent stairs)
        {
            Vector3 size = Vector3.Max(Vector3.Scale(stairs.Size, stairs.transform.localScale), Vector3.one * 0.1f);
            string key = $"KitStairs_{Format(size)}";
            MeshFilter filter = stairs.GetComponent<MeshFilter>();
            MeshCollider[] colliders = stairs.GetComponents<MeshCollider>();

            if (stairs.transform.localScale == Vector3.one && filter.sharedMesh != null && filter.sharedMesh.name == key && colliders.Length == 2)
                return;

            Undo.RecordObjects(new Object[] { stairs, stairs.transform, filter }, "Resize kit stairs");
            stairs.SetSize(size);
            stairs.transform.localScale = Vector3.one;

            for (int i = colliders.Length; i < 2; i++)
                Undo.AddComponent<MeshCollider>(stairs.gameObject);

            colliders = stairs.GetComponents<MeshCollider>();
            Undo.RecordObjects(colliders, "Resize kit stairs");
            int steps = Mathf.Max(2, Mathf.RoundToInt(size.y / StepHeight));
            float run = size.z / steps;
            DungeonMeshBuilder visual = new DungeonMeshBuilder(0.5f);

            for (int i = 0; i < steps; i++)
            {
                float height = size.y * (i + 1) / steps;
                visual.Box(new Vector3(0f, height * 0.5f, -size.z * 0.5f + (i + 0.5f) * run), new Vector3(size.x, height, run));
            }

            filter.sharedMesh = Load(key) ?? visual.Save(key);
            colliders[0].sharedMesh = Load(key + "_Ramp") ?? new DungeonMeshBuilder(0.5f).Prism(size.x, new Vector2(-size.z * 0.5f, 0f), new Vector2(-size.z * 0.5f, size.y / steps * 0.5f),
                new Vector2(size.z * 0.5f - run * 0.5f, size.y), new Vector2(size.z * 0.5f, size.y), new Vector2(size.z * 0.5f, 0f)).Save(key + "_Ramp");
            colliders[0].convex = true;
            DungeonMeshBuilder fill = new DungeonMeshBuilder(0.5f);

            for (int i = 0; i < steps; i++)
                fill.Box(new Vector3(0f, (i + 0.5f) * size.y / steps * 0.5f, -size.z * 0.5f + (i + 0.5f) * run), new Vector3(size.x, (i + 0.5f) * size.y / steps, run));

            colliders[1].sharedMesh = Load(key + "_Fill") ?? fill.Save(key + "_Fill");
            colliders[1].convex = false;
        }

        private static Mesh Load(string key)
        {
            return AssetDatabase.LoadAssetAtPath<Mesh>($"{DungeonMeshBuilder.Folder}/{key}.asset");
        }

        private static string Format(Vector3 size)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.##}x{1:0.##}x{2:0.##}", size.x, size.y, size.z);
        }

        private static void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; i++)
            {
                if (stream.GetEventType(i) != ObjectChangeKind.ChangeGameObjectOrComponentProperties)
                    continue;

                stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out ChangeGameObjectOrComponentPropertiesEventArgs change);
                Object target = EditorUtility.EntityIdToObject(change.entityId);
                GameObject owner = target is Component component ? component.gameObject : target as GameObject;

                if (owner == null || EditorUtility.IsPersistent(owner))
                    continue;

                if (owner.TryGetComponent(out KitBlockComponent block))
                    Apply(block);

                if (owner.TryGetComponent(out KitStairsComponent stairs))
                    Apply(stairs);
            }
        }

        private static void OnPrefabSaved(GameObject root)
        {
            string path = PrefabStageUtility.GetCurrentPrefabStage()?.assetPath;

            if (!root.TryGetComponent(out CatacombRoomComponent _) || string.IsNullOrEmpty(path))
                return;

            EditorApplication.delayCall += () => RefreshMap(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        }
    }
}
