using System;
using System.Collections.Generic;
using Fusion;
using Game.Scripts.Battle;
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

        private const string HealthBarPath = "Assets/Game/Prefabs/HealthBar.prefab";

        private sealed class BlockBox
        {
            public Vector3 Center;
            public Quaternion Rotation;
            public Vector3 Extents;
        }

        private sealed class Loadout
        {
            public WeaponConfig Config;
            public BlockBox Block;
        }

        [MenuItem("Tools/Game/Battle/Build Content")]
        public static void Build()
        {
            BattleEditorUtility.EnsureFolder(BattleEditorUtility.ConfigsFolder);
            BattleEditorUtility.EnsureFolder(BattleEditorUtility.PrefabsFolder);
            BattleEditorUtility.EnsureFolder(BattleEditorUtility.MaterialsFolder);
            BattleEditorUtility.EnsureLayer(BattleEditorUtility.HitboxLayer);

            GameObject sword = BattleWeaponPrefabBuilder.BuildSword("Sword", 0.12f, 0.9f, 0.06f, 0.2f, 0.08f);
            GameObject greatsword = BattleWeaponPrefabBuilder.BuildSword("Greatsword", 0.18f, 1.35f, 0.07f, 0.32f, 0.22f);
            GameObject shield = BattleWeaponPrefabBuilder.BuildShield();
            GameObject bow = BattleWeaponPrefabBuilder.BuildBow();
            GameObject arrow = BattleWeaponPrefabBuilder.BuildArrow();

            WeaponDefinition swordShield = BattleAnimationLibrary.CreateSwordShield();
            Loadout[] loadouts;

            using (TraceSampler sampler = new TraceSampler())
            {
                loadouts = new[]
                {
                    CreateWeapon(sampler, "SwordShield", swordShield, HandSide.Right,
                        (sword, WeaponSocket.RightHand), (shield, WeaponSocket.LeftShield)),
                    CreateWeapon(sampler, "Greatsword", BattleAnimationLibrary.CreateGreatsword(), HandSide.Right,
                        (greatsword, WeaponSocket.RightHand)),
                    CreateWeapon(sampler, "Bow", BattleAnimationLibrary.CreateBow(), HandSide.Right,
                        (bow, WeaponSocket.LeftHand)),
                    CreateWeapon(sampler, "SwordShieldLeft", swordShield, HandSide.Left,
                        (sword, WeaponSocket.LeftHand), (shield, WeaponSocket.RightShield))
                };
            }

            BodyConfig body = CreateBodyConfig();
            MovementConfig movement = BattleEditorUtility.LoadOrCreate<MovementConfig>($"{BattleEditorUtility.ConfigsFolder}/Movement.asset");
            BattleEditorUtility.Set(movement, "_crouchHeight", 1.4f);
            HitZoneConfig zones = BattleEditorUtility.LoadOrCreate<HitZoneConfig>($"{BattleEditorUtility.ConfigsFolder}/HitZones.asset");

            GameObject fighter = BuildFighter(body, movement, zones, loadouts, arrow);
            BuildBot(fighter);
            GameObject dummy = BuildDummy(zones, null, null);
            BuildDummy(zones, loadouts[0], shield);

            AssetDatabase.SaveAssets();
            Debug.Log($"[{nameof(BattleContentBuilder)}] Content built: {fighter.name}, {dummy.name}");
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

            so.FindProperty("_attachments").arraySize = attachments.Length;

            for (int i = 0; i < attachments.Length; i++)
            {
                BattleEditorUtility.Set(so, $"_attachments.Array.data[{i}]._prefab", attachments[i].prefab);
                BattleEditorUtility.Set(so, $"_attachments.Array.data[{i}]._socket", attachments[i].socket);
            }

            so.FindProperty("_attacks").arraySize = definition.Attacks.Length;

            for (int i = 0; i < definition.Attacks.Length; i++)
            {
                AttackDefinition attack = definition.Attacks[i];
                string path = $"_attacks.Array.data[{i}].";
                sampler.SampleTrace(definition, i, out List<Vector3> traceBase, out List<Vector3> traceTip);

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

            BattleEditorUtility.Set(so, "_ranged._fullDrawTime", BattleAnimationLibrary.FullDrawTime);
            BattleEditorUtility.Set(so, "_ranged._reloadTime", BattleAnimationLibrary.ReloadTime);
            so.ApplyModifiedPropertiesWithoutUndo();

            return new Loadout { Config = config, Block = definition.CanBlock ? sampler.SampleBlock(definition, isMirrored) : null };
        }

        private static BodyConfig CreateBodyConfig()
        {
            BodyConfig config = BattleEditorUtility.LoadOrCreate<BodyConfig>($"{BattleEditorUtility.ConfigsFolder}/Body.asset");
            GameObject model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(BattleEditorUtility.ModelPath));
            Animator animator = model.GetComponent<Animator>();

            Vector3[] pivots =
            {
                animator.GetBoneTransform(HumanBodyBones.Spine).position,
                animator.GetBoneTransform(HumanBodyBones.Chest).position,
                animator.GetBoneTransform(HumanBodyBones.UpperChest).position
            };
            Vector3 head = animator.GetBoneTransform(HumanBodyBones.Head).position;
            Object.DestroyImmediate(model);

            SerializedObject so = new SerializedObject(config);
            BattleEditorUtility.Set(so, "_spinePivots", pivots);
            BattleEditorUtility.Set(so, "_eyePoint", new Vector3(0f, head.y + 0.125f, head.z + 0.14f));
            BattleEditorUtility.Set(so, "_crouchDrop", BattleAnimationLibrary.CrouchDrop);
            so.ApplyModifiedPropertiesWithoutUndo();

            return config;
        }

        private static GameObject BuildFighter(BodyConfig bodyConfig, MovementConfig movement, HitZoneConfig zones,
            Loadout[] loadouts, GameObject arrow)
        {
            int hitboxLayer = LayerMask.NameToLayer(BattleEditorUtility.HitboxLayer);
            GameObject root = new GameObject("Fighter") { layer = LayerMask.NameToLayer(BattleEditorUtility.CharacterLayer) };

            CharacterController collider = root.AddComponent<CharacterController>();
            collider.height = 1.85f;
            collider.radius = 0.3f;
            collider.center = new Vector3(0f, 0.925f, 0f);
            collider.stepOffset = 0.3f;
            collider.skinWidth = 0.03f;

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

            SkinnedMeshRenderer renderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
            renderer.sharedMaterial = BattleEditorUtility.GetMaterial("FighterBody", new Color(0.62f, 0.66f, 0.72f));
            renderer.updateWhenOffscreen = true;

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

                blockHitboxes[i] = CreateBoxHitbox(hitboxes, hitboxRoot, "Block" + i, HitZone.Block, block.Center, block.Extents, hitboxLayer);
                blockHitboxes[i].transform.localRotation = block.Rotation;
                upper.Add(blockHitboxes[i].transform);
            }

            hitboxRoot.InitHitboxes();
            hitboxRoot.BroadRadius = 2.2f;
            hitboxRoot.Offset = new Vector3(0f, 1f, 0f);

            SerializedObject so = new SerializedObject(move);
            BattleEditorUtility.Set(so, "_config", movement);
            BattleEditorUtility.Set(so, "_controller", controller);
            BattleEditorUtility.Set(so, "_collider", collider);
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
            BattleEditorUtility.Set(so, "_hitMask", (LayerMask)(1 << hitboxLayer));
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

            GameObject healthBar = CreateHealthBar(root.transform, health, 2.15f);

            so = new SerializedObject(root.AddComponent<FighterCameraComponent>());
            BattleEditorUtility.Set(so, "_fighter", fighter);
            BattleEditorUtility.Set(so, "_hiddenForOwner", new[] { healthBar });
            so.ApplyModifiedPropertiesWithoutUndo();

            so = new SerializedObject(root.AddComponent<WeaponViewComponent>());
            BattleEditorUtility.Set(so, "_combat", combat);
            BattleEditorUtility.Set(so, "_sockets", sockets);
            so.ApplyModifiedPropertiesWithoutUndo();

            so = new SerializedObject(root.AddComponent<ProjectileViewComponent>());
            BattleEditorUtility.Set(so, "_projectiles", projectiles);
            BattleEditorUtility.Set(so, "_arrowPrefab", arrow);
            so.ApplyModifiedPropertiesWithoutUndo();

            BattleEditorUtility.Set(root.AddComponent<HitFeedbackComponent>(), "_receiver", receiver);

            return SavePrefab(root, FighterPath);
        }

        private static void BuildBot(GameObject fighterPrefab)
        {
            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(fighterPrefab);
            root.name = "BotFighter";
            root.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial =
                BattleEditorUtility.GetMaterial("BotBody", new Color(0.75f, 0.35f, 0.3f));
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

            Transform visual = BattleEditorUtility.CreateChild("Visual", parent).transform;
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Post", visual, new Vector3(0f, 0.5f, 0f), Vector3.zero, new Vector3(0.12f, 0.5f, 0.12f), wood, true);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Capsule, "Torso", visual, new Vector3(0f, 1.25f, 0f), Vector3.zero, new Vector3(0.42f, 0.3f, 0.28f), straw);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Arms", visual, new Vector3(0f, 1.42f, 0f), Vector3.zero, new Vector3(1.1f, 0.09f, 0.09f), wood);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Sphere, "Head", visual, new Vector3(0f, 1.72f, 0f), Vector3.zero, Vector3.one * 0.26f, straw);

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

                GameObject shield = (GameObject)PrefabUtility.InstantiatePrefab(shieldPrefab, visual);
                shield.transform.SetLocalPositionAndRotation(block.Center, block.Rotation);

                BattleEditorUtility.Set(so, "_blockWeapon", shieldLoadout.Config);
                BattleEditorUtility.Set(so, "_blockHitbox", blockHitbox);
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            hitboxRoot.InitHitboxes();
            hitboxRoot.BroadRadius = 1.5f;
            hitboxRoot.Offset = new Vector3(0f, 1f, 0f);

            SetupReceiver(receiver, health, hitboxRoot, zones);
            CreateHealthBar(parent, health, 2.1f);
            BattleEditorUtility.Set(root.AddComponent<HitFeedbackComponent>(), "_receiver", receiver);

            return SavePrefab(root, hasShield ? ShieldDummyPath : DummyPath);
        }

        private static void SetupReceiver(DamageReceiverComponent receiver, HealthComponent health, HitboxRoot hitboxRoot, HitZoneConfig zones)
        {
            SerializedObject so = new SerializedObject(receiver);
            BattleEditorUtility.Set(so, "_health", health);
            BattleEditorUtility.Set(so, "_hitboxRoot", hitboxRoot);
            BattleEditorUtility.Set(so, "_zoneConfig", zones);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateHealthBar(Transform parent, HealthComponent health, float height)
        {
            GameObject healthBar = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(HealthBarPath), parent);
            healthBar.transform.localPosition = new Vector3(0f, height, 0f);
            healthBar.transform.localScale *= 0.3f;
            healthBar.AddComponent<BillboardComponent>();

            SerializedObject so = new SerializedObject(healthBar.AddComponent<HealthBarComponent>());
            BattleEditorUtility.Set(so, "_healthComponent", health);
            BattleEditorUtility.Set(so, "_healthBarView", healthBar.GetComponent<HealthBarView>());
            so.ApplyModifiedPropertiesWithoutUndo();

            return healthBar;
        }

        private static ZoneHitbox CreateBoxHitbox(Transform parent, HitboxRoot root, string name, HitZone zone, Vector3 position,
            Vector3 extents, int layer)
        {
            ZoneHitbox hitbox = CreateHitbox(parent, root, name, zone, position, layer);
            hitbox.Type = HitboxTypes.Box;
            hitbox.BoxExtents = extents;

            return hitbox;
        }

        private static ZoneHitbox CreateSphereHitbox(Transform parent, HitboxRoot root, string name, HitZone zone, Vector3 position,
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

        private static GameObject SavePrefab(GameObject root, string path)
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

            public void SampleTrace(WeaponDefinition weapon, int attackIndex, out List<Vector3> traceBase, out List<Vector3> traceTip)
            {
                AttackDefinition attack = weapon.Attacks[attackIndex];
                string state = weapon.Prefix + FighterAnimComponent.AttackSuffix + attackIndex;
                int samples = Mathf.CeilToInt(attack.Duration * BattleAnimationBuilder.FrameRate);
                Transform socket = _sockets[(int)WeaponSocket.RightHand];

                traceBase = new List<Vector3>(samples + 1);
                traceTip = new List<Vector3>(samples + 1);

                for (int i = 0; i <= samples; i++)
                {
                    Play(state, Mathf.Min(i / BattleAnimationBuilder.FrameRate, attack.Duration) / attack.Duration);
                    traceBase.Add(socket.TransformPoint(0f, 0f, weapon.BladeBase));
                    traceTip.Add(socket.TransformPoint(0f, 0f, weapon.BladeTip));
                }
            }

            public BlockBox SampleBlock(WeaponDefinition weapon, bool isMirrored)
            {
                Play(weapon.Prefix + FighterAnimComponent.BlockSuffix, 1f);
                Transform socket = _sockets[(int)weapon.BlockSocket];
                Vector3 center = socket.TransformPoint(weapon.BlockBoxCenter);
                Quaternion rotation = socket.rotation;

                if (isMirrored)
                {
                    center.x = -center.x;
                    rotation = new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w);
                }

                return new BlockBox { Center = center, Rotation = rotation, Extents = weapon.BlockBoxExtents };
            }

            private void Play(string state, float normalizedTime)
            {
                _animator.Play(state, 1, Mathf.Clamp(normalizedTime, 0f, 0.999f));
                _animator.Update(0f);
            }
        }
    }
}
