using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Catacomb masonry from Poly Haven scans (CC0, Assets/Game/Textures/PolyHaven/<id>: Color, Normal GL, ARM) on Game/WorldStone:
    /// each tile is laid at its real size in world space, so bricks keep one size and one course height over every block,
    /// step and seam. ARM (AO, roughness, metal) is repacked into the URP mask (R metal, G AO, A smoothness).
    internal static class CatacombSurfaceBuilder
    {
        private const string TexturesFolder = "Assets/Game/Textures/PolyHaven";
        private const string MaterialsFolder = DungeonPropBuilder.MaterialsFolder + "/Catacomb";
        private const string KitFolder = CatacombRoomBuilder.Folder + "/Kit";
        private const int MaskSize = 2048;

        private sealed class Surface
        {
            public string Name;
            public string Source;
            /// Real width of one tile in metres, from Poly Haven.
            public float Tile;
            public Color Tint;
        }

        private static readonly Surface[] s_surfaces =
        {
            new() { Name = "CatacombWall", Source = "stone_brick_wall_001", Tile = 2.5f, Tint = Color.white },
            new() { Name = "CatacombBlock", Source = "large_sandstone_blocks", Tile = 3f, Tint = Color.white },
            new() { Name = "CatacombFloor", Source = "cobblestone_floor_02", Tile = 2f, Tint = Color.white },
            new() { Name = "CatacombStairs", Source = "slab_tiles", Tile = 2.37f, Tint = new Color(0.62f, 0.62f, 0.64f) },
            new() { Name = "CatacombCeiling", Source = "medieval_wall_02", Tile = 3.5f, Tint = new Color(0.8f, 0.8f, 0.82f) },
            new() { Name = "CatacombBrick", Source = "castle_brick_01", Tile = 1.5f, Tint = Color.white },
            new() { Name = "CatacombMossy", Source = "mossy_stone_wall", Tile = 2f, Tint = Color.white },
            new() { Name = "CatacombSlate", Source = "monastery_stone_floor", Tile = 1.8f, Tint = Color.white }
        };

        /// Kit piece → surface of its masonry; everything else that wore it gets the wall.
        private static readonly Dictionary<string, string> s_pieces = new()
        {
            ["Floor"] = "CatacombFloor", ["Ceiling"] = "CatacombCeiling", ["Stairs"] = "CatacombStairs", ["Platform"] = "CatacombBlock", ["Pit Wall"] = "CatacombBlock"
        };

        public static Material Wall => Load("CatacombWall");
        public static Material Block => Load("CatacombBlock");
        public static Material Floor => Load("CatacombFloor");
        public static Material Stairs => Load("CatacombStairs");
        public static Material Ceiling => Load("CatacombCeiling");

        /// Rebuilds the materials and puts them on the kit pieces, the sides and posts between rooms: rooms are kit instances and follow.
        [MenuItem("Tools/Game/Dungeon/Catacombs/Apply Stone Surfaces")]
        public static void Apply()
        {
            BuildMaterials();
            Material ashlar = DungeonPropBuilder.Ashlar;
            Material flagstone = DungeonPropBuilder.Flagstone;
            Material stone = DungeonPropBuilder.StoneWall;
            IEnumerable<string> paths = Directory.GetFiles($"{KitFolder}/Structure", "*.prefab").Concat(Directory.GetFiles($"{KitFolder}/Props", "*.prefab"))
                .Concat(new[] { "CatacombPost", "CatacombSide0", "CatacombSide1", "CatacombSide2", "CatacombSide3" }.Select(name => $"{CatacombRoomBuilder.Folder}/{name}.prefab"));

            foreach (string path in paths.Select(path => path.Replace('\\', '/')))
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                string piece = Path.GetFileNameWithoutExtension(path);
                Material masonry = s_pieces.TryGetValue(piece, out string surface) ? Load(surface) : Wall;
                bool isChanged = false;

                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials;

                    for (int i = 0; i < materials.Length; i++)
                    {
                        // Dressed stone (piers, jambs, arches, stone props) takes the big blocks, the rest its piece's surface.
                        Material target = materials[i] == stone ? Block : materials[i] == ashlar ? masonry : materials[i] == flagstone ? Floor : null;

                        if (target == null)
                            continue;

                        materials[i] = target;
                        isChanged = true;
                    }

                    renderer.sharedMaterials = materials;
                }

                if (isChanged)
                    PrefabUtility.SaveAsPrefabAsset(root, path);

                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Stone surfaces applied");
        }

        private static void BuildMaterials()
        {
            BattleEditorUtility.EnsureFolder(MaterialsFolder);
            Shader shader = Shader.Find("Game/WorldStone");

            foreach (Surface surface in s_surfaces)
            {
                string folder = $"{TexturesFolder}/{surface.Source}";
                string path = $"{MaterialsFolder}/{surface.Name}.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, path);
                }

                Texture2D mask = Mask(folder, surface.Source);
                material.shader = shader;
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{surface.Source}_Color.jpg"));
                material.SetTextureScale("_BaseMap", Vector2.one / surface.Tile);
                material.SetColor("_BaseColor", surface.Tint);
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{folder}/{surface.Source}_Normal.png"));
                material.SetTexture("_MetallicGlossMap", mask);
                material.SetTexture("_OcclusionMap", mask);
                material.SetFloat("_Metallic", 1f);
                material.SetFloat("_Smoothness", 1f);
                material.SetFloat("_BumpScale", 1f);
                material.SetFloat("_OcclusionStrength", 1f);
                EditorUtility.SetDirty(material);
            }
        }

        /// R metal, G AO, A smoothness, out of Poly Haven's ARM (R AO, G roughness, B metal).
        private static Texture2D Mask(string folder, string source)
        {
            string path = $"{folder}/{source}_Mask.png";

            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null)
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            string armPath = $"{folder}/{source}_ARM.jpg";
            TextureImporter arm = (TextureImporter)AssetImporter.GetAtPath(armPath);
            arm.isReadable = true;
            arm.sRGBTexture = false;
            arm.maxTextureSize = MaskSize;
            arm.textureCompression = TextureImporterCompression.Uncompressed;
            arm.SaveAndReimport();
            Color[] pixels = AssetDatabase.LoadAssetAtPath<Texture2D>(armPath).GetPixels();
            int size = Mathf.RoundToInt(Mathf.Sqrt(pixels.Length));
            Texture2D mask = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            mask.SetPixels(pixels.Select(p => new Color(p.b, p.r, 0f, 1f - p.g)).ToArray());
            File.WriteAllBytes(path, mask.EncodeToPNG());
            Object.DestroyImmediate(mask);
            arm.isReadable = false;
            arm.SaveAndReimport();

            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = false;
            importer.maxTextureSize = MaskSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>($"{MaterialsFolder}/{name}.mat");
        }
    }
}
