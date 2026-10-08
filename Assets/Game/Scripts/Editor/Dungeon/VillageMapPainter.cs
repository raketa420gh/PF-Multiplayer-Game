using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Parchment map of the village floor drawn from its data: hill shading, woods, water, roads, fields, the graveyard,
    /// buildings in ink, the two ways down marked, the module grid faint on top. Texture rows run south to north.
    internal static class VillageMapPainter
    {
        private const int Size = 1024;

        private static readonly Color s_parchment = new(0.8f, 0.71f, 0.52f);
        private static readonly Color s_ink = new(0.2f, 0.13f, 0.07f);
        private static readonly Color s_wood = new(0.3f, 0.31f, 0.2f);
        private static readonly Color s_water = new(0.38f, 0.42f, 0.4f);
        private static readonly Color s_road = new(0.88f, 0.8f, 0.6f);
        private static readonly Color s_field = new(0.72f, 0.6f, 0.38f);
        private static readonly Color s_way = new(0.55f, 0.1f, 0.06f);

        public static Texture2D Paint(VillageGround ground, List<(Vector2 center, Vector2 halfSize, float yaw)> buildings, string name)
        {
            Color[] pixels = new Color[Size * Size];
            float step = VillageLayout.Size / Size;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float wx = (x + 0.5f) * step - VillageLayout.Half;
                    float wz = (y + 0.5f) * step - VillageLayout.Half;
                    float height = ground.Height(wx, wz);
                    // Light from the north-west, like an engraver shades hills.
                    float shade = Mathf.Clamp((ground.Height(wx - step, wz + step) - ground.Height(wx + step, wz - step)) * 0.9f, -0.35f, 0.35f);
                    float grain = DungeonTextureBuilder.Noise(wx / 540f + 3f, wz / 540f + 1f, 120f, 2);
                    Color color = s_parchment * (0.92f + grain * 0.12f + shade);

                    float forest = VillageTerrainBuilder.Forest(ground, wx, wz);

                    if (forest > 0.5f)
                    {
                        // Crowns of trees: a stipple of round blobs, darker towards the deep woods.
                        float stipple = DungeonTextureBuilder.Noise(wx / 540f, wz / 540f, 260f, 1);
                        color = Color.Lerp(color, s_wood * (0.85f + shade + (stipple - 0.5f) * 0.5f), 0.7f + (stipple > 0.6f ? 0.2f : 0f));
                    }

                    if (VillageTerrainBuilder.InField(wx, wz, 0f))
                        color = Color.Lerp(color, s_field, Mathf.Repeat(wx + wz, 3f) < 1f ? 0.7f : 0.35f);

                    if (VillageLayout.Graveyard.Contains(new Vector2(wx, wz)) && ground.Open(wx, wz) < 0f)
                        color = Color.Lerp(color, new Color(0.6f, 0.58f, 0.5f), 0.35f);

                    float road = ground.RoadDistance(wx, wz);

                    if (road < 0.6f)
                        color = Color.Lerp(color, s_road, road < -0.5f ? 0.85f : 0.5f);

                    if (height < VillageLayout.WaterLevel)
                        color = Color.Lerp(s_water, s_water * 0.7f, Mathf.Clamp01(VillageLayout.WaterLevel - height));
                    else if (height < VillageLayout.WaterLevel + 0.15f)
                        color = Color.Lerp(color, s_ink, 0.45f);

                    if (VillageLayout.EdgeDistance(wx, wz) < 22f)
                        color *= 0.55f;

                    pixels[y * Size + x] = color;
                }
            }

            foreach ((Vector2 center, Vector2 halfSize, float yaw) in buildings)
                Fill(pixels, center, halfSize, yaw, s_ink);

            foreach ((Vector2 position, float yaw) in VillageLayout.RedPortals)
            {
                Fill(pixels, position, new Vector2(4.5f, 4.5f), yaw, s_way);
                Fill(pixels, position, new Vector2(2.6f, 2.6f), yaw, s_parchment);
                Fill(pixels, position, new Vector2(1.4f, 1.4f), yaw, s_way);
            }

            float module = VillageLayout.Size / VillageLayout.Grid / step;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (Mathf.Repeat(x, module) < 1.2f || Mathf.Repeat(y, module) < 1.2f)
                        pixels[y * Size + x] = Color.Lerp(pixels[y * Size + x], s_ink, 0.25f);
                }
            }

            Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            string path = $"{DungeonMinimapBuilder.Folder}/{name}.png";
            Battle.BattleEditorUtility.EnsureFolder(DungeonMinimapBuilder.Folder);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void Fill(Color[] pixels, Vector2 center, Vector2 halfSize, float yaw, Color color)
        {
            float step = VillageLayout.Size / Size;
            float reach = halfSize.magnitude;
            Quaternion inverse = Quaternion.Euler(0f, -yaw, 0f);
            int x0 = Mathf.Max(0, Mathf.FloorToInt((center.x - reach + VillageLayout.Half) / step));
            int x1 = Mathf.Min(Size - 1, Mathf.CeilToInt((center.x + reach + VillageLayout.Half) / step));
            int y0 = Mathf.Max(0, Mathf.FloorToInt((center.y - reach + VillageLayout.Half) / step));
            int y1 = Mathf.Min(Size - 1, Mathf.CeilToInt((center.y + reach + VillageLayout.Half) / step));

            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    Vector3 local = inverse * new Vector3((x + 0.5f) * step - VillageLayout.Half - center.x, 0f, (y + 0.5f) * step - VillageLayout.Half - center.y);

                    if (Mathf.Abs(local.x) <= halfSize.x && Mathf.Abs(local.z) <= halfSize.y)
                        pixels[y * Size + x] = color;
                }
            }
        }
    }
}
