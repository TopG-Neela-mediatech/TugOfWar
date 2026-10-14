using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.tugofwar
{
    /// <summary>
    /// Visual for the Player or the Pet. Two ways to drive art (use either or both):
    ///  1) Pose sprite lists below (frames flip at fps) - works with SpriteRenderer or UI Image.
    ///  2) An Animator with an int parameter "Pose" (value = (int)Pose enum).
    /// </summary>
    public class CharacterView : MonoBehaviour
    {
        [Serializable]
        public class PoseEntry
        {
            public Pose pose;
            public Sprite[] frames;
            public float fps = 8f;
            public bool loop = true;
        }

        [Header("Render target (assign one)")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Image uiImage;

        [Header("Optional Animator (int param 'Pose')")]
        [SerializeField] private Animator animator;

        [Header("Poses")]
        [SerializeField] private PoseEntry[] poses;

        [Header("Tap punch")]
        [SerializeField] private Transform punchTarget;
        [SerializeField] private float punchScale = 0.08f;
        [SerializeField] private float punchTime = 0.18f;

        private static readonly int PoseParam = Animator.StringToHash("Pose");

        public Pose CurrentPose { get; private set; } = Pose.Idle;

        private PoseEntry current;
        private float frameTimer;
        private int frameIndex;
        private Vector3 baseScale;
        private bool hasBaseScale;

        private Transform Target => punchTarget != null ? punchTarget : transform;

        private void Awake()
        {
            baseScale = Target.localScale;
            hasBaseScale = true;
        }

        public void SetPose(Pose pose, bool force = false)
        {
            if (!force && pose == CurrentPose && current != null) return;
            CurrentPose = pose;

            if (animator != null) animator.SetInteger(PoseParam, (int)pose);

            current = null;
            if (poses != null)
            {
                for (int i = 0; i < poses.Length; i++)
                {
                    if (poses[i].pose == pose && poses[i].frames != null && poses[i].frames.Length > 0)
                    {
                        current = poses[i];
                        break;
                    }
                }
            }
            frameIndex = 0;
            frameTimer = 0f;
            if (current != null) ApplySprite(current.frames[0]);
        }

        private void Update()
        {
            if (current == null || current.frames.Length < 2) return;
            frameTimer += Time.deltaTime;
            float step = 1f / Mathf.Max(1f, current.fps);
            while (frameTimer >= step)
            {
                frameTimer -= step;
                frameIndex++;
                if (frameIndex >= current.frames.Length)
                    frameIndex = current.loop ? 0 : current.frames.Length - 1;
                ApplySprite(current.frames[frameIndex]);
            }
        }

        private void ApplySprite(Sprite s)
        {
            if (spriteRenderer != null) spriteRenderer.sprite = s;
            if (uiImage != null) uiImage.sprite = s;
        }

        /// <summary>Quick squash on every tap.</summary>
        public void Punch()
        {
            if (!hasBaseScale) return;
            Target.DOKill(true);
            Target.localScale = baseScale;
            Target.DOPunchScale(Vector3.one * punchScale, punchTime, 6, 0.8f);
        }

        private void OnDisable()
        {
            if (hasBaseScale)
            {
                Target.DOKill();
                Target.localScale = baseScale;
            }
        }
    }
}
