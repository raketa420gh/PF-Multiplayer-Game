using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Editor.Battle
{
    /// Rigs the static Character Creator OBJ: skeleton from measured joints, bone-heat skin weights,
    /// A-pose baked to a T-pose and a humanoid avatar. The saved prefab replaces a rigged FBX.
    internal static class BattleCharacterBuilder
    {
        public const string SourcePath = "Assets/SpecialFolder/3D Models/Character/Obj/obj.obj";

        [Flags]
        private enum Region
        {
            None = 0,
            Head = 1,
            Body = 2,
            Arm = 4,
            Leg = 8,
            Nails = 16,
            Eye = 32
        }

        private sealed class Bone
        {
            public string Name;
            public string HumanName;
            public int Parent;
            public int Side;
            public Region Regions;
            public Vector3 Position;
            public Vector3[] Tails;

            public float Distance(Vector3 point)
            {
                float result = float.MaxValue;

                foreach (Vector3 tail in Tails)
                {
                    Vector3 axis = tail - Position;
                    float t = Mathf.Clamp01(Vector3.Dot(point - Position, axis) / axis.sqrMagnitude);
                    result = Mathf.Min(result, Vector3.Distance(point, Position + axis * t));
                }

                return result;
            }
        }

        private const string MeshPath = BattleEditorUtility.ModelsFolder + "/Character.asset";
        private const string AvatarPath = BattleEditorUtility.ModelsFolder + "/CharacterAvatar.asset";
        private const float Scale = 0.01f;
        private const float Heat = 1f;
        private const int HeatIterations = 200;
        private const int SpineBones = 6;
        private const int SideBones = 8;

        private static readonly (string material, Region region)[] s_regions =
        {
            ("Skin_Head", Region.Head), ("Skin_Body", Region.Body), ("Skin_Arm", Region.Arm),
            ("Skin_Leg", Region.Leg), ("Nails", Region.Nails), ("Ga_Eye", Region.Eye)
        };

        [MenuItem("Tools/Game/Battle/Build Character")]
        public static void Build()
        {
            SetupSource();
            BattleEditorUtility.EnsureFolder(BattleEditorUtility.ModelsFolder);
            BattleEditorUtility.EnsureFolder(BattleEditorUtility.MaterialsFolder);
            BattleEditorUtility.EnsureFolder(BattleEditorUtility.PrefabsFolder);

            List<Vector3> vertices = new();
            List<Vector3> normals = new();
            List<Vector2> uvs = new();
            List<int> triangles = new();
            List<int> weld = new();
            List<Vector3> points = new();
            List<Region> regions = new();
            ReadSource(vertices, normals, uvs, triangles, weld, points, regions);

            List<Bone> bones = CreateBones();
            float[] weights = SolveWeights(bones, points, regions, CreateLinks(triangles, weld, points.Count));
            BakeTPose(bones, vertices, normals, weld, weights);

            GameObject root = new GameObject("Character");
            Transform[] transforms = new Transform[bones.Count];

            for (int i = 0; i < bones.Count; i++)
            {
                transforms[i] = BattleEditorUtility.CreateChild(bones[i].Name, i == 0 ? root.transform : transforms[bones[i].Parent]).transform;
                transforms[i].position = bones[i].Position;
            }

            BoneWeight[] pointWeights = new BoneWeight[points.Count];

            for (int i = 0; i < pointWeights.Length; i++)
                pointWeights[i] = CreateBoneWeight(weights, i * bones.Count, bones.Count);

            Mesh mesh = new Mesh { name = "Character" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.boneWeights = weld.ConvertAll(id => pointWeights[id]).ToArray();
            mesh.bindposes = Array.ConvertAll(transforms, bone => bone.worldToLocalMatrix);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            SkinnedMeshRenderer renderer = BattleEditorUtility.CreateChild("Body", root.transform).AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = Save(mesh, MeshPath);
            renderer.bones = transforms;
            renderer.rootBone = transforms[0];
            renderer.sharedMaterial = BattleEditorUtility.GetMaterial("FighterBody", new Color(0.62f, 0.66f, 0.72f));

            HumanDescription description = new HumanDescription
            {
                human = bones.ConvertAll(bone => new HumanBone
                {
                    boneName = bone.Name,
                    humanName = bone.HumanName,
                    limit = new HumanLimit { useDefaultValues = true }
                }).ToArray(),
                skeleton = Array.ConvertAll(root.GetComponentsInChildren<Transform>(), bone => new SkeletonBone
                {
                    name = bone.name,
                    position = bone.localPosition,
                    rotation = bone.localRotation,
                    scale = bone.localScale
                }),
                upperArmTwist = 0.5f,
                lowerArmTwist = 0.5f,
                upperLegTwist = 0.5f,
                lowerLegTwist = 0.5f,
                armStretch = 0.05f,
                legStretch = 0.05f
            };

            Avatar avatar = AvatarBuilder.BuildHumanAvatar(root, description);
            avatar.name = "CharacterAvatar";

            if (!avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException("Character avatar is not a valid humanoid");

            root.AddComponent<Animator>().avatar = Save(avatar, AvatarPath);
            BattleContentBuilder.SavePrefab(root, BattleEditorUtility.ModelPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[{nameof(BattleCharacterBuilder)}] Character rigged: {vertices.Count} vertices, {bones.Count} bones");
        }

        private static void SetupSource()
        {
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(SourcePath);

            if (importer.isReadable && Mathf.Approximately(importer.globalScale, Scale))
                return;

            importer.isReadable = true;
            importer.globalScale = Scale;
            importer.SaveAndReimport();
        }

        /// Joints are measured on the source mesh in meters (A-pose, facing +Z); sided bones are mirrored by X.
        private static List<Bone> CreateBones()
        {
            List<Bone> bones = new();

            int Add(string name, string humanName, int parent, int side, Region regions, params Vector3[] joints)
            {
                for (int i = 0; i < joints.Length && side != 0; i++)
                    joints[i].x *= side;

                bones.Add(new Bone
                {
                    Name = name,
                    HumanName = humanName,
                    Parent = parent,
                    Side = side,
                    Regions = regions,
                    Position = joints[0],
                    Tails = joints[1..]
                });

                return bones.Count - 1;
            }

            Vector3 hips = new(0f, 0.99f, -0.015f);
            Vector3 spine = new(0f, 1.1f, -0.02f);
            Vector3 chest = new(0f, 1.225f, -0.03f);
            Vector3 upperChest = new(0f, 1.36f, -0.045f);
            Vector3 neck = new(0f, 1.535f, -0.05f);
            Vector3 head = new(0f, 1.63f, -0.025f);

            Add("Hips", "Hips", -1, 0, Region.Body | Region.Leg, hips, spine, new Vector3(-0.12f, 0.9f, -0.005f), new Vector3(0.12f, 0.9f, -0.005f));
            Add("Spine", "Spine", 0, 0, Region.Body, spine, chest);
            Add("Spine1", "Chest", 1, 0, Region.Body, chest, upperChest);
            Add("Spine2", "UpperChest", 2, 0, Region.Body | Region.Head, upperChest, neck, new Vector3(-0.15f, 1.38f, -0.04f), new Vector3(0.15f, 1.38f, -0.04f));
            Add("Neck", "Neck", 3, 0, Region.Body | Region.Head, neck, head);
            Add("Head", "Head", 4, 0, Region.Head | Region.Eye, head, new Vector3(0f, 1.8f, -0.01f), new Vector3(0f, 1.615f, 0.06f));

            for (int side = -1; side <= 1; side += 2)
            {
                string prefix = side < 0 ? "Left" : "Right";
                Vector3 upperArm = new(0.17f, 1.5f, -0.06f);
                Vector3 lowerArm = new(0.438f, 1.348f, -0.06f);
                Vector3 hand = new(0.686f, 1.21f, -0.058f);
                Vector3 lowerLeg = new(0.086f, 0.5f, -0.005f);
                Vector3 foot = new(0.076f, 0.09f, -0.015f);
                Vector3 toes = new(0.082f, 0.02f, 0.125f);

                int arm = Add(prefix + "Shoulder", prefix + "Shoulder", 3, side, Region.Body | Region.Head | Region.Arm, new Vector3(0.045f, 1.475f, -0.05f), upperArm);
                arm = Add(prefix + "Arm", prefix + "UpperArm", arm, side, Region.Arm, upperArm, lowerArm);
                arm = Add(prefix + "ForeArm", prefix + "LowerArm", arm, side, Region.Arm, lowerArm, hand);
                Add(prefix + "Hand", prefix + "Hand", arm, side, Region.Arm | Region.Nails, hand, new Vector3(0.875f, 1.112f, -0.057f));

                int leg = Add(prefix + "UpLeg", prefix + "UpperLeg", 0, side, Region.Body | Region.Leg, new Vector3(0.09f, 0.94f, -0.005f), lowerLeg);
                leg = Add(prefix + "Leg", prefix + "LowerLeg", leg, side, Region.Leg, lowerLeg, foot);
                leg = Add(prefix + "Foot", prefix + "Foot", leg, side, Region.Leg, foot, toes);
                Add(prefix + "ToeBase", prefix + "Toes", leg, side, Region.Leg | Region.Nails, toes, new Vector3(0.082f, 0.015f, 0.21f));
            }

            return bones;
        }

        /// Merges the skin and eye submeshes into one vertex stream; points are the same vertices welded by position.
        private static void ReadSource(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles,
            List<int> weld, List<Vector3> points, List<Region> regions)
        {
            Dictionary<Vector3Int, int> welded = new();

            foreach (MeshFilter filter in AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath).GetComponentsInChildren<MeshFilter>())
            {
                Mesh part = filter.sharedMesh;
                Material[] materials = filter.GetComponent<MeshRenderer>().sharedMaterials;
                Matrix4x4 matrix = filter.transform.localToWorldMatrix;
                Vector3[] partVertices = part.vertices;
                Vector3[] partNormals = part.normals;
                Vector2[] partUvs = part.uv;
                int[] remap = new int[part.vertexCount];

                for (int sub = 0; sub < part.subMeshCount; sub++)
                {
                    Region region = GetRegion(materials[sub].name);

                    if (region == Region.None)
                        continue;

                    foreach (int index in part.GetTriangles(sub))
                    {
                        if (remap[index] == 0)
                        {
                            Vector3 point = matrix.MultiplyPoint3x4(partVertices[index]);
                            Vector3Int key = Vector3Int.RoundToInt(point * 1e5f);

                            if (!welded.TryGetValue(key, out int id))
                            {
                                id = points.Count;
                                welded.Add(key, id);
                                points.Add(point);
                                regions.Add(Region.None);
                            }

                            regions[id] |= region;
                            vertices.Add(point);
                            normals.Add(matrix.MultiplyVector(partNormals[index]).normalized);
                            uvs.Add(partUvs[index]);
                            weld.Add(id);
                            remap[index] = vertices.Count;
                        }

                        triangles.Add(remap[index] - 1);
                    }
                }
            }
        }

        private static Region GetRegion(string material)
        {
            foreach ((string name, Region region) in s_regions)
            {
                if (material.Contains(name))
                    return region;
            }

            return Region.None;
        }

        private static int[][] CreateLinks(List<int> triangles, List<int> weld, int count)
        {
            HashSet<int>[] links = new HashSet<int>[count];

            for (int i = 0; i < count; i++)
                links[i] = new HashSet<int>();

            for (int i = 0; i < triangles.Count; i += 3)
            {
                for (int corner = 0; corner < 3; corner++)
                {
                    int a = weld[triangles[i + corner]];
                    int b = weld[triangles[i + (corner + 1) % 3]];

                    if (a == b)
                        continue;

                    links[a].Add(b);
                    links[b].Add(a);
                }
            }

            return Array.ConvertAll(links, set => new List<int>(set).ToArray());
        }

        /// Bone heat: every point is pinned to its nearest allowed bone with strength (edge / distance)², then diffused over the surface.
        private static float[] SolveWeights(List<Bone> bones, List<Vector3> points, List<Region> regions, int[][] links)
        {
            int boneCount = bones.Count;
            float[] weights = new float[points.Count * boneCount];
            int[] nearest = new int[points.Count];
            float[] heat = new float[points.Count];

            for (int i = 0; i < points.Count; i++)
            {
                float distance = float.MaxValue;

                for (int b = 0; b < boneCount; b++)
                {
                    if ((bones[b].Regions & regions[i]) == 0 || bones[b].Side * points[i].x < -1e-4f)
                        continue;

                    float candidate = bones[b].Distance(points[i]);

                    if (candidate >= distance)
                        continue;

                    distance = candidate;
                    nearest[i] = b;
                }

                float edge = 0f;

                foreach (int link in links[i])
                    edge += Vector3.Distance(points[i], points[link]) / links[i].Length;

                heat[i] = Heat * edge * edge / Mathf.Max(distance * distance, 1e-6f);
                weights[i * boneCount + nearest[i]] = 1f;
            }

            float[] sum = new float[boneCount];

            for (int iteration = 0; iteration < HeatIterations; iteration++)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    if (links[i].Length == 0)
                        continue;

                    Array.Clear(sum, 0, boneCount);

                    foreach (int link in links[i])
                    {
                        for (int b = 0; b < boneCount; b++)
                            sum[b] += weights[link * boneCount + b];
                    }

                    float scale = 1f / (links[i].Length * (1f + heat[i]));

                    for (int b = 0; b < boneCount; b++)
                        weights[i * boneCount + b] = sum[b] * scale;

                    weights[i * boneCount + nearest[i]] += heat[i] / (1f + heat[i]);
                }
            }

            return weights;
        }

        /// Raises both arms from the A-pose to horizontal with linear blend skinning and moves the arm joints along.
        private static void BakeTPose(List<Bone> bones, List<Vector3> vertices, List<Vector3> normals, List<int> weld, float[] weights)
        {
            for (int side = 0; side < 2; side++)
            {
                int upperArm = SpineBones + side * SideBones + 1;
                Vector3 pivot = bones[upperArm].Position;
                Quaternion rotation = Quaternion.FromToRotation(bones[upperArm + 2].Position - pivot, Vector3.right * bones[upperArm].Side);

                for (int i = 0; i < vertices.Count; i++)
                {
                    int offset = weld[i] * bones.Count + upperArm;
                    float weight = weights[offset] + weights[offset + 1] + weights[offset + 2];
                    vertices[i] = Vector3.LerpUnclamped(vertices[i], pivot + rotation * (vertices[i] - pivot), weight);
                    normals[i] = Vector3.LerpUnclamped(normals[i], rotation * normals[i], weight).normalized;
                }

                for (int i = upperArm; i < upperArm + 3; i++)
                    bones[i].Position = pivot + rotation * (bones[i].Position - pivot);
            }
        }

        private static BoneWeight CreateBoneWeight(float[] weights, int offset, int boneCount)
        {
            int[] order = new int[boneCount];

            for (int i = 0; i < boneCount; i++)
                order[i] = i;

            Array.Sort(order, (a, b) => weights[offset + b].CompareTo(weights[offset + a]));
            float sum = weights[offset + order[0]] + weights[offset + order[1]] + weights[offset + order[2]] + weights[offset + order[3]];

            return new BoneWeight
            {
                boneIndex0 = order[0],
                boneIndex1 = order[1],
                boneIndex2 = order[2],
                boneIndex3 = order[3],
                weight0 = weights[offset + order[0]] / sum,
                weight1 = weights[offset + order[1]] / sum,
                weight2 = weights[offset + order[2]] / sum,
                weight3 = weights[offset + order[3]] / sum
            };
        }

        /// Overwrites an existing asset in place so references to it survive rebuilds.
        private static T Save<T>(T asset, string path) where T : Object
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);

            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, path);

                return asset;
            }

            EditorUtility.CopySerialized(asset, existing);
            Object.DestroyImmediate(asset);

            return existing;
        }
    }
}
