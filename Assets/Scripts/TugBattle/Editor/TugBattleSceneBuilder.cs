#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace tmkoc.tugofwar.EditorTools
{
    /// <summary>
    /// Tools > Tug Battle > Build Scene
    /// Builds the ENTIRE game as UI under MainCanvas (1920x1080 reference): background, ground, win lines, rope, flag,
    /// Player, Pet, HUD, tap area and the tutorial hand. Uses placeholder shapes, creates
    /// Assets/ScriptableObjects/AssetsSO.asset and wires every reference. Your own BG and WinLose (EndPanelScript)
    /// objects are reused if they exist. Safe to run again: it rebuilds what it made (your AssetsSO is kept).
    /// </summary>
    public static class TugBattleSceneBuilder
    {
        private const string GameName = "TugGame";
        private const string OldWorldName = "TugBattleWorld";
        private const string HudName = "TugHUD";
        private const string TapAreaName = "TapArea";
        private const string TutorialName = "TutorialManager";
        private const string AssetsPath = "Assets/ScriptableObjects/AssetsSO.asset";
        private const string FredokaPath = "Assets/APIs/PlayschoolFonts/Fredoka/TMP_Fredoka/Fredoka-Regular SDF.asset";

        // Canvas pixels (HTML prototype 1600x900 scaled by 1.2 to the 1920x1080 reference)
        private const float WinLineX = 264f;
        private const float PlayerStartX = -528f;
        private const float PetStartX = 528f;
        private const float FeetY = -330f;
        private const float HandsY = -210f;
        private const float HalfH = 540f;   // half the 1080 reference height: converts a centre-based y into a bottom-based y

        private static Sprite square;
        private static Sprite circle;
        private static TMP_FontAsset fredoka;

        [MenuItem("Tools/Tug Battle/Build Scene")]
        public static void Build()
        {
            square = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            fredoka = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FredokaPath);

            if (!EditorUtility.DisplayDialog("Build Tug Battle scene", "This rebuilds TugGame, TugHUD, TapArea and the TutorialManager and discards manual edits made to them (sprites set directly on those objects, deleted objects, etc.). Your AssetsSO, BG and WinLose are kept. Continue?", "Rebuild", "Cancel"))
                return;

            var gm = Object.FindFirstObjectByType<GameManager>();
            var lm = Object.FindFirstObjectByType<LevelManager>();
            var battle = Object.FindFirstObjectByType<TugBattleController>();
            var vfx = Object.FindFirstObjectByType<TugBattleVfx>();
            var sound = Object.FindFirstObjectByType<SoundManager>();
            var cam = Camera.main;
            var canvas = FindMainCanvas();
            if (gm == null || lm == null || battle == null || vfx == null || sound == null || cam == null || canvas == null)
            {
                Debug.LogError("TugBattleSceneBuilder: scene is missing GameManager / LevelManager / TugBattle (controller + vfx) / SoundManager / Main Camera / MainCanvas.");
                return;
            }

            EndPanelScript endPanel = Object.FindFirstObjectByType<EndPanelScript>();
            if (endPanel == null)
            {
                Debug.LogError("TugBattleSceneBuilder: no EndPanelScript (your WinLose panel) in the scene.");
                return;
            }

            DestroyIfExists(OldWorldName); DestroyIfExists(GameName); DestroyIfExists(HudName);
            DestroyIfExists(TapAreaName); DestroyIfExists("EndPanel"); DestroyIfExists(TutorialName);

            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null) canvas.worldCamera = cam;
            EnsureInputModule();
            var assetsSO = EnsureAssetsSO();
            var canvasRt = (RectTransform)canvas.transform;

            // ------------------------------------------------------------ tap area (behind everything, catches taps)
            var tapArea = NewUI(TapAreaName, canvasRt);
            Stretch(tapArea);
            var tapImg = tapArea.gameObject.AddComponent<Image>();
            tapImg.color = new Color(1, 1, 1, 0);
            tapImg.raycastTarget = true;
            tapArea.SetAsFirstSibling();
            var tapInput = tapArea.gameObject.AddComponent<TapInput>();
            Set(tapInput, "controller", battle);

            // ------------------------------------------------------------ game (all UI)
            var userBg = canvasRt.Find("BG") as RectTransform;
            var game = NewUI(GameName, canvasRt);
            Stretch(game);
            game.SetSiblingIndex(1);

            Image bg;
            if (userBg != null)
            {
                bg = userBg.GetComponent<Image>();
                userBg.SetSiblingIndex(1);   // behind the game
                game.SetSiblingIndex(2);
            }
            else
            {
                bg = Box(game, "Background", Vector2.zero, Vector2.zero, new Color(0.56f, 0.82f, 0.95f));
                Stretch(bg.rectTransform);
            }
            var ground = Box(game, "Ground", Vector2.zero, Vector2.zero, new Color(0.45f, 0.75f, 0.4f));
            ground.rectTransform.anchorMin = new Vector2(0, 0);
            ground.rectTransform.anchorMax = new Vector2(1, 0);
            ground.rectTransform.pivot = new Vector2(0.5f, 0);
            ground.rectTransform.offsetMin = Vector2.zero;
            ground.rectTransform.offsetMax = new Vector2(0, 220f);
            ground.gameObject.SetActive(false);   // the HTML ground is part of the background art

            var playerLine = Box(game, "PlayerWinLine", new Vector2(-WinLineX, 257), new Vector2(28, 254), new Color(0.3f, 0.6f, 1f));
            BottomCentre(playerLine.rectTransform);
            var petLine = Box(game, "PetWinLine", new Vector2(WinLineX, 257), new Vector2(28, 254), new Color(1f, 0.3f, 0.45f));
            BottomCentre(petLine.rectTransform);

            var group = NewUI("RopeGroup", game);
            Anchor(group, new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);   // bottom-centre: stays on the ground on every aspect ratio

            var rope = Box(group, "Rope", new Vector2(-25, 336), new Vector2(850, 24), new Color(0.75f, 0.5f, 0.25f));

            var flag = Box(group, "Flag", new Vector2(0, 336), new Vector2(62, 94), new Color(1f, 0.3f, 0.35f));
            flag.rectTransform.pivot = new Vector2(0.5f, 1f);   // hangs from the rope, sways from the knot

            var playerView = BuildCharacter(group, "Player", new Vector2(PlayerStartX + 18f, 396), new Vector2(260, 384), new Color(1f, 0.44f, 0.57f), 0f, 1);
            var petView = BuildCharacter(group, "Pet", new Vector2(PetStartX, 321), new Vector2(313, 234), new Color(0.97f, 0.65f, 0.3f), -80f, -1);

            var ropeView = group.gameObject.AddComponent<RopeView>();
            Set(ropeView, "ropeRoot", group);
            Set(ropeView, "flag", flag.rectTransform);
            Set(ropeView, "flagImage", flag);
            Set(ropeView, "ropeImage", rope);
            SetFloat(ropeView, "maxOffset", WinLineX);

            // ------------------------------------------------------------ HUD
            var hud = NewUI(HudName, canvasRt);
            Stretch(hud);
            var ui = hud.gameObject.AddComponent<TugBattleUI>();

            var meter = NewUI("PowerMeter", hud);
            Anchor(meter, new Vector2(0.5f, 0f), new Vector2(0, 71), new Vector2(576, 58));
            var glow = MakeImage(meter, "Glow", new Color(1f, 0.85f, 0.2f, 0.45f));
            Stretch(glow.rectTransform, -14);
            glow.gameObject.SetActive(false);
            var meterBg = MakeImage(meter, "Bg", new Color(0.15f, 0.12f, 0.25f, 0.8f));
            Stretch(meterBg.rectTransform);
            var fill = MakeImage(meter, "Fill", new Color(1f, 0.55f, 0.2f));
            Stretch(fill.rectTransform, 6);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            var meterLabel = MakeText(meter, "Label", "POWER", 34, Color.white, new Vector2(300, 50));
            Anchor(meterLabel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 50));

            // countdown (text)
            var countdownText = MakeText(hud, "Countdown", "3", 307, new Color(1f, 0.95f, 0.4f), new Vector2(700, 420));
            Anchor(countdownText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 115), new Vector2(700, 420));
            countdownText.fontStyle = FontStyles.Bold;
            countdownText.gameObject.SetActive(false);

            // banners (text)
            string[] bannerText = { "TAP FAST!", "KEEP TAPPING!", "STRONG PULL!", "PET TIRED! TAP NOW!", "POWER PULL!" };
            Color[] bannerColor = { new Color(1f, 0.3f, 0.4f), new Color(0.3f, 0.8f, 0.4f), new Color(1f, 0.5f, 0.1f), new Color(0.3f, 0.65f, 1f), new Color(1f, 0.8f, 0.1f) };
            var banners = new RectTransform[5];
            for (int i = 0; i < 5; i++)
            {
                var bt = MakeText(hud, "Banner_" + (BannerKind)i, bannerText[i], i == 4 ? 123 : 100, bannerColor[i], new Vector2(1500, 130));
                bt.fontStyle = FontStyles.Bold;
                Anchor(bt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(1500, 130));   // top-centre
                bt.gameObject.SetActive(false);
                banners[i] = bt.rectTransform;
            }

            // pops
            var popParent = NewUI("PopParent", hud);
            Stretch(popParent);
            var popAnchor = NewUI("PopAnchor", hud);
            Anchor(popAnchor, new Vector2(0.5f, 0f), new Vector2(-438, 648), new Vector2(10, 10));   // bottom-centre, above the player
            string[] popText = { "Good!", "Great!", "Super Pull!" };
            Color[] popColor = { new Color(0.3f, 0.65f, 1f), new Color(0.3f, 0.8f, 0.4f), new Color(1f, 0.3f, 0.4f) };
            var popPrefabs = new RectTransform[3];
            for (int i = 0; i < 3; i++) popPrefabs[i] = MakePopPrefab("Pop_" + (PopKind)i, popText[i], popColor[i], 53 + i * 9.5f);

            WireUI(ui, countdownText.rectTransform, countdownText, banners, popPrefabs, popParent, popAnchor, fill, glow.gameObject, meter);
            SetFloat(ui, "popRise", 76f);   // HTML: floats rise 70 px/s for 0.9 s at 1.2x

            // ------------------------------------------------------------ tutorial hand (UI) + manager
            var handImg = MakeImage(hud, "TutorialHand", new Color(0.96f, 0.79f, 0.64f));
            handImg.sprite = circle;
            handImg.type = UnityEngine.UI.Image.Type.Simple;
            Anchor(handImg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, -150 + HalfH), new Vector2(150, 150));
            var handGroup = handImg.gameObject.AddComponent<CanvasGroup>();
            handGroup.interactable = false; handGroup.blocksRaycasts = false;
            var rippleImg = MakeImage(handImg.rectTransform, "Ripple", new Color(1, 1, 1, 0));
            rippleImg.sprite = circle;
            rippleImg.type = UnityEngine.UI.Image.Type.Simple;
            Anchor(rippleImg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(130, 130));
            var handLabel = MakeText(handImg.rectTransform, "Label", "TAP", 44, new Color(0.35f, 0.25f, 0.2f), new Vector2(140, 60));
            Anchor(handLabel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 60));
            handImg.gameObject.SetActive(false);

            var tutGo = new GameObject(TutorialName);
            tutGo.transform.SetParent(sound.transform.parent, false);
            var tutorial = tutGo.AddComponent<TutorialManager>();
            Set(tutorial, "hand", handImg.rectTransform);
            Set(tutorial, "group", handGroup);
            Set(tutorial, "ripple", rippleImg.rectTransform);
            Set(tutorial, "rippleImage", rippleImg);

            // ------------------------------------------------------------ end panel: keep the user's, draw on top
            endPanel.transform.SetAsLastSibling();

            // ------------------------------------------------------------ wire managers
            Set(battle, "ropeView", ropeView);
            Set(battle, "player", playerView);
            Set(battle, "pet", petView);
            Set(battle, "ui", ui);
            Set(battle, "vfx", vfx);

            Set(vfx, "worldCamera", cam);
            Set(vfx, "shakeTarget", game);
            Set(vfx, "playerWinLine", playerLine.transform);
            Set(vfx, "petWinLine", petLine.transform);

            Set(gm, "levelManager", lm);
            Set(gm, "soundManager", sound);
            Set(gm, "battleController", battle);
            Set(gm, "endPanelScript", endPanel);
            Set(gm, "assets", assetsSO);
            Set(gm, "tutorialManager", tutorial);

            var applier = battle.GetComponent<TugAssetsApplier>();
            if (applier == null) applier = battle.gameObject.AddComponent<TugAssetsApplier>();
            Set(applier, "gameManager", gm);
            Set(applier, "player", playerView);
            Set(applier, "pet", petView);
            Set(applier, "tutorial", tutorial);

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeObject = assetsSO;
            Debug.Log("Tug Battle built under MainCanvas and saved. Fill Assets/ScriptableObjects/AssetsSO with sprites, then press Play.");
        }

        // ================================================================== assets

        private static AssetsSO EnsureAssetsSO()
        {
            var existing = AssetDatabase.LoadAssetAtPath<AssetsSO>(AssetsPath);
            if (existing != null) return existing;

            Directory.CreateDirectory("Assets/ScriptableObjects");
            var so = ScriptableObject.CreateInstance<AssetsSO>();
            AssetDatabase.CreateAsset(so, AssetsPath);
            AssetDatabase.SaveAssets();
            return so;
        }

        // ================================================================== builders

        private static CharacterView BuildCharacter(Transform parent, string name, Vector2 pos, Vector2 bodySize, Color color, float slideX, int facing)
        {
            // Single sprite character: just a body Image (placeholder colour) - no head or eyes.
            var body = Box(parent, name, pos, bodySize, color);
            var view = body.gameObject.AddComponent<CharacterView>();
            Set(view, "uiImage", body);
            Set(view, "punchTarget", body.rectTransform);
            SetFloat(view, "jumpHeight", 50f);
            SetFloat(view, "slideX", slideX);
            SetFloat(view, "leanDirection", facing);   // player (left) = +1, pet (right) = -1
            return view;
        }

        private static RectTransform MakePopPrefab(string name, string text, Color color, float size)
        {
            string dir = "Assets/Prefabs/TugPops";
            Directory.CreateDirectory(dir);
            var t = MakeText(null, name, text, size, color, new Vector2(600, 140));
            t.fontStyle = FontStyles.Bold;
            Anchor(t.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 140));
            t.gameObject.AddComponent<CanvasGroup>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(t.gameObject, dir + "/" + name + ".prefab");
            Object.DestroyImmediate(t.gameObject);
            return (RectTransform)prefab.transform;
        }

        private static void WireUI(TugBattleUI ui, RectTransform countdownRoot, TMP_Text countdownText, RectTransform[] banners, RectTransform[] pops,
            RectTransform popParent, RectTransform popAnchor, Image fill, GameObject glow, RectTransform meter)
        {
            var so = new SerializedObject(ui);
            so.FindProperty("countdownRoot").objectReferenceValue = countdownRoot;
            so.FindProperty("countdownText").objectReferenceValue = countdownText;
            var b = so.FindProperty("banners");
            b.arraySize = banners.Length;
            for (int i = 0; i < banners.Length; i++)
            {
                var e = b.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("kind").enumValueIndex = i;
                e.FindPropertyRelative("root").objectReferenceValue = banners[i];
            }
            var p = so.FindProperty("pops");
            p.arraySize = pops.Length;
            for (int i = 0; i < pops.Length; i++)
            {
                var e = p.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("kind").enumValueIndex = i;
                e.FindPropertyRelative("prefab").objectReferenceValue = pops[i];
            }
            so.FindProperty("popParent").objectReferenceValue = popParent;
            so.FindProperty("popAnchor").objectReferenceValue = popAnchor;
            so.FindProperty("powerFill").objectReferenceValue = fill;
            so.FindProperty("powerFullGlow").objectReferenceValue = glow;
            so.FindProperty("powerMeterRoot").objectReferenceValue = meter;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ================================================================== small helpers

        /// <summary>Placeholder rectangle/circle: Image centred at an anchored position.</summary>
        private static Image Box(Transform parent, string name, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var img = MakeImage(parent, name, color);
            if (sprite != null) { img.sprite = sprite; img.type = UnityEngine.UI.Image.Type.Simple; }
            Anchor(img.rectTransform, new Vector2(0.5f, 0.5f), pos, size);
            return img;
        }

        private static void BottomCentre(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
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
            img.type = UnityEngine.UI.Image.Type.Sliced;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Fredoka, no wrapping, auto-sized to fit its box (never larger than <paramref name="size"/>).</summary>
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

        private static Canvas FindMainCanvas()
        {
            var go = GameObject.Find("MainCanvas");
            return go != null ? go.GetComponent<Canvas>() : Object.FindFirstObjectByType<Canvas>();
        }

        private static void DestroyIfExists(string name)
        {
            var go = GameObject.Find(name);
            while (go != null) { Object.DestroyImmediate(go); go = GameObject.Find(name); }
        }

        // The project uses the legacy Input Manager; make sure the EventSystem has a module that works with it.
        private static void EnsureInputModule()
        {
            var es = Object.FindFirstObjectByType<EventSystem>();
            if (es == null) es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
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
            if (p == null) { Debug.LogError("TugBattleSceneBuilder: no field '" + prop + "' on " + target.GetType().Name); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string prop, float value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogError("TugBattleSceneBuilder: no field '" + prop + "' on " + target.GetType().Name); return; }
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string prop, params Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(prop);
            if (p == null) { Debug.LogError("TugBattleSceneBuilder: no field '" + prop + "' on " + target.GetType().Name); return; }
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
