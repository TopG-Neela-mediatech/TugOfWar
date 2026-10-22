using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.tugofwar
{
    /// <summary>
    /// Light effects: soft smoke puffs at the characters' feet (pooled UI images animated with DOTween - no particle
    /// package), the DOTween screen shake and the win-line pulse. Every slot is optional.
    /// </summary>
    public class TugBattleVfx : MonoBehaviour
    {
        [Header("Smoke (soft dust puffs at the feet)")]
        [SerializeField] private Sprite smokeSprite;
        [Tooltip("Canvas object the puffs are spawned under (draws in front of the characters, does not move with the rope).")]
        [SerializeField] private RectTransform smokeRoot;
        [SerializeField] private RectTransform playerFeet;
        [SerializeField] private RectTransform petFeet;
        [SerializeField] private Color smokeColor = new Color(0.93f, 0.88f, 0.78f, 0.85f);
        [SerializeField] private int poolSize = 16;
        [SerializeField] private float puffSize = 110f;
        [SerializeField] private float puffLife = 0.75f;
        [SerializeField] private float smokeInterval = 0.28f;

        [Header("Tween effects")]
        [SerializeField] private Transform playerWinLine;
        [SerializeField] private Transform petWinLine;
        [Tooltip("Canvas object shaken on Power Pull (the whole game root).")]
        [SerializeField] private RectTransform shakeTarget;

        private Image[] pool;
        private int next;
        private float nextSmoke;
        private Vector2 shakeRest;
        private Vector3 playerLineScale = Vector3.one, petLineScale = Vector3.one;

        private void Awake()
        {
            if (shakeTarget != null) shakeRest = shakeTarget.anchoredPosition;
            if (playerWinLine != null) playerLineScale = playerWinLine.localScale;
            if (petWinLine != null) petLineScale = petWinLine.localScale;
            BuildPool();
        }

        private void BuildPool()
        {
            if (smokeRoot == null || smokeSprite == null) return;
            pool = new Image[Mathf.Max(1, poolSize)];
            for (int i = 0; i < pool.Length; i++)
            {
                var go = new GameObject("Smoke", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(smokeRoot, false);
                var img = go.GetComponent<Image>();
                img.sprite = smokeSprite;
                img.raycastTarget = false;
                img.color = new Color(smokeColor.r, smokeColor.g, smokeColor.b, 0f);
                ((RectTransform)go.transform).sizeDelta = new Vector2(puffSize, puffSize);
                go.SetActive(false);
                pool[i] = img;
            }
        }

        // ------------------------------------------------------------------ Smoke

        /// <summary>Soft puffs rise and fade at an anchor (feet).</summary>
        private void Puff(RectTransform feet, int count)
        {
            if (pool == null || feet == null) return;
            Vector2 origin = smokeRoot.InverseTransformPoint(feet.position);
            for (int i = 0; i < count; i++)
            {
                var img = pool[next];
                next = (next + 1) % pool.Length;
                var rt = (RectTransform)img.transform;
                DOTween.Kill(img);
                img.gameObject.SetActive(true);

                float side = Random.Range(-35f, 35f);
                rt.anchoredPosition = origin + new Vector2(side, Random.Range(-6f, 8f));
                rt.localScale = Vector3.one * Random.Range(0.25f, 0.4f);
                rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f));
                img.color = smokeColor;

                float life = puffLife * Random.Range(0.85f, 1.15f);
                float delay = i * 0.06f;
                var seq = DOTween.Sequence().SetTarget(img).SetDelay(delay);
                seq.Join(rt.DOScale(Random.Range(0.85f, 1.2f), life).SetEase(Ease.OutQuad));
                seq.Join(rt.DOAnchorPos(rt.anchoredPosition + new Vector2(side * 1.4f, Random.Range(45f, 85f)), life).SetEase(Ease.OutQuad));
                seq.Join(img.DOFade(0f, life).SetEase(Ease.InQuad));
                seq.OnComplete(() => { if (img != null) img.gameObject.SetActive(false); });
            }
        }

        public void TapFeedback(Vector2 screenPos, TapTier tier)
        {
            if (tier < TapTier.Fast || Time.time < nextSmoke) return;
            nextSmoke = Time.time + smokeInterval;
            Puff(playerFeet, 2);
        }

        public void StrongPullWarning() => Puff(petFeet, 3);
        public void PetTired() => Puff(petFeet, 3);
        public void PowerPullBurst() => Puff(playerFeet, 4);
        public void FinalPull() { Puff(playerFeet, 4); Puff(petFeet, 3); }

        // ------------------------------------------------------------------ Tweens

        /// <param name="strength">Canvas pixels.</param>
        public void ShakeScreen(float strength = 18f, float time = 0.3f)
        {
            if (shakeTarget == null) return;
            shakeTarget.DOKill();
            shakeTarget.anchoredPosition = shakeRest;
            shakeTarget.DOShakeAnchorPos(time, strength, 25, 90f).OnComplete(() => shakeTarget.anchoredPosition = shakeRest);
        }

        private void PulseLine(Transform line, Vector3 baseScale, bool on)
        {
            if (line == null) return;
            line.DOKill();
            line.localScale = baseScale;
            if (on) line.DOScale(baseScale * 1.15f, 0.25f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

        public void NearWinGlow(bool on, bool playerSide = true)
        {
            PulseLine(playerWinLine, playerLineScale, on && playerSide);
            PulseLine(petWinLine, petLineScale, on && !playerSide);
        }

        public void StopAll()
        {
            NearWinGlow(false);
            if (pool == null) return;
            foreach (var img in pool)
            {
                if (img == null) continue;
                DOTween.Kill(img);
                img.gameObject.SetActive(false);
            }
        }
    }
}
