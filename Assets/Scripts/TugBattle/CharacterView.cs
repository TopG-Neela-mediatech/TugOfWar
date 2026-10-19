using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.tugofwar
{
    /// <summary>
    /// Visual for the Player or the Pet: ONE sprite, every pose is a DOTween "feel" (lean, shake, bob, squash).
    /// No animation frames needed. The sprite comes from AssetsSO (player / pet) via TugAssetsApplier.
    /// </summary>
    public class CharacterView : MonoBehaviour
    {
        [SerializeField] private Image uiImage;
        [SerializeField] private Transform punchTarget;
        [Tooltip("Placeholder child shapes (head, eyes...) hidden as soon as real art is assigned.")]
        [SerializeField] private GameObject[] placeholderParts;

        [Header("Direction")]
        [Tooltip("+1 = the character is on the LEFT (leans back to the left). -1 = on the RIGHT.")]
        [SerializeField] private float leanDirection = 1f;

        [Header("Tap punch")]
        [SerializeField] private float punchScale = 0.08f;
        [SerializeField] private float punchTime = 0.18f;

        [Header("Reactions")]
        [SerializeField] private float jumpHeight = 50f;
        [Tooltip("Pet: negative = slides toward the Player when the Player wins. Player: leave 0.")]
        [SerializeField] private float slideX = 0f;

        public Pose CurrentPose { get; private set; } = Pose.Idle;

        private Vector3 baseScale = Vector3.one;
        private Vector2 baseAnchoredPos;
        private RectTransform rt;
        private Quaternion baseLocalRot = Quaternion.identity;
        private bool ready;
        private bool hasPose;
        private string PoseId => "pose" + GetInstanceID();
        private string PunchId => "punch" + GetInstanceID();

        private Transform Target => punchTarget != null ? punchTarget : transform;

        private void Awake()
        {
            baseScale = Target.localScale;
            rt = (RectTransform)Target;
            baseAnchoredPos = rt.anchoredPosition;
            baseLocalRot = Target.localRotation;
            ready = true;
        }

        /// <summary>Sprite from AssetsSO. Null keeps the placeholder shapes.</summary>
        public void SetSprite(Sprite sprite)
        {
            if (sprite == null || uiImage == null) return;
            uiImage.sprite = sprite;
            uiImage.type = Image.Type.Simple;
            uiImage.preserveAspect = true;
            uiImage.color = Color.white;
            if (placeholderParts != null)
                foreach (var part in placeholderParts)
                    if (part != null) part.SetActive(false);
        }

        // ------------------------------------------------------------------ Poses (all tween)

        // lean degrees (toward the character's own side), shake degrees + period, bob pixels + period, scale
        private struct Feel
        {
            public float lean, shake, shakeTime, bob, bobTime, scale;
            public Feel(float lean, float shake = 0f, float shakeTime = 0.1f, float bob = 0f, float bobTime = 0.3f, float scale = 1f)
            { this.lean = lean; this.shake = shake; this.shakeTime = shakeTime; this.bob = bob; this.bobTime = bobTime; this.scale = scale; }
        }

        private static Feel FeelFor(Pose pose)
        {
            switch (pose)
            {
                case Pose.Idle: return new Feel(0f, 0f, 0.1f, 6f, 0.9f);
                case Pose.Ready: return new Feel(4f, 0f, 0.1f, 6f, 0.6f);
                case Pose.RopeHold: return new Feel(8f, 1.5f, 0.16f);
                case Pose.NormalPull: return new Feel(12f, 2f, 0.12f);
                case Pose.StrongPull: return new Feel(18f, 3f, 0.09f);
                case Pose.VeryStrongPull: return new Feel(24f, 4f, 0.07f, 0f, 0.3f, 1.04f);
                case Pose.PowerPull: return new Feel(30f, 5f, 0.06f, 0f, 0.3f, 1.12f);
                case Pose.NearWin: return new Feel(16f, 2f, 0.1f, 14f, 0.18f);
                case Pose.FinalPull: return new Feel(34f, 4f, 0.07f, 0f, 0.3f, 1.1f);
                case Pose.Tired: return new Feel(-10f, 1f, 0.5f, 8f, 0.7f, 0.94f);
                case Pose.Recover: return new Feel(6f, 0f, 0.1f, 10f, 0.4f);
                case Pose.WinningGround: return new Feel(14f, 2.5f, 0.1f);
                case Pose.LosingGround: return new Feel(-6f, 3.5f, 0.08f);
                default: return new Feel(0f);
            }
        }

        public void SetPose(Pose pose, bool force = false)
        {
            if (!force && hasPose && pose == CurrentPose) return;
            CurrentPose = pose;
            hasPose = true;
            if (!ready) return;

            // Celebrate / Lose are handled by Celebrate() / Slump() - leave those tweens alone
            if (pose == Pose.Celebrate || pose == Pose.Lose) return;

            DOTween.Kill(PoseId);
            var f = FeelFor(pose);
            float dir = leanDirection;
            var t = Target;

            t.DOLocalRotate(new Vector3(0f, 0f, f.lean * dir), 0.18f).SetEase(Ease.OutBack).SetId(PoseId).OnComplete(() =>
            {
                if (f.shake > 0.01f)
                    t.DOLocalRotate(new Vector3(0f, 0f, (f.lean + f.shake) * dir), f.shakeTime)
                        .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetId(PoseId);
            });
            t.DOScale(baseScale * f.scale, 0.18f).SetEase(Ease.OutBack).SetId(PoseId);
            rt.DOAnchorPosY(baseAnchoredPos.y, 0.12f).SetId(PoseId).OnComplete(() =>
            {
                if (f.bob > 0.01f)
                    rt.DOAnchorPosY(baseAnchoredPos.y + f.bob, f.bobTime)
                        .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetId(PoseId);
            });
        }

        /// <summary>Quick squash on every tap.</summary>
        public void Punch()
        {
            if (!ready) return;
            DOTween.Kill(PunchId, true);
            Target.DOPunchScale(Vector3.one * punchScale, punchTime, 6, 0.8f).SetId(PunchId);
        }

        // ------------------------------------------------------------------ Reactions

        /// <summary>Kill every tween and return to the start transform (call at the start of every round).</summary>
        public void ResetTween()
        {
            if (!ready) return;
            Target.DOKill();
            DOTween.Kill(PoseId);
            DOTween.Kill(PunchId);
            Target.localScale = baseScale;
            rt.anchoredPosition = baseAnchoredPos;
            Target.localRotation = baseLocalRot;
            hasPose = false;
        }

        /// <summary>Happy loop: bouncing jump with a little wiggle.</summary>
        public void Celebrate()
        {
            Target.DOKill();
            DOTween.Kill(PoseId);
            Target.localScale = baseScale;
            Target.localRotation = baseLocalRot;
            rt.DOAnchorPosY(baseAnchoredPos.y + jumpHeight, 0.3f).SetEase(Ease.OutQuad).SetLoops(-1, LoopType.Yoyo);
            Target.DOLocalRotate(new Vector3(0f, 0f, 8f), 0.3f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

        /// <summary>Funny lose: tilt over and sink a little.</summary>
        public void Slump()
        {
            Target.DOKill();
            DOTween.Kill(PoseId);
            Target.localScale = baseScale;
            Target.DOLocalRotate(new Vector3(0f, 0f, 14f * leanDirection), 0.5f).SetEase(Ease.OutBack);
            rt.DOAnchorPosY(baseAnchoredPos.y - jumpHeight * 0.4f, 0.5f).SetEase(Ease.OutBounce);
        }

        /// <summary>Pet sliding forward while still holding the rope.</summary>
        public void SlideForward()
        {
            rt.DOAnchorPosX(baseAnchoredPos.x + slideX, 0.6f).SetEase(Ease.OutCubic);
        }

        private void OnDisable()
        {
            if (!ready) return;
            Target.DOKill();
            DOTween.Kill(PoseId);
            DOTween.Kill(PunchId);
            Target.localScale = baseScale;
        }
    }
}
