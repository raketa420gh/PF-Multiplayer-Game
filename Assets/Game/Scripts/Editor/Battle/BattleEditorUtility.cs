using System.Collections;
using System.IO;
using Game.Scripts.Battle;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace Game.Scripts.Editor.Battle
{
    internal static class BattleEditorUtility
    {
        public const string ModelPath = PrefabsFolder + "/Character.prefab";
        public const string ModelsFolder = "Assets/Game/Meshes/Battle";
        public const string AnimationsFolder = "Assets/Game/Animations/Battle";
        public const string ConfigsFolder = "Assets/Game/Configs/Battle";
        public const string PrefabsFolder = "Assets/Game/Prefabs/Battle";
        public const string MaterialsFolder = "Assets/Game/Materials/Battle";
        public const string AudioFolder = "Assets/Game/Audio/Battle";
        public const string ScenePath = "Assets/Game/Scenes/BattleScene.unity";
        public const string ControllerPath = AnimationsFolder + "/Fighter.controller";
        public const string HitboxLayer = "Hitbox";
        public const string CharacterLayer = "Character";
        public const string BlockerLayer = "BodyBlocker";
        public const float BlockerRadius = 0.4f;

        /// Crosshair arrow (points along +X, rotated by the view) and a thrust dot, centered on the screen.
        public static SwingHintView CreateSwingHint(Transform canvas, BattleContext context)
        {
            RectTransform root = CreateUiRect("SwingHint", canvas, Vector2.zero);
            HudPanelView arrow = CreateUiRect("Arrow", root, Vector2.zero).gameObject.AddComponent<HudPanelView>();
            Color color = new(1f, 0.9f, 0.55f, 0.9f);
            CreateUiBar(arrow.transform, new Vector2(9f, 0f), new Vector2(20f, 2f), 0f, color);
            CreateUiBar(arrow.transform, new Vector2(29f, 0f), new Vector2(8f, 2f), 150f, color);
            CreateUiBar(arrow.transform, new Vector2(29f, 0f), new Vector2(8f, 2f), -150f, color);
            HudPanelView thrust = CreateUiRect("Thrust", root, Vector2.zero).gameObject.AddComponent<HudPanelView>();
            CreateUiBar(thrust.transform, new Vector2(-6f, 0f), new Vector2(12f, 2f), 0f, color);
            CreateUiBar(thrust.transform, new Vector2(0f, -6f), new Vector2(12f, 2f), 90f, color);

            SwingHintView view = root.gameObject.AddComponent<SwingHintView>();
            SerializedObject so = new SerializedObject(view);
            Set(so, "_context", context);
            Set(so, "_arrow", arrow);
            Set(so, "_thrust", thrust);
            so.ApplyModifiedPropertiesWithoutUndo();
            arrow.gameObject.SetActive(false);
            thrust.gameObject.SetActive(false);

            return view;
        }

        private static RectTransform CreateUiRect(string name, Transform parent, Vector2 position)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = Vector2.zero;

            return rect;
        }

        /// Bar starting at its pivot (left edge), so rotating it swings around the start point.
        private static void CreateUiBar(Transform parent, Vector2 position, Vector2 size, float angle, Color color)
        {
            RectTransform rect = CreateUiRect("Bar", parent, position);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        /// Poses an animator with one moment of a clip outside play mode, humanoid retargeting included.
        public static void SampleClip(Animator animator, AnimationClip clip, float time)
        {
            PlayableGraph graph = PlayableGraph.Create();
            AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
            // Only imported clips carry foot IK goals; the generated ones would pull the feet to the origin.
            playable.SetApplyFootIK(AssetDatabase.IsSubAsset(clip));
            AnimationPlayableOutput.Create(graph, "Pose", animator).SetSourcePlayable(playable);
            playable.SetTime(time);
            graph.Evaluate(0f);
            graph.Destroy();
        }

        /// Finds a clip of the imported animation libraries by its take name.
        public static AnimationClip LoadLibraryClip(string name)
        {
            foreach (string path in new[] { BattleCharacterBuilder.AnimationsPath1, BattleCharacterBuilder.AnimationsPath2 })
            {
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is AnimationClip clip && clip.name == name)
                        return clip;
                }
            }

            throw new System.ArgumentException($"Animation library clip '{name}' not found");
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);

            return asset;
        }

        public static Material GetMaterial(string name, Color color, float metallic = 0f, float smoothness = 0.3f)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);

            return material;
        }

        public static Material GetUnlitMaterial(string name, Color color)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);

            return material;
        }

        public static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 position,
            Vector3 euler, Vector3 scale, Material material, bool keepCollider = false)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;

            if (!keepCollider)
                Object.DestroyImmediate(go.GetComponent<Collider>());

            return go;
        }

        public static GameObject CreateChild(string name, Transform parent, Vector3 position = default)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            return go;
        }

        public static void SetLayerRecursively(GameObject go, int layer)
        {
            foreach (Transform child in go.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;
        }

        public static int EnsureLayer(string name)
        {
            int layer = LayerMask.NameToLayer(name);

            if (layer >= 0)
                return layer;

            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            for (int i = 8; i < layers.arraySize; i++)
            {
                SerializedProperty element = layers.GetArrayElementAtIndex(i);

                if (!string.IsNullOrEmpty(element.stringValue))
                    continue;

                element.stringValue = name;
                tagManager.ApplyModifiedPropertiesWithoutUndo();

                return i;
            }

            throw new System.InvalidOperationException("No free layer slot");
        }

        /// Layer of the body capsules: it collides with the character controllers and nothing else.
        public static int EnsureBlockerLayer()
        {
            int layer = EnsureLayer(BlockerLayer);
            int character = LayerMask.NameToLayer(CharacterLayer);

            for (int i = 0; i < 32; i++)
                Physics.IgnoreLayerCollision(layer, i, i != character);

            return layer;
        }

        /// Capsule that keeps other characters at arm's length; no attack, sight or interaction mask includes its layer.
        public static CapsuleCollider CreateBlocker(Transform parent, float radius, float height)
        {
            GameObject blocker = CreateChild("BodyBlocker", parent);
            blocker.layer = EnsureBlockerLayer();
            CapsuleCollider capsule = blocker.AddComponent<CapsuleCollider>();
            capsule.radius = radius;
            capsule.height = height;
            capsule.center = new Vector3(0f, height * 0.5f, 0f);

            return capsule;
        }

        public static void Set(Object target, string path, object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            Assign(Find(serializedObject, path), value);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Set(SerializedObject serializedObject, string path, object value)
        {
            Assign(Find(serializedObject, path), value);
        }

        private static SerializedProperty Find(SerializedObject serializedObject, string path)
        {
            SerializedProperty property = serializedObject.FindProperty(path);

            if (property == null)
                throw new System.ArgumentException($"Property '{path}' not found on {serializedObject.targetObject}");

            return property;
        }

        private static void Assign(SerializedProperty property, object value)
        {
            switch (value)
            {
                case float number:
                    property.floatValue = number;
                    break;
                case int number when property.propertyType == SerializedPropertyType.Float:
                    property.floatValue = number;
                    break;
                case int number:
                    property.intValue = number;
                    break;
                case byte number:
                    property.intValue = number;
                    break;
                case short number:
                    property.intValue = number;
                    break;
                case bool flag:
                    property.boolValue = flag;
                    break;
                case string text:
                    property.stringValue = text;
                    break;
                case Vector2 vector:
                    property.vector2Value = vector;
                    break;
                case Vector3 vector:
                    property.vector3Value = vector;
                    break;
                case Color color:
                    property.colorValue = color;
                    break;
                case LayerMask mask:
                    property.intValue = mask.value;
                    break;
                case System.Enum enumValue:
                    property.intValue = System.Convert.ToInt32(enumValue);
                    break;
                case Object reference:
                    property.objectReferenceValue = reference;
                    break;
                case null:
                    property.objectReferenceValue = null;
                    break;
                case IList list:
                    property.arraySize = list.Count;

                    for (int i = 0; i < list.Count; i++)
                        Assign(property.GetArrayElementAtIndex(i), list[i]);
                    break;
                default:
                    throw new System.ArgumentException($"Unsupported value type {value.GetType()} for {property.propertyPath}");
            }
        }
    }
}
