using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Editor.Battle
{
    /// Renders built clips on the character as the player sees them, fingers included (SwingPreview poses the rig and
    /// leaves the fingers flat): the shots to set beside first-person footage. Called from outside the editor while
    /// first-person clips are being authored (.claude/skills/video-to-animation).
    internal static class ClipPreview
    {
        private const int Layer = 31;
        private const int Width = 960;
        private const int Height = 540;

        /// Clips: names in the animations folder. Phases: moments as fractions of each clip, 0..1. Writes
        /// c_{clip}_{index}.png into the folder and returns the paths.
        public static string Run(string clips, string phases, string folder)
        {
            Directory.CreateDirectory(folder);
            StringBuilder written = new StringBuilder();
            GameObject model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BattleEditorUtility.ModelPath), Vector3.zero, Quaternion.identity);
            List<GameObject> stage = new List<GameObject> { model, new GameObject("Camera"), SwingPreview.CreateLight(1.3f, 160f), SwingPreview.CreateLight(0.7f, -40f) };
            Animator animator = model.GetComponentInChildren<Animator>();

            foreach (GameObject item in stage)
                item.hideFlags = HideFlags.HideAndDontSave;

            Camera camera = stage[1].AddComponent<Camera>();
            camera.fieldOfView = 75f;
            camera.nearClipPlane = 0.04f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.32f, 0.36f, 0.42f);
            camera.cullingMask = 1 << Layer;
            stage[2].layer = Layer;
            stage[3].layer = Layer;
            AnimationMode.StartAnimationMode();

            try
            {
                foreach (string name in Split(clips))
                {
                    AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{BattleEditorUtility.AnimationsFolder}/{name}.anim")
                                         ?? throw new ArgumentException($"No clip '{name}'");
                    string[] moments = Split(phases);

                    for (int i = 0; i < moments.Length; i++)
                    {
                        AnimationMode.BeginSampling();
                        AnimationMode.SampleAnimationClip(animator.gameObject, clip, float.Parse(moments[i], System.Globalization.CultureInfo.InvariantCulture) * clip.length);
                        AnimationMode.EndSampling();
                        animator.GetBoneTransform(HumanBodyBones.Head).localScale = Vector3.zero;
                        camera.transform.SetPositionAndRotation(animator.transform.TransformPoint(BattleAnimationLibrary.Eye), animator.transform.rotation);

                        // Skinning runs once per editor frame: the pose is baked into plain meshes to be seen right away.
                        List<GameObject> baked = Bake(model);
                        string path = $"{folder}/c_{name}_{i}.png";
                        SwingPreview.Render(camera, Width, Height, path);
                        written.AppendLine(path);

                        foreach (GameObject item in baked)
                        {
                            Object.DestroyImmediate(item.GetComponent<MeshFilter>().sharedMesh);
                            Object.DestroyImmediate(item);
                        }
                    }
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();

                foreach (GameObject item in stage)
                    Object.DestroyImmediate(item);
            }

            return written.ToString();
        }

        private static string[] Split(string list)
        {
            return list.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static List<GameObject> Bake(GameObject model)
        {
            List<GameObject> baked = new List<GameObject>();

            foreach (SkinnedMeshRenderer skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Mesh mesh = new Mesh();
                skin.BakeMesh(mesh);
                GameObject item = new GameObject("Baked") { hideFlags = HideFlags.HideAndDontSave, layer = Layer };
                item.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
                item.AddComponent<MeshFilter>().sharedMesh = mesh;
                item.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                baked.Add(item);
            }

            return baked;
        }
    }
}
