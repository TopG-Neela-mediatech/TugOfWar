using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.threeinarow
{
    /// <summary>
    /// Responsive layout of the Three in a Row scene. No scripts at runtime: everything is anchors, pivots and an AspectRatioFitter.
    ///  - FitRoot fills the canvas; the HUD is pinned to the corners (back button top-left, lives top-right, banner top-centre).
    ///  - The board sits in a slot between the two character strips and is as big as fits (AspectRatioFitter, square);
    ///    every child of the board is placed by fractions of the board, so cells, faces, glows and the # lines scale together.
    ///  - Tapu / the Pet are stretched over a strip at the left / right edge and stand on its bottom edge, with their shadows and badges.
    /// Runs at the end of the scene builder and from Tools > Three In A Row > Apply Responsive Layout on the open scene.
    /// </summary>
    public static class ThreeInARowLayout
    {
        // the board the art was designed on (cells are on the # lines, 254 px apart)
        private const float DesignBoard = 940f;

        // character strips and the board slot, as fractions of the canvas
        private const float TapuLeft = 0.01f, TapuRight = 0.21f, TapuBottom = 0.13f, TapuTop = 0.72f;
        private const float PetLeft = 0.77f, PetRight = 0.99f, PetBottom = 0.10f, PetTop = 0.50f;
        private const float SlotLeft = 0.22f, SlotRight = 0.78f;
        private const float SlotBottomPx = 40f, SlotTopPx = 110f;   // room under the board / for the banner above it
        private const float CornerMargin = 60f;

        [MenuItem("Tools/Three In A Row/Apply Responsive Layout")]
        public static void ApplyToOpenScene()
        {
            var canvas = GameObject.Find("MainCanvas");
            if (canvas == null) { Debug.LogWarning("[ThreeInARow] MainCanvas not found in the open scene."); return; }
            Apply((RectTransform)canvas.transform);
            EditorSceneManager.MarkSceneDirty(canvas.scene);
            EditorSceneManager.SaveScene(canvas.scene);
        }

        public static void Apply(RectTransform canvas)
        {
            var fit = Find(canvas, "FitRoot");
            var game = Find(fit, "ThreeGame");
            var hud = Find(fit, "ThreeHUD");
            if (fit == null || game == null) return;

            Canvas.ForceUpdateCanvases();
            Fill(fit);
            fit.localScale = Vector3.one;
            Fill(game);
            if (hud != null) Fill(hud);

            // ---------------------------------------------------------------- Board
            var board = FindDeep(game, "Board");
            if (board != null)
            {
                ToFractions(board);   // every child of the board (and of each cell) by fractions of its parent
                foreach (RectTransform cell in board)
                    if (cell.name.StartsWith("Cell")) ToFractions(cell, "Tile", "Glow", "Face");

                var slot = Find(game, "BoardSlot");
                if (slot == null)
                {
                    var go = new GameObject("BoardSlot", typeof(RectTransform));
                    slot = (RectTransform)go.transform;
                    slot.SetParent(game, false);
                    slot.SetAsFirstSibling();
                }
                slot.anchorMin = new Vector2(SlotLeft, 0f);
                slot.anchorMax = new Vector2(SlotRight, 1f);
                slot.pivot = new Vector2(0.5f, 0.5f);
                slot.offsetMin = new Vector2(0f, SlotBottomPx);
                slot.offsetMax = new Vector2(0f, -SlotTopPx);
                board.SetParent(slot, false);

                board.localScale = Vector3.one;
                board.pivot = new Vector2(0.5f, 0.5f);
                var arf = board.GetComponent<AspectRatioFitter>();
                if (arf == null) arf = board.gameObject.AddComponent<AspectRatioFitter>();
                arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                arf.aspectRatio = 1f;
            }

            // ---------------------------------------------------------------- Characters, shadows, badges
            Strip(Find(game, "Tapu"), TapuLeft, TapuRight, TapuBottom, TapuTop);
            Strip(Find(game, "Pet"), PetLeft, PetRight, PetBottom, PetTop);
            Point(Find(game, "TapuShadow"), (TapuLeft + TapuRight) * 0.5f, TapuBottom, new Vector2(0f, 6f));
            Point(Find(game, "PetShadow"), (PetLeft + PetRight) * 0.5f, PetBottom, new Vector2(0f, 6f));

            // ---------------------------------------------------------------- HUD
            if (hud != null)
            {
                Point(Find(hud, "TapuBadge"), (TapuLeft + TapuRight) * 0.5f, 0.80f, Vector2.zero);
                Point(Find(hud, "PetBadge"), (PetLeft + PetRight) * 0.5f, 0.80f, Vector2.zero);
                Corner(Find(hud, "LivesPill"), new Vector2(1f, 1f), new Vector2(-CornerMargin, -40f));
                Corner(Find(hud, "Banner"), new Vector2(0.5f, 1f), new Vector2(0f, -35f));
            }
            Corner(Find(fit, "PlayschoolBackButton"), new Vector2(0f, 1f), new Vector2(CornerMargin, -40f));

            Canvas.ForceUpdateCanvases();
        }

        // ------------------------------------------------------------------ helpers

        private static RectTransform Find(Transform parent, string name) => parent == null ? null : parent.Find(name) as RectTransform;

        private static RectTransform FindDeep(Transform parent, string name)
        {
            foreach (Transform t in parent)
            {
                if (t.name == name) return (RectTransform)t;
                var r = FindDeep(t, name);
                if (r != null) return r;
            }
            return null;
        }

        private static void Fill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // The rig is fitted into this rect and stands on its bottom edge, so the pivot is bottom-centre (idle breathing scales from the feet).
        private static void Strip(RectTransform rt, float left, float right, float bottom, float top)
        {
            if (rt == null) return;
            rt.anchorMin = new Vector2(left, bottom);
            rt.anchorMax = new Vector2(right, top);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static void Point(RectTransform rt, float x, float y, Vector2 offset)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = new Vector2(x, y);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = offset;
        }

        private static void Corner(RectTransform rt, Vector2 corner, Vector2 offset)
        {
            if (rt == null) return;
            rt.anchorMin = rt.anchorMax = corner;
            rt.pivot = corner;
            rt.anchoredPosition = offset;
        }

        /// <summary>Re-anchors the children (all of them, or just the named ones) so they follow their parent's size by fractions.</summary>
        private static void ToFractions(RectTransform parent, params string[] only)
        {
            Rect pr = parent.rect;
            if (pr.width < 1f || pr.height < 1f) return;
            var corners = new Vector3[4];
            foreach (RectTransform child in parent)
            {
                if (only.Length > 0 && System.Array.IndexOf(only, child.name) < 0) continue;
                if (child.name == "WinLine" || child.name == "Highlight") continue;   // positioned by script / particle prefab

                child.GetWorldCorners(corners);
                Vector2 min = parent.InverseTransformPoint(corners[0]);
                Vector2 max = parent.InverseTransformPoint(corners[2]);
                Vector2 pivotPoint = min + Vector2.Scale(max - min, child.pivot);
                child.anchorMin = new Vector2((min.x - pr.xMin) / pr.width, (min.y - pr.yMin) / pr.height);
                child.anchorMax = new Vector2((max.x - pr.xMin) / pr.width, (max.y - pr.yMin) / pr.height);
                child.offsetMin = child.offsetMax = Vector2.zero;
            }
        }
    }
}
