using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.threeinarow
{
    /// <summary>
    /// One square of the 3x3 board. Square image + Face image (the marker) + Glow image (hint / winning glow).
    /// Everything that "animates" is a DOTween: pop, hop, idle breathing, blink, hint pulse, winning glow + bounce.
    /// </summary>
    public class CellView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image square;
        [SerializeField] private Image face;
        [SerializeField] private Image glow;

        public int Index { get; private set; }
        public Mark Mark { get; private set; }
        public RectTransform Rect => (RectTransform)transform;

        private string TweenId => "cell" + GetInstanceID();
        private Action<int> onClick;
        private Sprite tapuSprite, petSprite, placeholder;
        private Color glowBase = new Color(1f, 0.95f, 0.45f, 1f);

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
            Clear();
        }

        public void ApplyArt(Sprite emptySquare, Sprite tapuFace, Sprite petFace)
        {
            tapuSprite = tapuFace;
            petSprite = petFace;
            if (emptySquare != null && square != null)
            {
                square.sprite = emptySquare;
                square.type = Image.Type.Simple;
                square.preserveAspect = true;
                square.color = Color.white;
            }
        }

        public void SetInteractable(bool on)
        {
            if (button != null) button.interactable = on && Mark == Mark.None;
        }

        public bool IsEmpty => Mark == Mark.None;

        // ------------------------------------------------------------------ Reset

        public void Clear()
        {
            DOTween.Kill(TweenId);
            Mark = Mark.None;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            if (face != null)
            {
                face.gameObject.SetActive(false);
                face.rectTransform.localScale = Vector3.one;
                face.rectTransform.localRotation = Quaternion.identity;
                face.rectTransform.anchoredPosition = Vector2.zero;
                face.color = Color.white;
            }
            SetGlow(0f);
            if (button != null) button.interactable = true;
        }

        // ------------------------------------------------------------------ Placing a face

        /// <summary>Tapu: scales from small with a bounce + smile wobble. Pet: a playful hop with an ear-wiggle tilt.</summary>
        public void Place(Mark mark)
        {
            DOTween.Kill(TweenId);
            Mark = mark;
            if (button != null) button.interactable = false;
            SetGlow(0f);

            var rt = face.rectTransform;
            Sprite art = mark == Mark.Tapu ? tapuSprite : petSprite;
            face.sprite = art != null ? art : placeholder;   // no art assigned: the placeholder circle
            face.color = art != null ? Color.white : (mark == Mark.Tapu ? new Color(1f, 0.82f, 0.25f) : new Color(0.95f, 0.5f, 0.5f));
            face.gameObject.SetActive(true);
            rt.anchoredPosition = Vector2.zero;

            if (mark == Mark.Tapu)
            {
                rt.localScale = Vector3.one * 0.1f;
                rt.localRotation = Quaternion.Euler(0, 0, -12f);
                var s = DOTween.Sequence().SetId(TweenId);
                s.Append(rt.DOScale(1.22f, 0.22f).SetEase(Ease.OutQuad));
                s.Join(rt.DOLocalRotate(new Vector3(0, 0, 8f), 0.22f));
                s.Append(rt.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
                s.Join(rt.DOLocalRotate(Vector3.zero, 0.2f));
                s.AppendCallback(StartIdle);
            }
            else
            {
                rt.localScale = new Vector3(0.6f, 0.6f, 1f);
                rt.localRotation = Quaternion.identity;
                float hop = 40f;
                var s = DOTween.Sequence().SetId(TweenId);
                s.Append(rt.DOAnchorPosY(hop, 0.2f).SetEase(Ease.OutQuad));
                s.Join(rt.DOScale(new Vector3(0.95f, 1.1f, 1f), 0.2f));
                s.Append(rt.DOAnchorPosY(0f, 0.16f).SetEase(Ease.InQuad));
                s.Join(rt.DOScale(new Vector3(1.12f, 0.9f, 1f), 0.16f));
                s.Append(rt.DOScale(1f, 0.14f).SetEase(Ease.OutBack));
                // ear wiggle
                s.Append(rt.DOLocalRotate(new Vector3(0, 0, 7f), 0.07f));
                s.Append(rt.DOLocalRotate(new Vector3(0, 0, -7f), 0.12f));
                s.Append(rt.DOLocalRotate(Vector3.zero, 0.07f));
                s.AppendCallback(StartIdle);
            }
        }

        // Idle: a very gentle breathing; the pet also blinks (a quick squash) every few seconds.
        private void StartIdle()
        {
            if (face == null || Mark == Mark.None) return;
            var rt = face.rectTransform;
            var idle = DOTween.Sequence().SetId(TweenId).SetLoops(-1);
            idle.AppendInterval(UnityEngine.Random.Range(0.1f, 1.2f));
            idle.Append(rt.DOScale(1.04f, 1.1f).SetEase(Ease.InOutSine));
            idle.Append(rt.DOScale(1f, 1.1f).SetEase(Ease.InOutSine));
            if (Mark == Mark.Pet)
            {
                idle.AppendInterval(UnityEngine.Random.Range(1.5f, 3f));
                idle.Append(rt.DOScale(new Vector3(1.04f, 0.88f, 1f), 0.07f));
                idle.Append(rt.DOScale(1f, 0.09f));
            }
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

        /// <summary>Winning glow + bounce, all three faces together.</summary>
        public void WinCelebrate(float delay)
        {
            DOTween.Kill(TweenId);
            StopPulse();
            if (face == null) return;
            var rt = face.rectTransform;
            rt.localScale = Vector3.one;
            rt.anchoredPosition = Vector2.zero;

            SetGlow(0.35f);
            glow.DOKill();
            glow.DOFade(1f, 0.4f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetId(TweenId);

            var bounce = DOTween.Sequence().SetId(TweenId).SetLoops(-1);
            bounce.AppendInterval(delay);
            bounce.Append(rt.DOAnchorPosY(46f, 0.26f).SetEase(Ease.OutQuad));
            bounce.Join(rt.DOScale(new Vector3(0.95f, 1.1f, 1f), 0.26f));
            bounce.Append(rt.DOAnchorPosY(0f, 0.22f).SetEase(Ease.InQuad));
            bounce.Join(rt.DOScale(new Vector3(1.12f, 0.88f, 1f), 0.22f));
            bounce.Append(rt.DOScale(1f, 0.12f).SetEase(Ease.OutBack));
            bounce.AppendInterval(0.35f - delay > 0 ? 0.35f - delay : 0.05f);
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
        }

        private void OnDestroy()
        {
            DOTween.Kill(TweenId);
            DOTween.Kill(TweenId + "p");
        }
    }
}
