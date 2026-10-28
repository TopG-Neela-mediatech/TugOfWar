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
        private const string BackButtonSprite = "Assets/ImportedAssets/NewUI 1/NewUI/BackButton.png";
        private const string BgSprite = "Assets/ImportedAssets/0MainAssets/BG 2.jpg";
        private const string TugAssetsPath = "Assets/ScriptableObjects/AssetsSO.asset";
        private const string PinkuIcon = "Assets/ImportedAssets/NewUI 1/NewUI/Pinku_Icon.png";
        private const string DogIcon = "Assets/ImportedAssets/NewUI 1/NewUI/Dog_Icon.png";
        private const string ConfettiPrefab = "Assets/Prefabs/TugConfetti.prefab";
        private const string PinkuRig = "Assets/Scenes/Characters/Prefabs/Pinku/Pinku_Front.prefab";
        private const string DogRig = "Assets/Scenes/Characters/Prefabs/Animals/Dog_Rig.prefab";

        private const float CellSize = 230f;
        private const float CellGap = 22f;
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
            Stretch(bg.rectTransform);
            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgSprite);   // the Tug Battle background
            if (bgSprite != null) { bg.sprite = bgSprite; bg.type = Image.Type.Simple; bg.preserveAspect = false; bg.color = Color.white; }

            // ------------------------------------------------------------ Game
            var game = NewUI("ThreeGame", canvasRt);
            Stretch(game);

            float boardSize = CellSize * 3 + CellGap * 4;
            var board = NewUI("Board", game);
            Anchor(board, new Vector2(0.5f, 0.5f), new Vector2(0, -25), new Vector2(boardSize, boardSize));
            var plate = MakeImage(board, "BoardPlate", new Color(0.99f, 0.93f, 0.72f));
            Stretch(plate.rectTransform);

            var cells = new CellView[9];
            for (int i = 0; i < 9; i++)
            {
                int row = i / 3, col = i % 3;
                var pos = new Vector2((col - 1) * (CellSize + CellGap), (1 - row) * (CellSize + CellGap));
                cells[i] = BuildCell(board, i, pos);
            }

            var bar = MakeImage(board, "WinLine", Color.white);
            Anchor(bar.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 30));
            var winLine = bar.gameObject.AddComponent<WinLineView>();
            Set(winLine, "bar", bar);

            // Pinku and the Dog: the animated rigs from Tug Battle, fitted into these rects (bottoms on the same line)
            var tapuView = BuildSideCharacter(game, "Tapu", new Vector2(-690, -90), new Vector2(380, 540), new Color(1f, 0.7f, 0.3f), 1f,
                PinkuRig, false, "PinkuFrontIdle", "PinkuFrontCelebration", "");
            var petView = BuildSideCharacter(game, "Pet", new Vector2(690, -195), new Vector2(400, 330), new Color(0.97f, 0.55f, 0.6f), -1f,
                DogRig, true, "DogIdle", "DogHappy", "DogSadIdle");

            // ------------------------------------------------------------ Back button (Playschool)
            var backImg = MakeImage(canvasRt, "PlayschoolBackButton", Color.white);
            var backSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackButtonSprite);
            if (backSprite != null) { backImg.sprite = backSprite; backImg.type = Image.Type.Simple; backImg.preserveAspect = true; }
            else backImg.color = new Color(0.9f, 0.4f, 0.4f);
            backImg.raycastTarget = true;
            var backBtn = backImg.gameObject.AddComponent<Button>();
            backBtn.targetGraphic = backImg;
            backImg.rectTransform.anchorMin = backImg.rectTransform.anchorMax = new Vector2(0f, 1f);
            backImg.rectTransform.pivot = new Vector2(0f, 1f);
            backImg.rectTransform.anchoredPosition = new Vector2(30f, -30f);
            backImg.rectTransform.sizeDelta = new Vector2(125f, 125f);

            // ------------------------------------------------------------ HUD
            var hud = NewUI("ThreeHUD", canvasRt);
            Stretch(hud);
            var ui = hud.gameObject.AddComponent<ThreeInARowUI>();

            var tapuBadge = BuildBadge(hud, "TapuBadge", "Tapu", new Vector2(-690, 330), new Color(1f, 0.85f, 0.35f), out var tapuGlow);
            var petBadge = BuildBadge(hud, "PetBadge", "Pet", new Vector2(690, 330), new Color(1f, 0.6f, 0.65f), out var petGlow);

            var banner = MakeImage(hud, "Banner", new Color(1f, 1f, 1f, 0.92f));
            Anchor(banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 470), new Vector2(560, 110));
            var bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.interactable = false; bannerGroup.blocksRaycasts = false;
            var bannerText = MakeText(banner.rectTransform, "Text", "Your turn!", 64, Violet, new Vector2(520, 90));
            Stretch(bannerText.rectTransform, 12f);
            banner.gameObject.SetActive(false);

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
            Set(ui, "tapuBadge", tapuBadge);
            Set(ui, "petBadge", petBadge);
            Set(ui, "tapuBadgeGlow", tapuGlow);
            Set(ui, "petBadgeGlow", petGlow);
            Set(ui, "banner", banner.rectTransform);
            Set(ui, "bannerText", bannerText);
            Set(ui, "bannerGroup", bannerGroup);
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
            SetArray(applier, "cells", cells);
            Set(applier, "tapu", tapuView);
            Set(applier, "pet", petView);
            Set(applier, "winLine", winLine);
            Set(applier, "tutorial", tutorial);

            endPanel.transform.SetAsLastSibling();

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
            var sq = MakeImage(board, "Cell" + index, new Color(1f, 1f, 1f, 0.95f));
            Anchor(sq.rectTransform, new Vector2(0.5f, 0.5f), pos, new Vector2(CellSize, CellSize));
            sq.raycastTarget = true;
            var btn = sq.gameObject.AddComponent<Button>();
            btn.targetGraphic = sq;
            btn.transition = Selectable.Transition.None;

            var glow = MakeImage(sq.rectTransform, "Glow", new Color(1f, 0.95f, 0.45f, 0f));
            Stretch(glow.rectTransform, -14f);
            glow.gameObject.SetActive(false);

            var face = MakeImage(sq.rectTransform, "Face", Color.white);
            face.sprite = circle; face.type = Image.Type.Simple; face.preserveAspect = true;
            Anchor(face.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CellSize * 0.78f, CellSize * 0.78f));
            face.gameObject.SetActive(false);

            var view = sq.gameObject.AddComponent<CellView>();
            Set(view, "button", btn);
            Set(view, "square", sq);
            Set(view, "face", face);
            Set(view, "glow", glow);
            return view;
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

            // Empty slots are filled with Tug Battle art: Pinku's and the dog's round icons for the face markers
            // (the dog's Tug faces are biting the rope), and the tutorial hand.
            var tug = AssetDatabase.LoadAssetAtPath<tmkoc.tugofwar.AssetsSO>(TugAssetsPath);
            if (so.tapuFace == null) so.tapuFace = AssetDatabase.LoadAssetAtPath<Sprite>(PinkuIcon);
            if (so.petFace == null) so.petFace = AssetDatabase.LoadAssetAtPath<Sprite>(DogIcon);
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
