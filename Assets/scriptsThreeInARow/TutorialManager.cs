using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.threeinarow
{
    /// <summary>
    /// Tap tutorial hand. It moves onto a square and taps it in sets of 3 (pressed down + ripple), pauses and repeats.
    /// Shown on the very first level at the start of the player's first turn, and again whenever the player waits too long.
    /// Everything is DOTween - the hand is a single sprite (AssetsSO.tutorialHand).
    /// Reach it via GameManager.Instance.TutorialManager.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform hand;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform ripple;
        [SerializeField] private Image rippleImage;

        [Header("Tap animation")]
        [SerializeField] private int tapsPerSet = 3;
        [SerializeField] private float tapDuration = 0.28f;
        [SerializeField] private float gapBetweenTaps = 0.12f;
        [SerializeField] private float pauseBetweenSets = 0.9f;
        [SerializeField] private float pressDistance = 30f;
        [SerializeField] private float pressScale = 0.85f;
        [Tooltip("Where the hand sits relative to the centre of the square (the finger tip should land on the square).")]
        [SerializeField] private Vector2 handOffset = new Vector2(50f, -50f);

        [Header("When to show")]
        [Tooltip("Show on the very first level when the first turn starts.")]
        [SerializeField] private bool showOnFirstLevel = true;

        public bool IsVisible { get; private set; }

        private Sequence loop;
        private bool firstLevel;

        private void Awake() => SetActive(false);
        private void OnDestroy() => KillTweens();

        /// <summary>Applies the hand sprite from AssetsSO. Null keeps the placeholder.</summary>
        public void ApplyHand(Sprite sprite)
        {
            if (sprite == null || hand == null) return;
            var img = hand.GetComponent<Image>();
            if (img == null) return;
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
            foreach (Transform child in hand) if (child != ripple) child.gameObject.SetActive(false);   // placeholder label
        }

        // ------------------------------------------------------------------ Called by ThreeInARowController

        public void BeginLevel(int levelIndex)
        {
            firstLevel = showOnFirstLevel && levelIndex == 0;
            Hide();
        }

        /// <summary>True on level 1: the hand shows up on the first turn.</summary>
        public bool ShowsOnFirstTurn => firstLevel;

        /// <summary>Hand taps the given square. Does nothing if it is already showing on that square.</summary>
        public void ShowAt(RectTransform target)
        {
            if (hand == null || target == null) return;
            KillTweens();
            IsVisible = true;
            SetActive(true);

            // park the hand on the square (world position -> this canvas), then loop the taps from there
            var parent = (RectTransform)hand.parent;
            Vector2 local = parent.InverseTransformPoint(target.position);
            hand.anchoredPosition = local + handOffset;
            hand.localScale = Vector3.one;
            if (ripple != null) ripple.anchoredPosition = -handOffset;   // ripple sits on the square, not on the hand
            Vector2 rest = hand.anchoredPosition;

            if (group != null) { group.alpha = 0f; group.DOFade(1f, 0.25f); }

            loop = DOTween.Sequence().SetTarget(hand);
            loop.AppendInterval(0.3f);
            for (int i = 0; i < Mathf.Max(1, tapsPerSet); i++)
            {
                float half = tapDuration * 0.5f;
                loop.Append(hand.DOAnchorPosY(rest.y - pressDistance, half).SetEase(Ease.OutQuad));
                loop.Join(hand.DOScale(pressScale, half).SetEase(Ease.OutQuad));
                loop.AppendCallback(PlayRipple);
                loop.Append(hand.DOAnchorPosY(rest.y, half).SetEase(Ease.InQuad));
                loop.Join(hand.DOScale(1f, half).SetEase(Ease.InQuad));
                loop.AppendInterval(gapBetweenTaps);
            }
            loop.AppendInterval(pauseBetweenSets);
            loop.SetLoops(-1, LoopType.Restart);
        }

        public void Hide()
        {
            IsVisible = false;
            KillTweens();
            SetActive(false);
        }

        private void PlayRipple()
        {
            if (ripple == null) return;
            ripple.DOKill();
            ripple.localScale = Vector3.one * 0.3f;
            if (rippleImage != null)
            {
                rippleImage.DOKill();
                var c = rippleImage.color; c.a = 0.55f; rippleImage.color = c;
                rippleImage.DOFade(0f, 0.35f);
            }
            ripple.DOScale(1.6f, 0.35f).SetEase(Ease.OutQuad);
        }

        private void KillTweens()
        {
            if (loop != null) { loop.Kill(); loop = null; }
            if (hand != null) hand.DOKill();
            if (group != null) group.DOKill();
            if (ripple != null) ripple.DOKill();
            if (rippleImage != null) rippleImage.DOKill();
        }

        private void SetActive(bool on)
        {
            if (hand == null) return;
            hand.gameObject.SetActive(on);
            if (!on)
            {
                if (group != null) group.alpha = 0f;
                hand.localScale = Vector3.one;
            }
        }
    }
}
