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
    /// Builds the uGUI layers laid out like Dark and Darker. Gameplay scenes: compass, HP bar with Q/E, belt, weapon row, minimap,
    /// inventory with 3D doll, result and loading screens. Tavern scene: the lobby, skills, stash and merchants pages.
    internal static class DungeonUiBuilder
    {
        public const string PreviewLayer = "Preview";
        private const float Cell = 44f;
        private const string HelpText =
            "<b>Controls</b>\n" +
            "WASD move · Shift walk (quiet) · Space jump · Ctrl/C crouch (duck under swings)\n" +
            "LMB attack / draw · RMB block, or hold to cast a readied spell · 1 / 2 weapon sets · X put the weapon away · Tab inventory\n" +
            "3 / 4 belt item in hand (press again for the next of three), LMB use, RMB put away · F interact (hold; chests and corpses open at once) · Q / E skills · G rest · M map · H help\n" +
            "Casters need a staff, spellbook or crystal ball in hand; bards need an instrument. Rest at a campfire to recover charges.";

        private static readonly Color s_panel = new(0.04f, 0.035f, 0.03f, 0.92f);
        private static readonly Color s_panelLight = new(0.12f, 0.1f, 0.08f, 0.95f);
        private static readonly Color s_frame = new(0.45f, 0.36f, 0.22f, 1f);
        private static readonly Color s_gold = new(0.85f, 0.72f, 0.45f);
        private static readonly Color s_text = new(0.95f, 0.93f, 0.88f);
        private static readonly Color s_textDim = new(0.62f, 0.58f, 0.5f);
        private static readonly Color s_lobbyBack = Color.black;

        public sealed class Inputs
        {
            public DungeonContext Context;
            public ItemDatabase Database;
            public Camera Camera;
            public GameObject PreviewRig;
            public ArmorPieceSetConfig PieceSet;
            public Texture2D[] FloorMaps;
            public string[] ModuleNames;
            /// Name of the scene shown on its loading screen.
            public string Title;
        }

        /// UI of a gameplay scene (the dungeon or the test ground).
        public static GameObject Build(Inputs inputs)
        {
            GameObject canvasObject = BuildCanvas(inputs, out Canvas canvas);
            Transform root = canvasObject.transform;
            ItemView itemPrefab = BuildItemPrefab();
            Image cellPrefab = BuildCellPrefab();

            DungeonHudView hud = BuildHud(root, inputs);
            CharacterPreviewView dungeonPreview;
            InventoryView dungeonInventory = BuildInventory(root, canvas, inputs, itemPrefab, cellPrefab, "Inventory", true, 0, out dungeonPreview);
            ResultView result = BuildResult(root);
            HelpView help = BuildHelp(root);
            SpellWheelView wheel = BuildWheel(root);
            MapView map = BuildMap(root, inputs);
            LoadingView loading = BuildLoading(root, inputs.Title);

            DungeonUiRoot uiRoot = canvasObject.AddComponent<DungeonUiRoot>();
            SerializedObject so = new SerializedObject(uiRoot);
            BattleEditorUtility.Set(so, "_context", inputs.Context);
            BattleEditorUtility.Set(so, "_loading", loading);
            BattleEditorUtility.Set(so, "_hud", hud);
            BattleEditorUtility.Set(so, "_inventory", dungeonInventory);
            BattleEditorUtility.Set(so, "_result", result);
            BattleEditorUtility.Set(so, "_help", help);
            BattleEditorUtility.Set(so, "_wheel", wheel);
            BattleEditorUtility.Set(so, "_map", map);
            BattleEditorUtility.Set(so, "_inventoryPreview", dungeonPreview);
            so.ApplyModifiedPropertiesWithoutUndo();

            hud.gameObject.SetActive(false);
            dungeonInventory.gameObject.SetActive(false);
            result.gameObject.SetActive(false);
            help.gameObject.SetActive(false);
            wheel.gameObject.SetActive(false);
            map.gameObject.SetActive(false);
            BattleEditorUtility.SetLayerRecursively(canvasObject, LayerMask.NameToLayer("UI"));

            return canvasObject;
        }

        /// UI of the tavern scene: the lobby pages under a loading screen that lifts when the session is ready.
        public static GameObject BuildLobbyScene(Inputs inputs)
        {
            GameObject canvasObject = BuildCanvas(inputs, out Canvas canvas);
            Transform root = canvasObject.transform;
            LobbyView lobby = BuildLobby(root, canvas, inputs, BuildItemPrefab(), BuildCellPrefab());
            HelpView help = BuildHelp(root);
            LoadingView loading = BuildLoading(root, inputs.Title);

            SerializedObject so = new SerializedObject(canvasObject.AddComponent<LobbyUiRoot>());
            BattleEditorUtility.Set(so, "_context", inputs.Context);
            BattleEditorUtility.Set(so, "_lobby", lobby);
            BattleEditorUtility.Set(so, "_help", help);
            BattleEditorUtility.Set(so, "_loading", loading);
            so.ApplyModifiedPropertiesWithoutUndo();

            help.gameObject.SetActive(false);
            BattleEditorUtility.SetLayerRecursively(canvasObject, LayerMask.NameToLayer("UI"));

            return canvasObject;
        }

        /// UI of the character select scene: the tavern backdrop with the slot list and the creation page on top.
        public static GameObject BuildCharacterSelectScene(Inputs inputs, ClassConfig[] classes)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            GameObject canvasObject = BuildCanvas(inputs, out Canvas _);
            Transform root = canvasObject.transform;

            RawImage backdrop = CreateRect("Backdrop", root, center, Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
            Stretch(backdrop.rectTransform, 0f);
            AspectRatioFitter fitter = backdrop.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            CharacterPreviewView preview = BuildPreview(backdrop, inputs, 6, 1920, 1080, out Camera camera);
            BuildTavern(camera, preview);
            Image vignette = CreateImage("Vignette", root, center, Vector2.zero, Vector2.zero, Color.white);
            vignette.sprite = DungeonUiSpriteBuilder.Load("Vignette");
            Stretch(vignette.rectTransform, 0f);

            RectTransform selectPage = CharacterPage(root, "Select", "Select Character", "Enter Tavern", "Delete",
                out TMP_Text selectName, out Button enter, out Button delete);
            Button[] slots = new Button[StashService.SlotCount];
            TMP_Text[] slotTexts = new TMP_Text[slots.Length];

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = CharacterCard(selectPage, "Slot" + i, i, string.Empty);
                slotTexts[i] = slots[i].GetComponentInChildren<TMP_Text>();
            }

            CharacterSelectView select = selectPage.gameObject.AddComponent<CharacterSelectView>();
            SerializedObject so = new SerializedObject(select);
            BattleEditorUtility.Set(so, "_classes", classes);
            BattleEditorUtility.Set(so, "_preview", preview);
            BattleEditorUtility.Set(so, "_slots", slots);
            BattleEditorUtility.Set(so, "_slotTexts", slotTexts);
            BattleEditorUtility.Set(so, "_nameText", selectName);
            BattleEditorUtility.Set(so, "_enterButton", enter);
            BattleEditorUtility.Set(so, "_deleteButton", delete);
            so.ApplyModifiedPropertiesWithoutUndo();

            RectTransform createPage = CharacterPage(root, "Create", "Create Character", "Create", "Back",
                out TMP_Text className, out Button create, out Button back);
            Button[] classButtons = new Button[classes.Length];

            for (int i = 0; i < classes.Length; i++)
                classButtons[i] = CharacterCard(createPage, "Class" + classes[i].DisplayName, i, classes[i].DisplayName);

            RectTransform info = CreateRect("Info", createPage, Vector2.one, new Vector2(-56f, -140f), new Vector2(520f, 240f));
            CreateImage("Frame", info, center, Vector2.zero, Vector2.zero, s_frame).rectTransform.StretchFill();
            Stretch(CreateImage("Back", info, center, Vector2.zero, Vector2.zero, s_panel).rectTransform, 2f);
            TMP_Text description = CreateText("Text", info, center, Vector2.zero, Vector2.zero, 19f, TextAlignmentOptions.TopLeft);
            Stretch(description.rectTransform, 22f);
            description.color = s_text;

            CharacterCreateView createView = createPage.gameObject.AddComponent<CharacterCreateView>();
            so = new SerializedObject(createView);
            BattleEditorUtility.Set(so, "_classes", classes);
            BattleEditorUtility.Set(so, "_preview", preview);
            BattleEditorUtility.Set(so, "_classButtons", classButtons);
            BattleEditorUtility.Set(so, "_nameText", className);
            BattleEditorUtility.Set(so, "_descriptionText", description);
            BattleEditorUtility.Set(so, "_createButton", create);
            BattleEditorUtility.Set(so, "_backButton", back);
            so.ApplyModifiedPropertiesWithoutUndo();

            LoadingView loading = BuildLoading(root, SceneTravel.CharacterSelectTitle);
            so = new SerializedObject(canvasObject.AddComponent<CharacterSelectUiRoot>());
            BattleEditorUtility.Set(so, "_select", select);
            BattleEditorUtility.Set(so, "_create", createView);
            BattleEditorUtility.Set(so, "_loading", loading);
            so.ApplyModifiedPropertiesWithoutUndo();

            // Only one page may bind the shared preview on the first frame.
            createPage.gameObject.SetActive(false);
            BattleEditorUtility.SetLayerRecursively(canvasObject, LayerMask.NameToLayer("UI"));

            return canvasObject;
        }

        /// Full-screen page of the character screens: title top left, name under the top edge, the main button bottom middle
        /// and the secondary one bottom left.
        private static RectTransform CharacterPage(Transform root, string name, string title, string mainLabel, string secondLabel,
            out TMP_Text nameText, out Button main, out Button second)
        {
            RectTransform page = CreateRect(name, root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(page, 0f);
            TMP_Text titleText = CreateText("Title", page, new Vector2(0f, 1f), new Vector2(56f, -48f), new Vector2(600f, 60f), 34f, TextAlignmentOptions.MidlineLeft);
            titleText.text = title;
            titleText.color = s_gold;
            titleText.fontStyle = FontStyles.Bold;
            CreateImage("TitleRule", page, new Vector2(0f, 1f), new Vector2(56f, -112f), new Vector2(380f, 2f), s_frame);

            nameText = CreateText("Name", page, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(900f, 60f), 36f, TextAlignmentOptions.Center);
            nameText.color = s_text;
            CreateImage("NameRule", page, new Vector2(0.5f, 1f), new Vector2(0f, -214f), new Vector2(260f, 2f), s_frame);

            main = CreateButton(mainLabel, page, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(360f, 86f), mainLabel);
            main.image.color = new Color(0.17f, 0.16f, 0.15f, 0.97f);
            main.GetComponentInChildren<TMP_Text>().fontSize = 32f;
            second = CreateButton(secondLabel, page, Vector2.zero, new Vector2(56f, 40f), new Vector2(220f, 64f), secondLabel);
            second.GetComponentInChildren<TMP_Text>().fontSize = 22f;

            return page;
        }

        /// Row of the left column: a character slot or a class.
        private static Button CharacterCard(RectTransform page, string name, int row, string label)
        {
            Button card = CreateButton(name, page, new Vector2(0f, 1f), new Vector2(56f, -140f - row * 108f), new Vector2(380f, 96f), label);
            TMP_Text text = card.GetComponentInChildren<TMP_Text>();
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.margin = new Vector4(22f, 0f, 12f, 0f);
            text.color = s_text;

            return card;
        }

        private static GameObject BuildCanvas(Inputs inputs, out Canvas canvas)
        {
            BattleEditorUtility.EnsureLayer(PreviewLayer);
            GameObject canvasObject = new GameObject("[UI]");
            canvas = canvasObject.AddComponent<Canvas>();
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

            return canvasObject;
        }

        /// Black cover with the destination name, a status line, a progress bar and a spinner; active from the first frame.
        private static LoadingView BuildLoading(Transform root, string title)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            RectTransform panel = CreateRect("Loading", root, center, Vector2.zero, Vector2.zero);
            Stretch(panel, 0f);
            Image back = CreateImage("Back", panel, center, Vector2.zero, Vector2.zero, Color.black);
            Stretch(back.rectTransform, 0f);
            back.raycastTarget = true;
            Image vignette = CreateImage("Vignette", panel, center, Vector2.zero, Vector2.zero, new Color(0.45f, 0.3f, 0.12f, 0.35f));
            Stretch(vignette.rectTransform, 0f);
            vignette.sprite = DungeonUiSpriteBuilder.Load("Glow");

            TMP_Text titleText = CreateText("Title", panel, center, new Vector2(0f, 60f), new Vector2(1400f, 80f), 58f, TextAlignmentOptions.Center);
            titleText.color = s_gold;
            titleText.fontStyle = FontStyles.Bold;
            CreateImage("Rule", panel, center, new Vector2(0f, 8f), new Vector2(420f, 2f), s_frame);
            TMP_Text statusText = CreateText("Status", panel, center, new Vector2(0f, -30f), new Vector2(1000f, 34f), 22f, TextAlignmentOptions.Center);
            statusText.color = s_text;
            CreateImage("BarBack", panel, center, new Vector2(0f, -74f), new Vector2(424f, 12f), s_frame);
            CreateImage("BarInner", panel, center, new Vector2(0f, -74f), new Vector2(420f, 8f), new Color(0.05f, 0.04f, 0.03f));
            Image fill = CreateImage("BarFill", panel, center, new Vector2(0f, -74f), new Vector2(420f, 8f), s_gold);
            MakeFilled(fill);
            Image spinner = CreateImage("Spinner", panel, new Vector2(1f, 0f), new Vector2(-70f, 70f), new Vector2(54f, 54f), s_gold);
            spinner.rectTransform.pivot = center;
            spinner.sprite = DungeonUiSpriteBuilder.Load("Diamond");
            TMP_Text tip = CreateText("Tip", panel, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1200f, 30f), 16f, TextAlignmentOptions.Center);
            tip.text = "Containers open at once, but their loot has to be searched. Perception makes the search faster.";
            tip.color = s_textDim;

            LoadingView view = panel.gameObject.AddComponent<LoadingView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_sceneTitle", title);
            BattleEditorUtility.Set(so, "_titleText", titleText);
            BattleEditorUtility.Set(so, "_statusText", statusText);
            BattleEditorUtility.Set(so, "_progressFill", fill);
            BattleEditorUtility.Set(so, "_spinner", spinner.rectTransform);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
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

            // Unsearched loot: an eye in a ring that fills while the item is being discovered.
            RectTransform hidden = CreateRect("Hidden", rect, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(hidden, 0f);
            Image ringBack = CreateImage("RingBack", hidden, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38f, 38f), new Color(0f, 0f, 0f, 0.55f));
            ringBack.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Image searchFill = CreateImage("Ring", hidden, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(38f, 38f), new Color(0.85f, 0.72f, 0.45f, 0.9f));
            MakeRadial(searchFill);
            Image ringCore = CreateImage("RingCore", hidden, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(31f, 31f), new Color(0.09f, 0.08f, 0.07f));
            ringCore.sprite = ringBack.sprite;
            Image eye = CreateImage("Eye", hidden, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f), new Color(0.8f, 0.76f, 0.66f));
            eye.sprite = DungeonUiSpriteBuilder.Load("Eye");
            hidden.gameObject.SetActive(false);

            SerializedObject so = new SerializedObject(root.AddComponent<ItemView>());
            BattleEditorUtility.Set(so, "_background", background);
            BattleEditorUtility.Set(so, "_frame", frame);
            BattleEditorUtility.Set(so, "_icon", icon);
            BattleEditorUtility.Set(so, "_glyph", glyph);
            BattleEditorUtility.Set(so, "_count", count);
            BattleEditorUtility.Set(so, "_group", group);
            BattleEditorUtility.Set(so, "_hiddenRoot", hidden.gameObject);
            BattleEditorUtility.Set(so, "_searchFill", searchFill);
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

            List<(GameObject, Image, TMP_Text, TMP_Text, TMP_Text, Image)> skills = new();
            skills.Add(AbilitySlot(hud, bottom, new Vector2(-272f, 44f), "Q"));
            skills.Add(AbilitySlot(hud, bottom, new Vector2(272f, 44f), "E"));
            List<(GameObject, Image, TMP_Text, TMP_Text, TMP_Text, Image)> spells = new();
            TMP_Text readied = CreateText("Readied", hud, bottom, new Vector2(372f, 44f), new Vector2(360f, 44f), 15f, TextAlignmentOptions.MidlineLeft);
            readied.rectTransform.pivot = new Vector2(0f, 0.5f);

            // Two belt groups of three: key 3 cycles the left group, key 4 the right one.
            List<(GameObject, Image, TMP_Text, TMP_Text, Image)> belt = new();

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
            BattleEditorUtility.Set(mso, "_worldSize", DungeonMapBuilder.WorldSize);
            BattleEditorUtility.Set(mso, "_floorSizes", DungeonMapBuilder.FloorSizes);
            BattleEditorUtility.Set(mso, "_floorGrids", DungeonMapBuilder.FloorGrids);
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
                element.FindPropertyRelative("Icon").objectReferenceValue = belt[i].Item2;
                element.FindPropertyRelative("Glyph").objectReferenceValue = belt[i].Item3;
                element.FindPropertyRelative("Count").objectReferenceValue = belt[i].Item4;
                element.FindPropertyRelative("Frame").objectReferenceValue = belt[i].Item5;
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

        private static void WriteAbilitySlots(SerializedObject so, string property, List<(GameObject, Image, TMP_Text, TMP_Text, TMP_Text, Image)> slots)
        {
            SerializedProperty array = so.FindProperty(property);
            array.arraySize = slots.Count;

            for (int i = 0; i < slots.Count; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Root").objectReferenceValue = slots[i].Item1;
                element.FindPropertyRelative("Icon").objectReferenceValue = slots[i].Item2;
                element.FindPropertyRelative("Glyph").objectReferenceValue = slots[i].Item3;
                element.FindPropertyRelative("Key").objectReferenceValue = slots[i].Item4;
                element.FindPropertyRelative("Charges").objectReferenceValue = slots[i].Item5;
                element.FindPropertyRelative("Cooldown").objectReferenceValue = slots[i].Item6;
            }
        }

        private static (GameObject, Image, TMP_Text, TMP_Text, TMP_Text, Image) AbilitySlot(RectTransform parent, Vector2 anchor, Vector2 position, string key)
        {
            RectTransform slot = CreateRect("Ability" + key, parent, anchor, position, new Vector2(56f, 56f));
            CreateImage("Back", slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f), s_panelLight);
            CreateImage("Frame", slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60f, 60f), s_frame).transform.SetAsFirstSibling();
            Image icon = CreateIcon(slot, 50f);
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

            return (slot.gameObject, icon, glyph, keyText, charges, cooldown);
        }

        private static (GameObject, Image, TMP_Text, TMP_Text, Image) BeltSlot(RectTransform parent, Vector2 anchor, Vector2 position, string key)
        {
            RectTransform slot = CreateRect("Belt" + key, parent, anchor, position, new Vector2(44f, 44f));
            CreateImage("Back", slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f), s_panelLight);
            Image frame = CreateImage("Frame", slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 48f), s_frame);
            frame.transform.SetAsFirstSibling();
            Image icon = CreateIcon(slot, 40f);
            TMP_Text glyph = CreateText("Glyph", slot, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f), 18f, TextAlignmentOptions.Center);
            glyph.fontStyle = FontStyles.Bold;
            TMP_Text keyText = CreateText("Key", slot, new Vector2(0f, 1f), new Vector2(3f, -1f), new Vector2(20f, 14f), 10f, TextAlignmentOptions.TopLeft);
            keyText.rectTransform.pivot = new Vector2(0f, 1f);
            keyText.text = key;
            keyText.color = new Color(1f, 0.9f, 0.6f);
            TMP_Text count = CreateText("Count", slot, new Vector2(1f, 0f), new Vector2(-3f, 1f), new Vector2(30f, 14f), 11f, TextAlignmentOptions.BottomRight);
            count.rectTransform.pivot = new Vector2(1f, 0f);

            return (slot.gameObject, icon, glyph, count, frame);
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
            preview = BuildPreview(previewImage, inputs, previewIndex, 352, 472, out _);
            TMP_Text titleText = CreateText("Title", dollPanel, new Vector2(0.5f, 1f), new Vector2(0f, -510f), new Vector2(400f, 30f), 24f, TextAlignmentOptions.Center);
            titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            titleText.color = s_gold;
            StatsView stats = BuildStats(dollPanel, new Vector2(0f, -546f), 460f, 105f, true, panel, out RectTransform statsTooltip);

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
            hints.text = "[Drag] move   [R Click] equip / use / unequip   [Shift + L Click] quick transfer   [Ctrl + Drag] split stack   [Drag outside] drop   ·   eye = not searched yet";
            hints.color = new Color(0.8f, 0.75f, 0.65f);

            statsTooltip.SetAsLastSibling();
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
            BattleEditorUtility.Set(so, "_statsView", stats);
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

        private static CharacterPreviewView BuildPreview(RawImage target, Inputs inputs, int index, int width, int height, out Camera camera)
        {
            GameObject stageRoot = new GameObject("[Preview" + index + "]");
            stageRoot.transform.position = new Vector3(500f + index * 20f, 300f, 500f);
            int layer = LayerMask.NameToLayer(PreviewLayer);
            stageRoot.layer = layer;

            GameObject stage = BattleEditorUtility.CreateChild("Stage", stageRoot.transform);
            stage.layer = layer;

            camera = BattleEditorUtility.CreateChild("Camera", stageRoot.transform, new Vector3(0f, 1.05f, 2.9f)).AddComponent<Camera>();
            camera.transform.LookAt(stageRoot.transform.position + new Vector3(0f, 0.95f, 0f));
            camera.cullingMask = 1 << layer;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.04f, 0.035f, 1f);
            camera.fieldOfView = 42f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 20f;
            camera.enabled = false;
            RenderTexture texture = new RenderTexture(width, height, 24) { name = "Preview" + index };
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
            Image back = CreateImage("Back", panel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, s_lobbyBack);
            Stretch(back.rectTransform, 0f);

            LobbyHomeView home = BuildLobbyHome(panel, inputs);
            SkillsView skills = BuildSkills(panel, inputs);
            InventoryView inventory = BuildInventory(panel, canvas, inputs, itemPrefab, cellPrefab, "Kit", false, 1, out CharacterPreviewView preview);
            RectTransform inventoryRect = (RectTransform)inventory.transform;
            inventoryRect.anchorMin = inventoryRect.anchorMax = inventoryRect.pivot = new Vector2(0.5f, 1f);
            inventoryRect.anchoredPosition = new Vector2(0f, -60f);
            inventory.transform.Find("Hints").gameObject.SetActive(false);
            StashPagesView stashPages = BuildStashPages(inventory);
            MerchantsView merchants = BuildMerchants(panel);

            // Top bar: rank shield and the page tabs, each marked by a diamond tick on the rule.
            RectTransform bar = CreateRect("TopBar", panel, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 82f));
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = Vector2.one;
            CreateImage("Back", bar, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.94f)).rectTransform.StretchFill();
            RectTransform rule = CreateImage("Rule", bar, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 2f), s_frame).rectTransform;
            rule.anchorMin = Vector2.zero;
            rule.anchorMax = new Vector2(1f, 0f);

            string[] names = { "Lobby", "Skills", "Stash", "Merchants" };
            Button[] tabs = new Button[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                float x = 240f + i * 170f;
                tabs[i] = CreateButton("Tab" + names[i], bar, new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(170f, 72f), names[i]);
                ((RectTransform)tabs[i].transform).pivot = new Vector2(0.5f, 0.5f);
                tabs[i].image.sprite = DungeonUiSpriteBuilder.Load("Glow");
                tabs[i].image.type = Image.Type.Simple;
                tabs[i].GetComponentInChildren<TMP_Text>().fontSize = 26f;
                RectTransform tick = CreateImage("Tick", bar, Vector2.zero, new Vector2(x, 1f), new Vector2(9f, 9f), s_gold).rectTransform;
                tick.pivot = new Vector2(0.5f, 0.5f);
                tick.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }

            Image shield = CreateImage("Rank", bar, new Vector2(0f, 1f), new Vector2(26f, 0f), new Vector2(88f, 110f), Color.white);
            shield.sprite = DungeonUiSpriteBuilder.Load("Shield");
            TMP_Text rank = CreateText("Text", shield.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(88f, 70f), 15f, TextAlignmentOptions.Center);
            rank.color = new Color(0.16f, 0.1f, 0.03f);
            rank.fontStyle = FontStyles.Bold;
            rank.lineSpacing = -30f;

            LobbyView view = panel.gameObject.AddComponent<LobbyView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_home", home);
            BattleEditorUtility.Set(so, "_skills", skills);
            BattleEditorUtility.Set(so, "_inventory", inventory);
            BattleEditorUtility.Set(so, "_inventoryPreview", preview);
            BattleEditorUtility.Set(so, "_tabs", tabs);
            BattleEditorUtility.Set(so, "_merchants", merchants);
            BattleEditorUtility.Set(so, "_stashPages", stashPages);
            BattleEditorUtility.Set(so, "_pages", new DisplayableView[] { home, skills, inventory, merchants });
            BattleEditorUtility.Set(so, "_rankText", rank);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        /// Stash page tabs under the stash grid and the sell counter below them.
        private static StashPagesView BuildStashPages(InventoryView inventory)
        {
            Transform other = inventory.transform.Find("OtherPanel");
            Vector2 topLeft = new Vector2(0f, 1f);
            Button[] tabs = new Button[PlayerSessionComponent.StashPages];

            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i] = CreateButton("Page" + (i + 1), other, topLeft, new Vector2(16f + i * 96f, -284f), new Vector2(88f, 34f), $"Page {i + 1}");
                ((RectTransform)tabs[i].transform).pivot = topLeft;
                tabs[i].gameObject.AddComponent<ItemDropZone>();
            }

            TMP_Text coins = CreateText("Coins", other, new Vector2(1f, 1f), new Vector2(-16f, -284f), new Vector2(240f, 34f), 18f, TextAlignmentOptions.MidlineRight);
            coins.rectTransform.pivot = new Vector2(1f, 1f);
            coins.color = s_gold;

            Image counter = CreateImage("Sell", other, topLeft, new Vector2(16f, -334f), new Vector2(648f, 88f), new Color(0.12f, 0.09f, 0.05f, 0.95f));
            counter.rectTransform.pivot = topLeft;
            counter.raycastTarget = true;
            ItemDropZone sellZone = counter.gameObject.AddComponent<ItemDropZone>();
            CreateImage("Frame", counter.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(648f, 2f), s_frame).rectTransform.pivot = new Vector2(0.5f, 1f);
            TMP_Text sellText = CreateText("Text", counter.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 20f, TextAlignmentOptions.Center);
            Stretch(sellText.rectTransform, 8f);
            sellText.color = new Color(0.85f, 0.8f, 0.7f);

            StashPagesView view = other.gameObject.AddComponent<StashPagesView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_inventory", inventory);
            BattleEditorUtility.Set(so, "_tabs", tabs);
            BattleEditorUtility.Set(so, "_sellZone", sellZone);
            BattleEditorUtility.Set(so, "_sellText", sellText);
            BattleEditorUtility.Set(so, "_coinsText", coins);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        /// Front page laid out like the Dark and Darker lobby: character in the middle, party on the right, map card and Start bottom left.
        private static LobbyHomeView BuildLobbyHome(RectTransform lobby, Inputs inputs)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            Vector2 top = new Vector2(0.5f, 1f);
            Vector2 topLeft = new Vector2(0f, 1f);
            Vector2 topRight = Vector2.one;
            Vector2 bottom = new Vector2(0.5f, 0f);
            Vector2 bottomLeft = Vector2.zero;
            Vector2 bottomRight = new Vector2(1f, 0f);
            RectTransform page = CreateRect("Home", lobby, center, Vector2.zero, Vector2.zero);
            Stretch(page, 0f);

            RawImage backdrop = CreateRect("Backdrop", page, center, Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
            Stretch(backdrop.rectTransform, 0f);
            AspectRatioFitter fitter = backdrop.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;
            CharacterPreviewView preview = BuildPreview(backdrop, inputs, 5, 1920, 1080, out Camera camera);
            BuildTavern(camera, preview);
            Image vignette = CreateImage("Vignette", page, center, Vector2.zero, Vector2.zero, Color.white);
            vignette.sprite = DungeonUiSpriteBuilder.Load("Vignette");
            Stretch(vignette.rectTransform, 0f);

            RectTransform help = CreateRect("Help", page, topLeft, new Vector2(56f, -128f), new Vector2(250f, 150f));
            CreateImage("RuleTop", help, top, Vector2.zero, new Vector2(250f, 2f), s_frame);
            TMP_Text helpTitle = CreateText("Title", help, top, new Vector2(0f, -6f), new Vector2(250f, 28f), 17f, TextAlignmentOptions.Center);
            helpTitle.text = "Need help?";
            helpTitle.color = s_gold;
            TMP_Text helpText = CreateText("Text", help, top, new Vector2(0f, -40f), new Vector2(250f, 100f), 15f, TextAlignmentOptions.Top);
            helpText.text = "Press <b>H</b> to see the controls.\nPick perks and skills on the <b>Skills</b> tab, pack the kit in the <b>Stash</b>.";
            helpText.color = s_text;
            CreateImage("RuleBottom", help, bottom, Vector2.zero, new Vector2(250f, 2f), s_frame);

            TMP_Text nameText = CreateText("Name", page, top, new Vector2(0f, -196f), new Vector2(700f, 96f), 27f, TextAlignmentOptions.Bottom);
            nameText.color = s_text;
            CreateImage("NameRule", page, top, new Vector2(0f, -298f), new Vector2(260f, 2f), s_frame);

            TMP_Text partyTitle = CreateText("PartyTitle", page, topRight, new Vector2(-56f, -128f), new Vector2(270f, 30f), 17f, TextAlignmentOptions.Center);
            partyTitle.color = s_text;
            CreateImage("PartyRule", page, topRight, new Vector2(-56f, -164f), new Vector2(270f, 2f), s_frame);
            TMP_Text[] partySlots = new TMP_Text[3];

            for (int i = 0; i < partySlots.Length; i++)
            {
                RectTransform card = CreateRect("PartySlot" + i, page, topRight, new Vector2(-56f, -180f - i * 98f), new Vector2(270f, 86f));
                CreateImage("Frame", card, center, Vector2.zero, Vector2.zero, s_frame).rectTransform.StretchFill();
                Stretch(CreateImage("Back", card, center, Vector2.zero, Vector2.zero, new Color(0.05f, 0.04f, 0.035f, 0.96f)).rectTransform, 2f);
                partySlots[i] = CreateText("Text", card, center, Vector2.zero, Vector2.zero, 20f, TextAlignmentOptions.MidlineLeft);
                Stretch(partySlots[i].rectTransform, 14f);
            }

            TMP_Text mapLabel = CreateText("MapLabel", page, bottomLeft, new Vector2(24f, 322f), new Vector2(307f, 26f), 17f, TextAlignmentOptions.Center);
            mapLabel.text = "Select Map";
            mapLabel.color = s_textDim;
            Button map = CreateButton("Map", page, bottomLeft, new Vector2(24f, 122f), new Vector2(307f, 194f), "Change Map");
            map.image.color = s_panel;
            TMP_Text mapCaption = map.GetComponentInChildren<TMP_Text>();
            mapCaption.fontSize = 20f;
            mapCaption.alignment = TextAlignmentOptions.Bottom;
            mapCaption.margin = new Vector4(0f, 0f, 0f, 6f);
            TMP_Text mapTitle = CreateText("Title", map.transform, top, new Vector2(0f, -8f), new Vector2(291f, 26f), 19f, TextAlignmentOptions.Center);
            TMP_Text mapInfo = CreateText("Info", map.transform, top, new Vector2(0f, -34f), new Vector2(291f, 20f), 13f, TextAlignmentOptions.Center);
            mapInfo.color = new Color(0.45f, 0.7f, 0.95f);
            RawImage mapImage = CreateRect("Picture", map.transform, top, new Vector2(0f, -60f), new Vector2(291f, 96f)).gameObject.AddComponent<RawImage>();
            mapImage.raycastTarget = false;
            mapImage.uvRect = new Rect(0f, 0.33f, 1f, 0.33f);
            mapImage.color = new Color(0.8f, 0.75f, 0.65f);

            Button start = CreateButton("Start", page, bottomLeft, new Vector2(24f, 24f), new Vector2(307f, 86f), "Start");
            start.image.color = new Color(0.17f, 0.16f, 0.15f, 0.97f);
            start.GetComponentInChildren<TMP_Text>().fontSize = 34f;

            Button Tool(string name, string caption, string glyph, int column)
            {
                Button button = CreateButton(name, page, bottomRight, new Vector2(-24f - column * 86f, 24f), new Vector2(68f, 68f), glyph);
                button.image.sprite = DungeonUiSpriteBuilder.Load("Square");
                button.image.type = Image.Type.Simple;
                button.image.color = new Color(0.72f, 0.64f, 0.5f);
                TMP_Text text = CreateText("Caption", button.transform, top, new Vector2(0f, 4f), new Vector2(86f, 36f), 12f, TextAlignmentOptions.Bottom);
                text.rectTransform.pivot = bottom;
                text.text = caption;
                text.color = s_text;

                return button;
            }

            Button characters = Tool("Characters", "Change\nCharacter", "Chr", 0);
            Button wipe = Tool("Wipe", "Wipe\nSave", "Wipe", 1);
            Button reset = Tool("ResetKit", "Squire\nKit", "Kit", 2);

            Image resultBack = CreateImage("Result", page, bottom, new Vector2(0f, 24f), new Vector2(640f, 34f), new Color(0f, 0f, 0f, 0.65f));
            TMP_Text result = CreateText("Text", resultBack.rectTransform, center, Vector2.zero, new Vector2(620f, 34f), 16f, TextAlignmentOptions.Center);
            result.color = s_text;
            resultBack.gameObject.SetActive(false);

            (string title, string info, Texture picture, string scene)[] destinations =
            {
                (DungeonSceneBuilder.Title, "Two floors · undead · escape through the portals", inputs.FloorMaps.Length > 0 ? inputs.FloorMaps[0] : null, SceneTravel.DungeonScene),
                ("Training Grounds", "Test scene · weapon table, dummies, dev spawns", DungeonTextureBuilder.Load("Cobble", false), SceneTravel.SandboxScene)
            };

            LobbyHomeView view = page.gameObject.AddComponent<LobbyHomeView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_context", inputs.Context);
            BattleEditorUtility.Set(so, "_preview", preview);
            BattleEditorUtility.Set(so, "_nameText", nameText);
            BattleEditorUtility.Set(so, "_partyTitleText", partyTitle);
            BattleEditorUtility.Set(so, "_partySlots", partySlots);
            BattleEditorUtility.Set(so, "_mapButton", map);
            BattleEditorUtility.Set(so, "_mapTitleText", mapTitle);
            BattleEditorUtility.Set(so, "_mapInfoText", mapInfo);
            BattleEditorUtility.Set(so, "_mapImage", mapImage);
            BattleEditorUtility.Set(so, "_startButton", start);
            BattleEditorUtility.Set(so, "_resetKitButton", reset);
            BattleEditorUtility.Set(so, "_wipeButton", wipe);
            BattleEditorUtility.Set(so, "_charactersButton", characters);
            BattleEditorUtility.Set(so, "_resultRoot", resultBack.gameObject);
            BattleEditorUtility.Set(so, "_resultText", result);
            so.FindProperty("_destinations").arraySize = destinations.Length;

            for (int i = 0; i < destinations.Length; i++)
            {
                BattleEditorUtility.Set(so, $"_destinations.Array.data[{i}].Title", destinations[i].title);
                BattleEditorUtility.Set(so, $"_destinations.Array.data[{i}].Info", destinations[i].info);
                BattleEditorUtility.Set(so, $"_destinations.Array.data[{i}].Picture", destinations[i].picture);
                BattleEditorUtility.Set(so, $"_destinations.Array.data[{i}].Scene", destinations[i].scene);
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        /// Dresses the preview stage as a candle-lit tavern corner and frames the character behind the table.
        private static void BuildTavern(Camera camera, CharacterPreviewView preview)
        {
            Transform root = camera.transform.parent;
            camera.transform.localPosition = new Vector3(0f, 1.5f, 4f);
            camera.transform.LookAt(root.position + new Vector3(0f, 1.38f, 0f));
            camera.fieldOfView = 32f;
            camera.farClipPlane = 30f;
            camera.backgroundColor = Color.black;
            UnityEngine.Rendering.Universal.UniversalAdditionalCameraData data = camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            // The rig spawns turned 15 degrees aside; here it faces the camera.
            root.Find("Stage").localRotation = Quaternion.Euler(0f, 15f, 0f);
            BattleEditorUtility.Set(preview, "_isUnarmed", true);

            Transform set = BattleEditorUtility.CreateChild("Tavern", root).transform;
            Mesh shell = new DungeonMeshBuilder(0.5f)
                .Box(new Vector3(0f, 2.2f, -3f), new Vector3(11f, 4.4f, 0.6f))
                .Box(new Vector3(-5.2f, 2.2f, 0f), new Vector3(0.6f, 4.4f, 6.6f))
                .Box(new Vector3(5.2f, 2.2f, 0f), new Vector3(0.6f, 4.4f, 6.6f))
                .Save("TavernShell");
            Mesh floor = new DungeonMeshBuilder(0.5f).Quad(Vector3.zero, Vector3.up, Vector3.right * 5.5f, Vector3.forward * 4f).Save("TavernFloor");
            DungeonPropBuilder.MeshObject("Shell", set, shell, DungeonPropBuilder.StoneWall, default, default, false, false);
            DungeonPropBuilder.MeshObject("Floor", set, floor, DungeonPropBuilder.WoodPlanks, default, default, false, false);

            void Prop(string prefab, Vector3 position, float yaw)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(DungeonContentBuilder.Prefab(prefab)), set);
                instance.transform.localPosition = position;
                instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            }

            // The camera looks down -Z, so +X is the left side of the picture.
            Prop("Table", new Vector3(0f, 0f, 1.25f), 180f);
            Prop("CandleCluster", new Vector3(0.8f, 0.82f, 1.2f), 0f);
            Prop("WallTorch", new Vector3(-1.7f, 2f, -2.7f), 0f);
            Prop("WallTorch", new Vector3(1.7f, 2f, -2.7f), 0f);
            Prop("Pillar", new Vector3(-3.6f, 0f, -2.3f), 0f);
            Prop("Pillar", new Vector3(3.6f, 0f, -2.3f), 0f);
            Prop("StandRangerMale", new Vector3(-1.8f, 0f, -1.6f), 15f);
            Prop("StandPeasantFemale", new Vector3(1.8f, 0f, -1.6f), -15f);
            Prop("SkullPile", new Vector3(-1.1f, 0f, -2.3f), 0f);

            // Dungeon props are lit for big rooms; this close to the camera they would burn the picture out.
            foreach (FlickerLightComponent flicker in set.GetComponentsInChildren<FlickerLightComponent>())
            {
                SerializedObject so = new SerializedObject(flicker);
                so.FindProperty("_baseIntensity").floatValue *= 0.5f;
                so.FindProperty("_amplitude").floatValue *= 0.5f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            BattleEditorUtility.SetLayerRecursively(set.gameObject, root.gameObject.layer);
        }

        /// "Perks and Skills" page laid out like Dark and Darker: attribute sheet, the doll in an arch with equipped slots, the class pools.
        private static SkillsView BuildSkills(RectTransform lobby, Inputs inputs)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            Vector2 top = new Vector2(0.5f, 1f);
            Color line = new Color(0.6f, 0.57f, 0.5f);
            RectTransform page = CreateRect("Skills", lobby, center, Vector2.zero, Vector2.zero);
            Stretch(page, 0f);

            Image sheet = CreateImage("Stats", page, new Vector2(0f, 1f), new Vector2(20f, -96f), new Vector2(530f, 964f), Color.white);
            sheet.sprite = DungeonUiSpriteBuilder.Load("Panel");
            StatsView stats = BuildStats(sheet.rectTransform, new Vector2(0f, -34f), 530f, 130f, false, page, out RectTransform statsTooltip);

            TMP_Text title = CreateText("Title", page, top, new Vector2(0f, -104f), new Vector2(620f, 34f), 23f, TextAlignmentOptions.Center);
            title.text = "Perks and Skills";
            title.color = s_gold;
            CreateImage("TitleRule", page, top, new Vector2(0f, -146f), new Vector2(620f, 2f), s_frame);
            TMP_Text hint = CreateText("Hint", page, top, new Vector2(0f, -154f), new Vector2(760f, 22f), 13f, TextAlignmentOptions.Center);
            hint.text = "Drag a perk or a skill onto a slot  ·  click a perk to equip or remove it  ·  LMB / RMB on a skill binds it to Q / E";
            hint.color = s_textDim;

            RawImage doll = CreateRect("Doll", page, top, new Vector2(0f, -262f), new Vector2(520f, 700f)).gameObject.AddComponent<RawImage>();
            CharacterPreviewView preview = BuildPreview(doll, inputs, 2, 520, 700, out Camera camera);
            camera.backgroundColor = s_lobbyBack;
            CreateImage("Arch", page, top, new Vector2(0f, -226f), new Vector2(576f, 740f), line).sprite = DungeonUiSpriteBuilder.Load("Arch");
            CreateImage("ArchBase", page, top, new Vector2(0f, -967f), new Vector2(470f, 2f), line);

            // Equipped perks sit on the arch posts, the two skills close it at the bottom.
            AbilityIconView[] perkSlots = new AbilityIconView[4];
            AbilityIconView[] skillSlots = new AbilityIconView[2];

            for (int i = 0; i < perkSlots.Length; i++)
                perkSlots[i] = AbilityIcon("PerkSlot" + i, page, top, new Vector2(i < 2 ? -284f : 284f, i % 2 == 0 ? -574f : -768f), "Diamond", 104f);

            for (int i = 0; i < skillSlots.Length; i++)
                skillSlots[i] = AbilityIcon("SkillSlot" + i, page, top, new Vector2(i == 0 ? -284f : 284f, -968f), "Square", 88f);

            Image pool = CreateImage("Pool", page, Vector2.one, new Vector2(-20f, -96f), new Vector2(530f, 964f), Color.white);
            pool.sprite = DungeonUiSpriteBuilder.Load("Panel");

            RectTransform Section(string name, float y)
            {
                CreateImage(name + "Band", pool.rectTransform, top, new Vector2(0f, y), new Vector2(470f, 38f), new Color(0f, 0f, 0f, 0.35f));
                TMP_Text header = CreateText(name + "Title", pool.rectTransform, top, new Vector2(0f, y), new Vector2(470f, 38f), 22f, TextAlignmentOptions.Center);
                header.text = name;
                header.color = s_text;
                RectTransform grid = CreateRect(name, pool.rectTransform, top, new Vector2(0f, y - 48f), new Vector2(470f, 222f));
                GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
                layout.cellSize = new Vector2(104f, 104f);
                layout.spacing = new Vector2(18f, 14f);
                layout.childAlignment = TextAnchor.UpperCenter;

                return grid;
            }

            RectTransform perks = Section("Perks", -28f);
            RectTransform skillRoot = Section("Skills", -336f);
            RectTransform spells = Section("Spells", -644f);
            // Ten spells in two rows of five; the title tells how they go into the two wheels.
            GridLayoutGroup spellLayout = spells.GetComponent<GridLayoutGroup>();
            spellLayout.cellSize = new Vector2(84f, 84f);
            spellLayout.spacing = new Vector2(10f, 12f);
            pool.rectTransform.Find("SpellsTitle").GetComponent<TMP_Text>().text = "Spells  <size=70%>L Click wheel I · R Click wheel II</size>";
            AbilityIconView perkIcon = AbilityIcon("PerkIcon", page, center, Vector2.zero, "Diamond", 104f);
            AbilityIconView skillIcon = AbilityIcon("SkillIcon", page, center, Vector2.zero, "Square", 88f);
            perkIcon.gameObject.SetActive(false);
            skillIcon.gameObject.SetActive(false);

            RectTransform tooltip = CreateRect("Tooltip", page, center, Vector2.zero, new Vector2(340f, 112f));
            CreateImage("Back", tooltip, center, Vector2.zero, Vector2.zero, new Color(0.03f, 0.03f, 0.03f, 0.96f)).rectTransform.StretchFill();
            CreateImage("Frame", tooltip, top, Vector2.zero, new Vector2(340f, 3f), s_frame);
            TMP_Text tooltipText = CreateText("Text", tooltip, center, Vector2.zero, Vector2.zero, 15f, TextAlignmentOptions.TopLeft);
            Stretch(tooltipText.rectTransform, 12f);
            statsTooltip.SetAsLastSibling();

            SkillsView view = page.gameObject.AddComponent<SkillsView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_preview", preview);
            BattleEditorUtility.Set(so, "_perkIconPrefab", perkIcon);
            BattleEditorUtility.Set(so, "_skillIconPrefab", skillIcon);
            BattleEditorUtility.Set(so, "_perksRoot", perks);
            BattleEditorUtility.Set(so, "_skillsRoot", skillRoot);
            BattleEditorUtility.Set(so, "_spellsRoot", spells);
            BattleEditorUtility.Set(so, "_perkSlots", perkSlots);
            BattleEditorUtility.Set(so, "_skillSlots", skillSlots);
            BattleEditorUtility.Set(so, "_stats", stats);
            BattleEditorUtility.Set(so, "_tooltip", tooltip);
            BattleEditorUtility.Set(so, "_tooltipText", tooltipText);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        /// Character sheet: the hexagram with the attributes on its corners and the edge stats between them, derived stats as
        /// circles grouped by their attribute below. The tooltip opens beside the sheet (hence the width of the panel it sits on) and is created under
        /// the given parent so the caller can keep it above the other panels.
        private static StatsView BuildStats(Transform parent, Vector2 position, float width, float radius, bool isCompact, Transform tooltipParent,
            out RectTransform tooltip)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            Vector2 top = new Vector2(0.5f, 1f);
            float chartHeight = 2f * radius + 116f;
            RectTransform root = CreateRect("StatSheet", parent, top, position, new Vector2(width, chartHeight));
            RectTransform chartRoot = CreateRect("Hexagram", root, top, new Vector2(0f, -chartHeight * 0.5f), Vector2.zero);
            chartRoot.pivot = center;
            float webSize = 2f * radius / DungeonUiSpriteBuilder.HexagramRadius;
            CreateImage("Web", chartRoot, center, Vector2.zero, new Vector2(webSize, webSize), Color.white).sprite = DungeonUiSpriteBuilder.Load("Hexagram");
            HexChartGraphic chart = CreateRect("Chart", chartRoot, center, Vector2.zero, new Vector2(2f * radius, 2f * radius)).gameObject.AddComponent<HexChartGraphic>();
            chart.raycastTarget = false;
            Color[] colors = new Color[StatSheet.AttributeCount];

            for (int i = 0; i < colors.Length; i++)
                colors[i] = StatSheet.ColorOf((StatId)i);

            SerializedObject chartObject = new SerializedObject(chart);
            BattleEditorUtility.Set(chartObject, "_radius", radius);
            BattleEditorUtility.Set(chartObject, "_colors", colors);
            chartObject.ApplyModifiedPropertiesWithoutUndo();

            List<StatLabelView> labels = new();
            Vector2 badge = Vector2.one * (isCompact ? 50f : 58f);
            Vector2 circle = Vector2.one * (isCompact ? 42f : 50f);

            // Edge stats first: the attribute badges on the corners are drawn over the web and the circles.
            for (int i = 0; i < StatSheet.AttributeCount; i++)
            {
                float angle = (i + 0.5f) * Mathf.PI / 3f;
                Vector2 direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                labels.Add(StatNode(chartRoot, StatSheet.Edge(i), direction * radius * 0.866f, "Circle", circle, isCompact ? 11.5f : 13f, isCompact ? 11f : 12f));
            }

            for (int i = 0; i < StatSheet.AttributeCount; i++)
            {
                float angle = i * Mathf.PI / 3f;
                Vector2 direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                labels.Add(StatNode(chartRoot, (StatId)i, direction * radius, "Diamond", badge, isCompact ? 17f : 19f, isCompact ? 14f : 15f));
            }

            float y = -chartHeight - 6f;

            // Every derived stat is a circle in its own colour, like the edge stats on the web: none of them is a footnote.
            if (isCompact)
            {
                StatId[] stats =
                {
                    StatId.Health, StatId.MoveSpeed, StatId.ArmorRating, StatId.PhysicalReduction, StatId.MagicResistance, StatId.MagicalReduction,
                    StatId.PhysicalDamage, StatId.ActionSpeed
                };
                float pitch = (width - 16f) / stats.Length;

                for (int i = 0; i < stats.Length; i++)
                    labels.Add(StatCircle(root, stats[i], new Vector2((i - (stats.Length - 1) * 0.5f) * pitch, y), 42f, 11.5f, 9.5f, pitch - 1f));
            }
            else
            {
                const float pitch = 80f;
                const float gap = 16f;
                const float headerHeight = 26f;
                const float rowHeight = 116f;
                (string title, Color color, StatId[] stats) Own(StatId attribute) => (StatSheet.Name(attribute), StatSheet.ColorOf(attribute), StatSheet.OwnStats(attribute));

                // Own stats of the attributes in ring order, then what the gear gives.
                (string title, Color color, StatId[] stats)[][] rows =
                {
                    new[] { Own(StatId.Flesh), Own(StatId.Grip) },
                    new[] { Own(StatId.Reflex), Own(StatId.Craft) },
                    new[] { Own(StatId.Insight), Own(StatId.Resonance) },
                    new[]
                    {
                        ("Defense", new Color(0.75f, 0.76f, 0.8f),
                            new[] { StatId.ArmorRating, StatId.PhysicalReduction, StatId.MagicResistance, StatId.MagicalReduction })
                    }
                };

                foreach ((string title, Color color, StatId[] stats)[] row in rows)
                {
                    float total = (row.Length - 1) * gap;

                    foreach ((string title, Color color, StatId[] stats) group in row)
                        total += group.stats.Length * pitch;

                    float x = -total * 0.5f;

                    foreach ((string title, Color color, StatId[] stats) group in row)
                    {
                        StatHeader(root, group.title, group.color, new Vector2(x + 8f, y), group.stats.Length * pitch - 16f);

                        for (int i = 0; i < group.stats.Length; i++)
                            labels.Add(StatCircle(root, group.stats[i], new Vector2(x + (i + 0.5f) * pitch, y - headerHeight), 54f, 13.5f, 11.5f, pitch - 2f));

                        x += group.stats.Length * pitch + gap;
                    }

                    y -= rowHeight;
                }
            }

            tooltip = CreateRect("StatTooltip", tooltipParent, center, Vector2.zero, new Vector2(380f, 100f));
            Image tooltipBack = tooltip.gameObject.AddComponent<Image>();
            tooltipBack.color = new Color(0.03f, 0.03f, 0.03f, 0.97f);
            tooltipBack.raycastTarget = false;
            VerticalLayoutGroup layout = tooltip.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            tooltip.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            CreateImage("Frame", tooltip, top, Vector2.zero, new Vector2(380f, 3f), s_frame).gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            TMP_Text tooltipText = CreateText("Text", tooltip, center, Vector2.zero, Vector2.zero, 15f, TextAlignmentOptions.TopLeft);
            tooltipText.color = s_text;
            tooltip.gameObject.SetActive(false);

            StatsView view = root.gameObject.AddComponent<StatsView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_labels", labels);
            BattleEditorUtility.Set(so, "_chart", chart);
            BattleEditorUtility.Set(so, "_tooltip", tooltip);
            BattleEditorUtility.Set(so, "_tooltipText", tooltipText);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        /// Badge of the hexagram with the value inside and the name outside: above or below for the nodes next to the
        /// vertical axis, at the side for the others.
        private static StatLabelView StatNode(RectTransform parent, StatId stat, Vector2 position, string sprite, Vector2 size, float valueSize, float nameSize)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            bool isAttribute = StatSheet.IsAttribute(stat);
            RectTransform node = CreateRect(stat.ToString(), parent, center, position, size);
            Image frame = node.gameObject.AddComponent<Image>();
            frame.sprite = DungeonUiSpriteBuilder.Load(sprite);
            frame.color = StatSheet.ColorOf(stat);
            TMP_Text value = CreateText("Value", node, center, Vector2.zero, size + new Vector2(16f, 0f), valueSize, TextAlignmentOptions.Center);
            value.fontStyle = FontStyles.Bold;
            Vector2 direction = position.normalized;
            bool isAtSide = Mathf.Abs(direction.x) > 0.8f;
            float side = Mathf.Abs(direction.x) < 0.1f ? 0f : Mathf.Sign(direction.x);
            TMP_Text name = CreateText("Name", node, center, Vector2.zero, new Vector2(120f, nameSize + 6f), nameSize,
                !isAtSide ? TextAlignmentOptions.Center : side > 0f ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight);

            if (isAtSide)
            {
                name.rectTransform.pivot = new Vector2(side > 0f ? 0f : 1f, 0.5f);
                name.rectTransform.anchoredPosition = new Vector2(side * (size.x * 0.5f + 4f), 0f);
            }
            else
            {
                name.rectTransform.anchoredPosition = new Vector2(side * 10f, Mathf.Sign(direction.y) * (size.y * 0.5f + nameSize * 0.5f + 5f));
            }

            name.text = StatSheet.Name(stat);
            name.color = isAttribute ? StatSheet.ColorOf(stat) : s_textDim;
            name.fontStyle = isAttribute ? FontStyles.Bold : FontStyles.Normal;
            name.raycastTarget = true;

            return StatLabel(node.gameObject, stat, value);
        }

        /// Round badge of a derived stat, placed by its top: the value inside in the stat's own colour ring, the name underneath.
        private static StatLabelView StatCircle(RectTransform parent, StatId stat, Vector2 position, float diameter, float valueSize, float nameSize, float nameWidth)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            RectTransform node = CreateRect(stat.ToString(), parent, new Vector2(0.5f, 1f), position, Vector2.one * diameter);
            Image frame = node.gameObject.AddComponent<Image>();
            frame.sprite = DungeonUiSpriteBuilder.Load("Circle");
            frame.color = StatSheet.ColorOf(stat);
            TMP_Text value = CreateText("Value", node, center, Vector2.zero, new Vector2(diameter + 14f, diameter), valueSize, TextAlignmentOptions.Center);
            value.fontStyle = FontStyles.Bold;
            TMP_Text name = CreateText("Name", node, new Vector2(0.5f, 0f), new Vector2(0f, -3f), new Vector2(nameWidth, nameSize * 2.5f), nameSize, TextAlignmentOptions.Top);
            name.rectTransform.pivot = new Vector2(0.5f, 1f);
            name.text = StatSheet.Name(stat);
            name.color = new Color(0.8f, 0.77f, 0.7f);
            name.lineSpacing = -12f;
            name.raycastTarget = true;

            return StatLabel(node.gameObject, stat, value);
        }

        private static void StatHeader(RectTransform parent, string title, Color color, Vector2 position, float width)
        {
            TMP_Text header = CreateText(title + "Header", parent, new Vector2(0.5f, 1f), position, new Vector2(width, 22f), 13f, TextAlignmentOptions.BottomLeft);
            header.rectTransform.pivot = new Vector2(0f, 1f);
            header.text = title.ToUpperInvariant();
            header.color = color;
            header.fontStyle = FontStyles.Bold;
            header.characterSpacing = 6f;
            color.a = 0.4f;
            CreateImage("Rule", header.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, -2f), new Vector2(width, 1f), color);
        }

        private static StatLabelView StatLabel(GameObject target, StatId stat, TMP_Text value)
        {
            StatLabelView label = target.AddComponent<StatLabelView>();
            SerializedObject so = new SerializedObject(label);
            BattleEditorUtility.Set(so, "_stat", stat);
            BattleEditorUtility.Set(so, "_valueText", value);
            so.ApplyModifiedPropertiesWithoutUndo();

            return label;
        }

        /// "Merchants" page: who sells on the left, the counter with kind filters and a scrolling ware grid in the middle,
        /// the purse and the details of the hovered ware on the right.
        private static MerchantsView BuildMerchants(RectTransform lobby)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            Vector2 top = new Vector2(0.5f, 1f);
            Sprite panelSprite = DungeonUiSpriteBuilder.Load("Panel");
            RectTransform page = CreateRect("Merchants", lobby, center, Vector2.zero, Vector2.zero);
            Stretch(page, 0f);

            TMP_Text Title(Transform parent, string text, float y, float size)
            {
                TMP_Text title = CreateText("Title", parent, top, new Vector2(0f, y), new Vector2(460f, 36f), size, TextAlignmentOptions.Center);
                title.text = text;
                title.color = s_gold;

                return title;
            }

            Image list = CreateImage("List", page, new Vector2(0f, 1f), new Vector2(20f, -96f), new Vector2(360f, 964f), Color.white);
            list.sprite = panelSprite;
            Title(list.rectTransform, "Merchants", -34f, 23f);
            CreateImage("Rule", list.rectTransform, top, new Vector2(0f, -76f), new Vector2(290f, 2f), s_frame);
            RectTransform merchantsRoot = CreateRect("Merchants", list.rectTransform, top, new Vector2(0f, -92f), new Vector2(296f, 800f));
            VerticalLayoutGroup merchantsLayout = merchantsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            merchantsLayout.spacing = 10f;
            merchantsLayout.childControlHeight = false;
            merchantsLayout.childForceExpandHeight = false;
            Button merchantButton = CreateButton("MerchantButton", merchantsRoot, top, Vector2.zero, new Vector2(296f, 72f), string.Empty);
            TMP_Text merchantLabel = merchantButton.GetComponentInChildren<TMP_Text>();
            merchantLabel.fontSize = 19f;
            merchantLabel.alignment = TextAlignmentOptions.MidlineLeft;
            merchantLabel.margin = new Vector4(16f, 0f, 8f, 0f);
            merchantButton.gameObject.SetActive(false);

            Image counter = CreateImage("Counter", page, top, new Vector2(-90f, -96f), new Vector2(940f, 964f), Color.white);
            counter.sprite = panelSprite;
            TMP_Text title = Title(counter.rectTransform, string.Empty, -30f, 26f);
            title.fontStyle = FontStyles.Bold;
            TMP_Text description = CreateText("Description", counter.rectTransform, top, new Vector2(0f, -68f), new Vector2(840f, 24f), 15f, TextAlignmentOptions.Center);
            description.color = s_textDim;

            string[] filters = { "All", "Weapons", "Armor", "Consumables", "Utility", "Treasure" };
            Button[] filterButtons = new Button[filters.Length];

            for (int i = 0; i < filters.Length; i++)
            {
                filterButtons[i] = CreateButton("Filter" + filters[i], counter.rectTransform, top, new Vector2((i - (filters.Length - 1) * 0.5f) * 140f, -104f), new Vector2(132f, 34f), filters[i]);
                filterButtons[i].image.color = new Color(0.05f, 0.045f, 0.04f, 0.9f);
            }

            // Ten wares in a row; the wheel and a drag scroll the counter.
            const float ware = 80f;
            const float wareGap = 8f;
            RectTransform viewport = CreateRect("Viewport", counter.rectTransform, top, new Vector2(0f, -154f), new Vector2(ware * 10f + wareGap * 9f, 774f));
            Image viewportHit = viewport.gameObject.AddComponent<Image>();
            viewportHit.color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform waresRoot = CreateRect("Wares", viewport, top, Vector2.zero, Vector2.zero);
            waresRoot.anchorMin = new Vector2(0f, 1f);
            waresRoot.anchorMax = Vector2.one;
            waresRoot.sizeDelta = Vector2.zero;
            GridLayoutGroup grid = waresRoot.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(ware, ware);
            grid.spacing = new Vector2(wareGap, wareGap);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 10;
            waresRoot.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.content = waresRoot;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            RectTransform wareRect = CreateRect("Ware", page, center, Vector2.zero, new Vector2(ware, ware));
            Image wareBack = wareRect.gameObject.AddComponent<Image>();
            wareBack.color = new Color(0.09f, 0.08f, 0.07f, 0.95f);
            Image wareFrame = CreateImage("Frame", wareRect, center, Vector2.zero, Vector2.zero, Color.white);
            Stretch(wareFrame.rectTransform, 0f);
            wareFrame.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            wareFrame.type = Image.Type.Sliced;
            wareFrame.fillCenter = false;
            Image wareIcon = CreateIcon(wareRect, ware - 12f);
            TMP_Text wareGlyph = CreateText("Glyph", wareRect, center, Vector2.zero, new Vector2(ware, ware), 22f, TextAlignmentOptions.Center);
            wareGlyph.fontStyle = FontStyles.Bold;
            TMP_Text warePrice = CreateText("Price", wareRect, new Vector2(1f, 0f), new Vector2(-5f, 3f), new Vector2(60f, 16f), 13f, TextAlignmentOptions.BottomRight);
            warePrice.color = new Color(1f, 0.85f, 0.4f);
            warePrice.fontStyle = FontStyles.Bold;
            MerchantWareView wareView = wareRect.gameObject.AddComponent<MerchantWareView>();
            SerializedObject so = new SerializedObject(wareView);
            BattleEditorUtility.Set(so, "_background", wareBack);
            BattleEditorUtility.Set(so, "_frame", wareFrame);
            BattleEditorUtility.Set(so, "_icon", wareIcon);
            BattleEditorUtility.Set(so, "_glyph", wareGlyph);
            BattleEditorUtility.Set(so, "_priceText", warePrice);
            so.ApplyModifiedPropertiesWithoutUndo();
            wareRect.gameObject.SetActive(false);

            Image purse = CreateImage("Purse", page, Vector2.one, new Vector2(-20f, -96f), new Vector2(520f, 964f), Color.white);
            purse.sprite = panelSprite;
            Title(purse.rectTransform, "Your Purse", -34f, 23f);
            Image coinsIcon = CreateImage("CoinsIcon", purse.rectTransform, top, new Vector2(-84f, -84f), new Vector2(64f, 64f), Color.white);
            coinsIcon.preserveAspect = true;
            TMP_Text coins = CreateText("Coins", purse.rectTransform, top, new Vector2(54f, -98f), new Vector2(200f, 44f), 36f, TextAlignmentOptions.MidlineLeft);
            TMP_Text note = CreateText("Note", purse.rectTransform, top, new Vector2(0f, -160f), new Vector2(420f, 44f), 14f, TextAlignmentOptions.Top);
            note.text = "Purchases land in the stash. Coins are taken from the stash first, then from the kit bag.";
            note.color = s_textDim;
            CreateImage("Rule", purse.rectTransform, top, new Vector2(0f, -216f), new Vector2(420f, 2f), s_frame);
            Image detailsIcon = CreateImage("DetailsIcon", purse.rectTransform, top, new Vector2(0f, -236f), new Vector2(150f, 150f), Color.white);
            detailsIcon.preserveAspect = true;
            TMP_Text details = CreateText("Details", purse.rectTransform, top, new Vector2(0f, -400f), new Vector2(420f, 420f), 16f, TextAlignmentOptions.TopLeft);
            details.color = s_text;
            TMP_Text status = CreateText("Status", purse.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(440f, 50f), 17f, TextAlignmentOptions.Center);

            MerchantsView view = page.gameObject.AddComponent<MerchantsView>();
            so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_merchantButtonPrefab", merchantButton);
            BattleEditorUtility.Set(so, "_merchantsRoot", merchantsRoot);
            BattleEditorUtility.Set(so, "_warePrefab", wareView);
            BattleEditorUtility.Set(so, "_waresRoot", waresRoot);
            BattleEditorUtility.Set(so, "_filterButtons", filterButtons);
            BattleEditorUtility.Set(so, "_titleText", title);
            BattleEditorUtility.Set(so, "_descriptionText", description);
            BattleEditorUtility.Set(so, "_coinsText", coins);
            BattleEditorUtility.Set(so, "_coinsIcon", coinsIcon);
            BattleEditorUtility.Set(so, "_detailsIcon", detailsIcon);
            BattleEditorUtility.Set(so, "_detailsText", details);
            BattleEditorUtility.Set(so, "_statusText", status);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        /// Icon root sized as a pool cell with the tinted frame sprite inside; the frame takes the pointer events.
        private static AbilityIconView AbilityIcon(string name, Transform parent, Vector2 anchor, Vector2 position, string sprite, float size)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            RectTransform rect = CreateRect(name, parent, anchor, position, new Vector2(104f, 104f));
            rect.pivot = center;
            Image frame = CreateImage("Frame", rect, center, Vector2.zero, new Vector2(size, size), new Color(0.5f, 0.42f, 0.3f));
            frame.sprite = DungeonUiSpriteBuilder.Load(sprite);
            frame.raycastTarget = true;
            // A diamond leaves less room inside its plate than a square of the same size.
            Image icon = CreateIcon(rect, size * (sprite == "Diamond" ? 0.56f : 0.72f));
            TMP_Text glyph = CreateText("Glyph", rect, center, Vector2.zero, new Vector2(size, 36f), 25f, TextAlignmentOptions.Center);
            glyph.fontStyle = FontStyles.Bold;
            // The key of a skill hangs under its square; the level of a locked perk slot is written inside the empty diamond.
            TMP_Text badge = CreateText("Badge", rect, center, new Vector2(0f, sprite == "Diamond" ? -26f : -size * 0.5f - 9f), new Vector2(size, 18f), 13f, TextAlignmentOptions.Center);
            badge.fontStyle = FontStyles.Bold;
            badge.color = s_gold;

            AbilityIconView view = rect.gameObject.AddComponent<AbilityIconView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_frame", frame);
            BattleEditorUtility.Set(so, "_icon", icon);
            BattleEditorUtility.Set(so, "_glyph", glyph);
            BattleEditorUtility.Set(so, "_badge", badge);
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
            Image cursor = CreateImage("Cursor", panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 14f), s_gold);
            cursor.sprite = dim.sprite;
            cursor.raycastTarget = false;

            RectTransform slot = CreateRect("Slot", panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 84f));
            Image slotBack = slot.gameObject.AddComponent<Image>();
            slotBack.color = new Color(0.08f, 0.07f, 0.06f, 0.9f);
            slotBack.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            slotBack.type = Image.Type.Sliced;
            TMP_Text glyph = CreateText("Glyph", slot, new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(100f, 34f), 24f, TextAlignmentOptions.Center);
            glyph.rectTransform.pivot = new Vector2(0.5f, 1f);
            glyph.fontStyle = FontStyles.Bold;
            CreateIcon(slot, 38f).rectTransform.anchoredPosition = new Vector2(0f, 20f);
            TMP_Text name = CreateText("Name", slot, new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(100f, 44f), 12f, TextAlignmentOptions.Center);
            name.rectTransform.pivot = new Vector2(0.5f, 0f);
            slot.gameObject.SetActive(false);

            SpellWheelView view = panel.gameObject.AddComponent<SpellWheelView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_slotPrefab", slot);
            BattleEditorUtility.Set(so, "_slotsRoot", slotsRoot);
            BattleEditorUtility.Set(so, "_titleText", title);
            BattleEditorUtility.Set(so, "_centerText", centerText);
            BattleEditorUtility.Set(so, "_cursorMark", cursor.rectTransform);
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
            RectTransform panel = CreateRect("DevPanel", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(240f, 20f), new Vector2(680f, 224f));
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
            BattleEditorUtility.Set(so, "_lobbyButton", DevButton(panel, 3, 1, "To the lobby"));
            BattleEditorUtility.Set(so, "_debugButton", DevButton(panel, 0, 2, "Hitboxes"));
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

        /// Full floor map on M: parchment with the names of the modules, the player's arrow on top.
        private static MapView BuildMap(Transform root, Inputs inputs)
        {
            const float size = 760f;
            Vector2 center = new(0.5f, 0.5f);
            int grid = Mathf.Max(DungeonMapBuilder.FloorGrids);
            RectTransform panel = CreateRect("Map", root, center, Vector2.zero, new Vector2(size + 40f, size + 90f));
            CreateImage("Back", panel, center, Vector2.zero, Vector2.zero, s_panel).rectTransform.StretchFill();
            TMP_Text title = CreateText("Title", panel, new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(size, 34f), 24f, TextAlignmentOptions.Center);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.color = s_gold;
            CreateImage("Frame", panel, center, new Vector2(0f, -15f), new Vector2(size + 8f, size + 8f), s_frame);
            RawImage mapImage = CreateRect("Floor", panel, center, new Vector2(0f, -15f), new Vector2(size, size)).gameObject.AddComponent<RawImage>();
            mapImage.color = new Color(0.9f, 0.85f, 0.7f);
            mapImage.raycastTarget = false;
            TMP_Text[] labels = new TMP_Text[grid * grid];

            for (int i = 0; i < labels.Length; i++)
            {
                // The view lays the labels out over the modules of the floor it shows.
                labels[i] = CreateText("Module" + i, mapImage.transform, center, Vector2.zero, new Vector2(size / grid - 8f, 40f), 14f, TextAlignmentOptions.Center);
                labels[i].color = new Color(0.25f, 0.16f, 0.08f);
                labels[i].fontStyle = FontStyles.Bold;
            }

            Image arrow = CreateImage("Arrow", mapImage.transform, center, Vector2.zero, new Vector2(18f, 18f), new Color(0.95f, 0.75f, 0.2f));
            arrow.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            CreateImage("Tip", arrow.transform, center, new Vector2(0f, 12f), new Vector2(5f, 12f), new Color(0.95f, 0.75f, 0.2f));
            TMP_Text hint = CreateText("Hint", panel, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(size, 20f), 13f, TextAlignmentOptions.Center);
            hint.rectTransform.pivot = new Vector2(0.5f, 0f);
            hint.color = s_textDim;
            hint.text = "M / Esc close";

            MapView view = panel.gameObject.AddComponent<MapView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_context", inputs.Context);
            BattleEditorUtility.Set(so, "_map", mapImage);
            BattleEditorUtility.Set(so, "_arrow", arrow.rectTransform);
            BattleEditorUtility.Set(so, "_title", title);
            BattleEditorUtility.Set(so, "_moduleLabels", labels);
            BattleEditorUtility.Set(so, "_floorMaps", inputs.FloorMaps);
            BattleEditorUtility.Set(so, "_floorModuleNames", inputs.ModuleNames);
            BattleEditorUtility.Set(so, "_worldSize", DungeonMapBuilder.WorldSize);
            BattleEditorUtility.Set(so, "_floorSizes", DungeonMapBuilder.FloorSizes);
            BattleEditorUtility.Set(so, "_floorGrids", DungeonMapBuilder.FloorGrids);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
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

        /// Centered sprite holder that stays hidden until a view gives it a sprite.
        private static Image CreateIcon(Transform parent, float size)
        {
            Image icon = CreateImage("Icon", parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size), Color.white);
            icon.preserveAspect = true;
            icon.enabled = false;

            return icon;
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
