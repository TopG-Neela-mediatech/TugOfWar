using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.tugofwar
{
    /// <summary>
    /// Moves the whole "rope group" (rope, flag, Player AND Pet as children) left/right as the rope value changes.
    /// Rope value: 0 = centre, -100 = Player Win Line (left), +100 = Pet Win Line (right).
    /// Win lines themselves stay fixed in the scene.
    /// </summary>
    public class RopeView : MonoBehaviour
    {
        [Serializable]
        public class RopeSprite
        {
            public RopeState state;
            public Sprite sprite;
        }

        [Header("Movement")]
        [SerializeField] private Transform ropeRoot;
        [Tooltip("Distance (local units / pixels) the group travels when rope = 100, i.e. centre -> win line.")]
        [SerializeField] private float maxOffset = 220f;

        [Header("Flag")]
        [SerializeField] private Transform flag;
        [SerializeField] private float flagSwayAngle = 6f;
        [SerializeField] private float flagSwaySpeed = 4f;

        [Header("Rope art (optional)")]
        [SerializeField] private SpriteRenderer ropeSpriteRenderer;
        [SerializeField] private Image ropeImage;
        [SerializeField] private RopeSprite[] ropeSprites;

        private Vector3 startLocalPos;
        private RectTransform ropeRect;
        private float startAnchoredX;
        private RopeState state = RopeState.Neutral;
        private float strain;

        private void Awake()
        {
            ropeRect = ropeRoot as RectTransform;
            startLocalPos = ropeRoot.localPosition;
            if (ropeRect != null) startAnchoredX = ropeRect.anchoredPosition.x;
        }

        public void SetRope(float rope)
        {
            float x = rope / 100f * maxOffset;
            if (ropeRect != null)
            {
                var p = ropeRect.anchoredPosition;
                p.x = startAnchoredX + x;
                ropeRect.anchoredPosition = p;
            }
            else
            {
                ropeRoot.localPosition = new Vector3(startLocalPos.x + x, startLocalPos.y, startLocalPos.z);
            }
        }

        /// <summary>0..1 how hard the rope is being pulled; drives flag sway.</summary>
        public void SetStrain(float value) => strain = Mathf.Clamp01(value);

        private void Update()
        {
            if (flag == null) return;
            float a = Mathf.Sin(Time.time * flagSwaySpeed) * flagSwayAngle * (0.4f + strain);
            flag.localRotation = Quaternion.Euler(0f, 0f, a);
        }

        public void SetState(RopeState s)
        {
            if (s == state) return;
            state = s;
            if (ropeSprites == null) return;
            for (int i = 0; i < ropeSprites.Length; i++)
            {
                if (ropeSprites[i].state != s || ropeSprites[i].sprite == null) continue;
                if (ropeSpriteRenderer != null) ropeSpriteRenderer.sprite = ropeSprites[i].sprite;
                if (ropeImage != null) ropeImage.sprite = ropeSprites[i].sprite;
                return;
            }
        }

        public void FlagShake(float strength = 1f)
        {
            if (flag == null) return;
            flag.DOKill(true);
            flag.DOPunchScale(new Vector3(0.12f, 0.12f, 0f) * strength, 0.25f, 8, 0.6f);
        }

        private void OnDisable()
        {
            if (flag != null) flag.DOKill();
        }
    }
}
