#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace tmkoc.threeinarow.EditorTools
{
    /// <summary>
    /// Tools > Three In A Row > Build Scene
    /// Creates Assets/Scenes/ThreeInARow.unity with the whole game as UI under MainCanvas (1920x1080 landscape reference):
    /// background, 3x3 board, two characters, HUD (turn badges + banner), tutorial hand, back button and your WinLose panel.
    /// Placeholder shapes are used until you fill Assets/ScriptableObjects/ThreeInARowAssetsSO.
    /// Replaces the scene file if it already exists.
    /// </summary>
    public static class ThreeInARowSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/ThreeInARow.unity";
        private const string AssetsPath = "Assets/ScriptableObjects/ThreeInARowAssetsSO.asset";
        private const string FredokaPath = "Assets/APIs/PlayschoolFonts/Fredoka/TMP_Fredoka/Fredoka-Regular SDF.asset";
        private const string WinLosePrefab = "Assets/Prefabs/WinLose.prefab";
        private const string BackButtonSprite = "Assets/ImportedAssets/NewUI 2/BackButton.png";
        private const string ArtFolder = "Assets/ImportedAssets/ThreeInARow/";
        private const string BgSprite = ArtFolder + "Bg.png";
        private const string BoardSprite = ArtFolder + "Board.png";
        private const string GridSprite = ArtFolder + "Cross.png";
        private const string ShadowSprite = ArtFolder + "Shadow.png";
        private const string TugAssetsPath = "Assets/ScriptableObjects/AssetsSO.asset";
        private const string PinkuIcon = "Assets/ImportedAssets/NewUI 2/Pinku_Icon.png";
        private const string DogIcon = "Assets/ImportedAssets/NewUI 2/Dog_Icon.png";
        private const string CatIcon = "Assets/ImportedAssets/NewUI 2/Cat_Icon.png";
        private const string CatRig = "Assets/Scenes/Characters/Prefabs/Animals/Cat_Rig.prefab";
        private const string HighlightPrefab = "Assets/AssetKits/ParticleImage/Demo/Prefabs/Highlight.prefab";
        private const string LivesPillSprite = "Assets/ImportedAssets/NewUI 2/button.png";
        private const string HeartSprite = "Assets/ImportedAssets/NewUI 2/lifebutton.png";
        private const string ConfettiPrefab = "Assets/Prefabs/TugConfetti.prefab";
        private const string PinkuRig = "Assets/Scenes/Characters/Prefabs/Pinku/Pinku_Front.prefab";
        private const string DogRig = "Assets/Scenes/Characters/Prefabs/Animals/Dog_Rig.prefab";

        // Layout measured from the reference mock-up (Ref.png, 1920x1080): board plate 940, # lines 718 (native size),
        // squares 239, X / O glyph about 175 px (the sprites are 1024 px with padding, so the image is drawn at 320).
        private const float BoardSize = 940f;
        private const float GridSize = 718f;
        // Measured from Cross.png: the # lines are 36 px thick with centres 254 px apart (x = 231.5 and 485.5 of 718),
        // so every cell is 254 px and the tile / glow fit between the lines.
        private const float CellSize = 254f;
        private const float TileInset = 20f;                 // 254 - 2*20 = 214 px: clear of the 36 px lines
        private const float FaceSize = 180f;                 // Pinku / pet icons sit inside the 214 px tile
        private static readonly Color Violet = new Color(0.27f, 0.11f, 0.55f);

        private static Sprite square, circle;
        private static TMP_FontAsset fredoka;

        [MenuItem("Tools/Three In A Row/Build Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            square = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            fredoka = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FredokaPath);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ------------------------------------------------------------ Others: camera, light, event system
            var others = new GameObject("Others").transform;
            var cam = BuildCamera(others);
            var light = new GameObject("Global Light 2D", typeof(Light2D)).GetComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.transform.SetParent(others, false);
            var es = new GameObject("EventSystem", typeof(EventSystem));
            es.transform.SetParent(others, false);
            EnsureInputModule(es.GetComponent<EventSystem>());

            // ------------------------------------------------------------ MainCanvas
            var canvasGo = new GameObject("MainCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 100f;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var canvasRt = (RectTransform)canvasGo.transform;

            // ------------------------------------------------------------ Scripts (managers)
            var scripts = new GameObject("Scripts").transform;
            var gm = NewManager<GameManager>(scripts, "GameManager");
            var lm = NewManager<LevelManager>(scripts, "LevelManager");
            var controllerGo = new GameObject("ThreeInARow");
            controllerGo.transform.SetParent(scripts, false);
            var controller = controllerGo.AddComponent<ThreeInARowController>();
            var applier = controllerGo.AddComponent<ThreeAssetsApplier>();
            var sound = NewManager<SoundManager>(scripts, "SoundManager");
            var tutorial = NewManager<TutorialManager>(scripts, "TutorialManager");
            PopulateSounds(sound);

            // ------------------------------------------------------------ Background (replace the sprite in the editor)
            var bg = MakeImage(canvasRt, "BG", new Color(0.62f, 0.85f, 0.97f));
            Anchor(bg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            var bgFit = bg.gameObject.AddComponent<AspectRatioFitter>();   // fills the screen on every aspect ratio, never stretched
            bgFit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            bgFit.aspectRatio = 1920f / 1080f;
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgSprite);   // the Tug Battle background
            if (bgSprite != null) { bg.sprite = bgSprite; bg.type = Image.Type.Simple; bg.preserveAspect = false; bg.color = Color.white; }

            // ------------------------------------------------------------ Fit root: fills the canvas; ThreeInARowLayout anchors everything inside it
            var fitRoot = NewUI("FitRoot", canvasRt);
            Stretch(fitRoot);

            // ------------------------------------------------------------ Game
            var game = NewUI("ThreeGame", fitRoot);
            Stretch(game);

            var board = NewUI("Board", game);
            Anchor(board, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(BoardSize, BoardSize));
            var plate = MakeImage(board, "BoardPlate", Color.white);
            Stretch(plate.rectTransform);
            var plateSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BoardSprite);
            if (plateSprite != null) { plate.sprite = plateSprite; plate.type = Image.Type.Simple; plate.preserveAspect = true; }
            else plate.color = new Color(0.99f, 0.93f, 0.72f);

            var grid = MakeImage(board, "GridLines", Color.white);
            Anchor(grid.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(GridSize, GridSize));
            var gridSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GridSprite);
            if (gridSprite != null) { grid.sprite = gridSprite; grid.type = Image.Type.Simple; grid.preserveAspect = true; }
            else grid.color = new Color(0.72f, 0.3f, 0.05f, 0.9f);

            var cells = new CellView[9];
            for (int i = 0; i < 9; i++)
            {
                int row = i / 3, col = i % 3;
                var pos = new Vector2((col - 1) * CellSize, (1 - row) * CellSize);
                cells[i] = BuildCell(board, i, pos);
            }

            var bar = MakeImage(board, "WinLine", Color.white);
            Anchor(bar.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 30));
            var winLine = bar.gameObject.AddComponent<WinLineView>();
            Set(winLine, "bar", bar);

            // Pinku and the Dog: the animated rigs from Tug Battle, fitted into these rects (bottoms on the same line)
            var shadowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ShadowSprite);
            BuildShadow(game, "TapuShadow", new Vector2(-745, -365), new Vector2(270, 54), shadowSprite);
            BuildShadow(game, "PetShadow", new Vector2(740, -425), new Vector2(300, 54), shadowSprite);

            var tapuView = BuildSideCharacter(game, "Tapu", new Vector2(-745, -75), new Vector2(420, 600), new Color(1f, 0.7f, 0.3f), 1f,
                PinkuRig, false, "PinkuFrontIdle", "PinkuFrontCelebration", "");
            var petView = BuildSideCharacter(game, "Pet", new Vector2(745, -235), new Vector2(440, 380), new Color(0.97f, 0.55f, 0.6f), -1f,
                DogRig, true, "DogIdle", "DogHappy", "DogSadIdle");

            // ------------------------------------------------------------ Back button (Playschool)
            var backImg = MakeImage(fitRoot, "PlayschoolBackButton", Color.white);
            var backSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackButtonSprite);
            if (backSprite != null) { backImg.sprite = backSprite; backImg.type = Image.Type.Simple; backImg.preserveAspect = true; }
            else backImg.color = new Color(0.9f, 0.4f, 0.4f);
            backImg.raycastTarget = true;
            var backBtn = backImg.gameObject.AddComponent<Button>();
            backBtn.targetGraphic = backImg;
            backImg.rectTransform.anchorMin = backImg.rectTransform.anchorMax = new Vector2(0f, 1f);
            backImg.rectTransform.pivot = new Vector2(0f, 1f);
            backImg.rectTransform.anchoredPosition = new Vector2(50f, -35f);
            backImg.rectTransform.sizeDelta = new Vector2(100f, 100f);

            // ------------------------------------------------------------ HUD
            var hud = NewUI("ThreeHUD", fitRoot);
            Stretch(hud);
            var ui = hud.gameObject.AddComponent<ThreeInARowUI>();

            // no Tapu / Pet labels and no turn banner: the characters' animations say whose turn it is

            // lives: a white pill, top right - orange heart + the number of lives left in blue
            var pill = MakeImage(hud, "LivesPill", Color.white);
            var pillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LivesPillSprite);
            if (pillSprite != null) { pill.sprite = pillSprite; pill.type = Image.Type.Simple; }
            pill.raycastTarget = false;
            pill.rectTransform.anchorMin = pill.rectTransform.anchorMax = new Vector2(1f, 1f);
            pill.rectTransform.pivot = new Vector2(1f, 1f);
            pill.rectTransform.anchoredPosition = new Vector2(-50f, -35f);
            pill.rectTransform.sizeDelta = new Vector2(245f, 96f);   // the sprite's own size: white face with a soft shadow underneath
            var heart = MakeImage(pill.rectTransform, "Heart", Color.white);
            var heartSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HeartSprite);
            if (heartSprite != null) { heart.sprite = heartSprite; heart.type = Image.Type.Simple; heart.preserveAspect = true; }
            else heart.color = new Color(1f, 0.5f, 0f);
            heart.raycastTarget = false;
            Anchor(heart.rectTransform, new Vector2(0f, 0.5f), new Vector2(70f, 6f), new Vector2(64f, 64f));
            var livesText = MakeText(pill.rectTransform, "Count", "3", 56, new Color(0f, 0.47f, 0.82f), new Vector2(90f, 70f));
            Anchor(livesText.rectTransform, new Vector2(1f, 0.5f), new Vector2(-62f, 6f), new Vector2(90f, 70f));

            // confetti: the same ParticleImage prefab as Tug Battle (shower + burst)
            ParticleImageRefs confetti = default;
            var confettiAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ConfettiPrefab);
            if (confettiAsset != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(confettiAsset, hud);
                inst.name = "ThreeConfetti";
                var rt = (RectTransform)inst.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                confetti.shower = inst.transform.Find("Confetti_Shower")?.GetComponent<AssetKits.ParticleImage.ParticleImage>();
                confetti.burst = inst.transform.Find("Confetti_Burst")?.GetComponent<AssetKits.ParticleImage.ParticleImage>();
            }
            else Debug.LogWarning("ThreeInARowSceneBuilder: " + ConfettiPrefab + " not found - no confetti.");

            // tutorial hand
            var handImg = MakeImage(hud, "TutorialHand", new Color(0.96f, 0.79f, 0.64f));
            handImg.sprite = circle; handImg.type = Image.Type.Simple;
            Anchor(handImg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
            var handGroup = handImg.gameObject.AddComponent<CanvasGroup>();
            handGroup.interactable = false; handGroup.blocksRaycasts = false;
            var ripple = MakeImage(handImg.rectTransform, "Ripple", new Color(1, 1, 1, 0));
            ripple.sprite = circle; ripple.type = Image.Type.Simple;
            Anchor(ripple.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130, 130));
            var handLabel = MakeText(handImg.rectTransform, "Label", "TAP", 44, new Color(0.35f, 0.25f, 0.2f), new Vector2(140, 60));
            Anchor(handLabel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 60));
            handImg.gameObject.SetActive(false);

            // ------------------------------------------------------------ WinLose (your prefab, swapped to this namespace)
            var endPanel = BuildEndPanel(canvasRt);

            // ------------------------------------------------------------ AssetsSO
            var assetsSO = EnsureAssetsSO();

            // ------------------------------------------------------------ wire
            Set(ui, "boardRoot", board);
            Set(ui, "livesHeart", heart);
            Set(ui, "livesText", livesText);
            Set(ui, "confettiShower", confetti.shower);
            Set(ui, "confettiBurst", confetti.burst);

            Set(tutorial, "hand", handImg.rectTransform);
            Set(tutorial, "group", handGroup);
            Set(tutorial, "ripple", ripple.rectTransform);
            Set(tutorial, "rippleImage", ripple);

            Set(controller, "ui", ui);
            SetArray(controller, "cells", cells);
            Set(controller, "winLine", winLine);
            Set(controller, "tapu", tapuView);
            Set(controller, "pet", petView);

            Set(gm, "levelManager", lm);
            Set(gm, "soundManager", sound);
            Set(gm, "boardController", controller);
            Set(gm, "endPanelScript", endPanel);
            Set(gm, "assets", assetsSO);
            Set(gm, "tutorialManager", tutorial);

            Set(lm, "playSchoolBackButton", backBtn);

            Set(applier, "gameManager", gm);
            Set(applier, "boardPlate", plate);
            Set(applier, "gridImage", grid);
            SetArray(applier, "cells", cells);
            Set(applier, "tapu", tapuView);
            Set(applier, "pet", petView);
            Set(applier, "winLine", winLine);
            Set(applier, "tutorial", tutorial);

            endPanel.transform.SetAsLastSibling();

            ThreeInARowLayout.Apply(canvasRt);   // responsive anchors for the board, characters and HUD

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeObject = assetsSO;
            Debug.Log("Three In A Row built and saved to " + ScenePath + ". Fill " + AssetsPath + " with sprites, then press Play.");
        }

        // ================================================================== builders

        private static Camera BuildCamera(Transform parent)
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(0, 0, -10);
            var cam = go.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.192f, 0.302f, 0.475f, 0f);
            go.AddComponent<UniversalAdditionalCameraData>();
            return cam;
        }

        private static CellView BuildCell(Transform board, int index, Vector2 pos)
        {
            // the cell itself is an invisible tap area on the # grid
            var root = MakeImage(board, "Cell" + index, new Color(1f, 1f, 1f, 0f));
            Anchor(root.rectTransform, new Vector2(0.5f, 0.5f), pos, new Vector2(CellSize, CellSize));
            root.raycastTarget = true;
            var btn = root.gameObject.AddComponent<Button>();
            btn.targetGraphic = root;
            btn.transition = Selectable.Transition.None;

            // the tile: the cell colour (clear until someone plays here), inset so the # lines stay visible
            var tile = MakeImage(root.rectTransform, "Tile", new Color(1f, 1f, 1f, 0f));
            Stretch(tile.rectTransform, TileInset);

            var glow = MakeImage(root.rectTransform, "Glow", new Color(1f, 0.95f, 0.45f, 0f));
            Stretch(glow.rectTransform, TileInset);
            glow.gameObject.SetActive(false);

            var face = MakeImage(root.rectTransform, "Face", Color.white);
            face.sprite = circle; face.type = Image.Type.Simple; face.preserveAspect = true;
            Anchor(face.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(FaceSize, FaceSize));
            face.gameObject.SetActive(false);

            // the AssetKits Highlight particle image, played over the cell when the player puts a face here
            AssetKits.ParticleImage.ParticleImage highlight = null;
            var hp = AssetDatabase.LoadAssetAtPath<GameObject>(HighlightPrefab);
            if (hp != null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(hp, root.rectTransform);
                inst.name = "Highlight";
                var hrt = (RectTransform)inst.transform;
                hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.5f);
                hrt.anchoredPosition = Vector2.zero;
                hrt.localScale = Vector3.one * 1.6f;   // the prefab scatters sparkles around its centre; keep them near the cell
                highlight = inst.GetComponent<AssetKits.ParticleImage.ParticleImage>();
            }

            var view = root.gameObject.AddComponent<CellView>();
            Set(view, "button", btn);
            Set(view, "tile", tile);
            Set(view, "face", face);
            Set(view, "glow", glow);
            Set(view, "highlight", highlight);
            return view;
        }

        private static void BuildShadow(Transform parent, string name, Vector2 pos, Vector2 size, Sprite sprite)
        {
            var img = MakeImage(parent, name, new Color(1f, 1f, 1f, 0.5f));
            if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Simple; img.preserveAspect = false; }
            else img.color = new Color(0f, 0f, 0f, 0.25f);
            Anchor(img.rectTransform, new Vector2(0.5f, 0.5f), pos, size);
        }

        private struct ParticleImageRefs
        {
            public AssetKits.ParticleImage.ParticleImage shower, burst;
        }

        private static SideCharacter BuildSideCharacter(Transform parent, string name, Vector2 pos, Vector2 size, Color color, float facing,
            string rigPath, bool flip, string idle, string win, string sad)
        {
            var body = MakeImage(parent, name, color);
            Anchor(body.rectTransform, new Vector2(0.5f, 0.5f), pos, size);
            var label = MakeText(body.rectTransform, "Label", name, 54, Color.white, size);
            Stretch(label.rectTransform);
            var view = body.gameObject.AddComponent<SideCharacter>();
            Set(view, "body", body);
            SetFloat(view, "facing", facing);
            Set(view, "rigPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(rigPath));
            SetBool(view, "flipRig", flip);
            SetString(view, "idleState", idle);
            SetString(view, "winState", win);
            SetString(view, "sadState", sad);
            if (name == "Pet")   // Playschool Main: the cat rig is used when the selected pet is the cat
            {
                SetBool(view, "isPet", true);
                Set(view, "catRigPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(CatRig));
            }
            return view;
        }

        private static RectTransform BuildBadge(Transform parent, string name, string text, Vector2 pos, Color color, out Image glow)
        {
            var badge = MakeImage(parent, name, color);
            Anchor(badge.rectTransform, new Vector2(0.5f, 0.5f), pos, new Vector2(300, 100));
            glow = MakeImage(badge.rectTransform, "Glow", new Color(1f, 1f, 0.7f, 0.9f));
            Stretch(glow.rectTransform, -12f);
            glow.transform.SetAsFirstSibling();
            glow.gameObject.SetActive(false);
            var label = MakeText(badge.rectTransform, "Text", text, 56, Violet, new Vector2(280, 80));
            Stretch(label.rectTransform, 10f);
            return badge.rectTransform;
        }

        // Instantiates the user's WinLose prefab and swaps its Tug Battle EndPanelScript for this game's, keeping every reference.
        private static EndPanelScript BuildEndPanel(Transform canvas)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WinLosePrefab);
            if (prefab == null)
            {
                Debug.LogError("ThreeInARowSceneBuilder: " + WinLosePrefab + " not found - add your WinLose panel and an EndPanelScript by hand.");
                return null;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            go.name = "WinLose";

            var old = go.GetComponent<tmkoc.tugofwar.EndPanelScript>();
            var fresh = go.AddComponent<EndPanelScript>();
            if (old != null)
            {
                var from = new SerializedObject(old);
                var to = new SerializedObject(fresh);
                foreach (var name in new[] { "winPanel", "losePanel", "homeButtons", "winRaysT", "loseRaysT", "onScreenAnchoredPos",
                                             "slideInDuration", "slideOutDuration", "slideInEase", "slideOutEase", "minDisplayTime" })
                {
                    var src = from.FindProperty(name);
                    var dst = to.FindProperty(name);
                    if (src != null && dst != null) to.CopyFromSerializedProperty(src);
                }
                to.ApplyModifiedPropertiesWithoutUndo();
                Object.DestroyImmediate(old);
            }
            return fresh;
        }

        private static AssetsSO EnsureAssetsSO()
        {
            var so = AssetDatabase.LoadAssetAtPath<AssetsSO>(AssetsPath);
            if (so == null)
            {
                Directory.CreateDirectory("Assets/ScriptableObjects");
                so = ScriptableObject.CreateInstance<AssetsSO>();
                AssetDatabase.CreateAsset(so, AssetsPath);
            }

            // The ThreeInARow art folder: board plate and # lines. The cell faces are Pinku's, the dog's and the cat's round icons;
            // the tutorial hand comes from Tug Battle.
            var tug = AssetDatabase.LoadAssetAtPath<tmkoc.tugofwar.AssetsSO>(TugAssetsPath);
            so.board = AssetDatabase.LoadAssetAtPath<Sprite>(BoardSprite);
            so.gridLines = AssetDatabase.LoadAssetAtPath<Sprite>(GridSprite);
            so.tapuFace = AssetDatabase.LoadAssetAtPath<Sprite>(PinkuIcon);
            so.petFace = AssetDatabase.LoadAssetAtPath<Sprite>(DogIcon);
            so.petFaceCat = AssetDatabase.LoadAssetAtPath<Sprite>(CatIcon);
            if (so.tutorialHand == null && tug != null) so.tutorialHand = tug.tutorialHand;
            EditorUtility.SetDirty(so);
            AssetDatabase.SaveAssets();
            return so;
        }

        // Creates the six SFX rows and reuses the BGM already used by Tug Battle, if it is in the project.
        private static void PopulateSounds(SoundManager sound)
        {
            var m = typeof(SoundManager).GetMethod("PopulateAll", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (m != null) m.Invoke(sound, null);

            foreach (var guid in AssetDatabase.FindAssets("Chetan Game BGM t:AudioClip"))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                if (clip == null) continue;
                Set(sound, "bgm", clip);
                break;
            }
        }

        // ================================================================== small helpers

        private static T NewManager<T>(Transform parent, string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        private static RectTransform NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        }

        private static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static Image MakeImage(Transform parent, string name, Color color)
        {
            var rt = NewUI(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = square;
            img.type = Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Fredoka, no wrapping, auto-sized to fit its box (never larger than <paramref name="size"/>). No outline, no shadow.</summary>
        private static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color, Vector2 box)
        {
            var rt = NewUI(name, parent);
            rt.sizeDelta = box;
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (fredoka != null) t.font = fredoka;
            t.text = text;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = Mathf.Max(12f, size * 0.4f);
            t.fontSize = size;
            return t;
        }

        // The project uses the legacy Input Manager; make sure the EventSystem has a module that works with it.
        private static void EnsureInputModule(EventSystem es)
        {
#if ENABLE_LEGACY_INPUT_MANAGER && !ENABLE_INPUT_SYSTEM
            foreach (var m in es.GetComponents<BaseInputModule>())
                if (m.GetType().Name == "InputSystemUIInputModule") Object.DestroyImmediate(m);
            if (es.GetComponent<StandaloneInputModule>() == null) es.gameObject.AddComponent<StandaloneInputModule>();
#else
            if (es.GetComponent<BaseInputModule>() == null) es.gameObject.AddComponent<StandaloneInputModule>();
#endif
        }

        private static void Set(Object target, string prop, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogError("ThreeInARowSceneBuilder: no field '" + prop + "' on " + target.GetType().Name); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string prop, float value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogError("ThreeInARowSceneBuilder: no field '" + prop + "' on " + target.GetType().Name); return; }
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string prop, bool value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogError("ThreeInARowSceneBuilder: no field '" + prop + "' on " + target.GetType().Name); return; }
            p.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string prop, string value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogError("ThreeInARowSceneBuilder: no field '" + prop + "' on " + target.GetType().Name); return; }
            p.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string prop, params Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogError("ThreeInARowSceneBuilder: no field '" + prop + "' on " + target.GetType().Name); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
