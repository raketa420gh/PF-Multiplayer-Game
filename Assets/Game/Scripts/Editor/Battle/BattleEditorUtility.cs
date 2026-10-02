using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    internal static class BattleEditorUtility
    {
        public const string ModelPath = "Assets/SpecialFolder/character_rigged.fbx";
        public const string AnimationsFolder = "Assets/Game/Animations/Battle";
        public const string ConfigsFolder = "Assets/Game/Configs/Battle";
        public const string PrefabsFolder = "Assets/Game/Prefabs/Battle";
        public const string MaterialsFolder = "Assets/Game/Materials/Battle";
        public const string AudioFolder = "Assets/Game/Audio/Battle";
        public const string ScenePath = "Assets/Game/Scenes/BattleScene.unity";
        public const string ControllerPath = AnimationsFolder + "/Fighter.controller";
        public const string HitboxLayer = "Hitbox";
        public const string CharacterLayer = "Character";

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
