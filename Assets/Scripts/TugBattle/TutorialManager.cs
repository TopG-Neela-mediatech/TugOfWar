using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.tugofwar
{
    /// <summary>
    /// Tap tutorial hand. Shows on the first level once "GO" is over, and again whenever the player starts losing.
    /// The hand taps in sets of 3 (pressed down + ripple), pauses, and repeats until the player has tapped a few times.
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
        [SerializeField] private float pressDistance = 36f;
        [SerializeField] private float pressScale = 0.85f;

        [Header("When to show")]
        [Tooltip("Show on the very first level when play starts.")]
        [SerializeField] private bool showOnFirstLevel = true;
        [Tooltip("Rope value (0..100) past which the player counts as losing.")]
        [SerializeField] private float losingRope = 40f;
        [Tooltip("Rope value below which the hint goes away by itself.")]
        [SerializeField] private float recoveredRope = 10f;
        [SerializeField] private int tapsToDismiss = 3;
        [SerializeField] private float cooldownAfterHide = 6f;

        public bool IsVisible { get; private set; }

        private Sequence loop;
        private Vector2 restPos;
        private bool firstLevel;
        private int tapsSinceShown;
        private float cooldownUntil;
        private bool hasRest;

        private void Awake()
        {
            CacheRest();
            SetActive(false);
        }

        private void CacheRest()
        {
            if (hasRest || hand == null) return;
            restPos = hand.anchoredPosition;
            hasRest = true;
        }

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
            // hide the placeholder label that sits under the hand
            foreach (Transform child in hand) if (child != ripple) child.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ Called by TugBattleController

        public void BeginLevel(int levelIndex)
        {
            firstLevel = showOnFirstLevel && levelIndex == 0;
            cooldownUntil = 0f;
            Hide(false);
        }

        public void OnPlayStart()
        {
            if (firstLevel) Show();
        }

        public void NotifyTap()
        {
            if (!IsVisible) return;
            tapsSinceShown++;
            if (tapsSinceShown >= tapsToDismiss) Hide();
        }

        /// <param name="rope">-100 (player winning) .. +100 (pet winning)</param>
        public void Tick(float rope)
        {
            if (!IsVisible)
            {
                if (rope > losingRope && Time.time >= cooldownUntil) Show();
            }
            else if (rope < recoveredRope && tapsSinceShown > 0)
            {
                Hide();
            }
        }

        // ------------------------------------------------------------------ Show / hide

        public void Show()
        {
            if (hand == null || IsVisible) return;
            CacheRest();
            IsVisible = true;
            tapsSinceShown = 0;

            KillTweens();
            SetActive(true);
            hand.anchoredPosition = restPos;
            hand.localScale = Vector3.one;
            if (group != null) { group.alpha = 0f; group.DOFade(1f, 0.25f); }

            loop = DOTween.Sequence().SetTarget(hand);
            loop.AppendInterval(0.3f);
            for (int i = 0; i < Mathf.Max(1, tapsPerSet); i++)
            {
                float half = tapDuration * 0.5f;
                loop.Append(hand.DOAnchorPosY(restPos.y - pressDistance, half).SetEase(Ease.OutQuad));
                loop.Join(hand.DOScale(pressScale, half).SetEase(Ease.OutQuad));
                loop.AppendCallback(PlayRipple);
                loop.Append(hand.DOAnchorPosY(restPos.y, half).SetEase(Ease.InQuad));
                loop.Join(hand.DOScale(1f, half).SetEase(Ease.InQuad));
                loop.AppendInterval(gapBetweenTaps);
            }
            loop.AppendInterval(pauseBetweenSets);
            loop.SetLoops(-1, LoopType.Restart);
        }

        public void Hide() => Hide(true);

        private void Hide(bool startCooldown)
        {
            if (startCooldown && IsVisible) cooldownUntil = Time.time + cooldownAfterHide;
            IsVisible = false;
            KillTweens();
            if (hand == null) return;
            if (group != null && hand.gameObject.activeInHierarchy && startCooldown)
                group.DOFade(0f, 0.2f).OnComplete(() => SetActive(false));
            else
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
                if (hasRest) hand.anchoredPosition = restPos;
                hand.localScale = Vector3.one;
            }
        }
    }
}
