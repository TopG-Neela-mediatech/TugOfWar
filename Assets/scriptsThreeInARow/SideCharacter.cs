using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.threeinarow
{
    /// <summary>
    /// Tapu / the Pet standing beside the board. Uses the animated rig prefab (Pinku / Dog from Tug Battle) shown on the
    /// canvas and fitted into this object's rect, and plays its idle / celebrate / sad animation clips.
    /// On top of the clips, DOTween adds the small reactions: nod, thinking head tilt, excited hops, surprise, slump.
    /// With no rig prefab it falls back to a single sprite (AssetsSO) or the placeholder box.
    /// </summary>
    public class SideCharacter : MonoBehaviour
    {
        [SerializeField] private Image body;
        [Tooltip("1 = faces right, -1 = faces left (only used to lean the right way).")]
        [SerializeField] private float facing = 1f;

        [Header("Rig (animated prefab)")]
        [SerializeField] private GameObject rigPrefab;
        [Tooltip("Mirror the rig so it faces the other way.")]
        [SerializeField] private bool flipRig;
        [Range(0.3f, 1.2f)] [SerializeField] private float fitScale = 0.9f;
        [Tooltip("Animator state names. Leave a name empty to keep playing the idle clip.")]
        [SerializeField] private string idleState = "";
        [SerializeField] private string winState = "";
        [SerializeField] private string sadState = "";

        private RectTransform Rt => (RectTransform)transform;
        private string TweenId => "side" + GetInstanceID();
        private Vector2 rest;
        private bool cached;
        private GameObject rig;
        private Animator rigAnimator;

        private void Awake() => Cache();

        private void Cache()
        {
            if (cached) return;
            rest = Rt.anchoredPosition;
            cached = true;
        }

        /// <summary>Single-sprite fallback (used only when there is no rig prefab).</summary>
        public void ApplyArt(Sprite sprite)
        {
            if (rigPrefab != null || sprite == null || body == null) return;
            body.sprite = sprite;
            body.type = Image.Type.Simple;
            body.preserveAspect = true;
            body.color = Color.white;
            foreach (Transform child in transform) child.gameObject.SetActive(false);   // placeholder label
        }

        // ------------------------------------------------------------------ Rig

        private void BuildRig()
        {
            if (rigPrefab == null || rig != null) return;
            rig = Instantiate(rigPrefab, Rt);
            rig.name = rigPrefab.name + " (Rig)";
            var t = rig.transform;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            rigAnimator = rig.GetComponentInChildren<Animator>();

            // the rig replaces the placeholder box and its label
            if (body != null) body.enabled = false;
            foreach (Transform child in transform) if (child.gameObject != rig) child.gameObject.SetActive(false);

            Fit();
        }

        // Sprite renderers sit above the canvas content; the rig is scaled to fit this rect and stands on its bottom edge.
        private void Fit()
        {
            var renderers = rig.GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers.Length == 0) return;

            var canvas = Rt.GetComponentInParent<Canvas>();
            int order = canvas != null ? canvas.rootCanvas.sortingOrder : 0;
            string layer = canvas != null ? SortingLayer.IDToName(canvas.rootCanvas.sortingLayerID) : "Default";
            foreach (var r in renderers)
            {
                r.sortingLayerName = layer;
                r.sortingOrder = order + 5 + r.sortingOrder;
            }

            Bounds b = Measure(renderers);
            if (b.size.x < 0.0001f || b.size.y < 0.0001f) return;

            var corners = new Vector3[4];
            Rt.GetWorldCorners(corners);
            float rectW = Vector3.Distance(corners[0], corners[3]);
            float rectH = Vector3.Distance(corners[0], corners[1]);
            float k = Mathf.Min(rectW / b.size.x, rectH / b.size.y) * fitScale;
            rig.transform.localScale = new Vector3(flipRig ? -k : k, k, k);

            b = Measure(renderers);
            Vector3 bottomCentre = (corners[0] + corners[3]) * 0.5f;
            Vector3 d = bottomCentre - new Vector3(b.center.x, b.min.y, b.center.z);
            d.z = 0f;
            rig.transform.position += d;
        }

        private static Bounds Measure(SpriteRenderer[] renderers)
        {
            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }

        private void PlayClip(string state)
        {
            if (rig == null) return;
            rig.SetActive(true);
            if (rigAnimator == null) return;
            if (string.IsNullOrEmpty(state)) state = idleState;
            if (string.IsNullOrEmpty(state) || !rigAnimator.HasState(0, Animator.StringToHash(state))) return;
            rigAnimator.Play(state, 0, 0f);
        }

        /// <summary>Removes the character (called right before the end panel appears; the rig would draw over it).</summary>
        public void Hide()
        {
            DOTween.Kill(TweenId);
            RestPose();
            if (rig != null) rig.SetActive(false);
        }

        // ------------------------------------------------------------------ Poses

        private void RestPose()
        {
            Cache();
            BuildRig();   // first use: after the canvas has been scaled, so the fit is exact
            DOTween.Kill(TweenId);
            Rt.anchoredPosition = rest;
            Rt.localScale = Vector3.one;
            Rt.localRotation = Quaternion.identity;
        }

        public void Idle()
        {
            RestPose();
            PlayClip(idleState);
            var s = DOTween.Sequence().SetId(TweenId).SetLoops(-1);
            s.Append(Rt.DOScale(new Vector3(1.015f, 0.985f, 1f), 1.2f).SetEase(Ease.InOutSine));
            s.Append(Rt.DOScale(Vector3.one, 1.2f).SetEase(Ease.InOutSine));
        }

        /// <summary>"Your turn": a happy nod.</summary>
        public void Nod()
        {
            RestPose();
            PlayClip(idleState);
            var s = DOTween.Sequence().SetId(TweenId);
            s.Append(Rt.DOLocalRotate(new Vector3(0, 0, -5f * facing), 0.15f));
            s.Append(Rt.DOLocalRotate(new Vector3(0, 0, 3f * facing), 0.18f));
            s.Append(Rt.DOLocalRotate(Vector3.zero, 0.15f));
            s.OnComplete(Idle);
        }

        /// <summary>Head-tilt thinking, repeats until another pose is called.</summary>
        public void Think()
        {
            RestPose();
            PlayClip(idleState);
            var s = DOTween.Sequence().SetId(TweenId).SetLoops(-1);
            s.Append(Rt.DOLocalRotate(new Vector3(0, 0, 8f * facing), 0.35f).SetEase(Ease.InOutSine));
            s.AppendInterval(0.2f);
            s.Append(Rt.DOLocalRotate(new Vector3(0, 0, -5f * facing), 0.45f).SetEase(Ease.InOutSine));
            s.AppendInterval(0.15f);
        }

        /// <summary>Two in a row: quick excited hops.</summary>
        public void Excited()
        {
            RestPose();
            PlayClip(winState);
            var s = DOTween.Sequence().SetId(TweenId);
            for (int i = 0; i < 2; i++)
            {
                s.Append(Rt.DOAnchorPosY(rest.y + 34f, 0.14f).SetEase(Ease.OutQuad));
                s.Append(Rt.DOAnchorPosY(rest.y, 0.12f).SetEase(Ease.InQuad));
            }
            s.OnComplete(Idle);
        }

        /// <summary>Win: the celebration clip plus big jumps with a squash on landing, forever.</summary>
        public void Win()
        {
            RestPose();
            PlayClip(winState);
            var s = DOTween.Sequence().SetId(TweenId).SetLoops(-1);
            s.Append(Rt.DOAnchorPosY(rest.y + 60f, 0.3f).SetEase(Ease.OutQuad));
            s.Join(Rt.DOScale(new Vector3(0.96f, 1.06f, 1f), 0.3f));
            s.Append(Rt.DOAnchorPosY(rest.y, 0.26f).SetEase(Ease.InQuad));
            s.Join(Rt.DOScale(new Vector3(1.08f, 0.92f, 1f), 0.26f));
            s.Append(Rt.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutBack));
        }

        /// <summary>The other side won: a surprised pop, then a clapping sway (congratulating).</summary>
        public void Surprise()
        {
            RestPose();
            PlayClip(idleState);
            var s = DOTween.Sequence().SetId(TweenId);
            s.Append(Rt.DOScale(1.15f, 0.12f).SetEase(Ease.OutQuad));
            s.Join(Rt.DOAnchorPosY(rest.y + 24f, 0.12f));
            s.Append(Rt.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
            s.Join(Rt.DOAnchorPosY(rest.y, 0.2f));
            s.AppendInterval(0.2f);
            s.OnComplete(Clap);
        }

        /// <summary>Draw / congratulating: the happy clip with a gentle rhythmic sway.</summary>
        public void Clap()
        {
            RestPose();
            PlayClip(winState);
            var s = DOTween.Sequence().SetId(TweenId).SetLoops(-1);
            s.Append(Rt.DOScale(new Vector3(0.97f, 1.04f, 1f), 0.18f).SetEase(Ease.OutQuad));
            s.Append(Rt.DOScale(new Vector3(1.03f, 0.98f, 1f), 0.18f).SetEase(Ease.InQuad));
            s.Join(Rt.DOLocalRotate(new Vector3(0, 0, 3f), 0.18f));
            s.Append(Rt.DOLocalRotate(new Vector3(0, 0, -3f), 0.2f));
        }

        /// <summary>Lost: the sad clip (if there is one) with a small slump, then back to a smile (encouraging).</summary>
        public void Slump()
        {
            RestPose();
            PlayClip(sadState);
            var s = DOTween.Sequence().SetId(TweenId);
            s.Append(Rt.DOAnchorPosY(rest.y - 14f, 0.3f).SetEase(Ease.OutQuad));
            s.Join(Rt.DOScale(new Vector3(1.04f, 0.94f, 1f), 0.3f));
            s.AppendInterval(0.9f);
            s.Append(Rt.DOAnchorPosY(rest.y, 0.3f).SetEase(Ease.OutBack));
            s.Join(Rt.DOScale(Vector3.one, 0.3f));
            s.OnComplete(Idle);
        }

        public void ResetPose() => Idle();

        private void OnDestroy() => DOTween.Kill(TweenId);
    }
}
