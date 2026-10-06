using System;
using System.Collections.Generic;
using Fusion;
using Game.Scripts.Battle;
using Game.Scripts.Editor.Dungeon;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Scripts.Editor.Battle
{
    internal static class BattleContentBuilder
    {
        public const string FighterPath = BattleEditorUtility.PrefabsFolder + "/Fighter.prefab";
        public const string BotPath = BattleEditorUtility.PrefabsFolder + "/BotFighter.prefab";
        public const string DummyPath = BattleEditorUtility.PrefabsFolder + "/TrainingDummy.prefab";
        public const string ShieldDummyPath = BattleEditorUtility.PrefabsFolder + "/ShieldDummy.prefab";

        /// Block hitbox riding on the animated weapon socket; Center/Rotation are the body-space block pose for static dummies.
        internal sealed class BlockBox
        {
            public WeaponSocket Socket;
            public Vector3 LocalCenter;
            public Vector3 Extents;
            public Vector3 Center;
            public Quaternion Rotation;
            public bool IsRound;
        }

        internal sealed class Loadout
        {
            public string Name;
            public WeaponConfig Config;
            public BlockBox Block;
        }

        /// Everything the fighter prefab is made of, so other builders can add components before saving.
        internal sealed class FighterParts
        {
            public GameObject Root;
            public Animator Animator;
            public CharacterModelComponent Model;
            public HealthComponent Health;
            public FighterMoveComponent Move;
            public FighterBodyComponent Body;
            public CombatComponent Combat;
            public DamageReceiverComponent Receiver;
            public ProjectileComponent Projectiles;
            public FighterComponent Fighter;
            public HitboxRoot HitboxRoot;
            public Transform[] Sockets;
        }

        [MenuItem("Tools/Game/Battle/Build Content")]
        public static void Build()
        {
            Loadout[] loadouts = BuildWeapons(out GameObject arrow, out GameObject orb);
            HitZoneConfig zones = BattleEditorUtility.LoadOrCreate<HitZoneConfig>($"{BattleEditorUtility.ConfigsFolder}/HitZones.asset");

            FighterParts parts = CreateFighter(loadouts, arrow, orb, 4, "Fighter");
            GameObject fighter = SavePrefab(parts.Root, FighterPath);
            BuildBot(fighter);
            GameObject dummy = BuildDummy(zones, null, null);
            BuildDummy(zones, loadouts[0], AssetDatabase.LoadAssetAtPath<GameObject>($"{BattleEditorUtility.PrefabsFolder}/Shield.prefab"));

            AssetDatabase.SaveAssets();
            Debug.Log($"[{nameof(BattleContentBuilder)}] Content built: {fighter.name}, {dummy.name}");
        }

        /// Builds weapon visuals and configs for the full catalog, in DungeonWeaponLibrary.CatalogOrder.
        internal static Loadout[] BuildWeapons(out GameObject arrow, out GameObject magicOrb)
        {
            BattleEditorUtility.EnsureFolder(BattleEditorUtility.ConfigsFolder);
            BattleEditorUtility.EnsureFolder(BattleEditorUtility.PrefabsFolder);
            BattleEditorUtility.EnsureFolder(BattleEditorUtility.MaterialsFolder);
            BattleEditorUtility.EnsureLayer(BattleEditorUtility.HitboxLayer);

            GameObject sword = BattleWeaponPrefabBuilder.BuildSword("Sword", 0.12f, 0.9f, 0.052f, 0.2f, 0.08f, SwordStyle.Arming);
            GameObject falchion = BattleWeaponPrefabBuilder.BuildSword("Falchion", 0.12f, 0.95f, 0.062f, 0.16f, 0.08f, SwordStyle.Falchion);
            GameObject longsword = BattleWeaponPrefabBuilder.BuildSword("Longsword", 0.18f, 1.05f, 0.05f, 0.26f, 0.18f, SwordStyle.Longsword);
            GameObject greatsword = BattleWeaponPrefabBuilder.BuildSword("Greatsword", 0.18f, 1.35f, 0.056f, 0.34f, 0.22f, SwordStyle.Greatsword);
            GameObject dagger = BattleWeaponPrefabBuilder.BuildSword("Dagger", 0.06f, 0.42f, 0.03f, 0.075f, 0.06f, SwordStyle.Rondel);
            GameObject axe = DungeonWeaponPrefabBuilder.BuildAxe("BattleAxe", 0.92f, 0.3f, AxeStyle.Battle);
            GameObject mace = DungeonWeaponPrefabBuilder.BuildMace();
            GameObject spear = DungeonWeaponPrefabBuilder.BuildSpear();
            GameObject staff = DungeonWeaponPrefabBuilder.BuildStaff();
            GameObject torch = DungeonWeaponPrefabBuilder.BuildTorch();
            GameObject crossbow = DungeonWeaponPrefabBuilder.BuildCrossbow();
            GameObject shield = BattleWeaponPrefabBuilder.BuildShield();
            GameObject bow = BattleWeaponPrefabBuilder.BuildBow();
            GameObject book = DungeonWeaponPrefabBuilder.BuildBook();
            GameObject lute = DungeonWeaponPrefabBuilder.BuildLute();
            GameObject shortSword = BattleWeaponPrefabBuilder.BuildSword("ShortSword", 0.1f, 0.72f, 0.056f, 0.16f, 0.08f, SwordStyle.Short);
            GameObject rapier = BattleWeaponPrefabBuilder.BuildSword("Rapier", 0.12f, 1f, 0.022f, 0.14f, 0.08f, SwordStyle.Rapier);
            GameObject vikingSword = BattleWeaponPrefabBuilder.BuildSword("VikingSword", 0.12f, 0.92f, 0.058f, 0.11f, 0.09f, SwordStyle.Viking);
            GameObject hatchet = DungeonWeaponPrefabBuilder.BuildAxe("Hatchet", 0.62f, 0.2f, AxeStyle.Hatchet);
            GameObject morningStar = DungeonWeaponPrefabBuilder.BuildMorningStar();
            GameObject castillon = BattleWeaponPrefabBuilder.BuildSword("CastillonDagger", 0.06f, 0.5f, 0.046f, 0.1f, 0.06f, SwordStyle.Castillon);
            GameObject stiletto = BattleWeaponPrefabBuilder.BuildSword("Stiletto", 0.06f, 0.46f, 0.014f, 0.07f, 0.06f, SwordStyle.Stiletto);
            GameObject fellingAxe = DungeonWeaponPrefabBuilder.BuildAxe("FellingAxe", 0.94f, 0.24f, AxeStyle.Felling);
            GameObject maul = DungeonWeaponPrefabBuilder.BuildMaul();
            GameObject halberd = DungeonWeaponPrefabBuilder.BuildHalberd();
            GameObject horsemansAxe = DungeonWeaponPrefabBuilder.BuildHorsemansAxe();
            GameObject quarterstaff = DungeonWeaponPrefabBuilder.BuildQuarterstaff();
            GameObject bardiche = DungeonWeaponPrefabBuilder.BuildBardiche();
            arrow = BattleWeaponPrefabBuilder.BuildArrow();
            magicOrb = DungeonWeaponPrefabBuilder.BuildMagicOrb();

            WeaponDefinition[] definitions = DungeonWeaponLibrary.CreateAll();
            Dictionary<string, (GameObject prefab, WeaponSocket socket)[]> attachments = new()
            {
                [DungeonWeaponLibrary.SwordShield] = new[] { (sword, WeaponSocket.RightHand), (shield, WeaponSocket.LeftShield) },
                [DungeonWeaponLibrary.Greatsword] = new[] { (greatsword, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.Bow] = new[] { (bow, WeaponSocket.LeftHand) },
                [DungeonWeaponLibrary.SwordShieldLeft] = new[] { (sword, WeaponSocket.LeftHand), (shield, WeaponSocket.RightShield) },
                [DungeonWeaponLibrary.Fists] = Array.Empty<(GameObject, WeaponSocket)>(),
                [DungeonWeaponLibrary.ArmingSword] = new[] { (sword, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.Falchion] = new[] { (falchion, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.Longsword] = new[] { (longsword, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.BattleAxe] = new[] { (axe, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.Spear] = new[] { (spear, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.Mace] = new[] { (mace, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.Dagger] = new[] { (dagger, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.Crossbow] = new[] { (crossbow, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.Staff] = new[] { (staff, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.Torch] = new[] { (torch, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.MaceShield] = new[] { (mace, WeaponSocket.RightHand), (shield, WeaponSocket.LeftShield) },
                [DungeonWeaponLibrary.Spellbook] = new[] { (book, WeaponSocket.LeftHand) },
                [DungeonWeaponLibrary.Lute] = new[] { (lute, WeaponSocket.LeftHand) },
                [DungeonWeaponLibrary.BearClaws] = Array.Empty<(GameObject, WeaponSocket)>(),
                [DungeonWeaponLibrary.PantherClaws] = Array.Empty<(GameObject, WeaponSocket)>(),
                [DungeonWeaponLibrary.RatBite] = Array.Empty<(GameObject, WeaponSocket)>(),
                [DungeonWeaponLibrary.ShortSword] = new[] { (shortSword, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.Rapier] = new[] { (rapier, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.VikingSword] = new[] { (vikingSword, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.Hatchet] = new[] { (hatchet, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.MorningStar] = new[] { (morningStar, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.CastillonDagger] = new[] { (castillon, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.Stiletto] = new[] { (stiletto, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.FellingAxe] = new[] { (fellingAxe, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.WarMaul] = new[] { (maul, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.Halberd] = new[] { (halberd, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.HorsemansAxe] = new[] { (horsemansAxe, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.Quarterstaff] = new[] { (quarterstaff, WeaponSocket.RightHand) },
                [DungeonWeaponLibrary.Bardiche] = new[] { (bardiche, WeaponSocket.RightHand) }
            };

            string[] order = DungeonWeaponLibrary.CatalogOrder;
            Loadout[] loadouts = new Loadout[order.Length];

            using (TraceSampler sampler = new TraceSampler())
            {
                for (int i = 0; i < order.Length; i++)
                {
                    string name = order[i];
                    bool isLeft = name == DungeonWeaponLibrary.SwordShieldLeft;
                    WeaponDefinition definition = FindDefinition(definitions, name);
                    loadouts[i] = CreateWeapon(sampler, name, definition, isLeft ? HandSide.Left : HandSide.Right, attachments[name]);
                }
            }

            MovementConfig movement = BattleEditorUtility.LoadOrCreate<MovementConfig>($"{BattleEditorUtility.ConfigsFolder}/Movement.asset");
            SerializedObject so = new SerializedObject(movement);
            BattleEditorUtility.Set(so, "_runSpeed", 3.36f);
            BattleEditorUtility.Set(so, "_walkMultiplier", 0.4f);
            BattleEditorUtility.Set(so, "_crouchMultiplier", 0.65f);
            BattleEditorUtility.Set(so, "_backpedalMultiplier", 0.6f);
            BattleEditorUtility.Set(so, "_crouchHeight", 1.4f);
            so.ApplyModifiedPropertiesWithoutUndo();

            return loadouts;
        }

        /// Definitions are matched by display identity: entries sharing clips keep their own damage through a dedicated definition.
        private static WeaponDefinition FindDefinition(WeaponDefinition[] definitions, string name)
        {
            string prefix = DungeonWeaponLibrary.SharedPrefix(name);
            string displayName = DungeonWeaponLibrary.VariantName(name);

            foreach (WeaponDefinition definition in definitions)
            {
                if (definition.Prefix == prefix && (displayName == null ? !DungeonWeaponLibrary.IsVariant(definition) : definition.DisplayName == displayName))
                    return definition;
            }

            return DungeonWeaponLibrary.Find(definitions, prefix);
        }

        private static Loadout CreateWeapon(TraceSampler sampler, string assetName, WeaponDefinition definition, HandSide mainHand,
            params (GameObject prefab, WeaponSocket socket)[] attachments)
        {
            WeaponConfig config = BattleEditorUtility.LoadOrCreate<WeaponConfig>($"{BattleEditorUtility.ConfigsFolder}/{assetName}.asset");
            SerializedObject so = new SerializedObject(config);
            bool isMirrored = mainHand == HandSide.Left;

            BattleEditorUtility.Set(so, "_displayName", isMirrored ? definition.DisplayName + " (left hand)" : definition.DisplayName);
            BattleEditorUtility.Set(so, "_kind", definition.Kind);
            BattleEditorUtility.Set(so, "_mainHand", mainHand);
            BattleEditorUtility.Set(so, "_animationPrefix", definition.Prefix);
            BattleEditorUtility.Set(so, "_deflectDuration", definition.DeflectDuration);
            BattleEditorUtility.Set(so, "_reach", definition.Reach);
            BattleEditorUtility.Set(so, "_damageType", DamageType.Physical);
            (int impact, int stability) = DungeonWeaponLibrary.Force(definition.DisplayName);
            BattleEditorUtility.Set(so, "_impact", impact);
            BattleEditorUtility.Set(so, "_block._stability", stability);

            so.FindProperty("_attachments").arraySize = attachments.Length;

            for (int i = 0; i < attachments.Length; i++)
            {
                BattleEditorUtility.Set(so, $"_attachments.Array.data[{i}]._prefab", attachments[i].prefab);
                BattleEditorUtility.Set(so, $"_attachments.Array.data[{i}]._socket", attachments[i].socket);
            }

            so.FindProperty("_attacks").arraySize = definition.Attacks.Length;

            for (int i = 0; i < definition.Attacks.Length; i++)
                SetAttack(sampler, so, $"_attacks.Array.data[{i}].", assetName, definition, definition.Attacks[i], FighterAnimComponent.AttackSuffix + i);

            BattleEditorUtility.Set(so, "_hasRiposte", definition.Riposte != null);

            if (definition.Riposte != null)
                SetAttack(sampler, so, "_riposte.", assetName, definition, definition.Riposte, FighterAnimComponent.RiposteSuffix);

            BattleEditorUtility.Set(so, "_block._canBlock", definition.CanBlock);

            if (definition.CanBlock)
            {
                BattleEditorUtility.Set(so, "_block._raiseTime", definition.BlockRaise);
                BattleEditorUtility.Set(so, "_block._mitigation", definition.BlockMitigation);
                BattleEditorUtility.Set(so, "_block._impactDuration", definition.BlockImpact);
                BattleEditorUtility.Set(so, "_block._recoveryDuration", definition.BlockRecovery);
                BattleEditorUtility.Set(so, "_block._angleTolerance", definition.BlockAngle);
                BattleEditorUtility.Set(so, "_block._moveMultiplier", definition.BlockMove);
            }

            BattleEditorUtility.Set(so, "_ranged._fullDrawTime", definition.DrawTime > 0f ? definition.DrawTime : BattleAnimationLibrary.FullDrawTime);
            BattleEditorUtility.Set(so, "_ranged._reloadTime", definition.ReloadTime > 0f ? definition.ReloadTime : BattleAnimationLibrary.ReloadTime);
            BattleEditorUtility.Set(so, "_ranged._minSpeed", definition.ArrowMinSpeed > 0f ? definition.ArrowMinSpeed : BattleAnimationLibrary.ArrowMinSpeed);
            BattleEditorUtility.Set(so, "_ranged._maxSpeed", definition.ArrowMaxSpeed > 0f ? definition.ArrowMaxSpeed : BattleAnimationLibrary.ArrowMaxSpeed);
            BattleEditorUtility.Set(so, "_ranged._minDamage", definition.ArrowMinDamage > 0 ? definition.ArrowMinDamage : 14);
            BattleEditorUtility.Set(so, "_ranged._maxDamage", definition.ArrowMaxDamage > 0 ? definition.ArrowMaxDamage : 31);
            BattleEditorUtility.Set(so, "_ranged._gravity", BattleAnimationLibrary.ArrowGravity);
            so.ApplyModifiedPropertiesWithoutUndo();

            return new Loadout
            {
                Name = assetName,
                Config = config,
                Block = definition.CanBlock && definition.Prefix != DungeonWeaponLibrary.Fists ? sampler.SampleBlock(definition, isMirrored, attachments) : null
            };
        }

        private static void SetAttack(TraceSampler sampler, SerializedObject so, string path, string assetName, WeaponDefinition definition,
            AttackDefinition attack, string suffix)
        {
            sampler.SampleTrace(definition, attack, definition.Prefix + suffix, out List<Vector3> traceBase, out List<Vector3> traceTip);
            CheckPeak(assetName + suffix, attack, traceBase, traceTip);

            BattleEditorUtility.Set(so, path + "_windupTime", attack.Windup);
            BattleEditorUtility.Set(so, path + "_activeTime", attack.Active);
            BattleEditorUtility.Set(so, path + "_recoveryTime", attack.Recovery);
            BattleEditorUtility.Set(so, path + "_comboWindowStart", attack.ComboStart);
            BattleEditorUtility.Set(so, path + "_comboWindowEnd", attack.ComboEnd);
            BattleEditorUtility.Set(so, path + "_damage", attack.Damage);
            BattleEditorUtility.Set(so, path + "_moveMultiplier", attack.MoveMultiplier);
            BattleEditorUtility.Set(so, path + "_staggerDuration", attack.Stagger);
            BattleEditorUtility.Set(so, path + "_traceSampleRate", BattleAnimationBuilder.FrameRate);
            BattleEditorUtility.Set(so, path + "_traceBase", traceBase);
            BattleEditorUtility.Set(so, path + "_traceTip", traceTip);
        }

        /// The baked blade must cross the crosshair ray at the peak of the swing, or aimed hits would not register.
        private static void CheckPeak(string swing, AttackDefinition attack, List<Vector3> traceBase, List<Vector3> traceTip)
        {
            const float tolerance = 0.01f;
            int sample = Mathf.RoundToInt(BattleAnimationLibrary.PeakTime(attack) * BattleAnimationBuilder.FrameRate);
            Vector2 eye = BattleAnimationLibrary.Eye;
            Vector2 start = traceBase[sample];
            Vector2 end = traceTip[sample];
            Vector2 blade = end - start;
            float along = Mathf.Clamp01(Vector2.Dot(eye - start, blade) / Mathf.Max(blade.sqrMagnitude, 1e-6f));
            float miss = Vector2.Distance(eye, start + blade * along);

            if (miss > tolerance)
                Debug.LogError($"[{nameof(BattleContentBuilder)}] {swing}: the peak misses the crosshair by {miss:0.000} m");
        }

        /// The simulation body is authored data, not measured on the model: swapping the character must not move the
        /// hitbox pivots or the eye. The character builder scales the model to this body instead.
        private static BodyConfig CreateBodyConfig()
        {
            BodyConfig config = BattleEditorUtility.LoadOrCreate<BodyConfig>($"{BattleEditorUtility.ConfigsFolder}/Body.asset");
            SerializedObject so = new SerializedObject(config);
            BattleEditorUtility.Set(so, "_spinePivots", new[] { new Vector3(0f, 1.1f, -0.02f), new Vector3(0f, 1.225f, -0.03f), new Vector3(0f, 1.36f, -0.045f) });
            BattleEditorUtility.Set(so, "_eyePoint", BattleAnimationLibrary.Eye);
            BattleEditorUtility.Set(so, "_crouchDrop", BattleAnimationLibrary.CrouchDrop);
            so.ApplyModifiedPropertiesWithoutUndo();

            return config;
        }

        /// Assembles an unsaved fighter with the whole weapon catalog, hitboxes, animation, camera and views.
        internal static FighterParts CreateFighter(Loadout[] loadouts, GameObject arrow, GameObject magicOrb, int slotCount, string name)
        {
            BodyConfig bodyConfig = CreateBodyConfig();
            MovementConfig movement = BattleEditorUtility.LoadOrCreate<MovementConfig>($"{BattleEditorUtility.ConfigsFolder}/Movement.asset");
            HitZoneConfig zones = BattleEditorUtility.LoadOrCreate<HitZoneConfig>($"{BattleEditorUtility.ConfigsFolder}/HitZones.asset");
            int hitboxLayer = LayerMask.NameToLayer(BattleEditorUtility.HitboxLayer);
            GameObject root = new GameObject(name) { layer = LayerMask.NameToLayer(BattleEditorUtility.CharacterLayer) };

            CharacterController collider = root.AddComponent<CharacterController>();
            collider.height = 1.85f;
            collider.radius = 0.3f;
            collider.center = new Vector3(0f, 0.925f, 0f);
            collider.stepOffset = 0.3f;
            collider.skinWidth = 0.03f;
            CapsuleCollider blocker = BattleEditorUtility.CreateBlocker(root.transform, BattleEditorUtility.BlockerRadius, collider.height);

            root.AddComponent<NetworkObject>();
            NetworkCharacterController controller = root.AddComponent<NetworkCharacterController>();
            HealthComponent health = root.AddComponent<HealthComponent>();
            FighterMoveComponent move = root.AddComponent<FighterMoveComponent>();
            FighterBodyComponent body = root.AddComponent<FighterBodyComponent>();
            HitboxRoot hitboxRoot = root.AddComponent<HitboxRoot>();
            DamageReceiverComponent receiver = root.AddComponent<DamageReceiverComponent>();
            ProjectileComponent projectiles = root.AddComponent<ProjectileComponent>();
            CombatComponent combat = root.AddComponent<CombatComponent>();
            FighterComponent fighter = root.AddComponent<FighterComponent>();

            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(BattleEditorUtility.ModelPath), root.transform);
            model.name = "Model";
            BattleEditorUtility.SetLayerRecursively(model, root.layer);

            Animator animator = model.GetComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(BattleEditorUtility.ControllerPath);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            Transform[] sockets = BattlePoseRig.CreateSockets(animator);

            Transform hitboxes = BattleEditorUtility.CreateChild("Hitboxes", root.transform).transform;
            List<Transform> upper = new List<Transform>
            {
                CreateSphereHitbox(hitboxes, hitboxRoot, "Head", HitZone.Head, new Vector3(0f, 1.74f, 0f), 0.14f, hitboxLayer).transform,
                CreateBoxHitbox(hitboxes, hitboxRoot, "Chest", HitZone.Torso, new Vector3(0f, 1.38f, -0.02f), new Vector3(0.22f, 0.2f, 0.14f), hitboxLayer).transform
            };
            Transform[] lower =
            {
                CreateBoxHitbox(hitboxes, hitboxRoot, "Abdomen", HitZone.Torso, new Vector3(0f, 1.08f, -0.01f), new Vector3(0.18f, 0.12f, 0.12f), hitboxLayer).transform,
                CreateBoxHitbox(hitboxes, hitboxRoot, "Thighs", HitZone.Legs, new Vector3(0f, 0.7f, 0f), new Vector3(0.19f, 0.25f, 0.13f), hitboxLayer).transform
            };
            CreateBoxHitbox(hitboxes, hitboxRoot, "Shins", HitZone.Legs, new Vector3(0f, 0.24f, 0.02f), new Vector3(0.17f, 0.24f, 0.13f), hitboxLayer);

            Hitbox[] blockHitboxes = new Hitbox[loadouts.Length];
            WeaponConfig[] configs = new WeaponConfig[loadouts.Length];

            for (int i = 0; i < loadouts.Length; i++)
            {
                configs[i] = loadouts[i].Config;
                BlockBox block = loadouts[i].Block;

                if (block == null)
                    continue;

                blockHitboxes[i] = CreateBoxHitbox(sockets[(int)block.Socket], hitboxRoot, "Block" + i, HitZone.Block, block.LocalCenter, block.Extents, hitboxLayer);
                BattleEditorUtility.Set(blockHitboxes[i], "_isRound", block.IsRound);
            }

            hitboxRoot.InitHitboxes();
            hitboxRoot.BroadRadius = 2.2f;
            hitboxRoot.Offset = new Vector3(0f, 1f, 0f);

            SerializedObject so = new SerializedObject(move);
            BattleEditorUtility.Set(so, "_config", movement);
            BattleEditorUtility.Set(so, "_controller", controller);
            BattleEditorUtility.Set(so, "_collider", collider);
            BattleEditorUtility.Set(so, "_blocker", blocker);
            so.ApplyModifiedPropertiesWithoutUndo();

            so = new SerializedObject(body);
            BattleEditorUtility.Set(so, "_config", bodyConfig);
            BattleEditorUtility.Set(so, "_move", move);
            BattleEditorUtility.Set(so, "_upperHitboxes", upper);
            BattleEditorUtility.Set(so, "_lowerHitboxes", lower);
            so.ApplyModifiedPropertiesWithoutUndo();

            SetupReceiver(receiver, health, hitboxRoot, zones);

            so = new SerializedObject(projectiles);
            BattleEditorUtility.Set(so, "_ownReceiver", receiver);
            BattleEditorUtility.Set(so, "_hitMask", (LayerMask)((1 << hitboxLayer) | 1));
            so.ApplyModifiedPropertiesWithoutUndo();

            so = new SerializedObject(combat);
            BattleEditorUtility.Set(so, "_body", body);
            BattleEditorUtility.Set(so, "_projectiles", projectiles);
            BattleEditorUtility.Set(so, "_receiver", receiver);
            BattleEditorUtility.Set(so, "_hitboxRoot", hitboxRoot);
            BattleEditorUtility.Set(so, "_loadout", configs);
            BattleEditorUtility.Set(so, "_blockHitboxes", blockHitboxes);
            BattleEditorUtility.Set(so, "_hitMask", (LayerMask)((1 << hitboxLayer) | 1));
            BattleEditorUtility.Set(so, "_slotCount", slotCount);
            so.ApplyModifiedPropertiesWithoutUndo();

            so = new SerializedObject(fighter);
            BattleEditorUtility.Set(so, "_health", health);
            BattleEditorUtility.Set(so, "_move", move);
            BattleEditorUtility.Set(so, "_body", body);
            BattleEditorUtility.Set(so, "_combat", combat);
            BattleEditorUtility.Set(so, "_receiver", receiver);
            so.ApplyModifiedPropertiesWithoutUndo();

            so = new SerializedObject(root.AddComponent<FighterAnimComponent>());
            BattleEditorUtility.Set(so, "_fighter", fighter);
            BattleEditorUtility.Set(so, "_animator", animator);
            BattleEditorUtility.Set(so, "_spineBones", new[]
            {
                animator.GetBoneTransform(HumanBodyBones.Spine),
                animator.GetBoneTransform(HumanBodyBones.Chest),
                animator.GetBoneTransform(HumanBodyBones.UpperChest)
            });
            BattleEditorUtility.Set(so, "_headBone", animator.GetBoneTransform(HumanBodyBones.Head));
            so.ApplyModifiedPropertiesWithoutUndo();

            BattleEditorUtility.Set(root.AddComponent<FighterCameraComponent>(), "_fighter", fighter);

            so = new SerializedObject(root.AddComponent<WeaponViewComponent>());
            BattleEditorUtility.Set(so, "_combat", combat);
            BattleEditorUtility.Set(so, "_sockets", sockets);
            so.ApplyModifiedPropertiesWithoutUndo();

            so = new SerializedObject(root.AddComponent<ProjectileViewComponent>());
            BattleEditorUtility.Set(so, "_projectiles", projectiles);
            BattleEditorUtility.Set(so, "_arrowPrefab", arrow);
            BattleEditorUtility.Set(so, "_magicPrefab", magicOrb);
            so.ApplyModifiedPropertiesWithoutUndo();

            so = new SerializedObject(root.AddComponent<HitFeedbackComponent>());
            BattleEditorUtility.Set(so, "_receiver", receiver);
            BattleEditorUtility.Set(so, "_combat", combat);
            so.ApplyModifiedPropertiesWithoutUndo();

            return new FighterParts
            {
                Root = root,
                Animator = animator,
                Model = model.GetComponent<CharacterModelComponent>(),
                Health = health,
                Move = move,
                Body = body,
                Combat = combat,
                Receiver = receiver,
                Projectiles = projectiles,
                Fighter = fighter,
                HitboxRoot = hitboxRoot,
                Sockets = sockets
            };
        }

        private static void BuildBot(GameObject fighterPrefab)
        {
            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(fighterPrefab);
            root.name = "BotFighter";
            CharacterModelComponent model = root.GetComponentInChildren<CharacterModelComponent>();
            model.SetFemale(true);

            for (OutfitPart part = OutfitPart.RangerBody; part <= OutfitPart.RangerBoots; part++)
                model.Show(part, null, Color.white);

            BattleEditorUtility.Set(root.AddComponent<BotBrainComponent>(), "_fighter", root.GetComponent<FighterComponent>());

            SavePrefab(root, BotPath);
        }

        private static GameObject BuildDummy(HitZoneConfig zones, Loadout shieldLoadout, GameObject shieldPrefab)
        {
            bool hasShield = shieldLoadout != null;
            int hitboxLayer = LayerMask.NameToLayer(BattleEditorUtility.HitboxLayer);
            Material straw = BattleEditorUtility.GetMaterial("DummyStraw", new Color(0.8f, 0.68f, 0.35f));
            Material wood = BattleEditorUtility.GetMaterial("DummyWood", new Color(0.35f, 0.24f, 0.14f));

            GameObject root = new GameObject(hasShield ? "ShieldDummy" : "TrainingDummy");
            Transform parent = root.transform;
            root.AddComponent<NetworkObject>();
            HealthComponent health = root.AddComponent<HealthComponent>();
            HitboxRoot hitboxRoot = root.AddComponent<HitboxRoot>();
            DamageReceiverComponent receiver = root.AddComponent<DamageReceiverComponent>();
            TrainingDummyComponent dummy = root.AddComponent<TrainingDummyComponent>();

            // The mannequins of the animation packs are the dummies: the plain one stands in the T-pose, the shield one holds the block.
            Transform visual = BattleEditorUtility.CreateChild("Visual", parent).transform;
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Post", visual, new Vector3(0f, 0.5f, -0.16f), Vector3.zero, new Vector3(0.1f, 0.5f, 0.1f), wood, true);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Mount", visual, new Vector3(0f, 1f, -0.1f), Vector3.zero, new Vector3(0.08f, 0.08f, 0.16f), wood);
            AnimationClip pose = hasShield ? AssetDatabase.LoadAssetAtPath<AnimationClip>(
                $"{BattleEditorUtility.AnimationsFolder}/{shieldLoadout.Config.AnimationPrefix}{FighterAnimComponent.BlockSuffix}.anim") : null;
            BattleCharacterBuilder.CreateFigure(root.name, hasShield, pose, 10f, straw, null).transform.SetParent(visual, false);

            Transform hitboxes = BattleEditorUtility.CreateChild("Hitboxes", parent).transform;
            CreateSphereHitbox(hitboxes, hitboxRoot, "Head", HitZone.Head, new Vector3(0f, 1.72f, 0f), 0.14f, hitboxLayer);
            CreateBoxHitbox(hitboxes, hitboxRoot, "Torso", HitZone.Torso, new Vector3(0f, 1.25f, 0f), new Vector3(0.22f, 0.3f, 0.15f), hitboxLayer);
            CreateBoxHitbox(hitboxes, hitboxRoot, "Legs", HitZone.Legs, new Vector3(0f, 0.48f, 0f), new Vector3(0.15f, 0.47f, 0.15f), hitboxLayer);

            SerializedObject so = new SerializedObject(dummy);
            BattleEditorUtility.Set(so, "_health", health);
            BattleEditorUtility.Set(so, "_receiver", receiver);

            if (hasShield)
            {
                BlockBox block = shieldLoadout.Block;
                Hitbox blockHitbox = CreateBoxHitbox(hitboxes, hitboxRoot, "Block", HitZone.Block, block.Center, block.Extents, hitboxLayer);
                blockHitbox.transform.localRotation = block.Rotation;
                BattleEditorUtility.Set(blockHitbox, "_isRound", block.IsRound);

                GameObject shield = (GameObject)PrefabUtility.InstantiatePrefab(shieldPrefab, visual);
                shield.transform.SetLocalPositionAndRotation(block.Center - block.Rotation * block.LocalCenter, block.Rotation);

                BattleEditorUtility.Set(so, "_blockWeapon", shieldLoadout.Config);
                BattleEditorUtility.Set(so, "_blockHitbox", blockHitbox);
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            hitboxRoot.InitHitboxes();
            hitboxRoot.BroadRadius = 1.5f;
            hitboxRoot.Offset = new Vector3(0f, 1f, 0f);

            SetupReceiver(receiver, health, hitboxRoot, zones);
            BattleEditorUtility.Set(root.AddComponent<HitFeedbackComponent>(), "_receiver", receiver);
            BattleEditorUtility.CreateBlocker(parent, 0.35f, 1.8f);

            return SavePrefab(root, hasShield ? ShieldDummyPath : DummyPath);
        }

        internal static void SetupReceiver(DamageReceiverComponent receiver, HealthComponent health, HitboxRoot hitboxRoot, HitZoneConfig zones)
        {
            SerializedObject so = new SerializedObject(receiver);
            BattleEditorUtility.Set(so, "_health", health);
            BattleEditorUtility.Set(so, "_hitboxRoot", hitboxRoot);
            BattleEditorUtility.Set(so, "_zoneConfig", zones);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static ZoneHitbox CreateBoxHitbox(Transform parent, HitboxRoot root, string name, HitZone zone, Vector3 position,
            Vector3 extents, int layer)
        {
            ZoneHitbox hitbox = CreateHitbox(parent, root, name, zone, position, layer);
            hitbox.Type = HitboxTypes.Box;
            hitbox.BoxExtents = extents;

            return hitbox;
        }

        internal static ZoneHitbox CreateSphereHitbox(Transform parent, HitboxRoot root, string name, HitZone zone, Vector3 position,
            float radius, int layer)
        {
            ZoneHitbox hitbox = CreateHitbox(parent, root, name, zone, position, layer);
            hitbox.Type = HitboxTypes.Sphere;
            hitbox.SphereRadius = radius;

            return hitbox;
        }

        private static ZoneHitbox CreateHitbox(Transform parent, HitboxRoot root, string name, HitZone zone, Vector3 position, int layer)
        {
            GameObject go = BattleEditorUtility.CreateChild(name, parent, position);
            go.layer = layer;
            ZoneHitbox hitbox = go.AddComponent<ZoneHitbox>();
            hitbox.Root = root;
            BattleEditorUtility.Set(hitbox, "_zone", zone);

            return hitbox;
        }

        internal static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            return prefab;
        }

        /// Plays the built controller on a model instance to read weapon socket poses exactly as they appear at runtime.
        private sealed class TraceSampler : IDisposable
        {
            private readonly GameObject _root;
            private readonly Animator _animator;
            private readonly Transform[] _sockets;

            public TraceSampler()
            {
                _root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BattleEditorUtility.ModelPath), Vector3.zero, Quaternion.identity);
                _animator = _root.GetComponent<Animator>();
                _sockets = BattlePoseRig.CreateSockets(_animator);
                _animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(BattleEditorUtility.ControllerPath);
                _animator.applyRootMotion = false;
                _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                _animator.Rebind();
            }

            public void Dispose()
            {
                Object.DestroyImmediate(_root);
            }

            public void SampleTrace(WeaponDefinition weapon, AttackDefinition attack, string state, out List<Vector3> traceBase, out List<Vector3> traceTip)
            {
                int samples = Mathf.CeilToInt(attack.Duration * BattleAnimationBuilder.FrameRate);
                Transform socket = _sockets[(int)(attack.IsOffHand ? WeaponSocket.LeftHand : WeaponSocket.RightHand)];

                traceBase = new List<Vector3>(samples + 1);
                traceTip = new List<Vector3>(samples + 1);
                Vector3[] strike = new Vector3[samples + 1];
                Quaternion[] rotations = new Quaternion[samples + 1];

                for (int i = 0; i <= samples; i++)
                {
                    Play(state, Mathf.Min(i / BattleAnimationBuilder.FrameRate, attack.Duration) / attack.Duration);
                    traceBase.Add(socket.TransformPoint(0f, 0f, weapon.BladeBase));
                    traceTip.Add(socket.TransformPoint(0f, 0f, weapon.BladeTip));
                    strike[i] = socket.TransformPoint(0f, 0f, weapon.StrikeOf(attack));
                    rotations[i] = socket.rotation;
                }

                CheckSwing(state, attack, BattleAnimationLibrary.IsCut(weapon, attack) && !weapon.IsUnarmed && !weapon.IsRound, strike, rotations,
                    weapon.IsEdgeBack ? Vector3.down : Vector3.up);
            }

            /// The weapon must not spin about its own axis between two frames, and while a cut is active and under way its
            /// leading edge must face where the strike point travels.
            private static void CheckSwing(string state, AttackDefinition attack, bool isCut, Vector3[] strike, Quaternion[] rotations, Vector3 edge)
            {
                const float maxRoll = 25f;
                const float maxLean = 15f;
                const float underWay = 0.4f;
                float roll = 0f;
                float lean = 0f;
                float fastest = 0f;

                for (int i = 1; i < strike.Length - 1; i++)
                    fastest = Mathf.Max(fastest, IsActive(attack, i) ? (strike[i + 1] - strike[i - 1]).magnitude : 0f);

                for (int i = 1; i < strike.Length - 1; i++)
                {
                    Vector3 blade = rotations[i] * Vector3.forward;
                    (rotations[i + 1] * Quaternion.Inverse(rotations[i])).ToAngleAxis(out float angle, out Vector3 axis);
                    roll = Mathf.Max(roll, Mathf.Abs(Mathf.DeltaAngle(0f, angle) * Vector3.Dot(axis, blade)));

                    Vector3 travel = strike[i + 1] - strike[i - 1];
                    Vector3 across = Vector3.ProjectOnPlane(travel, blade);

                    if (isCut && IsActive(attack, i) && travel.magnitude > fastest * underWay && across.magnitude > travel.magnitude * 0.5f)
                        lean = Mathf.Max(lean, Vector3.Angle(rotations[i] * edge, across));
                }

                if (roll > maxRoll)
                    Debug.LogError($"[{nameof(BattleContentBuilder)}] {state}: the weapon spins {roll:0} degrees about its axis within a frame");

                if (lean > maxLean)
                    Debug.LogError($"[{nameof(BattleContentBuilder)}] {state}: the edge is {lean:0} degrees off the path of the cut");
            }

            private static bool IsActive(AttackDefinition attack, int sample)
            {
                float time = sample / BattleAnimationBuilder.FrameRate;

                return time >= attack.Windup && time <= attack.Windup + attack.Active;
            }

            /// The box wraps the mesh of the attachment held in the block socket, so it matches the visible weapon exactly.
            public BlockBox SampleBlock(WeaponDefinition weapon, bool isMirrored, (GameObject prefab, WeaponSocket socket)[] attachments)
            {
                WeaponSocket socketId = isMirrored ? Mirror(weapon.BlockSocket) : weapon.BlockSocket;
                Bounds bounds = new Bounds(weapon.BlockBoxCenter, weapon.BlockBoxExtents * 2f);

                foreach ((GameObject prefab, WeaponSocket socket) attachment in attachments)
                {
                    if (attachment.socket == socketId && TryGetMeshBounds(attachment.prefab, out Bounds meshBounds))
                        bounds = meshBounds;
                }

                Play(weapon.Prefix + FighterAnimComponent.BlockSuffix, 1f);
                Transform sampled = _sockets[(int)weapon.BlockSocket];
                Vector3 center = sampled.TransformPoint(bounds.center);
                Quaternion rotation = sampled.rotation;

                if (isMirrored)
                {
                    center.x = -center.x;
                    rotation = new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w);
                }

                return new BlockBox
                {
                    Socket = socketId,
                    LocalCenter = bounds.center,
                    Extents = Vector3.Max(bounds.extents, Vector3.one * 0.02f),
                    Center = center,
                    Rotation = rotation,
                    IsRound = socketId is WeaponSocket.LeftShield or WeaponSocket.RightShield
                };
            }

            private static WeaponSocket Mirror(WeaponSocket socket)
            {
                return socket switch
                {
                    WeaponSocket.RightHand => WeaponSocket.LeftHand,
                    WeaponSocket.LeftHand => WeaponSocket.RightHand,
                    WeaponSocket.RightShield => WeaponSocket.LeftShield,
                    _ => WeaponSocket.RightShield
                };
            }

            /// Mesh bounds in the prefab root space, which is the socket space once attached.
            private static bool TryGetMeshBounds(GameObject prefab, out Bounds bounds)
            {
                bounds = default;
                bool hasBounds = false;
                Matrix4x4 toRoot = prefab.transform.worldToLocalMatrix;

                foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null)
                        continue;

                    Bounds local = filter.sharedMesh.bounds;
                    Matrix4x4 matrix = toRoot * filter.transform.localToWorldMatrix;

                    for (int i = 0; i < 8; i++)
                    {
                        Vector3 corner = matrix.MultiplyPoint3x4(local.center + Vector3.Scale(local.extents,
                            new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f)));

                        if (!hasBounds)
                            bounds = new Bounds(corner, Vector3.zero);
                        else
                            bounds.Encapsulate(corner);

                        hasBounds = true;
                    }
                }

                return hasBounds;
            }

            private void Play(string state, float normalizedTime)
            {
                _animator.SetFloat(FighterAnimComponent.ActionSpeedParam, 1f);
                _animator.Play(state, 1, Mathf.Clamp(normalizedTime, 0f, 0.999f));
                _animator.Update(0f);
            }
        }
    }
}
