using System.Collections.Generic;
using System.Linq;
using Fusion;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// The networked mechanism of a room, built from its mockup: one prefab per room holding the secret's slabs, levers, floor
    /// plates, the lift and the drop points, spawned by the host at the room's origin.
    internal static class RoomMechanismBuilder
    {
        public const string Door = "SecretDoor";
        public const string Lever = "SecretLever";
        public const string Plate = "FloorPlate";
        public const string Lift = "Lift";
        public const string Drop = "MonsterDrop";

        private const float LeverHold = 0.6f;
        private const float DeckThickness = 0.2f;
        private const float NotchSize = 0.12f;
        private const string DropMonster = "SkeletonSwordsman";
        private static readonly Color s_notch = new(0.43f, 0.66f, 1f);

        public static bool IsPart(string name)
        {
            return name is Door or Lever or Plate or Lift or Drop;
        }

        public static string PrefabName(string room)
        {
            return $"Mech{room}";
        }

        public static void Build(string room, List<HallItem> items)
        {
            GameObject root = new GameObject(PrefabName(room));
            root.AddComponent<NetworkObject>();
            // Spawned at the room's origin, turned with the room: clients need the pose it stands in.
            root.AddComponent<NetworkTransform>();
            RoomMechanismComponent mechanism = root.AddComponent<RoomMechanismComponent>();
            List<RoomSlabComponent> slabs = items.Where(item => item.name == Door).Select(item => Slab(root.transform, room, item)).ToList();
            HallItem liftItem = items.FirstOrDefault(item => item.name == Lift);
            Transform lift = liftItem != null ? BuildLift(root.transform, room, liftItem, mechanism) : null;

            foreach (HallItem item in items.Where(item => item.name == Lever))
                AddLever(root.transform, item.Position, item.ry, mechanism, item.order, item.action == "lift");

            foreach (HallItem item in items.Where(item => item.name == Plate))
                AddPlate(root.transform, room, item, mechanism);

            Transform[] drops = items.Where(item => item.name == Drop).Select(item => AddDrop(root.transform, room, item)).ToArray();
            SerializedObject so = new SerializedObject(mechanism);
            BattleEditorUtility.Set(so, "_slabs", slabs.ToArray());
            BattleEditorUtility.Set(so, "_sequence", items.Where(item => item.name is Lever or Plate && item.action != "lift").Select(item => item.order).DefaultIfEmpty(0).Max());
            BattleEditorUtility.Set(so, "_drops", drops);

            if (drops.Length > 0)
                BattleEditorUtility.Set(so, "_dropMonster", DungeonSceneBuilder.LoadNetworkObject(DropMonster));

            if (lift != null)
            {
                BattleEditorUtility.Set(so, "_lift", lift);
                BattleEditorUtility.Set(so, "_liftStops", new Vector2(liftItem.Position.y, liftItem.Position.y + liftItem.travel));
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            BattleContentBuilder.SavePrefab(root, DungeonContentBuilder.Prefab(PrefabName(room)));
        }

        /// A block dressed as the wall around it. Without an offset in the mockup it sinks into the floor.
        private static RoomSlabComponent Slab(Transform parent, string room, HallItem item)
        {
            Vector3 center = Vector3.up * item.Size.y * 0.5f;
            GameObject block = DungeonPropBuilder.MeshObject("Wall Secret", parent, new DungeonMeshBuilder(0.5f).Box(center, item.Size).Save($"{PrefabName(room)}{item.id}"),
                DungeonPropBuilder.Ashlar, item.Position, new Vector3(0f, item.ry, 0f), false, false);
            BoxCollider collider = block.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = item.Size;
            Vector3 offset = item.open is { Length: 3 } ? new Vector3(item.open[0], item.open[1], item.open[2]) : Vector3.down * (item.Size.y + 0.05f);
            SerializedObject so = new SerializedObject(block.AddComponent<RoomSlabComponent>());
            BattleEditorUtility.Set(so, "_closed", item.Position);
            BattleEditorUtility.Set(so, "_offset", offset);
            so.ApplyModifiedPropertiesWithoutUndo();

            return block.GetComponent<RoomSlabComponent>();
        }

        /// A plank deck on a chain; the lever riding it stands on its corner.
        private static Transform BuildLift(Transform parent, string room, HallItem item, RoomMechanismComponent mechanism)
        {
            GameObject lift = BattleEditorUtility.CreateChild("Lift", parent, item.Position);
            Vector3 center = Vector3.down * DeckThickness * 0.5f;
            Vector3 size = new Vector3(item.Size.x, DeckThickness, item.Size.z);
            GameObject deck = DungeonPropBuilder.MeshObject("Deck", lift.transform, new DungeonMeshBuilder(0.5f).Box(center, size).Save($"{PrefabName(room)}Deck"),
                DungeonPropBuilder.WoodPlanks, default, default, false, false);
            BoxCollider collider = deck.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
            DungeonPropBuilder.TagSurface(deck, FootstepSurface.Wood);
            Mesh chain = new DungeonMeshBuilder(1f).Cylinder(Vector3.up * 3f, 0.06f, 6f, 6).Save($"{PrefabName(room)}Chain");
            DungeonPropBuilder.Shadowless(DungeonPropBuilder.MeshObject("Chain", lift.transform, chain, DungeonPropBuilder.RustyMetal, default, default, false, false));
            SerializedObject so = new SerializedObject(lift.AddComponent<RoomLiftComponent>());
            BattleEditorUtility.Set(so, "_mechanism", mechanism);
            so.ApplyModifiedPropertiesWithoutUndo();
            AddLever(lift.transform, new Vector3(item.Size.x * 0.5f - 0.3f, 0f, item.Size.z * 0.5f - 0.3f), 225f, mechanism, 0, true);

            return lift.transform;
        }

        private static void AddLever(Transform parent, Vector3 position, float yaw, RoomMechanismComponent mechanism, int step, bool isLift)
        {
            GameObject lever = DungeonPropBuilder.Lever(out Transform handle);
            lever.transform.SetParent(parent, false);
            lever.transform.localPosition = position;
            lever.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            SerializedObject so = new SerializedObject(lever.AddComponent<RoomLeverComponent>());
            BattleEditorUtility.Set(so, "_mechanism", mechanism);
            BattleEditorUtility.Set(so, "_step", step);
            BattleEditorUtility.Set(so, "_isLift", isLift);
            BattleEditorUtility.Set(so, "_handle", handle);
            BattleEditorUtility.Set(so, "_holdTime", LeverHold);
            so.ApplyModifiedPropertiesWithoutUndo();
            DungeonContentBuilder.AddInteractCollider(lever, new Vector3(0f, 0.8f, 0f), new Vector3(0.8f, 1.6f, 0.8f));

            if (step > 0)
                Notches(lever.transform, new Vector3(0f, 1.7f, 0f), step, Vector3.right);
        }

        private static void AddPlate(Transform parent, string room, HallItem item, RoomMechanismComponent mechanism)
        {
            GameObject plate = BattleEditorUtility.CreateChild("Plate", parent, item.Position);
            Vector3 size = new Vector3(item.Size.x, 0.1f, item.Size.z);
            GameObject top = DungeonPropBuilder.MeshObject("Top", plate.transform, new DungeonMeshBuilder(0.5f).Box(Vector3.up * size.y * 0.5f, size).Save($"{PrefabName(room)}{item.id}"),
                DungeonPropBuilder.RustyMetal, default, default, false, false);
            SerializedObject so = new SerializedObject(plate.AddComponent<RoomPlateComponent>());
            BattleEditorUtility.Set(so, "_mechanism", mechanism);
            BattleEditorUtility.Set(so, "_step", item.order);
            BattleEditorUtility.Set(so, "_top", top.transform);
            BattleEditorUtility.Set(so, "_halfExtents", new Vector3(size.x * 0.5f, 0.6f, size.z * 0.5f));
            BattleEditorUtility.Set(so, "_bodyMask", (LayerMask)(1 << LayerMask.NameToLayer(BattleEditorUtility.CharacterLayer)));
            so.ApplyModifiedPropertiesWithoutUndo();
            Notches(top.transform, new Vector3(0f, size.y + 0.01f, 0f), item.order, Vector3.right);
        }

        /// The hatch in the ceiling the monster falls from: it appears a body's height below.
        private static Transform AddDrop(Transform parent, string room, HallItem item)
        {
            Mesh mesh = new DungeonMeshBuilder(0.5f).Box(Vector3.up * item.Size.y * 0.5f, item.Size).Save($"{PrefabName(room)}{item.id}");
            DungeonPropBuilder.MeshObject("Hatch", parent, mesh, DungeonPropBuilder.DarkWood, item.Position, default, false, false);

            return BattleEditorUtility.CreateChild("Drop", parent, item.Position + Vector3.down * 2f).transform;
        }

        /// As many glowing marks as the step's number: the clue to the order.
        private static void Notches(Transform parent, Vector3 center, int count, Vector3 along)
        {
            Material material = DungeonPropBuilder.Emissive("OssariteVein", s_notch, 2.5f);

            for (int i = 0; i < count; i++)
            {
                Vector3 point = center + along * ((i - (count - 1) * 0.5f) * NotchSize * 2f);
                Mesh mesh = new DungeonMeshBuilder(0.5f).Box(Vector3.zero, new Vector3(NotchSize, 0.04f, 0.3f)).Save("RoomNotch");
                DungeonPropBuilder.Shadowless(DungeonPropBuilder.MeshObject("Notch", parent, mesh, material, point, default, false, false));
            }
        }
    }
}
