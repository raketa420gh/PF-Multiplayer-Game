using System;
using System.Collections.Generic;
using System.IO;
using Game.Scripts.Battle;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Editor.Battle
{
    /// Assembles the modular character from the Quaternius packs: the base character skeleton rebuilt with world-aligned bones,
    /// the base bodies split into body regions and every outfit part rebound to that skeleton.
    internal static class BattleCharacterBuilder
    {
        public const string AnimationsPath1 = PacksFolder + "/Universal Animation Library[Standard]/Unity/UAL1_Standard.fbx";
        public const string AnimationsPath2 = PacksFolder + "/Universal Animation Library 2[Standard]/Unity/UAL2_Standard.fbx";
        public const string PeasantMaterial = "OutfitPeasant";
        public const string PeasantAltMaterial = "OutfitPeasant2";
        public const string RangerMaterial = "OutfitRanger";
        public const string RangerAltMaterial = "OutfitRanger3";
        public const string WoodMaterial = "DummyWood";

        private const string PacksFolder = "Assets/SpecialFolder/3D Models";
        private const string BaseFolder = PacksFolder + "/Universal Base Characters[Standard]/Base Characters";
        private const string MaleBodyPath = BaseFolder + "/Unity/Superhero_Male_FullBody.fbx";
        private const string FemaleBodyPath = BaseFolder + "/Unity/Superhero_Female_FullBody.fbx";
        private const string OutfitsFolder = PacksFolder + "/Modular Character Outfits - Fantasy[Standard]";
        public const string HeadMeshPath = MeshesFolder + "/Male_Head.asset";
        private const string MeshesFolder = BattleEditorUtility.ModelsFolder + "/Character";
        private const string TexturesFolder = "Assets/Game/Textures/Battle";
        private const string AvatarPath = BattleEditorUtility.ModelsFolder + "/CharacterAvatar.asset";
        // Root bone of the animation libraries, their root motion node.
        private const string MotionNode = "Armature/root";
        private const int MaskSize = 1024;
        // The combat body model (hitboxes, eye point, weapon poses) is authored for a head joint at this height.
        private const float HeadHeight = 1.63f;
        // Boots reach above the ankle: the shin below this height (source meters) is hidden together with the feet.
        private const float BootTop = 0.37f;

        private static readonly Bounds s_bounds = new(new Vector3(0f, 1f, 0f), new Vector3(4.4f, 2.8f, 4.4f));

        private static readonly (string bone, BodyRegion region)[] s_regions =
        {
            ("Head", BodyRegion.Head), ("neck", BodyRegion.Head), ("upperarm", BodyRegion.UpperArms), ("lowerarm", BodyRegion.Forearms),
            ("hand", BodyRegion.Hands), ("thumb", BodyRegion.Hands), ("index", BodyRegion.Hands), ("middle", BodyRegion.Hands),
            ("ring", BodyRegion.Hands), ("pinky", BodyRegion.Hands), ("thigh", BodyRegion.Thighs), ("calf", BodyRegion.Calves),
            ("foot", BodyRegion.Feet), ("ball", BodyRegion.Feet)
        };

        /// Indexed by OutfitPart. Sources are renderer names inside the Male_/Female_ outfit files.
        private static readonly (string outfit, string male, string female, BodyRegion covers)[] s_parts =
        {
            ("Peasant", "Peasant_Body", "Peasant_Body", BodyRegion.Torso),
            ("Peasant", "Peasant_Arms", "Peasant_Arms", BodyRegion.UpperArms | BodyRegion.Forearms | BodyRegion.Hands),
            ("Peasant", "Peasant_Legs", "Peasant_Legs", BodyRegion.Thighs | BodyRegion.Calves),
            ("Peasant", "Peasant_Feet", "Peasant_Feet", BodyRegion.Feet),
            ("Ranger", "Ranger_Body", "Ranger_Body", BodyRegion.Torso),
            ("Ranger", "Ranger_Arms", "Ranger_Arms", BodyRegion.UpperArms | BodyRegion.Forearms | BodyRegion.Hands),
            ("Ranger", "Ranger_Arms_Bracer", "Ranger_Arms_Bracer", BodyRegion.None),
            ("Ranger", "Ranger_Body_Belt_1", "Ranger_Body_Belt_1", BodyRegion.None),
            ("Ranger", "Ranger_Body_Belt_2", "Ranger_Body_Belt_2", BodyRegion.None),
            ("Ranger", "Ranger_Acc_Pauldron", "Ranger_Acc_Pauldrons", BodyRegion.None),
            ("Ranger", "Ranger_Head_Hood", "Ranger_Head_Hood", BodyRegion.None),
            ("Ranger", "Ranger_Legs", "Ranger_Legs", BodyRegion.Thighs | BodyRegion.Calves),
            ("Ranger", "Ranger_Feet_Boots", "Ranger_Feet", BodyRegion.Feet)
        };

        /// Upper-body joints of the simulation body the weapon poses were authored on (meters, T-pose, sided bones given for
        /// the right side). The skeleton takes them as they are, so arm IK, reach and weapon traces do not depend on the model.
        private static readonly (string bone, Vector3 position)[] s_joints =
        {
            ("pelvis", new Vector3(0f, 0.99f, -0.015f)), ("spine_01", new Vector3(0f, 1.1f, -0.02f)), ("spine_02", new Vector3(0f, 1.225f, -0.03f)),
            ("spine_03", new Vector3(0f, 1.36f, -0.045f)), ("neck_01", new Vector3(0f, 1.535f, -0.05f)), ("Head", new Vector3(0f, 1.63f, -0.025f)),
            ("clavicle", new Vector3(0.045f, 1.475f, -0.05f)), ("upperarm", new Vector3(0.17f, 1.5f, -0.06f)),
            ("lowerarm", new Vector3(0.478f, 1.5f, -0.06f)), ("hand", new Vector3(0.762f, 1.5f, -0.06f))
        };

        private static readonly (string bone, string human)[] s_humanBones =
        {
            ("pelvis", "Hips"), ("spine_01", "Spine"), ("spine_02", "Chest"), ("spine_03", "UpperChest"), ("neck_01", "Neck"), ("Head", "Head")
        };

        private static readonly (string bone, string human)[] s_sideBones =
        {
            ("clavicle", "Shoulder"), ("upperarm", "UpperArm"), ("lowerarm", "LowerArm"), ("hand", "Hand"),
            ("thigh", "UpperLeg"), ("calf", "LowerLeg"), ("foot", "Foot"), ("ball", "Toes")
        };

        private static readonly (string bone, string human)[] s_fingers =
        {
            ("thumb", "Thumb"), ("index", "Index"), ("middle", "Middle"), ("ring", "Ring"), ("pinky", "Little")
        };

        private static readonly string[] s_phalanges = { "Proximal", "Intermediate", "Distal" };

        /// Extra cuts of library takes (start and end are shares of the take): looped variants, the wind-up part of the throw,
        /// the take-off without its squat, the kneel between going down and getting up, the reload as hands wrapping a bandage.
        private static readonly (string name, string take, float start, float end, bool isLoop)[] s_cuts =
        {
            ("Consume_Loop", "Consume", 0f, 1f, true), ("Interact_Loop", "Interact", 0f, 1f, true), ("Throw", "OverhandThrow", 0f, 0.45f, false),
            ("Jump_Rise", "Jump_Start", 0.08f, 1f, false), ("Kneel_Loop", "Fixing_Kneeling", 0.3f, 0.7f, true), ("Bandage_Loop", "Pistol_Reload", 0f, 1f, true)
        };

        [MenuItem("Tools/Game/Battle/Build Character")]
        public static void Build()
        {
            SetupSources();
            BattleEditorUtility.EnsureFolder(MeshesFolder);
            BattleEditorUtility.EnsureFolder(TexturesFolder);
            BattleEditorUtility.EnsureFolder(BattleEditorUtility.MaterialsFolder);
            BattleEditorUtility.EnsureFolder(BattleEditorUtility.PrefabsFolder);

            Material peasant = ClothMaterial(PeasantMaterial, "Peasant", PackTexture("Peasant", "T_Peasant_BaseColor"));
            Material ranger = ClothMaterial(RangerMaterial, "Ranger", PackTexture("Ranger", "T_Ranger_BaseColor"));
            ClothMaterial(PeasantAltMaterial, "Peasant", PackTexture("Peasant", "T_Peasant_2_BaseColor"));
            ClothMaterial(RangerAltMaterial, "Ranger", PackTexture("Ranger", "T_Ranger_3_BaseColor"));
            OutfitDyeBuilder.Build();
            GameObject maleSource = LoadSource(MaleBodyPath);
            GameObject femaleSource = LoadSource(FemaleBodyPath);
            GameObject root = new GameObject("Character");
            List<GameObject> sources = new() { maleSource, femaleSource };

            try
            {
                SkinnedMeshRenderer maleSkin = BodySource(maleSource, "SuperHero_Male");
                Transform[] sourceBones = maleSkin.bones;
                float scale = HeadHeight / Array.Find(sourceBones, bone => bone.name == "Head").position.y;
                Transform[] bones = CreateSkeleton(root.transform, sourceBones, scale);

                Transform bodyRoot = BattleEditorUtility.CreateChild("Body", root.transform).transform;
                Transform outfitRoot = BattleEditorUtility.CreateChild("Outfit", root.transform).transform;
                Renderer[] maleBody = CreateBody(maleSource, maleSkin, "Male", bodyRoot, bones, scale);
                Renderer[] femaleBody = CreateBody(femaleSource, BodySource(femaleSource, "Superhero_Female"), "Female", bodyRoot, bones, scale);
                Renderer[] maleParts = new Renderer[s_parts.Length];
                Renderer[] femaleParts = new Renderer[s_parts.Length];
                BodyRegion[] covers = new BodyRegion[s_parts.Length];
                Dictionary<string, GameObject> outfits = new();

                for (int i = 0; i < s_parts.Length; i++)
                {
                    (string outfit, string male, string female, BodyRegion region) = s_parts[i];
                    Material cloth = outfit == "Peasant" ? peasant : ranger;
                    covers[i] = region;
                    maleParts[i] = CreatePart(LoadOutfit(outfits, sources, "Male_" + outfit), "Male_" + male, outfitRoot, bones, scale, cloth, SkinMaterial("Male"));
                    femaleParts[i] = CreatePart(LoadOutfit(outfits, sources, "Female_" + outfit), "Female_" + female, outfitRoot, bones, scale, cloth, SkinMaterial("Female"));
                }

                CharacterModelComponent model = root.AddComponent<CharacterModelComponent>();
                SerializedObject so = new SerializedObject(model);
                BattleEditorUtility.Set(so, "_maleBody", maleBody);
                BattleEditorUtility.Set(so, "_femaleBody", femaleBody);
                BattleEditorUtility.Set(so, "_maleParts", maleParts);
                BattleEditorUtility.Set(so, "_femaleParts", femaleParts);
                BattleEditorUtility.Set(so, "_covers", covers);
                so.ApplyModifiedPropertiesWithoutUndo();
                model.Clear();

                root.AddComponent<Animator>().avatar = Save(CreateAvatar(root), AvatarPath);
                // Freshly created mesh assets must be on disk before the prefab that refers to them is imported.
                AssetDatabase.SaveAssets();
                BattleContentBuilder.SavePrefab(root, BattleEditorUtility.ModelPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[{nameof(BattleCharacterBuilder)}] Character built: {bones.Length} bones, scale {scale:F3}");
            }
            finally
            {
                foreach (GameObject source in sources)
                    Object.DestroyImmediate(source);
            }
        }

        /// Static figure for props: the character dressed with the given parts, posed by one moment of a clip and baked into
        /// a plain mesh. Body replaces the mannequin surface and bare skin, surface (if set) replaces every material.
        public static GameObject CreateFigure(string name, bool isFemale, AnimationClip clip, float time, Material body, Material surface,
            params OutfitPart[] parts)
        {
            GameObject instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BattleEditorUtility.ModelPath), Vector3.zero, Quaternion.identity);
            Dictionary<Material, List<CombineInstance>> groups = new();
            List<Mesh> baked = new();

            try
            {
                CharacterModelComponent model = instance.GetComponent<CharacterModelComponent>();
                model.SetFemale(isFemale);

                foreach (OutfitPart part in parts)
                    model.Show(part, null, Color.white);

                if (body != null)
                    model.SetBodyMaterial(body);

                if (clip != null)
                    BattleEditorUtility.SampleClip(instance.GetComponent<Animator>(), clip, Mathf.Min(time, clip.length));

                foreach (SkinnedMeshRenderer renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    Mesh mesh = new Mesh();
                    renderer.BakeMesh(mesh);
                    baked.Add(mesh);

                    for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    {
                        if (mesh.GetSubMesh(sub).indexCount == 0)
                            continue;

                        Material material = surface != null ? surface : renderer.sharedMaterials[sub];

                        if (!groups.TryGetValue(material, out List<CombineInstance> group))
                            groups[material] = group = new List<CombineInstance>();

                        group.Add(new CombineInstance { mesh = mesh, subMeshIndex = sub, transform = renderer.transform.localToWorldMatrix });
                    }
                }

                List<CombineInstance> merged = new();

                foreach (List<CombineInstance> group in groups.Values)
                {
                    Mesh mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                    mesh.CombineMeshes(group.ToArray(), true, true);
                    baked.Add(mesh);
                    merged.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.identity });
                }

                Mesh figure = GetMesh("Figure_" + name);
                figure.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                figure.CombineMeshes(merged.ToArray(), false, false);
                figure.RecalculateBounds();
                EditorUtility.SetDirty(figure);

                GameObject root = new GameObject(name);
                root.AddComponent<MeshFilter>().sharedMesh = figure;
                MeshRenderer figureRenderer = root.AddComponent<MeshRenderer>();
                figureRenderer.sharedMaterials = new List<Material>(groups.Keys).ToArray();
                figureRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

                return root;
            }
            finally
            {
                Object.DestroyImmediate(instance);

                foreach (Mesh mesh in baked)
                    Object.DestroyImmediate(mesh);
            }
        }

        /// Static T-pose copy of outfit parts (male cut), used to render item icons.
        public static GameObject CreateOutfitModel(string name, Material cloth, OutfitPart[] parts)
        {
            GameObject root = new GameObject(name);

            foreach (OutfitPart part in parts)
            {
                string source = "Male_" + s_parts[(int)part].male;
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{MeshesFolder}/{source}.asset");
                GameObject child = BattleEditorUtility.CreateChild(source, root.transform);
                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                child.AddComponent<MeshRenderer>().sharedMaterials = mesh.subMeshCount == 1 ? new[] { cloth } : new[] { cloth, LoadMaterial("SkinMale") };
            }

            return root;
        }

        public static Material LoadMaterial(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>($"{BattleEditorUtility.MaterialsFolder}/{name}.mat");
        }

        private static void SetupSources()
        {
            SetupModel(AnimationsPath1, true);
            SetupModel(AnimationsPath2, true);
            SetupModel(MaleBodyPath, false);
            SetupModel(FemaleBodyPath, false);

            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { OutfitsFolder + "/Exports/FBX (Unity)/Outfits" }))
                SetupModel(AssetDatabase.GUIDToAssetPath(guid), false);

            foreach (string guid in AssetDatabase.FindAssets("_Normal t:Texture2D", new[] { OutfitsFolder + "/Textures", BaseFolder + "/Textures/Normals Unity - Godot" }))
            {
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));

                if (importer.textureType == TextureImporterType.NormalMap)
                    continue;

                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
        }

        /// Pack setup: bake the axis conversion; animation libraries become humanoid with in-place clips named after their takes.
        private static void SetupModel(string path, bool hasAnimations)
        {
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
            List<ModelImporterClipAnimation> clips = new();

            foreach (ModelImporterClipAnimation take in hasAnimations ? importer.defaultClipAnimations : Array.Empty<ModelImporterClipAnimation>())
            {
                string name = take.takeName.Substring(take.takeName.IndexOf('|') + 1);
                clips.Add(CreateClip(take, name, 0f, 1f, name.EndsWith("_Loop")));

                foreach ((string cut, string source, float start, float end, bool isLoop) in s_cuts)
                {
                    if (source == name)
                        clips.Add(CreateClip(take, cut, start, end, isLoop));
                }
            }

            bool isHuman = importer.animationType == ModelImporterAnimationType.Human && importer.clipAnimations.Length == clips.Count
                && importer.motionNodeName == MotionNode;

            if (!importer.isReadable || !importer.bakeAxisConversion || hasAnimations && !isHuman)
            {
                importer.isReadable = true;
                importer.bakeAxisConversion = true;

                if (hasAnimations)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                    importer.motionNodeName = MotionNode;
                    importer.clipAnimations = clips.ToArray();
                }

                importer.SaveAndReimport();
            }

            if (hasAnimations)
                SetRestPose(path);
        }

        /// Unity's automatic avatar turns the clavicles, upper arms and thumbs into its own T-pose. The character avatar takes
        /// the rest pose as its T-pose, so the libraries must do the same, or every retargeted clip carries that turn.
        private static void SetRestPose(string path)
        {
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
            HumanDescription description = importer.humanDescription;
            SkeletonBone[] skeleton = description.skeleton;
            Transform[] bones = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Transform>();
            bool isRest = true;

            for (int i = 0; i < skeleton.Length; i++)
            {
                string name = skeleton[i].name;
                Transform bone = Array.Find(bones, candidate => candidate.name == name);

                if (bone == null || Quaternion.Angle(skeleton[i].rotation, bone.localRotation) < 0.1f)
                    continue;

                skeleton[i].rotation = bone.localRotation;
                isRest = false;
            }

            if (isRest)
                return;

            description.skeleton = skeleton;
            importer.humanDescription = description;
            importer.SaveAndReimport();
        }

        private static ModelImporterClipAnimation CreateClip(ModelImporterClipAnimation take, string name, float start, float end, bool isLoop)
        {
            return new ModelImporterClipAnimation
            {
                name = name,
                takeName = take.takeName,
                firstFrame = Mathf.Lerp(take.firstFrame, take.lastFrame, start),
                lastFrame = Mathf.Lerp(take.firstFrame, take.lastFrame, end),
                loopTime = isLoop,
                lockRootRotation = true,
                lockRootHeightY = true,
                lockRootPositionXZ = true,
                keepOriginalOrientation = true,
                keepOriginalPositionY = true,
                keepOriginalPositionXZ = true
            };
        }

        /// Source instance turned to face +Z like the rest of the game.
        private static GameObject LoadSource(string path)
        {
            GameObject source = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), Vector3.zero, Quaternion.identity);
            source.hideFlags = HideFlags.HideAndDontSave;

            foreach (Transform bone in source.GetComponentsInChildren<Transform>())
            {
                if (bone.name == "hand_l" && bone.position.x > 0f)
                    source.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            return source;
        }

        private static GameObject LoadOutfit(Dictionary<string, GameObject> outfits, List<GameObject> sources, string name)
        {
            if (outfits.TryGetValue(name, out GameObject outfit))
                return outfit;

            outfit = LoadSource($"{OutfitsFolder}/Exports/FBX (Unity)/Outfits/{name}.fbx");
            outfits.Add(name, outfit);
            sources.Add(outfit);

            return outfit;
        }

        private static Transform[] CreateSkeleton(Transform root, Transform[] sourceBones, float scale)
        {
            Transform[] bones = new Transform[sourceBones.Length];
            int hands = RegionIndex("hand");

            for (int i = 0; i < bones.Length; i++)
            {
                Transform source = sourceBones[i];
                int parent = Array.IndexOf(sourceBones, source.parent);
                bones[i] = BattleEditorUtility.CreateChild(source.name, parent < 0 ? root : bones[parent]).transform;

                // Fingers ride on the relocated wrist; legs keep the mannequin's own joints.
                if (TryGetJoint(source.name, out Vector3 joint))
                    bones[i].position = joint;
                else if (parent >= 0 && RegionIndex(source.parent.name) == hands)
                    bones[i].position = bones[parent].position + (source.position - source.parent.position) * scale;
                else
                    bones[i].position = source.position * scale;
            }

            return bones;
        }

        private static bool TryGetJoint(string bone, out Vector3 position)
        {
            foreach ((string name, Vector3 joint) in s_joints)
            {
                if (bone != name && bone != name + "_l" && bone != name + "_r")
                    continue;

                position = bone.EndsWith("_l") ? Vector3.Scale(joint, new Vector3(-1f, 1f, 1f)) : joint;

                return true;
            }

            position = default;

            return false;
        }

        private static SkinnedMeshRenderer BodySource(GameObject source, string name)
        {
            return Array.Find(source.GetComponentsInChildren<SkinnedMeshRenderer>(), renderer => renderer.name == name);
        }

        /// Body regions of a base character; its eyes and eyebrows ride on the head region.
        private static Renderer[] CreateBody(GameObject character, SkinnedMeshRenderer source, string prefix, Transform parent, Transform[] bones, float scale)
        {
            Material[] materials = { BodyMaterial(prefix) };
            Mesh mesh = source.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            BoneWeight[] weights = mesh.boneWeights;
            Transform[] sourceBones = source.bones;
            Matrix4x4 toWorld = sourceBones[0].localToWorldMatrix * mesh.bindposes[0];
            int calves = RegionIndex("calf");
            int[] boneRegions = Array.ConvertAll(sourceBones, bone => RegionIndex(bone.name));
            Renderer[] renderers = new Renderer[CharacterModelComponent.RegionCount];
            List<int>[][] regions = new List<int>[renderers.Length][];

            for (int i = 0; i < regions.Length; i++)
                regions[i] = new[] { new List<int>() };

            float[] share = new float[regions.Length];

            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                int[] triangles = mesh.GetTriangles(sub);

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Array.Clear(share, 0, share.Length);

                    for (int corner = 0; corner < 3; corner++)
                    {
                        BoneWeight weight = weights[triangles[i + corner]];
                        share[boneRegions[weight.boneIndex0]] += weight.weight0;
                        share[boneRegions[weight.boneIndex1]] += weight.weight1;
                        share[boneRegions[weight.boneIndex2]] += weight.weight2;
                        share[boneRegions[weight.boneIndex3]] += weight.weight3;
                    }

                    int region = Array.IndexOf(share, Mathf.Max(share));

                    if (region == calves && toWorld.MultiplyPoint3x4(vertices[triangles[i]]).y < BootTop)
                        region++;

                    regions[region][0].AddRange(new[] { triangles[i], triangles[i + 1], triangles[i + 2] });
                }
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                string name = $"{prefix}_{(BodyRegion)(1 << i)}";
                renderers[i] = CreateRenderer(name, parent, Extract(source, name, regions[i], bones, scale), bones, materials);
            }

            Transform head = renderers[RegionIndex("Head")].transform;

            foreach ((string part, Material material) in new[] { ("Eyes", EyesMaterial()), ("Eyebrows", EyebrowsMaterial(prefix)) })
            {
                SkinnedMeshRenderer feature = BodySource(character, part);
                string name = $"{prefix}_{part}";
                List<int>[] triangles = { new(feature.sharedMesh.triangles) };
                CreateRenderer(name, head, Extract(feature, name, triangles, bones, scale), bones, new[] { material });
            }

            return renderers;
        }

        private static int RegionIndex(string bone)
        {
            foreach ((string name, BodyRegion region) in s_regions)
            {
                if (bone.StartsWith(name))
                    return Mathf.RoundToInt(Mathf.Log((int)region, 2f));
            }

            return 1;
        }

        /// Cloth goes to material slot 0, bare skin (if the part has any) to slot 1.
        private static Renderer CreatePart(GameObject outfit, string name, Transform parent, Transform[] bones, float scale, Material cloth, Material skin)
        {
            SkinnedMeshRenderer source = Array.Find(outfit.GetComponentsInChildren<SkinnedMeshRenderer>(), renderer => renderer.name == name);

            if (source == null)
                throw new InvalidOperationException($"Outfit part {name} not found");

            Mesh mesh = source.sharedMesh;
            List<int>[] submeshes = new List<int>[mesh.subMeshCount];

            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                bool isSkin = source.sharedMaterials[sub].name.Contains("Regular");
                submeshes[mesh.subMeshCount == 1 || !isSkin ? 0 : 1] = new List<int>(mesh.GetTriangles(sub));
            }

            Material[] materials = submeshes.Length == 1 ? new[] { cloth } : new[] { cloth, skin };
            Renderer renderer = CreateRenderer(name, parent, Extract(source, name, submeshes, bones, scale), bones, materials);
            renderer.gameObject.SetActive(false);

            return renderer;
        }

        private static Renderer CreateRenderer(string name, Transform parent, Mesh mesh, Transform[] bones, Material[] materials)
        {
            SkinnedMeshRenderer renderer = BattleEditorUtility.CreateChild(name, parent).AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh;
            renderer.bones = bones;
            renderer.rootBone = bones[0];
            renderer.localBounds = s_bounds;
            renderer.sharedMaterials = materials;

            return renderer;
        }

        /// Copies the listed triangles into a compact mesh bound to the target skeleton. Every vertex keeps its offset to its
        /// own source joint, so parts made for other proportions follow the target joints instead of floating beside them.
        private static Mesh Extract(SkinnedMeshRenderer source, string name, List<int>[] submeshes, Transform[] bones, float scale)
        {
            Mesh sourceMesh = source.sharedMesh;
            Vector3[] vertices = sourceMesh.vertices;
            Vector3[] normals = sourceMesh.normals;
            Vector4[] tangents = sourceMesh.tangents;
            Vector2[] uvs = sourceMesh.uv;
            BoneWeight[] weights = sourceMesh.boneWeights;
            Matrix4x4[] sourceBind = sourceMesh.bindposes;
            Transform[] sourceBones = source.bones;
            int[] boneMap = Array.ConvertAll(sourceBones, bone => Array.FindIndex(bones, target => target.name == bone.name));
            int[] remap = new int[vertices.Length];
            List<Vector3> newVertices = new();
            List<Vector3> newNormals = new();
            List<Vector4> newTangents = new();
            List<Vector2> newUvs = new();
            List<BoneWeight> newWeights = new();

            foreach (List<int> triangles in submeshes)
            {
                for (int i = 0; i < triangles.Count; i++)
                {
                    int index = triangles[i];

                    if (remap[index] == 0)
                    {
                        BoneWeight weight = weights[index];
                        weight.boneIndex0 = boneMap[weight.boneIndex0];
                        weight.boneIndex1 = boneMap[weight.boneIndex1];
                        weight.boneIndex2 = boneMap[weight.boneIndex2];
                        weight.boneIndex3 = boneMap[weight.boneIndex3];
                        newVertices.Add(vertices[index]);
                        newNormals.Add(normals[index]);
                        newUvs.Add(uvs[index]);
                        newWeights.Add(weight);

                        if (tangents.Length > 0)
                            newTangents.Add(tangents[index]);

                        remap[index] = newVertices.Count;
                    }

                    triangles[i] = remap[index] - 1;
                }
            }

            Matrix4x4[] bind = new Matrix4x4[bones.Length];

            for (int i = 0; i < bind.Length; i++)
                bind[i] = Matrix4x4.identity;

            for (int i = 0; i < sourceBones.Length; i++)
            {
                // Pelvis and clavicles are pivots inside the torso: their skin stays where it is and only turns around the new pivot.
                Matrix4x4 toWorld = sourceBones[i].localToWorldMatrix * sourceBind[i];
                bool isPivot = sourceBones[i].name == "pelvis" || sourceBones[i].name.StartsWith("clavicle");
                bind[boneMap[i]] = isPivot
                    ? Matrix4x4.Translate(-bones[boneMap[i]].position) * Matrix4x4.Scale(Vector3.one * scale) * toWorld
                    : Matrix4x4.Scale(Vector3.one * scale) * Matrix4x4.Translate(-sourceBones[i].position) * toWorld;
            }

            Mesh mesh = GetMesh(name);
            mesh.SetVertices(newVertices);
            mesh.SetNormals(newNormals);
            mesh.SetUVs(0, newUvs);

            if (newTangents.Count > 0)
                mesh.SetTangents(newTangents);

            mesh.subMeshCount = submeshes.Length;

            for (int i = 0; i < submeshes.Length; i++)
                mesh.SetTriangles(submeshes[i], i);

            mesh.boneWeights = newWeights.ToArray();
            mesh.bindposes = bind;
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);

            return mesh;
        }

        /// Mesh assets are refilled in place so references to them survive rebuilds.
        private static Mesh GetMesh(string name)
        {
            string path = $"{MeshesFolder}/{name}.asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (mesh == null)
            {
                mesh = new Mesh { name = name };
                AssetDatabase.CreateAsset(mesh, path);
            }

            mesh.Clear();

            return mesh;
        }

        private static Avatar CreateAvatar(GameObject root)
        {
            List<HumanBone> human = new();

            void Add(string bone, string humanName)
            {
                human.Add(new HumanBone { boneName = bone, humanName = humanName, limit = new HumanLimit { useDefaultValues = true } });
            }

            foreach ((string bone, string humanName) in s_humanBones)
                Add(bone, humanName);

            foreach ((string suffix, string side) in new[] { ("_l", "Left"), ("_r", "Right") })
            {
                foreach ((string bone, string humanName) in s_sideBones)
                    Add(bone + suffix, side + humanName);

                foreach ((string bone, string humanName) in s_fingers)
                {
                    for (int i = 0; i < s_phalanges.Length; i++)
                        Add($"{bone}_0{i + 1}{suffix}", $"{side} {humanName} {s_phalanges[i]}");
                }
            }

            HumanDescription description = new HumanDescription
            {
                human = human.ToArray(),
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

            return avatar;
        }

        public static string PackTexture(string outfit, string name)
        {
            return $"{OutfitsFolder}/Textures/{outfit}/{name}.png";
        }

        /// Cloth of an outfit: any base colour atlas over the normals and the mask of the pack.
        public static Material ClothMaterial(string name, string outfit, string baseColor)
        {
            return TexturedMaterial(name, baseColor, PackTexture(outfit, $"T_{outfit}_Normal"), CreateMask(PackTexture(outfit, $"T_{outfit}_ORM"), true));
        }

        /// Another mask for a cloth material: dyed looks may turn cloth into metal.
        public static void SetMask(Material material, Texture2D mask)
        {
            material.SetTexture("_MetallicGlossMap", mask);
            material.SetTexture("_OcclusionMap", mask);
        }

        private static Material BodyMaterial(string gender)
        {
            string color = gender == "Male" ? "T_Superhero_Male_Dark" : "T_Superhero_Female_Dark_BaseColor";
            string normal = $"{BaseFolder}/Textures/Normals Unity - Godot/T_Superhero_{gender}_Normal.png";

            return TexturedMaterial("Body" + gender, $"{BaseFolder}/Textures/{color}.png", normal,
                CreateMask($"{BaseFolder}/Textures/T_Superhero_{gender}_Roughness.png", false));
        }

        private static Material EyesMaterial()
        {
            Material material = BattleEditorUtility.GetMaterial("BodyEyes", Color.white, 0f, 0.85f);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{BaseFolder}/Textures/T_Eye_Brown.png"));

            return material;
        }

        /// Hair cards: grey atlas tinted by the material, alpha-clipped and two-sided.
        private static Material EyebrowsMaterial(string gender)
        {
            Material material = BattleEditorUtility.GetMaterial("BodyEyebrows" + gender, new Color(0.2f, 0.13f, 0.08f), 0f, 0.2f);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{BaseFolder}/Textures/T_Hair_{(gender == "Male" ? 1 : 2)}_BaseColor.png"));
            material.SetFloat("_AlphaClip", 1f);
            material.SetFloat("_Cutoff", 0.5f);
            material.SetFloat("_Cull", 0f);
            material.EnableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;

            return material;
        }

        private static Material SkinMaterial(string gender)
        {
            string prefix = $"{OutfitsFolder}/Textures/Base/T_Regular_{gender}_";

            return TexturedMaterial("Skin" + gender, prefix + "Dark_BaseColor.png", prefix + "Normal.png", CreateMask(prefix + "Roughness.png", false));
        }

        private static Material TexturedMaterial(string name, string baseColor, string normal, Texture2D mask)
        {
            Material material = BattleEditorUtility.GetMaterial(name, Color.white, 1f, 1f);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(baseColor));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            material.SetTexture("_MetallicGlossMap", mask);
            material.SetTexture("_OcclusionMap", mask);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.EnableKeyword("_OCCLUSIONMAP");

            return material;
        }

        /// URP mask (R metallic, G occlusion, A smoothness) from the pack's ORM (occlusion, roughness, metallic) or plain roughness map.
        internal static Texture2D CreateMask(string sourcePath, bool isOrm)
        {
            string path = $"{TexturesFolder}/{Path.GetFileNameWithoutExtension(sourcePath)}_Mask.png";
            Texture2D mask = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (mask != null)
                return mask;

            SetLinear(sourcePath);
            RenderTexture target = RenderTexture.GetTemporary(MaskSize, MaskSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath), target);
            RenderTexture.active = target;
            Texture2D pixels = new Texture2D(MaskSize, MaskSize, TextureFormat.RGBA32, false, true);
            pixels.ReadPixels(new Rect(0f, 0f, MaskSize, MaskSize), 0, 0);
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(target);
            Color32[] colors = pixels.GetPixels32();

            for (int i = 0; i < colors.Length; i++)
            {
                Color32 color = colors[i];
                colors[i] = isOrm ? new Color32(color.b, color.r, 0, (byte)(255 - color.g)) : new Color32(0, 255, 0, (byte)(255 - color.r));
            }

            pixels.SetPixels32(colors);
            File.WriteAllBytes(path, pixels.EncodeToPNG());
            Object.DestroyImmediate(pixels);
            AssetDatabase.ImportAsset(path);
            SetLinear(path);

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        internal static void SetLinear(string path)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);

            if (!importer.sRGBTexture)
                return;

            importer.sRGBTexture = false;
            importer.SaveAndReimport();
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
