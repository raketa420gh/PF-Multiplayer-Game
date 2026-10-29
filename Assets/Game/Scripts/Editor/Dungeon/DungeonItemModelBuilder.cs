using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.Scripts.Editor.Dungeon
{
    /// Small 3D models for consumables, utilities and treasures: used on the floor and rendered into inventory icons.
    internal static class DungeonItemModelBuilder
    {
        public const string Folder = DungeonPropBuilder.PrefabsFolder + "/Items";

        private const string MeshesFolder = "Assets/Game/Meshes/Dungeon/Items";

        private const float HandTorchLength = 0.6f;
        private const float HandTorchSlim = 0.6f;
        private const float TorchLightLift = 0.25f;

        public static GameObject Build(ItemDef def)
        {
            BattleEditorUtility.EnsureFolder(Folder);
            GameObject root = new GameObject("Item_" + Sanitize(def.Name));
            Transform parent = root.transform;

            switch (def.Kind)
            {
                case ItemKind.Consumable:
                    BuildConsumable(def, parent);
                    break;
                case ItemKind.Utility:
                    BuildUtility(def, parent);
                    break;
                case ItemKind.Treasure:
                    BuildTreasure(def, parent);
                    break;
                default:
                    Object.DestroyImmediate(root);

                    return null;
            }

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{Folder}/{root.name}.prefab");
            Object.DestroyImmediate(root);

            return prefab;
        }

        private static void BuildConsumable(ItemDef def, Transform parent)
        {
            Material glass = Glass("Potion" + Sanitize(def.Name), def.Color);
            Material cork = BattleEditorUtility.GetMaterial("Cork", new Color(0.5f, 0.38f, 0.22f), 0f, 0.3f);
            Material cloth = BattleEditorUtility.GetMaterial("Linen", new Color(0.88f, 0.86f, 0.78f), 0f, 0.1f);

            switch (def.Effect)
            {
                case ConsumableEffect.HealInstant when def.Magnitude >= 100f:
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Case", parent, new Vector3(0f, 0.05f, 0f), Vector3.zero, new Vector3(0.3f, 0.1f, 0.2f), BattleEditorUtility.GetMaterial("Leather", new Color(0.25f, 0.15f, 0.08f)));
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Clasp", parent, new Vector3(0f, 0.1f, 0.1f), Vector3.zero, new Vector3(0.06f, 0.03f, 0.02f), BattleEditorUtility.GetMaterial("Steel", new Color(0.75f, 0.77f, 0.8f), 0.9f, 0.7f));
                    break;
                case ConsumableEffect.HealInstant:
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Roll", parent, new Vector3(0f, 0.05f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.1f, 0.09f, 0.1f), cloth);
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Tail", parent, new Vector3(0.06f, 0.012f, 0.03f), new Vector3(0f, 20f, 0f), new Vector3(0.12f, 0.01f, 0.06f), cloth);
                    break;
                case ConsumableEffect.Haste:
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Mug", parent, new Vector3(0f, 0.08f, 0f), Vector3.zero, new Vector3(0.12f, 0.08f, 0.12f), BattleEditorUtility.GetMaterial("DarkWood", new Color(0.22f, 0.14f, 0.08f)));
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Foam", parent, new Vector3(0f, 0.17f, 0f), Vector3.zero, new Vector3(0.11f, 0.015f, 0.11f), cloth);
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Handle", parent, new Vector3(0.08f, 0.08f, 0f), Vector3.zero, new Vector3(0.03f, 0.1f, 0.03f), BattleEditorUtility.GetMaterial("DarkWood", new Color(0.22f, 0.14f, 0.08f)));
                    break;
                case ConsumableEffect.Light:
                    // The wall torch cut to a hand length and slimmed, sunk so the hand grips its lower third; it carries its own
                    // light, so the flame lights the dark in the hand and on the floor alike.
                    Transform slim = BattleEditorUtility.CreateChild("Slim", parent, Vector3.zero).transform;
                    slim.localScale = new Vector3(HandTorchSlim, 1f, HandTorchSlim);
                    Transform torch = DungeonMedievalBuilder.Model("Torch", slim).transform;
                    Bounds bounds = DungeonKitBuilder.Bounds(torch.gameObject);
                    float scale = HandTorchLength / bounds.size.y;
                    torch.localScale *= scale;
                    torch.localPosition = new Vector3(-bounds.center.x, -bounds.min.y - HandTorchLength * 0.3f / scale, -bounds.center.z) * scale;

                    // The head would shade its own shaft from the light above it.
                    foreach (Renderer renderer in torch.GetComponentsInChildren<Renderer>())
                        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                    Vector3 flame = Vector3.up * HandTorchLength * 0.68f;
                    DungeonPropBuilder.Flame(parent, flame, 0.5f);
                    // Well above the flame: right by it the torch head burns out white.
                    Light light = DungeonPropBuilder.PointLight(parent, flame + Vector3.up * TorchLightLift, new Color(1f, 0.65f, 0.3f), def.Magnitude, 3f, true, true);
                    light.shadows = LightShadows.Hard;
                    BattleEditorUtility.Set(light.GetUniversalAdditionalLightData(), "m_AdditionalLightsShadowResolutionTier", UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow);
                    break;
                default:
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Sphere, "Flask", parent, new Vector3(0f, 0.07f, 0f), Vector3.zero, new Vector3(0.13f, 0.13f, 0.13f), glass);
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Neck", parent, new Vector3(0f, 0.15f, 0f), Vector3.zero, new Vector3(0.05f, 0.03f, 0.05f), glass);
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Cork", parent, new Vector3(0f, 0.19f, 0f), Vector3.zero, new Vector3(0.04f, 0.015f, 0.04f), cork);
                    break;
            }
        }

        private static void BuildUtility(ItemDef def, Transform parent)
        {
            Material steel = BattleEditorUtility.GetMaterial("Steel", new Color(0.75f, 0.77f, 0.8f), 0.9f, 0.7f);
            Material wood = BattleEditorUtility.GetMaterial("BowWood", new Color(0.4f, 0.26f, 0.13f));

            switch (def.Utility)
            {
                case UtilityKind.ThrowingWeapon when def.Name.Contains("Axe"):
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Handle", parent, new Vector3(0f, 0.03f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.025f, 0.18f, 0.025f), wood);
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Head", parent, new Vector3(0f, 0.09f, 0.14f), new Vector3(0f, 0f, 15f), new Vector3(0.015f, 0.12f, 0.1f), steel);
                    break;
                case UtilityKind.ThrowingWeapon:
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Blade", parent, new Vector3(0f, 0.03f, 0.08f), Vector3.zero, new Vector3(0.012f, 0.035f, 0.2f), steel);
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Grip", parent, new Vector3(0f, 0.03f, -0.07f), Vector3.zero, new Vector3(0.025f, 0.04f, 0.1f), BattleEditorUtility.GetMaterial("Leather", new Color(0.25f, 0.15f, 0.08f)));
                    break;
                case UtilityKind.Campfire:
                    for (int i = 0; i < 3; i++)
                        BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Log" + i, parent, new Vector3(0f, 0.035f + i * 0.03f, 0f), new Vector3(0f, i * 60f, 90f), new Vector3(0.05f, 0.18f, 0.05f), wood);

                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Rope", parent, new Vector3(0f, 0.08f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.14f, 0.02f, 0.14f), BattleEditorUtility.GetMaterial("Linen", new Color(0.88f, 0.86f, 0.78f), 0f, 0.1f));
                    break;
                case UtilityKind.Lockpick:
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Pick", parent, new Vector3(0f, 0.02f, 0f), new Vector3(0f, 30f, 0f), new Vector3(0.006f, 0.006f, 0.22f), steel);
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Hook", parent, new Vector3(0.03f, 0.02f, 0.11f), new Vector3(0f, 30f, 0f), new Vector3(0.04f, 0.006f, 0.006f), steel);
                    break;
            }
        }

        private static void BuildTreasure(ItemDef def, Transform parent)
        {
            Material gold = BattleEditorUtility.GetMaterial("GoldMetal", new Color(1f, 0.8f, 0.3f), 1f, 0.85f);
            Material silver = BattleEditorUtility.GetMaterial("SilverMetal", new Color(0.85f, 0.86f, 0.9f), 1f, 0.85f);
            Material metal = def.Name.Contains("Silver") ? silver : gold;

            if (BuildValuable(def, parent, metal))
                return;

            if (def.Name.Contains("Coin"))
            {
                int count = def.Name.Contains("Chest") ? 0 : def.Name.Contains("Bag") ? 0 : 7;

                if (def.Name.Contains("Purse") || def.Name.Contains("Bag"))
                {
                    float size = def.Name.Contains("Bag") ? 0.22f : 0.14f;
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Sphere, "Sack", parent, new Vector3(0f, size * 0.5f, 0f), Vector3.zero, new Vector3(size, size * 0.9f, size), BattleEditorUtility.GetMaterial("Leather", new Color(0.25f, 0.15f, 0.08f)));
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Tie", parent, new Vector3(0f, size * 0.95f, 0f), Vector3.zero, new Vector3(size * 0.35f, 0.01f, size * 0.35f), gold);

                    return;
                }

                System.Random random = new System.Random(3);

                for (int i = 0; i < count; i++)
                {
                    Vector3 position = new Vector3(((float)random.NextDouble() - 0.5f) * 0.12f, 0.008f + i * 0.006f, ((float)random.NextDouble() - 0.5f) * 0.12f);
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Coin" + i, parent, position, new Vector3(0f, random.Next(0, 90), 0f), new Vector3(0.05f, 0.004f, 0.05f), gold);
                }

                return;
            }

            if (def.Name.Contains("Crown"))
            {
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Band", parent, new Vector3(0f, 0.03f, 0f), Vector3.zero, new Vector3(0.2f, 0.03f, 0.2f), metal);

                for (int i = 0; i < 6; i++)
                {
                    float angle = i * 60f * Mathf.Deg2Rad;
                    Vector3 position = new Vector3(Mathf.Cos(angle) * 0.09f, 0.09f, Mathf.Sin(angle) * 0.09f);
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Point" + i, parent, position, new Vector3(0f, -i * 60f, 45f), new Vector3(0.035f, 0.035f, 0.012f), metal);
                }

                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Jewel", parent, new Vector3(0.1f, 0.035f, 0f), new Vector3(45f, 0f, 45f), Vector3.one * 0.03f, Glass("GemRuby", new Color(0.95f, 0.2f, 0.25f)));

                return;
            }

            if (def.Name.Contains("Ore"))
            {
                Material rock = BattleEditorUtility.GetMaterial("OreRock", new Color(0.32f, 0.3f, 0.28f), 0f, 0.2f);
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Rock", parent, new Vector3(0f, 0.06f, 0f), new Vector3(20f, 35f, 10f), new Vector3(0.14f, 0.11f, 0.12f), rock);
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Chip", parent, new Vector3(0.06f, 0.035f, 0.05f), new Vector3(-15f, 60f, 25f), new Vector3(0.07f, 0.06f, 0.06f), rock);
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "VeinA", parent, new Vector3(-0.02f, 0.1f, 0.02f), new Vector3(20f, 35f, 10f), new Vector3(0.06f, 0.05f, 0.05f), metal);
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "VeinB", parent, new Vector3(0.05f, 0.07f, -0.03f), new Vector3(40f, 10f, 30f), new Vector3(0.04f, 0.04f, 0.04f), metal);

                return;
            }

            if (def.Name.Contains("Necklace"))
            {
                Material pearl = BattleEditorUtility.GetMaterial("Pearl", new Color(0.95f, 0.93f, 0.88f), 0.1f, 0.85f);

                for (int i = 0; i < 12; i++)
                {
                    float angle = i * 30f * Mathf.Deg2Rad;
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Sphere, "Pearl" + i, parent, new Vector3(Mathf.Cos(angle) * 0.08f, 0.015f, Mathf.Sin(angle) * 0.1f), Vector3.zero, Vector3.one * 0.03f, pearl);
                }

                return;
            }

            if (def.Name.Contains("Bolt"))
            {
                Material wood = BattleEditorUtility.GetMaterial("BowWood", new Color(0.4f, 0.26f, 0.13f));
                Material steel = BattleEditorUtility.GetMaterial("Steel", new Color(0.75f, 0.77f, 0.8f), 0.9f, 0.7f);

                for (int i = 0; i < 4; i++)
                {
                    Vector3 position = new Vector3((i - 1.5f) * 0.025f, 0.012f, 0f);
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Shaft" + i, parent, position, new Vector3(90f, 0f, 0f), new Vector3(0.012f, 0.1f, 0.012f), wood);
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Head" + i, parent, position + new Vector3(0f, 0f, 0.11f), new Vector3(0f, 45f, 0f), new Vector3(0.018f, 0.012f, 0.018f), steel);
                }

                return;
            }

            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Scroll", parent, new Vector3(0f, 0.03f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.05f, 0.12f, 0.05f), BattleEditorUtility.GetMaterial("Parchment", new Color(0.85f, 0.75f, 0.5f), 0f, 0.15f));
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Ribbon", parent, new Vector3(0f, 0.03f, 0f), Vector3.zero, new Vector3(0.055f, 0.055f, 0.03f), BattleEditorUtility.GetMaterial("ClothRibbon", new Color(0.6f, 0.1f, 0.1f), 0f, 0.2f));
        }

        /// Tableware, gems, jewelry and raw materials; false when the item is none of them.
        private static bool BuildValuable(ItemDef def, Transform parent, Material metal)
        {
            Material wax = BattleEditorUtility.GetMaterial("Linen", new Color(0.88f, 0.86f, 0.78f), 0f, 0.1f);
            Material iron = BattleEditorUtility.GetMaterial("IronMetal", new Color(0.42f, 0.43f, 0.45f), 0.85f, 0.45f);
            string name = def.Name;

            if (name is "Ruby" or "Emerald" or "Sapphire" or "Diamond")
            {
                // A brilliant cut: pavilion point down, girdle, crown and table.
                GameObject gem = Lathe("ItemGemCut" + (name == "Diamond" ? 12 : 8), parent, Vector3.zero, Vector3.zero, Glass("Gem" + name, def.Color), name == "Diamond" ? 12 : 8, true,
                    new(0f, 0f), new(0.03f, 0.026f), new(0.03f, 0.03f), new(0.019f, 0.042f), new(0f, 0.042f));
                gem.transform.localScale = name == "Emerald" ? new Vector3(1f, 1f, 0.7f) : Vector3.one;

                return true;
            }

            if (name is "Tiger's Eye" or "Agate")
            {
                Material stone = BattleEditorUtility.GetMaterial("Gem" + Sanitize(name), def.Color, 0.05f, 0.85f);
                Lathe("ItemCabochon", parent, Vector3.zero, Vector3.zero, stone, 20, false,
                    new(0f, 0f), new(0.026f, 0f), new(0.028f, 0.004f), new(0.025f, 0.012f), new(0.017f, 0.019f), new(0f, 0.022f)).transform.localScale = new Vector3(1f, 1f, 0.75f);

                return true;
            }

            if (name.Contains("Goblet"))
            {
                Lathe("ItemGoblet", parent, Vector3.zero, Vector3.zero, metal, 24, false,
                    new(0f, 0f), new(0.04f, 0f), new(0.04f, 0.008f), new(0.012f, 0.02f), new(0.01f, 0.05f), new(0.03f, 0.07f), new(0.045f, 0.1f), new(0.05f, 0.14f),
                    new(0.046f, 0.14f), new(0.042f, 0.105f), new(0.026f, 0.08f), new(0f, 0.075f));

                return true;
            }

            if (name.Contains("Chalice"))
            {
                Lathe("ItemChalice", parent, Vector3.zero, Vector3.zero, metal, 24, false,
                    new(0f, 0f), new(0.045f, 0f), new(0.045f, 0.006f), new(0.03f, 0.012f), new(0.012f, 0.03f), new(0.009f, 0.05f), new(0.016f, 0.06f), new(0.009f, 0.07f),
                    new(0.01f, 0.085f), new(0.035f, 0.1f), new(0.05f, 0.13f), new(0.052f, 0.17f), new(0.047f, 0.17f), new(0.044f, 0.135f), new(0.025f, 0.11f), new(0f, 0.105f));

                return true;
            }

            if (name.Contains("Candelabrum"))
            {
                Lathe("ItemCandelabrumStem", parent, Vector3.zero, Vector3.zero, metal, 24, false,
                    new(0f, 0f), new(0.07f, 0f), new(0.07f, 0.01f), new(0.045f, 0.02f), new(0.015f, 0.035f), new(0.011f, 0.12f), new(0.018f, 0.13f), new(0.011f, 0.14f),
                    new(0.011f, 0.2f), new(0f, 0.2f));

                foreach (float side in new[] { -1f, 1f })
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Arm", parent, new Vector3(side * 0.05f, 0.17f, 0f), new Vector3(0f, 0f, -side * 68f), new Vector3(0.016f, 0.054f, 0.016f), metal);

                foreach (Vector3 cup in new[] { new Vector3(0f, 0.2f, 0f), new Vector3(-0.1f, 0.19f, 0f), new Vector3(0.1f, 0.19f, 0f) })
                    Candle(parent, cup, metal, wax);

                return true;
            }

            if (name.Contains("Candlestick"))
            {
                Lathe("ItemCandlestick", parent, Vector3.zero, Vector3.zero, metal, 24, false,
                    new(0f, 0f), new(0.05f, 0f), new(0.05f, 0.008f), new(0.035f, 0.016f), new(0.012f, 0.025f), new(0.01f, 0.09f), new(0.016f, 0.1f), new(0.01f, 0.11f),
                    new(0.01f, 0.13f), new(0f, 0.13f));
                Candle(parent, new Vector3(0f, 0.13f, 0f), metal, wax);

                return true;
            }

            if (name.Contains("Jug"))
            {
                Lathe("ItemJug", parent, Vector3.zero, Vector3.zero, metal, 24, false,
                    new(0f, 0f), new(0.045f, 0f), new(0.05f, 0.01f), new(0.065f, 0.05f), new(0.066f, 0.08f), new(0.055f, 0.12f), new(0.03f, 0.15f), new(0.026f, 0.17f),
                    new(0.034f, 0.2f), new(0.03f, 0.2f), new(0.022f, 0.17f), new(0f, 0.165f));
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Spout", parent, new Vector3(0.034f, 0.195f, 0f), new Vector3(0f, 0f, -30f), new Vector3(0.03f, 0.006f, 0.022f), metal);
                Ring("ItemJugHandle", parent, new Vector3(-0.058f, 0.13f, 0f), new Vector3(90f, 0f, 0f), metal, 0.04f, 0.007f).transform.localScale = new Vector3(0.75f, 1f, 1.3f);

                return true;
            }

            if (name.Contains("Fork") || name.Contains("Spoon"))
            {
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Handle", parent, new Vector3(0f, 0.002f, -0.04f), Vector3.zero, new Vector3(0.014f, 0.004f, 0.11f), metal);
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "HandleEnd", parent, new Vector3(0f, 0.002f, -0.095f), Vector3.zero, new Vector3(0.026f, 0.002f, 0.03f), metal);

                if (name.Contains("Spoon"))
                {
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Sphere, "Bowl", parent, new Vector3(0f, 0.006f, 0.045f), Vector3.zero, new Vector3(0.04f, 0.012f, 0.055f), metal);

                    return true;
                }

                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Neck", parent, new Vector3(0f, 0.002f, 0.025f), Vector3.zero, new Vector3(0.026f, 0.004f, 0.025f), metal);

                for (int i = -1; i <= 1; i++)
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Tine", parent, new Vector3(i * 0.009f, 0.002f, 0.058f), Vector3.zero, new Vector3(0.005f, 0.004f, 0.045f), metal);

                return true;
            }

            if (name.Contains("Chain"))
            {
                // Links laid in a loop, every other one standing on its edge.
                const int Links = 18;

                for (int i = 0; i < Links; i++)
                {
                    float angle = i * 360f / Links;
                    Vector3 position = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * 0.06f, 0.004f, Mathf.Sin(angle * Mathf.Deg2Rad) * 0.06f);
                    Ring("ItemChainLink", parent, position, new Vector3(i % 2 * 90f, -(angle + 90f), 0f), metal, 0.007f, 0.0018f).transform.localScale = new Vector3(1.6f, 1f, 1f);
                }

                return true;
            }

            if (name.Contains("Bracelet"))
            {
                Ring("ItemBracelet", parent, new Vector3(0f, 0.007f, 0f), Vector3.zero, metal, 0.038f, 0.007f);

                for (int i = 0; i < 6; i++)
                {
                    float angle = i * 60f * Mathf.Deg2Rad;
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Sphere, "Bead", parent, new Vector3(Mathf.Cos(angle) * 0.044f, 0.007f, Mathf.Sin(angle) * 0.044f), Vector3.zero, Vector3.one * 0.011f, metal);
                }

                return true;
            }

            if (name.Contains("Ingot"))
            {
                Material bar = name.Contains("Metal") ? iron : metal;
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Bar", parent, new Vector3(0f, 0.025f, 0f), Vector3.zero, new Vector3(0.1f, 0.05f, 0.22f), bar);
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Top", parent, new Vector3(0f, 0.055f, 0f), Vector3.zero, new Vector3(0.07f, 0.012f, 0.19f), bar);

                return true;
            }

            if (name.Contains("Leather") || name.Contains("Cloth"))
            {
                bool isLeather = name.Contains("Leather");
                Material sheet = BattleEditorUtility.GetMaterial(Sanitize(name), def.Color, 0f, isLeather ? 0.35f : 0.05f);
                Material tie = BattleEditorUtility.GetMaterial("Cord", new Color(0.3f, 0.22f, 0.14f), 0f, 0.1f);

                if (name.Contains("Scraps"))
                {
                    // Torn offcuts in a loose heap.
                    for (int i = 0; i < 3; i++)
                        BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Scrap" + i, parent, new Vector3((i - 1) * 0.025f, 0.004f + i * 0.005f, (i % 2) * 0.02f), new Vector3(i * 4f, i * 55f + 10f, -i * 3f),
                            new Vector3(0.07f - i * 0.01f, isLeather ? 0.005f : 0.003f, 0.05f + i * 0.008f), sheet);

                    return true;
                }

                if (name.Contains("Simple"))
                {
                    // A hide rolled up, or a cloth folded in a stack, tied with a cord.
                    if (isLeather)
                        BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Roll", parent, new Vector3(0f, 0.03f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.06f, 0.07f, 0.06f), sheet);
                    else
                    {
                        for (int i = 0; i < 3; i++)
                            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Fold" + i, parent, new Vector3(0f, 0.007f + i * 0.013f, 0f), new Vector3(0f, i * 4f - 4f, 0f), new Vector3(0.12f, 0.012f, 0.09f), sheet);
                    }

                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Cord", parent, new Vector3(0f, 0.03f, 0f), Vector3.zero, new Vector3(0.012f, 0.064f, isLeather ? 0.064f : 0.094f), tie);

                    return true;
                }

                // Thick hides folded in a heavy stack; sturdy cloth wound on a bolt.
                if (isLeather)
                {
                    for (int i = 0; i < 3; i++)
                        BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Hide" + i, parent, new Vector3(0f, 0.01f + i * 0.02f, 0f), new Vector3(0f, i * 5f - 5f, 0f), new Vector3(0.12f, 0.019f, 0.2f), sheet);

                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Cord", parent, new Vector3(0f, 0.03f, 0f), Vector3.zero, new Vector3(0.124f, 0.064f, 0.012f), tie);

                    return true;
                }

                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Bolt", parent, new Vector3(0f, 0.04f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.08f, 0.1f, 0.08f), sheet);

                foreach (float z in new[] { -0.06f, 0.06f })
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Band", parent, new Vector3(0f, 0.04f, z), new Vector3(90f, 0f, 0f), new Vector3(0.083f, 0.006f, 0.083f), tie);

                return true;
            }

            if (name.Contains("Metal Scraps"))
            {
                // Bent offcuts, a broken rivet plate and a nail.
                (Vector3 position, Vector3 euler, Vector3 scale)[] pieces =
                {
                    (new Vector3(-0.02f, 0.006f, 0f), new Vector3(0f, 20f, 8f), new Vector3(0.07f, 0.008f, 0.04f)),
                    (new Vector3(0.025f, 0.012f, 0.015f), new Vector3(25f, -35f, 0f), new Vector3(0.05f, 0.006f, 0.03f)),
                    (new Vector3(0.005f, 0.016f, -0.025f), new Vector3(-10f, 70f, 30f), new Vector3(0.04f, 0.01f, 0.025f)),
                    (new Vector3(0.03f, 0.004f, -0.03f), new Vector3(0f, 50f, 90f), new Vector3(0.006f, 0.06f, 0.006f))
                };

                foreach ((Vector3 position, Vector3 euler, Vector3 scale) in pieces)
                    BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Scrap", parent, position, euler, scale, iron);

                return true;
            }

            if (name.Contains("Bottle"))
            {
                Lathe("ItemBottle", parent, Vector3.zero, Vector3.zero, Glass("BottleGlass", def.Color), 20, false,
                    new(0f, 0f), new(0.035f, 0f), new(0.038f, 0.008f), new(0.038f, 0.09f), new(0.03f, 0.11f), new(0.012f, 0.13f), new(0.011f, 0.17f), new(0.014f, 0.175f),
                    new(0.014f, 0.18f), new(0.009f, 0.18f), new(0.009f, 0.13f), new(0.032f, 0.1f), new(0.033f, 0.012f), new(0f, 0.012f));

                return true;
            }

            return false;
        }

        /// Drip pan and a candle standing in it, from the cup's bottom up.
        private static void Candle(Transform parent, Vector3 bottom, Material metal, Material wax)
        {
            Lathe("ItemCandleCup", parent, bottom, Vector3.zero, metal, 20, false,
                new(0f, 0f), new(0.012f, 0f), new(0.01f, 0.012f), new(0.028f, 0.022f), new(0.028f, 0.028f), new(0f, 0.028f));
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Candle", parent, bottom + new Vector3(0f, 0.065f, 0f), Vector3.zero, new Vector3(0.022f, 0.038f, 0.022f), wax);
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Wick", parent, bottom + new Vector3(0f, 0.107f, 0f), Vector3.zero, new Vector3(0.002f, 0.004f, 0.002f),
                BattleEditorUtility.GetMaterial("Wick", new Color(0.08f, 0.07f, 0.06f), 0f, 0.1f));
        }

        /// Torus lying in the XZ plane: a circle of the tube turned around Y.
        private static GameObject Ring(string mesh, Transform parent, Vector3 position, Vector3 euler, Material material, float radius, float tube)
        {
            const int Sides = 8;
            Vector2[] profile = new Vector2[Sides + 1];

            for (int i = 0; i <= Sides; i++)
            {
                float angle = i * Mathf.PI * 2f / Sides;
                profile[i] = new Vector2(radius + Mathf.Cos(angle) * tube, Mathf.Sin(angle) * tube);
            }

            return Lathe(mesh, parent, position, euler, material, 16, false, profile);
        }

        /// Surface of revolution around Y from a (radius, height) profile running bottom up; faceted ones get flat faces.
        private static GameObject Lathe(string mesh, Transform parent, Vector3 position, Vector3 euler, Material material, int segments, bool faceted, params Vector2[] profile)
        {
            List<Vector3> vertices = new();
            List<int> triangles = new();
            int row = segments + 1;

            foreach (Vector2 point in profile)
            {
                for (int s = 0; s <= segments; s++)
                {
                    float angle = s * Mathf.PI * 2f / segments;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * point.x, point.y, Mathf.Sin(angle) * point.x));
                }
            }

            for (int i = 0; i < profile.Length - 1; i++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int a = i * row + s;
                    triangles.AddRange(new[] { a, a + row, a + row + 1, a, a + row + 1, a + 1 });
                }
            }

            if (faceted)
            {
                vertices = triangles.ConvertAll(index => vertices[index]);
                triangles = Enumerable.Range(0, vertices.Count).ToList();
            }

            BattleEditorUtility.EnsureFolder(MeshesFolder);
            string path = $"{MeshesFolder}/{mesh}.asset";
            Mesh asset = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            if (asset == null)
            {
                asset = new Mesh { name = mesh };
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.Clear();
            asset.SetVertices(vertices);
            asset.SetTriangles(triangles, 0);
            asset.RecalculateNormals();
            asset.RecalculateBounds();
            EditorUtility.SetDirty(asset);

            GameObject go = BattleEditorUtility.CreateChild(mesh, parent, position);
            go.transform.localRotation = Quaternion.Euler(euler);
            go.AddComponent<MeshFilter>().sharedMesh = asset;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;

            return go;
        }

        private static string Sanitize(string name)
        {
            return name.Replace(" ", string.Empty).Replace("'", string.Empty);
        }

        private static Material Glass(string name, Color color)
        {
            Material material = BattleEditorUtility.GetMaterial(name, new Color(color.r, color.g, color.b, 1f), 0.15f, 0.92f);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            material.SetFloat("_ZWrite", 1f);
            material.SetOverrideTag("RenderType", "Opaque");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = -1;
            EditorUtility.SetDirty(material);

            return material;
        }
    }
}
