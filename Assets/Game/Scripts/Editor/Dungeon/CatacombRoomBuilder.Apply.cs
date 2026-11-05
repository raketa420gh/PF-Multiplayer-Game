using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Mockup → prefab: a room whose file in the atlas is marked "edited":true is built object by object from it, over the prefab.
    /// Static stone goes into the room, the rest of its mechanism into a networked prefab of its own (RoomMechanismBuilder).
    internal static partial class CatacombRoomBuilder
    {
        private const string InteriorDoor = "Door";
        private const string InteriorDoorHalf = "DoorHalf";
        private const float ThresholdHeight = 0.15f;
        private const float ThresholdDepth = 1.2f;
        /// A torch on a side of the cell, out of the mockups the exporter wrote without a yaw.
        private const float SideWall = 15f;

        [Serializable]
        private sealed class MockupItems
        {
            public HallItem[] items;
        }

        [MenuItem("Tools/Game/Dungeon/Catacombs/Apply Mockups")]
        public static void ApplyMockups()
        {
            List<Layout> layouts = Layouts().ToList();
            PrepareKit(layouts);

            foreach (Layout layout in layouts.Where(layout => IsMockup(layout.Name)))
                ApplyMockup(layout);

            FinishRooms(layouts);
        }

        /// Only the rooms without a prefab are built: the others keep their assets, and the scene gets the whole pool.
        [MenuItem("Tools/Game/Dungeon/Catacombs/Build Missing Rooms")]
        public static void BuildMissingRooms()
        {
            List<Layout> layouts = Layouts().ToList();
            PrepareKit(layouts);
            layouts.ForEach(layout => Realize(layout));
            FinishRooms(layouts);
        }

        /// The pool is read last: the separation saves the prefabs anew and opening the scene unloads assets, both leave older references null.
        private static void FinishRooms(List<Layout> layouts)
        {
            AssetDatabase.SaveAssets();
            CatacombRoomSeparator.SeparateAll();
            DressRooms();
            BalanceRooms();
            Fusion.Editor.NetworkProjectConfigUtilities.RebuildPrefabTable();
            DungeonMapBuilder.SetRoomPool(() => layouts.Select(Realize).ToArray());
            Debug.Log($"Mockups applied: {layouts.Count} rooms");
        }

        private static void PrepareKit(List<Layout> layouts)
        {
            BuildKit(layouts);
            HallMarkers();
            DungeonContentBuilder.BuildHalfDoor();
            Marker($"Trap {InteriorDoor}", CatacombMarkerKind.Trap, "CatacombDoor");
            Marker($"Trap {InteriorDoorHalf}", CatacombMarkerKind.Trap, "CatacombDoorHalf");

            foreach (string stone in new[] { "Саркофаг", "Весовая платформа" })
                s_stone.Add(stone);

            foreach (string lamp in CatacombRoomLighting.Switchable)
            {
                CatacombRoomLighting.BuildLamp(lamp);
                Marker(CatacombRoomLighting.SwitchMarker + lamp, CatacombMarkerKind.Trap, CatacombRoomLighting.LampPrefab(lamp));
            }
        }

        [MenuItem("Tools/Game/Dungeon/Catacombs/Balance Room Lighting")]
        public static void BalanceLighting()
        {
            PrepareKit(Layouts().ToList());
            BalanceRooms();
            Fusion.Editor.NetworkProjectConfigUtilities.RebuildPrefabTable();
        }

        private static void BalanceRooms()
        {
            foreach (string path in CatacombLightBaker.RoomPaths())
            {
                GameObject room = PrefabUtility.LoadPrefabContents(path);
                CatacombRoomLighting.Balance(room.transform, (name, position, yaw) => Kit(name, room.transform, position, yaw));
                PrefabUtility.SaveAsPrefabAsset(room, path);
                PrefabUtility.UnloadPrefabContents(room);
            }
        }

        private static string MockupPath(string name)
        {
            return Path.Combine(Application.dataPath, "..", MockupFolder, $"{name}.js");
        }

        private static bool IsMockup(string name)
        {
            return File.Exists(MockupPath(name)) && File.ReadAllText(MockupPath(name)).Contains(EditedMark);
        }

        private static List<HallItem> LoadMockup(string name)
        {
            string text = File.ReadAllText(MockupPath(name));
            int start = text.IndexOf(",[", StringComparison.Ordinal) + 1;
            int end = text.LastIndexOf(']') + 1;

            return JsonUtility.FromJson<MockupItems>("{\"items\":" + text.Substring(start, end - start) + "}").items.ToList();
        }

        private static Kind ApplyMockup(Layout layout)
        {
            List<HallItem> items = LoadMockup(layout.Name);
            Site room = new Site { Layout = layout, Root = new GameObject(layout.Name).transform };
            room.Root.position = Workshop;

            foreach (HallItem item in items)
                PlaceMockupItem(room, item);

            List<HallItem> parts = items.Where(item => item.cat == "trap" && RoomMechanismBuilder.IsPart(item.name)).ToList();

            if (parts.Count > 0)
            {
                string mechanism = RoomMechanismBuilder.PrefabName(layout.Name);
                RoomMechanismBuilder.Build(layout.Name, parts);
                Marker($"Trap {mechanism}", CatacombMarkerKind.Trap, mechanism);
                Kit($"Trap {mechanism}", room.Root, Vector3.zero, 0f);
            }

            Texture2D map = DungeonMinimapBuilder.Render(room.Root, Workshop, Pitch * 0.5f, MapPixels, $"Catacomb{layout.Name}", true);
            SerializedObject so = new SerializedObject(room.Root.gameObject.AddComponent<CatacombRoomComponent>());
            BattleEditorUtility.Set(so, "_title", layout.Title);
            BattleEditorUtility.Set(so, "_isCorner", layout.IsCorner);
            BattleEditorUtility.Set(so, "_map", map);
            so.ApplyModifiedPropertiesWithoutUndo();
            room.Root.position = Vector3.zero;

            return new Kind { Prefab = Save(room.Root.gameObject, layout.Name).GetComponent<CatacombRoomComponent>(), Weight = layout.Weight };
        }

        private static void PlaceMockupItem(Site room, HallItem item)
        {
            Transform root = room.Root;
            Vector3 point = item.Position;

            switch (item.cat)
            {
                case "floor":
                    KitBlock("Floor", root, point, item.Size, item.ry);
                    break;
                case "platform":
                    KitBlock("Platform", root, point, item.Size, item.ry);
                    break;
                case "pit":
                    KitBlock("Pit Wall", root, point, item.Size, item.ry);
                    break;
                case "wall":
                    KitBlock("Wall", root, point, item.Size, item.ry);
                    break;
                case "upper":
                    KitBlock("Wall", root, point, item.Size, item.ry).name = CatacombGenerator.UpperWall;
                    break;
                case "ceiling":
                    KitBlock("Ceiling", root, point, item.Size, item.ry);
                    break;
                case "column":
                    KitBlock("Wall Column", root, point, item.Size, item.ry, item.round);
                    break;
                case "stairs":
                    GameObject stairs = Kit("Stairs", root, point, item.ry);
                    stairs.GetComponent<KitStairsComponent>().SetSize(item.Size);
                    CatacombKit.Apply(stairs.GetComponent<KitStairsComponent>());
                    break;
                case "water":
                    Kit("Water", root, point + Vector3.down * WaterLevel, 0f).transform.localScale = new Vector3(item.Size.x / KitWater, 1f, item.Size.z / KitWater);
                    break;
                case "bars":
                    bool isAlongX = item.Size.x >= item.Size.z;
                    float half = Mathf.Max(item.Size.x, item.Size.z) * 0.5f;
                    BuildBars(root, isAlongX ? Bars(point.x - half, point.z, point.x + half, point.z) : Bars(point.x, point.z - half, point.x, point.z + half));
                    root.GetChild(root.childCount - 1).localPosition += Vector3.up * point.y;
                    break;
                case "fx" when item.name == "Rune":
                    BuildRune(root, Rune(point.x, point.z, item.Size.x * 0.5f));
                    root.GetChild(root.childCount - 1).localPosition += Vector3.up * point.y;
                    break;
                case "prop":
                    Item prop = new Item { Shape = Shape.Prop, Name = item.name, X = point.x, Z = point.z, W = item.Size.x, D = item.Size.z, H = item.Size.y, R = item.ry, IsRound = item.round };

                    if (s_kit.ContainsKey(PropName(prop)))
                        Place(room, prop, point.y);
                    else
                        BuildProp(root, prop, point.y);

                    // Lower than a step: walked over, and not to cut the NavMesh in a narrow passage.
                    if (item.Size.y <= StepHeight)
                    {
                        foreach (Collider collider in root.GetChild(root.childCount - 1).GetComponentsInChildren<Collider>())
                            collider.enabled = false;
                    }


                    break;
                case "chest":
                    Kit($"Container {item.name}", root, point, item.ry);
                    break;
                case "monster":
                    Kit($"Monster {item.name}", root, point, item.ry);
                    break;
                case "start" when item.name == "Start":
                    Kit("Start", root, point, item.ry);
                    break;
                case "trap" when item.name is InteriorDoor or InteriorDoorHalf:
                    Kit($"Trap {item.name}", root, UpperDoorway(root, item), item.ry);
                    break;
                case "trap" when !RoomMechanismBuilder.IsPart(item.name):
                    Kit($"Trap {item.name}", root, point + Vector3.up * ExportedTrapOffset(item), item.ry);
                    break;
                case "light":
                    MockupLight(root, item);
                    break;
            }
        }

        /// The mockup holds where a lamp's light is: the kit piece stands that much lower.
        private static void MockupLight(Transform root, HallItem item)
        {
            string kit = $"Light {item.name}";

            if (!s_kit.ContainsKey(kit))
            {
                Debug.LogWarning($"Mockup light {item.name} has no kit piece");

                return;
            }

            Light light = s_kit[kit].GetComponentInChildren<Light>(true);
            Vector3 point = item.Position - Vector3.up * (light != null ? light.transform.position.y - s_kit[kit].transform.position.y : 0f);
            Kit(kit, root, point, item.name == "Torch" ? TorchYaw(item) : 0f);
        }

        /// A wall under any storey but the ground one ends level with its floor, and a wall's top cuts the NavMesh: a door there stands on a
        /// low threshold across the wall.
        private static Vector3 UpperDoorway(Transform root, HallItem item)
        {
            if (Mathf.Approximately(item.Position.y, 0f))
                return item.Position;

            KitBlock("Floor", root, item.Position, new Vector3(item.Size.x, ThresholdHeight, ThresholdDepth), item.ry);

            return item.Position + Vector3.up * ThresholdHeight;
        }

        /// The exporter writes a trap by the bottom of its mesh, the room kit (a box of 0.8) by the point on the floor.
        private static float ExportedTrapOffset(HallItem item)
        {
            if (item.Size == Vector3.one * 0.8f)
                return 0f;

            return item.name == "Spike" ? 0.7f : item.name == "Blade" ? -1.05f : 0f;
        }

        private static float TorchYaw(HallItem item)
        {
            Vector3 point = item.Position;

            if (item.ry != 0f)
                return item.ry;

            return point.x > SideWall ? 270f : point.x < -SideWall ? 90f : point.z > SideWall ? 180f : 0f;
        }
    }
}
