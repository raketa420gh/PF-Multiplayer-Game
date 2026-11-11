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
    /// Turns the recorded takes picked by the user (Audio/Battle/Weapons/<set>/<surface>_NN.wav, Audio/Dungeon/Steps/<bare|leather|plate>_<surface>_NN.wav)
    /// into sound configs and hands them to weapons, bodies, footsteps and the scenes' feedback.
    internal static class CombatSoundBuilder
    {
        private const string WeaponsFolder = "Assets/Game/Audio/Battle/Weapons";
        private const string StepsFolder = "Assets/Game/Audio/Dungeon/Steps";
        private const string SoundsFolder = BattleEditorUtility.ConfigsFolder + "/Sounds";
        private const string ItemsFolder = "Assets/Game/Audio/Dungeon/Items";
        private const string ItemSoundsFolder = SoundsFolder + "/Items";
        private const string FootstepsPath = "Assets/Game/Configs/Dungeon/Footsteps.asset";
        private const string MonsterAudioFolder = "Assets/Game/Audio/Dungeon/Monsters";
        private const string MonstersFolder = "Assets/Game/Configs/Dungeon/Monsters";

        /// Audio/Dungeon/Monsters/<kind>/<Category>_NN.wav → Configs/Dungeon/Monsters/Sounds/<kind>.asset, handed to the monster configs of that kind.
        private static readonly (string Kind, string[] Monsters)[] s_monsterSounds =
        {
            ("Skeleton", new[] { "SkeletonSwordsman", "SkeletonArcher", "SkeletonCrossbowman", "SkeletonWarrior" })
        };

        private static readonly (string Category, string Field)[] s_monsterCategories =
        {
            ("Step", "_steps"), ("Foley", "_foley"), ("Idle", "_idle"), ("Alert", "_alert"), ("AttackVoice", "_attackVoice"), ("AttackWhoosh", "_attackWhoosh"),
            ("Pain", "_pain"), ("Death", "_death"), ("BoneDrop", "_boneDrop"), ("ReloadVoice", "_reloadVoice")
        };

        /// Order is the network id: never reorder, only append.
        private static readonly string[] s_sets = { "arming", "viking", "axe", "mace", "morning", "staff", "fists", "bow", "dagger", "crossbow", "poniard", "zweihander", "club" };

        /// A set borrows its parent's takes for the categories it has none of, apart from the parent's own mechanics (a crossbow is not drawn or restocked like a bow).
        private static readonly Dictionary<string, string> s_parents = new() { ["crossbow"] = "bow", ["poniard"] = "dagger" };
        private static readonly HashSet<string> s_unborrowed = new() { "draw", "creak", "hold", "restock" };

        /// Item sounds a consumable has no takes of its own for are borrowed from a related material or weapon.
        private static readonly Dictionary<string, string> s_itemParents = new()
        {
            ["ale"] = ItemsFolder + "/potion", ["bandage"] = ItemsFolder + "/fabric", ["surgical"] = ItemsFolder + "/leather", ["throwknife"] = WeaponsFolder + "/dagger"
        };
        private static readonly string[] s_itemCategories = { "drop", "pickup", "drag", "place", "equip", "use", "throw", "impact", "ignite", "burn" };

        private static readonly (string Category, string Field)[] s_categories =
        {
            ("swing", "_swing"), ("swing1", "_swing1"), ("swing2", "_swing2"), ("swing3", "_swing3"), ("swing4", "_swing4"), ("riposte", "_riposte"),
            ("flesh", "_flesh"), ("bone", "_bone"), ("plate", "_plate"), ("shield", "_shield"), ("clash", "_clash"), ("stone", "_stone"), ("wood", "_wood"),
            ("head", "_head"), ("handle", "_handle"), ("grass", "_grass"), ("snow", "_snow"), ("woodhard", "_woodHard"),
            ("equip", "_equip"), ("unequip", "_unequip"), ("blockraise", "_blockRaise"), ("drop", "_drop"), ("pickup", "_pickup"), ("drag", "_drag"), ("place", "_place"),
            ("draw", "_draw"), ("creak", "_creak"), ("hold", "_hold"), ("fire", "_fire"), ("firefar", "_fireFar"), ("flight", "_flight"),
            ("flightfar", "_flightFar"), ("nock", "_nock"), ("restock", "_restock")
        };

        /// Share of the trace from the grip that is the haft (WeaponSoundConfig.HandleShare); sets without one never hit with the handle.
        private static readonly Dictionary<string, float> s_handleShares = new() { ["morning"] = 0.6f, ["zweihander"] = 0.15f, ["club"] = 0.45f };

        /// Categories where the user kept no take borrow the nearest family's.
        private static readonly Dictionary<string, string> s_fallbacks = new()
        {
            ["arming/clash"] = "viking/clash", ["arming/shield"] = "viking/shield", ["axe/clash"] = "viking/clash",
            ["mace/clash"] = "mace/plate", ["morning/clash"] = "morning/plate", ["staff/stone"] = "staff/wood", ["fists/clash"] = "fists/flesh",
            ["bow/bone"] = "bow/woodhard", ["bow/clash"] = "bow/plate", ["bow/shield"] = "bow/woodhard",
            ["dagger/bone"] = "arming/bone", ["dagger/plate"] = "arming/plate",
            ["zweihander/bone"] = "arming/bone", ["zweihander/plate"] = "arming/plate", ["zweihander/shield"] = "viking/shield",
            ["club/bone"] = "staff/bone", ["club/plate"] = "staff/plate", ["club/shield"] = "staff/shield"
        };

        private static readonly Dictionary<string, string> s_weapons = new()
        {
            ["ArmingSword"] = "arming", ["SwordEcu"] = "arming", ["SwordShield"] = "arming", ["SwordShieldLeft"] = "arming",
            ["VikingSword"] = "viking", ["VikingEcu"] = "viking", ["VikingShield"] = "viking", ["BattleAxe"] = "axe",
            ["MaceEcu"] = "mace", ["MaceShield"] = "mace", ["MorningStar"] = "morning", ["Staff"] = "staff", ["Fists"] = "fists", ["Bow"] = "bow", ["Crossbow"] = "crossbow",
            ["Misericorde"] = "dagger", ["MisericordeShield"] = "dagger", ["MisericordeEcu"] = "dagger", ["Poniard"] = "poniard",
            ["Zweihander"] = "zweihander", ["Club"] = "club", ["ClubShield"] = "club", ["ClubEcu"] = "club"
        };

        private static readonly Dictionary<string, (ImpactSurface Body, ImpactSurface Block, Footwear Feet)> s_prefabs = new()
        {
            ["Dungeon/SkeletonWarrior"] = (ImpactSurface.Bone, ImpactSurface.Clash, Footwear.Bare),
            ["Dungeon/SkeletonSwordsman"] = (ImpactSurface.Bone, ImpactSurface.Clash, Footwear.Bare),
            ["Dungeon/SkeletonArcher"] = (ImpactSurface.Bone, ImpactSurface.Clash, Footwear.Bare),
            ["Dungeon/SkeletonCrossbowman"] = (ImpactSurface.Bone, ImpactSurface.Clash, Footwear.Bare),
            ["Dungeon/FlyingHead"] = (ImpactSurface.Bone, ImpactSurface.Clash, Footwear.Bare),
            ["Dungeon/Juggernaut"] = (ImpactSurface.Flesh, ImpactSurface.Clash, Footwear.Heavy)
        };

        [MenuItem("Tools/Game/Battle/Build Combat Sounds")]
        public static void Build()
        {
            BattleEditorUtility.EnsureFolder(SoundsFolder);
            KeepLoudness($"{WeaponsFolder}/bow");
            KeepLoudness($"{WeaponsFolder}/crossbow");

            for (int i = 0; i < s_sets.Length; i++)
            {
                SerializedObject so = new SerializedObject(BattleEditorUtility.LoadOrCreate<WeaponSoundConfig>($"{SoundsFolder}/{s_sets[i]}.asset"));
                BattleEditorUtility.Set(so, "_id", i + 1);
                BattleEditorUtility.Set(so, "_handleShare", s_handleShares.GetValueOrDefault(s_sets[i]));

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
            FootstepSurface[] surfaces = (FootstepSurface[])System.Enum.GetValues(typeof(FootstepSurface));
            steps.FindProperty("_surfaces").arraySize = surfaces.Length;
            KeepLoudness(StepsFolder);

            for (int i = 0; i < surfaces.Length; i++)
            {
                string entry = $"_surfaces.Array.data[{i}]";
                BattleEditorUtility.Set(steps, entry + "._surface", surfaces[i]);

                foreach (string shoe in new[] { "bare", "leather", "plate" })
                    BattleEditorUtility.Set(steps, $"{entry}._{shoe}", Load(StepsFolder, $"{shoe}_{surfaces[i].ToString().ToLowerInvariant()}"));
            }

            steps.ApplyModifiedPropertiesWithoutUndo();
            BuildMonsterSounds();
            AssetDatabase.SaveAssets();
            ApplyToPrefabs();
        }

        public static WeaponSoundConfig[] LoadSets()
        {
            return s_sets.Select(LoadSet).ToArray();
        }

        /// Handling and use sounds of a non-weapon item family (Audio/Dungeon/Items/<set>/<category>_NN.wav, s_itemCategories); no network id, items play them by item id.
        public static WeaponSoundConfig LoadItemSet(string set)
        {
            string folder = $"{ItemsFolder}/{set}";
            BattleEditorUtility.EnsureFolder(ItemSoundsFolder);

            if (Directory.Exists(folder))
                KeepLoudness(folder);

            WeaponSoundConfig config = BattleEditorUtility.LoadOrCreate<WeaponSoundConfig>($"{ItemSoundsFolder}/{set}.asset");

            foreach (string category in s_itemCategories)
            {
                AudioClip[] clips = Load(folder, category);
                BattleEditorUtility.Set(config, "_" + category, clips.Length == 0 && s_itemParents.TryGetValue(set, out string parent) ? Load(parent, category) : clips);
            }

            return config;
        }

        public static FootstepSoundConfig LoadFootsteps()
        {
            return AssetDatabase.LoadAssetAtPath<FootstepSoundConfig>(FootstepsPath);
        }

        /// The feedback of every scene in the build gets the weapon table; scene builders set it on a rebuild.
        [MenuItem("Tools/Game/Battle/Apply Combat Sounds To Scenes")]
        public static void ApplyToScenes()
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                UnityEngine.SceneManagement.Scene opened = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
                // Loaded after the scene opens: opening one unloads the sets loaded before and they would be saved as empty references.
                WeaponSoundConfig[] sets = LoadSets();
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

        private static void BuildMonsterSounds()
        {
            BattleEditorUtility.EnsureFolder($"{MonstersFolder}/Sounds");

            foreach ((string kind, string[] monsters) in s_monsterSounds)
            {
                string folder = $"{MonsterAudioFolder}/{kind}";
                KeepLoudness(folder);
                SerializedObject so = new SerializedObject(BattleEditorUtility.LoadOrCreate<MonsterSoundConfig>($"{MonstersFolder}/Sounds/{kind}.asset"));

                foreach ((string category, string field) in s_monsterCategories)
                    BattleEditorUtility.Set(so, field, Load(folder, category));

                so.ApplyModifiedPropertiesWithoutUndo();

                foreach (string monster in monsters)
                {
                    MonsterConfig config = AssetDatabase.LoadAssetAtPath<MonsterConfig>($"{MonstersFolder}/{monster}.asset");

                    if (config != null)
                        BattleEditorUtility.Set(config, "_sounds", so.targetObject);
                }
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

                if (root.TryGetComponent(out CampfireComponent campfire))
                {
                    BattleEditorUtility.Set(campfire, "_sounds", LoadItemSet("campfire"));
                    isChanged = true;
                }

                if (root.TryGetComponent(out FootstepComponent step))
                {
                    SerializedObject so = new SerializedObject(step);
                    BattleEditorUtility.Set(so, "_sounds", footsteps);
                    BattleEditorUtility.Set(so, "_footwear", isKnown ? look.Feet : Footwear.Light);
                    BattleEditorUtility.Set(so, "_inventory", root.GetComponentInChildren<InventoryComponent>(true));
                    MonsterSoundConfig monsterSounds = root.TryGetComponent(out MonsterComponent monster) && monster.Config != null ? monster.Config.Sounds : null;
                    BattleEditorUtility.Set(so, "_ownSteps", monsterSounds != null ? monsterSounds.Steps : new AudioClip[0]);
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

            if (clips.Length == 0 && s_parents.TryGetValue(set, out string parent) && !s_unborrowed.Contains(category))
                return LoadTakes(parent, category);

            if (clips.Length > 0 || !s_fallbacks.TryGetValue($"{set}/{category}", out string fallback))
                return clips;

            string[] parts = fallback.Split('/');

            return Load($"{WeaponsFolder}/{parts[0]}", parts[1]);
        }

        /// The takes are levelled against each other as in Dark and Darker: the importer must not normalize them.
        private static void KeepLoudness(string folder)
        {
            foreach (string file in Directory.GetFiles(folder, "*.wav"))
            {
                AssetImporter importer = AssetImporter.GetAtPath(file.Replace('\\', '/'));

                // Takes copied in after the last refresh are not imported yet.
                if (importer == null)
                    continue;

                SerializedObject so = new SerializedObject(importer);
                SerializedProperty normalize = so.FindProperty("m_Normalize");

                if (!normalize.boolValue)
                    continue;

                normalize.boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
                importer.SaveAndReimport();
            }
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
