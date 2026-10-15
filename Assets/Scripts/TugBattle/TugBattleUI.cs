using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace tmkoc.tugofwar
{
    /// <summary>
    /// Gameplay HUD: countdown, prompts/banners, floating Good/Great/Super text, power meter, level, pause.
    /// Banners and pops are just GameObjects - drop in an Image (your art) or a TMP text, whatever you like.
    /// </summary>
    public class TugBattleUI : MonoBehaviour
    {
        [Serializable]
        public class BannerEntry
        {
            public BannerKind kind;
            public RectTransform root;
        }

        [Serializable]
        public class PopEntry
        {
            public PopKind kind;
            [Tooltip("Prefab (Image or TMP text). Add a CanvasGroup to get a fade-out.")]
            public RectTransform prefab;
        }

        [Header("Countdown")]
        [SerializeField] private TMP_Text countdownText;

        [Header("Banners (Tap Fast / Keep Tapping / Strong Pull / Pet Tired / Power Pull)")]
        [SerializeField] private BannerEntry[] banners;

        [Header("Floating pops (Good / Great / Super Pull)")]
        [SerializeField] private PopEntry[] pops;
        [SerializeField] private RectTransform popParent;
        [Tooltip("Pops spawn around this point (put it above the Player's head).")]
        [SerializeField] private RectTransform popAnchor;
        [SerializeField] private float popRise = 120f;

        [Header("Power meter")]
        [SerializeField] private Image powerFill;
        [SerializeField] private GameObject powerFullGlow;
        [SerializeField] private float powerGlowThreshold = 0.75f;
        [SerializeField] private RectTransform powerMeterRoot;

        [Header("Level / Pause")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button homeButton;

        public event Action PauseClicked;
        public event Action ResumeClicked;

        private BannerKind? activeBanner;
        private Tween bannerHideTween;

        private void Awake()
        {
            if (pauseButton != null) pauseButton.onClick.AddListener(() => PauseClicked?.Invoke());
            if (resumeButton != null) resumeButton.onClick.AddListener(() => ResumeClicked?.Invoke());
            if (homeButton != null)
                homeButton.onClick.AddListener(() => SceneManager.LoadScene(TMKOCPlaySchoolConstants.TMKOCPlayMainMenu));
            ResetUI();
        }

        public void ResetUI()
        {
            HideCountdown();
            HideBanner();
            SetPower(0f);
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        public void SetLevel(int levelIndex)
        {
            if (levelText != null) levelText.text = "Level " + (levelIndex + 1);
        }

        public void SetPauseVisible(bool visible)
        {
            if (pausePanel == null) return;
            var t = pausePanel.transform;
            t.DOKill();
            pausePanel.SetActive(visible);
            if (!visible) return;
            t.localScale = Vector3.one * 0.6f;
            t.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }

        public void PunchPowerMeter()
        {
            var t = powerMeterRoot != null ? powerMeterRoot : (powerFill != null ? powerFill.rectTransform : null);
            if (t == null) return;
            t.DOKill(true);
            t.DOPunchScale(Vector3.one * 0.2f, 0.35f, 8, 0.6f);
        }

        public void SetPauseButtonVisible(bool visible)
        {
            if (pauseButton != null) pauseButton.gameObject.SetActive(visible);
        }

        #region Countdown
        public void ShowCountdown(string text)
        {
            if (countdownText == null) return;
            countdownText.gameObject.SetActive(true);
            countdownText.text = text;
            var t = countdownText.rectTransform;
            t.DOKill();
            t.localScale = Vector3.one * 1.6f;
            t.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
        }

        public void HideCountdown()
        {
            if (countdownText == null) return;
            countdownText.rectTransform.DOKill();
            countdownText.gameObject.SetActive(false);
        }
        #endregion

        #region Banners
        /// <param name="duration">Seconds to stay. 0 = stay (pulsing) until HideBanner / another banner.</param>
        public void ShowBanner(BannerKind kind, float duration = 0f)
        {
            if (duration <= 0f && activeBanner == kind) return;

            HideBanner();
            for (int i = 0; i < banners.Length; i++)
            {
                if (banners[i].kind != kind || banners[i].root == null) continue;
                var r = banners[i].root;
                activeBanner = kind;
                r.gameObject.SetActive(true);
                r.DOKill();
                r.localScale = Vector3.one * 0.4f;
                var pop = r.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
                if (duration <= 0f)
                    pop.OnComplete(() => r.DOScale(1.08f, 0.35f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine));
                else
                    bannerHideTween = DOVirtual.DelayedCall(duration, () => { if (activeBanner == kind) HideBanner(); });
                return;
            }
        }

        public void HideBanner()
        {
            bannerHideTween?.Kill();
            bannerHideTween = null;
            activeBanner = null;
            if (banners == null) return;
            for (int i = 0; i < banners.Length; i++)
            {
                if (banners[i].root == null) continue;
                banners[i].root.DOKill();
                banners[i].root.gameObject.SetActive(false);
            }
        }
        #endregion

        #region Pops
        public void SpawnPop(PopKind kind)
        {
            if (popParent == null) return;
            for (int i = 0; i < pops.Length; i++)
            {
                if (pops[i].kind != kind || pops[i].prefab == null) continue;
                var pop = Instantiate(pops[i].prefab, popParent);
                Vector2 basePos = popAnchor != null ? popAnchor.anchoredPosition : Vector2.zero;
                pop.anchoredPosition = basePos + new Vector2(UnityEngine.Random.Range(-40f, 60f), 0f);
                pop.localScale = Vector3.one * 0.5f;

                var cg = pop.GetComponent<CanvasGroup>();
                var seq = DOTween.Sequence().SetTarget(pop);
                seq.Append(pop.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
                seq.Join(pop.DOAnchorPosY(pop.anchoredPosition.y + popRise, 0.9f).SetEase(Ease.OutQuad));
                if (cg != null) seq.Insert(0.5f, cg.DOFade(0f, 0.4f));
                seq.OnComplete(() => { if (pop != null) Destroy(pop.gameObject); });
                return;
            }
        }
        #endregion

        #region Power meter
        public void SetPower(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            if (powerFill != null) powerFill.fillAmount = normalized;
            if (powerFullGlow != null) powerFullGlow.SetActive(normalized >= powerGlowThreshold);
        }
        #endregion

        private void OnDestroy()
        {
            bannerHideTween?.Kill();
        }
    }
}
