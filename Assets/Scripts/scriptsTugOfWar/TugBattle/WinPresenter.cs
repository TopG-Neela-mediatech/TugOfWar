using AssetKits.ParticleImage;
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

        [Header("Win animation prefabs (rigged, optional - the win image is used when empty)")]
        [Tooltip("Pinku prefab. It is spawned inside the PlayerWin rect, fitted to that rect, and plays Celebration State.")]
        [SerializeField] private GameObject playerWinPrefab;
        [SerializeField] private GameObject petWinPrefab;
        [SerializeField] private string playerCelebrationState = "PinkuFrontCelebration";
        [SerializeField] private string petCelebrationState = "DogHappy";
        [Tooltip("Playschool Main: used instead of the dog when the player's selected pet is the cat.")]
        [SerializeField] private GameObject petWinCatPrefab;
        [SerializeField] private string petCatCelebrationState = "CatHappy";
        [Tooltip("1 = the animation fills the win rect (keeps its proportions). Lower it if the jump clips.")]
        [Range(0.3f, 1.2f)] [SerializeField] private float fitScale = 0.85f;

        [Tooltip("Hidden while a win image is showing, restored on reset: rope, player, pet, both win lines.")]
        [SerializeField] private GameObject[] hideOnWin;

        [Header("Confetti (plays only when the PLAYER wins)")]
        [SerializeField] private ParticleImage confettiShower;
        [SerializeField] private ParticleImage confettiBurst;

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
        private GameObject playerAnim, petAnim;
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
            if (img != null) { img.color = r.color; img.enabled = true; }
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
            if (playerAnim != null) playerAnim.SetActive(false);
            if (petAnim != null) petAnim.SetActive(false);
            if (hideOnWin != null)
                foreach (var go in hideOnWin)
                    if (go != null) go.SetActive(true);
            StopConfetti();
            IsShowing = false;
        }

        /// <summary>Removes the celebrating character (called right before the end panel pops up). Confetti keeps going.</summary>
        public void HideCelebration()
        {
            DOTween.Kill(TweenId);
            Restore(playerWin, playerRest);
            Restore(petWin, petRest);
            if (playerAnim != null) playerAnim.SetActive(false);
            if (petAnim != null) petAnim.SetActive(false);
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
            rt.localScale = rest.scale;
            var img = rt.GetComponent<Image>();

            // rigged animation prefab replaces the still image
            bool animated = ShowAnimation(playerWon, rt, img);
            rt.localScale = rest.scale * 0.2f;
            if (animated)
            {
                IsShowing = true;
                if (playerWon) PlayConfetti();
                DOTween.Sequence().SetId(TweenId)
                    .Append(rt.DOScale(rest.scale * 1.12f, popTime * 0.55f).SetEase(Ease.OutQuad))
                    .Append(rt.DOScale(rest.scale, popTime * 0.45f).SetEase(Ease.OutBack));
                return;
            }
            if (img != null)
            {
                var c = rest.color; c.a = 0f; img.color = c;
                img.DOFade(rest.color.a, 0.2f).SetId(TweenId);
            }
            IsShowing = true;
            if (playerWon) PlayConfetti();

            // pop in (overshoot, settle), then jump forever
            var pop = DOTween.Sequence().SetId(TweenId);
            pop.Append(rt.DOScale(rest.scale * 1.18f, popTime * 0.55f).SetEase(Ease.OutQuad));
            pop.Append(rt.DOScale(rest.scale, popTime * 0.45f).SetEase(Ease.OutBack));
            pop.AppendCallback(() => StartJumpLoop(rt, rest));
        }

        // ------------------------------------------------------------------ Rigged animation prefab on the canvas

        private bool ShowAnimation(bool playerWon, RectTransform rt, Image img)
        {
            var prefab = playerWon ? playerWinPrefab : petWinPrefab;
            string state = playerWon ? playerCelebrationState : petCelebrationState;
#if PLAYSCHOOL_MAIN
            if (!playerWon && petWinCatPrefab != null)
            {
                PetType pet = PlayroomSaveService.GetSelectedPet();

                if (pet == PetType.Cat)
                {
                    // Handle the cat activity.
                    prefab = petWinCatPrefab;
                    state = petCatCelebrationState;
                }
                else
                {
                    // handle the dog
                }
            }
#endif
            if (prefab == null) return false;

            ref GameObject inst = ref (playerWon ? ref playerAnim : ref petAnim);
            if (inst == null)
            {
                inst = Instantiate(prefab, rt);
                inst.name = prefab.name + " (Win)";
                var t = inst.transform;
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
                Fit(inst, rt);
            }
            inst.SetActive(true);
            if (img != null) img.enabled = false;   // the rig is shown instead of the still image

            var anim = inst.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                anim.Rebind();
                anim.Play(state, 0, 0f);
                anim.Update(0f);
            }
            return true;
        }

        // Sprite renderers sit above the canvas content, then the rig is scaled to fit the win rect and
        // stands on its bottom edge, centred.
        private void Fit(GameObject inst, RectTransform rect)
        {
            var renderers = inst.GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers.Length == 0) return;

            var canvas = rect.GetComponentInParent<Canvas>();
            int order = canvas != null ? canvas.rootCanvas.sortingOrder : 0;
            string layer = canvas != null ? SortingLayer.IDToName(canvas.rootCanvas.sortingLayerID) : "Default";
            foreach (var r in renderers)
            {
                r.sortingLayerName = layer;
                r.sortingOrder = order + 5 + r.sortingOrder;
            }

            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            if (b.size.x < 0.0001f || b.size.y < 0.0001f) return;

            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float rectW = Vector3.Distance(corners[0], corners[3]);
            float rectH = Vector3.Distance(corners[0], corners[1]);

            float k = Mathf.Min(rectW / b.size.x, rectH / b.size.y) * fitScale;
            inst.transform.localScale = Vector3.one * k;

            // re-measure, then put the feet on the bottom edge and centre horizontally
            b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            Vector3 bottomCentre = (corners[0] + corners[3]) * 0.5f;
            Vector3 d = bottomCentre - new Vector3(b.center.x, b.min.y, b.center.z);
            d.z = 0f;
            inst.transform.position += d;
        }

        private void PlayConfetti()
        {
            Fire(confettiBurst);
            Fire(confettiShower);
        }

        private void StopConfetti()
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
