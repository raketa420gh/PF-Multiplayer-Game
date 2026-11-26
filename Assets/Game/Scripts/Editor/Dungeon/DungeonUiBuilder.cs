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
    /// inventory with 3D doll, result and loading screens. Hub scene: the HUD and the pages its stations open (board, skills, kit and stash, merchants).
    internal static class DungeonUiBuilder
    {
        public const string PreviewLayer = "Preview";
        private const float Cell = 44f;
        /// The equipment sheet is laid out in the pixels of the Dark and Darker reference (607 x 560), scaled to fit 600 x 490.
        private const float EquipScale = 0.83f;
        private const float EquipWidth = 607f;
        private const float EquipTop = 14f;
        private const string HelpText =
            "<b>Controls</b>\n" +
            "WASD move · Shift walk (quiet) · Space jump · Ctrl/C crouch (duck under swings)\n" +
            "LMB attack / draw · RMB block, or hold and release to cast a readied spell · 1 / 2 weapon sets · X put the weapon away · Tab inventory\n" +
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
            PauseMenuView pause = BuildPause(root);
            LoadingView loading = BuildLoading(root, inputs.Title);
            DungeonTestView test = BattleEditorUtility.CreateChild("DungeonTest", root).AddComponent<DungeonTestView>();
            BattleEditorUtility.Set(test, "_context", inputs.Context);

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
            BattleEditorUtility.Set(so, "_pause", pause);
            BattleEditorUtility.Set(so, "_test", test);
            so.ApplyModifiedPropertiesWithoutUndo();

            test.gameObject.SetActive(false);
            pause.gameObject.SetActive(false);
            hud.gameObject.SetActive(false);
            dungeonInventory.gameObject.SetActive(false);
            result.gameObject.SetActive(false);
            help.gameObject.SetActive(false);
            wheel.gameObject.SetActive(false);
            map.gameObject.SetActive(false);
            BattleEditorUtility.SetLayerRecursively(canvasObject, LayerMask.NameToLayer("UI"));

            return canvasObject;
        }

        /// UI of the hub scene: the gameplay HUD over the settlement, the pages its stations open, the spell wheels and the menu,
        /// all under a loading screen that lifts when the body stands in the hub.
        public static GameObject BuildHubScene(Inputs inputs)
        {
            GameObject canvasObject = BuildCanvas(inputs, out Canvas canvas);
            Transform root = canvasObject.transform;
            DungeonHudView hud = BuildHud(root, inputs);

            // No run and no map here: neither a timer nor a minimap.
            foreach (string name in new[] { "Minimap", "Timer", "Swarm", "Floor" })
                hud.transform.Find(name).gameObject.SetActive(false);

            TMP_Text prompt = CreateText("StationPrompt", root, new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(700f, 30f), 20f, TextAlignmentOptions.Center);
            prompt.color = s_text;
            TMP_Text notice = CreateText("Notice", root, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(900f, 30f), 18f, TextAlignmentOptions.Center);
            notice.color = new Color(1f, 0.78f, 0.45f);
            HubPagesView pages = BuildHubPages(root, canvas, inputs, BuildItemPrefab(), BuildCellPrefab());
            HelpView help = BuildHelp(root);
            SpellWheelView wheel = BuildWheel(root);
            PauseMenuView pause = BuildPause(root);
            LoadingView loading = BuildLoading(root, inputs.Title);

            SerializedObject so = new SerializedObject(canvasObject.AddComponent<HubUiRoot>());
            BattleEditorUtility.Set(so, "_context", inputs.Context);
            BattleEditorUtility.Set(so, "_loading", loading);
            BattleEditorUtility.Set(so, "_hud", hud);
            BattleEditorUtility.Set(so, "_pages", pages);
            BattleEditorUtility.Set(so, "_help", help);
            BattleEditorUtility.Set(so, "_wheel", wheel);
            BattleEditorUtility.Set(so, "_pause", pause);
            BattleEditorUtility.Set(so, "_promptText", prompt);
            BattleEditorUtility.Set(so, "_noticeText", notice);
            so.FindProperty("_stationMask").intValue = 1 | (1 << LayerMask.NameToLayer(HubBlockoutBuilder.StationLayer));
            so.ApplyModifiedPropertiesWithoutUndo();

            hud.gameObject.SetActive(false);
            pages.gameObject.SetActive(false);
            help.gameObject.SetActive(false);
            wheel.gameObject.SetActive(false);
            pause.gameObject.SetActive(false);
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
            BuildTavern(camera);
            Image vignette = CreateImage("Vignette", root, center, Vector2.zero, Vector2.zero, Color.white);
            vignette.sprite = DungeonUiSpriteBuilder.Load("Vignette");
            Stretch(vignette.rectTransform, 0f);

            RectTransform selectPage = CharacterPage(root, "Select", "Select Character", "Enter Hub", "Delete",
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

            // Creation is UI only: an opaque page over the tavern render (switched off meanwhile), the classes on the left,
            // the class hexagram in the middle with its description under it, the nickname above Create.
            RectTransform createPage = CharacterPage(root, "Create", "Create Character", "Create", "Back",
                out TMP_Text className, out Button create, out Button back);
            Image createBack = CreateImage("Background", createPage, center, Vector2.zero, Vector2.zero, new Color(0.035f, 0.03f, 0.027f, 1f));
            Stretch(createBack.rectTransform, 0f);
            createBack.transform.SetAsFirstSibling();
            Button[] classButtons = new Button[classes.Length];

            for (int i = 0; i < classes.Length; i++)
                classButtons[i] = CharacterCard(createPage, "Class" + classes[i].DisplayName, i, classes[i].DisplayName);

            Vector2 top = new Vector2(0.5f, 1f);
            Image sheet = CreateImage("Class", createPage, top, new Vector2(0f, -236f), new Vector2(560f, 560f), Color.white);
            sheet.sprite = DungeonUiSpriteBuilder.Load("Panel");
            StatsView classStats = BuildStats(sheet.rectTransform, new Vector2(0f, -24f), 560f, 115f, true, createPage, out RectTransform statsTooltip, false);
            statsTooltip.SetAsLastSibling();
            CreateImage("Rule", sheet.rectTransform, top, new Vector2(0f, -386f), new Vector2(420f, 2f), s_frame);
            TMP_Text description = CreateText("Description", sheet.rectTransform, top, new Vector2(0f, -404f), new Vector2(480f, 140f), 19f, TextAlignmentOptions.Top);
            description.color = s_text;

            TMP_InputField nickname = CreateInput("Nickname", createPage, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(360f, 56f), "Nickname");
            nickname.textComponent.fontSize = 24f;
            nickname.targetGraphic.color = s_panelLight;

            CharacterCreateView createView = createPage.gameObject.AddComponent<CharacterCreateView>();
            so = new SerializedObject(createView);
            BattleEditorUtility.Set(so, "_classes", classes);
            BattleEditorUtility.Set(so, "_classButtons", classButtons);
            BattleEditorUtility.Set(so, "_stats", classStats);
            BattleEditorUtility.Set(so, "_nameText", className);
            BattleEditorUtility.Set(so, "_descriptionText", description);
            BattleEditorUtility.Set(so, "_nicknameInput", nickname);
            BattleEditorUtility.Set(so, "_createButton", create);
            BattleEditorUtility.Set(so, "_backButton", back);
            so.ApplyModifiedPropertiesWithoutUndo();

            LoadingView loading = BuildLoading(root, SceneTravel.CharacterSelectTitle);
            so = new SerializedObject(canvasObject.AddComponent<CharacterSelectUiRoot>());
            BattleEditorUtility.Set(so, "_select", select);
            BattleEditorUtility.Set(so, "_create", createView);
            BattleEditorUtility.Set(so, "_loading", loading);
            BattleEditorUtility.Set(so, "_backdrop", backdrop.gameObject);
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
            RectTransform scales = CreateRect("Scales", panel, new Vector2(1f, 0f), new Vector2(-90f, 90f), new Vector2(120f, 120f));
            scales.pivot = center;
            RectTransform beam = BuildScales(scales, out RectTransform[] pans);
            TMP_Text tip = CreateText("Tip", panel, new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(1200f, 30f), 16f, TextAlignmentOptions.Center);
            tip.text = "Containers open at once, but their loot has to be searched. Perception makes the search faster.";
            tip.color = s_textDim;

            LoadingView view = panel.gameObject.AddComponent<LoadingView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_sceneTitle", title);
            BattleEditorUtility.Set(so, "_titleText", titleText);
            BattleEditorUtility.Set(so, "_statusText", statusText);
            BattleEditorUtility.Set(so, "_progressFill", fill);
            BattleEditorUtility.Set(so, "_beam", beam);
            BattleEditorUtility.Set(so, "_pans", pans);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        /// Loading icon: scales with a bone on the left pan and a pinch of coins on the right, laid out in the 120 px box of the
        /// UI screens page from its top-left corner; LoadingView tips the beam and hangs the pans from its ends.
        private static RectTransform BuildScales(RectTransform root, out RectTransform[] pans)
        {
            Sprite knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Color bone = new Color(0.91f, 0.86f, 0.75f);

            Image Part(string name, Transform parent, float x, float y, float width, float height, Color color, Sprite sprite = null)
            {
                Image image = CreateImage(name, parent, new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(width, height), color);
                image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                image.sprite = sprite;

                return image;
            }

            RectTransform Group(string name, float x, float y)
            {
                RectTransform group = CreateRect(name, root, new Vector2(0f, 1f), new Vector2(x, -y), Vector2.zero);
                group.pivot = new Vector2(0.5f, 0.5f);

                return group;
            }

            Part("Base", root, 60f, 102f, 32f, 8f, s_frame);
            Part("Post", root, 60f, 64f, 3f, 72f, s_frame);
            Part("Top", root, 60f, 24f, 8f, 8f, s_gold, knob);
            RectTransform beam = Group("Beam", 60f, 30f);
            Part("Bar", beam, 0f, 0f, 80f, 3f, s_gold);
            Part("Hub", beam, 0f, 0f, 6f, 6f, s_gold, knob);
            pans = new RectTransform[2];

            for (int side = 0; side < 2; side++)
            {
                RectTransform pan = pans[side] = Group(side == 0 ? "PanLeft" : "PanRight", side == 0 ? 20f : 100f, 30f);

                // Strings from the beam end to the bowl rim, 13 px aside and 33 px down.
                for (int k = -1; k <= 1; k += 2)
                {
                    Image cord = Part("Cord", pan, 0f, 0f, 1.2f, 35.5f, s_frame);
                    cord.rectTransform.pivot = new Vector2(0.5f, 1f);
                    cord.rectTransform.localEulerAngles = new Vector3(0f, 0f, k * 21.5f);
                }

                if (side == 0)
                {
                    Part("Bone", pan, 0f, 28.7f, 16f, 3.4f, bone);

                    foreach (Vector2 end in new[] { new Vector2(-8f, 26.6f), new Vector2(-8f, 30.6f), new Vector2(8f, 26.6f), new Vector2(8f, 30.6f) })
                        Part("Knuckle", pan, end.x, end.y, 4.8f, 4.8f, bone, knob);
                }
                else
                    foreach (Vector2 coin in new[] { new Vector2(-5f, 31f), new Vector2(5f, 31f), new Vector2(0f, 31f), new Vector2(-2.5f, 27.5f), new Vector2(2.5f, 27.5f), new Vector2(0f, 24f) })
                    {
                        Part("Rim", pan, coin.x, coin.y, 10.8f, 5.8f, new Color(0.54f, 0.42f, 0.14f), knob);
                        Part("Coin", pan, coin.x, coin.y, 9.2f, 4.2f, new Color(0.94f, 0.78f, 0.38f), knob);
                    }

                Image bowl = Part("Bowl", pan, 0f, 33f, 30f, 13f, s_gold, knob);
                bowl.type = Image.Type.Filled;
                bowl.fillMethod = Image.FillMethod.Vertical;
                bowl.fillOrigin = (int)Image.OriginVertical.Bottom;
                bowl.fillAmount = 0.5f;
            }

            return beam;
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

        /// Two bars beside the crosshair dot, shown while an aimed item or ranged weapon is in hand (AimCrosshairView). Public: a built
        /// scene gets them without a rebuild.
        public static AimCrosshairView BuildAimCrosshair(Transform hud)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            Color color = new Color(1f, 1f, 1f, 0.75f);
            RectTransform root = CreateRect("AimCrosshair", hud, center, Vector2.zero, Vector2.zero);
            RectTransform left = CreateImage("Left", root, center, Vector2.zero, new Vector2(12f, 2f), color).rectTransform;
            RectTransform right = CreateImage("Right", root, center, Vector2.zero, new Vector2(12f, 2f), color).rectTransform;
            left.pivot = new Vector2(1f, 0.5f);
            right.pivot = new Vector2(0f, 0.5f);
            AimCrosshairView view = root.gameObject.AddComponent<AimCrosshairView>();
            BattleEditorUtility.Set(view, "_left", left);
            BattleEditorUtility.Set(view, "_right", right);
            root.gameObject.SetActive(false);

            return view;
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
            AimCrosshairView aimCrosshair = BuildAimCrosshair(hud);
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

            // Health bar with skill boxes at the sides, name above, the belt groups beside the skill boxes, effect icons over it; a red glow flashes under it on taken damage.
            Image healthFlash = CreateImage("HealthFlash", hud, bottom, new Vector2(0f, 40f), new Vector2(540f, 84f), new Color(0.9f, 0.05f, 0.03f, 0f));
            healthFlash.sprite = DungeonUiSpriteBuilder.Load("SoftBar");
            healthFlash.raycastTarget = false;
            CreateImage("HealthFrame", hud, bottom, new Vector2(0f, 40f), new Vector2(470f, 40f), s_frame);
            CreateImage("HealthBack", hud, bottom, new Vector2(0f, 40f), new Vector2(462f, 32f), new Color(0.06f, 0.03f, 0.02f, 0.95f));
            Image recoverableFill = CreateImage("RecoverableFill", hud, bottom, new Vector2(0f, 40f), new Vector2(452f, 22f), new Color(0.75f, 0.1f, 0.08f, 0.35f));
            MakeFilled(recoverableFill);
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
            TMP_Text readied = CreateText("Readied", hud, bottom, new Vector2(0f, 156f), new Vector2(800f, 28f), 15f, TextAlignmentOptions.Center);

            // Two belt groups of three: key 3 cycles the group left of Q, key 4 the one right of E.
            List<(GameObject, Image, TMP_Text, TMP_Text, Image, Image)> belt = new();

            for (int i = 0; i < 6; i++)
                belt.Add(BeltSlot(hud, bottom, new Vector2((i < 3 ? -440f : 336f) + i % 3 * 52f, 50f), (3 + i / 3).ToString()));

            // Effect icons in a centred row over the health bar; the class resource and the resting line above the readied spell.
            RectTransform effectRow = CreateRect("Effects", hud, bottom, new Vector2(0f, 106f), new Vector2(StatusEffectComponent.Capacity * 46f, 42f));
            HorizontalLayoutGroup effectLayout = effectRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            effectLayout.spacing = 4f;
            effectLayout.childAlignment = TextAnchor.MiddleCenter;
            effectLayout.childControlWidth = effectLayout.childControlHeight = false;
            effectLayout.childForceExpandWidth = effectLayout.childForceExpandHeight = false;
            List<(GameObject, Image, Image, TMP_Text, TMP_Text)> effectViews = new();

            for (int i = 0; i < StatusEffectComponent.Capacity; i++)
            {
                RectTransform cell = CreateRect("Effect" + i, effectRow, center, Vector2.zero, new Vector2(42f, 42f));
                Image cellFrame = CreateImage("Frame", cell, center, Vector2.zero, new Vector2(42f, 42f), s_frame);
                CreateImage("Back", cell, center, Vector2.zero, new Vector2(38f, 38f), s_panelLight);
                Image cellIcon = CreateIcon(cell, 36f);
                TMP_Text cellGlyph = CreateText("Glyph", cell, center, Vector2.zero, new Vector2(38f, 38f), 14f, TextAlignmentOptions.Center);
                cellGlyph.fontStyle = FontStyles.Bold;
                TMP_Text cellTime = CreateText("Time", cell, bottomRight, new Vector2(-2f, 1f), new Vector2(30f, 14f), 11f, TextAlignmentOptions.BottomRight);
                effectViews.Add((cell.gameObject, cellFrame, cellIcon, cellGlyph, cellTime));
                cell.gameObject.SetActive(false);
            }

            TMP_Text effects = CreateText("Status", hud, bottom, new Vector2(0f, 186f), new Vector2(800f, 22f), 13f, TextAlignmentOptions.Bottom);
            effects.color = new Color(0.8f, 0.95f, 1f);

            // Weapon sets 1 and 2 bottom right, centred over the weapon name: a key square with the tall box of the set's weapons above.
            RectTransform weaponRow = CreateRect("WeaponRow", hud, bottomRight, new Vector2(-355f, 56f), new Vector2(90f, 40f));
            List<(TMP_Text, Image, Image, Image, Image)> weaponSets = new();

            for (int i = 0; i < 2; i++)
            {
                RectTransform slot = CreateRect("Slot" + (i + 1), weaponRow, new Vector2(0f, 0.5f), new Vector2(i * 62f, 0f), new Vector2(28f, 28f));
                slot.pivot = new Vector2(0f, 0.5f);
                Image keyFrame = CreateImage("Frame", slot, center, Vector2.zero, new Vector2(32f, 32f), s_frame);
                CreateImage("Back", slot, center, Vector2.zero, new Vector2(28f, 28f), s_panelLight);
                TMP_Text key = CreateText("Key", slot, center, Vector2.zero, new Vector2(28f, 28f), 14f, TextAlignmentOptions.Center);
                key.text = (i + 1).ToString();
                RectTransform box = CreateRect("Weapons", slot, center, new Vector2(0f, 70f), new Vector2(54f, 96f));
                Image boxFrame = CreateImage("Frame", box, center, Vector2.zero, new Vector2(56f, 98f), s_frame);
                CreateImage("Back", box, center, Vector2.zero, new Vector2(52f, 94f), s_panelLight);
                Image main = CreateImage("Main", box, center, Vector2.zero, new Vector2(50f, 90f), Color.white);
                main.preserveAspect = true;
                main.enabled = false;
                Image offBack = CreateImage("OffBack", box, bottomRight, new Vector2(-2f, 2f), new Vector2(24f, 24f), s_frame);
                Image off = CreateImage("Off", offBack.rectTransform, center, Vector2.zero, new Vector2(22f, 22f), Color.white);
                off.preserveAspect = true;
                off.enabled = false;
                weaponSets.Add((key, keyFrame, boxFrame, main, off));

                if (i == 0)
                    CreateImage("Line", weaponRow, new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(30f, 2f), new Color(0.5f, 0.45f, 0.35f, 0.8f)).rectTransform.pivot = new Vector2(0f, 0.5f);
            }

            CreateImage("WeaponBack", hud, bottomRight, new Vector2(-270f, 22f), new Vector2(260f, 26f), new Color(0.05f, 0.04f, 0.03f, 0.85f));
            TMP_Text weapon = CreateText("Weapon", hud, bottomRight, new Vector2(-270f, 22f), new Vector2(260f, 26f), 16f, TextAlignmentOptions.Center);

            // Minimap, bottom right, with timer above and module name below.
            RectTransform minimapRoot = CreateRect("Minimap", hud, bottomRight, new Vector2(-24f, 50f), new Vector2(230f, 230f));
            minimapRoot.pivot = new Vector2(1f, 0f);
            CreateImage("Frame", minimapRoot, center, Vector2.zero, new Vector2(238f, 238f), s_frame);
            RawImage mapImage = CreateRect("Map", minimapRoot, center, Vector2.zero, new Vector2(230f, 230f)).gameObject.AddComponent<RawImage>();
            mapImage.color = new Color(0.9f, 0.85f, 0.7f);
            mapImage.raycastTarget = false;
            minimapRoot.gameObject.AddComponent<RectMask2D>();
            SwarmZoneGraphic minimapSwarm = CreateSwarmZone(mapImage.transform);
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
            BattleEditorUtility.Set(mso, "_swarm", minimapSwarm);
            BattleEditorUtility.Set(mso, "_floorMaps", inputs.FloorMaps);
            BattleEditorUtility.Set(mso, "_floorModuleNames", inputs.ModuleNames);
            BattleEditorUtility.Set(mso, "_floorSizes", DungeonMapBuilder.FloorSizes);
            BattleEditorUtility.Set(mso, "_windowSizes", DungeonMapBuilder.WindowSizes);
            BattleEditorUtility.Set(mso, "_floorGrids", DungeonMapBuilder.FloorGrids);
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
            BattleEditorUtility.Set(so, "_recoverableFill", recoverableFill);
            BattleEditorUtility.Set(so, "_healthText", healthText);
            BattleEditorUtility.Set(so, "_healthFlash", healthFlash);
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
            BattleEditorUtility.Set(so, "_aimCrosshair", aimCrosshair);
            BattleEditorUtility.Set(so, "_killText", kills);
            BattleEditorUtility.Set(so, "_readiedText", readied);
            BattleEditorUtility.Set(so, "_bossFill", bossFill);
            BattleEditorUtility.Set(so, "_bossRoot", bossRoot.gameObject);
            BattleEditorUtility.Set(so, "_bossText", bossText);
            BattleEditorUtility.Set(so, "_effectIcons", EffectIcons());
            SerializedProperty setsProperty = so.FindProperty("_weaponSets");
            setsProperty.arraySize = weaponSets.Count;

            for (int i = 0; i < weaponSets.Count; i++)
            {
                SerializedProperty element = setsProperty.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Key").objectReferenceValue = weaponSets[i].Item1;
                element.FindPropertyRelative("KeyFrame").objectReferenceValue = weaponSets[i].Item2;
                element.FindPropertyRelative("BoxFrame").objectReferenceValue = weaponSets[i].Item3;
                element.FindPropertyRelative("Main").objectReferenceValue = weaponSets[i].Item4;
                element.FindPropertyRelative("Off").objectReferenceValue = weaponSets[i].Item5;
            }

            SerializedProperty cellsProperty = so.FindProperty("_effectCells");
            cellsProperty.arraySize = effectViews.Count;

            for (int i = 0; i < effectViews.Count; i++)
            {
                SerializedProperty element = cellsProperty.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Root").objectReferenceValue = effectViews[i].Item1;
                element.FindPropertyRelative("Frame").objectReferenceValue = effectViews[i].Item2;
                element.FindPropertyRelative("Icon").objectReferenceValue = effectViews[i].Item3;
                element.FindPropertyRelative("Glyph").objectReferenceValue = effectViews[i].Item4;
                element.FindPropertyRelative("Time").objectReferenceValue = effectViews[i].Item5;
            }

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
                element.FindPropertyRelative("Fuel").objectReferenceValue = belt[i].Item6;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        /// Icon of every effect kind: that of the first ability (by asset path) that puts it on.
        private static Sprite[] EffectIcons()
        {
            Sprite[] icons = new Sprite[System.Enum.GetValues(typeof(StatusEffectKind)).Length];
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(AbilityConfig));
            System.Array.Sort(guids, (a, b) => string.CompareOrdinal(AssetDatabase.GUIDToAssetPath(a), AssetDatabase.GUIDToAssetPath(b)));

            foreach (string guid in guids)
            {
                AbilityConfig ability = AssetDatabase.LoadAssetAtPath<AbilityConfig>(AssetDatabase.GUIDToAssetPath(guid));

                foreach (StatusEffectKind kind in new[] { ability.Effect, ability.SecondEffect })
                    if (kind != StatusEffectKind.None && (int)kind < icons.Length && icons[(int)kind] == null)
                        icons[(int)kind] = ability.Icon;
            }

            // Kinds no ability puts on (potions, perks, monsters) take the nearest ability picture.
            (StatusEffectKind kind, string icon)[] fallbacks =
            {
                (StatusEffectKind.HealOverTime, "HealingBrew"), (StatusEffectKind.Protection, "VowofProtection"), (StatusEffectKind.Invisible, "Invisibility"),
                (StatusEffectKind.Fortify, "FortifyingMix"), (StatusEffectKind.Rupture, "SavageBlow"), (StatusEffectKind.Taunt, "OverHere!"),
                (StatusEffectKind.ArmorPenetration, "Armor-Piercing"), (StatusEffectKind.QuickCast, "QuickChant"), (StatusEffectKind.Silence, "Rebuke"),
                (StatusEffectKind.Vulnerable, "Brittleness"), (StatusEffectKind.Weakened, "Weakness"), (StatusEffectKind.Marked, "HuntersMark"),
                (StatusEffectKind.Spirit, "HealthySpirit"), (StatusEffectKind.Knowledge, "Well-Read"), (StatusEffectKind.Armor, "IronSkin"),
                (StatusEffectKind.DamageBonus, "Fury"), (StatusEffectKind.UseSpeed, "QuickHands"), (StatusEffectKind.Retaliation, "Counterblow"),
                (StatusEffectKind.Onslaught, "Onslaught"), (StatusEffectKind.Wrath, "RighteousWrath"), (StatusEffectKind.Festering, "Contagion"),
                (StatusEffectKind.WrathSpent, "RageWithin"), (StatusEffectKind.Rotting, "Rot")
            };

            foreach ((StatusEffectKind kind, string icon) in fallbacks)
                if (icons[(int)kind] == null)
                    icons[(int)kind] = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Game/Textures/Icons/Abilities/{icon}.png");

            return icons;
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

        private static (GameObject, Image, TMP_Text, TMP_Text, Image, Image) BeltSlot(RectTransform parent, Vector2 anchor, Vector2 position, string key)
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
            Image fuelBack = CreateImage("FuelBack", slot, new Vector2(0.5f, 0f), new Vector2(0f, 5f), new Vector2(36f, 5f), new Color(0.08f, 0.06f, 0.04f, 0.9f));
            Image fuel = CreateImage("Fuel", fuelBack.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(34f, 3f), new Color(1f, 0.6f, 0.2f));
            MakeFilled(fuel);
            fuelBack.gameObject.SetActive(false);

            return (slot.gameObject, icon, glyph, count, frame, fuel);
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

            // Left: character doll in an arch frame plus attributes. In the lobby the attributes take the sheet of the Skills page
            // and the doll tops the center column, over the equipment.
            RectTransform dollPanel = fullscreen
                ? CreateRect("Doll", panel, new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(460f, 980f))
                : CreateRect("Doll", panel, new Vector2(0f, 1f), new Vector2(565f, -36f), new Vector2(620f, 464f));
            CreateImage("Back", dollPanel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, s_panel).rectTransform.StretchFill();
            CreateImage("ArchFrame", dollPanel, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(360f, 400f), s_frame).rectTransform.pivot = new Vector2(0.5f, 1f);
            RawImage previewImage = CreateRect("Preview", dollPanel, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(352f, 392f)).gameObject.AddComponent<RawImage>();
            previewImage.rectTransform.pivot = new Vector2(0.5f, 1f);
            previewImage.color = Color.white;
            preview = BuildPreview(previewImage, inputs, previewIndex, 352, 392, out _);
            TMP_Text titleText = CreateText("Title", dollPanel, new Vector2(0.5f, 1f), new Vector2(0f, -428f), new Vector2(400f, 30f), 24f, TextAlignmentOptions.Center);
            titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
            titleText.color = s_gold;
            RectTransform statsTooltip;
            StatsView stats;

            if (fullscreen)
                stats = BuildStats(dollPanel, new Vector2(0f, -462f), 460f, 105f, true, panel, out statsTooltip);
            else
            {
                Image sheet = CreateImage("Stats", panel, new Vector2(0f, 1f), new Vector2(20f, -36f), new Vector2(530f, 964f), Color.white);
                sheet.sprite = DungeonUiSpriteBuilder.Load("Panel");
                stats = BuildStats(sheet.rectTransform, new Vector2(0f, -34f), 530f, 130f, false, panel, out statsTooltip);
            }

            // Center: equipment sunburst and backpack.
            RectTransform equipPanel = CreateRect("Equipment", panel, new Vector2(0f, 1f), new Vector2(530f, -40f), new Vector2(640f, 500f));
            equipPanel.pivot = new Vector2(0f, 1f);
            CreateImage("Back", equipPanel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, s_panel).rectTransform.StretchFill();
            // The sheet of Dark and Darker in its pixels (EquipScale): weapon sets 1 and 2 in the top corners, each a pair of hands
            // side by side (a two-handed weapon spans both); the bags over the utility columns 3 and 4; rings beside the legs.
            foreach ((string number, float x, float y) in new[] { ("1", 0f, 0f), ("2", 453f, 0f), ("3", 0f, 310f), ("4", 529f, 310f) })
                Header(equipPanel, number, x, y, number is "1" or "2" ? 154f : 78f);

            List<EquipSlotView> slots = new()
            {
                Slot(equipPanel, EquipSlot.Weapon1Main, "", 0f, 24f, 76f, 186f, itemPrefab),
                Slot(equipPanel, EquipSlot.Weapon1Off, "", 78f, 24f, 76f, 186f, itemPrefab),
                Slot(equipPanel, EquipSlot.Weapon2Main, "", 453f, 24f, 76f, 186f, itemPrefab),
                Slot(equipPanel, EquipSlot.Weapon2Off, "", 531f, 24f, 76f, 186f, itemPrefab),
                Slot(equipPanel, EquipSlot.Head, "Head", 255f, 32f, 98f, 96f, itemPrefab),
                Slot(equipPanel, EquipSlot.Necklace, "Neck", 365f, 76f, 52f, 52f, itemPrefab),
                Slot(equipPanel, EquipSlot.Chest, "Chest", 238f, 144f, 132f, 176f, itemPrefab),
                Slot(equipPanel, EquipSlot.Back, "Cloak", 384f, 225f, 96f, 96f, itemPrefab),
                Slot(equipPanel, EquipSlot.Pouch1, "Bag", 0f, 226f, 78f, 78f, itemPrefab),
                Slot(equipPanel, EquipSlot.Pouch2, "Bag", 529f, 226f, 78f, 78f, itemPrefab),
                Slot(equipPanel, EquipSlot.Ring1, "Ring", 172f, 338f, 52f, 52f, itemPrefab),
                Slot(equipPanel, EquipSlot.Legs, "Legs", 238f, 336f, 132f, 180f, itemPrefab),
                Slot(equipPanel, EquipSlot.Ring2, "Ring", 384f, 338f, 52f, 52f, itemPrefab),
                Slot(equipPanel, EquipSlot.Hands, "Hands", 126f, 404f, 97f, 96f, itemPrefab),
                Slot(equipPanel, EquipSlot.Feet, "Feet", 384f, 404f, 97f, 96f, itemPrefab),
                Slot(equipPanel, EquipSlot.Utility1, "", 0f, 334f, 78f, 74f, itemPrefab),
                Slot(equipPanel, EquipSlot.Utility2, "", 0f, 410f, 78f, 74f, itemPrefab),
                Slot(equipPanel, EquipSlot.Utility3, "", 0f, 486f, 78f, 74f, itemPrefab),
                Slot(equipPanel, EquipSlot.Utility4, "", 529f, 334f, 78f, 74f, itemPrefab),
                Slot(equipPanel, EquipSlot.Utility5, "", 529f, 410f, 78f, 74f, itemPrefab),
                Slot(equipPanel, EquipSlot.Utility6, "", 529f, 486f, 78f, 74f, itemPrefab)
            };

            // Five rows of the bag and a row of pouch cells under them.
            RectTransform bagPanel = CreateRect("BagPanel", panel, new Vector2(0f, 1f), new Vector2(530f, -560f), new Vector2(640f, 36f + Cell * 6f + 28f));
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

        /// A rect of the equipment sheet given in the reference's pixels from its top-left corner, centred across the panel.
        private static RectTransform EquipRect(string name, RectTransform parent, float x, float y, float width, float height)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(0.5f, 1f), new Vector2(x - EquipWidth * 0.5f, -y - EquipTop) * EquipScale,
                new Vector2(width, height) * EquipScale);
            rect.pivot = new Vector2(0f, 1f);

            return rect;
        }

        private static void Header(RectTransform parent, string number, float x, float y, float width)
        {
            RectTransform rect = EquipRect("Header" + number, parent, x, y, width, 20f);
            rect.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.13f, 0.1f, 0.95f);
            TMP_Text text = CreateText("Number", rect, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 14f, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, 0f);
            text.text = number;
            text.color = s_gold;
        }

        private static EquipSlotView Slot(RectTransform parent, EquipSlot slot, string label, float x, float y, float width, float height, ItemView itemPrefab)
        {
            RectTransform rect = EquipRect("Slot" + slot, parent, x, y, width, height);
            Image background = rect.gameObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.6f);
            CreateImage("Frame", rect, new Vector2(0.5f, 0.5f), Vector2.zero, rect.sizeDelta + new Vector2(4f, 4f), new Color(0.3f, 0.25f, 0.18f, 0.9f)).transform.SetAsFirstSibling();
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
            RectTransform rect = CreateRect(name, parent, new Vector2(0f, 1f), position, new Vector2(Cell * 10f, Cell * 5f));
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

        /// Pages of the hub stations over the whole screen: the board, the skills, the kit with the stash and the merchant counter
        /// under a bar with the level shield and the name of the station.
        private static HubPagesView BuildHubPages(Transform root, Canvas canvas, Inputs inputs, ItemView itemPrefab, Image cellPrefab)
        {
            RectTransform panel = CreateRect("Pages", root, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(panel, 0f);
            Image back = CreateImage("Back", panel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, s_lobbyBack);
            Stretch(back.rectTransform, 0f);

            HubBoardView board = BuildHubBoard(panel, inputs);
            SkillsView skills = BuildSkills(panel, inputs);
            InventoryView inventory = BuildInventory(panel, canvas, inputs, itemPrefab, cellPrefab, "Kit", false, 1, out CharacterPreviewView preview);
            LayoutLobbyStash(inventory);
            StashPagesView stashPages = BuildStashPages(inventory);

            MerchantsView merchants = BuildMerchants(panel);

            // Top bar: rank shield and the name of the station that opened the page.
            RectTransform bar = CreateRect("TopBar", panel, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 82f));
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = Vector2.one;
            CreateImage("Back", bar, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.94f)).rectTransform.StretchFill();
            RectTransform rule = CreateImage("Rule", bar, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 2f), s_frame).rectTransform;
            rule.anchorMin = Vector2.zero;
            rule.anchorMax = new Vector2(1f, 0f);

            TMP_Text title = CreateText("Title", bar, new Vector2(0f, 0.5f), new Vector2(150f, 0f), new Vector2(900f, 60f), 30f, TextAlignmentOptions.MidlineLeft);
            title.color = s_gold;
            TMP_Text close = CreateText("Close", bar, new Vector2(1f, 0.5f), new Vector2(-30f, 0f), new Vector2(300f, 60f), 18f, TextAlignmentOptions.MidlineRight);
            close.text = "[Esc] Close";
            close.color = s_textDim;

            Image shield = CreateImage("Rank", bar, new Vector2(0f, 1f), new Vector2(26f, 0f), new Vector2(88f, 110f), Color.white);
            shield.sprite = DungeonUiSpriteBuilder.Load("Shield");
            TMP_Text rank = CreateText("Text", shield.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(88f, 70f), 15f, TextAlignmentOptions.Center);
            rank.color = new Color(0.16f, 0.1f, 0.03f);
            rank.fontStyle = FontStyles.Bold;
            rank.lineSpacing = -30f;

            HubPagesView view = panel.gameObject.AddComponent<HubPagesView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_board", board);
            BattleEditorUtility.Set(so, "_skills", skills);
            BattleEditorUtility.Set(so, "_inventory", inventory);
            BattleEditorUtility.Set(so, "_inventoryPreview", preview);
            BattleEditorUtility.Set(so, "_titleText", title);
            BattleEditorUtility.Set(so, "_merchants", merchants);
            BattleEditorUtility.Set(so, "_stashPages", stashPages);
            BattleEditorUtility.Set(so, "_rankText", rank);
            BattleEditorUtility.Set(so, "_chrome", new[] { back.gameObject, bar.gameObject });
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        /// Kit page of the hub over the whole screen under the top bar: the attribute sheet where the Skills page has it, the doll
        /// with the equipment beside it and the backpack under both in the middle, the stash on the right edge.
        private static void LayoutLobbyStash(InventoryView inventory)
        {
            Vector2 top = new Vector2(0.5f, 1f);
            Vector2 topLeft = new Vector2(0f, 1f);
            Vector2 topRight = Vector2.one;
            RectTransform panel = (RectTransform)inventory.transform;
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.pivot = top;
            // Lowered by the top bar as a whole, so its centre (the drag ghost's origin) stays where it was.
            panel.offsetMin = new Vector2(0f, -60f);
            panel.offsetMax = new Vector2(0f, -60f);
            panel.Find("Hints").gameObject.SetActive(false);

            // The middle column spans from the sheet (20 + 530 + 20) to the stash (470 + 20 + 10 from the right edge).
            const float middleLeft = 35f - 425f;
            const float height = 490f;
            RectTransform doll = (RectTransform)panel.Find("Doll");
            Place(doll, top, topLeft, new Vector2(middleLeft, -36f), new Vector2(240f, height));
            Place((RectTransform)doll.Find("ArchFrame"), top, top, new Vector2(0f, -14f), new Vector2(224f, 420f));
            RawImage preview = doll.Find("Preview").GetComponent<RawImage>();
            Place(preview.rectTransform, top, top, new Vector2(0f, -18f), new Vector2(216f, 412f));
            ResizeTexture((RenderTexture)preview.texture, 216, 412);
            Place((RectTransform)doll.Find("Title"), top, top, new Vector2(0f, -442f), new Vector2(236f, 30f));

            RectTransform equipment = (RectTransform)panel.Find("Equipment");
            Place(equipment, top, topLeft, new Vector2(middleLeft + 250f, -36f), new Vector2(600f, height));

            RectTransform bagPanel = (RectTransform)panel.Find("BagPanel");
            Place(bagPanel, top, topLeft, new Vector2(middleLeft, -36f - height - 10f), new Vector2(850f, 1000f - 46f - height));
            bagPanel.Find("Bag").GetComponent<ItemGridView>().Rect.anchoredPosition = new Vector2(205f, -70f);
            TMP_Text hints = CreateText("Hints", bagPanel, new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(820f, 44f), 13f, TextAlignmentOptions.Center);
            hints.text = "[Drag] move   [R Click] equip / unequip   [Shift + L Click] to the stash and back   [Ctrl + Drag] split a stack\n" +
                "A belt pouch opens its cells under the backpack; it comes off only while they are empty";
            hints.color = s_textDim;

            RectTransform other = (RectTransform)panel.Find("OtherPanel");
            Place(other, topRight, topRight, new Vector2(-20f, -36f), new Vector2(470f, 964f));
            ((RectTransform)other.Find("Other")).anchoredPosition = new Vector2(15f, -50f);
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void ResizeTexture(RenderTexture texture, int width, int height)
        {
            texture.Release();
            texture.width = width;
            texture.height = height;
            EditorUtility.SetDirty(texture);
        }

        /// Stash page tabs under the stash grid and the sell counter at the bottom of the stash panel.
        private static StashPagesView BuildStashPages(InventoryView inventory)
        {
            Transform other = inventory.transform.Find("OtherPanel");
            Vector2 topLeft = new Vector2(0f, 1f);
            Button[] tabs = new Button[PlayerSessionComponent.StashPages];

            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i] = CreateButton("Page" + (i + 1), other, topLeft, new Vector2(15f + i * 150f, -504f), new Vector2(142f, 34f), $"Page {i + 1}");
                ((RectTransform)tabs[i].transform).pivot = topLeft;
                tabs[i].gameObject.AddComponent<ItemDropZone>();
            }

            TMP_Text coins = CreateText("Coins", other, new Vector2(1f, 1f), new Vector2(-15f, -504f), new Vector2(150f, 34f), 18f, TextAlignmentOptions.MidlineRight);
            coins.rectTransform.pivot = new Vector2(1f, 1f);
            coins.color = s_gold;

            Image counter = CreateImage("Sell", other, new Vector2(0.5f, 0f), new Vector2(0f, 15f), new Vector2(440f, 140f), new Color(0.12f, 0.09f, 0.05f, 0.95f));
            counter.raycastTarget = true;
            ItemDropZone sellZone = counter.gameObject.AddComponent<ItemDropZone>();
            CreateImage("Frame", counter.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(440f, 2f), s_frame).rectTransform.pivot = new Vector2(0.5f, 1f);
            TMP_Text sellText = CreateText("Text", counter.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 20f, TextAlignmentOptions.Center);
            Stretch(sellText.rectTransform, 8f);
            sellText.color = new Color(0.85f, 0.8f, 0.7f);

            Button unload = CreateButton("UnloadAll", inventory.transform.Find("BagPanel"), new Vector2(1f, 1f), new Vector2(-104f, -34f), new Vector2(120f, 26f), "Unload all");
            ((RectTransform)unload.transform).pivot = new Vector2(1f, 1f);

            StashPagesView view = other.gameObject.AddComponent<StashPagesView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_inventory", inventory);
            BattleEditorUtility.Set(so, "_tabs", tabs);
            BattleEditorUtility.Set(so, "_unloadButton", unload);
            BattleEditorUtility.Set(so, "_sellZone", sellZone);
            BattleEditorUtility.Set(so, "_sellText", sellText);
            BattleEditorUtility.Set(so, "_coinsText", coins);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        /// The party board and the book of descents as see-through windows over the hub (one shown at a time). The board: three
        /// full-height slots with the members' dolls, you in the middle, a big "+" in an open side slot, Leave Party or the join
        /// code under you. The book: the map page with ‹ › on the left; queue, party, last run and Start on the right; a footer
        /// with the character switch and the dev tools.
        private static HubBoardView BuildHubBoard(RectTransform lobby, Inputs inputs)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            Vector2 top = new Vector2(0.5f, 1f);
            Vector2 bottom = new Vector2(0.5f, 0f);
            Vector2 bottomLeft = Vector2.zero;
            Vector2 bottomRight = new Vector2(1f, 0f);
            Color notice = new Color(1f, 0.78f, 0.45f);
            Color veil = new Color(0.45f, 0.36f, 0.22f, 0.6f);
            RectTransform page = CreateRect("Board", lobby, center, Vector2.zero, Vector2.zero);
            Stretch(page, 0f);
            CreateImage("Dim", page, center, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.3f)).rectTransform.StretchFill();

            TMP_Text Label(string name, Transform parent, Vector2 position, Vector2 size, string text)
            {
                TMP_Text label = CreateText(name, parent, top, position, size, 15f, TextAlignmentOptions.Center);
                label.text = text;
                label.color = s_textDim;
                label.fontStyle = FontStyles.Bold;

                return label;
            }

            Button Styled(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string text, float fontSize, Color back, Color label)
            {
                Button button = CreateButton(name, parent, anchor, position, size, text);
                button.image.color = back;
                TMP_Text caption = button.GetComponentInChildren<TMP_Text>();
                caption.fontSize = fontSize;
                caption.color = label;

                return button;
            }

            // Party board.
            RectTransform party = HubWindow(page, "Party", new Vector2(1240f, 860f), out TMP_Text partyTitle, out TMP_Text partyRank);
            List<(Image back, Image[] border, CharacterPreviewView preview, GameObject band, TMP_Text name, GameObject leader, Button invite)> slots = new();

            for (int i = 0; i < 3; i++)
            {
                bool isMe = i == 1;
                Vector2 size = isMe ? new Vector2(340f, 600f) : new Vector2(290f, 540f);
                RectTransform slot = CreateRect(isMe ? "You" : "Slot" + i, party, top, new Vector2((i - 1) * 375f, isMe ? -100f : -130f), size);
                Image back = CreateImage("Back", slot, center, Vector2.zero, Vector2.zero, new Color(0.02f, 0.02f, 0.02f, 0.75f));
                back.rectTransform.StretchFill();
                Image[] border = Border(slot, isMe ? 3f : 2f, isMe ? s_gold : s_frame);
                Vector2 dollSize = new Vector2(size.x - 12f, size.y - (isMe ? 110f : 90f));
                RawImage doll = CreateRect("Preview", slot, top, new Vector2(0f, -6f), dollSize).gameObject.AddComponent<RawImage>();
                CharacterPreviewView preview = BuildPreview(doll, inputs, 3 + i, (int)dollSize.x, (int)dollSize.y, out _);
                Image band = CreateImage("NameBand", slot, bottom, new Vector2(0f, 3f), new Vector2(size.x - 6f, isMe ? 104f : 84f), new Color(0f, 0f, 0f, 0.6f));
                TMP_Text name = CreateText("Name", band.rectTransform, center, Vector2.zero, Vector2.zero, isMe ? 25f : 21f, TextAlignmentOptions.Center);
                Stretch(name.rectTransform, 4f);
                TMP_Text leader = Label("Leader", slot, new Vector2(0f, -12f), new Vector2(200f, 22f), "LEADER");
                leader.fontSize = 14f;
                leader.color = s_gold;
                Button invite = null;

                if (!isMe)
                {
                    invite = Styled("Invite", slot, center, new Vector2(0f, 30f), new Vector2(150f, 150f), "+", 120f, new Color(s_gold.r, s_gold.g, s_gold.b, 0.12f),
                        new Color(s_gold.r, s_gold.g, s_gold.b, 0.8f));
                    invite.image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                    invite.image.type = Image.Type.Simple;
                    TMP_Text caption = CreateText("Caption", invite.transform, center, new Vector2(0f, -110f), new Vector2(260f, 28f), 18f, TextAlignmentOptions.Center);
                    caption.text = "Invite a player";
                    caption.color = s_textDim;
                }

                slots.Add((back, border, preview, band.gameObject, name, leader.gameObject, invite));
            }

            Button leaveParty = Styled("LeaveParty", party, top, new Vector2(0f, -718f), new Vector2(300f, 46f), "Leave Party", 18f, s_panelLight, s_text);
            TMP_InputField partyCode = CreateInput("PartyCode", party, top, new Vector2(-80f, -718f), new Vector2(150f, 46f), "Code");
            partyCode.contentType = TMP_InputField.ContentType.IntegerNumber;
            partyCode.characterLimit = 4;
            Button joinParty = Styled("JoinParty", party, top, new Vector2(80f, -718f), new Vector2(150f, 46f), "Join Party", 18f, s_panelLight, s_text);
            TMP_Text partyNotice = CreateText("Notice", party, bottom, new Vector2(0f, 26f), new Vector2(900f, 30f), 18f, TextAlignmentOptions.Center);
            partyNotice.color = notice;

            // Book of descents: the map page.
            RectTransform descent = HubWindow(page, "Descent", new Vector2(1200f, 720f), out TMP_Text descentTitle, out TMP_Text descentRank);
            CreateImage("Spine", descent, top, new Vector2(0f, -90f), new Vector2(2f, 540f), veil);
            RectTransform mapPage = CreateRect("MapPage", descent, top, new Vector2(-300f, -86f), new Vector2(540f, 560f));
            Label("Label", mapPage, Vector2.zero, new Vector2(540f, 24f), "MAP");
            Image picture = CreateImage("Picture", mapPage, top, new Vector2(0f, -32f), new Vector2(500f, 260f), s_frame);
            RectTransform clip = CreateRect("Clip", picture.rectTransform, center, Vector2.zero, Vector2.zero);
            Stretch(clip, 2f);
            clip.gameObject.AddComponent<RectMask2D>();
            RawImage mapImage = CreateRect("Floor", clip, center, Vector2.zero, Vector2.zero).gameObject.AddComponent<RawImage>();
            mapImage.rectTransform.StretchFill();
            mapImage.raycastTarget = false;
            mapImage.uvRect = new Rect(0f, 0.242f, 1f, 0.516f);
            mapImage.color = new Color(0.8f, 0.75f, 0.65f);
            Button[] arrows = new Button[2];

            for (int i = 0; i < 2; i++)
            {
                arrows[i] = Styled(i == 0 ? "Prev" : "Next", picture.rectTransform, new Vector2(i, 0.5f), Vector2.zero, new Vector2(40f, 64f), i == 0 ? "<" : ">", 34f,
                    new Color(0f, 0f, 0f, 0.75f), s_gold);
                ((RectTransform)arrows[i].transform).pivot = center;
                arrows[i].GetComponentInChildren<TMP_Text>().richText = false;
            }

            TMP_Text mapTitle = CreateText("Title", mapPage, top, new Vector2(0f, -306f), new Vector2(500f, 36f), 28f, TextAlignmentOptions.Center);
            mapTitle.color = s_gold;
            mapTitle.fontStyle = FontStyles.Bold;
            TMP_Text mapInfo = CreateText("Info", mapPage, top, new Vector2(0f, -350f), new Vector2(500f, 100f), 16f, TextAlignmentOptions.Top);
            mapInfo.color = new Color(0.75f, 0.71f, 0.62f);
            TMP_Text mapPortals = CreateText("Portals", mapPage, top, new Vector2(0f, -424f), new Vector2(500f, 24f), 15f, TextAlignmentOptions.Center);
            mapPortals.color = new Color(0.45f, 0.7f, 0.95f);

            // The run page.
            RectTransform runPage = CreateRect("RunPage", descent, top, new Vector2(300f, -86f), new Vector2(540f, 560f));
            Label("Label", runPage, Vector2.zero, new Vector2(540f, 24f), "QUEUE");
            List<(Button button, TMP_Text label, GameObject mark)> queues = new();
            string[] queueNames = { "Solo", "Duo", "Trio" };

            for (int i = 0; i < queueNames.Length; i++)
            {
                Button queue = Styled("Queue" + queueNames[i], runPage, top, new Vector2((i - 1) * 170f, -32f), new Vector2(160f, 48f), queueNames[i], 21f, s_panelLight, s_text);
                queues.Add((queue, queue.GetComponentInChildren<TMP_Text>(), Border((RectTransform)queue.transform, 2f, s_gold)[0].transform.parent.gameObject));
            }

            TMP_Text queueNote = CreateText("QueueNote", runPage, top, new Vector2(0f, -88f), new Vector2(540f, 22f), 14f, TextAlignmentOptions.Center);
            queueNote.color = s_textDim;
            TMP_Text partyLabel = Label("PartyLabel", runPage, new Vector2(0f, -134f), new Vector2(540f, 24f), string.Empty);
            List<(Image back, TMP_Text text)> chips = new();

            for (int i = 0; i < 3; i++)
            {
                Image chip = CreateImage("Member" + i, runPage, top, new Vector2((i - 1) * 170f, -166f), new Vector2(160f, 64f), new Color(0.02f, 0.02f, 0.02f, 0.7f));
                Border(chip.rectTransform, 1f, i == 0 ? s_gold : s_frame);
                TMP_Text text = CreateText("Text", chip.rectTransform, center, Vector2.zero, Vector2.zero, 17f, TextAlignmentOptions.Center);
                Stretch(text.rectTransform, 4f);
                chips.Add((chip, text));
            }

            Label("ResultLabel", runPage, new Vector2(0f, -262f), new Vector2(540f, 24f), "LAST RUN");
            Image resultBack = CreateImage("Result", runPage, top, new Vector2(0f, -292f), new Vector2(500f, 44f), new Color(0f, 0f, 0f, 0.5f));
            TMP_Text result = CreateText("Text", resultBack.rectTransform, center, Vector2.zero, Vector2.zero, 17f, TextAlignmentOptions.Center);
            result.rectTransform.StretchFill();
            result.color = s_text;
            TMP_Text descentNotice = CreateText("Notice", runPage, bottom, new Vector2(0f, 108f), new Vector2(540f, 28f), 17f, TextAlignmentOptions.Center);
            descentNotice.color = notice;
            Button start = Styled("Start", runPage, bottom, Vector2.zero, new Vector2(500f, 92f), "Start", 40f, new Color(0.3f, 0.22f, 0.1f, 0.97f), s_gold);
            start.GetComponentInChildren<TMP_Text>().fontStyle = FontStyles.Bold;
            Border((RectTransform)start.transform, 2f, s_gold);

            // Footer: the character switch on the left, the dev tools on the right.
            CreateImage("FooterRule", descent, bottom, new Vector2(0f, 58f), new Vector2(1160f, 1f), veil);
            Button characters = Styled("Characters", descent, bottomLeft, new Vector2(20f, 12f), new Vector2(210f, 34f), "Change Character", 15f, s_panelLight, s_text);
            Color devBack = new Color(0.22f, 0.1f, 0.07f, 0.9f);
            Color devText = new Color(0.9f, 0.75f, 0.65f);
            Button wipe = Styled("WipeSave", descent, bottomRight, new Vector2(-20f, 12f), new Vector2(120f, 34f), "Wipe Save", 14f, devBack, devText);
            Button reset = Styled("SquireKit", descent, bottomRight, new Vector2(-148f, 12f), new Vector2(120f, 34f), "Squire Kit", 14f, devBack, devText);
            Button maxLevel = Styled("MaxLevel", descent, bottomRight, new Vector2(-276f, 12f), new Vector2(120f, 34f), "Max Level", 14f, devBack, devText);
            TMP_Text dev = CreateText("Dev", descent, bottomRight, new Vector2(-404f, 12f), new Vector2(60f, 34f), 13f, TextAlignmentOptions.MidlineRight);
            dev.text = "DEV";
            dev.color = s_textDim;
            dev.fontStyle = FontStyles.Bold;

            party.Find("TopBar").SetAsLastSibling();
            descent.Find("TopBar").SetAsLastSibling();

            Texture floorMap = inputs.FloorMaps.Length > 0 ? inputs.FloorMaps[0] : null;
            (string title, string info, string portals, Texture picture, string scene, bool isTest)[] destinations =
            {
                ("The Tangled Catacombs", "7x7 rooms laid out anew every run around the Great Hall · dark crypts, skeletons, trapped corridors · 15 min",
                    "Blue portals at 7:00 · 10:00 · 12:00 · 14:30", floorMap, SceneTravel.DungeonScene, false),
                ("Dungeon Test", "The Arena with one chest of each kind and Miners Row next door, no monsters · F2 calls monsters in to test mechanics",
                    string.Empty, floorMap, SceneTravel.DungeonScene, true)
            };

            HubBoardView view = page.gameObject.AddComponent<HubBoardView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_context", inputs.Context);
            BattleEditorUtility.Set(so, "_partyWindow", party.gameObject);
            BattleEditorUtility.Set(so, "_descentWindow", descent.gameObject);
            BattleEditorUtility.Set(so, "_titleTexts", new[] { partyTitle, descentTitle });
            BattleEditorUtility.Set(so, "_rankTexts", new[] { partyRank, descentRank });
            BattleEditorUtility.Set(so, "_partyLabelText", partyLabel);
            BattleEditorUtility.Set(so, "_prevMapButton", arrows[0]);
            BattleEditorUtility.Set(so, "_nextMapButton", arrows[1]);
            BattleEditorUtility.Set(so, "_mapTitleText", mapTitle);
            BattleEditorUtility.Set(so, "_mapInfoText", mapInfo);
            BattleEditorUtility.Set(so, "_mapPortalsText", mapPortals);
            BattleEditorUtility.Set(so, "_mapImage", mapImage);
            BattleEditorUtility.Set(so, "_queueNoteText", queueNote);
            BattleEditorUtility.Set(so, "_startButton", start);
            BattleEditorUtility.Set(so, "_joinPartyButton", joinParty);
            BattleEditorUtility.Set(so, "_partyCodeInput", partyCode);
            BattleEditorUtility.Set(so, "_leavePartyButton", leaveParty);
            BattleEditorUtility.Set(so, "_noticeTexts", new[] { partyNotice, descentNotice });
            BattleEditorUtility.Set(so, "_resetKitButton", reset);
            BattleEditorUtility.Set(so, "_wipeButton", wipe);
            BattleEditorUtility.Set(so, "_maxLevelButton", maxLevel);
            BattleEditorUtility.Set(so, "_charactersButton", characters);
            BattleEditorUtility.Set(so, "_resultText", result);
            so.FindProperty("_slots").arraySize = slots.Count;

            for (int i = 0; i < slots.Count; i++)
            {
                string path = $"_slots.Array.data[{i}].";
                BattleEditorUtility.Set(so, path + "Back", slots[i].back);
                BattleEditorUtility.Set(so, path + "Border", slots[i].border);
                BattleEditorUtility.Set(so, path + "Preview", slots[i].preview);
                BattleEditorUtility.Set(so, path + "Band", slots[i].band);
                BattleEditorUtility.Set(so, path + "Name", slots[i].name);
                BattleEditorUtility.Set(so, path + "Leader", slots[i].leader);
                BattleEditorUtility.Set(so, path + "Invite", slots[i].invite);
            }

            so.FindProperty("_chips").arraySize = chips.Count;

            for (int i = 0; i < chips.Count; i++)
            {
                BattleEditorUtility.Set(so, $"_chips.Array.data[{i}].Back", chips[i].back);
                BattleEditorUtility.Set(so, $"_chips.Array.data[{i}].Text", chips[i].text);
            }

            so.FindProperty("_queueButtons").arraySize = queues.Count;

            for (int i = 0; i < queues.Count; i++)
            {
                BattleEditorUtility.Set(so, $"_queueButtons.Array.data[{i}].Button", queues[i].button);
                BattleEditorUtility.Set(so, $"_queueButtons.Array.data[{i}].Label", queues[i].label);
                BattleEditorUtility.Set(so, $"_queueButtons.Array.data[{i}].Mark", queues[i].mark);
            }

            so.FindProperty("_destinations").arraySize = destinations.Length;

            for (int i = 0; i < destinations.Length; i++)
            {
                BattleEditorUtility.Set(so, $"_destinations.Array.data[{i}].Title", destinations[i].title);
                BattleEditorUtility.Set(so, $"_destinations.Array.data[{i}].Info", destinations[i].info);
                BattleEditorUtility.Set(so, $"_destinations.Array.data[{i}].Portals", destinations[i].portals);
                BattleEditorUtility.Set(so, $"_destinations.Array.data[{i}].Picture", destinations[i].picture);
                BattleEditorUtility.Set(so, $"_destinations.Array.data[{i}].Scene", destinations[i].scene);
                BattleEditorUtility.Set(so, $"_destinations.Array.data[{i}].IsTest", destinations[i].isTest);
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        /// See-through window of the hub board, centred on the screen: a framed dark glass with its own top bar (rank shield,
        /// title, close hint). The bar is built first; callers move it over their content.
        private static RectTransform HubWindow(RectTransform parent, string name, Vector2 size, out TMP_Text title, out TMP_Text rank)
        {
            Image window = CreateImage(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, size, new Color(0.03f, 0.025f, 0.02f, 0.78f));
            window.raycastTarget = true;
            RectTransform rect = window.rectTransform;
            Border(rect, 2f, s_frame);

            RectTransform bar = CreateRect("TopBar", rect, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 64f));
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = Vector2.one;
            CreateImage("Back", bar, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.45f)).rectTransform.StretchFill();
            RectTransform rule = CreateImage("Rule", bar, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 2f), s_frame).rectTransform;
            rule.anchorMin = Vector2.zero;
            rule.anchorMax = new Vector2(1f, 0f);
            title = CreateText("Title", bar, new Vector2(0f, 0.5f), new Vector2(112f, 0f), new Vector2(800f, 50f), 28f, TextAlignmentOptions.MidlineLeft);
            title.color = s_gold;
            TMP_Text close = CreateText("Close", bar, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(300f, 50f), 17f, TextAlignmentOptions.MidlineRight);
            close.text = "[Esc] Close";
            close.color = s_textDim;
            Image shield = CreateImage("Rank", bar, new Vector2(0f, 1f), new Vector2(20f, 12f), new Vector2(72f, 90f), Color.white);
            shield.sprite = DungeonUiSpriteBuilder.Load("Shield");
            rank = CreateText("Text", shield.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(72f, 60f), 13f, TextAlignmentOptions.Center);
            rank.color = new Color(0.16f, 0.1f, 0.03f);
            rank.fontStyle = FontStyles.Bold;
            rank.lineSpacing = -30f;

            return rect;
        }

        /// Inner frame of a rect: four edges of the given width under a "Border" child; returns the edges.
        private static Image[] Border(RectTransform rect, float width, Color color)
        {
            RectTransform root = CreateRect("Border", rect, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            root.StretchFill();
            Image[] edges = new Image[4];

            for (int i = 0; i < 4; i++)
            {
                bool isVertical = i >= 2;
                float side = i % 2;
                edges[i] = CreateImage("Edge" + i, root, Vector2.zero, Vector2.zero, Vector2.zero, color);
                RectTransform edge = edges[i].rectTransform;
                edge.anchorMin = isVertical ? new Vector2(side, 0f) : new Vector2(0f, side);
                edge.anchorMax = isVertical ? new Vector2(side, 1f) : new Vector2(1f, side);
                edge.pivot = isVertical ? new Vector2(side, 0.5f) : new Vector2(0.5f, side);
                edge.sizeDelta = isVertical ? new Vector2(width, 0f) : new Vector2(0f, width);
            }

            return edges;
        }

        /// Dresses the preview stage as a candle-lit tavern corner and frames the character behind the table.
        private static void BuildTavern(Camera camera)
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
            DimFlames(set);
            BattleEditorUtility.SetLayerRecursively(set.gameObject, root.gameObject.layer);
        }

        /// Dungeon props are lit for big rooms; this close to the camera they would burn the picture out.
        private static void DimFlames(Transform set)
        {
            foreach (FlickerLightComponent flicker in set.GetComponentsInChildren<FlickerLightComponent>())
            {
                SerializedObject so = new SerializedObject(flicker);
                so.FindProperty("_baseIntensity").floatValue *= 0.5f;
                so.FindProperty("_amplitude").floatValue *= 0.5f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
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

            // The title is a pair of tabs: the doll with the equipped perks and skills, or the spell wheels of a spell class.
            string[] modes = { "Perks and Skills", "Spell Wheels" };
            Button[] modeTabs = new Button[modes.Length];
            // A class without spells has the first tab alone, centered.
            RectTransform modeBar = CreateRect("Tabs", page, top, new Vector2(0f, -104f), new Vector2(620f, 40f));
            HorizontalLayoutGroup modeLayout = modeBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            modeLayout.spacing = 20f;
            modeLayout.childAlignment = TextAnchor.MiddleCenter;
            modeLayout.childControlWidth = modeLayout.childControlHeight = modeLayout.childForceExpandWidth = modeLayout.childForceExpandHeight = false;

            for (int i = 0; i < modes.Length; i++)
            {
                modeTabs[i] = CreateButton("Tab" + modes[i].Replace(" ", string.Empty), modeBar, top, Vector2.zero, new Vector2(300f, 40f), modes[i]);
                modeTabs[i].image.sprite = DungeonUiSpriteBuilder.Load("Glow");
                modeTabs[i].image.type = Image.Type.Simple;
                modeTabs[i].GetComponentInChildren<TMP_Text>().fontSize = 23f;
            }

            CreateImage("TitleRule", page, top, new Vector2(0f, -146f), new Vector2(620f, 2f), s_frame);
            // The chosen subclass's description under the title, above the arch.
            TMP_Text subclassText = CreateText("SubclassText", page, top, new Vector2(0f, -160f), new Vector2(900f, 24f), 13f, TextAlignmentOptions.Center);
            subclassText.color = s_textDim;

            RectTransform character = CreateRect("Character", page, center, Vector2.zero, Vector2.zero);
            Stretch(character, 0f);
            RawImage doll = CreateRect("Doll", character, top, new Vector2(0f, -205f), new Vector2(480f, 590f)).gameObject.AddComponent<RawImage>();
            CharacterPreviewView preview = BuildPreview(doll, inputs, 2, 480, 590, out Camera camera);
            camera.backgroundColor = s_lobbyBack;
            CreateImage("Arch", character, top, new Vector2(0f, -190f), new Vector2(532f, 620f), line).sprite = DungeonUiSpriteBuilder.Load("Arch");
            CreateImage("ArchBase", character, top, new Vector2(0f, -811f), new Vector2(440f, 2f), line);

            // Round subclass buttons stand under the doll's feet, their names under them.
            Sprite disc = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Button[] subclassTabs = new Button[3];
            Image[] subclassIcons = new Image[subclassTabs.Length];

            for (int i = 0; i < subclassTabs.Length; i++)
            {
                subclassTabs[i] = CreateButton("Subclass" + i, page, top, new Vector2((i - 1) * 150f, -828f), new Vector2(60f, 60f), string.Empty);
                subclassTabs[i].image.sprite = disc;
                subclassTabs[i].image.type = Image.Type.Simple;
                RectTransform tabRect = (RectTransform)subclassTabs[i].transform;
                CreateImage("Rim", tabRect, center, Vector2.zero, new Vector2(60f, 60f), line).sprite = DungeonUiSpriteBuilder.Load("Circle");
                subclassIcons[i] = CreateImage("Icon", tabRect, center, Vector2.zero, new Vector2(40f, 40f), Color.white);
                subclassIcons[i].preserveAspect = true;
                TMP_Text name = subclassTabs[i].GetComponentInChildren<TMP_Text>();
                name.rectTransform.anchorMin = name.rectTransform.anchorMax = name.rectTransform.pivot = new Vector2(0.5f, 0f);
                name.rectTransform.anchoredPosition = new Vector2(0f, -24f);
                name.rectTransform.sizeDelta = new Vector2(150f, 22f);
                name.fontSize = 14f;
            }

            // The equipped perks in a row under the subclasses, between the two skills (Q left, E right).
            AbilityIconView[] perkSlots = new AbilityIconView[4];
            AbilityIconView[] skillSlots = new AbilityIconView[2];

            for (int i = 0; i < perkSlots.Length; i++)
                perkSlots[i] = AbilityIcon("PerkSlot" + i, character, top, new Vector2((i - 1.5f) * 122f, -992f), "Diamond", 104f);

            for (int i = 0; i < skillSlots.Length; i++)
                skillSlots[i] = AbilityIcon("SkillSlot" + i, character, top, new Vector2(i == 0 ? -330f : 330f, -984f), "Square", 88f);

            // The pool stacks its sections from the top: perks, skills and, for a spell class, its spells.
            Image pool = CreateImage("Pool", page, Vector2.one, new Vector2(-20f, -96f), new Vector2(530f, 964f), Color.white);
            pool.sprite = DungeonUiSpriteBuilder.Load("Panel");
            RectTransform content = CreateRect("Content", pool.rectTransform, top, Vector2.zero, Vector2.zero);
            Stretch(content, 0f);
            VerticalLayoutGroup stack = content.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset(30, 30, 9, 60);
            stack.spacing = 14f;
            stack.childControlWidth = stack.childControlHeight = stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            RectTransform Section(string name, float cell, int columns, Vector2 spacing)
            {
                RectTransform section = CreateRect(name + "Section", content, top, Vector2.zero, Vector2.zero);
                VerticalLayoutGroup group = section.gameObject.AddComponent<VerticalLayoutGroup>();
                group.spacing = 10f;
                group.childControlWidth = group.childControlHeight = group.childForceExpandWidth = true;
                group.childForceExpandHeight = false;
                Image band = CreateImage(name + "Band", section, top, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.35f));
                band.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
                TMP_Text header = CreateText(name + "Title", band.rectTransform, center, Vector2.zero, Vector2.zero, 22f, TextAlignmentOptions.Center);
                header.rectTransform.StretchFill();
                header.text = name;
                header.color = s_text;
                RectTransform grid = CreateRect(name, section, top, Vector2.zero, Vector2.zero);
                GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
                layout.cellSize = new Vector2(cell, cell);
                layout.spacing = spacing;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = columns;

                return grid;
            }

            RectTransform perks = Section("Perks", 104f, 4, new Vector2(18f, 14f));
            RectTransform skillRoot = Section("Skills", 104f, 4, new Vector2(18f, 14f));
            // The spell keys hang under their squares, so the rows leave room for them.
            RectTransform spellRoot = Section("Spells", 92f, 5, new Vector2(2f, 20f));            TMP_Text hint = CreateText("Hint", pool.rectTransform, top, new Vector2(0f, -918f), new Vector2(470f, 40f), 12f, TextAlignmentOptions.Center);
            hint.text = "Drag a perk, a skill or a spell onto a slot  ·  click a perk to equip or remove it  ·  LMB / RMB on a skill binds it to Q / E, RMB on a Q / E slot empties it, LMB / RMB on a spell puts it into wheel I / II";
            hint.color = s_textDim;
            SpellsView spells = BuildSpells(page, spellRoot);
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
            BattleEditorUtility.Set(so, "_perkSlots", perkSlots);
            BattleEditorUtility.Set(so, "_skillSlots", skillSlots);
            BattleEditorUtility.Set(so, "_stats", stats);
            BattleEditorUtility.Set(so, "_tooltip", tooltip);
            BattleEditorUtility.Set(so, "_tooltipText", tooltipText);
            BattleEditorUtility.Set(so, "_subclassTabs", subclassTabs);
            BattleEditorUtility.Set(so, "_subclassIcons", subclassIcons);
            BattleEditorUtility.Set(so, "_subclassText", subclassText);
            BattleEditorUtility.Set(so, "_spells", spells);
            BattleEditorUtility.Set(so, "_modeTabs", modeTabs);
            BattleEditorUtility.Set(so, "_modeRoots", new[] { character.gameObject, spells.transform.Find("Wheels").gameObject });
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        /// "Spell Wheels" tab of the skills page: the wheels as rings of square slots around a hub between the attribute sheet and the pool;
        /// the class spells go into the pool section the page passes in.
        private static SpellsView BuildSpells(RectTransform skills, RectTransform spellRoot)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            Vector2 top = new Vector2(0.5f, 1f);
            Color line = new Color(0.6f, 0.57f, 0.5f);
            Sprite disc = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            Sprite circle = DungeonUiSpriteBuilder.Load("Circle");
            RectTransform page = CreateRect("Spells", skills, center, Vector2.zero, Vector2.zero);
            Stretch(page, 0f);
            RectTransform group = CreateRect("Wheels", page, center, Vector2.zero, Vector2.zero);
            Stretch(group, 0f);

            TMP_Text info = CreateText("Info", group, top, new Vector2(0f, -232f), new Vector2(820f, 24f), 13f, TextAlignmentOptions.Center);
            info.text = $"Each wheel holds {ClassConfig.SpellWheelSize} spells  ·  wheel I opens with the memory skill put on Q, wheel II with the one on E";
            info.color = s_textDim;

            GameObject[] wheels = new GameObject[ClassConfig.WheelCount];
            TMP_Text[] wheelTitles = new TMP_Text[ClassConfig.WheelCount];
            GameObject[] inactiveLabels = new GameObject[ClassConfig.WheelCount];
            AbilityIconView[] slots = new AbilityIconView[ClassConfig.WheelCount * ClassConfig.SpellWheelSize];

            for (int wheel = 0; wheel < wheels.Length; wheel++)
            {
                // Two full-size wheels don't fit between the sheet and the pool side by side.
                RectTransform ring = CreateRect("Wheel" + (wheel + 1), group, top, new Vector2((wheel * 2 - 1) * 205f, -600f), new Vector2(520f, 520f));
                ring.pivot = center;
                ring.localScale = Vector3.one * 0.78f;
                ring.gameObject.AddComponent<CanvasGroup>();
                CreateImage("Disc", ring, center, Vector2.zero, new Vector2(500f, 500f), new Color(0f, 0f, 0f, 0.45f)).sprite = disc;
                CreateImage("Rim", ring, center, Vector2.zero, new Vector2(500f, 500f), new Color(line.r, line.g, line.b, 0.5f)).sprite = circle;
                CreateImage("Track", ring, center, Vector2.zero, new Vector2(340f, 340f), new Color(line.r, line.g, line.b, 0.15f)).sprite = circle;
                CreateImage("Hub", ring, center, Vector2.zero, new Vector2(150f, 150f), new Color(0.08f, 0.07f, 0.06f, 0.95f)).sprite = disc;
                CreateImage("HubRim", ring, center, Vector2.zero, new Vector2(150f, 150f), line).sprite = circle;
                TMP_Text numeral = CreateText("Numeral", ring, center, new Vector2(0f, 16f), new Vector2(140f, 50f), 38f, TextAlignmentOptions.Center);
                numeral.text = wheel == 0 ? "I" : "II";
                numeral.color = s_gold;
                numeral.fontStyle = FontStyles.Bold;
                wheelTitles[wheel] = CreateText("Title", ring, center, new Vector2(0f, -26f), new Vector2(130f, 36f), 13f, TextAlignmentOptions.Center);
                wheelTitles[wheel].color = s_text;

                // Slots go clockwise from the top.
                for (int i = 0; i < ClassConfig.SpellWheelSize; i++)
                {
                    float angle = Mathf.PI * 0.5f - i * 2f * Mathf.PI / ClassConfig.SpellWheelSize;
                    Vector2 position = 170f * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    slots[wheel * ClassConfig.SpellWheelSize + i] = AbilityIcon("Slot" + i, ring, center, position, "Square", 88f);
                }

                wheels[wheel] = ring.gameObject;
                TMP_Text inactive = CreateText("Inactive" + (wheel + 1), group, top, ring.anchoredPosition, new Vector2(320f, 50f), 28f, TextAlignmentOptions.Center);
                inactive.rectTransform.pivot = center;
                inactive.text = "INACTIVE";
                inactive.color = s_textDim;
                inactive.fontStyle = FontStyles.Bold;
                inactiveLabels[wheel] = inactive.gameObject;
            }

            TMP_Text hint = CreateText("Hint", group, top, new Vector2(0f, -990f), new Vector2(820f, 40f), 13f, TextAlignmentOptions.Center);
            hint.text = "Drag a spell onto a wheel slot  ·  click a slot or drag it off to remove the spell  ·  LMB / RMB on a spell puts it into wheel I / II";
            hint.color = s_textDim;

            AbilityIconView spellIcon = AbilityIcon("SpellIcon", page, center, Vector2.zero, "Square", 88f);
            spellIcon.gameObject.SetActive(false);

            RectTransform tooltip = CreateRect("Tooltip", page, center, Vector2.zero, new Vector2(340f, 112f));
            CreateImage("Back", tooltip, center, Vector2.zero, Vector2.zero, new Color(0.03f, 0.03f, 0.03f, 0.96f)).rectTransform.StretchFill();
            CreateImage("Frame", tooltip, top, Vector2.zero, new Vector2(340f, 3f), s_frame);
            TMP_Text tooltipText = CreateText("Text", tooltip, center, Vector2.zero, Vector2.zero, 15f, TextAlignmentOptions.TopLeft);
            Stretch(tooltipText.rectTransform, 12f);

            SpellsView view = page.gameObject.AddComponent<SpellsView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_spellIconPrefab", spellIcon);
            BattleEditorUtility.Set(so, "_spellsRoot", spellRoot);
            BattleEditorUtility.Set(so, "_sectionRoot", spellRoot.parent.gameObject);
            BattleEditorUtility.Set(so, "_wheels", wheels);
            BattleEditorUtility.Set(so, "_wheelSlots", slots);
            BattleEditorUtility.Set(so, "_wheelTitles", wheelTitles);
            BattleEditorUtility.Set(so, "_inactiveLabels", inactiveLabels);
            BattleEditorUtility.Set(so, "_tooltip", tooltip);
            BattleEditorUtility.Set(so, "_tooltipText", tooltipText);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        /// Character sheet: the hexagram with the attributes on its corners and every small stat as a dot on its sides, then a
        /// Dark and Darker style list of all of them below. The tooltip opens beside the sheet (hence the width of the panel it sits on) and is created under
        /// the given parent so the caller can keep it above the other panels.
        private static StatsView BuildStats(Transform parent, Vector2 position, float width, float radius, bool isCompact, Transform tooltipParent,
            out RectTransform tooltip, bool hasList = true)
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
            List<(StatId stat, string header)> list = new();
            Vector2 badge = Vector2.one * (isCompact ? 50f : 58f);
            float dot = isCompact ? 10f : 12f;
            float pitch = isCompact ? 15f : 18f;
            Vector2 Corner(int index) => new Vector2(Mathf.Sin(index * Mathf.PI / 3f), Mathf.Cos(index * Mathf.PI / 3f)) * radius;

            void Dot(StatId stat, Vector2 dotPosition) => labels.Add(StatDot(chartRoot, stat, dotPosition, StatSheet.IsEdge(stat) ? dot * 1.5f : dot));

            // Every side holds the edge stat in its middle, the own stats of each end on the half nearer to it. Dots go
            // first: the attribute badges on the corners are drawn over them.
            for (int i = 0; i < StatSheet.AttributeCount; i++)
            {
                int next = (i + 1) % StatSheet.AttributeCount;
                Vector2 middle = (Corner(i) + Corner(next)) * 0.5f;
                Vector2 along = (Corner(next) - Corner(i)).normalized * pitch;
                StatId[] own = StatSheet.OwnStats((StatId)i);
                StatId[] nextOwn = StatSheet.OwnStats((StatId)next);

                for (int k = own.Length / 2; k < own.Length; k++)
                    Dot(own[k], middle - along * (own.Length - k));

                Dot(StatSheet.Edge(i), middle);

                for (int k = 0; k < nextOwn.Length / 2; k++)
                    Dot(nextOwn[k], middle + along * (k + 1));
            }

            for (int i = 0; i < StatSheet.AttributeCount; i++)
                labels.Add(StatNode(chartRoot, (StatId)i, Corner(i), "Diamond", badge, isCompact ? 17f : 19f, isCompact ? 14f : 15f));

            // The list: Health first, then everything else, the damage and defense groups at the bottom; every flat stat stands
            // right above the share it turns into.
            void Add(params StatId[] stats) => list.AddRange(System.Array.ConvertAll(stats, stat => (stat, (string)null)));

            Add(StatId.Health, StatId.MoveSpeed, StatId.ActionSpeed, StatId.CastSpeed, StatId.BonusCharges, StatId.CooldownSpeed, StatId.BuffDuration);

            for (int i = 0; i < StatSheet.AttributeCount; i++)
                Add(StatSheet.Edge(i));

            list.Add((default, "Damage"));
            Add(StatId.PhysicalPower, StatId.PhysicalDamage, StatId.MagicalPower, StatId.MagicalDamage);
            list.Add((default, "Defense"));
            Add(StatId.ArmorRating, StatId.PhysicalReduction, StatId.MagicResistance, StatId.MagicalReduction, StatId.ProjectileReduction, StatId.FlatReduction,
                StatId.DebuffReduction, StatId.BonusHealing);
            if (!hasList)
                list.Clear();

            int columns = isCompact ? 2 : 1;
            int rows = (list.Count + columns - 1) / columns;
            float rowHeight = isCompact ? 17f : 23f;
            float textSize = isCompact ? 12f : 15f;
            float columnWidth = (width - 40f - (columns - 1) * 16f) / columns;
            for (int i = 0; i < list.Count; i++)
            {
                Vector2 rowPosition = new Vector2(-width * 0.5f + 20f + i / rows * (columnWidth + 16f), -chartHeight - 4f - i % rows * rowHeight);
                (StatId stat, string header) = list[i];

                if (header != null)
                {
                    TMP_Text group = CreateText(header, root, top, rowPosition, new Vector2(columnWidth, rowHeight), textSize, TextAlignmentOptions.MidlineLeft);
                    group.rectTransform.pivot = new Vector2(0f, 1f);
                    group.text = header.ToUpperInvariant();
                    group.color = s_gold;
                    group.fontStyle = FontStyles.Bold;

                    continue;
                }

                float indent = i > 0 && StatSheet.TryGetResult(list[i - 1].stat, out StatId result) && result == stat && list[i - 1].header == null ? textSize : 0f;

                if (StatSheet.TryGetResult(stat, out _))
                    StatLink(root, stat, rowPosition, new Vector2(columnWidth, rowHeight), textSize);

                labels.Add(StatRow(root, stat, rowPosition + new Vector2(indent, 0f), new Vector2(columnWidth - indent, rowHeight), textSize));
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

        /// Small stat on a side of the hexagram, no text: an edge stat is a ring, an own stat a filled dot. The list and the tooltip tell the value.
        private static StatLabelView StatDot(RectTransform parent, StatId stat, Vector2 position, float diameter)
        {
            Image image = CreateImage(stat.ToString(), parent, new Vector2(0.5f, 0.5f), position, Vector2.one * diameter, StatSheet.ColorOf(stat));
            image.sprite = DotSprite(stat);
            image.raycastTarget = true;

            return StatLabel(image.gameObject, stat, null);
        }

        /// Line of the list under the hexagram, placed by its top left corner: the dot of the stat, its name, the value at the right end.
        private static StatLabelView StatRow(RectTransform parent, StatId stat, Vector2 position, Vector2 size, float textSize)
        {
            Image row = CreateImage(stat.ToString(), parent, new Vector2(0.5f, 1f), position, size, new Color(1f, 1f, 1f, 0.025f));
            row.rectTransform.pivot = new Vector2(0f, 1f);
            row.raycastTarget = true;
            Vector2 left = new Vector2(0f, 0.5f);
            Image marker = CreateImage("Dot", row.rectTransform, left, new Vector2(textSize * 0.5f, 0f), Vector2.one * textSize * 0.6f, StatSheet.ColorOf(stat));
            marker.sprite = DotSprite(stat);
            TMP_Text name = CreateText("Name", row.rectTransform, left, new Vector2(textSize * 1.2f, 0f), new Vector2(size.x - textSize * 1.2f - 60f, size.y), textSize,
                TextAlignmentOptions.MidlineLeft);
            name.rectTransform.pivot = new Vector2(0f, 0.5f);
            name.text = StatSheet.Name(stat);
            name.color = new Color(0.8f, 0.77f, 0.7f);
            TMP_Text value = CreateText("Value", row.rectTransform, new Vector2(1f, 0.5f), new Vector2(-4f, 0f), new Vector2(70f, size.y), textSize, TextAlignmentOptions.MidlineRight);
            value.rectTransform.pivot = new Vector2(1f, 0.5f);
            value.fontStyle = FontStyles.Bold;

            return StatLabel(row.gameObject, stat, value);
        }

        /// Plate under a flat stat and the share it turns into (the next row, indented), with an elbow line from the first dot to the second.
        private static void StatLink(RectTransform parent, StatId source, Vector2 position, Vector2 rowSize, float textSize)
        {
            Color color = StatSheet.ColorOf(source);
            Vector2 top = new Vector2(0.5f, 1f);
            Image plate = CreateImage(source + "Link", parent, top, position + new Vector2(-4f, 1f), new Vector2(rowSize.x + 4f, rowSize.y * 2f + 2f), color * new Color(1f, 1f, 1f, 0.1f));
            plate.rectTransform.pivot = new Vector2(0f, 1f);
            CreateImage("Accent", plate.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0f), new Vector2(2f, rowSize.y * 2f), color);
            float x = textSize * 0.5f + 4f;
            Image down = CreateImage("Down", plate.rectTransform, top, new Vector2(x - (rowSize.x + 4f) * 0.5f, -1f - rowSize.y * 0.5f), new Vector2(2f, rowSize.y), color);
            down.rectTransform.pivot = new Vector2(0.5f, 1f);
            Image across = CreateImage("Across", plate.rectTransform, top, new Vector2(x - (rowSize.x + 4f) * 0.5f, -1f - rowSize.y * 1.5f), new Vector2(textSize, 2f), color);
            across.rectTransform.pivot = new Vector2(0f, 0.5f);
        }

        private static Sprite DotSprite(StatId stat)
        {
            return StatSheet.IsEdge(stat) ? DungeonUiSpriteBuilder.Load("Circle") : AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
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

            // One merchant per page, the one the player came to: the counter and the purse stand side by side in the middle.
            Image counter = CreateImage("Counter", page, top, new Vector2(-270f, -96f), new Vector2(940f, 964f), Color.white);
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

            Image purse = CreateImage("Purse", page, top, new Vector2(480f, -96f), new Vector2(520f, 964f), Color.white);
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
            Image centerBack = CreateImage("CenterBack", panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 100f), new Color(0.08f, 0.07f, 0.06f, 0.9f));
            centerBack.sprite = dim.sprite;
            RectTransform slotsRoot = CreateRect("Slots", panel, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Image cursor = CreateImage("Cursor", panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 14f), s_gold);
            cursor.sprite = dim.sprite;
            cursor.raycastTarget = false;
            TMP_Text centerText = CreateText("Center", panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 40f), 16f, TextAlignmentOptions.Center);

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
            // Charge bar under the slot.
            Image bar = CreateImage("Bar", slot, new Vector2(0.5f, 0f), new Vector2(0f, -8f), new Vector2(96f, 7f), new Color(0f, 0f, 0f, 0.8f));
            Image fill = CreateImage("Fill", bar.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(94f, 5f), s_gold);
            fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            slot.gameObject.SetActive(false);

            SpellWheelView view = panel.gameObject.AddComponent<SpellWheelView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_slotPrefab", slot);
            BattleEditorUtility.Set(so, "_slotsRoot", slotsRoot);
            BattleEditorUtility.Set(so, "_titleText", title);
            BattleEditorUtility.Set(so, "_centerText", centerText);
            BattleEditorUtility.Set(so, "_centerBack", centerBack);
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
            Button back2 = CreateButton("Return", panel, new Vector2(0.5f, 0.5f), new Vector2(0f, -130f), new Vector2(300f, 54f), "Return to the hub");

            ResultView view = panel.gameObject.AddComponent<ResultView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_titleText", title);
            BattleEditorUtility.Set(so, "_detailsText", details);
            BattleEditorUtility.Set(so, "_returnButton", back2);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        /// Escape menu: Resume / Settings, and the settings page with the master volume slider.
        private static PauseMenuView BuildPause(Transform root)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            RectTransform panel = CreateRect("Pause", root, center, Vector2.zero, Vector2.zero);
            Stretch(panel, 0f);
            Image dim = CreateImage("Back", panel, center, Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.6f));
            Stretch(dim.rectTransform, 0f);
            dim.raycastTarget = true;

            RectTransform main = CreateImage("Main", panel, center, Vector2.zero, new Vector2(360f, 220f), s_panel).rectTransform;
            TMP_Text title = CreateText("Title", main, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(320f, 50f), 30f, TextAlignmentOptions.Center);
            title.text = "Menu";
            title.color = s_gold;
            Button resume = CreateButton("Resume", main, center, new Vector2(0f, 0f), new Vector2(260f, 48f), "Resume");
            Button settings = CreateButton("Settings", main, center, new Vector2(0f, -64f), new Vector2(260f, 48f), "Settings");

            RectTransform page = CreateImage("Settings", panel, center, Vector2.zero, new Vector2(460f, 320f), s_panel).rectTransform;
            TMP_Text pageTitle = CreateText("Title", page, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(420f, 50f), 30f, TextAlignmentOptions.Center);
            pageTitle.text = "Settings";
            pageTitle.color = s_gold;
            Slider slider = CreateSetting("Volume", page, 66f, out TMP_Text volumeText);
            Slider sensitivity = CreateSetting("Sensitivity", page, -14f, out TMP_Text sensitivityText);
            Button back = CreateButton("Back", page, center, new Vector2(0f, -118f), new Vector2(200f, 44f), "Back");

            PauseMenuView view = panel.gameObject.AddComponent<PauseMenuView>();
            SerializedObject so = new SerializedObject(view);
            BattleEditorUtility.Set(so, "_mainPage", main.gameObject);
            BattleEditorUtility.Set(so, "_settingsPage", page.gameObject);
            BattleEditorUtility.Set(so, "_resumeButton", resume);
            BattleEditorUtility.Set(so, "_settingsButton", settings);
            BattleEditorUtility.Set(so, "_backButton", back);
            BattleEditorUtility.Set(so, "_volumeSlider", slider);
            BattleEditorUtility.Set(so, "_volumeText", volumeText);
            BattleEditorUtility.Set(so, "_sensitivitySlider", sensitivity);
            BattleEditorUtility.Set(so, "_sensitivityText", sensitivityText);
            so.ApplyModifiedPropertiesWithoutUndo();
            page.gameObject.SetActive(false);

            return view;
        }

        /// A caption over a slider, the caption's centre at y.
        private static Slider CreateSetting(string name, RectTransform page, float y, out TMP_Text caption)
        {
            Vector2 center = new Vector2(0.5f, 0.5f);
            caption = CreateText(name, page, center, new Vector2(0f, y), new Vector2(380f, 30f), 20f, TextAlignmentOptions.Left);
            caption.color = s_text;
            Slider slider = DefaultControls.CreateSlider(new DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd")
            }).GetComponent<Slider>();
            RectTransform rect = (RectTransform)slider.transform;
            rect.SetParent(page, false);
            rect.anchorMin = rect.anchorMax = center;
            rect.anchoredPosition = new Vector2(0f, y - 36f);
            rect.sizeDelta = new Vector2(380f, 24f);
            slider.fillRect.GetComponent<Image>().color = s_gold;

            return slider;
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

            mapImage.gameObject.AddComponent<RectMask2D>();
            SwarmZoneGraphic swarm = CreateSwarmZone(mapImage.transform);
            Image portalMarker = CreateImage("PortalMarker", mapImage.transform, center, Vector2.zero, new Vector2(24f, 24f), Color.white);
            portalMarker.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            portalMarker.raycastTarget = false;
            portalMarker.gameObject.AddComponent<Outline>().effectColor = new Color(0.1f, 0.06f, 0.03f);
            portalMarker.gameObject.SetActive(false);
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
            BattleEditorUtility.Set(so, "_floorSizes", DungeonMapBuilder.FloorSizes);
            BattleEditorUtility.Set(so, "_floorGrids", DungeonMapBuilder.FloorGrids);
            BattleEditorUtility.Set(so, "_portalMarker", portalMarker);
            BattleEditorUtility.Set(so, "_swarm", swarm);
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }

        private static SwarmZoneGraphic CreateSwarmZone(Transform parent)
        {
            GameObject zone = CreateRect("Swarm", parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero).gameObject;
            zone.AddComponent<CanvasRenderer>();
            SwarmZoneGraphic swarm = zone.AddComponent<SwarmZoneGraphic>();
            swarm.color = new Color(0.75f, 0.3f, 1f, 0.95f);
            swarm.raycastTarget = false;

            return swarm;
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

        private static TMP_InputField CreateInput(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string placeholder)
        {
            RectTransform rect = CreateRect(name, parent, anchor, position, size);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.05f, 0.04f, 0.035f, 0.96f);
            TMP_InputField input = rect.gameObject.AddComponent<TMP_InputField>();
            RectTransform area = CreateRect("Text Area", rect, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Stretch(area, 6f);
            area.gameObject.AddComponent<RectMask2D>();
            TMP_Text hint = CreateText("Placeholder", area, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 18f, TextAlignmentOptions.Center);
            hint.rectTransform.StretchFill();
            hint.text = placeholder;
            hint.color = s_textDim;
            hint.fontStyle = FontStyles.Italic;
            TMP_Text text = CreateText("Text", area, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 20f, TextAlignmentOptions.Center);
            text.rectTransform.StretchFill();
            text.color = s_text;
            input.textViewport = area;
            input.textComponent = text;
            input.placeholder = hint;
            input.targetGraphic = image;

            return input;
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
