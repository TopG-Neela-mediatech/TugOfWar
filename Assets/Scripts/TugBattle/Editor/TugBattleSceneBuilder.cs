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
    /// Creates the world (rope, flag, Player, Pet, win lines), the HUD, TapArea and the EndPanel with PLACEHOLDER
    /// visuals (Unity's built-in UI sprites), then wires every reference on GameManager, TugBattleController, TugBattleUI,
    /// RopeView, CharacterView, TapInput, TugBattleVfx and EndPanelScript. Safe to run again: it rebuilds what it made.
    /// Afterwards just swap the sprites; nothing else needs to be assigned.
    /// </summary>
    public static class TugBattleSceneBuilder
    {
        private const string WorldName = "TugBattleWorld";
        private const string HudName = "TugHUD";
        private const string TapAreaName = "TapArea";
        private const string EndPanelName = "EndPanel";

        // Units match the HTML prototype scaled to the 17.78 x 10 ortho view (1600 px -> 17.78 units)
        private const float WinLineX = 2.44f;
        private const float HandsY = -1.9f;

        private static Sprite square;
        private static Sprite circle;

        [MenuItem("Tools/Tug Battle/Build Scene")]
        public static void Build()
        {
            square = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

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

            DestroyIfExists(WorldName); DestroyIfExists(HudName); DestroyIfExists(TapAreaName); DestroyIfExists(EndPanelName);

            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null) canvas.worldCamera = cam;
            EnsureInputModule();

            // ------------------------------------------------------------ world
            var world = new GameObject(WorldName).transform;
            MakeSprite(world, "Sky", new Vector2(0, 0), new Vector2(22, 12), new Color(0.56f, 0.82f, 0.95f), square, -10);
            MakeSprite(world, "Ground", new Vector2(0, -3.6f), new Vector2(22, 1f), new Color(0.45f, 0.75f, 0.4f), square, -9);

            var playerLine = MakeSprite(world, "PlayerWinLine", new Vector2(-WinLineX, -1.3f), new Vector2(0.14f, 3.6f), new Color(0.3f, 0.6f, 1f), square, -5);
            var petLine = MakeSprite(world, "PetWinLine", new Vector2(WinLineX, -1.3f), new Vector2(0.14f, 3.6f), new Color(1f, 0.3f, 0.45f), square, -5);

            var group = new GameObject("RopeGroup").transform;
            group.SetParent(world, false);
            var rope = MakeSprite(group, "Rope", new Vector2(0, HandsY), new Vector2(9.4f, 0.12f), new Color(0.75f, 0.5f, 0.25f), square, 1);

            var flag = new GameObject("Flag").transform;
            flag.SetParent(group, false);
            flag.localPosition = new Vector3(0, HandsY, 0);
            MakeSprite(flag, "Pole", new Vector2(0, 0.5f), new Vector2(0.07f, 1f), new Color(0.35f, 0.25f, 0.2f), square, 2);
            MakeSprite(flag, "Cloth", new Vector2(0.28f, 0.85f), new Vector2(0.5f, 0.32f), new Color(1f, 0.3f, 0.35f), square, 3);

            var playerView = BuildCharacter(group, "Player", new Vector2(-4.89f, -2.35f), new Vector2(0.9f, 1.5f), new Color(1f, 0.44f, 0.57f), 0f, 1);
            var petView = BuildCharacter(group, "Pet", new Vector2(4.89f, -2.65f), new Vector2(1.4f, 0.9f), new Color(0.97f, 0.65f, 0.3f), -0.6f, -1);

            var ropeView = group.gameObject.AddComponent<RopeView>();
            Set(ropeView, "ropeRoot", group);
            Set(ropeView, "flag", flag);
            Set(ropeView, "ropeSpriteRenderer", rope);
            SetFloat(ropeView, "maxOffset", WinLineX);

            // ------------------------------------------------------------ UI
            var canvasRt = canvas.transform;

            var tapArea = NewUI(TapAreaName, canvasRt);
            Stretch(tapArea);
            var tapImg = tapArea.gameObject.AddComponent<Image>();
            tapImg.color = new Color(1, 1, 1, 0);
            tapImg.raycastTarget = true;
            tapArea.SetAsFirstSibling();
            var tapInput = tapArea.gameObject.AddComponent<TapInput>();
            Set(tapInput, "controller", battle);

            var hud = NewUI(HudName, canvasRt);
            Stretch(hud);
            var ui = hud.gameObject.AddComponent<TugBattleUI>();

            var levelText = MakeText(hud, "LevelText", "Level 1", 56, Color.white);
            Anchor(levelText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(500, 90));

            var pauseButton = MakeButton(hud, "PauseButton", "II", new Vector2(1f, 1f), new Vector2(-80, -80), new Vector2(110, 110), new Color(1f, 0.8f, 0.25f));

            // power meter
            var meter = NewUI("PowerMeter", hud);
            Anchor(meter, new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(820, 64));
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
            var meterLabel = MakeText(meter, "Label", "POWER", 34, Color.white);
            Stretch(meterLabel.rectTransform);

            var countdown = MakeText(hud, "Countdown", "3", 320, new Color(1f, 0.95f, 0.4f));
            Anchor(countdown.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(700, 420));
            countdown.fontStyle = FontStyles.Bold;
            countdown.gameObject.SetActive(false);

            // banners
            string[] bannerText = { "TAP FAST!", "KEEP TAPPING!", "STRONG PULL!", "PET TIRED! TAP NOW!", "POWER PULL!" };
            Color[] bannerColor = { new Color(1f, 0.3f, 0.4f), new Color(0.3f, 0.8f, 0.4f), new Color(1f, 0.5f, 0.1f), new Color(0.3f, 0.65f, 1f), new Color(1f, 0.8f, 0.1f) };
            var banners = new RectTransform[5];
            for (int i = 0; i < 5; i++)
            {
                var t = MakeText(hud, "Banner_" + (BannerKind)i, bannerText[i], 110, bannerColor[i]);
                t.fontStyle = FontStyles.Bold;
                Anchor(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 330), new Vector2(1500, 160));
                t.gameObject.SetActive(false);
                banners[i] = t.rectTransform;
            }

            // pops
            var popParent = NewUI("PopParent", hud);
            Stretch(popParent);
            var popAnchor = NewUI("PopAnchor", hud);
            Anchor(popAnchor, new Vector2(0.5f, 0.5f), new Vector2(-480, 120), new Vector2(10, 10));
            string[] popText = { "Good!", "Great!", "Super Pull!" };
            Color[] popColor = { new Color(0.3f, 0.65f, 1f), new Color(0.3f, 0.8f, 0.4f), new Color(1f, 0.3f, 0.4f) };
            var popPrefabs = new RectTransform[3];
            for (int i = 0; i < 3; i++) popPrefabs[i] = MakePopPrefab("Pop_" + (PopKind)i, popText[i], popColor[i], 70 + i * 10);

            // pause panel
            var pausePanel = MakeImage(hud, "PausePanel", new Color(0.12f, 0.1f, 0.25f, 0.92f));
            Anchor(pausePanel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 520));
            var pauseTitle = MakeText(pausePanel.rectTransform, "Title", "PAUSED", 90, Color.white);
            Anchor(pauseTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(700, 120));
            var resumeButton = MakeButton(pausePanel.rectTransform, "ResumeButton", "RESUME", new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(420, 110), new Color(0.3f, 0.8f, 0.4f));
            var pauseHome = MakeButton(pausePanel.rectTransform, "HomeButton", "HOME", new Vector2(0.5f, 0.5f), new Vector2(0, -140), new Vector2(420, 110), new Color(1f, 0.5f, 0.4f));
            pausePanel.gameObject.SetActive(false);

            WireUI(ui, countdown, banners, popPrefabs, popParent, popAnchor, fill, glow.gameObject, meter, levelText, pauseButton, pausePanel.gameObject, resumeButton, pauseHome);

            // ------------------------------------------------------------ end panel
            var endRoot = NewUI(EndPanelName, canvasRt);
            Stretch(endRoot);
            var endPanel = endRoot.gameObject.AddComponent<EndPanelScript>();
            var win = BuildEndPanel(endRoot, "WinPanel", "YOU WIN!", "Next level coming up...", new Color(0.3f, 0.75f, 0.45f, 0.93f), true, out var winRays, out var winHome);
            var lose = BuildEndPanel(endRoot, "LosePanel", "ALMOST!", "Tap Faster! Try again...", new Color(0.95f, 0.5f, 0.4f, 0.93f), false, out var loseRays, out var loseHome);
            endRoot.SetAsLastSibling();
            Set(endPanel, "winPanel", win);
            Set(endPanel, "losePanel", lose);
            Set(endPanel, "winRaysT", winRays);
            Set(endPanel, "loseRaysT", loseRays);
            SetArray(endPanel, "homeButtons", winHome, loseHome);

            // ------------------------------------------------------------ wire managers
            Set(battle, "ropeView", ropeView);
            Set(battle, "player", playerView);
            Set(battle, "pet", petView);
            Set(battle, "ui", ui);
            Set(battle, "vfx", vfx);

            Set(vfx, "worldCamera", cam);
            Set(vfx, "playerWinLine", playerLine.transform);
            Set(vfx, "petWinLine", petLine.transform);

            Set(gm, "levelManager", lm);
            Set(gm, "soundManager", sound);
            Set(gm, "battleController", battle);
            Set(gm, "endPanelScript", endPanel);

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = world.gameObject;
            Debug.Log("Tug Battle scene built and saved. Press Play - the game starts straight away. Swap the placeholder sprites when your art is ready.");
        }

        // ================================================================== builders

        private static CharacterView BuildCharacter(Transform parent, string name, Vector2 pos, Vector2 bodySize, Color color, float slideX, int facing)
        {
            var body = MakeSprite(parent, name, pos, bodySize, color, square, 2);
            MakeSprite(body.transform, "Head", new Vector2(0, bodySize.y * 0.5f + 0.35f), new Vector2(0.75f, 0.75f), new Color(0.96f, 0.79f, 0.64f), circle, 3, true);
            MakeSprite(body.transform, "Eye", new Vector2(0.18f * -facing, bodySize.y * 0.5f + 0.42f), new Vector2(0.12f, 0.12f), new Color(0.23f, 0.18f, 0.29f), circle, 4, true);
            var view = body.gameObject.AddComponent<CharacterView>();
            Set(view, "spriteRenderer", body);
            Set(view, "punchTarget", body.transform);
            SetFloat(view, "jumpHeight", 0.4f);
            SetFloat(view, "slideX", slideX);
            return view;
        }

        private static RectTransform BuildEndPanel(RectTransform parent, string name, string title, string sub, Color bg, bool stars, out RectTransform rays, out Button home)
        {
            var panel = MakeImage(parent, name, bg);
            Stretch(panel.rectTransform);

            var raysImg = MakeImage(panel.rectTransform, "Rays", new Color(1f, 0.95f, 0.5f, 0.22f));
            Anchor(raysImg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1100, 1100));
            raysImg.raycastTarget = false;
            rays = raysImg.rectTransform;

            var t = MakeText(panel.rectTransform, "Title", title, 190, Color.white);
            t.fontStyle = FontStyles.Bold;
            Anchor(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1500, 260));

            if (stars)
            {
                for (int i = 0; i < 3; i++)
                {
                    var s = MakeImage(panel.rectTransform, "Star" + (i + 1), new Color(1f, 0.85f, 0.15f));
                    s.sprite = circle;
                    Anchor(s.rectTransform, new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 200, -40 + (i == 1 ? 30 : 0)), new Vector2(150, 150));
                }
            }
            var subText = MakeText(panel.rectTransform, "Sub", sub, 60, Color.white);
            Anchor(subText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -200), new Vector2(1400, 100));

            home = MakeButton(panel.rectTransform, "HomeButton", "HOME", new Vector2(0f, 1f), new Vector2(150, -90), new Vector2(220, 100), new Color(1f, 0.8f, 0.25f));
            panel.gameObject.SetActive(false);
            return panel.rectTransform;
        }

        private static RectTransform MakePopPrefab(string name, string text, Color color, float size)
        {
            string dir = "Assets/Prefabs/TugPops";
            Directory.CreateDirectory(dir);
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(600, 140);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, dir + "/" + name + ".prefab");
            Object.DestroyImmediate(go);
            return (RectTransform)prefab.transform;
        }

        private static void WireUI(TugBattleUI ui, TMP_Text countdown, RectTransform[] banners, RectTransform[] pops, RectTransform popParent,
            RectTransform popAnchor, Image fill, GameObject glow, RectTransform meter, TMP_Text levelText, Button pauseButton,
            GameObject pausePanel, Button resumeButton, Button homeButton)
        {
            var so = new SerializedObject(ui);
            so.FindProperty("countdownText").objectReferenceValue = countdown;
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
            so.FindProperty("levelText").objectReferenceValue = levelText;
            so.FindProperty("pauseButton").objectReferenceValue = pauseButton;
            so.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            so.FindProperty("resumeButton").objectReferenceValue = resumeButton;
            so.FindProperty("homeButton").objectReferenceValue = homeButton;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ================================================================== small helpers

        private static SpriteRenderer MakeSprite(Transform parent, string name, Vector2 pos, Vector2 size, Color color, Sprite sprite, int order, bool local = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            Vector2 native = sprite.bounds.size;
            // keep child sizes independent of a scaled parent: parents here are never scaled
            go.transform.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
            return sr;
        }

        private static RectTransform NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
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

        private static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color)
        {
            var rt = NewUI(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return t;
        }

        private static Button MakeButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            var img = MakeImage(parent, name, color);
            img.raycastTarget = true;
            Anchor(img.rectTransform, anchor, pos, size);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var t = MakeText(img.rectTransform, "Label", label, 54, new Color(0.2f, 0.15f, 0.3f));
            t.fontStyle = FontStyles.Bold;
            Stretch(t.rectTransform);
            return btn;
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
            if (es == null)
            {
                es = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            }
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
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
