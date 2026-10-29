using System.Linq;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// The Great Hall, the empire's shaft: the key room of the catacombs, two cells long (the middle cell and the one along its
    /// local +Z) and four storeys high, built object by object from its approved mockup (GreatHallLayout). Static stone goes into
    /// the room; the moving machinery, the vents, the arrow gallery, the collapsing slabs and the gold are networked pieces the
    /// host spawns at the room's markers. Built only when the prefab is missing, like every room: then it is the user's.
    internal static partial class CatacombRoomBuilder
    {
        public const string HallName = "GreatHallShaft";
        public const string HallTitle = "Great Hall";
        public const float HallHeight = 24f;

        private const float Storey = DungeonMapBuilder.Height;
        private static readonly Color s_ossarite = new(0.43f, 0.66f, 1f);

        /// The page's props that are kit props or lights under another name.
        private static readonly System.Collections.Generic.Dictionary<string, string> s_hallProps = new()
        {
            ["Ящики"] = "Штабель ящиков", ["Бочки"] = "Бочки", ["Груда костей"] = "Черепа", ["Нары"] = "Койка", ["Стол надзирателей"] = "Стол",
            ["Стойки с оружием"] = "Стойка с оружием", ["Алтарь Собора"] = "Алтарь", ["Груда руды"] = "Обломки", ["Останки жильщика"] = "Кости"
        };

        private static readonly string[] s_hallStone = { "Саркофаг", "Весовая платформа", "Великие весы" };

        public static CatacombRoomComponent BuildHall(Transform floor, GameObject[] sides)
        {
            string path = $"{Folder}/{HallName}.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            HallMarkers();

            if (existing != null && !IsOverwriting)
                return DungeonMapBuilder.Place(existing, floor, Vector3.zero, 0f, false).GetComponent<CatacombRoomComponent>();

            foreach (string stone in s_hallStone)
                s_stone.Add(stone);

            Transform hall = BattleEditorUtility.CreateChild(HallName, floor).transform;
            HallItem[] items = GreatHallLayout.Load();

            foreach (HallItem item in items)
                PlaceHallItem(hall, item);

            Kit("Trap ArrowGallery", hall, items.Where(item => item.kind == "slit").Aggregate(Vector3.zero, (sum, item) => sum + item.Position * 0.5f) - Vector3.up * 0.9f, 0f);
            Kit("Trap HallMachinery", hall, Vector3.zero, 0f);

            // The two doorways of each of the six outer sides, as the generator expects of the key room; above them the walls go on.
            (Vector3 position, float yaw)[] doorways =
            {
                (new Vector3(0f, 0f, -Pitch * 0.5f), 180f), (new Vector3(Pitch * 0.5f, 0f, 0f), 90f), (new Vector3(-Pitch * 0.5f, 0f, 0f), 270f),
                (new Vector3(0f, 0f, Pitch * 1.5f), 0f), (new Vector3(Pitch * 0.5f, 0f, Pitch), 90f), (new Vector3(-Pitch * 0.5f, 0f, Pitch), 270f)
            };

            foreach ((Vector3 position, float yaw) in doorways)
                DungeonMapBuilder.Place(sides[3], hall, position, yaw).name = "Wall Doorways";

            KitBlock("Ceiling", hall, new Vector3(0f, HallHeight, Pitch * 0.5f), new Vector3(Pitch, Thickness, Pitch * 2f));

            SerializedObject so = new SerializedObject(hall.gameObject.AddComponent<CatacombRoomComponent>());
            BattleEditorUtility.Set(so, "_title", HallTitle);
            BattleEditorUtility.Set(so, "_map", RenderHallMap(hall));
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAssetAndConnect(hall.gameObject, path, InteractionMode.AutomatedAction);

            return hall.GetComponent<CatacombRoomComponent>();
        }

        /// Map of the hall's two cells stacked, its first cell at the bottom, as the generator cuts it.
        public static Texture2D RenderHallMap(Transform hall)
        {
            Texture2D[] cells =
            {
                DungeonMinimapBuilder.Render(hall, hall.position, Pitch * 0.5f, MapPixels, $"Catacomb{HallName}A", true),
                DungeonMinimapBuilder.Render(hall, hall.TransformPoint(Vector3.forward * Pitch), Pitch * 0.5f, MapPixels, $"Catacomb{HallName}B", true)
            };
            Texture2D map = new Texture2D(MapPixels, MapPixels * 2, TextureFormat.RGBA32, false);

            for (int i = 0; i < cells.Length; i++)
                map.SetPixels(0, i * MapPixels, MapPixels, MapPixels, cells[i].GetPixels());

            map.Apply();
            Texture2D saved = DungeonMinimapBuilder.Save(map, $"Catacomb{HallName}", true);
            Object.DestroyImmediate(map);

            foreach (Texture2D cell in cells)
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(cell));

            return saved;
        }

        /// Markers of the hall's own networked pieces.
        private static void HallMarkers()
        {
            Marker("Trap HallMachinery", CatacombMarkerKind.Trap, GreatHallContentBuilder.Machinery);
            Marker("Trap Vent", CatacombMarkerKind.Trap, GreatHallContentBuilder.Vent);
            Marker("Trap ArrowGallery", CatacombMarkerKind.Trap, GreatHallContentBuilder.Arrows);
            Marker("Trap Collapse", CatacombMarkerKind.Trap, GreatHallContentBuilder.Collapse);
            Marker("Container GoldPile", CatacombMarkerKind.Container, GreatHallContentBuilder.GoldPile);
        }

        private static void PlaceHallItem(Transform hall, HallItem item)
        {
            Vector3 point = item.Position;

            switch (item.cat)
            {
                case "floor":
                case "platform" when item.name != "Свод казны":
                    KitBlock("Floor", hall, point, item.Size, item.ry);
                    break;
                case "platform":
                case "wall":
                case "bwall":
                case "upper":
                    KitBlock("Wall", hall, point, item.Size, item.ry);
                    break;
                case "column":
                    KitBlock("Wall Column", hall, point, item.Size, item.ry, item.round);
                    break;
                case "stairs":
                    GameObject stairs = Kit("Stairs", hall, point, item.ry);
                    stairs.GetComponent<KitStairsComponent>().SetSize(item.Size);
                    CatacombKit.Apply(stairs.GetComponent<KitStairsComponent>());
                    break;
                case "bars":
                    bool isAlongX = item.Size.x >= item.Size.z;
                    float half = Mathf.Max(item.Size.x, item.Size.z) * 0.5f;
                    BuildBars(hall, isAlongX ? Bars(point.x - half, point.z, point.x + half, point.z) : Bars(point.x, point.z - half, point.x, point.z + half));
                    Transform bars = hall.GetChild(hall.childCount - 1);
                    bars.localPosition += Vector3.up * point.y;
                    break;
                case "prop" when item.name == "Жаровня":
                    break;
                case "prop":
                case "secret" when item.name == "Останки жильщика":
                    string name = s_hallProps.TryGetValue(item.name, out string kitName) ? kitName : item.name;
                    BuildProp(hall, new Item { Shape = Shape.Prop, Name = name, X = point.x, Z = point.z, W = item.Size.x, D = item.Size.z, H = item.Size.y, R = item.ry, IsRound = item.round }, point.y);
                    break;
                case "secret" when item.name.StartsWith("Пол"):
                    KitBlock("Floor", hall, point, item.Size);
                    break;
                case "secret" when item.name == "Запечатанная дверь":
                    GameObject door = KitBlock("Wall", hall, point, item.Size);
                    door.name = "Wall Sealed Door";
                    door.GetComponent<MeshRenderer>().sharedMaterial = DungeonPropBuilder.RustyMetal;
                    Vein(hall, point + new Vector3(-0.22f, 0f, 0f), new Vector3(0.06f, item.Size.y * 0.9f, 0.12f));
                    Vein(hall, point + new Vector3(-0.22f, item.Size.y * 0.55f, 0f), new Vector3(0.06f, 0.12f, item.Size.z * 0.8f));
                    break;
                case "secret":
                    Vein(hall, point, item.Size);
                    break;
                case "chest":
                case "monster":
                    Kit(item.name, hall, point, item.ry);
                    break;
                case "gold":
                    Kit("Container GoldPile", hall, point, item.ry);
                    break;
                case "start":
                    Kit("Start", hall, point, item.ry);
                    break;
                case "trap":
                    HallTrap(hall, item);
                    break;
                case "light":
                    HallLight(hall, item);
                    break;
            }
        }

        private static void HallTrap(Transform hall, HallItem item)
        {
            Vector3 point = item.Position;

            switch (item.kind)
            {
                case "spikes":
                    Kit("Trap Spike", hall, point, item.ry);
                    break;
                // The page hangs the blade at its pivot; the trap stands on the gallery floor below it.
                case "blade":
                    Kit("Trap Blade", hall, point - Vector3.up * 4f, item.ry);
                    break;
                case "vent":
                    Kit("Trap Vent", hall, point, item.ry);
                    break;
                case "collapse":
                    Kit("Trap Collapse", hall, point + Vector3.up * item.Size.y, item.ry);
                    break;
            }
        }

        /// Torches on the outer walls face into the hall; other lamps become candles or braziers on the floor below them.
        private static void HallLight(Transform hall, HallItem item)
        {
            Vector3 point = item.Position;
            float floorY = Mathf.Floor((point.y + 0.01f) / Storey) * Storey;
            Vector3 onFloor = new Vector3(point.x, floorY, point.z);
            float half = Pitch * 0.5f - 1f;

            switch (item.kind)
            {
                case "torch" when Mathf.Abs(point.x) > half || point.z < -half || point.z > Pitch + half:
                    float yaw = point.x > half ? 270f : point.x < -half ? 90f : point.z < -half ? 0f : 180f;
                    Kit("Light Torch", hall, point - Vector3.up * TorchHeight, yaw);
                    break;
                case "torch":
                case "candle":
                    Kit("Light Candle", hall, onFloor, 0f);
                    break;
                case "fire":
                    Kit("Light Brazier", hall, onFloor, 0f);
                    break;
                default:
                    ColorUtility.TryParseHtmlString(item.color, out Color color);
                    DungeonPropBuilder.PointLight(hall, point, color, item.r, item.i * 2f, false);
                    break;
            }
        }

        /// Ossarite grown through the stone: a glowing block without a collider.
        private static void Vein(Transform hall, Vector3 bottom, Vector3 size)
        {
            Mesh mesh = new DungeonMeshBuilder(0.5f).Box(Vector3.up * size.y * 0.5f, size).Save($"HallVein_{size.x:0.##}x{size.y:0.##}x{size.z:0.##}".Replace(',', '.'));
            DungeonPropBuilder.Shadowless(DungeonPropBuilder.MeshObject("Vein", hall, mesh, DungeonPropBuilder.Emissive("OssariteVein", s_ossarite, 2.5f), bottom, default, false));
        }
    }
}
