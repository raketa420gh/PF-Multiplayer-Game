using System.Linq;
using Fusion;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Networked pieces of the Great Hall the host spawns at the hall's markers: its machinery (two cages on one chain, the
    /// sliding wall, three seals with their counterweights, the treasury slab and the drawbridge to the scales, with every
    /// lever), dust vents, the arrow gallery, collapsing slabs and the treasury's piles of gold. Positions come from the mockup.
    internal static class GreatHallContentBuilder
    {
        public const string Machinery = "HallMachinery";
        public const string Vent = "DustVent";
        public const string Arrows = "ArrowGallery";
        public const string Collapse = "CollapsingSlab";
        public const string GoldPile = "GoldPile";

        private const float LeverHold = 0.6f;
        private const float CageBars = 2.4f;

        public static void Build(LootTableConfig treasury)
        {
            BuildMachinery(GreatHallLayout.Load());
            BuildVent();
            BuildArrowGallery();
            BuildCollapse();
            BuildGoldPile(treasury);
        }

        private static LayerMask Victims => 1 << LayerMask.NameToLayer(BattleEditorUtility.CharacterLayer);

        private static void BuildMachinery(HallItem[] items)
        {
            GameObject root = new GameObject(Machinery);
            root.AddComponent<NetworkObject>();
            // Spawned at the hall's origin, turned with the hall: clients need the pose it stands in.
            root.AddComponent<NetworkTransform>();
            GreatHallMachineryComponent machinery = root.AddComponent<GreatHallMachineryComponent>();
            Transform oreCage = Cage(root.transform, GreatHallLayout.Find(items, "П1 Рудная клеть"), machinery, 0, -1f);
            Transform coffinCage = Cage(root.transform, GreatHallLayout.Find(items, "П2 Гробовая клеть"), machinery, 1, 1f);
            HallItem wallItem = GreatHallLayout.Find(items, "Ходовая стена");
            Transform wall = Block(root.transform, "Wall Sliding", wallItem, DungeonPropBuilder.Ashlar);
            Transform slab = Block(root.transform, "Wall Treasury Slab", GreatHallLayout.Find(items, "Плита казны"), DungeonPropBuilder.Ashlar);
            Transform[] weights = items.Where(item => item.name.StartsWith("Противовес")).OrderBy(item => item.name.Length).Select(item =>
            {
                Transform weight = Block(root.transform, "Counterweight", item, DungeonPropBuilder.RustyMetal);
                Chain(weight, item.Size.y, 6f);

                return weight;
            }).ToArray();
            HallItem wheelItem = GreatHallLayout.Find(items, "Колесо ствола");
            Mesh wheelMesh = new DungeonMeshBuilder(1f).Cylinder(Vector3.zero, wheelItem.Size.x * 0.5f, wheelItem.Size.y, 24).Save("HallWheel");
            Transform wheel = DungeonPropBuilder.MeshObject("Wheel", root.transform, wheelMesh, DungeonPropBuilder.DarkWood, wheelItem.Position, default, false, false).transform;

            foreach (HallItem item in items.Where(item => item.cat == "lever"))
            {
                if (item.name == "Ворот моста")
                    Bridge(root.transform, GreatHallLayout.Find(items, "Мост Весов"), item);
                else
                    Lever(root.transform, item.Position, item.ry, machinery, LeverAction(item), item.seal - 1);
            }

            Lever(oreCage, new Vector3(1.3f, 0f, 1.3f), 270f, machinery, HallLeverAction.Ride, 0);
            Lever(coffinCage, new Vector3(-1.3f, 0f, 2f), 90f, machinery, HallLeverAction.Ride, 0);

            SerializedObject so = new SerializedObject(machinery);
            BattleEditorUtility.Set(so, "_cages", new[] { oreCage, coffinCage });
            BattleEditorUtility.Set(so, "_wall", wall);
            BattleEditorUtility.Set(so, "_wallClosed", wall.localPosition);
            BattleEditorUtility.Set(so, "_wallOpen", wall.localPosition + Vector3.forward * (wallItem.Size.z + 0.05f));
            BattleEditorUtility.Set(so, "_weights", weights);
            BattleEditorUtility.Set(so, "_slab", slab);
            BattleEditorUtility.Set(so, "_wheel", wheel);
            BattleEditorUtility.Set(so, "_victimMask", Victims);
            so.ApplyModifiedPropertiesWithoutUndo();
            BattleContentBuilder.SavePrefab(root, DungeonContentBuilder.Prefab(Machinery));
        }

        /// Which way a lever sends the cages, by the mockup's name: the ore cage goes up when the coffin cage is called down.
        private static HallLeverAction LeverAction(HallItem item)
        {
            if (item.seal > 0)
                return HallLeverAction.Seal;

            return item.name is "Вызов П1 (ярус 2)" or "Вызов П2 (ярус 1)" ? HallLeverAction.OreCageUp : HallLeverAction.OreCageDown;
        }

        /// A cage: a plank floor that carries bodies, bars on three sides and a roof frame hung on a chain; the open side faces
        /// the stops along local X by exit's sign.
        private static Transform Cage(Transform parent, HallItem item, GreatHallMachineryComponent machinery, int index, float exit)
        {
            Vector3 size = item.Size;
            GameObject cage = BattleEditorUtility.CreateChild(index == 0 ? "Cage Ore" : "Cage Coffin", parent, item.Position);
            SerializedObject so = new SerializedObject(cage.AddComponent<HallCageComponent>());
            BattleEditorUtility.Set(so, "_machinery", machinery);
            BattleEditorUtility.Set(so, "_index", index);
            so.ApplyModifiedPropertiesWithoutUndo();

            Vector3 floorCenter = new Vector3(0f, -0.1f, 0f);
            Vector3 floorSize = new Vector3(size.x, 0.2f, size.z);
            GameObject floor = DungeonPropBuilder.MeshObject("Floor", cage.transform, new DungeonMeshBuilder(0.5f).Box(floorCenter, floorSize).Save($"HallCageFloor{index}"),
                DungeonPropBuilder.WoodPlanks, default, default, false, false);
            BoxCollider deck = floor.AddComponent<BoxCollider>();
            deck.center = floorCenter;
            deck.size = floorSize;
            DungeonPropBuilder.TagSurface(floor, FootstepSurface.Wood);

            DungeonMeshBuilder bars = new DungeonMeshBuilder(1f).Box(new Vector3(0f, size.y, 0f), new Vector3(size.x, 0.1f, size.z));
            float halfX = size.x * 0.5f - 0.05f;
            float halfZ = size.z * 0.5f - 0.05f;
            (Vector3 from, Vector3 to)[] sides =
            {
                (new Vector3(-exit * halfX, 0f, -halfZ), new Vector3(-exit * halfX, 0f, halfZ)),
                (new Vector3(-halfX, 0f, -halfZ), new Vector3(halfX, 0f, -halfZ)),
                (new Vector3(-halfX, 0f, halfZ), new Vector3(halfX, 0f, halfZ))
            };

            foreach ((Vector3 from, Vector3 to) in sides)
            {
                int count = Mathf.CeilToInt(Vector3.Distance(from, to) / 0.3f);

                for (int i = 0; i <= count; i++)
                    bars.Cylinder(Vector3.Lerp(from, to, i / (float)count) + Vector3.up * CageBars * 0.5f, 0.025f, CageBars, 6);

                BoxCollider fence = cage.AddComponent<BoxCollider>();
                fence.center = (from + to) * 0.5f + Vector3.up * CageBars * 0.5f;
                fence.size = new Vector3(Mathf.Max(0.1f, Mathf.Abs(to.x - from.x)), CageBars, Mathf.Max(0.1f, Mathf.Abs(to.z - from.z)));
            }

            for (int i = -1; i <= 1; i += 2)
            {
                for (int j = -1; j <= 1; j += 2)
                    bars.Box(new Vector3(i * halfX, size.y * 0.5f, j * halfZ), new Vector3(0.12f, size.y, 0.12f));
            }

            DungeonPropBuilder.MeshObject("Bars", cage.transform, bars.Save($"HallCageBars{index}"), DungeonPropBuilder.RustyMetal, default, default, false, false);
            Chain(cage.transform, size.y, 24f - item.Position.y - size.y);

            return cage.transform;
        }

        private static void Chain(Transform parent, float from, float length)
        {
            Mesh chain = new DungeonMeshBuilder(1f).Cylinder(new Vector3(0f, from + length * 0.5f, 0f), 0.06f, length, 6).Save($"HallChain_{length:0.#}".Replace(',', '.'));
            DungeonPropBuilder.Shadowless(DungeonPropBuilder.MeshObject("Chain", parent, chain, DungeonPropBuilder.RustyMetal, default, default, false, false));
        }

        /// A moving block of the mockup's size with a collider, its pivot at the bottom middle.
        private static Transform Block(Transform parent, string name, HallItem item, Material material)
        {
            Vector3 center = Vector3.up * item.Size.y * 0.5f;
            GameObject block = DungeonPropBuilder.MeshObject(name, parent, new DungeonMeshBuilder(0.5f).Box(center, item.Size).Save($"Hall{item.id}"), material,
                item.Position, new Vector3(0f, item.ry, 0f), false, false);
            BoxCollider collider = block.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = item.Size;

            return block.transform;
        }

        private static void Lever(Transform parent, Vector3 position, float yaw, GreatHallMachineryComponent machinery, HallLeverAction action, int seal)
        {
            GameObject lever = DungeonPropBuilder.Lever(out Transform handle);
            lever.transform.SetParent(parent, false);
            lever.transform.localPosition = position;
            lever.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            SerializedObject so = new SerializedObject(lever.AddComponent<HallLeverComponent>());
            BattleEditorUtility.Set(so, "_machinery", machinery);
            BattleEditorUtility.Set(so, "_action", action);
            BattleEditorUtility.Set(so, "_seal", Mathf.Max(0, seal));
            BattleEditorUtility.Set(so, "_handle", handle);
            BattleEditorUtility.Set(so, "_holdTime", action == HallLeverAction.Seal ? 2f : LeverHold);
            so.ApplyModifiedPropertiesWithoutUndo();
            DungeonContentBuilder.AddInteractCollider(lever, new Vector3(0f, 0.8f, 0f), new Vector3(0.8f, 1.6f, 0.8f));
        }

        /// The drawbridge to the scales: planks on a hinge, lowered for a while by the winch lever in the pendulum gallery.
        private static void Bridge(Transform parent, HallItem item, HallItem leverItem)
        {
            float length = item.Size.z;
            Transform hinge = BattleEditorUtility.CreateChild("Bridge Hinge", parent, item.Position).transform;
            hinge.localRotation = item.Rotation;
            Transform bridge = BattleEditorUtility.CreateChild("Bridge", hinge).transform;
            Vector3 size = new Vector3(item.Size.x, item.Size.y, length + 0.3f);
            Vector3 center = new Vector3(0f, -size.y * 0.5f, length * 0.5f - 0.15f);
            DungeonPropBuilder.MeshObject("Planks", bridge, new DungeonMeshBuilder(0.5f).Box(center, size).Save("HallBridgePlanks"), DungeonPropBuilder.WoodPlanks, default, default, false, false);
            BoxCollider deck = bridge.gameObject.AddComponent<BoxCollider>();
            deck.center = center;
            deck.size = size;
            DungeonPropBuilder.TagSurface(bridge.gameObject, FootstepSurface.Wood);

            GameObject lever = DungeonPropBuilder.Lever(out Transform handle);
            lever.transform.SetParent(parent, false);
            lever.transform.localPosition = leverItem.Position;
            lever.transform.localRotation = leverItem.Rotation;
            SerializedObject so = new SerializedObject(lever.AddComponent<BridgeLeverComponent>());
            BattleEditorUtility.Set(so, "_bridge", bridge);
            BattleEditorUtility.Set(so, "_handle", handle);
            BattleEditorUtility.Set(so, "_holdTime", LeverHold);
            BattleEditorUtility.Set(so, "_openTime", 40f);
            so.ApplyModifiedPropertiesWithoutUndo();
            DungeonContentBuilder.AddInteractCollider(lever, new Vector3(0f, 0.8f, 0f), new Vector3(0.8f, 1.6f, 0.8f));
        }

        /// An iron grate breathing out the vein's ash: a burn of 4 every half second and a slowing cough while it does.
        private static void BuildVent()
        {
            GameObject root = new GameObject(Vent);
            DungeonMeshBuilder grate = new DungeonMeshBuilder(1f).Box(new Vector3(0f, 0.02f, 0f), new Vector3(1.4f, 0.04f, 1.4f));

            for (int i = 0; i < 5; i++)
                grate.Box(new Vector3(-0.56f + i * 0.28f, 0.07f, 0f), new Vector3(0.06f, 0.08f, 1.4f));

            DungeonPropBuilder.MeshObject("Grate", root.transform, grate.Save("DustVentGrate"), DungeonPropBuilder.RustyMetal, default, default, false, false);
            ParticleSystem dust = BattleEditorUtility.CreateChild("Dust", root.transform, Vector3.up * 0.1f).AddComponent<ParticleSystem>();
            dust.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            ParticleSystem.MainModule main = dust.main;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.5f, 0.42f, 0.65f), new Color(0.4f, 0.36f, 0.3f, 0.5f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 150;
            ParticleSystem.EmissionModule emission = dust.emission;
            emission.rateOverTime = 45f;
            ParticleSystem.ShapeModule shape = dust.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.5f;
            ParticleSystemRenderer renderer = dust.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{DungeonPropBuilder.MaterialsFolder}/Smoke.mat");
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            SerializedObject so = new SerializedObject(root.AddComponent<TrapComponent>());
            BattleEditorUtility.Set(so, "_kind", TrapKind.Vent);
            BattleEditorUtility.Set(so, "_damage", 4);
            BattleEditorUtility.Set(so, "_period", 8f);
            BattleEditorUtility.Set(so, "_activeTime", 1.5f);
            BattleEditorUtility.Set(so, "_tick", 0.5f);
            BattleEditorUtility.Set(so, "_halfExtents", new Vector3(0.9f, 1.2f, 0.9f));
            BattleEditorUtility.Set(so, "_center", new Vector3(0f, 1.2f, 0f));
            BattleEditorUtility.Set(so, "_victimMask", Victims);
            BattleEditorUtility.Set(so, "_slowMagnitude", 20f);
            BattleEditorUtility.Set(so, "_slowDuration", 1f);
            BattleEditorUtility.Set(so, "_effect", dust);
            so.ApplyModifiedPropertiesWithoutUndo();
            BattleContentBuilder.SavePrefab(root, DungeonContentBuilder.Prefab(Vent));
        }

        /// Two pressure plates 6 m either side of the middle of a 33.6 m gallery and a slit in each end wall; the root stands
        /// on the floor in the middle of the lane.
        private static void BuildArrowGallery()
        {
            const float half = 16.6f;
            GameObject root = new GameObject(Arrows);
            Mesh plate = new DungeonMeshBuilder(0.5f).Box(new Vector3(0f, 0.02f, 0f), new Vector3(1.2f, 0.06f, 1.2f)).Save("ArrowPlate");
            Mesh slit = new DungeonMeshBuilder(0.5f).Box(Vector3.zero, new Vector3(0.12f, 0.5f, 1.4f)).Save("ArrowSlit");
            Mesh arrow = new DungeonMeshBuilder(1f).Box(Vector3.zero, new Vector3(0.8f, 0.03f, 0.03f)).Box(new Vector3(0.38f, 0f, 0f), new Vector3(0.08f, 0.06f, 0.06f)).Save("ArrowShaft");
            Vector3[] plates = { new(-6f, 0f, 0f), new(6f, 0f, 0f) };

            foreach (Vector3 point in plates)
                DungeonPropBuilder.MeshObject("Plate", root.transform, plate, DungeonPropBuilder.RustyMetal, point, default, false, false);

            for (int side = -1; side <= 1; side += 2)
                DungeonPropBuilder.MeshObject("Slit", root.transform, slit, DungeonPropBuilder.RustyMetal, new Vector3(side * (half + 0.15f), 1.3f, 0f), default, false, false);

            Transform[] arrows = new Transform[6];

            for (int i = 0; i < arrows.Length; i++)
            {
                arrows[i] = DungeonPropBuilder.MeshObject($"Arrow{i}", root.transform, arrow, DungeonPropBuilder.DarkWood,
                    new Vector3(0f, 0.9f + i / 2 * 0.4f, (i / 2 - 1) * 0.3f), new Vector3(0f, i % 2 == 0 ? 0f : 180f, 0f), false, false).transform;
                arrows[i].gameObject.SetActive(false);
            }

            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            SerializedObject so = new SerializedObject(root.AddComponent<ArrowGalleryComponent>());
            BattleEditorUtility.Set(so, "_plates", plates);
            BattleEditorUtility.Set(so, "_laneHalfExtents", new Vector3(half, 0.9f, 0.9f));
            BattleEditorUtility.Set(so, "_victimMask", Victims);
            BattleEditorUtility.Set(so, "_arrows", arrows);
            so.ApplyModifiedPropertiesWithoutUndo();
            BattleContentBuilder.SavePrefab(root, DungeonContentBuilder.Prefab(Arrows));
        }

        /// A 4 × 2 m floor slab, its top at the root, that falls away under whoever steps on it.
        private static void BuildCollapse()
        {
            GameObject root = new GameObject(Collapse);
            Transform slab = BattleEditorUtility.CreateChild("Slab", root.transform).transform;
            Vector3 center = new Vector3(0f, -0.25f, 0f);
            Vector3 size = new Vector3(4f, 0.5f, 2f);
            DungeonPropBuilder.MeshObject("Floor", slab, new DungeonMeshBuilder(0.5f).Box(center, size).Save("CollapsingSlab"), DungeonPropBuilder.Flagstone, default, default, false, false);
            BoxCollider collider = slab.gameObject.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
            DungeonPropBuilder.TagSurface(slab.gameObject, FootstepSurface.Rubble);

            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            SerializedObject so = new SerializedObject(root.AddComponent<CollapsingFloorComponent>());
            BattleEditorUtility.Set(so, "_slab", slab);
            BattleEditorUtility.Set(so, "_collider", collider);
            BattleEditorUtility.Set(so, "_halfExtents", new Vector3(2f, 0.6f, 1f));
            BattleEditorUtility.Set(so, "_victimMask", Victims);
            so.ApplyModifiedPropertiesWithoutUndo();
            BattleContentBuilder.SavePrefab(root, DungeonContentBuilder.Prefab(Collapse));
        }

        /// A heap of coins and bars searched like a chest.
        private static void BuildGoldPile(LootTableConfig table)
        {
            GameObject root = new GameObject(GoldPile);
            DungeonMeshBuilder heap = new DungeonMeshBuilder(1f).Cylinder(new Vector3(0f, 0.3f, 0f), 0.8f, 0.6f, 18, 0.12f);
            System.Random random = new System.Random(7);

            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                heap.Box(new Vector3(Mathf.Sin(angle) * 0.75f, 0.05f, Mathf.Cos(angle) * 0.75f), new Vector3(0.3f, 0.1f, 0.14f + (float)random.NextDouble() * 0.05f));
            }

            DungeonPropBuilder.MeshObject("Gold", root.transform, heap.Save("GoldPile"), DungeonPropBuilder.Gold, default, default, false, false);
            BoxCollider body = root.AddComponent<BoxCollider>();
            body.center = new Vector3(0f, 0.3f, 0f);
            body.size = new Vector3(1.6f, 0.6f, 1.6f);
            DungeonContentBuilder.SetupContainer(root, "Pile of Gold", table, null, default, false, 6, 4);
            DungeonContentBuilder.AddInteractCollider(root, body.center, body.size + new Vector3(0.2f, 0.2f, 0.2f));
            BattleContentBuilder.SavePrefab(root, DungeonContentBuilder.Prefab(GoldPile));
        }
    }
}
