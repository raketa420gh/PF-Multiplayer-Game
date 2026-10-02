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
