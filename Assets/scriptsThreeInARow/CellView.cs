using System;
using AssetKits.ParticleImage;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.threeinarow
{
    /// <summary>
    /// One square of the 3x3 board. A transparent tap area (this object) with a Tile (the cell colour), a Glow (hint),
    /// the Face (Tapu's or the pet's face, placed when someone plays here) and a Highlight particle effect.
    /// Everything that "animates" is a DOTween: the face pops in, hops, blinks, tilts and breathes, and the tile
    /// changes colour for whoever owns the square.
    /// </summary>
    public class CellView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image tile;
        [SerializeField] private Image face;
        [SerializeField] private Image glow;
        [Tooltip("The AssetKits 'Highlight' particle image, played when the player puts a face here.")]
        [SerializeField] private ParticleImage highlight;

        [Header("Cell colours")]
        [SerializeField] private Color tapuTint = new Color(0.70f, 0.93f, 0.52f, 0.95f);
        [SerializeField] private Color petTint = new Color(1f, 0.74f, 0.48f, 0.95f);
        [SerializeField] private Color winTint = new Color(1f, 0.9f, 0.35f, 1f);

        public int Index { get; private set; }
        public Mark Mark { get; private set; }
        public RectTransform Rect => (RectTransform)transform;
        public bool IsEmpty => Mark == Mark.None;

        private string TweenId => "cell" + GetInstanceID();
        private string TileId => "celltile" + GetInstanceID();
        private Action<int> onClick;
        private Sprite tapuSprite, petSprite, placeholder;
        private Color glowBase = new Color(1f, 0.95f, 0.45f, 1f);
        private Color emptyTint = new Color(1f, 1f, 1f, 0f);

        public void Init(int index, Action<int> clicked)
        {
            if (placeholder == null && face != null) placeholder = face.sprite;
            Index = index;
            onClick = clicked;
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke(Index));
            }
            if (highlight != null) highlight.Stop(true);
            Clear();
        }

        public void ApplyArt(Sprite emptySquare, Sprite tapuFace, Sprite petFace)
        {
            tapuSprite = tapuFace;
            petSprite = petFace;
            if (emptySquare != null && tile != null)
            {
                tile.sprite = emptySquare;
                tile.type = Image.Type.Simple;
                tile.preserveAspect = true;
                emptyTint = Color.white;
                if (Mark == Mark.None) tile.color = emptyTint;
            }
        }

        public void SetInteractable(bool on)
        {
            if (button != null) button.interactable = on && Mark == Mark.None;
        }

        // ------------------------------------------------------------------ Reset

        public void Clear()
        {
            DOTween.Kill(TweenId);
            DOTween.Kill(TileId);
            DOTween.Kill(TweenId + "p");
            Mark = Mark.None;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            if (tile != null) tile.color = emptyTint;
            if (face != null)
            {
                face.gameObject.SetActive(false);
                face.rectTransform.localScale = Vector3.one;
                face.rectTransform.localRotation = Quaternion.identity;
                face.rectTransform.anchoredPosition = Vector2.zero;
                face.color = Color.white;
            }
            SetGlow(0f);
            if (highlight != null) highlight.Stop(true);
            if (button != null) button.interactable = true;
        }

        // ------------------------------------------------------------------ Placing a face

        /// <summary>The face pops in with a cute tween and the tile turns to that side's colour.</summary>
        public void Place(Mark mark)
        {
            DOTween.Kill(TweenId);
            DOTween.Kill(TileId);
            Mark = mark;
            if (button != null) button.interactable = false;
            SetGlow(0f);

            // cell colour: a bright flash that settles into Tapu's / the pet's colour
            if (tile != null)
            {
                tile.color = new Color(1f, 1f, 1f, 0.95f);
                tile.DOColor(mark == Mark.Tapu ? tapuTint : petTint, 0.4f).SetEase(Ease.OutQuad).SetId(TileId);
            }

            Sprite art = mark == Mark.Tapu ? tapuSprite : petSprite;
            face.sprite = art != null ? art : placeholder;   // no art assigned: the placeholder circle
            face.color = art != null ? Color.white : (mark == Mark.Tapu ? new Color(1f, 0.82f, 0.25f) : new Color(0.95f, 0.5f, 0.5f));
            face.gameObject.SetActive(true);

            var rt = face.rectTransform;
            rt.anchoredPosition = Vector2.zero;
            var s = DOTween.Sequence().SetId(TweenId);

            if (mark == Mark.Tapu)
            {
                // Tapu: pops up from nothing with a happy wobble, then two little hops
                rt.localScale = Vector3.one * 0.05f;
                rt.localRotation = Quaternion.Euler(0, 0, -18f);
                s.Append(rt.DOScale(1.3f, 0.2f).SetEase(Ease.OutQuad));
                s.Join(rt.DOLocalRotate(new Vector3(0, 0, 10f), 0.2f));
                s.Append(rt.DOScale(0.92f, 0.1f));
                s.Join(rt.DOLocalRotate(new Vector3(0, 0, -5f), 0.1f));
                s.Append(rt.DOScale(1f, 0.16f).SetEase(Ease.OutBack));
                s.Join(rt.DOLocalRotate(Vector3.zero, 0.16f));
                for (int i = 0; i < 2; i++)
                {
                    s.Append(rt.DOAnchorPosY(20f - i * 8f, 0.11f).SetEase(Ease.OutQuad));
                    s.Join(rt.DOScale(new Vector3(0.95f, 1.07f, 1f), 0.11f));
                    s.Append(rt.DOAnchorPosY(0f, 0.1f).SetEase(Ease.InQuad));
                    s.Join(rt.DOScale(new Vector3(1.08f, 0.92f, 1f), 0.1f));
                    s.Append(rt.DOScale(1f, 0.07f));
                }
            }
            else
            {
                // pet: a playful hop with a squash, then an ear-wiggle shake
                rt.localScale = new Vector3(0.5f, 0.5f, 1f);
                rt.localRotation = Quaternion.identity;
                s.Append(rt.DOAnchorPosY(54f, 0.2f).SetEase(Ease.OutQuad));
                s.Join(rt.DOScale(new Vector3(0.95f, 1.12f, 1f), 0.2f));
                s.Append(rt.DOAnchorPosY(0f, 0.14f).SetEase(Ease.InQuad));
                s.Join(rt.DOScale(new Vector3(1.18f, 0.84f, 1f), 0.14f));
                s.Append(rt.DOScale(1f, 0.14f).SetEase(Ease.OutBack));
                for (int i = 0; i < 2; i++)
                {
                    s.Append(rt.DOLocalRotate(new Vector3(0, 0, 9f), 0.07f));
                    s.Append(rt.DOLocalRotate(new Vector3(0, 0, -9f), 0.12f));
                }
                s.Append(rt.DOLocalRotate(Vector3.zero, 0.07f));
            }
            s.AppendCallback(StartIdle);
        }

        private static readonly Color HighlightTint = new Color(0.45f, 0.78f, 1f, 1f);

        /// <summary>The Highlight particle image bursts over this cell (played when the player places a face).</summary>
        public void PlayHighlight()
        {
            if (highlight == null) return;
            highlight.gameObject.SetActive(true);
            highlight.transform.localScale = Vector3.one;
            // the prefab is white and vanishes on the light tiles: tint it (and the particle images under it) light blue
            foreach (var p in highlight.GetComponentsInChildren<ParticleImage>(true)) p.color = HighlightTint;
            highlight.Stop(true);
            highlight.Play();
        }

        // Idle "alive" loop: it breathes, blinks (a quick squash), looks around (a slow tilt) and now and then does a happy squish.
        private void StartIdle()
        {
            if (face == null || Mark == Mark.None) return;
            var rt = face.rectTransform;
            var idle = DOTween.Sequence().SetId(TweenId).SetLoops(-1);
            idle.AppendInterval(UnityEngine.Random.Range(0.2f, 1.6f));
            idle.Append(rt.DOScale(1.04f, 0.9f).SetEase(Ease.InOutSine));
            idle.Append(rt.DOScale(1f, 0.9f).SetEase(Ease.InOutSine));
            // blink
            idle.Append(rt.DOScale(new Vector3(1.03f, 0.8f, 1f), 0.07f));
            idle.Append(rt.DOScale(1f, 0.1f));
            // look around
            idle.Append(rt.DOLocalRotate(new Vector3(0, 0, 6f), 0.35f).SetEase(Ease.InOutSine));
            idle.Append(rt.DOLocalRotate(new Vector3(0, 0, -6f), 0.6f).SetEase(Ease.InOutSine));
            idle.Append(rt.DOLocalRotate(Vector3.zero, 0.35f).SetEase(Ease.InOutSine));
            if (Mark == Mark.Pet)
            {
                // ear wiggle
                idle.Append(rt.DOLocalRotate(new Vector3(0, 0, 8f), 0.06f));
                idle.Append(rt.DOLocalRotate(new Vector3(0, 0, -8f), 0.1f));
                idle.Append(rt.DOLocalRotate(Vector3.zero, 0.06f));
            }
            else
            {
                // happy squish
                idle.Append(rt.DOScale(new Vector3(1.08f, 0.92f, 1f), 0.12f));
                idle.Append(rt.DOScale(new Vector3(0.95f, 1.06f, 1f), 0.12f));
                idle.Append(rt.DOScale(1f, 0.1f).SetEase(Ease.OutBack));
            }
            idle.AppendInterval(UnityEngine.Random.Range(0.8f, 2f));
        }

        // ------------------------------------------------------------------ Hints / glow

        private void SetGlow(float alpha)
        {
            if (glow == null) return;
            var c = glowBase; c.a = alpha;
            glow.color = c;
            glow.gameObject.SetActive(alpha > 0.001f);
        }

        /// <summary>Soft pulse on an empty square ("this one is free" / "tap here").</summary>
        public void PulseHint(bool strong)
        {
            if (Mark != Mark.None) return;
            StopPulse();
            if (glow != null)
            {
                SetGlow(0.2f);
                glow.DOKill();
                glow.DOFade(strong ? 0.95f : 0.55f, strong ? 0.45f : 0.7f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetId(TweenId + "p");
            }
            transform.DOScale(strong ? 1.07f : 1.03f, strong ? 0.45f : 0.7f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetId(TweenId + "p");
        }

        public void StopPulse()
        {
            DOTween.Kill(TweenId + "p");
            if (glow != null) glow.DOKill();
            transform.localScale = Vector3.one;
            if (Mark == Mark.None) SetGlow(0f);
        }

        // ------------------------------------------------------------------ Winning

        /// <summary>Winning glow + bounce, all three faces together; the cell turns gold.</summary>
        public void WinCelebrate(float delay)
        {
            DOTween.Kill(TweenId);
            DOTween.Kill(TileId);
            StopPulse();
            if (face == null) return;
            var rt = face.rectTransform;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            rt.anchoredPosition = Vector2.zero;

            if (tile != null)
            {
                var from = tile.color;
                tile.DOColor(winTint, 0.4f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetId(TileId);
            }

            var bounce = DOTween.Sequence().SetId(TweenId).SetLoops(-1);
            bounce.AppendInterval(delay);
            bounce.Append(rt.DOAnchorPosY(46f, 0.26f).SetEase(Ease.OutQuad));
            bounce.Join(rt.DOScale(new Vector3(0.95f, 1.1f, 1f), 0.26f));
            bounce.Append(rt.DOAnchorPosY(0f, 0.22f).SetEase(Ease.InQuad));
            bounce.Join(rt.DOScale(new Vector3(1.12f, 0.88f, 1f), 0.22f));
            bounce.Append(rt.DOScale(1f, 0.12f).SetEase(Ease.OutBack));
            bounce.AppendInterval(Mathf.Max(0.05f, 0.35f - delay));
        }

        /// <summary>Draw: every face does a small happy wiggle.</summary>
        public void DrawCelebrate(float delay)
        {
            DOTween.Kill(TweenId);
            if (face == null || Mark == Mark.None) return;
            var rt = face.rectTransform;
            rt.localScale = Vector3.one;
            var s = DOTween.Sequence().SetId(TweenId).SetLoops(-1);
            s.AppendInterval(delay);
            s.Append(rt.DOLocalRotate(new Vector3(0, 0, 9f), 0.12f));
            s.Append(rt.DOLocalRotate(new Vector3(0, 0, -9f), 0.2f));
            s.Append(rt.DOLocalRotate(Vector3.zero, 0.12f));
            s.AppendInterval(0.6f);
        }

        public void Dim(bool on)
        {
            if (face != null && Mark != Mark.None) face.DOFade(on ? 0.45f : 1f, 0.25f);
            if (tile != null && Mark != Mark.None && on) tile.DOFade(0.35f, 0.25f);
        }

        private void OnDestroy()
        {
            DOTween.Kill(TweenId);
            DOTween.Kill(TileId);
            DOTween.Kill(TweenId + "p");
        }
    }
}
