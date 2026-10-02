using System.Collections.Generic;
using Game.Scripts.Dungeon;
using Game.Scripts.Editor.Battle;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Scripts.Editor.Dungeon
{
    /// Builds the whole uGUI layer: tavern, HUD, inventory screen, result screen and help.
    internal static class DungeonUiBuilder
    {
        private const float Cell = 44f;
        private const string HelpText =
            "<b>Dark and Darker prototype — controls</b>\n" +
            "WASD move · Shift walk (quiet) · Space jump · Ctrl/C crouch (duck under swings)\n" +
            "LMB attack / draw · RMB block · 1 / 2 weapon sets · Tab inventory · F interact (hold)\n" +
            "Q / E skills · Z X V R T spells · 5 6 7 8 belt items · G rest · Esc cursor · H help\n" +
            "Loot chests, descend by the red portal, escape through a blue portal before the Dark Swarm closes.";

        private static readonly Color s_panel = new(0.04f, 0.035f, 0.03f, 0.92f);
        private static readonly Color s_panelLight = new(0.12f, 0.1f, 0.08f, 0.95f);
        private static readonly Color s_frame = new(0.45f, 0.36f, 0.22f, 1f);

        public static GameObject Build(DungeonContext context, ItemDatabase database)
        {
            GameObject canvasObject = new GameObject("[UI]");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.transform.SetParent(canvasObject.transform, false);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            Transform root = canvasObject.transform;
            ItemView itemPrefab = BuildItemPrefab();
            Image cellPrefab = BuildCellPrefab();

            DungeonHudView hud = BuildHud(root, context);
            InventoryView dungeonInventory = BuildInventory(root, canvas, database, itemPrefab, cellPrefab, "Inventory", true);
            LobbyView lobby = BuildLobby(root, canvas, context, database, itemPrefab, cellPrefab);
            ResultView result = BuildResult(root);
            HelpView help = BuildHelp(root);

            DungeonUiRoot uiRoot = canvasObject.AddComponent<DungeonUiRoot>();
            SerializedObject so = new SerializedObject(uiRoot);
            BattleEditorUtility.Set(so, "_context", context);
            BattleEditorUtility.Set(so, "_lobby", lobby);
            BattleEditorUtility.Set(so, "_hud", hud);
            BattleEditorUtility.Set(so, "_inventory", dungeonInventory);
            BattleEditorUtility.Set(so, "_result", result);
            BattleEditorUtility.Set(so, "_help", help);
            so.ApplyModifiedPropertiesWithoutUndo();

            hud.gameObject.SetActive(false);
            dungeonInventory.gameObject.SetActive(false);
            result.gameObject.SetActive(false);
            help.gameObject.SetActive(false);

            return canvasObject;
        }

        private static ItemView BuildItemPrefab()
        {
            GameObject root = new GameObject("ItemView", typeof(RectTransform));
            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(Cell, Cell);

            Image background = root.AddComponent<Image>();
            background.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            CanvasGroup group = root.AddComponent<CanvasGroup>();

            Image frame = CreateImage("Frame", rect, Vector2.zero, Vector2.zero, Vector2.zero, Color.white);
            Stretch(frame.rectTransform, 0f);
            frame.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            frame.type = Image.Type.Sliced;
            frame.fillCenter = false;
            frame.raycastTarget = false;

            TMP_Text glyph = CreateText("Glyph", rect, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 18f, TextAlignmentOptions.Center);
            Stretch(glyph.rectTransform, 2f);
            glyph.fontStyle = FontStyles.Bold;
            glyph.enableAutoSizing = true;
            glyph.fontSizeMin = 10f;
            glyph.fontSizeMax = 26f;

            TMP_Text count = CreateText("Count", rect, new Vector2(1f, 0f), new Vector2(-3f, 2f), new Vector2(40f, 16f), 12f, TextAlignmentOptions.BottomRight);
            count.rectTransform.pivot = new Vector2(1f, 0f);

            SerializedObject so = new SerializedObject(root.AddComponent<ItemView>());
            BattleEditorUtility.Set(so, "_background", background);
            BattleEditorUtility.Set(so, "_frame", frame);
            BattleEditorUtility.Set(so, "_glyph", glyph);
            BattleEditorUtility.Set(so, "_count", count);
            BattleEditorUtility.Set(so, "_group", group);
            so.ApplyModifiedPropertiesWithoutUndo();

            BattleEditorUtility.EnsureFolder(DungeonPropBuilder.PrefabsFolder + "/UI");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DungeonPropBuilder.PrefabsFolder + "/UI/ItemView.prefab");
            Object.DestroyImmediate(root);

            return prefab.GetComponent<ItemView>();
        }

        private static Image BuildCellPrefab()
        {
            GameObject root = new GameObject("Cell", typeof(RectTransform));
            Image image = root.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.55f);
            image.raycastTarget = true;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DungeonPropBuilder.PrefabsFolder + "/UI/Cell.prefab");
            Object.DestroyImmediate(root);

            return prefab.GetComponent<Image>();
        }

        private static DungeonHudView BuildHud(Transform root, DungeonContext context)
        {
            RectTransform hud = CreateRect("HUD", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(hud, 0f);
            Vector2 center = new Vector2(0.5f, 0.5f);
            Vector2 bottom = new Vector2(0.5f, 0f);
            Vector2 top = new Vector2(0.5f, 1f);
            Vector2 topLeft = new Vector2(0f, 1f);
            Vector2 topRight = Vector2.one;
            Vector2 bottomRight = new Vector2(1f, 0f);
            Vector2 bottomLeft = Vector2.zero;

            Image vignette = CreateImage("Vignette", hud, center, Vector2.zero, Vector2.zero, new Color(0.5f, 0f, 0f, 0f));
            Stretch(vignette.rectTransform, 0f);

            CreateImage("Crosshair", hud, center, Vector2.zero, new Vector2(5f, 5f), new Color(1f, 1f, 1f, 0.8f));
            Image ring = CreateImage("PromptRing", hud, center, Vector2.zero, new Vector2(56f, 56f), new Color(1f, 0.9f, 0.6f, 0.9f));
            MakeRadial(ring);
            TMP_Text prompt = CreateText("Prompt", hud, center, new Vector2(0f, -56f), new Vector2(700f, 30f), 22f, TextAlignmentOptions.Center);

            RectTransform castRoot = CreateRect("CastBar", hud, center, new Vector2(0f, -90f), new Vector2(240f, 12f));
            CreateImage("Back", castRoot, center, Vector2.zero, new Vector2(240f, 12f), new Color(0f, 0f, 0f, 0.6f));
            Image castFill = CreateImage("Fill", castRoot, center, Vector2.zero, new Vector2(236f, 8f), new Color(0.6f, 0.8f, 1f));
            MakeFilled(castFill);

            CreateImage("HealthBack", hud, bottom, new Vector2(0f, 42f), new Vector2(520f, 34f), new Color(0.05f, 0.03f, 0.02f, 0.85f));
            CreateImage("HealthFrame", hud, bottom, new Vector2(0f, 42f), new Vector2(524f, 38f), s_frame).transform.SetAsFirstSibling();
            Image healthFill = CreateImage("HealthFill", hud, bottom, new Vector2(0f, 42f), new Vector2(512f, 26f), new Color(0.72f, 0.12f, 0.1f));
            MakeFilled(healthFill);
            TMP_Text healthText = CreateText("HealthText", hud, bottom, new Vector2(0f, 42f), new Vector2(512f, 26f), 18f, TextAlignmentOptions.Center);
            TMP_Text nameText = CreateText("Name", hud, bottom, new Vector2(0f, 68f), new Vector2(600f, 24f), 18f, TextAlignmentOptions.Center);
            nameText.color = new Color(0.9f, 0.85f, 0.7f);

            TMP_Text timer = CreateText("Timer", hud, top, new Vector2(0f, -30f), new Vector2(300f, 40f), 34f, TextAlignmentOptions.Center);
            TMP_Text swarm = CreateText("Swarm", hud, top, new Vector2(0f, -66f), new Vector2(800f, 28f), 20f, TextAlignmentOptions.Center);
            TMP_Text floor = CreateText("Floor", hud, topRight, new Vector2(-30f, -30f), new Vector2(400f, 30f), 22f, TextAlignmentOptions.MidlineRight);
            TMP_Text kills = CreateText("Kills", hud, topRight, new Vector2(-30f, -60f), new Vector2(400f, 26f), 18f, TextAlignmentOptions.MidlineRight);
            TMP_Text weapon = CreateText("Weapon", hud, bottomRight, new Vector2(-30f, 40f), new Vector2(600f, 32f), 24f, TextAlignmentOptions.MidlineRight);
            TMP_Text effects = CreateText("Effects", hud, bottomLeft, new Vector2(30f, 140f), new Vector2(400f, 220f), 18f, TextAlignmentOptions.BottomLeft);
            effects.rectTransform.pivot = new Vector2(0f, 0f);
            effects.color = new Color(0.8f, 0.95f, 1f);

            List<(GameObject, TMP_Text, TMP_Text, TMP_Text, Image)> skills = new();
            skills.Add(AbilitySlot(hud, bottom, new Vector2(-320f, 50f), "Q"));
            skills.Add(AbilitySlot(hud, bottom, new Vector2(320f, 50f), "E"));
            List<(GameObject, TMP_Text, TMP_Text, TMP_Text, Image)> spells = new();
            string[] spellKeys = { "Z", "X", "V", "R", "T" };

            for (int i = 0; i < 5; i++)
                spells.Add(AbilitySlot(hud, bottomRight, new Vector2(-370f + i * 64f, 110f), spellKeys[i]));

            List<(GameObject, TMP_Text, TMP_Text)> belt = new();

            for (int i = 0; i < 4; i++)
                belt.Add(BeltSlot(hud, bottom, new Vector2(-96f + i * 64f, 112f), (5 + i).ToString()));

            DungeonHudView view = hud.gameObject.AddComponent<DungeonHudView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_context", context);
            BattleEditorUtility.Set(so, "_healthFill", healthFill);
            BattleEditorUtility.Set(so, "_healthText", healthText);
            BattleEditorUtility.Set(so, "_nameText", nameText);
            BattleEditorUtility.Set(so, "_timerText", timer);
            BattleEditorUtility.Set(so, "_floorText", floor);
            BattleEditorUtility.Set(so, "_swarmText", swarm);
            BattleEditorUtility.Set(so, "_weaponText", weapon);
            BattleEditorUtility.Set(so, "_effectsText", effects);
            BattleEditorUtility.Set(so, "_promptText", prompt);
            BattleEditorUtility.Set(so, "_promptRing", ring);
            BattleEditorUtility.Set(so, "_castBar", castFill);
            BattleEditorUtility.Set(so, "_castRoot", castRoot.gameObject);
            BattleEditorUtility.Set(so, "_vignette", vignette);
            BattleEditorUtility.Set(so, "_killText", kills);
            WriteAbilitySlots(so, "_skills", skills);
            WriteAbilitySlots(so, "_spells", spells);
            SerializedProperty beltProperty = so.FindProperty("_belt");
            beltProperty.arraySize = belt.Count;

            for (int i = 0; i < belt.Count; i++)
            {
                SerializedProperty element = beltProperty.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Root").objectReferenceValue = belt[i].Item1;
                element.FindPropertyRelative("Glyph").objectReferenceValue = belt[i].Item2;
                element.FindPropertyRelative("Count").objectReferenceValue = belt[i].Item3;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        private static void WriteAbilitySlots(SerializedObject so, string property, List<(GameObject, TMP_Text, TMP_Text, TMP_Text, Image)> slots)
        {
            SerializedProperty array = so.FindProperty(property);
            array.arraySize = slots.Count;

            for (int i = 0; i < slots.Count; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Root").objectReferenceValue = slots[i].Item1;
                element.FindPropertyRelative("Glyph").objectReferenceValue = slots[i].Item2;
                element.FindPropertyRelative("Key").objectReferenceValue = slots[i].Item3;
                element.FindPropertyRelative("Charges").objectReferenceValue = slots[i].Item4;
                element.FindPropertyRelative("Cooldown").objectReferenceValue = slots[i].Item5;
            }
        }

        private static (GameObject, TMP_Text, TMP_Text, TMP_Text, Image) AbilitySlot(RectTransform parent, Vector2 anchor, Vector2 position, string key)
        {
            RectTransform slot = CreateRect("Ability" + key, parent, anchor, position, new Vector2(56f, 56f));
            CreateImage("Back", slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f), s_panelLight);
            CreateImage("Frame", slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f), s_frame).transform.SetAsFirstSibling();
            TMP_Text glyph = CreateText("Glyph", slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f), 22f, TextAlignmentOptions.Center);
            glyph.fontStyle = FontStyles.Bold;
            Image cooldown = CreateImage("Cooldown", slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f), new Color(0f, 0f, 0f, 0.7f));
            MakeRadial(cooldown);
            TMP_Text keyText = CreateText("Key", slot, new Vector2(0f, 1f), new Vector2(4f, -2f), new Vector2(30f, 16f), 12f, TextAlignmentOptions.TopLeft);
            keyText.rectTransform.pivot = new Vector2(0f, 1f);
            keyText.text = key;
            keyText.color = new Color(1f, 0.9f, 0.6f);
            TMP_Text charges = CreateText("Charges", slot, new Vector2(1f, 0f), new Vector2(-4f, 2f), new Vector2(40f, 16f), 13f, TextAlignmentOptions.BottomRight);
            charges.rectTransform.pivot = new Vector2(1f, 0f);

            return (slot.gameObject, glyph, keyText, charges, cooldown);
        }

        private static (GameObject, TMP_Text, TMP_Text) BeltSlot(RectTransform parent, Vector2 anchor, Vector2 position, string key)
        {
            RectTransform slot = CreateRect("Belt" + key, parent, anchor, position, new Vector2(52f, 52f));
            CreateImage("Back", slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 52f), s_panelLight);
            CreateImage("Frame", slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f), s_frame).transform.SetAsFirstSibling();
            TMP_Text glyph = CreateText("Glyph", slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(52f, 52f), 20f, TextAlignmentOptions.Center);
            glyph.fontStyle = FontStyles.Bold;
            TMP_Text keyText = CreateText("Key", slot, new Vector2(0f, 1f), new Vector2(4f, -2f), new Vector2(30f, 16f), 12f, TextAlignmentOptions.TopLeft);
            keyText.rectTransform.pivot = new Vector2(0f, 1f);
            keyText.text = key;
            keyText.color = new Color(1f, 0.9f, 0.6f);
            TMP_Text count = CreateText("Count", slot, new Vector2(1f, 0f), new Vector2(-4f, 2f), new Vector2(40f, 16f), 13f, TextAlignmentOptions.BottomRight);
            count.rectTransform.pivot = new Vector2(1f, 0f);

            return (slot.gameObject, glyph, count);
        }

        private static InventoryView BuildInventory(Transform root, Canvas canvas, ItemDatabase database, ItemView itemPrefab, Image cellPrefab, string title, bool fullscreen)
        {
            RectTransform panel = CreateRect(title + "Panel", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));

            if (fullscreen)
            {
                Image back = CreateImage("Back", panel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.02f, 0.02f, 0.02f, 0.78f));
                Stretch(back.rectTransform, 0f);
            }

            TMP_Text titleText = CreateText("Title", panel, new Vector2(0f, 1f), new Vector2(560f, -60f), new Vector2(400f, 36f), 28f, TextAlignmentOptions.MidlineLeft);
            titleText.rectTransform.pivot = new Vector2(0f, 1f);
            titleText.color = new Color(0.9f, 0.82f, 0.6f);

            RectTransform doll = CreateRect("PaperDoll", panel, new Vector2(0f, 1f), new Vector2(60f, -60f), new Vector2(480f, 640f));
            doll.pivot = new Vector2(0f, 1f);
            CreateImage("Back", doll, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, s_panel).rectTransform.StretchFill();
            List<EquipSlotView> slots = new()
            {
                Slot(doll, EquipSlot.Head, "Head", new Vector2(20f, -20f), 2, 2, itemPrefab),
                Slot(doll, EquipSlot.Necklace, "Neck", new Vector2(120f, -20f), 1, 1, itemPrefab),
                Slot(doll, EquipSlot.Chest, "Chest", new Vector2(20f, -116f), 2, 3, itemPrefab),
                Slot(doll, EquipSlot.Back, "Back", new Vector2(120f, -116f), 2, 3, itemPrefab),
                Slot(doll, EquipSlot.Hands, "Hands", new Vector2(20f, -256f), 2, 2, itemPrefab),
                Slot(doll, EquipSlot.Ring1, "Ring", new Vector2(120f, -256f), 1, 1, itemPrefab),
                Slot(doll, EquipSlot.Ring2, "Ring", new Vector2(168f, -256f), 1, 1, itemPrefab),
                Slot(doll, EquipSlot.Legs, "Legs", new Vector2(20f, -352f), 2, 2, itemPrefab),
                Slot(doll, EquipSlot.Feet, "Feet", new Vector2(120f, -352f), 2, 2, itemPrefab),
                Slot(doll, EquipSlot.Weapon1Main, "I main", new Vector2(240f, -20f), 1, 5, itemPrefab),
                Slot(doll, EquipSlot.Weapon1Off, "I off", new Vector2(292f, -20f), 2, 3, itemPrefab),
                Slot(doll, EquipSlot.Weapon2Main, "II main", new Vector2(240f, -256f), 1, 5, itemPrefab),
                Slot(doll, EquipSlot.Weapon2Off, "II off", new Vector2(292f, -256f), 2, 3, itemPrefab),
                Slot(doll, EquipSlot.Utility1, "5", new Vector2(240f, -500f), 1, 1, itemPrefab),
                Slot(doll, EquipSlot.Utility2, "6", new Vector2(292f, -500f), 1, 1, itemPrefab),
                Slot(doll, EquipSlot.Utility3, "7", new Vector2(344f, -500f), 1, 1, itemPrefab),
                Slot(doll, EquipSlot.Utility4, "8", new Vector2(396f, -500f), 1, 1, itemPrefab)
            };

            TMP_Text stats = CreateText("Stats", panel, new Vector2(0f, 1f), new Vector2(60f, -720f), new Vector2(480f, 320f), 15f, TextAlignmentOptions.TopLeft);
            stats.rectTransform.pivot = new Vector2(0f, 1f);
            CreateImage("StatsBack", panel, new Vector2(0f, 1f), new Vector2(50f, -710f), new Vector2(500f, 340f), s_panel).rectTransform.pivot = new Vector2(0f, 1f);
            stats.transform.SetAsLastSibling();

            ItemGridView bag = Grid(panel, "Bag", new Vector2(560f, -110f), itemPrefab, cellPrefab);
            RectTransform otherPanel = CreateRect("OtherPanel", panel, new Vector2(0f, 1f), new Vector2(1180f, -60f), new Vector2(620f, 420f));
            otherPanel.pivot = new Vector2(0f, 1f);
            CreateImage("Back", otherPanel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, s_panel).rectTransform.StretchFill();
            TMP_Text otherTitle = CreateText("Title", otherPanel, new Vector2(0f, 1f), new Vector2(16f, -10f), new Vector2(400f, 30f), 22f, TextAlignmentOptions.MidlineLeft);
            otherTitle.rectTransform.pivot = new Vector2(0f, 1f);
            otherTitle.color = new Color(0.9f, 0.82f, 0.6f);
            ItemGridView other = Grid(otherPanel, "Other", new Vector2(16f, -50f), itemPrefab, cellPrefab);
            Button takeAll = CreateButton("TakeAll", otherPanel, new Vector2(1f, 1f), new Vector2(-16f, -12f), new Vector2(140f, 32f), "Take all");
            ((RectTransform)takeAll.transform).pivot = new Vector2(1f, 1f);

            RectTransform tooltip = CreateRect("Tooltip", panel, new Vector2(0f, 1f), Vector2.zero, new Vector2(380f, 230f));
            tooltip.pivot = new Vector2(0f, 1f);
            Image tooltipBack = CreateImage("Back", tooltip, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.03f, 0.03f, 0.03f, 0.95f));
            tooltipBack.rectTransform.StretchFill();
            TMP_Text tooltipText = CreateText("Text", tooltip, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 15f, TextAlignmentOptions.TopLeft);
            Stretch(tooltipText.rectTransform, 10f);

            RectTransform ghost = CreateRect("DragGhost", panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Cell, Cell));
            Image ghostImage = ghost.gameObject.AddComponent<Image>();
            ghostImage.color = new Color(1f, 1f, 1f, 0.35f);
            ghostImage.raycastTarget = false;
            TMP_Text ghostText = CreateText("Glyph", ghost, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 20f, TextAlignmentOptions.Center);
            Stretch(ghostText.rectTransform, 0f);
            ghostText.fontStyle = FontStyles.Bold;

            InventoryView view = panel.gameObject.AddComponent<InventoryView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_database", database);
            BattleEditorUtility.Set(so, "_bagGrid", bag);
            BattleEditorUtility.Set(so, "_otherGrid", other);
            BattleEditorUtility.Set(so, "_otherTitle", otherTitle);
            BattleEditorUtility.Set(so, "_otherPanel", otherPanel.gameObject);
            BattleEditorUtility.Set(so, "_takeAllButton", takeAll);
            BattleEditorUtility.Set(so, "_slots", slots);
            BattleEditorUtility.Set(so, "_statsText", stats);
            BattleEditorUtility.Set(so, "_titleText", titleText);
            BattleEditorUtility.Set(so, "_tooltip", tooltip);
            BattleEditorUtility.Set(so, "_tooltipText", tooltipText);
            BattleEditorUtility.Set(so, "_dragGhost", ghost);
            BattleEditorUtility.Set(so, "_dragGhostText", ghostText);
            BattleEditorUtility.Set(so, "_canvas", canvas);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        private static EquipSlotView Slot(RectTransform parent, EquipSlot slot, string label, Vector2 position, int width, int height, ItemView itemPrefab)
        {
            RectTransform rect = CreateRect("Slot" + slot, parent, new Vector2(0f, 1f), position, new Vector2(width * Cell, height * Cell));
            rect.pivot = new Vector2(0f, 1f);
            Image background = rect.gameObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.6f);
            TMP_Text labelText = CreateText("Label", rect, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 12f, TextAlignmentOptions.Center);
            Stretch(labelText.rectTransform, 2f);
            labelText.text = label;
            labelText.color = new Color(1f, 1f, 1f, 0.4f);

            ItemView item = (ItemView)PrefabUtility.InstantiatePrefab(itemPrefab, rect);
            RectTransform itemRect = (RectTransform)item.transform;
            itemRect.anchorMin = itemRect.anchorMax = itemRect.pivot = new Vector2(0f, 1f);
            itemRect.anchoredPosition = Vector2.zero;
            itemRect.sizeDelta = rect.sizeDelta;
            item.gameObject.SetActive(false);

            EquipSlotView view = rect.gameObject.AddComponent<EquipSlotView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_slot", slot);
            BattleEditorUtility.Set(so, "_background", background);
            BattleEditorUtility.Set(so, "_label", labelText);
            BattleEditorUtility.Set(so, "_item", item);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        private static ItemGridView Grid(RectTransform parent, string name, Vector2 position, ItemView itemPrefab, Image cellPrefab)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), position, new Vector2(Cell * 10f, Cell * 4f));
            rect.pivot = new Vector2(0f, 1f);
            RectTransform cells = CreateRect("Cells", rect, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            cells.pivot = new Vector2(0f, 1f);
            RectTransform items = CreateRect("Items", rect, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            items.pivot = new Vector2(0f, 1f);

            ItemGridView view = rect.gameObject.AddComponent<ItemGridView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_cellsRoot", cells);
            BattleEditorUtility.Set(so, "_itemsRoot", items);
            BattleEditorUtility.Set(so, "_itemPrefab", itemPrefab);
            BattleEditorUtility.Set(so, "_cellPrefab", cellPrefab);
            BattleEditorUtility.Set(so, "_cellSize", Cell);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        private static LobbyView BuildLobby(Transform root, Canvas canvas, DungeonContext context, ItemDatabase database, ItemView itemPrefab, Image cellPrefab)
        {
            RectTransform panel = CreateRect("Lobby", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(panel, 0f);
            Image back = CreateImage("Back", panel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.05f, 0.04f, 0.035f, 1f));
            Stretch(back.rectTransform, 0f);

            TMP_Text title = CreateText("Title", panel, new Vector2(0f, 1f), new Vector2(60f, -20f), new Vector2(800f, 40f), 34f, TextAlignmentOptions.MidlineLeft);
            title.rectTransform.pivot = new Vector2(0f, 1f);
            title.text = "THE TAVERN";
            title.color = new Color(0.9f, 0.8f, 0.55f);
            TMP_Text profile = CreateText("Profile", panel, new Vector2(1f, 1f), new Vector2(-60f, -24f), new Vector2(900f, 30f), 20f, TextAlignmentOptions.MidlineRight);
            profile.rectTransform.pivot = new Vector2(1f, 1f);

            RectTransform classes = CreateRect("Classes", panel, new Vector2(0f, 1f), new Vector2(1180f, -60f), new Vector2(620f, 60f));
            classes.pivot = new Vector2(0f, 1f);
            GridLayoutGroup layout = classes.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(118f, 34f);
            layout.spacing = new Vector2(6f, 6f);
            Button classButton = CreateButton("ClassButton", classes, new Vector2(0f, 1f), Vector2.zero, new Vector2(118f, 34f), "Class");
            classButton.gameObject.SetActive(false);

            RectTransform infoPanel = CreateRect("ClassInfo", panel, new Vector2(0f, 1f), new Vector2(1180f, -150f), new Vector2(620f, 560f));
            infoPanel.pivot = new Vector2(0f, 1f);
            CreateImage("Back", infoPanel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, s_panel).rectTransform.StretchFill();
            TMP_Text info = CreateText("Text", infoPanel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 14f, TextAlignmentOptions.TopLeft);
            Stretch(info.rectTransform, 12f);

            Button enter = CreateButton("Enter", panel, new Vector2(1f, 0f), new Vector2(-60f, 40f), new Vector2(300f, 56f), "ENTER THE DUNGEON");
            ((RectTransform)enter.transform).pivot = new Vector2(1f, 0f);
            enter.image.color = new Color(0.45f, 0.12f, 0.1f);
            enter.GetComponentInChildren<TMP_Text>().fontSize = 22f;
            Button reset = CreateButton("ResetKit", panel, new Vector2(1f, 0f), new Vector2(-380f, 40f), new Vector2(200f, 44f), "Squire kit");
            ((RectTransform)reset.transform).pivot = new Vector2(1f, 0f);
            Button wipe = CreateButton("Wipe", panel, new Vector2(1f, 0f), new Vector2(-600f, 40f), new Vector2(160f, 44f), "Wipe save");
            ((RectTransform)wipe.transform).pivot = new Vector2(1f, 0f);

            TMP_Text result = CreateText("Result", panel, new Vector2(0f, 0f), new Vector2(60f, 50f), new Vector2(1000f, 30f), 20f, TextAlignmentOptions.MidlineLeft);
            result.rectTransform.pivot = new Vector2(0f, 0f);

            InventoryView inventory = BuildInventory(panel, canvas, database, itemPrefab, cellPrefab, "Kit", false);
            RectTransform inventoryRect = (RectTransform)inventory.transform;
            inventoryRect.anchorMin = inventoryRect.anchorMax = new Vector2(0f, 1f);
            inventoryRect.pivot = new Vector2(0f, 1f);
            inventoryRect.anchoredPosition = new Vector2(0f, -40f);
            inventoryRect.sizeDelta = new Vector2(1920f, 1080f);
            inventory.transform.Find("OtherPanel").GetComponent<RectTransform>().anchoredPosition = new Vector2(560f, -340f);
            inventory.transform.Find("OtherPanel").GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 300f);
            inventory.transform.Find("Title").GetComponent<RectTransform>().anchoredPosition = new Vector2(560f, -60f);
            inventory.transform.Find("Bag").GetComponent<RectTransform>().anchoredPosition = new Vector2(560f, -110f);

            LobbyView view = panel.gameObject.AddComponent<LobbyView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_context", context);
            BattleEditorUtility.Set(so, "_inventory", inventory);
            BattleEditorUtility.Set(so, "_classButtonsRoot", classes);
            BattleEditorUtility.Set(so, "_classButtonPrefab", classButton);
            BattleEditorUtility.Set(so, "_classInfoText", info);
            BattleEditorUtility.Set(so, "_profileText", profile);
            BattleEditorUtility.Set(so, "_resultText", result);
            BattleEditorUtility.Set(so, "_enterButton", enter);
            BattleEditorUtility.Set(so, "_resetKitButton", reset);
            BattleEditorUtility.Set(so, "_wipeButton", wipe);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        private static ResultView BuildResult(Transform root)
        {
            RectTransform panel = CreateRect("Result", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(panel, 0f);
            Image back = CreateImage("Back", panel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.85f));
            Stretch(back.rectTransform, 0f);
            TMP_Text title = CreateText("Title", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1000f, 100f), 72f, TextAlignmentOptions.Center);
            title.fontStyle = FontStyles.Bold;
            TMP_Text details = CreateText("Details", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(900f, 140f), 24f, TextAlignmentOptions.Center);
            Button back2 = CreateButton("Return", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, -130f), new Vector2(300f, 54f), "Return to the tavern");

            ResultView view = panel.gameObject.AddComponent<ResultView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_titleText", title);
            BattleEditorUtility.Set(so, "_detailsText", details);
            BattleEditorUtility.Set(so, "_returnButton", back2);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        private static HelpView BuildHelp(Transform root)
        {
            RectTransform panel = CreateRect("Help", root, new Vector2(0f, 1f), new Vector2(30f, -100f), new Vector2(980f, 170f));
            panel.pivot = new Vector2(0f, 1f);
            CreateImage("Back", panel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.7f)).rectTransform.StretchFill();
            TMP_Text text = CreateText("Text", panel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 18f, TextAlignmentOptions.TopLeft);
            Stretch(text.rectTransform, 12f);
            text.text = HelpText;

            return panel.gameObject.AddComponent<HelpView>();
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            return rect;
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private static void StretchFill(this RectTransform rect)
        {
            Stretch(rect, 0f);
        }

        private static Image CreateImage(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            Image image = CreateRect(name, parent, anchor, position, size).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            return image;
        }

        private static void MakeFilled(Image image)
        {
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
        }

        private static void MakeRadial(Image image)
        {
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = true;
            image.fillAmount = 0f;
        }

        private static TMP_Text CreateText(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI text = CreateRect(name, parent, anchor, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.text = string.Empty;
            text.richText = true;

            return text;
        }

        private static Button CreateButton(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string label)
        {
            RectTransform rect = CreateRect(name, parent, anchor, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = s_panelLight;
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            Button button = rect.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.3f, 1.25f, 1.1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            button.colors = colors;
            TMP_Text text = CreateText("Label", rect, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 16f, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 2f);
            text.text = label;

            return button;
        }
    }
}
