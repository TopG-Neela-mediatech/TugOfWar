using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.tugofwar
{
    public enum Face { Calm, Effort, Strain, Sad }

    /// <summary>
    /// Visual for the Player or the Pet: a BODY image plus a swappable FACE image, both scaled by one shared factor so
    /// they always match. Every pose is a DOTween "feel" (lean, shake, bob, squash) and picks a face; while straining
    /// the face alternates between two sprites so the character looks animated. No animation frames needed.
    /// The character pivots on its rope-attach point (hands / mouth) so the rope always stays connected.
    /// </summary>
    public class CharacterView : MonoBehaviour
    {
        [Header("Body + face")]
        [SerializeField] private Image uiImage;
        [SerializeField] private Image faceImage;
        [Tooltip("Optional placeholder shapes hidden when real art is applied.")]
        [SerializeField] private GameObject[] placeholderParts;
        [SerializeField] private Transform punchTarget;

        [Header("Art layout (sprite pixels, origin = top-left of the BODY sprite)")]
        [Tooltip("Canvas px per sprite px. Body and faces share it, so their scales match.")]
        [SerializeField] private float pixelScale = 0.38f;
        [Tooltip("Where the rope is held (kid: hand tip, dog: mouth). The character pivots here, and its position IS the rope end.")]
        [SerializeField] private Vector2 ropePx = new Vector2(945f, 487f);
        [Tooltip("Body pixel where the face attaches (kid: top of the neck, dog: centre of the head blob).")]
        [SerializeField] private Vector2 faceAttachPx = new Vector2(478f, 330f);
        [Tooltip("Point of the FACE sprite (0..1) that sits on the attach pixel. Chin for the kid, middle of the head for the dog.")]
        [SerializeField] private Vector2 faceAlign = new Vector2(0.55f, 0.03f);
        [Tooltip("Extra size multiplier for faces only (fine tune head size).")]
        [SerializeField] private float faceScale = 1f;
        [Tooltip("Mirror the face (the dog's faces look right but the dog faces left).")]
        [SerializeField] private bool flipFace;
        [Tooltip("Mirror the BODY and everything on it. The dog art faces right, the dog must face left. Faces then need no flip of their own.")]
        [SerializeField] private bool flipBody;

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

        // face animation
        private readonly Sprite[] faces = new Sprite[4];
        private bool hasFaces;
        private Face[] facePlan = { Face.Calm };
        private float faceFps;
        private float faceTimer;
        private int faceIndex;

        private Transform Target => punchTarget != null ? punchTarget : transform;

        private void Awake()
        {
            baseScale = Target.localScale;
            rt = (RectTransform)Target;
            baseAnchoredPos = rt.anchoredPosition;
            baseLocalRot = Target.localRotation;
            ready = true;
        }

        // ------------------------------------------------------------------ Art

        /// <summary>
        /// Applies body + faces from AssetsSO and lays them out. Null body keeps the placeholder.
        /// Safe to call in the Editor as well, so the scene shows the real art without pressing Play.
        /// </summary>
        public void ApplyArt(Sprite body, Sprite calm, Sprite effort, Sprite strain, Sprite sad)
        {
            if (body == null || uiImage == null) return;

            float k = pixelScale;
            Vector2 bs = body.rect.size;
            var bodyRt = uiImage.rectTransform;
            uiImage.sprite = body;
            uiImage.type = Image.Type.Simple;
            uiImage.preserveAspect = false;
            uiImage.color = Color.white;
            bodyRt.sizeDelta = bs * k;
            var pivot = new Vector2(ropePx.x / bs.x, 1f - ropePx.y / bs.y);
            bodyRt.pivot = pivot;
            bodyRt.localScale = new Vector3(flipBody ? -1f : 1f, 1f, 1f);   // mirrors about the pivot, so the rope-hold point stays put

            if (placeholderParts != null)
                foreach (var part in placeholderParts)
                    if (part != null) part.SetActive(false);

            faces[(int)Face.Calm] = calm;
            faces[(int)Face.Effort] = effort != null ? effort : calm;
            faces[(int)Face.Strain] = strain != null ? strain : calm;
            faces[(int)Face.Sad] = sad != null ? sad : calm;
            hasFaces = calm != null && faceImage != null;

            if (faceImage != null)
            {
                faceImage.gameObject.SetActive(hasFaces);
                var fr = faceImage.rectTransform;
                fr.anchorMin = fr.anchorMax = pivot;                       // anchor on the body's pivot ...
                fr.pivot = faceAlign;
                fr.anchoredPosition = new Vector2((faceAttachPx.x - ropePx.x) * k, (ropePx.y - faceAttachPx.y) * k);   // ... then offset
                fr.localScale = new Vector3(flipFace ? -1f : 1f, 1f, 1f);
                faceImage.preserveAspect = false;
                faceImage.color = Color.white;
                faceImage.raycastTarget = false;
                if (hasFaces) { ShowFace(Face.Calm); RefreshFacePlan(); }
            }
        }

        private void ShowFace(Face f)
        {
            if (!hasFaces) return;
            var s = faces[(int)f];
            if (s == null) s = faces[(int)Face.Calm];
            faceImage.sprite = s;
            faceImage.rectTransform.sizeDelta = s.rect.size * pixelScale * faceScale;
        }

        // calm / effort / strain / sad by pose; two entries = the face flips between them (the "animation")
        private static Face[] PlanFor(Pose pose, out float fps)
        {
            fps = 0f;
            switch (pose)
            {
                case Pose.RopeHold:
                case Pose.NormalPull: return new[] { Face.Effort };
                case Pose.StrongPull: fps = 3f; return new[] { Face.Effort, Face.Strain };
                case Pose.VeryStrongPull:
                case Pose.FinalPull: fps = 5f; return new[] { Face.Effort, Face.Strain };
                case Pose.PowerPull: fps = 7f; return new[] { Face.Effort, Face.Strain };
                case Pose.Tired:
                case Pose.Lose: return new[] { Face.Sad };
                case Pose.LosingGround: fps = 2f; return new[] { Face.Strain, Face.Sad };
                default: return new[] { Face.Calm };   // idle, ready, recover, near win, winning ground, celebrate
            }
        }

        private void RefreshFacePlan()
        {
            facePlan = PlanFor(CurrentPose, out faceFps);
            faceIndex = 0;
            faceTimer = 0f;
            ShowFace(facePlan[0]);
        }

        private void Update()
        {
            if (!hasFaces || facePlan.Length < 2) return;
            faceTimer += Time.deltaTime;
            float step = 1f / Mathf.Max(0.5f, faceFps);
            while (faceTimer >= step)
            {
                faceTimer -= step;
                faceIndex = (faceIndex + 1) % facePlan.Length;
                ShowFace(facePlan[faceIndex]);
            }
        }

        // ------------------------------------------------------------------ Poses (all tween)

        // lean degrees (toward the character's own side), shake degrees + period, bob pixels + period, scale
        private struct Feel
        {
            public float lean, shake, shakeTime, bob, bobTime, scale;
            public Feel(float lean, float shake = 0f, float shakeTime = 0.1f, float bob = 0f, float bobTime = 0.3f, float scale = 1f)
            { this.lean = lean; this.shake = shake; this.shakeTime = shakeTime; this.bob = bob; this.bobTime = bobTime; this.scale = scale; }
        }

        // Smaller leans than a free-standing character: the body pivots on the rope-hold point, so big angles would swing the feet.
        // Bobs are kept small too: the rope does not bob, so a big bounce would slide the hands / mouth off the rope end.
        private static Feel FeelFor(Pose pose)
        {
            switch (pose)
            {
                case Pose.Idle: return new Feel(0f, 0f, 0.1f, 3f, 0.9f);
                case Pose.Ready: return new Feel(2f, 0f, 0.1f, 3f, 0.6f);
                case Pose.RopeHold: return new Feel(3f, 1f, 0.16f);
                case Pose.NormalPull: return new Feel(5f, 1.5f, 0.12f);
                case Pose.StrongPull: return new Feel(8f, 2f, 0.09f);
                case Pose.VeryStrongPull: return new Feel(10f, 2.5f, 0.07f, 0f, 0.3f, 1.03f);
                case Pose.PowerPull: return new Feel(13f, 3f, 0.06f, 0f, 0.3f, 1.08f);
                case Pose.NearWin: return new Feel(7f, 1.5f, 0.1f, 6f, 0.18f);
                case Pose.FinalPull: return new Feel(14f, 3f, 0.07f, 0f, 0.3f, 1.06f);
                case Pose.Tired: return new Feel(-5f, 1f, 0.5f, 4f, 0.7f, 0.96f);
                case Pose.Recover: return new Feel(3f, 0f, 0.1f, 4f, 0.4f);
                case Pose.WinningGround: return new Feel(6f, 1.5f, 0.1f);
                case Pose.LosingGround: return new Feel(-3f, 2.5f, 0.08f);
                default: return new Feel(0f);
            }
        }

        public void SetPose(Pose pose, bool force = false)
        {
            if (!force && hasPose && pose == CurrentPose) return;
            CurrentPose = pose;
            hasPose = true;
            RefreshFacePlan();                       // face reacts to every pose, including celebrate / lose
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
