using System;
using System.Collections.Generic;
using System.IO;
using Game.Scripts.Battle;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Editor.Battle
{
    /// Plate harness: one Tripo shell (1 m from cuff to cuff, chest to +Z, sleeves sagging forward) warped onto the T-pose skeleton and
    /// skinned. The torso is scaled as a whole; each sleeve segment is laid along its bone from joint to joint, the cuff rim on the wrist.
    internal static partial class BattleCharacterBuilder
    {
        public const string PlateHarnessMaterial = "OutfitPlateHarness";

        private const string HarnessPart = "PlateHarness";
        private const string HarnessFolder = PacksFolder + "/Armor/" + HarnessPart;
        private const string HarnessColorPath = HarnessFolder + "/Textures/" + HarnessPart + "_Color.jpg";
        private const string HarnessMaskPath = HarnessFolder + "/Textures/" + HarnessPart + "_MetallicSmoothness.png";
        private const float HarnessScale = 1.2f;
        // Sleeves are a third too short for the torso: stretched to the joints, widened less. The forearm skin stays inside the vambrace.
        private const float HarnessUpperGirth = 1.38f;
        private const float HarnessForeGirth = 1.5f;
        // The skirt widens from the belt (source height) to the hem to go round the female hips.
        private const float HarnessBelt = 0.27f;
        private const float HarnessFlare = 1.15f;
        // Shell height (source units) that lands on the shoulder joints: it puts the belt on the waist and the armpits on the armpits.
        private const float HarnessShoulderLine = 0.52f;
        // The body's torso axis sits behind the skeleton's zero.
        private const float HarnessDepth = -0.04f;
        // Half-width of the elbow bend and the reach of a sleeve around its axis (source units).
        private const float HarnessElbowBlend = 0.03f;
        private const float HarnessSleeveNear = 0.1f;
        private const float HarnessSleeveFar = 0.14f;
        private const int HarnessNearest = 4;
        private const int HarnessSmoothing = 3;

        // Right sleeve of the shell (source units): the middle of the couter and of the cuff rim.
        private static readonly Vector3 s_harnessElbow = new(0.335f, 0.456f, 0.033f);
        private static readonly Vector3 s_harnessCuff = new(0.497f, 0.4435f, 0.102f);
        private static readonly BodyRegion[] s_harnessSkin = { BodyRegion.Torso, BodyRegion.UpperArms, BodyRegion.Thighs };

        /// Adds the harness to the built character prefab without rebuilding the bodies and the other parts.
        [MenuItem("Tools/Game/Battle/Build Plate Harness")]
        public static void BuildHarness()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BattleEditorUtility.ModelPath);

            try
            {
                SerializedObject so = new SerializedObject(root.GetComponent<CharacterModelComponent>());
                Transform outfitRoot = root.transform.Find("Outfit");
                SerializedProperty bodyProperty = so.FindProperty("_maleBody");
                Renderer[] body = new Renderer[bodyProperty.arraySize];

                for (int i = 0; i < body.Length; i++)
                    body[i] = (Renderer)bodyProperty.GetArrayElementAtIndex(i).objectReferenceValue;

                foreach (string prefix in new[] { "Male_", "Female_" })
                {
                    Transform old = outfitRoot.Find(prefix + HarnessPart);

                    if (old != null)
                        Object.DestroyImmediate(old.gameObject);
                }

                Renderer[] harness = CreateHarness(outfitRoot, ((SkinnedMeshRenderer)body[0]).bones, body);
                int index = (int)OutfitPart.PlateHarness;
                Element(so, "_maleParts", index).objectReferenceValue = harness[0];
                Element(so, "_femaleParts", index).objectReferenceValue = harness[1];
                Element(so, "_covers", index).intValue = (int)s_parts[index].covers;
                so.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssets();
                PrefabUtility.SaveAsPrefabAsset(root, BattleEditorUtility.ModelPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// One mesh for both bodies: male first, female second.
        private static Renderer[] CreateHarness(Transform parent, Transform[] bones, Renderer[] body)
        {
            Mesh mesh = CreateHarnessMesh(bones, body);
            Material[] materials = { HarnessMaterial() };

            return Array.ConvertAll(new[] { "Male_", "Female_" }, prefix =>
            {
                Renderer renderer = CreateRenderer(prefix + HarnessPart, parent, mesh, bones, materials);
                renderer.gameObject.SetActive(false);

                return renderer;
            });
        }

        private static Mesh CreateHarnessMesh(Transform[] bones, Renderer[] body)
        {
            Mesh source = AssetDatabase.LoadAssetAtPath<GameObject>($"{HarnessFolder}/{HarnessPart}.fbx").GetComponentInChildren<MeshFilter>().sharedMesh;
            Vector3[] vertices = source.vertices;
            Vector3[] normals = source.normals;
            int[] triangles = source.triangles;
            Vector3 shoulder = Find(bones, "upperarm_r");
            Vector3 elbow = Find(bones, "lowerarm_r");
            Vector3 anchor = new Vector3(0f, HarnessShoulderLine, 0f);
            Vector3 chest = new Vector3(0f, shoulder.y, HarnessDepth);
            Vector3 sourceShoulder = anchor + (shoulder - chest) / HarnessScale;
            Sleeve upper = new Sleeve(sourceShoulder, s_harnessElbow, shoulder, elbow, HarnessUpperGirth);
            Sleeve fore = new Sleeve(s_harnessElbow, s_harnessCuff, elbow, Find(bones, "hand_r"), HarnessForeGirth);
            Vector3 bend = (upper.Axis + fore.Axis).normalized;
            float[] lower = new float[vertices.Length];
            float[] rigid = new float[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 mirror = new Vector3(vertices[i].x < 0f ? -1f : 1f, 1f, 1f);
                Vector3 point = Vector3.Scale(vertices[i], mirror);
                Vector3 normal = Vector3.Scale(normals[i], mirror);
                float along = Vector3.Dot(point - sourceShoulder, upper.Axis);
                float reach = Mathf.Min(SegmentDistance(point, sourceShoulder, s_harnessElbow), SegmentDistance(point, s_harnessElbow, s_harnessCuff));
                float sleeve = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(HarnessSleeveNear, HarnessSleeveFar, reach));
                float arm = sleeve * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.02f, 0.12f, along));
                lower[i] = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-HarnessElbowBlend, HarnessElbowBlend, Vector3.Dot(point - s_harnessElbow, bend)));
                // From the middle of the upper arm on the plates are rigid on their bones.
                rigid[i] = sleeve * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.6f, along / upper.Length));
                Vector3 onArm = Vector3.Lerp(upper.Map(point), fore.Map(point), lower[i]);
                Vector3 normalOnArm = Vector3.Lerp(upper.MapNormal(normal), fore.MapNormal(normal), lower[i]);
                float flare = Mathf.Lerp(1f, HarnessFlare, Mathf.InverseLerp(HarnessBelt, 0f, point.y));
                Vector3 onTorso = chest + Vector3.Scale(point - anchor, new Vector3(flare, 1f, flare)) * HarnessScale;
                vertices[i] = Vector3.Scale(Vector3.Lerp(onTorso, onArm, arm), mirror);
                normals[i] = Vector3.Scale(Vector3.Lerp(normal, normalOnArm, arm).normalized, mirror);
            }

            float[][] skin = BodyWeights(source.vertices, vertices, triangles, body, bones);
            BoneWeight[] weights = new BoneWeight[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                string side = vertices[i].x < 0f ? "_l" : "_r";
                float[] row = (float[])skin[i].Clone();

                for (int bone = 0; bone < row.Length; bone++)
                    row[bone] *= 1f - rigid[i];

                row[Array.FindIndex(bones, bone => bone.name == "upperarm" + side)] += rigid[i] * (1f - lower[i]);
                row[Array.FindIndex(bones, bone => bone.name == "lowerarm" + side)] += rigid[i] * lower[i];
                weights[i] = Strongest(row);
            }

            Mesh mesh = GetMesh("Male_" + HarnessPart);
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, source.uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateTangents();
            mesh.boneWeights = weights;
            mesh.bindposes = Array.ConvertAll(bones, bone => bone.worldToLocalMatrix);
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);

            return mesh;
        }

        /// Per shell vertex, the weights of the nearest body skin, smoothed along the shell. Vertices split by UV seams share one row;
        /// the collar stays on the chest instead of turning with the head.
        private static float[][] BodyWeights(Vector3[] sourceVertices, Vector3[] vertices, int[] triangles, Renderer[] body, Transform[] bones)
        {
            List<Vector3> skinPoints = new();
            List<BoneWeight> skinWeights = new();
            int chest = Array.FindIndex(bones, bone => bone.name == "spine_03");
            int[] remap = Array.ConvertAll(bones, bone => RegionIndex(bone.name) == RegionIndex("Head") ? chest : Array.IndexOf(bones, bone));

            foreach (BodyRegion region in s_harnessSkin)
            {
                SkinnedMeshRenderer renderer = (SkinnedMeshRenderer)body[Mathf.RoundToInt(Mathf.Log((int)region, 2f))];
                Mesh baked = new Mesh();
                renderer.BakeMesh(baked);

                foreach (Vector3 vertex in baked.vertices)
                    skinPoints.Add(renderer.transform.TransformPoint(vertex));

                skinWeights.AddRange(renderer.sharedMesh.boneWeights);
                Object.DestroyImmediate(baked);
            }

            Dictionary<Vector3, int> welded = new();
            int[] weld = new int[vertices.Length];
            float[][] rows = new float[vertices.Length][];
            HashSet<int>[] neighbours = new HashSet<int>[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                if (!welded.TryGetValue(sourceVertices[i], out weld[i]))
                {
                    welded[sourceVertices[i]] = weld[i] = i;
                    rows[i] = NearestWeights(vertices[i], skinPoints, skinWeights, remap);
                    neighbours[i] = new HashSet<int>();
                }
            }

            for (int i = 0; i < triangles.Length; i++)
            {
                neighbours[weld[triangles[i]]].Add(weld[triangles[i - i % 3 + (i + 1) % 3]]);
                neighbours[weld[triangles[i]]].Add(weld[triangles[i - i % 3 + (i + 2) % 3]]);
            }

            for (int pass = 0; pass < HarnessSmoothing; pass++)
            {
                float[][] smoothed = new float[vertices.Length][];

                for (int i = 0; i < vertices.Length; i++)
                {
                    if (weld[i] != i)
                        continue;

                    smoothed[i] = new float[bones.Length];

                    for (int bone = 0; bone < bones.Length; bone++)
                    {
                        float sum = 0f;

                        foreach (int neighbour in neighbours[i])
                            sum += rows[neighbour][bone];

                        smoothed[i][bone] = neighbours[i].Count == 0 ? rows[i][bone] : (rows[i][bone] + sum / neighbours[i].Count) * 0.5f;
                    }
                }

                rows = smoothed;
            }

            for (int i = 0; i < vertices.Length; i++)
                rows[i] = rows[weld[i]];

            return rows;
        }

        private static float[] NearestWeights(Vector3 point, List<Vector3> skinPoints, List<BoneWeight> skinWeights, int[] remap)
        {
            int[] nearest = new int[HarnessNearest];
            float[] distances = new float[HarnessNearest];
            Array.Fill(distances, float.MaxValue);

            for (int i = 0; i < skinPoints.Count; i++)
            {
                float distance = (skinPoints[i] - point).sqrMagnitude;

                for (int slot = 0; slot < HarnessNearest; slot++)
                {
                    if (distance >= distances[slot])
                        continue;

                    Array.Copy(distances, slot, distances, slot + 1, HarnessNearest - slot - 1);
                    Array.Copy(nearest, slot, nearest, slot + 1, HarnessNearest - slot - 1);
                    distances[slot] = distance;
                    nearest[slot] = i;
                    break;
                }
            }

            float[] row = new float[remap.Length];
            float total = 0f;

            for (int slot = 0; slot < HarnessNearest; slot++)
            {
                BoneWeight weight = skinWeights[nearest[slot]];
                float share = 1f / (distances[slot] + 1e-6f);
                row[remap[weight.boneIndex0]] += weight.weight0 * share;
                row[remap[weight.boneIndex1]] += weight.weight1 * share;
                row[remap[weight.boneIndex2]] += weight.weight2 * share;
                row[remap[weight.boneIndex3]] += weight.weight3 * share;
                total += share;
            }

            for (int bone = 0; bone < row.Length; bone++)
                row[bone] /= total;

            return row;
        }

        /// The four heaviest bones of a row, normalized.
        private static BoneWeight Strongest(float[] row)
        {
            int[] order = new int[row.Length];

            for (int i = 0; i < order.Length; i++)
                order[i] = i;

            Array.Sort(order, (a, b) => row[b].CompareTo(row[a]));
            float total = row[order[0]] + row[order[1]] + row[order[2]] + row[order[3]];

            return new BoneWeight
            {
                boneIndex0 = order[0], weight0 = row[order[0]] / total, boneIndex1 = order[1], weight1 = row[order[1]] / total,
                boneIndex2 = order[2], weight2 = row[order[2]] / total, boneIndex3 = order[3], weight3 = row[order[3]] / total
            };
        }

        private static float SegmentDistance(Vector3 point, Vector3 from, Vector3 to)
        {
            Vector3 axis = to - from;

            return (point - from - axis * Mathf.Clamp01(Vector3.Dot(point - from, axis) / axis.sqrMagnitude)).magnitude;
        }

        private static SerializedProperty Element(SerializedObject so, string array, int index)
        {
            SerializedProperty property = so.FindProperty(array);
            property.arraySize = Mathf.Max(property.arraySize, index + 1);

            return property.GetArrayElementAtIndex(index);
        }

        private static Material HarnessMaterial()
        {
            Material material = BattleEditorUtility.GetMaterial(PlateHarnessMaterial, Color.white, 1f, 1f);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(HarnessColorPath));
            material.SetTexture("_MetallicGlossMap", HarnessMask());
            material.EnableKeyword("_METALLICSPECGLOSSMAP");

            return material;
        }

        /// Tripo gave the colour alone. URP mask (R metallic, A smoothness) read off it: grey is steel, the brighter the more polished
        /// (plate over mail); anything coloured or black is leather and cloth.
        private static Texture2D HarnessMask()
        {
            Texture2D mask = AssetDatabase.LoadAssetAtPath<Texture2D>(HarnessMaskPath);

            if (mask != null)
                return mask;

            RenderTexture target = RenderTexture.GetTemporary(MaskSize, MaskSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(AssetDatabase.LoadAssetAtPath<Texture2D>(HarnessColorPath), target);
            RenderTexture.active = target;
            Texture2D pixels = new Texture2D(MaskSize, MaskSize, TextureFormat.RGBA32, false, true);
            pixels.ReadPixels(new Rect(0f, 0f, MaskSize, MaskSize), 0, 0);
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(target);
            Color32[] colors = pixels.GetPixels32();

            for (int i = 0; i < colors.Length; i++)
            {
                float bright = Mathf.Max(colors[i].r, colors[i].g, colors[i].b) / 255f;
                float saturation = bright > 0f ? 1f - Mathf.Min(colors[i].r, colors[i].g, colors[i].b) / 255f / bright : 0f;
                float metal = Mathf.Clamp01((0.3f - saturation) / 0.15f) * Mathf.Clamp01((bright - 0.15f) / 0.2f);
                colors[i] = new Color32((byte)(metal * 255f), 255, 0, (byte)(Mathf.Lerp(0.2f, 0.3f + 0.35f * bright, metal) * 255f));
            }

            pixels.SetPixels32(colors);
            File.WriteAllBytes(HarnessMaskPath, pixels.EncodeToPNG());
            Object.DestroyImmediate(pixels);
            AssetDatabase.ImportAsset(HarnessMaskPath);
            SetLinear(HarnessMaskPath);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(HarnessMaskPath);
        }

        /// A sleeve segment of the shell laid along a bone: stretched to the bone's length, widened by the girth.
        private readonly struct Sleeve
        {
            public readonly Vector3 Axis;
            public readonly float Length;

            private readonly Vector3 _from;
            private readonly Vector3 _to;
            private readonly Quaternion _turn;
            private readonly float _stretch;
            private readonly float _girth;

            public Sleeve(Vector3 from, Vector3 fromEnd, Vector3 to, Vector3 toEnd, float girth)
            {
                _from = from;
                _to = to;
                _girth = girth;
                Length = (fromEnd - from).magnitude;
                Axis = (fromEnd - from) / Length;
                _turn = Quaternion.FromToRotation(Axis, toEnd - to);
                _stretch = (toEnd - to).magnitude / Length;
            }

            public Vector3 Map(Vector3 point)
            {
                float along = Vector3.Dot(point - _from, Axis);

                return _to + _turn * (Axis * (along * _stretch) + (point - _from - Axis * along) * _girth);
            }

            public Vector3 MapNormal(Vector3 normal)
            {
                float along = Vector3.Dot(normal, Axis);

                return _turn * (Axis * (along / _stretch) + (normal - Axis * along) / _girth);
            }
        }
    }
}
