using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.tugofwar
{
    /// <summary>
    /// End-of-round winner display. When someone wins, the matching "win" image pops in and jumps (all DOTween on the
    /// canvas), while the rope, the two characters and the win lines are hidden. ResetState() puts everything back
    /// exactly as it was when the next round / retry starts.
    /// </summary>
    public class WinPresenter : MonoBehaviour
    {
        [Header("Win images (disabled in the scene until someone wins)")]
        [SerializeField] private RectTransform playerWin;
        [SerializeField] private RectTransform petWin;

        [Tooltip("Hidden while a win image is showing, restored on reset: rope, player, pet, both win lines.")]
        [SerializeField] private GameObject[] hideOnWin;

        [Header("Winning animation")]
        [SerializeField] private float popTime = 0.45f;
        [SerializeField] private float jumpHeight = 70f;
        [SerializeField] private float jumpUpTime = 0.32f;
        [SerializeField] private float wiggleAngle = 6f;

        public bool IsShowing { get; private set; }

        private struct Rest
        {
            public Vector2 pos;
            public Vector3 scale;
            public Quaternion rot;
            public Color color;
        }

        private Rest playerRest, petRest;
        private bool cached;
        private string TweenId => "winpresenter" + GetInstanceID();

        private void Awake()
        {
            Cache();
            ResetState();
        }

        private void OnDestroy() => DOTween.Kill(TweenId);

        private void Cache()
        {
            if (cached) return;
            playerRest = Capture(playerWin);
            petRest = Capture(petWin);
            cached = true;
        }

        private static Rest Capture(RectTransform rt)
        {
            if (rt == null) return default;
            var img = rt.GetComponent<Image>();
            return new Rest
            {
                pos = rt.anchoredPosition,
                scale = rt.localScale,
                rot = rt.localRotation,
                color = img != null ? img.color : Color.white
            };
        }

        private static void Restore(RectTransform rt, Rest r)
        {
            if (rt == null) return;
            rt.anchoredPosition = r.pos;
            rt.localScale = r.scale;
            rt.localRotation = r.rot;
            var img = rt.GetComponent<Image>();
            if (img != null) img.color = r.color;
            rt.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ Public API

        /// <summary>Back to the normal board: win images off + at rest, rope / characters / lines back on.</summary>
        public void ResetState()
        {
            Cache();
            DOTween.Kill(TweenId);
            Restore(playerWin, playerRest);
            Restore(petWin, petRest);
            if (hideOnWin != null)
                foreach (var go in hideOnWin)
                    if (go != null) go.SetActive(true);
            IsShowing = false;
        }

        public void Show(bool playerWon)
        {
            Cache();
            var rt = playerWon ? playerWin : petWin;
            if (rt == null) return;

            DOTween.Kill(TweenId);
            if (hideOnWin != null)
                foreach (var go in hideOnWin)
                    if (go != null) go.SetActive(false);

            // never show both winners at once, even if Show is called twice without a reset
            Restore(playerWon ? petWin : playerWin, playerWon ? petRest : playerRest);

            var rest = playerWon ? playerRest : petRest;
            rt.gameObject.SetActive(true);
            rt.anchoredPosition = rest.pos;
            rt.localRotation = rest.rot;
            rt.localScale = rest.scale * 0.2f;
            var img = rt.GetComponent<Image>();
            if (img != null)
            {
                var c = rest.color; c.a = 0f; img.color = c;
                img.DOFade(rest.color.a, 0.2f).SetId(TweenId);
            }
            IsShowing = true;

            // pop in (overshoot, settle), then jump forever
            var pop = DOTween.Sequence().SetId(TweenId);
            pop.Append(rt.DOScale(rest.scale * 1.18f, popTime * 0.55f).SetEase(Ease.OutQuad));
            pop.Append(rt.DOScale(rest.scale, popTime * 0.45f).SetEase(Ease.OutBack));
            pop.AppendCallback(() => StartJumpLoop(rt, rest));
        }

        // ------------------------------------------------------------------ Tweens

        private void StartJumpLoop(RectTransform rt, Rest rest)
        {
            float up = Mathf.Max(0.1f, jumpUpTime);
            Vector3 s = rest.scale;

            var jump = DOTween.Sequence().SetId(TweenId).SetLoops(-1);
            // launch: rise and stretch
            jump.Append(rt.DOAnchorPosY(rest.pos.y + jumpHeight, up).SetEase(Ease.OutQuad));
            jump.Join(rt.DOScale(new Vector3(s.x * 0.94f, s.y * 1.08f, s.z), up).SetEase(Ease.OutQuad));
            // fall and relax
            jump.Append(rt.DOAnchorPosY(rest.pos.y, up * 0.9f).SetEase(Ease.InQuad));
            jump.Join(rt.DOScale(s, up * 0.9f).SetEase(Ease.InQuad));
            // landing squash + bounce back
            jump.Append(rt.DOScale(new Vector3(s.x * 1.1f, s.y * 0.88f, s.z), 0.08f).SetEase(Ease.OutQuad));
            jump.Append(rt.DOScale(s, 0.1f).SetEase(Ease.OutBack));
            jump.AppendInterval(0.05f);

            // little side-to-side wiggle while jumping
            rt.localRotation = rest.rot * Quaternion.Euler(0f, 0f, -wiggleAngle);
            rt.DOLocalRotate(rest.rot.eulerAngles + new Vector3(0f, 0f, wiggleAngle), up)
                .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetId(TweenId);
        }
    }
}
