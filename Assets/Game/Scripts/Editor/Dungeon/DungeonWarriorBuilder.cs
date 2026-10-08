using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// The Zombie Warrior pack as the body of the skeleton warrior. The pack is static meshes in an A-pose: they are merged into one mesh,
    /// skinned onto a humanoid skeleton placed at the pack's joints and shown over the fighter rig like the other skeletons.
    internal static class DungeonWarriorBuilder
    {
        private const string PackFolder = "Assets/SpecialFolder/Models/Monsters/ZombieWarrior";
        private const string MeshPath = DungeonMeshBuilder.Folder + "/ZombieWarrior.asset";
        private const string AvatarPath = DungeonMeshBuilder.Folder + "/ZombieWarriorAvatar.asset";
        private const string TexturesFolder = "Assets/Game/Textures/Dungeon/ZombieWarrior";
        private const string MaterialsFolder = DungeonPropBuilder.MaterialsFolder + "/ZombieWarrior";
        private const int MapSize = 2048;
        // Pieces no larger than this ride one bone whole: plates, the helmet, the hands, the arrows in the body.
        private const float RigidSize = 0.3f;

        private static readonly string[] s_parts = { "Body", "Head", "Armor", "Weapon" };
        // The pack's sword, its shield and the arrows stuck in the shield: the warrior holds the catalog sword and écu instead.
        private static readonly HashSet<string> s_loose = new()
        {
            "weapon2", "weapon4", "weapon6", "weapon20", "weapon24", "weapon26", "weapon28",
            "polySurface7", "polySurface8", "polySurface24", "polySurface25", "polySurface26"
        };
        // Whatever hangs off the belt stays rigid on the hips however long it is.
        private static readonly HashSet<string> s_belt = new() { "weapon8", "weapon10", "weapon12", "weapon14", "weapon16", "weapon18" };

        /// Joints measured on the pack's A-pose, right side; the left side mirrors x. Tips end the last bone of a chain.
        private static readonly (string bone, string parent, Vector3 position)[] s_joints =
        {
            ("Hips", null, new(0f, 0.92f, -0.05f)),
            ("Spine", "Hips", new(0f, 1.04f, -0.06f)),
            ("Chest", "Spine", new(0f, 1.2f, -0.07f)),
            ("Neck", "Chest", new(0f, 1.42f, -0.08f)),
            ("Head", "Neck", new(0f, 1.52f, -0.04f)),
            ("Shoulder", "Chest", new(0.05f, 1.38f, -0.08f)),
            ("UpperArm", "Shoulder", new(0.17f, 1.33f, -0.09f)),
            ("LowerArm", "UpperArm", new(0.34f, 1.18f, -0.09f)),
            ("Hand", "LowerArm", new(0.5f, 1.03f, -0.02f)),
            ("UpperLeg", "Hips", new(0.1f, 0.88f, -0.05f)),
            ("LowerLeg", "UpperLeg", new(0.155f, 0.5f, -0.03f)),
            ("Foot", "LowerLeg", new(0.2f, 0.1f, -0.05f)),
            ("Toes", "Foot", new(0.21f, 0.03f, 0.11f))
        };

        private static readonly Dictionary<string, Vector3> s_tips = new()
        {
            ["Head"] = new(0f, 1.74f, -0.02f), ["Hand"] = new(0.64f, 0.85f, 0.1f), ["Toes"] = new(0.21f, 0.02f, 0.19f)
        };

        private static readonly HashSet<string> s_arm = new() { "UpperArm", "LowerArm", "Hand" };
        private static readonly HashSet<string> s_leg = new() { "UpperLeg", "LowerLeg" };

        /// Puts the warrior over the rig and hides the rig's own body; weapons and hitboxes keep riding the rig.
        public static Transform Attach(GameObject root, Animator rig)
        {
            List<Bone> bones = CreateBones();
            GameObject model = new GameObject("ZombieWarrior");
            Transform[] transforms = new Transform[bones.Count];

            for (int i = 0; i < bones.Count; i++)
            {
                Bone bone = bones[i];
                Transform parent = bone.Parent < 0 ? model.transform : transforms[bone.Parent];
                transforms[i] = BattleEditorUtility.CreateChild(bone.Name, parent, bone.Pose - (bone.Parent < 0 ? Vector3.zero : bones[bone.Parent].Pose)).transform;
            }

            Avatar avatar = BuildAvatar(model, bones);
            SkinnedMeshRenderer renderer = BattleEditorUtility.CreateChild("Mesh", model.transform).AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = BuildMesh(bones);
            renderer.bones = transforms;
            renderer.rootBone = transforms[0];
            renderer.sharedMaterials = s_parts.Select(Material).ToArray();
            renderer.updateWhenOffscreen = true;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            DungeonSkeletonBuilder.Retarget(root, rig, model, avatar);

            return model.transform;
        }

        private sealed class Bone
        {
            public string Name;
            public int Parent;
            public int Side;
            /// Where the joint sits on the pack's A-pose and on the straightened T-pose the avatar is described in.
            public Vector3 Bind;
            public Vector3 Pose;
            public Vector3 BindEnd;
            public Quaternion BindRotation = Quaternion.identity;
        }

        /// Arms straighten out sideways and legs straight down for the avatar; every bone of those chains is turned from its T-pose
        /// direction onto the pack's to bind.
        private static List<Bone> CreateBones()
        {
            List<Bone> bones = new();
            Dictionary<string, Vector3> tips = new();

            foreach ((string name, string parent, Vector3 position) in s_joints)
            {
                foreach (int side in name is "Hips" or "Spine" or "Chest" or "Neck" or "Head" ? new[] { 0 } : new[] { -1, 1 })
                {
                    string prefix = side < 0 ? "Left" : side > 0 ? "Right" : "";
                    string parentName = parent == null ? null : s_joints.First(joint => joint.bone == parent).position.x == 0f ? parent : prefix + parent;
                    Bone bone = new() { Name = prefix + name, Side = side, Bind = Mirror(position, side), Parent = bones.FindIndex(b => b.Name == parentName) };
                    bone.Pose = bone.Parent < 0 ? bone.Bind : bones[bone.Parent].Pose + Straighten(parent, bone.Bind - bones[bone.Parent].Bind, side);
                    bones.Add(bone);

                    if (s_tips.TryGetValue(name, out Vector3 tip))
                        tips[bone.Name] = Mirror(tip, side);
                }
            }

            foreach (Bone bone in bones)
            {
                Bone child = bones.Find(b => b.Parent >= 0 && bones[b.Parent] == bone && (b.Side == bone.Side || bone.Side == 0) && !b.Name.EndsWith("Shoulder") && !b.Name.EndsWith("UpperLeg"));
                string chain = bone.Name.Replace("Left", "").Replace("Right", "");
                bone.BindEnd = tips.TryGetValue(bone.Name, out Vector3 tip) ? tip : child.Bind;
                Vector3 poseEnd = child != null ? child.Pose : bone.Pose + Straighten(chain, bone.BindEnd - bone.Bind, bone.Side);

                if (s_arm.Contains(chain) || s_leg.Contains(chain))
                    bone.BindRotation = Quaternion.FromToRotation(poseEnd - bone.Pose, bone.BindEnd - bone.Bind);
            }

            return bones;
        }

        private static Vector3 Mirror(Vector3 position, int side) => side < 0 ? new Vector3(-position.x, position.y, position.z) : position;

        /// The offset of a joint from its parent with the parent's chain straightened: along the arm sideways, along the leg down.
        private static Vector3 Straighten(string parent, Vector3 offset, int side)
        {
            if (parent != null && s_arm.Contains(parent))
                return new Vector3(side * offset.magnitude, 0f, 0f);

            return parent != null && s_leg.Contains(parent) ? Vector3.down * offset.magnitude : offset;
        }

        private static Avatar BuildAvatar(GameObject model, List<Bone> bones)
        {
            HumanDescription description = new()
            {
                human = bones.Select(bone => new HumanBone { boneName = bone.Name, humanName = bone.Name, limit = new HumanLimit { useDefaultValues = true } }).ToArray(),
                skeleton = model.GetComponentsInChildren<Transform>().Select(t => new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale }).ToArray(),
                upperArmTwist = 0.5f, lowerArmTwist = 0.5f, upperLegTwist = 0.5f, lowerLegTwist = 0.5f, armStretch = 0.05f, legStretch = 0.05f, feetSpacing = 0f
            };

            Avatar avatar = AvatarBuilder.BuildHumanAvatar(model, description);

            if (!avatar.isValid || !avatar.isHuman)
                throw new System.InvalidOperationException("Zombie warrior avatar is not a valid humanoid");

            avatar.name = "ZombieWarriorAvatar";
            Avatar existing = AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath);

            if (existing == null)
            {
                AssetDatabase.CreateAsset(avatar, AvatarPath);

                return avatar;
            }

            EditorUtility.CopySerialized(avatar, existing);
            Object.DestroyImmediate(avatar);
            EditorUtility.SetDirty(existing);

            return existing;
        }

        /// Every worn piece of the pack in one mesh, a submesh per texture set, skinned on the A-pose.
        private static Mesh BuildMesh(List<Bone> bones)
        {
            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<Vector2> uvs = new();
            List<BoneWeight> weights = new();
            List<int>[] triangles = s_parts.Select(_ => new List<int>()).ToArray();

            for (int part = 0; part < s_parts.Length; part++)
            {
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>($"{PackFolder}/Meshes/{s_parts[part]}.fbx");

                foreach (MeshFilter filter in source.GetComponentsInChildren<MeshFilter>())
                {
                    if (s_loose.Contains(filter.name))
                        continue;

                    Mesh mesh = filter.sharedMesh;
                    Matrix4x4 matrix = filter.transform.localToWorldMatrix;
                    Vector3[] points = mesh.vertices.Select(v => matrix.MultiplyPoint3x4(v)).ToArray();
                    Bounds bounds = GeometryUtility.CalculateBounds(points, Matrix4x4.identity);
                    int rigid = s_belt.Contains(filter.name) ? 0 : Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) <= RigidSize ? Nearest(bones, bounds.center) : -1;
                    int offset = vertices.Count;
                    vertices.AddRange(points);
                    normals.AddRange(mesh.normals.Select(n => matrix.MultiplyVector(n).normalized));
                    uvs.AddRange(mesh.uv);
                    weights.AddRange(points.Select(point => rigid >= 0 ? new BoneWeight { boneIndex0 = rigid, weight0 = 1f } : Weigh(bones, point)));
                    triangles[part].AddRange(mesh.triangles.Select(index => index + offset));
                }
            }

            Mesh result = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);

            if (result == null)
            {
                result = new Mesh { name = "ZombieWarrior" };
                AssetDatabase.CreateAsset(result, MeshPath);
            }

            result.Clear();
            result.indexFormat = vertices.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            result.SetVertices(vertices);
            result.SetNormals(normals);
            result.SetUVs(0, uvs);
            result.subMeshCount = triangles.Length;

            for (int i = 0; i < triangles.Length; i++)
                result.SetTriangles(triangles[i], i);

            result.boneWeights = weights.ToArray();
            result.bindposes = bones.Select(bone => Matrix4x4.TRS(bone.Bind, bone.BindRotation, Vector3.one).inverse).ToArray();
            result.RecalculateTangents();
            result.RecalculateBounds();
            EditorUtility.SetDirty(result);

            return result;
        }

        private static int Nearest(List<Bone> bones, Vector3 point)
        {
            int best = 0;

            for (int i = 1; i < bones.Count; i++)
            {
                if (Distance(bones[i], point) < Distance(bones[best], point))
                    best = i;
            }

            return best;
        }

        /// Up to three nearest bones of the vertex's own side, by inverse distance to the bone's segment.
        private static BoneWeight Weigh(List<Bone> bones, Vector3 point)
        {
            (int index, float weight)[] nearest = bones
                .Select((bone, index) => (index, weight: bone.Side * point.x < -0.03f ? 0f : 1f / Mathf.Pow(Distance(bone, point) + 0.01f, 4f)))
                .OrderByDescending(pair => pair.weight).Take(3).Where(pair => pair.weight > 0f).ToArray();
            float total = nearest.Sum(pair => pair.weight);
            (int index, float weight) Get(int i) => i < nearest.Length && nearest[i].weight / total > 0.05f ? (nearest[i].index, nearest[i].weight) : (0, 0f);
            float sum = Enumerable.Range(0, 3).Sum(i => Get(i).weight);

            return new BoneWeight
            {
                boneIndex0 = Get(0).index, weight0 = Get(0).weight / sum,
                boneIndex1 = Get(1).index, weight1 = Get(1).weight / sum,
                boneIndex2 = Get(2).index, weight2 = Get(2).weight / sum
            };
        }

        private static float Distance(Bone bone, Vector3 point)
        {
            Vector3 segment = bone.BindEnd - bone.Bind;
            float t = Mathf.Clamp01(Vector3.Dot(point - bone.Bind, segment) / segment.sqrMagnitude);

            return Vector3.Distance(point, bone.Bind + segment * t);
        }

        private static Material Material(string part)
        {
            BattleEditorUtility.EnsureFolder(MaterialsFolder);
            string path = $"{MaterialsFolder}/ZombieWarrior{part}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }

            string prefix = $"{PackFolder}/Textures/{part}_";
            material.SetTexture("_BaseMap", Map(prefix + "BaseColor.png", false));
            material.SetTexture("_BumpMap", Normal(prefix + "Normal.png"));
            material.SetTexture("_OcclusionMap", Map(prefix + "AO.png", true));
            material.SetTexture("_MetallicGlossMap", MetallicSmoothness(part));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.EnableKeyword("_OCCLUSIONMAP");
            EditorUtility.SetDirty(material);

            return material;
        }

        /// The pack's 4K maps import at 2K, data maps linear.
        private static Texture2D Map(string path, bool isLinear)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);

            if (importer.maxTextureSize != MapSize || importer.sRGBTexture == isLinear)
            {
                importer.maxTextureSize = MapSize;
                importer.sRGBTexture = !isLinear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Texture2D Normal(string path)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);

            if (importer.maxTextureSize != MapSize || importer.textureType != TextureImporterType.NormalMap)
            {
                importer.maxTextureSize = MapSize;
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// URP reads metalness from red and smoothness from alpha of one map: the pack's metallic and inverted roughness maps packed.
        private static Texture2D MetallicSmoothness(string part)
        {
            BattleEditorUtility.EnsureFolder(TexturesFolder);
            string path = $"{TexturesFolder}/{part}_MetallicSmoothness.png";

            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
            {
                string prefix = $"{PackFolder}/Textures/{part}_";
                Color32[] metallic = ReadPixels(Map(prefix + "Metallic.png", true));
                Color32[] roughness = ReadPixels(Map(prefix + "Roughness.png", true));
                Color32[] colors = metallic.Select((m, i) => new Color32(m.r, m.r, m.r, (byte)(255 - roughness[i].r))).ToArray();
                Texture2D pixels = new Texture2D(MapSize, MapSize, TextureFormat.RGBA32, false, true);
                pixels.SetPixels32(colors);
                File.WriteAllBytes(path, pixels.EncodeToPNG());
                Object.DestroyImmediate(pixels);
                AssetDatabase.ImportAsset(path);
                Map(path, true);
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Color32[] ReadPixels(Texture2D source)
        {
            RenderTexture target = RenderTexture.GetTemporary(MapSize, MapSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            Texture2D pixels = new Texture2D(MapSize, MapSize, TextureFormat.RGBA32, false, true);
            pixels.ReadPixels(new Rect(0f, 0f, MapSize, MapSize), 0, 0);
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(target);
            Color32[] colors = pixels.GetPixels32();
            Object.DestroyImmediate(pixels);

            return colors;
        }
    }
}
