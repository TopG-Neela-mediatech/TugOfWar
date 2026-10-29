using AssetKits.ParticleImage;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.threeinarow
{
    /// <summary>
    /// HUD + feedback: the two turn indicators (Tapu / Pet), the message banner, the board entrance and the confetti
    /// (the same ParticleImage confetti as Tug Battle). Everything else is DOTween on the canvas.
    /// </summary>
    public class ThreeInARowUI : MonoBehaviour
    {
        [Header("Board")]
        [SerializeField] private RectTransform boardRoot;

        [Header("Turn indicators")]
        [SerializeField] private RectTransform tapuBadge;
        [SerializeField] private RectTransform petBadge;
        [SerializeField] private Image tapuBadgeGlow;
        [SerializeField] private Image petBadgeGlow;

        [Header("Lives")]
        [Tooltip("One heart per life, left to right. The last one is lost first.")]
        [SerializeField] private Image[] hearts;
        [SerializeField] private Color lostHeartColor = new Color(0.35f, 0.35f, 0.4f, 0.45f);

        [Header("Banner")]
        [SerializeField] private RectTransform banner;
        [SerializeField] private TMP_Text bannerText;
        [SerializeField] private CanvasGroup bannerGroup;

        [Header("Confetti (ParticleImage, same prefab as Tug Battle)")]
        [Tooltip("Falling shower - played when the player wins.")]
        [SerializeField] private ParticleImage confettiShower;
        [Tooltip("Burst - played on a win and on a draw.")]
        [SerializeField] private ParticleImage confettiBurst;

        private const string Id = "threeui";
        private Vector3 tapuBadgeScale = Vector3.one, petBadgeScale = Vector3.one;
        private bool cached;

        private void Awake()
        {
            Cache();
            HideBanner(false);
            SetTurn(Mark.None);
            ClearConfetti();
        }

        private void Cache()
        {
            if (cached) return;
            if (tapuBadge != null) tapuBadgeScale = tapuBadge.localScale;
            if (petBadge != null) petBadgeScale = petBadge.localScale;
            cached = true;
        }

        private void OnDestroy() => DOTween.Kill(Id);

        // ------------------------------------------------------------------ Lives

        /// <param name="lives">Lives left (0..hearts.Length). With animate, the heart that was just lost pops and greys out.</param>
        public void SetLives(int lives, bool animate)
        {
            if (hearts == null) return;
            for (int i = 0; i < hearts.Length; i++)
            {
                var h = hearts[i];
                if (h == null) continue;
                var rt = h.rectTransform;
                rt.DOKill();
                h.DOKill();
                bool alive = i < lives;
                if (alive)
                {
                    h.color = Color.white;
                    rt.localScale = Vector3.one;
                    rt.localRotation = Quaternion.identity;
                    continue;
                }

                bool justLost = animate && h.color.a > 0.9f;
                if (!justLost)
                {
                    h.color = lostHeartColor;
                    rt.localScale = Vector3.one * 0.8f;
                    rt.localRotation = Quaternion.identity;
                    continue;
                }

                // the lost heart: a big pop and shake, then it greys out and shrinks
                var s = DOTween.Sequence().SetId(Id);
                s.Append(rt.DOScale(1.6f, 0.18f).SetEase(Ease.OutQuad));
                s.Join(rt.DOLocalRotate(new Vector3(0, 0, 18f), 0.18f));
                s.Append(rt.DOShakeAnchorPos(0.35f, 14f, 24));
                s.Append(rt.DOScale(0.8f, 0.25f).SetEase(Ease.InBack));
                s.Join(rt.DOLocalRotate(Vector3.zero, 0.25f));
                s.Join(h.DOColor(lostHeartColor, 0.25f));
            }
        }

        // ------------------------------------------------------------------ Board

        public void BoardAppear()
        {
            if (boardRoot == null) return;
            boardRoot.DOKill();
            boardRoot.localScale = Vector3.one * 0.2f;
            boardRoot.DOScale(1f, 0.55f).SetEase(Ease.OutBack).SetId(Id);
        }

        // ------------------------------------------------------------------ Turn indicators

        /// <param name="who">Mark.Tapu / Mark.Pet lights that badge; Mark.None dims both.</param>
        public void SetTurn(Mark who)
        {
            Cache();
            Light(tapuBadge, tapuBadgeGlow, tapuBadgeScale, who == Mark.Tapu);
            Light(petBadge, petBadgeGlow, petBadgeScale, who == Mark.Pet);
        }

        private static void Light(RectTransform badge, Image glow, Vector3 rest, bool on)
        {
            if (badge == null) return;
            badge.DOKill();
            if (glow != null) glow.DOKill();
            if (on)
            {
                badge.localScale = rest * 0.9f;
                badge.DOScale(rest * 1.12f, 0.3f).SetEase(Ease.OutBack);
                if (glow != null)
                {
                    glow.gameObject.SetActive(true);
                    var c = glow.color; c.a = 0.3f; glow.color = c;
                    glow.DOFade(0.95f, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
                }
            }
            else
            {
                badge.DOScale(rest * 0.92f, 0.2f);
                if (glow != null) glow.gameObject.SetActive(false);
            }
        }

        // ------------------------------------------------------------------ Banner

        public void ShowBanner(string text, float seconds = 1.6f)
        {
            if (banner == null) return;
            if (bannerText != null) bannerText.text = text;
            DOTween.Kill(Id + "banner");
            banner.gameObject.SetActive(true);
            banner.localScale = Vector3.one * 0.5f;
            if (bannerGroup != null) bannerGroup.alpha = 0f;

            var s = DOTween.Sequence().SetId(Id + "banner");
            s.Append(banner.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            if (bannerGroup != null) s.Join(bannerGroup.DOFade(1f, 0.2f));
            if (seconds > 0f)
            {
                s.AppendInterval(seconds);
                if (bannerGroup != null) s.Append(bannerGroup.DOFade(0f, 0.25f));
                s.OnComplete(() => banner.gameObject.SetActive(false));
            }
        }

        public void HideBanner(bool animate = true)
        {
            if (banner == null) return;
            DOTween.Kill(Id + "banner");
            if (animate && bannerGroup != null && banner.gameObject.activeSelf)
                bannerGroup.DOFade(0f, 0.2f).SetId(Id + "banner").OnComplete(() => banner.gameObject.SetActive(false));
            else
                banner.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ Confetti

        /// <param name="count">60+ = full celebration (burst + shower), smaller = just the burst (draw).</param>
        public void Confetti(int count = 60)
        {
            Fire(confettiBurst);
            if (count >= 40) Fire(confettiShower);
        }

        public void ClearConfetti()
        {
            if (confettiBurst != null) confettiBurst.Stop(true);
            if (confettiShower != null) confettiShower.Stop(true);
        }

        private static void Fire(ParticleImage p)
        {
            if (p == null) return;
            p.gameObject.SetActive(true);
            p.Stop(true);
            p.Play();
        }
    }
}
