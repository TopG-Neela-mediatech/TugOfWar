using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.tugofwar
{
    /// <summary>
    /// Moves the whole "rope group" (rope, flag, Player AND Pet as children) left/right as the rope value changes.
    /// Rope value: 0 = centre, -100 = Player Win Line (left), +100 = Pet Win Line (right).
    /// Win lines themselves stay fixed in the scene. Only ONE rope sprite is needed - stretch / tension is a tween.
    /// </summary>
    public class RopeView : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private RectTransform ropeRoot;
        [Tooltip("Canvas pixels the group travels when rope = 100, i.e. centre -> win line.")]
        [SerializeField] private float maxOffset = 264f;

        [Header("Rope + flag")]
        [SerializeField] private Image ropeImage;
        [SerializeField] private RectTransform flag;
        [SerializeField] private Image flagImage;
        [SerializeField] private float flagSwayAngle = 6f;
        [SerializeField] private float flagSwaySpeed = 4f;

        private float startAnchoredX;
        private float ropeRestY;
        private RopeState state = (RopeState)(-1);
        private float strain;
        private string RopeId => "rope" + GetInstanceID();

        private void Awake()
        {
            startAnchoredX = ropeRoot.anchoredPosition.x;
            if (ropeImage != null) ropeRestY = ropeImage.rectTransform.localPosition.y;
        }

        public void SetRope(float rope)
        {
            var p = ropeRoot.anchoredPosition;
            p.x = startAnchoredX + rope / 100f * maxOffset;
            ropeRoot.anchoredPosition = p;
        }

        /// <summary>0..1 how hard the rope is being pulled; drives flag sway.</summary>
        public void SetStrain(float value) => strain = Mathf.Clamp01(value);

        private void Update()
        {
            if (flag == null) return;
            float a = Mathf.Sin(Time.time * flagSwaySpeed) * flagSwayAngle * (0.4f + strain);
            flag.localRotation = Quaternion.Euler(0f, 0f, a);
        }

        /// <summary>Sprites from AssetsSO. Null keeps the placeholder.</summary>
        public void ApplySprites(Sprite rope, Sprite flagSprite)
        {
            if (rope != null && ropeImage != null)
            {
                ropeImage.sprite = rope;
                ropeImage.type = Image.Type.Simple;
                ropeImage.preserveAspect = true;
                ropeImage.color = Color.white;
            }
            if (flagSprite != null && flagImage != null)
            {
                flagImage.sprite = flagSprite;
                flagImage.type = Image.Type.Simple;
                flagImage.preserveAspect = true;
                flagImage.color = Color.white;
                // hide the placeholder pole/cloth pieces that sit under the flag object
                foreach (Transform child in flagImage.transform) child.gameObject.SetActive(false);
            }
        }

        /// <summary>Rope tension look: thinner and slightly longer as the pull gets harder (tween, no extra sprites).</summary>
        public void SetState(RopeState s)
        {
            if (s == state || ropeImage == null) return;
            state = s;

            Vector3 scale;
            float shake = 0f;
            switch (s)
            {
                case RopeState.Stretch: scale = new Vector3(1.03f, 0.75f, 1f); break;
                case RopeState.StrongPull: scale = new Vector3(1.05f, 0.6f, 1f); shake = 3f; break;
                case RopeState.PowerPull: scale = new Vector3(1.08f, 0.45f, 1f); shake = 5f; break;
                case RopeState.PlayerPull:
                case RopeState.PetPull: scale = new Vector3(1.01f, 0.92f, 1f); break;
                default: scale = Vector3.one; break;
            }

            var rt = ropeImage.rectTransform;
            DOTween.Kill(RopeId);
            rt.DOScale(scale, 0.12f).SetEase(Ease.OutQuad).SetId(RopeId);
            if (shake > 0f)
                rt.DOLocalMoveY(ropeRestY + shake, 0.05f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetId(RopeId);
            else
                rt.DOLocalMoveY(ropeRestY, 0.12f).SetId(RopeId);
        }

        public void FlagShake(float strength = 1f)
        {
            if (flag == null) return;
            flag.DOKill(true);
            flag.DOPunchScale(new Vector3(0.12f, 0.12f, 0f) * strength, 0.25f, 8, 0.6f);
        }

        private void OnDisable()
        {
            DOTween.Kill(RopeId);
            if (flag != null) flag.DOKill();
        }
    }
}
