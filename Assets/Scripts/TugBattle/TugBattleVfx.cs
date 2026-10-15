using DG.Tweening;
using UnityEngine;

namespace tmkoc.tugofwar
{
    /// <summary>
    /// Thin wrapper over the scene's particle systems. Every slot is optional - leave empty to skip that effect.
    /// </summary>
    public class TugBattleVfx : MonoBehaviour
    {
        [Header("Camera used to place tap effects (defaults to Camera.main)")]
        [SerializeField] private Camera worldCamera;

        [Header("Tap / pull")]
        [SerializeField] private ParticleSystem tapFeedback;
        [SerializeField] private ParticleSystem footDust;
        [SerializeField] private ParticleSystem ropeStretch;
        [SerializeField] private ParticleSystem pullTrail;
        [SerializeField] private ParticleSystem goodPop;
        [SerializeField] private ParticleSystem greatPop;
        [SerializeField] private ParticleSystem superPullEffect;

        [Header("Pet")]
        [SerializeField] private ParticleSystem strongPullWarning;
        [SerializeField] private ParticleSystem petTired;

        [Header("Power")]
        [SerializeField] private ParticleSystem powerMeterFull;
        [SerializeField] private ParticleSystem powerPullBurst;

        [Header("Near win / finish")]
        [SerializeField] private ParticleSystem nearWinGlow;
        [SerializeField] private ParticleSystem finalPull;
        [SerializeField] private ParticleSystem victoryStars;
        [SerializeField] private ParticleSystem confetti;
        [SerializeField] private ParticleSystem finalCelebration;

        [Header("Tween effects")]
        [SerializeField] private Transform playerWinLine;
        [SerializeField] private Transform petWinLine;

        private Camera Cam => worldCamera != null ? worldCamera : Camera.main;
        private Vector3 camStart;
        private bool hasCamStart;
        private Vector3 playerLineScale = Vector3.one, petLineScale = Vector3.one;

        private void Awake()
        {
            if (Cam != null) { camStart = Cam.transform.localPosition; hasCamStart = true; }
            if (playerWinLine != null) playerLineScale = playerWinLine.localScale;
            if (petWinLine != null) petLineScale = petWinLine.localScale;
        }

        public void ShakeCamera(float strength = 0.3f, float time = 0.3f)
        {
            if (!hasCamStart || Cam == null) return;
            var t = Cam.transform;
            t.DOKill();
            t.localPosition = camStart;
            t.DOShakePosition(time, strength, 25, 90f).OnComplete(() => t.localPosition = camStart);
        }

        private void PulseLine(Transform line, Vector3 baseScale, bool on)
        {
            if (line == null) return;
            line.DOKill();
            line.localScale = baseScale;
            if (on) line.DOScale(baseScale * 1.15f, 0.25f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
        }

        public void TapFeedback(Vector2 screenPos, TapTier tier)
        {
            if (tapFeedback != null)
            {
                MoveToScreen(tapFeedback, screenPos);
                tapFeedback.Emit(4 + (int)tier * 3);
            }
            if (tier >= TapTier.Fast) PlayOnce(footDust);
            if (tier >= TapTier.Fast) PlayOnce(pullTrail);
        }

        public void RopeStretch() => PlayOnce(ropeStretch);
        public void Pop(PopKind kind)
        {
            switch (kind)
            {
                case PopKind.Good: PlayOnce(goodPop); break;
                case PopKind.Great: PlayOnce(greatPop); break;
                case PopKind.SuperPull: PlayOnce(superPullEffect); break;
            }
        }

        public void StrongPullWarning() => PlayOnce(strongPullWarning);
        public void PetTired() => PlayOnce(petTired);
        public void PowerMeterFull() => PlayOnce(powerMeterFull);
        public void PowerPullBurst() => PlayOnce(powerPullBurst);
        public void FinalPull() => PlayOnce(finalPull);
        public void VictoryStars() => PlayOnce(victoryStars);
        public void Confetti() => PlayOnce(confetti);
        public void FinalCelebration() => PlayOnce(finalCelebration);

        public void NearWinGlow(bool on, bool playerSide = true)
        {
            SetLoop(nearWinGlow, on);
            PulseLine(playerWinLine, playerLineScale, on && playerSide);
            PulseLine(petWinLine, petLineScale, on && !playerSide);
        }

        public void StopAll()
        {
            NearWinGlow(false);
            ParticleSystem[] all =
            {
                tapFeedback, footDust, ropeStretch, pullTrail, goodPop, greatPop, superPullEffect, strongPullWarning,
                petTired, powerMeterFull, powerPullBurst, nearWinGlow, finalPull, victoryStars, confetti, finalCelebration
            };
            for (int i = 0; i < all.Length; i++)
                if (all[i] != null) all[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private static void PlayOnce(ParticleSystem ps)
        {
            if (ps == null) return;
            if (!ps.gameObject.activeInHierarchy) ps.gameObject.SetActive(true);
            ps.Play(true);
        }

        private static void SetLoop(ParticleSystem ps, bool on)
        {
            if (ps == null) return;
            if (on)
            {
                if (!ps.gameObject.activeInHierarchy) ps.gameObject.SetActive(true);
                if (!ps.isPlaying) ps.Play(true);
            }
            else if (ps.isPlaying)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        private void MoveToScreen(ParticleSystem ps, Vector2 screenPos)
        {
            var cam = Cam;
            if (cam == null) return;
            float depth = Mathf.Abs(cam.transform.position.z - ps.transform.position.z);
            Vector3 w = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
            w.z = ps.transform.position.z;
            ps.transform.position = w;
        }
    }
}
