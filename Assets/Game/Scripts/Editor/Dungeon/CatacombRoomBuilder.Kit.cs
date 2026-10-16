using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// The level kit rooms are put together of, one prefab per piece, for the user to build rooms with: resizable blocks
    /// (walls, columns, floors, ceilings, platforms, parapets and the other stone and wood props), stairs, doorway arches, bars,
    /// water, lights, furniture and the markers the host spawns monsters, containers and traps at. Pieces are saved only when
    /// missing, so the user's edits to them stay.
    internal static partial class CatacombRoomBuilder
    {
        public const string KitFolder = Folder + "/Kit";
        private const string StructureFolder = KitFolder + "/Structure";
        private const string PropsFolder = KitFolder + "/Props";
        private const string LightsFolder = KitFolder + "/Lights";
        private const string MarkersFolder = KitFolder + "/Markers";
        /// Side of the water piece; rooms scale it to their pools.
        private const float KitWater = 4f;

        private static readonly Dictionary<string, GameObject> s_kit = new();

        /// An instance of a kit piece, kept a prefab instance so edits of the piece reach every room.
        public static GameObject Kit(string name, Transform parent, Vector3 position, float yaw)
        {
            return DungeonMapBuilder.Place(s_kit[name], parent, position, yaw, false);
        }

        /// A kit block of the given size standing on the point.
        public static GameObject KitBlock(string name, Transform parent, Vector3 bottom, Vector3 size, float yaw = 0f, bool isRound = false)
        {
            GameObject block = Kit(name, parent, bottom, yaw);
            KitBlockComponent component = block.GetComponent<KitBlockComponent>();
            component.SetSize(size);
            component.SetRound(isRound);
            CatacombKit.Apply(component);

            return block;
        }

        private static void BuildKit(List<Layout> layouts)
        {
            Piece("Wall", DungeonPropBuilder.Ashlar, new Vector3(4f, Height, Thickness));
            Piece("Wall Column", DungeonPropBuilder.StoneWall, new Vector3(1.2f, Height, 1.2f));
            Piece("Floor", DungeonPropBuilder.Flagstone, new Vector3(4f, Thickness, 4f));
            Piece("Ceiling", DungeonPropBuilder.Ashlar, new Vector3(4f, Thickness, 4f));
            Piece("Ceiling Beam", DungeonPropBuilder.DarkWood, new Vector3(4f, 0.4f, 0.35f), false);
            Piece("Platform", DungeonPropBuilder.Ashlar, new Vector3(4f, 1.5f, 4f));
            Piece("Pit Wall", DungeonPropBuilder.Ashlar, new Vector3(4f, 1f, PitWall), true, true);
            Register("Stairs", StructureFolder, () =>
            {
                GameObject root = new GameObject("Stairs") { isStatic = true };
                Dress(root.AddComponent<KitStairsComponent>(), DungeonPropBuilder.Ashlar);
                CatacombKit.Apply(root.GetComponent<KitStairsComponent>());

                return root;
            });
            Register("Wall Doorway", StructureFolder, () => Group("Wall Doorway", root => Doorway(root, 0f)));
            Register("Bars", StructureFolder, () => Group("Bars", root => BuildBars(root, Bars(-2, 0, 2, 0))));
            Register("Water", StructureFolder, () => Group("Water", root => BuildWater(root, Water(0, 0, KitWater, KitWater))));
            Register("Rune", StructureFolder, () => Group("Rune", root => BuildRune(root, Rune(0, 0, 2))));

            foreach (Glim glim in System.Enum.GetValues(typeof(Glim)))
            {
                Item lamp = glim == Glim.Shaft ? Shaft(0, 0) : glim == Glim.Glow ? Glow(0, 0, Hex("#5cff7a")) : Lamp(glim, 0, 0);
                Register($"Light {glim}", LightsFolder, () => Group($"Light {glim}", root => BuildLight(root, lamp, 0f)));
            }

            Register("Light Chandelier", LightsFolder, () => Group("Light Chandelier",
                root => DungeonMapBuilder.Place(DungeonMedievalBuilder.Load("Chandelier"), root, Vector3.up * (Height - 0.4f), 0f)));

            foreach (Item prop in layouts.SelectMany(layout => layout.Items).Where(item => item.Shape == Shape.Prop).GroupBy(item => item.Name).Select(group => group.First()))
            {
                Item origin = prop.Turn(0);
                (origin.X, origin.Z, origin.R) = (0f, 0f, 0f);
                Register(PropName(prop), PropsFolder, () => BuildProp(null, origin, 0f));
            }

            foreach (Mob mob in System.Enum.GetValues(typeof(Mob)))
                Marker(MarkerName(new Item { Shape = Shape.Monster, Kind = (int)mob }), CatacombMarkerKind.Monster, MonsterPrefab(mob));

            foreach (Loot loot in System.Enum.GetValues(typeof(Loot)))
                Marker(MarkerName(new Item { Shape = Shape.Container, Kind = (int)loot }), CatacombMarkerKind.Container, LootPrefab(loot));

            foreach (Snare snare in System.Enum.GetValues(typeof(Snare)))
                Marker(MarkerName(new Item { Shape = Shape.Trap, Kind = (int)snare }), CatacombMarkerKind.Trap, $"{snare}Trap");

            Marker("Start", CatacombMarkerKind.Start, null);
        }

        private static void Piece(string name, Material material, Vector3 size, bool hasCollider = true, bool isShadowless = false)
        {
            Register(name, StructureFolder, () =>
            {
                GameObject root = new GameObject(name) { isStatic = true };

                if (hasCollider)
                    root.AddComponent<BoxCollider>();

                KitBlockComponent block = root.AddComponent<KitBlockComponent>();
                block.SetSize(size);
                Dress(block, material);

                if (isShadowless)
                    DungeonPropBuilder.Shadowless(root);

                CatacombKit.Apply(block);

                return root;
            });
        }

        /// A spawn point; a start gets the three spots of a team as children.
        private static void Marker(string name, CatacombMarkerKind kind, string prefab)
        {
            Register(name, MarkersFolder, () =>
            {
                GameObject root = new GameObject(name);
                SerializedObject so = new SerializedObject(root.AddComponent<CatacombMarkerComponent>());
                BattleEditorUtility.Set(so, "_kind", kind);
                BattleEditorUtility.Set(so, "_prefab", prefab != null ? DungeonSceneBuilder.LoadNetworkObject(prefab) : null);
                so.ApplyModifiedPropertiesWithoutUndo();

                for (int i = -1; i <= 1 && kind == CatacombMarkerKind.Start; i++)
                    BattleEditorUtility.CreateChild("Spot", root.transform, new Vector3(i * 1.2f, 0f, 0f));

                return root;
            });
        }

        private static void Dress(Component piece, Material material)
        {
            MeshRenderer renderer = piece.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        }

        private static GameObject Group(string name, System.Action<Transform> build)
        {
            GameObject root = new GameObject(name);
            build(root.transform);

            return root;
        }

        private static void Register(string name, string folder, System.Func<GameObject> create)
        {
            BattleEditorUtility.EnsureFolder(folder);
            s_kit[name] = SaveNew(create, name, folder);
        }
    }
}
