using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor.Dungeon
{
    /// Small 3D models for consumables, utilities and treasures: used on the floor and rendered into inventory icons.
    internal static class DungeonItemModelBuilder
    {
        public const string Folder = DungeonPropBuilder.PrefabsFolder + "/Items";

        public static GameObject Build(ItemDef def)
        {
            BattleEditorUtility.EnsureFolder(Folder);
            GameObject root = new GameObject("Item_" + def.Name.Replace(" ", string.Empty));
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
            Material glass = Glass("Potion" + def.Name.Replace(" ", string.Empty), def.Color);
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

            if (def.Name is "Ruby" or "Emerald" or "Sapphire" or "Diamond")
            {
                Material gem = Glass("Gem" + def.Name, def.Color);
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Top", parent, new Vector3(0f, 0.07f, 0f), new Vector3(45f, 45f, 0f), new Vector3(0.07f, 0.07f, 0.07f), gem);

                return;
            }

            if (def.Name.Contains("Goblet"))
            {
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Cup", parent, new Vector3(0f, 0.14f, 0f), Vector3.zero, new Vector3(0.09f, 0.05f, 0.09f), gold);
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Stem", parent, new Vector3(0f, 0.06f, 0f), Vector3.zero, new Vector3(0.025f, 0.05f, 0.025f), gold);
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Base", parent, new Vector3(0f, 0.01f, 0f), Vector3.zero, new Vector3(0.08f, 0.01f, 0.08f), gold);

                return;
            }

            if (def.Name.Contains("Candlestick"))
            {
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Base", parent, new Vector3(0f, 0.01f, 0f), Vector3.zero, new Vector3(0.09f, 0.01f, 0.09f), gold);
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Stem", parent, new Vector3(0f, 0.1f, 0f), Vector3.zero, new Vector3(0.02f, 0.09f, 0.02f), gold);
                BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Candle", parent, new Vector3(0f, 0.24f, 0f), Vector3.zero, new Vector3(0.025f, 0.05f, 0.025f), BattleEditorUtility.GetMaterial("Linen", new Color(0.88f, 0.86f, 0.78f), 0f, 0.1f));

                return;
            }

            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cylinder, "Scroll", parent, new Vector3(0f, 0.03f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.05f, 0.12f, 0.05f), BattleEditorUtility.GetMaterial("Parchment", new Color(0.85f, 0.75f, 0.5f), 0f, 0.15f));
            BattleEditorUtility.CreatePrimitive(PrimitiveType.Cube, "Ribbon", parent, new Vector3(0f, 0.03f, 0f), Vector3.zero, new Vector3(0.055f, 0.055f, 0.03f), BattleEditorUtility.GetMaterial("ClothRibbon", new Color(0.6f, 0.1f, 0.1f), 0f, 0.2f));
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
