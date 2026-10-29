using UnityEngine;

namespace tmkoc.threeinarow
{
    /// <summary>
    /// Keeps the whole 1920x1080 design area on screen on every device. Put it on a centred RectTransform sized 1920x1080 that holds
    /// the game and the HUD: it scales that root down (never up) so it fits inside the canvas minus the device's safe area
    /// (notches, rounded corners), and shifts it clear of the notch. Wide phones and 4:3 tablets get empty margins instead of cut-off content.
    /// The background is not inside this root, so it still fills the screen.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFit : MonoBehaviour
    {
        [SerializeField] private Vector2 designSize = new Vector2(1920f, 1080f);

        private RectTransform rt;
        private Rect lastSafe;
        private Vector2 lastParent;

        private void OnEnable() => Apply();
        private void OnRectTransformDimensionsChange() => Apply();
        private void Update()
        {
            // the canvas can resize (rotation, simulator, window) without this object's own rect changing
            var parent = transform.parent as RectTransform;
            if (Screen.safeArea != lastSafe || (parent != null && parent.rect.size != lastParent)) Apply();
        }

        private void Apply()
        {
            if (rt == null) rt = (RectTransform)transform;
            var parent = rt.parent as RectTransform;
            if (parent == null) return;

            Rect pr = parent.rect;
            if (pr.width < 1f || pr.height < 1f) return;

            Rect safe = Screen.safeArea;
            float sw = Mathf.Max(1f, Screen.width), sh = Mathf.Max(1f, Screen.height);
            float l = safe.xMin / sw, r = 1f - safe.xMax / sw;
            float b = safe.yMin / sh, t = 1f - safe.yMax / sh;
            lastSafe = safe;
            lastParent = pr.size;

            float availW = pr.width * (1f - l - r);
            float availH = pr.height * (1f - b - t);
            float s = Mathf.Min(1f, availW / designSize.x, availH / designSize.y);

            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = designSize;
            rt.localScale = new Vector3(s, s, 1f);
            rt.anchoredPosition = new Vector2((l - r) * 0.5f * pr.width, (b - t) * 0.5f * pr.height);
        }
    }
}
