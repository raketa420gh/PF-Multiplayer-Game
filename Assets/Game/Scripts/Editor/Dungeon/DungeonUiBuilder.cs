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
    /// Builds the whole uGUI layer laid out like Dark and Darker: compass, HP bar with Q/E, belt, weapon row, minimap, inventory with 3D doll, tavern.
    internal static class DungeonUiBuilder
    {
        public const string PreviewLayer = "Preview";
        private const float Cell = 44f;
        private const string HelpText =
            "<b>Controls</b>\n" +
            "WASD move · Shift walk (quiet) · Space jump · Ctrl/C crouch (duck under swings)\n" +
            "LMB attack / draw · RMB block, or hold to cast a readied spell · 1 / 2 weapon sets · Tab inventory\n" +
            "3 / 4 belt item in hand (press again for the next of three), LMB use, RMB put away · F interact (hold) · Q / E skills · G rest · H help\n" +
            "Casters need a staff, spellbook or crystal ball in hand; bards need an instrument. Rest at a campfire to recover charges.";

        private static readonly Color s_panel = new(0.04f, 0.035f, 0.03f, 0.92f);
        private static readonly Color s_panelLight = new(0.12f, 0.1f, 0.08f, 0.95f);
        private static readonly Color s_frame = new(0.45f, 0.36f, 0.22f, 1f);
        private static readonly Color s_gold = new(0.85f, 0.72f, 0.45f);

        public sealed class Inputs
        {
            public DungeonContext Context;
            public ItemDatabase Database;
            public Camera Camera;
            public GameObject PreviewRig;
            public ArmorPieceSetConfig PieceSet;
            public Texture2D[] FloorMaps;
            public string[] ModuleNames;
        }

        public static GameObject Build(Inputs inputs)
        {
            BattleEditorUtility.EnsureLayer(PreviewLayer);
            GameObject canvasObject = new GameObject("[UI]");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            Camera uiCamera = BuildUiCamera(inputs.Camera);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;
            canvas.planeDistance = 1f;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            canvasObject.AddComponent<GraphicRaycaster>();

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.transform.SetParent(canvasObject.transform, false);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();

            inputs.Camera.cullingMask &= ~(1 << LayerMask.NameToLayer(PreviewLayer));

            Transform root = canvasObject.transform;
            ItemView itemPrefab = BuildItemPrefab();
            Image cellPrefab = BuildCellPrefab();

            DungeonHudView hud = BuildHud(root, inputs);
            CharacterPreviewView dungeonPreview;
            InventoryView dungeonInventory = BuildInventory(root, canvas, inputs, itemPrefab, cellPrefab, "Inventory", true, 0, out dungeonPreview);
            LobbyView lobby = BuildLobby(root, canvas, inputs, itemPrefab, cellPrefab);
            ResultView result = BuildResult(root);
            HelpView help = BuildHelp(root);
            SpellWheelView wheel = BuildWheel(root);

            DungeonUiRoot uiRoot = canvasObject.AddComponent<DungeonUiRoot>();
            SerializedObject so = new SerializedObject(uiRoot);
            BattleEditorUtility.Set(so, "_context", inputs.Context);
            BattleEditorUtility.Set(so, "_lobby", lobby);
            BattleEditorUtility.Set(so, "_hud", hud);
            BattleEditorUtility.Set(so, "_inventory", dungeonInventory);
            BattleEditorUtility.Set(so, "_result", result);
            BattleEditorUtility.Set(so, "_help", help);
            BattleEditorUtility.Set(so, "_wheel", wheel);
            BattleEditorUtility.Set(so, "_inventoryPreview", dungeonPreview);
            so.ApplyModifiedPropertiesWithoutUndo();

            hud.gameObject.SetActive(false);
            dungeonInventory.gameObject.SetActive(false);
            result.gameObject.SetActive(false);
            help.gameObject.SetActive(false);
            wheel.gameObject.SetActive(false);
            BattleEditorUtility.SetLayerRecursively(canvasObject, LayerMask.NameToLayer("UI"));

            return canvasObject;
        }

        /// Overlay camera in the main camera's stack: the UI always draws above first-person weapons.
        private static Camera BuildUiCamera(Camera main)
        {
            GameObject go = BattleEditorUtility.CreateChild("UICamera", main.transform);
            Camera camera = go.AddComponent<Camera>();
            camera.cullingMask = 1 << LayerMask.NameToLayer("UI");
            camera.clearFlags = CameraClearFlags.Depth;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 10f;
            camera.orthographic = false;
            camera.fieldOfView = 60f;
            UnityEngine.Rendering.Universal.UniversalAdditionalCameraData data = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Overlay;
            data.renderPostProcessing = false;
            // Overlay cameras keep the base depth by default; the canvas plane would be hidden by nearby walls or the floor after death.
            SerializedObject dataObject = new SerializedObject(data);
            dataObject.FindProperty("m_ClearDepth").boolValue = true;
            dataObject.ApplyModifiedPropertiesWithoutUndo();
            main.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
            UnityEngine.Rendering.Universal.CameraExtensions.GetUniversalAdditionalCameraData(main).cameraStack.Add(camera);

            return camera;
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

            Image icon = CreateImage("Icon", rect, Vector2.zero, Vector2.zero, Vector2.zero, Color.white);
            Stretch(icon.rectTransform, 3f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

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
            BattleEditorUtility.Set(so, "_icon", icon);
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
            image.color = new Color(0.16f, 0.14f, 0.11f, 0.85f);
            image.raycastTarget = true;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, DungeonPropBuilder.PrefabsFolder + "/UI/Cell.prefab");
            Object.DestroyImmediate(root);

            return prefab.GetComponent<Image>();
        }

        private static DungeonHudView BuildHud(Transform root, Inputs inputs)
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

            BuildCompass(hud, inputs.Context);

            CreateImage("Crosshair", hud, center, Vector2.zero, new Vector2(4f, 4f), new Color(1f, 1f, 1f, 0.75f));
            BattleEditorUtility.CreateSwingHint(hud, inputs.Context.Battle);
            Image ring = CreateImage("PromptRing", hud, center, Vector2.zero, new Vector2(60f, 60f), new Color(1f, 0.9f, 0.6f, 0.9f));
            MakeRadial(ring);
            TMP_Text prompt = CreateText("Prompt", hud, center, new Vector2(0f, -58f), new Vector2(700f, 30f), 20f, TextAlignmentOptions.Center);

            RectTransform castRoot = CreateRect("CastBar", hud, bottom, new Vector2(0f, 190f), new Vector2(240f, 60f));
            TMP_Text castText = CreateText("Name", castRoot, center, new Vector2(0f, 20f), new Vector2(300f, 24f), 18f, TextAlignmentOptions.Center);
            castText.color = new Color(0.95f, 0.9f, 0.85f);
            CreateImage("Back", castRoot, center, Vector2.zero, new Vector2(240f, 14f), new Color(0f, 0f, 0f, 0.7f));
            Image castFill = CreateImage("Fill", castRoot, center, Vector2.zero, new Vector2(236f, 10f), new Color(0.78f, 0.1f, 0.42f));
            MakeFilled(castFill);
            TMP_Text cancel = CreateText("Cancel", castRoot, center, new Vector2(0f, -22f), new Vector2(300f, 20f), 14f, TextAlignmentOptions.Center);
            cancel.text = "[F] Cancel";
            cancel.color = new Color(0.8f, 0.8f, 0.8f);

            // Health bar with skill boxes at the sides, name above, belt and effects over it.
            CreateImage("HealthFrame", hud, bottom, new Vector2(0f, 40f), new Vector2(470f, 40f), s_frame);
            CreateImage("HealthBack", hud, bottom, new Vector2(0f, 40f), new Vector2(462f, 32f), new Color(0.06f, 0.03f, 0.02f, 0.95f));
            Image healthFill = CreateImage("HealthFill", hud, bottom, new Vector2(0f, 40f), new Vector2(452f, 22f), new Color(0.75f, 0.1f, 0.08f));
            MakeFilled(healthFill);
            TMP_Text healthText = CreateText("HealthText", hud, bottom, new Vector2(0f, 40f), new Vector2(452f, 22f), 15f, TextAlignmentOptions.Center);
            TMP_Text nameText = CreateText("Name", hud, bottom, new Vector2(0f, 70f), new Vector2(600f, 22f), 17f, TextAlignmentOptions.Center);
            nameText.color = new Color(0.92f, 0.88f, 0.78f);
            Image restRing = CreateImage("RestRing", hud, bottom, new Vector2(-150f, 74f), new Vector2(26f, 26f), new Color(0.6f, 0.6f, 0.6f, 0.9f));
            MakeRadial(restRing);
            restRing.fillAmount = 1f;
            TMP_Text restKey = CreateText("RestKey", hud, bottom, new Vector2(-124f, 74f), new Vector2(30f, 20f), 12f, TextAlignmentOptions.Center);
            restKey.text = "G";

            List<(GameObject, TMP_Text, TMP_Text, TMP_Text, Image)> skills = new();
            skills.Add(AbilitySlot(hud, bottom, new Vector2(-272f, 44f), "Q"));
            skills.Add(AbilitySlot(hud, bottom, new Vector2(272f, 44f), "E"));
            List<(GameObject, TMP_Text, TMP_Text, TMP_Text, Image)> spells = new();
            TMP_Text readied = CreateText("Readied", hud, bottom, new Vector2(372f, 44f), new Vector2(360f, 44f), 15f, TextAlignmentOptions.MidlineLeft);
            readied.rectTransform.pivot = new Vector2(0f, 0.5f);

            // Two belt groups of three: key 3 cycles the left group, key 4 the right one.
            List<(GameObject, TMP_Text, TMP_Text, Image)> belt = new();

            for (int i = 0; i < 6; i++)
                belt.Add(BeltSlot(hud, bottom, new Vector2((i < 3 ? -182f : 78f) + i % 3 * 52f, 104f), (3 + i / 3).ToString()));

            TMP_Text effects = CreateText("Effects", hud, bottom, new Vector2(0f, 138f), new Vector2(700f, 24f), 13f, TextAlignmentOptions.Center);
            effects.color = new Color(0.8f, 0.95f, 1f);

            // Weapon row and name, bottom right.
            RectTransform weaponRow = CreateRect("WeaponRow", hud, bottomRight, new Vector2(-330f, 56f), new Vector2(240f, 40f));
            TMP_Text[] slotLabels = new TMP_Text[4];

            for (int i = 0; i < 4; i++)
            {
                RectTransform slot = CreateRect("Slot" + (i + 1), weaponRow, new Vector2(0f, 0.5f), new Vector2(i * 62f, 0f), new Vector2(28f, 28f));
                slot.pivot = new Vector2(0f, 0.5f);
                CreateImage("Back", slot, center, Vector2.zero, new Vector2(28f, 28f), s_panelLight);
                CreateImage("Frame", slot, center, Vector2.zero, new Vector2(32f, 32f), s_frame).transform.SetAsFirstSibling();
                slotLabels[i] = CreateText("Key", slot, center, Vector2.zero, new Vector2(28f, 28f), 14f, TextAlignmentOptions.Center);
                slotLabels[i].text = (i + 1).ToString();

                if (i < 3)
                    CreateImage("Line", weaponRow, new Vector2(0f, 0.5f), new Vector2(i * 62f + 30f, 0f), new Vector2(30f, 2f), new Color(0.5f, 0.45f, 0.35f, 0.8f)).rectTransform.pivot = new Vector2(0f, 0.5f);
            }

            CreateImage("WeaponBack", hud, bottomRight, new Vector2(-280f, 22f), new Vector2(300f, 26f), new Color(0.05f, 0.04f, 0.03f, 0.85f));
            TMP_Text weapon = CreateText("Weapon", hud, bottomRight, new Vector2(-280f, 22f), new Vector2(300f, 26f), 16f, TextAlignmentOptions.Center);

            // Minimap, bottom right, with timer above and module name below.
            RectTransform minimapRoot = CreateRect("Minimap", hud, bottomRight, new Vector2(-24f, 50f), new Vector2(230f, 230f));
            minimapRoot.pivot = new Vector2(1f, 0f);
            CreateImage("Frame", minimapRoot, center, Vector2.zero, new Vector2(238f, 238f), s_frame);
            RawImage mapImage = CreateRect("Map", minimapRoot, center, Vector2.zero, new Vector2(230f, 230f)).gameObject.AddComponent<RawImage>();
            mapImage.color = new Color(0.9f, 0.85f, 0.7f);
            mapImage.raycastTarget = false;
            minimapRoot.gameObject.AddComponent<RectMask2D>();
            Image arrow = CreateImage("Arrow", minimapRoot, center, Vector2.zero, new Vector2(14f, 14f), new Color(0.95f, 0.75f, 0.2f));
            arrow.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            RectTransform arrowTip = CreateRect("Tip", arrow.rectTransform, center, new Vector2(0f, 9f), new Vector2(4f, 10f));
            arrowTip.gameObject.AddComponent<Image>().color = new Color(0.95f, 0.75f, 0.2f);
            arrowTip.GetComponent<Image>().raycastTarget = false;
            CreateImage("TimerBack", hud, bottomRight, new Vector2(-24f, 284f), new Vector2(230f, 26f), new Color(0.05f, 0.04f, 0.03f, 0.9f)).rectTransform.pivot = new Vector2(1f, 0f);
            TMP_Text timer = CreateText("Timer", hud, bottomRight, new Vector2(-24f, 284f), new Vector2(230f, 26f), 18f, TextAlignmentOptions.Center);
            timer.rectTransform.pivot = new Vector2(1f, 0f);
            TMP_Text swarm = CreateText("Swarm", hud, bottomRight, new Vector2(-24f, 312f), new Vector2(360f, 22f), 13f, TextAlignmentOptions.MidlineRight);
            swarm.rectTransform.pivot = new Vector2(1f, 0f);
            CreateImage("ModuleBack", hud, bottomRight, new Vector2(-24f, 22f), new Vector2(230f, 26f), new Color(0.3f, 0.22f, 0.1f, 0.95f)).rectTransform.pivot = new Vector2(1f, 0f);
            TMP_Text moduleText = CreateText("Module", hud, bottomRight, new Vector2(-24f, 22f), new Vector2(230f, 26f), 15f, TextAlignmentOptions.Center);
            moduleText.rectTransform.pivot = new Vector2(1f, 0f);
            moduleText.color = s_gold;

            MinimapView minimap = minimapRoot.gameObject.AddComponent<MinimapView>();
            SerializedObject mso = new SerializedObject(minimap);
            BattleEditorUtility.Set(mso, "_context", inputs.Context);
            BattleEditorUtility.Set(mso, "_map", mapImage);
            BattleEditorUtility.Set(mso, "_arrow", arrow.rectTransform);
            BattleEditorUtility.Set(mso, "_moduleText", moduleText);
            BattleEditorUtility.Set(mso, "_floorMaps", inputs.FloorMaps);
            BattleEditorUtility.Set(mso, "_floorModuleNames", inputs.ModuleNames);
            BattleEditorUtility.Set(mso, "_worldSize", DungeonMapBuilder.Module * DungeonMapBuilder.Grid);
            BattleEditorUtility.Set(mso, "_moduleSize", DungeonMapBuilder.Module);
            BattleEditorUtility.Set(mso, "_windowSize", DungeonMapBuilder.Module * 1.3f);
            mso.ApplyModifiedPropertiesWithoutUndo();

            // Top corners.
            RectTransform tab = CreateRect("TabHint", hud, topLeft, new Vector2(24f, -24f), new Vector2(60f, 70f));
            tab.pivot = new Vector2(0f, 1f);
            CreateImage("Back", tab, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(52f, 52f), s_panelLight).rectTransform.pivot = new Vector2(0.5f, 1f);
            TMP_Text tabIcon = CreateText("Icon", tab, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(52f, 36f), 16f, TextAlignmentOptions.Center);
            tabIcon.text = "Bag";
            tabIcon.rectTransform.pivot = new Vector2(0.5f, 1f);
            TMP_Text tabKey = CreateText("Key", tab, new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(60f, 16f), 12f, TextAlignmentOptions.Center);
            tabKey.text = "Tab";
            tabKey.rectTransform.pivot = new Vector2(0.5f, 1f);
            TMP_Text floor = CreateText("Floor", hud, topRight, new Vector2(-30f, -24f), new Vector2(400f, 24f), 16f, TextAlignmentOptions.MidlineRight);
            TMP_Text kills = CreateText("Kills", hud, topRight, new Vector2(-30f, -48f), new Vector2(400f, 22f), 14f, TextAlignmentOptions.MidlineRight);

            RectTransform bossRoot = CreateRect("Boss", hud, top, new Vector2(0f, -70f), new Vector2(520f, 40f));
            TMP_Text bossText = CreateText("Name", bossRoot, center, new Vector2(0f, 12f), new Vector2(520f, 20f), 16f, TextAlignmentOptions.Center);
            bossText.color = new Color(1f, 0.6f, 0.5f);
            CreateImage("Back", bossRoot, center, new Vector2(0f, -8f), new Vector2(520f, 12f), new Color(0f, 0f, 0f, 0.7f));
            Image bossFill = CreateImage("Fill", bossRoot, center, new Vector2(0f, -8f), new Vector2(514f, 8f), new Color(0.8f, 0.15f, 0.1f));
            MakeFilled(bossFill);
            bossRoot.gameObject.SetActive(false);

            DungeonHudView view = hud.gameObject.AddComponent<DungeonHudView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_context", inputs.Context);
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
            BattleEditorUtility.Set(so, "_castText", castText);
            BattleEditorUtility.Set(so, "_castRoot", castRoot.gameObject);
            BattleEditorUtility.Set(so, "_vignette", vignette);
            BattleEditorUtility.Set(so, "_killText", kills);
            BattleEditorUtility.Set(so, "_readiedText", readied);
            BattleEditorUtility.Set(so, "_bossFill", bossFill);
            BattleEditorUtility.Set(so, "_bossRoot", bossRoot.gameObject);
            BattleEditorUtility.Set(so, "_bossText", bossText);
            BattleEditorUtility.Set(so, "_weaponSlotLabels", slotLabels);
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
                element.FindPropertyRelative("Frame").objectReferenceValue = belt[i].Item4;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        private static void BuildCompass(RectTransform hud, DungeonContext context)
        {
            RectTransform strip = CreateRect("Compass", hud, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(560f, 30f));
            strip.pivot = new Vector2(0.5f, 1f);
            strip.gameObject.AddComponent<RectMask2D>();
            CreateImage("Center", strip, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(2f, 8f), new Color(0.95f, 0.85f, 0.6f)).rectTransform.pivot = new Vector2(0.5f, 1f);
            TMP_Text label = CreateText("Label", strip, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 24f), 16f, TextAlignmentOptions.Center);
            label.gameObject.SetActive(false);

            CompassView compass = strip.gameObject.AddComponent<CompassView>();
            SerializedObject so = new SerializedObject(compass);
            BattleEditorUtility.Set(so, "_context", context);
            BattleEditorUtility.Set(so, "_strip", strip);
            BattleEditorUtility.Set(so, "_labelPrefab", label);
            so.ApplyModifiedPropertiesWithoutUndo();
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
            TMP_Text keyText = CreateText("Key", slot, new Vector2(0.5f, 1f), new Vector2(0f, 14f), new Vector2(30f, 16f), 12f, TextAlignmentOptions.Center);
            keyText.rectTransform.pivot = new Vector2(0.5f, 0f);
            keyText.text = key;
            keyText.color = new Color(1f, 0.9f, 0.6f);
            CreateImage("KeyBack", slot, new Vector2(0.5f, 1f), new Vector2(0f, 14f), new Vector2(24f, 16f), new Color(0.1f, 0.08f, 0.06f, 0.9f)).rectTransform.pivot = new Vector2(0.5f, 0f);
            keyText.transform.SetAsLastSibling();
            TMP_Text charges = CreateText("Charges", slot, new Vector2(1f, 0f), new Vector2(-4f, 2f), new Vector2(40f, 16f), 13f, TextAlignmentOptions.BottomRight);
            charges.rectTransform.pivot = new Vector2(1f, 0f);

            return (slot.gameObject, glyph, keyText, charges, cooldown);
        }

        private static (GameObject, TMP_Text, TMP_Text, Image) BeltSlot(RectTransform parent, Vector2 anchor, Vector2 position, string key)
        {
            RectTransform slot = CreateRect("Belt" + key, parent, anchor, position, new Vector2(44f, 44f));
            CreateImage("Back", slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f), s_panelLight);
            Image frame = CreateImage("Frame", slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 48f), s_frame);
            frame.transform.SetAsFirstSibling();
            TMP_Text glyph = CreateText("Glyph", slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f), 18f, TextAlignmentOptions.Center);
            glyph.fontStyle = FontStyles.Bold;
            TMP_Text keyText = CreateText("Key", slot, new Vector2(0f, 1f), new Vector2(3f, -1f), new Vector2(20f, 14f), 10f, TextAlignmentOptions.TopLeft);
            keyText.rectTransform.pivot = new Vector2(0f, 1f);
            keyText.text = key;
            keyText.color = new Color(1f, 0.9f, 0.6f);
            TMP_Text count = CreateText("Count", slot, new Vector2(1f, 0f), new Vector2(-3f, 1f), new Vector2(30f, 14f), 11f, TextAlignmentOptions.BottomRight);
            count.rectTransform.pivot = new Vector2(1f, 0f);

            return (slot.gameObject, glyph, count, frame);
        }

        private static InventoryView BuildInventory(Transform root, Canvas canvas, Inputs inputs, ItemView itemPrefab, Image cellPrefab, string title, bool fullscreen,
            int previewIndex, out CharacterPreviewView preview)
        {
            RectTransform panel = CreateRect(title + "Panel", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));

            if (fullscreen)
            {
                Image back = CreateImage("Back", panel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.02f, 0.02f, 0.02f, 0.82f));
                Stretch(back.rectTransform, 0f);
            }

            // Left: character doll in an arch frame plus attributes.
            RectTransform dollPanel = CreateRect("Doll", panel, new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(460f, 980f));
            dollPanel.pivot = new Vector2(0f, 1f);
            CreateImage("Back", dollPanel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, s_panel).rectTransform.StretchFill();
            CreateImage("ArchFrame", dollPanel, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(360f, 480f), s_frame).rectTransform.pivot = new Vector2(0.5f, 1f);
            RawImage previewImage = CreateRect("Preview", dollPanel, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(352f, 472f)).gameObject.AddComponent<RawImage>();
            previewImage.rectTransform.pivot = new Vector2(0.5f, 1f);
            previewImage.color = Color.white;
            previewImage.raycastTarget = false;
            preview = BuildPreview(previewImage, inputs, previewIndex);
            TMP_Text titleText = CreateText("Title", dollPanel, new Vector2(0.5f, 1f), new Vector2(0f, -510f), new Vector2(400f, 30f), 24f, TextAlignmentOptions.Center);
            titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            titleText.color = s_gold;
            TMP_Text stats = CreateText("Stats", dollPanel, new Vector2(0f, 1f), new Vector2(24f, -550f), new Vector2(420f, 420f), 15f, TextAlignmentOptions.TopLeft);
            stats.rectTransform.pivot = new Vector2(0f, 1f);

            // Center: equipment sunburst and backpack.
            RectTransform equipPanel = CreateRect("Equipment", panel, new Vector2(0f, 1f), new Vector2(530f, -40f), new Vector2(640f, 500f));
            equipPanel.pivot = new Vector2(0f, 1f);
            CreateImage("Back", equipPanel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, s_panel).rectTransform.StretchFill();
            List<EquipSlotView> slots = new()
            {
                Slot(equipPanel, EquipSlot.Weapon1Main, "I", new Vector2(14f, -14f), 2, 4, itemPrefab),
                Slot(equipPanel, EquipSlot.Weapon1Off, "I off", new Vector2(14f, -204f), 2, 3, itemPrefab),
                Slot(equipPanel, EquipSlot.Weapon2Main, "II", new Vector2(538f, -14f), 2, 4, itemPrefab),
                Slot(equipPanel, EquipSlot.Weapon2Off, "II off", new Vector2(538f, -204f), 2, 3, itemPrefab),
                Slot(equipPanel, EquipSlot.Head, "Head", new Vector2(276f, -14f), 2, 2, itemPrefab),
                Slot(equipPanel, EquipSlot.Necklace, "Neck", new Vector2(372f, -36f), 1, 1, itemPrefab),
                Slot(equipPanel, EquipSlot.Back, "Cloak", new Vector2(170f, -110f), 2, 3, itemPrefab),
                Slot(equipPanel, EquipSlot.Chest, "Chest", new Vector2(276f, -110f), 2, 3, itemPrefab),
                Slot(equipPanel, EquipSlot.Hands, "Hands", new Vector2(382f, -110f), 2, 2, itemPrefab),
                Slot(equipPanel, EquipSlot.Ring1, "Ring", new Vector2(222f, -256f), 1, 1, itemPrefab),
                Slot(equipPanel, EquipSlot.Legs, "Legs", new Vector2(276f, -256f), 2, 2, itemPrefab),
                Slot(equipPanel, EquipSlot.Ring2, "Ring", new Vector2(382f, -256f), 1, 1, itemPrefab),
                Slot(equipPanel, EquipSlot.Feet, "Feet", new Vector2(276f, -352f), 2, 2, itemPrefab),
                Slot(equipPanel, EquipSlot.Utility1, "3", new Vector2(120f, -300f), 1, 1, itemPrefab),
                Slot(equipPanel, EquipSlot.Utility2, "3", new Vector2(120f, -352f), 1, 1, itemPrefab),
                Slot(equipPanel, EquipSlot.Utility3, "3", new Vector2(120f, -404f), 1, 1, itemPrefab),
                Slot(equipPanel, EquipSlot.Utility4, "4", new Vector2(476f, -300f), 1, 1, itemPrefab),
                Slot(equipPanel, EquipSlot.Utility5, "4", new Vector2(476f, -352f), 1, 1, itemPrefab),
                Slot(equipPanel, EquipSlot.Utility6, "4", new Vector2(476f, -404f), 1, 1, itemPrefab)
            };

            RectTransform bagPanel = CreateRect("BagPanel", panel, new Vector2(0f, 1f), new Vector2(530f, -560f), new Vector2(640f, 230f));
            bagPanel.pivot = new Vector2(0f, 1f);
            CreateImage("Back", bagPanel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, s_panel).rectTransform.StretchFill();
            TMP_Text bagTitle = CreateText("BagTitle", bagPanel, new Vector2(0f, 1f), new Vector2(16f, -8f), new Vector2(300f, 24f), 16f, TextAlignmentOptions.MidlineLeft);
            bagTitle.rectTransform.pivot = new Vector2(0f, 1f);
            bagTitle.text = "Backpack";
            bagTitle.color = s_gold;
            ItemGridView bag = Grid(bagPanel, "Bag", new Vector2(100f, -36f), itemPrefab, cellPrefab);
            TMP_Text valueText = CreateText("Value", bagPanel, new Vector2(1f, 1f), new Vector2(-16f, -8f), new Vector2(300f, 24f), 14f, TextAlignmentOptions.MidlineRight);
            valueText.rectTransform.pivot = new Vector2(1f, 1f);

            // Right: other container / stash and tooltip.
            RectTransform otherPanel = CreateRect("OtherPanel", panel, new Vector2(0f, 1f), new Vector2(1200f, -40f), new Vector2(680f, 440f));
            otherPanel.pivot = new Vector2(0f, 1f);
            CreateImage("Back", otherPanel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, s_panel).rectTransform.StretchFill();
            TMP_Text otherTitle = CreateText("Title", otherPanel, new Vector2(0f, 1f), new Vector2(16f, -10f), new Vector2(400f, 30f), 20f, TextAlignmentOptions.MidlineLeft);
            otherTitle.rectTransform.pivot = new Vector2(0f, 1f);
            otherTitle.color = s_gold;
            ItemGridView other = Grid(otherPanel, "Other", new Vector2(16f, -50f), itemPrefab, cellPrefab);
            Button takeAll = CreateButton("TakeAll", otherPanel, new Vector2(1f, 1f), new Vector2(-16f, -12f), new Vector2(140f, 32f), "Take all");
            ((RectTransform)takeAll.transform).pivot = new Vector2(1f, 1f);
            Button sort = CreateButton("Sort", bagPanel, new Vector2(1f, 1f), new Vector2(-16f, -34f), new Vector2(80f, 26f), "Sort");
            ((RectTransform)sort.transform).pivot = new Vector2(1f, 1f);

            TMP_Text hints = CreateText("Hints", panel, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(1400f, 24f), 13f, TextAlignmentOptions.Center);
            hints.rectTransform.pivot = new Vector2(0.5f, 1f);
            hints.text = "[Drag] move   [R Click] equip / use / unequip   [Shift + L Click] quick transfer   [Ctrl + Drag] split stack   [Drag outside] drop";
            hints.color = new Color(0.8f, 0.75f, 0.65f);

            RectTransform tooltip = CreateRect("Tooltip", panel, new Vector2(0f, 1f), Vector2.zero, new Vector2(380f, 240f));
            tooltip.pivot = new Vector2(0f, 1f);
            Image tooltipBack = CreateImage("Back", tooltip, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.03f, 0.03f, 0.03f, 0.96f));
            tooltipBack.rectTransform.StretchFill();
            CreateImage("Frame", tooltip, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(380f, 3f), s_frame).rectTransform.pivot = new Vector2(0.5f, 1f);
            TMP_Text tooltipText = CreateText("Text", tooltip, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 15f, TextAlignmentOptions.TopLeft);
            Stretch(tooltipText.rectTransform, 12f);

            RectTransform ghost = CreateRect("DragGhost", panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(Cell, Cell));
            Image ghostImage = ghost.gameObject.AddComponent<Image>();
            ghostImage.color = new Color(1f, 1f, 1f, 0.35f);
            ghostImage.raycastTarget = false;
            Image ghostIcon = CreateImage("Icon", ghost, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, Color.white);
            Stretch(ghostIcon.rectTransform, 3f);
            ghostIcon.preserveAspect = true;
            TMP_Text ghostText = CreateText("Glyph", ghost, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 20f, TextAlignmentOptions.Center);
            Stretch(ghostText.rectTransform, 0f);
            ghostText.fontStyle = FontStyles.Bold;

            InventoryView view = panel.gameObject.AddComponent<InventoryView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_database", inputs.Database);
            BattleEditorUtility.Set(so, "_bagGrid", bag);
            BattleEditorUtility.Set(so, "_otherGrid", other);
            BattleEditorUtility.Set(so, "_otherTitle", otherTitle);
            BattleEditorUtility.Set(so, "_otherPanel", otherPanel.gameObject);
            BattleEditorUtility.Set(so, "_takeAllButton", takeAll);
            BattleEditorUtility.Set(so, "_sortButton", sort);
            BattleEditorUtility.Set(so, "_slots", slots);
            BattleEditorUtility.Set(so, "_statsText", stats);
            BattleEditorUtility.Set(so, "_titleText", titleText);
            BattleEditorUtility.Set(so, "_valueText", valueText);
            BattleEditorUtility.Set(so, "_tooltip", tooltip);
            BattleEditorUtility.Set(so, "_tooltipText", tooltipText);
            BattleEditorUtility.Set(so, "_dragGhost", ghost);
            BattleEditorUtility.Set(so, "_dragGhostText", ghostText);
            BattleEditorUtility.Set(so, "_dragGhostIcon", ghostIcon);
            BattleEditorUtility.Set(so, "_canvas", canvas);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        private static CharacterPreviewView BuildPreview(RawImage target, Inputs inputs, int index)
        {
            GameObject stageRoot = new GameObject("[Preview" + index + "]");
            stageRoot.transform.position = new Vector3(500f + index * 20f, 300f, 500f);
            int layer = LayerMask.NameToLayer(PreviewLayer);
            stageRoot.layer = layer;

            GameObject stage = BattleEditorUtility.CreateChild("Stage", stageRoot.transform);
            stage.layer = layer;

            Camera camera = BattleEditorUtility.CreateChild("Camera", stageRoot.transform, new Vector3(0f, 1.05f, 2.9f)).AddComponent<Camera>();
            camera.transform.LookAt(stageRoot.transform.position + new Vector3(0f, 0.95f, 0f));
            camera.cullingMask = 1 << layer;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.04f, 0.035f, 1f);
            camera.fieldOfView = 42f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 20f;
            camera.enabled = false;
            RenderTexture texture = new RenderTexture(352, 472, 24) { name = "Preview" + index };
            string path = $"{DungeonContentBuilder.ConfigsFolder}/PreviewTexture{index}.renderTexture";
            AssetDatabase.CreateAsset(texture, path);
            camera.targetTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
            target.texture = camera.targetTexture;
            camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = false;

            Light key = BattleEditorUtility.CreateChild("Key", stageRoot.transform, new Vector3(1.5f, 2.5f, 2f)).AddComponent<Light>();
            key.type = LightType.Point;
            key.range = 8f;
            key.intensity = 2.2f;
            key.color = new Color(1f, 0.85f, 0.65f);
            key.cullingMask = 1 << layer;
            Light rim = BattleEditorUtility.CreateChild("Rim", stageRoot.transform, new Vector3(-1.5f, 2f, -1.5f)).AddComponent<Light>();
            rim.type = LightType.Point;
            rim.range = 7f;
            rim.intensity = 1.2f;
            rim.color = new Color(0.5f, 0.6f, 0.9f);
            rim.cullingMask = 1 << layer;

            CharacterPreviewView view = target.gameObject.AddComponent<CharacterPreviewView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_rigPrefab", inputs.PreviewRig);
            BattleEditorUtility.Set(so, "_pieceSet", inputs.PieceSet);
            BattleEditorUtility.Set(so, "_image", target);
            BattleEditorUtility.Set(so, "_camera", camera);
            BattleEditorUtility.Set(so, "_stage", stage.transform);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        private static EquipSlotView Slot(RectTransform parent, EquipSlot slot, string label, Vector2 position, int width, int height, ItemView itemPrefab)
        {
            RectTransform rect = CreateRect("Slot" + slot, parent, new Vector2(0f, 1f), position, new Vector2(width * Cell, height * Cell));
            rect.pivot = new Vector2(0f, 1f);
            Image background = rect.gameObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.6f);
            CreateImage("Frame", rect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width * Cell + 4f, height * Cell + 4f), new Color(0.3f, 0.25f, 0.18f, 0.9f)).transform.SetAsFirstSibling();
            TMP_Text labelText = CreateText("Label", rect, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 12f, TextAlignmentOptions.Center);
            Stretch(labelText.rectTransform, 2f);
            labelText.text = label;
            labelText.color = new Color(1f, 1f, 1f, 0.35f);

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

        private static LobbyView BuildLobby(Transform root, Canvas canvas, Inputs inputs, ItemView itemPrefab, Image cellPrefab)
        {
            RectTransform panel = CreateRect("Lobby", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(panel, 0f);
            Image back = CreateImage("Back", panel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.05f, 0.04f, 0.035f, 1f));
            Stretch(back.rectTransform, 0f);

            TMP_Text title = CreateText("Title", panel, new Vector2(0f, 1f), new Vector2(60f, -14f), new Vector2(800f, 30f), 24f, TextAlignmentOptions.MidlineLeft);
            title.rectTransform.pivot = new Vector2(0f, 1f);
            title.text = "THE TAVERN";
            title.color = s_gold;
            TMP_Text profile = CreateText("Profile", panel, new Vector2(1f, 1f), new Vector2(-60f, -16f), new Vector2(900f, 26f), 17f, TextAlignmentOptions.MidlineRight);
            profile.rectTransform.pivot = new Vector2(1f, 1f);

            RectTransform classes = CreateRect("Classes", panel, new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(1240f, 30f));
            classes.pivot = new Vector2(0.5f, 1f);
            HorizontalLayoutGroup layout = classes.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            Button classButton = CreateButton("ClassButton", classes, new Vector2(0f, 1f), Vector2.zero, new Vector2(118f, 28f), "Class");
            classButton.gameObject.SetActive(false);

            CharacterPreviewView preview;
            InventoryView inventory = BuildInventory(panel, canvas, inputs, itemPrefab, cellPrefab, "Kit", false, 1, out preview);
            RectTransform inventoryRect = (RectTransform)inventory.transform;
            inventoryRect.anchorMin = inventoryRect.anchorMax = new Vector2(0f, 1f);
            inventoryRect.pivot = new Vector2(0f, 1f);
            inventoryRect.anchoredPosition = new Vector2(0f, -10f);
            inventoryRect.sizeDelta = new Vector2(1920f, 1080f);
            inventory.transform.Find("Hints").gameObject.SetActive(false);

            RectTransform otherPanel = inventory.transform.Find("OtherPanel").GetComponent<RectTransform>();
            otherPanel.anchoredPosition = new Vector2(1200f, -40f);
            otherPanel.sizeDelta = new Vector2(680f, 300f);

            RectTransform buildPanel = CreateRect("Build", panel, new Vector2(0f, 1f), new Vector2(1200f, -360f), new Vector2(680f, 620f));
            buildPanel.pivot = new Vector2(0f, 1f);
            CreateImage("Back", buildPanel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, s_panel).rectTransform.StretchFill();
            TMP_Text info = CreateText("Info", buildPanel, new Vector2(0f, 1f), new Vector2(14f, -10f), new Vector2(650f, 110f), 13f, TextAlignmentOptions.TopLeft);
            info.rectTransform.pivot = new Vector2(0f, 1f);
            TMP_Text skillsTitle = CreateText("SkillsTitle", buildPanel, new Vector2(0f, 1f), new Vector2(14f, -126f), new Vector2(400f, 22f), 15f, TextAlignmentOptions.MidlineLeft);
            skillsTitle.rectTransform.pivot = new Vector2(0f, 1f);
            skillsTitle.text = "Skills — assign to Q / E";
            skillsTitle.color = s_gold;
            RectTransform skillRows = CreateRect("SkillRows", buildPanel, new Vector2(0f, 1f), new Vector2(14f, -150f), new Vector2(650f, 220f));
            skillRows.pivot = new Vector2(0f, 1f);
            VerticalLayoutGroup skillLayout = skillRows.gameObject.AddComponent<VerticalLayoutGroup>();
            skillLayout.spacing = 3f;
            skillLayout.childForceExpandHeight = false;
            skillLayout.childControlHeight = true;
            skillLayout.childControlWidth = true;
            RectTransform skillRow = CreateRect("SkillRow", skillRows, new Vector2(0f, 1f), Vector2.zero, new Vector2(650f, 32f));
            skillRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;
            CreateImage("Back", skillRow, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.1f, 0.09f, 0.07f, 0.8f)).rectTransform.StretchFill();
            TMP_Text skillText = CreateText("Text", skillRow, new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(560f, 32f), 12f, TextAlignmentOptions.MidlineLeft);
            skillText.rectTransform.pivot = new Vector2(0f, 0.5f);
            Button q = CreateButton("Q", skillRow, new Vector2(1f, 0.5f), new Vector2(-44f, 0f), new Vector2(34f, 26f), "Q");
            ((RectTransform)q.transform).pivot = new Vector2(1f, 0.5f);
            Button e = CreateButton("E", skillRow, new Vector2(1f, 0.5f), new Vector2(-6f, 0f), new Vector2(34f, 26f), "E");
            ((RectTransform)e.transform).pivot = new Vector2(1f, 0.5f);
            skillRow.gameObject.SetActive(false);

            TMP_Text perksTitle = CreateText("PerksTitle", buildPanel, new Vector2(0f, 1f), new Vector2(14f, -376f), new Vector2(400f, 22f), 15f, TextAlignmentOptions.MidlineLeft);
            perksTitle.rectTransform.pivot = new Vector2(0f, 1f);
            perksTitle.text = "Perks — click to toggle";
            perksTitle.color = s_gold;
            RectTransform perkRows = CreateRect("PerkRows", buildPanel, new Vector2(0f, 1f), new Vector2(14f, -400f), new Vector2(650f, 210f));
            perkRows.pivot = new Vector2(0f, 1f);
            GridLayoutGroup perkLayout = perkRows.gameObject.AddComponent<GridLayoutGroup>();
            perkLayout.cellSize = new Vector2(322f, 48f);
            perkLayout.spacing = new Vector2(6f, 4f);
            Button perkRow = CreateButton("PerkRow", perkRows, new Vector2(0f, 1f), Vector2.zero, new Vector2(322f, 48f), "Perk");
            perkRow.GetComponentInChildren<TMP_Text>().fontSize = 12f;
            perkRow.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.MidlineLeft;
            perkRow.gameObject.SetActive(false);

            Button enter = CreateButton("Enter", panel, new Vector2(1f, 0f), new Vector2(-60f, 30f), new Vector2(300f, 54f), "ENTER THE DUNGEON");
            ((RectTransform)enter.transform).pivot = new Vector2(1f, 0f);
            enter.image.color = new Color(0.45f, 0.12f, 0.1f);
            enter.GetComponentInChildren<TMP_Text>().fontSize = 22f;
            Button reset = CreateButton("ResetKit", panel, new Vector2(1f, 0f), new Vector2(-380f, 30f), new Vector2(200f, 44f), "Squire kit");
            ((RectTransform)reset.transform).pivot = new Vector2(1f, 0f);
            Button wipe = CreateButton("Wipe", panel, new Vector2(1f, 0f), new Vector2(-600f, 30f), new Vector2(160f, 44f), "Wipe save");
            ((RectTransform)wipe.transform).pivot = new Vector2(1f, 0f);

            TMP_Text result = CreateText("Result", panel, new Vector2(0f, 0f), new Vector2(60f, 40f), new Vector2(1000f, 30f), 18f, TextAlignmentOptions.MidlineLeft);
            result.rectTransform.pivot = new Vector2(0f, 0f);

            LobbyView view = panel.gameObject.AddComponent<LobbyView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_context", inputs.Context);
            BattleEditorUtility.Set(so, "_inventory", inventory);
            BattleEditorUtility.Set(so, "_preview", preview);
            BattleEditorUtility.Set(so, "_classButtonsRoot", classes);
            BattleEditorUtility.Set(so, "_classButtonPrefab", classButton);
            BattleEditorUtility.Set(so, "_classInfoText", info);
            BattleEditorUtility.Set(so, "_skillRowsRoot", skillRows);
            BattleEditorUtility.Set(so, "_skillRowPrefab", skillRow);
            BattleEditorUtility.Set(so, "_perkRowsRoot", perkRows);
            BattleEditorUtility.Set(so, "_perkRowPrefab", perkRow);
            BattleEditorUtility.Set(so, "_profileText", profile);
            BattleEditorUtility.Set(so, "_resultText", result);
            BattleEditorUtility.Set(so, "_enterButton", enter);
            BattleEditorUtility.Set(so, "_resetKitButton", reset);
            BattleEditorUtility.Set(so, "_wipeButton", wipe);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        private static SpellWheelView BuildWheel(Transform root)
        {
            RectTransform panel = CreateRect("SpellWheel", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 500f));
            Image dim = CreateImage("Dim", panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 420f), new Color(0f, 0f, 0f, 0.45f));
            dim.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            TMP_Text title = CreateText("Title", panel, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(400f, 30f), 20f, TextAlignmentOptions.Center);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.color = s_gold;
            TMP_Text centerText = CreateText("Center", panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 40f), 16f, TextAlignmentOptions.Center);
            RectTransform slotsRoot = CreateRect("Slots", panel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

            RectTransform slot = CreateRect("Slot", panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 84f));
            Image slotBack = slot.gameObject.AddComponent<Image>();
            slotBack.color = new Color(0.08f, 0.07f, 0.06f, 0.9f);
            slotBack.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            slotBack.type = Image.Type.Sliced;
            TMP_Text glyph = CreateText("Glyph", slot, new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(100f, 34f), 24f, TextAlignmentOptions.Center);
            glyph.rectTransform.pivot = new Vector2(0.5f, 1f);
            glyph.fontStyle = FontStyles.Bold;
            TMP_Text name = CreateText("Name", slot, new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(100f, 44f), 12f, TextAlignmentOptions.Center);
            name.rectTransform.pivot = new Vector2(0.5f, 0f);
            slot.gameObject.SetActive(false);

            SpellWheelView view = panel.gameObject.AddComponent<SpellWheelView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_slotPrefab", slot);
            BattleEditorUtility.Set(so, "_slotsRoot", slotsRoot);
            BattleEditorUtility.Set(so, "_titleText", title);
            BattleEditorUtility.Set(so, "_centerText", centerText);
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

        /// Test ground developer buttons, placed in the free corner under the container panel of the inventory.
        public static SandboxDevView BuildDevPanel(GameObject canvas, SandboxDirector director, string[] monsterNames)
        {
            // Same 1920x1080 frame as the centered inventory panel: x 1200, y -520 from its top-left corner.
            RectTransform panel = CreateRect("DevPanel", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(240f, 20f), new Vector2(680f, 170f));
            panel.pivot = new Vector2(0f, 1f);
            CreateImage("Back", panel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, s_panel).rectTransform.StretchFill();
            TMP_Text title = CreateText("Title", panel, new Vector2(0f, 1f), new Vector2(16f, -10f), new Vector2(400f, 28f), 18f, TextAlignmentOptions.MidlineLeft);
            title.rectTransform.pivot = new Vector2(0f, 1f);
            title.text = "Developer (host)";
            title.color = s_gold;

            Button[] monsters = new Button[monsterNames.Length];

            for (int i = 0; i < monsterNames.Length; i++)
                monsters[i] = DevButton(panel, i, 0, "+ " + monsterNames[i]);

            SandboxDevView view = panel.gameObject.AddComponent<SandboxDevView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_director", director);
            BattleEditorUtility.Set(so, "_monsterButtons", monsters);
            BattleEditorUtility.Set(so, "_botButton", DevButton(panel, 0, 1, "+ Duel bot"));
            BattleEditorUtility.Set(so, "_clearButton", DevButton(panel, 1, 1, "Remove mobs"));
            BattleEditorUtility.Set(so, "_restockButton", DevButton(panel, 2, 1, "Restock table"));
            so.ApplyModifiedPropertiesWithoutUndo();

            BattleEditorUtility.SetLayerRecursively(panel.gameObject, LayerMask.NameToLayer("UI"));
            panel.gameObject.SetActive(false);
            BattleEditorUtility.Set(canvas.GetComponent<DungeonUiRoot>(), "_devPanel", view);

            return view;
        }

        private static Button DevButton(RectTransform panel, int column, int row, string label)
        {
            Button button = CreateButton(label, panel, new Vector2(0f, 1f), new Vector2(16f + column * 164f, -50f - row * 54f), new Vector2(154f, 42f), label);
            ((RectTransform)button.transform).pivot = new Vector2(0f, 1f);

            return button;
        }

        private static HelpView BuildHelp(Transform root)
        {
            RectTransform panel = CreateRect("Help", root, new Vector2(0f, 1f), new Vector2(100f, -30f), new Vector2(1100f, 150f));
            panel.pivot = new Vector2(0f, 1f);
            CreateImage("Back", panel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.75f)).rectTransform.StretchFill();
            TMP_Text text = CreateText("Text", panel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 16f, TextAlignmentOptions.TopLeft);
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
