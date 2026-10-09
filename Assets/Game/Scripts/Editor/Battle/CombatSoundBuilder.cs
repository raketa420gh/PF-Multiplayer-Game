using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Scripts.Battle;
using Game.Scripts.Dungeon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Scripts.Editor.Battle
{
    /// Turns the recorded takes picked by the user (Audio/Battle/Weapons/<set>/<surface>_NN.wav, Audio/Dungeon/Steps/<footwear>_NN.wav)
    /// into sound configs and hands them to weapons, bodies, footsteps and the scenes' feedback.
    internal static class CombatSoundBuilder
    {
        private const string WeaponsFolder = "Assets/Game/Audio/Battle/Weapons";
        private const string StepsFolder = "Assets/Game/Audio/Dungeon/Steps";
        private const string SoundsFolder = BattleEditorUtility.ConfigsFolder + "/Sounds";
        private const string FootstepsPath = "Assets/Game/Configs/Dungeon/Footsteps.asset";

        /// Order is the network id: never reorder, only append.
        private static readonly string[] s_sets = { "arming", "viking", "axe", "mace", "morning", "staff", "fists" };

        private static readonly (string Category, string Field)[] s_categories =
        {
            ("swing", "_swing"), ("flesh", "_flesh"), ("bone", "_bone"), ("plate", "_plate"), ("shield", "_shield"), ("clash", "_clash"), ("stone", "_stone"), ("wood", "_wood")
        };

        /// Categories where the user kept no take borrow the nearest family's.
        private static readonly Dictionary<string, string> s_fallbacks = new()
        {
            ["arming/clash"] = "viking/clash", ["arming/shield"] = "viking/shield", ["axe/clash"] = "viking/clash",
            ["mace/clash"] = "mace/plate", ["morning/clash"] = "morning/plate", ["staff/stone"] = "staff/wood", ["fists/clash"] = "fists/flesh"
        };

        private static readonly Dictionary<string, string> s_weapons = new()
        {
            ["ArmingSword"] = "arming", ["SwordEcu"] = "arming", ["SwordShield"] = "arming", ["SwordShieldLeft"] = "arming",
            ["VikingSword"] = "viking", ["VikingEcu"] = "viking", ["VikingShield"] = "viking", ["BattleAxe"] = "axe",
            ["MaceEcu"] = "mace", ["MaceShield"] = "mace", ["MorningStar"] = "morning", ["Staff"] = "staff", ["Fists"] = "fists"
        };

        private static readonly Dictionary<string, (ImpactSurface Body, ImpactSurface Block, Footwear Feet)> s_prefabs = new()
        {
            ["Battle/TrainingDummy"] = (ImpactSurface.Wood, ImpactSurface.Clash, Footwear.Light),
            ["Battle/ShieldDummy"] = (ImpactSurface.Wood, ImpactSurface.Shield, Footwear.Light),
            ["Dungeon/SkeletonWarrior"] = (ImpactSurface.Bone, ImpactSurface.Clash, Footwear.Bare),
            ["Dungeon/SkeletonSwordsman"] = (ImpactSurface.Bone, ImpactSurface.Clash, Footwear.Bare),
            ["Dungeon/SkeletonArcher"] = (ImpactSurface.Bone, ImpactSurface.Clash, Footwear.Bare),
            ["Dungeon/FlyingHead"] = (ImpactSurface.Bone, ImpactSurface.Clash, Footwear.Bare),
            ["Dungeon/Juggernaut"] = (ImpactSurface.Flesh, ImpactSurface.Clash, Footwear.Heavy)
        };

        [MenuItem("Tools/Game/Battle/Build Combat Sounds")]
        public static void Build()
        {
            BattleEditorUtility.EnsureFolder(SoundsFolder);

            for (int i = 0; i < s_sets.Length; i++)
            {
                SerializedObject so = new SerializedObject(BattleEditorUtility.LoadOrCreate<WeaponSoundConfig>($"{SoundsFolder}/{s_sets[i]}.asset"));
                BattleEditorUtility.Set(so, "_id", i + 1);

                foreach ((string category, string field) in s_categories)
                    BattleEditorUtility.Set(so, field, LoadTakes(s_sets[i], category));

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            foreach ((string asset, string set) in s_weapons)
            {
                WeaponConfig weapon = AssetDatabase.LoadAssetAtPath<WeaponConfig>($"{BattleEditorUtility.ConfigsFolder}/{asset}.asset");

                if (weapon != null)
                    BattleEditorUtility.Set(weapon, "_sounds", LoadSet(set));
            }

            SerializedObject steps = new SerializedObject(BattleEditorUtility.LoadOrCreate<FootstepSoundConfig>(FootstepsPath));

            foreach (string footwear in new[] { "bare", "light", "heavy", "plate" })
            {
                AudioClip[] clips = Load(StepsFolder, footwear);
                BattleEditorUtility.Set(steps, "_" + footwear, clips.Length > 0 ? clips : Load(StepsFolder, "light"));
            }

            steps.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            ApplyToPrefabs();
        }

        public static WeaponSoundConfig[] LoadSets()
        {
            return s_sets.Select(LoadSet).ToArray();
        }

        public static FootstepSoundConfig LoadFootsteps()
        {
            return AssetDatabase.LoadAssetAtPath<FootstepSoundConfig>(FootstepsPath);
        }

        /// The feedback of every scene in the build gets the weapon table; scene builders set it on a rebuild.
        public static void ApplyToScenes()
        {
            WeaponSoundConfig[] sets = LoadSets();

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                UnityEngine.SceneManagement.Scene opened = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
                bool isChanged = false;

                foreach (GameObject root in opened.GetRootGameObjects())
                {
                    foreach (BattleFeedback feedback in root.GetComponentsInChildren<BattleFeedback>(true))
                    {
                        SerializedObject so = new SerializedObject(feedback);
                        BattleEditorUtility.Set(so, "_weaponSounds", sets);
                        so.ApplyModifiedPropertiesWithoutUndo();
                        isChanged = true;
                    }
                }

                if (isChanged)
                    EditorSceneManager.SaveScene(opened);
            }
        }

        private static void ApplyToPrefabs()
        {
            FootstepSoundConfig footsteps = LoadFootsteps();

            foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { BattleEditorUtility.PrefabsFolder, "Assets/Game/Prefabs/Dungeon" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                bool isChanged = false;
                string key = path.Replace("Assets/Game/Prefabs/", string.Empty).Replace(".prefab", string.Empty);
                bool isKnown = s_prefabs.TryGetValue(key, out (ImpactSurface Body, ImpactSurface Block, Footwear Feet) look);

                if (isKnown && root.TryGetComponent(out HitFeedbackComponent feedback))
                {
                    SerializedObject so = new SerializedObject(feedback);
                    BattleEditorUtility.Set(so, "_body", look.Body);
                    BattleEditorUtility.Set(so, "_blockSurface", look.Block);
                    so.ApplyModifiedPropertiesWithoutUndo();
                    isChanged = true;
                }

                if (root.TryGetComponent(out FootstepComponent step))
                {
                    SerializedObject so = new SerializedObject(step);
                    BattleEditorUtility.Set(so, "_sounds", footsteps);
                    BattleEditorUtility.Set(so, "_footwear", isKnown ? look.Feet : Footwear.Light);
                    BattleEditorUtility.Set(so, "_inventory", root.GetComponentInChildren<InventoryComponent>(true));
                    so.ApplyModifiedPropertiesWithoutUndo();
                    isChanged = true;
                }

                if (isChanged)
                    PrefabUtility.SaveAsPrefabAsset(root, path);

                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static WeaponSoundConfig LoadSet(string set)
        {
            return AssetDatabase.LoadAssetAtPath<WeaponSoundConfig>($"{SoundsFolder}/{set}.asset");
        }

        private static AudioClip[] LoadTakes(string set, string category)
        {
            AudioClip[] clips = Load($"{WeaponsFolder}/{set}", category);

            if (clips.Length > 0 || !s_fallbacks.TryGetValue($"{set}/{category}", out string fallback))
                return clips;

            string[] parts = fallback.Split('/');

            return Load($"{WeaponsFolder}/{parts[0]}", parts[1]);
        }

        private static AudioClip[] Load(string folder, string category)
        {
            if (!Directory.Exists(folder))
                return new AudioClip[0];

            return Directory.GetFiles(folder, category + "_*.wav").OrderBy(file => file)
                .Select(file => AssetDatabase.LoadAssetAtPath<AudioClip>(file.Replace('\\', '/'))).Where(clip => clip != null).ToArray();
        }
    }
}
