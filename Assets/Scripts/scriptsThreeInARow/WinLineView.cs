using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.threeinarow
{
    /// <summary>
    /// The bright line that connects the three winning faces (horizontal, vertical or diagonal).
    /// One bar Image that is rotated to the line's angle and grows with a DOTween from the first to the last square.
    /// </summary>
    public class WinLineView : MonoBehaviour
    {
        [SerializeField] private Image bar;
        [SerializeField] private float thickness = 30f;
        [Tooltip("Extra length past the centre of the end squares.")]
        [SerializeField] private float overshoot = 70f;
        [SerializeField] private float drawTime = 0.45f;

        private RectTransform Rt => bar != null ? bar.rectTransform : null;
        private string TweenId => "winline" + GetInstanceID();

        private void Awake() => Hide();

        public void ApplyArt(Sprite sprite)
        {
            if (sprite == null || bar == null) return;
            bar.sprite = sprite;
            bar.type = Image.Type.Sliced;
            bar.color = Color.white;
        }

        public void Hide()
        {
            DOTween.Kill(TweenId);
            if (bar != null) bar.gameObject.SetActive(false);
        }

        /// <summary>Draws the line over the board between two squares (RectTransforms of the first and last winning cell).</summary>
        public void Draw(RectTransform from, RectTransform to, Color color)
        {
            if (bar == null) return;
            DOTween.Kill(TweenId);

            var parent = (RectTransform)bar.rectTransform.parent;
            Vector2 a = parent.InverseTransformPoint(from.position);
            Vector2 b = parent.InverseTransformPoint(to.position);
            Vector2 dir = b - a;
            float k = Mathf.Max(0.1f, parent.rect.width / 940f);   // the board is resized to fit the screen; thickness and overshoot follow it
            float thickness = this.thickness * k, overshoot = this.overshoot * k;
            float length = dir.magnitude + overshoot * 2f;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            var rt = bar.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);           // grows from the first square
            Vector2 unit = dir.normalized;
            rt.anchoredPosition = a - unit * overshoot;
            rt.localRotation = Quaternion.Euler(0, 0, angle);
            rt.sizeDelta = new Vector2(0f, thickness);
            bar.color = color;
            bar.gameObject.SetActive(true);

            DOTween.To(() => rt.sizeDelta.x, x => rt.sizeDelta = new Vector2(x, thickness), length, drawTime)
                .SetEase(Ease.OutCubic).SetId(TweenId);
        }

        private void OnDestroy() => DOTween.Kill(TweenId);
    }
}
