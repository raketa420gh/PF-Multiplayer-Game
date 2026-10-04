using System.IO;
using Game.Scripts.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// Renders the fighter prefab frozen at a given animator state/time to a PNG for tuning generated poses.
    internal static class BattlePosePreview
    {
        public const string OutputFolder = "Temp/BattlePreview";

        public static string Capture(string state, float normalizedTime, int slot, string view, float pitch = 0f, float crouch = 0f)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BattleContentBuilder.FighterPath);
            GameObject fighter = Object.Instantiate(prefab, new Vector3(0f, 200f, 0f), Quaternion.identity);
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(0f, 199.95f, 0f);
            floor.transform.localScale = new Vector3(8f, 0.1f, 8f);
            GameObject cameraObject = new GameObject("PreviewCamera");

            try
            {
                Animator animator = fighter.GetComponentInChildren<Animator>();
                animator.Rebind();
                animator.SetFloat(FighterAnimComponent.CrouchParam, crouch);
                animator.Play(state, 1, Mathf.Clamp(normalizedTime, 0f, 0.999f));
                animator.Update(0f);

                SerializedObject combat = new SerializedObject(fighter.GetComponent<CombatComponent>());
                WeaponConfig weapon = (WeaponConfig)combat.FindProperty("_loadout").GetArrayElementAtIndex(slot).objectReferenceValue;
                SerializedProperty sockets = new SerializedObject(fighter.GetComponent<WeaponViewComponent>()).FindProperty("_sockets");

                foreach (WeaponAttachment attachment in weapon.Attachments)
                {
                    Transform socket = (Transform)sockets.GetArrayElementAtIndex((int)attachment.Socket).objectReferenceValue;
                    Object.Instantiate(attachment.Prefab, socket, false);
                }

                Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
                Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
                Transform upperChest = animator.GetBoneTransform(HumanBodyBones.UpperChest);
                Quaternion step = Quaternion.AngleAxis(pitch / 3f, Vector3.right);

                foreach (Transform bone in new[] { spine, chest, upperChest })
                    bone.rotation = step * bone.rotation;

                BodyConfig body = AssetDatabase.LoadAssetAtPath<BodyConfig>($"{BattleEditorUtility.ConfigsFolder}/Body.asset");
                Vector3 origin = fighter.transform.position;
                Vector3 eye = origin + body.TransformPoint(body.EyePoint, pitch, crouch);

                Camera camera = cameraObject.AddComponent<Camera>();
                camera.nearClipPlane = 0.04f;
                camera.fieldOfView = 75f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.35f, 0.45f, 0.6f);

                switch (view)
                {
                    case "fp":
                        animator.GetBoneTransform(HumanBodyBones.Head).localScale = Vector3.zero;
                        camera.transform.SetPositionAndRotation(eye, Quaternion.Euler(pitch, 0f, 0f));
                        break;
                    case "front":
                        LookFrom(camera, origin + new Vector3(0f, 1.4f, 3f), origin + Vector3.up * 1.2f);
                        break;
                    case "side":
                        LookFrom(camera, origin + new Vector3(3f, 1.4f, 0.3f), origin + new Vector3(0f, 1.2f, 0.3f));
                        break;
                    case "back":
                        LookFrom(camera, origin + new Vector3(0.8f, 2.2f, -2.6f), origin + new Vector3(0f, 1.3f, 0.4f));
                        break;
                    default:
                        LookFrom(camera, origin + new Vector3(0f, 4.5f, 0.4f), origin + new Vector3(0f, 1f, 0.4f));
                        break;
                }

                return Render(camera, $"{state}_{Mathf.RoundToInt(normalizedTime * 100f)}_{view}");
            }
            finally
            {
                Object.DestroyImmediate(fighter);
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(cameraObject);
            }
        }

        /// Tiles several moments of one state (2 rows) into a single contact sheet.
        public static string CaptureSheet(string state, int slot, string view, int frames, float from = 0f, float to = 1f,
            float pitch = 0f, float crouch = 0f)
        {
            int columns = Mathf.CeilToInt(frames / 2f);
            Texture2D sheet = new Texture2D(columns * 640, 720, TextureFormat.RGB24, false);

            for (int i = 0; i < frames; i++)
            {
                string path = Capture(state, Mathf.Lerp(from, to, i / (frames - 1f)), slot, view, pitch, crouch);
                Texture2D frame = new Texture2D(2, 2);
                frame.LoadImage(File.ReadAllBytes(path));
                sheet.SetPixels(i % columns * 640, (1 - i / columns) * 360, 640, 360, frame.GetPixels());
                Object.DestroyImmediate(frame);
            }

            string sheetPath = $"{OutputFolder}/{state}_{view}_sheet.png";
            File.WriteAllBytes(sheetPath, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);

            return sheetPath;
        }

        /// Renders any prefab from the front, side and back into one sheet with its own lights; setup poses or dresses the instance.
        public static string CaptureTurnaround(GameObject prefab, string name, System.Action<GameObject> setup = null, float distance = 3.4f, float height = 0.95f)
        {
            Vector3[] views = { new(0f, 0.25f, 1f), new(0.94f, 0.25f, 0.35f), new(0f, 0.25f, -1f) };

            return CaptureFrames(prefab, name, views.Length, (_, i) => views[i], setup, distance, height);
        }

        /// Contact sheet of one clip sampled on the prefab's animator at evenly spaced moments.
        public static string CaptureClip(GameObject prefab, AnimationClip clip, int frames, System.Action<GameObject> setup = null, float side = 0.6f)
        {
            return CaptureFrames(prefab, clip.name, frames, (instance, i) =>
            {
                BattleEditorUtility.SampleClip(instance.GetComponentInChildren<Animator>(), clip, clip.length * i / Mathf.Max(1, frames - 1));

                return new Vector3(side, 0.25f, 0.8f);
            }, setup, 3.8f, 0.95f);
        }

        /// Stands props side by side with their +Z up and renders each into its own cell, turned by the given euler.
        public static string CaptureProps(string name, GameObject[] prefabs, Vector3 euler, int cellWidth = 220, int cellHeight = 880)
        {
            Vector3 origin = new Vector3(0f, 400f, 0f);
            GameObject cameraObject = new GameObject("PreviewCamera");
            GameObject[] lights = { new GameObject("PreviewLight"), new GameObject("PreviewLight") };
            Texture2D sheet = new Texture2D(cellWidth * prefabs.Length, cellHeight, TextureFormat.RGB24, false);
            bool asyncCompile = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;

            try
            {
                for (int i = 0; i < lights.Length; i++)
                {
                    Light light = lights[i].AddComponent<Light>();
                    light.type = LightType.Directional;
                    light.intensity = i == 0 ? 1.4f : 0.6f;
                    light.transform.rotation = Quaternion.Euler(35f, 150f + i * 150f, 0f);
                }

                Camera camera = cameraObject.AddComponent<Camera>();
                camera.orthographic = true;
                camera.nearClipPlane = 0.05f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.3f, 0.33f, 0.38f);

                for (int i = 0; i < prefabs.Length; i++)
                {
                    GameObject instance = Object.Instantiate(prefabs[i], origin, Quaternion.Euler(euler) * Quaternion.Euler(-90f, 0f, 0f));
                    Bounds bounds = new Bounds(origin, Vector3.zero);

                    foreach (MeshRenderer renderer in instance.GetComponentsInChildren<MeshRenderer>())
                        bounds.Encapsulate(renderer.bounds);

                    camera.orthographicSize = Mathf.Max(bounds.extents.y, bounds.extents.x * cellHeight / cellWidth) * 1.06f;
                    camera.transform.position = bounds.center + Vector3.back * 5f;
                    camera.transform.rotation = Quaternion.identity;
                    RenderTexture texture = RenderTexture.GetTemporary(cellWidth, cellHeight, 24);
                    camera.targetTexture = texture;
                    camera.Render();
                    RenderTexture.active = texture;
                    Texture2D frame = new Texture2D(cellWidth, cellHeight, TextureFormat.RGB24, false);
                    frame.ReadPixels(new Rect(0f, 0f, cellWidth, cellHeight), 0, 0);
                    sheet.SetPixels(i * cellWidth, 0, cellWidth, cellHeight, frame.GetPixels());
                    RenderTexture.active = null;
                    camera.targetTexture = null;
                    RenderTexture.ReleaseTemporary(texture);
                    Object.DestroyImmediate(frame);
                    Object.DestroyImmediate(instance);
                }

                Directory.CreateDirectory(OutputFolder);
                string path = $"{OutputFolder}/{name}.png";
                File.WriteAllBytes(path, sheet.EncodeToPNG());

                return path;
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = asyncCompile;
                Object.DestroyImmediate(sheet);
                Object.DestroyImmediate(cameraObject);

                foreach (GameObject light in lights)
                    Object.DestroyImmediate(light);
            }
        }

        private static string CaptureFrames(GameObject prefab, string name, int count, System.Func<GameObject, int, Vector3> pose,
            System.Action<GameObject> setup, float distance, float height)
        {
            int width = Mathf.Min(640, 2400 / count);
            Vector3 origin = new Vector3(0f, 300f, 0f);
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = origin + Vector3.down * 0.05f;
            floor.transform.localScale = new Vector3(6f, 0.1f, 6f);
            GameObject cameraObject = new GameObject("PreviewCamera");
            GameObject[] lights = { new GameObject("PreviewLight"), new GameObject("PreviewLight") };
            Texture2D sheet = new Texture2D(width * count, 640, TextureFormat.RGB24, false);
            bool asyncCompile = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;

            try
            {
                for (int i = 0; i < lights.Length; i++)
                {
                    Light light = lights[i].AddComponent<Light>();
                    light.type = LightType.Directional;
                    light.intensity = 0.9f;
                    light.transform.rotation = Quaternion.Euler(40f, 30f + i * 180f, 0f);
                }

                Camera camera = cameraObject.AddComponent<Camera>();
                camera.nearClipPlane = 0.05f;
                camera.fieldOfView = 40f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.35f, 0.45f, 0.6f);

                for (int i = 0; i < count; i++)
                {
                    // Skinning is evaluated once per editor frame, so every shot gets a fresh instance.
                    GameObject instance = Object.Instantiate(prefab, origin, Quaternion.identity);
                    setup?.Invoke(instance);
                    camera.transform.position = origin + Vector3.up * height + pose(instance, i).normalized * distance;
                    camera.transform.LookAt(origin + Vector3.up * height);
                    RenderTexture texture = RenderTexture.GetTemporary(width, 640, 24);
                    camera.targetTexture = texture;
                    camera.Render();
                    RenderTexture.active = texture;
                    Texture2D frame = new Texture2D(width, 640, TextureFormat.RGB24, false);
                    frame.ReadPixels(new Rect(0f, 0f, width, 640f), 0, 0);
                    sheet.SetPixels(i * width, 0, width, 640, frame.GetPixels());
                    RenderTexture.active = null;
                    camera.targetTexture = null;
                    RenderTexture.ReleaseTemporary(texture);
                    Object.DestroyImmediate(frame);
                    Object.DestroyImmediate(instance);
                }

                Directory.CreateDirectory(OutputFolder);
                string path = $"{OutputFolder}/{name}.png";
                File.WriteAllBytes(path, sheet.EncodeToPNG());

                return path;
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = asyncCompile;
                Object.DestroyImmediate(sheet);
                Object.DestroyImmediate(floor);
                Object.DestroyImmediate(cameraObject);

                foreach (GameObject light in lights)
                    Object.DestroyImmediate(light);
            }
        }

        private static void LookFrom(Camera camera, Vector3 position, Vector3 target)
        {
            camera.fieldOfView = 45f;
            camera.transform.position = position;
            camera.transform.LookAt(target);
        }

        private static string Render(Camera camera, string name)
        {
            RenderTexture texture = RenderTexture.GetTemporary(640, 360, 24);
            camera.targetTexture = texture;
            camera.Render();

            RenderTexture.active = texture;
            Texture2D image = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(texture);

            Directory.CreateDirectory(OutputFolder);
            string path = $"{OutputFolder}/{name}.png";
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.DestroyImmediate(image);

            return path;
        }
    }
}
