using System.Collections.Generic;
using UnityEngine;

namespace tmkoc.tugofwar
{
    /// <summary>
    /// Core Tug Battle match logic (tap power, rope physics, pet bot, win/lose).
    /// LevelManager owns level progression/saving: it calls BeginLevel(), and this class reports back
    /// through GameManager.InvokeLevelWin / InvokeLevelLose.
    ///
    /// Rope value: 0 = centre, -100 = Player Win Line, +100 = Pet Win Line.
    /// </summary>
    public class TugBattleController : MonoBehaviour
    {
        [Header("Scene references")]
        [SerializeField] private RopeView ropeView;
        [SerializeField] private CharacterView player;
        [SerializeField] private CharacterView pet;
        [SerializeField] private TugBattleUI ui;
        [SerializeField] private TugBattleVfx vfx;
        [Tooltip("Shows the winner image + hides rope, characters and lines at the end of a round. Optional: without it the old celebrate / slump on the characters is used.")]
        [SerializeField] private WinPresenter winPresenter;

        [Header("Tap power")]
        [Tooltip("Rope % pulled by one tap at Normal speed.")]
        [SerializeField] private float tapBase = 2.6f;
        [Tooltip("Multiplier per tier: Weak, Normal, Fast, Power.")]
        [SerializeField] private float[] tierMultiplier = { 0.75f, 1f, 1.3f, 1.6f };
        [SerializeField] private int[] tierTapsPerSecond = { 3, 6, 9 };

        [Header("Power meter")]
        [SerializeField] private float powerPush = 28f;
        [SerializeField] private float powerTime = 0.7f;
        [SerializeField] private float powerDecayPerSecond = 14f;
        [SerializeField] private float powerDecayDelay = 0.6f;

        [Header("Rope")]
        [SerializeField] private float nearWinRope = 62f;
        [Tooltip("Level 1 intro: the Pet does not pull until the flag is this far (rope %) toward the Player.")]
        [SerializeField] private float introGoalRope = 25f;
        [Tooltip("Level 1 intro: show the tap hint again if the player stops tapping for this long.")]
        [SerializeField] private float introIdleHint = 3f;
        [SerializeField] private float finalPushRope = 70f;
        [SerializeField] private float rubberBand = 7f;
        [SerializeField] private float countdownStep = 0.75f;
        [SerializeField] private float endingDuration = 0.95f;

        public MatchState State { get; private set; } = MatchState.Idle;
        public float Rope => rope;

        private readonly PetBot petBot = new PetBot();
        private readonly Queue<float> taps = new Queue<float>();

        private TugLevelConfig cfg;
        private bool isFinalLevel;
        private float rope, target, gameTime, lastTap;
        private float power, powerT;
        private TapTier lastTier;
        private bool playerWon;

        // countdown
        private int cdStep;
        private float cdTimer, cdSpeed;

        // cooldowns / one-shot flags
        private float effortAt, popAt, voiceAt, alertAt, tensionAt, firstTapPromptUntil;
        private bool tappedOnce, meterFullPlayed, finalPush, dangerZone, safeZone;
        private float endTimer;
        private bool introActive;
        private int currentLevel;

        private void Awake()
        {
            petBot.StateChanged += OnPetStateChanged;
        }

        private void OnDestroy()
        {
            petBot.StateChanged -= OnPetStateChanged;
        }

        // ------------------------------------------------------------------ Flow

        /// <param name="fastCountdown">True on retry so the player gets back in quicker.</param>
        public void BeginLevel(int levelIndex, TugLevelConfig config, bool isFinal, bool fastCountdown = false)
        {
            cfg = config;
            isFinalLevel = isFinal;
            introActive = levelIndex == 0;   // mandatory tutorial: player pulls ahead before the Pet starts
            currentLevel = levelIndex;
            if (winPresenter != null) winPresenter.ResetState();   // win images off, rope / characters / lines back on

            rope = target = 0f;
            gameTime = 0f;
            lastTap = -9f;
            power = powerT = 0f;
            lastTier = TapTier.Weak;
            taps.Clear();
            effortAt = popAt = voiceAt = alertAt = tensionAt = -9f;
            tappedOnce = meterFullPlayed = finalPush = dangerZone = safeZone = false;
            endTimer = 0f;
            playerWon = false;
            petBot.Begin(cfg);
            Tut(t => t.BeginLevel(levelIndex));

            vfx?.StopAll();
            player.ResetTween();
            pet.ResetTween();
            ui.ResetUI();
            ApplyVisuals();
            player.SetPose(Pose.Ready, true);
            pet.SetPose(Pose.Ready, true);
            ropeView.SetState(RopeState.Neutral);

            cdStep = 3;
            cdTimer = 0f;
            cdSpeed = fastCountdown ? 1.8f : 1f;
            State = MatchState.Countdown;
            ui.ShowCountdown("3");
            Sound(s => { s.StopVoice(); s.PlayMusic(MusicId.MatchStart); s.PlaySfx(SfxId.Countdown); s.PlayVoice(VoiceId.LevelIntro, VoicePriority.High); });
        }

        // ------------------------------------------------------------------ Update

        private void Update()
        {
            float dt = Time.deltaTime;
            switch (State)
            {
                case MatchState.Countdown: UpdateCountdown(dt); break;
                case MatchState.Playing: UpdatePlay(dt); break;
                case MatchState.Ending: UpdateEnding(dt); break;
            }
        }

        private void UpdateCountdown(float dt)
        {
            rope += (0f - rope) * Mathf.Min(1f, dt * 4f);
            ApplyVisuals();

            cdTimer += dt * cdSpeed;
            if (cdTimer < countdownStep) return;
            cdTimer = 0f;
            cdStep--;

            if (cdStep > 0)
            {
                ui.ShowCountdown(cdStep.ToString());
                Sound(s => s.PlaySfx(SfxId.Countdown));
            }
            else if (cdStep == 0)
            {
                ui.ShowCountdown("GO!");
                Sound(s => s.PlaySfx(SfxId.Go));
            }
            else
            {
                ui.HideCountdown();
                State = MatchState.Playing;
                Tut(t => t.OnPlayStart());
                firstTapPromptUntil = gameTime + 3f;
                ui.ShowBanner(BannerKind.TapFast);          // Phase 1: teach "Tap Fast!"
                Sound(s => { s.PlayMusic(MusicId.Gameplay); s.PlayVoice(introActive ? VoiceId.Tutorial : VoiceId.Go); });
            }
        }

        private void UpdatePlay(float dt)
        {
            gameTime += dt;
            if (introActive)
            {
                // Pet waits (and the hint hand keeps showing) until the player has won a little ground
                if (rope <= -introGoalRope) introActive = false;
                else if (gameTime - lastTap > introIdleHint) Tut(t => t.Show());
            }
            else
            {
                petBot.Tick(dt);
            }

            // Pet pull is suspended during the intro and during a Power Pull
            float petForce = (introActive || powerT > 0f) ? 0f : petBot.Pull;
            target += petForce * dt;

            if (powerT > 0f)
            {
                target -= powerPush / powerTime * dt;
                powerT -= dt;
            }
            if (gameTime - lastTap > powerDecayDelay)
                power = Mathf.Max(0f, power - powerDecayPerSecond * dt);

            target = Mathf.Clamp(target, -108f, 108f);
            rope += (target - rope) * (1f - Mathf.Exp(-dt * rubberBand));
            Tut(t => t.Tick(rope));

            UpdateFeedback(dt);
            ApplyVisuals();

            if (rope <= -100f) EndRound(true);
            else if (rope >= 100f) EndRound(false);
        }

        private void UpdateFeedback(float dt)
        {
            // Pet strong-pull ambience
            if (petBot.State == PetState.Strong && gameTime >= tensionAt)
            {
                tensionAt = gameTime + 0.5f;
                Sound(s => s.PlaySfx(SfxId.RopeTension));
            }

            // Near-win alert + final push music (hysteresis so it doesn't flicker)
            float abs = Mathf.Abs(rope);
            if (abs > finalPushRope && gameTime >= alertAt)
            {
                alertAt = gameTime + 0.55f;
                Sound(s => s.PlaySfx(SfxId.NearWinAlert));
            }
            if (!finalPush && abs > finalPushRope)
            {
                finalPush = true;
                Sound(s => s.PlayMusic(MusicId.FinalPush));
                vfx?.NearWinGlow(true, rope < 0f);
            }
            else if (finalPush && abs < finalPushRope - 18f)
            {
                finalPush = false;
                Sound(s => s.PlayMusic(MusicId.Gameplay));
                vfx?.NearWinGlow(false);
            }

            // "Keep Tapping!" (winning) / "Tap Fast!" (losing) prompts
            if (rope < -nearWinRope && !safeZone)
            {
                safeZone = true; dangerZone = false;
                ui.ShowBanner(BannerKind.KeepTapping);
                Sound(s => s.PlayVoice(VoiceId.KeepTapping, VoicePriority.Low));
            }
            else if (rope > nearWinRope && !dangerZone)
            {
                dangerZone = true; safeZone = false;
                ui.ShowBanner(BannerKind.TapFast);
                Sound(s => s.PlayVoice(VoiceId.Encourage));
            }
            else if (Mathf.Abs(rope) < nearWinRope - 12f && (safeZone || dangerZone))
            {
                safeZone = dangerZone = false;
                ui.HideBanner();
            }

            // Phase 1 prompt fades once the player starts tapping
            if (tappedOnce && gameTime < firstTapPromptUntil && !safeZone && !dangerZone) ui.HideBanner();
        }

        private void UpdateEnding(float dt)
        {
            endTimer += dt;
            target = playerWon ? -128f : 125f;
            rope += (target - rope) * (1f - Mathf.Exp(-dt * (playerWon ? 5f : 3f)));
            ApplyVisuals();
            if (endTimer >= endingDuration) FinishRound();
        }

        // ------------------------------------------------------------------ Input

        public void OnTap(Vector2 screenPos)
        {
            if (State != MatchState.Playing) return;

            tappedOnce = true;
            Tut(t => t.NotifyTap());
            taps.Enqueue(gameTime);
            lastTap = gameTime;

            TapTier tier = GetTier();
            target -= tapBase * tierMultiplier[(int)tier];
            power = Mathf.Min(100f, power + 2.6f + (int)tier * 1.7f);

            player.Punch();
            vfx?.TapFeedback(screenPos, tier);
            Sound(s => s.PlayTap(tier));

            if (tier >= TapTier.Fast && gameTime - effortAt > 0.45f)
            {
                effortAt = gameTime;
                Sound(s => s.PlaySfx(SfxId.PlayerEffort));
                ropeView.FlagShake(0.6f);
            }

            if (tier > lastTier && tier >= TapTier.Fast) Sound(s => s.PlaySfx(SfxId.Combo));
            lastTier = tier;

            if (tier >= TapTier.Normal && gameTime - popAt > 0.42f)
            {
                popAt = gameTime;
                var pop = (PopKind)((int)tier - 1);   // Normal->Good, Fast->Great, Power->Super
                ui.SpawnPop(pop);
                if (gameTime - voiceAt > 2.5f)
                {
                    voiceAt = gameTime;
                    var v = pop == PopKind.Good ? VoiceId.Good : pop == PopKind.Great ? VoiceId.Great : VoiceId.SuperPull;
                    Sound(s => s.PlayVoice(v, VoicePriority.Low));
                }
            }

            UpdatePowerFeedback();
            if (power >= 100f) PowerPull();
        }

        private TapTier GetTier()
        {
            while (taps.Count > 0 && gameTime - taps.Peek() > 1f) taps.Dequeue();
            int rate = taps.Count;
            if (rate < tierTapsPerSecond[0]) return TapTier.Weak;
            if (rate < tierTapsPerSecond[1]) return TapTier.Normal;
            if (rate < tierTapsPerSecond[2]) return TapTier.Fast;
            return TapTier.Power;
        }

        private void UpdatePowerFeedback()
        {
            ui.SetPower(power / 100f);
            if (power >= 75f && !meterFullPlayed)
            {
                meterFullPlayed = true;
                Sound(s => s.PlaySfx(SfxId.MeterFull));
            }
            else if (power < 50f) meterFullPlayed = false;
            else if (!meterFullPlayed && Mathf.FloorToInt(power) % 12 == 0)
                Sound(s => s.PlaySfx(SfxId.MeterFill, 1f + power / 200f, 0.6f));
        }

        private void PowerPull()
        {
            power = 0f;
            powerT = powerTime;
            meterFullPlayed = false;
            ui.SetPower(0f);
            ui.ShowBanner(BannerKind.PowerPull, 1.1f);
            vfx?.PowerPullBurst();
            vfx?.ShakeScreen(18f);
            ui.PunchPowerMeter();
            ropeView.FlagShake(1.2f);
            Sound(s => { s.PlaySfx(SfxId.PowerWhoosh); s.PlayVoice(VoiceId.PowerPull); });
        }

        // ------------------------------------------------------------------ Pet events

        private void OnPetStateChanged(PetState s)
        {
            if (State != MatchState.Playing) return;
            switch (s)
            {
                case PetState.Warn:
                    ui.ShowBanner(BannerKind.StrongPull, cfg.warnTime + cfg.strongDuration);
                    vfx?.StrongPullWarning();
                    Sound(x => { x.PlaySfx(SfxId.StrongAlert); x.PlayVoice(VoiceId.StrongPull); });
                    break;
                case PetState.Strong:
                    Sound(x => x.PlaySfx(SfxId.PetEffort));
                    break;
                case PetState.Tired:
                    ui.ShowBanner(BannerKind.PetTired, Mathf.Min(cfg.tiredDuration, 2f));
                    vfx?.PetTired();
                    Sound(x => { x.PlaySfx(SfxId.PetTired); x.PlayVoice(VoiceId.PetTired); });
                    break;
            }
        }

        // ------------------------------------------------------------------ End of round

        private void EndRound(bool won)
        {
            playerWon = won;
            endTimer = 0f;
            State = MatchState.Ending;
            Tut(t => t.Hide());
            ui.HideBanner();
            vfx?.NearWinGlow(false);

            if (won)
            {
                player.SetPose(Pose.FinalPull);
                pet.SetPose(Pose.LosingGround);
                pet.SlideForward();
                vfx?.FinalPull();
                ropeView.FlagShake(1.5f);
                Sound(s => { s.PlaySfx(SfxId.FinalPull); s.PlaySfx(SfxId.FlagMove, 1f, 1f, 0.1f); });
            }
            else
            {
                player.SetPose(Pose.LosingGround);
                pet.SetPose(Pose.WinningGround);
                Sound(s => s.PlaySfx(SfxId.FlagMove));
            }
        }

        private void FinishRound()
        {
            State = MatchState.Result;
            if (playerWon)
            {
                if (winPresenter != null) winPresenter.Show(true);
                else { player.SetPose(Pose.Celebrate); player.Celebrate(); pet.SetPose(Pose.Lose); }
                Sound(s =>
                {
                    s.PlayMusic(isFinalLevel ? MusicId.FinalCelebration : MusicId.Victory, 0.15f);
                    s.PlaySfx(SfxId.VictoryChime);
                    s.PlaySfx(SfxId.Stars, 1f, 1f, 0.5f);
                    s.PlaySfx(SfxId.Confetti, 1f, 1f, 0.2f);
                    s.PlayVoice(isFinalLevel ? VoiceId.FinalWin : VoiceId.Win, VoicePriority.High, 0.35f, currentLevel);
                });
                GameManager.Instance.InvokeLevelWin();
            }
            else
            {
                if (winPresenter != null) winPresenter.Show(false);
                else { player.SetPose(Pose.Lose); player.Slump(); pet.SetPose(Pose.Celebrate); pet.Celebrate(); }
                Sound(s =>
                {
                    s.PlayMusic(MusicId.Lose, 0.15f);
                    s.PlaySfx(SfxId.FunnyLose);
                    s.PlayVoice(VoiceId.Lose, VoicePriority.High, 0.5f, currentLevel);
                });
                GameManager.Instance.InvokeLevelLose();
            }
        }

        // ------------------------------------------------------------------ Visuals

        private void ApplyVisuals()
        {
            ropeView.SetRope(rope);
            ui.SetPower(powerT > 0f ? powerT / powerTime : power / 100f);
            if (State != MatchState.Playing) return;

            player.SetPose(PlayerPose());
            pet.SetPose(PetPose());

            RopeState rs;
            if (powerT > 0f) rs = RopeState.PowerPull;
            else if (petBot.State == PetState.Strong) rs = RopeState.StrongPull;
            else if (lastTier >= TapTier.Fast && gameTime - lastTap < 0.5f) rs = RopeState.Stretch;
            else if (rope < -8f) rs = RopeState.PlayerPull;
            else if (rope > 8f) rs = RopeState.PetPull;
            else rs = RopeState.Neutral;
            ropeView.SetState(rs);
            ropeView.SetStrain(petBot.State == PetState.Strong ? 1f : (gameTime - lastTap < 0.5f ? (int)lastTier / 3f : 0f));
        }

        private Pose PlayerPose()
        {
            if (powerT > 0f) return Pose.PowerPull;
            if (rope < -nearWinRope) return Pose.NearWin;
            float idle = gameTime - lastTap;
            if (idle > 2.5f && tappedOnce) return Pose.Tired;
            if (idle > powerDecayDelay) return Pose.RopeHold;
            switch (lastTier)
            {
                case TapTier.Fast: return Pose.StrongPull;
                case TapTier.Power: return Pose.VeryStrongPull;
                default: return Pose.NormalPull;
            }
        }

        private Pose PetPose()
        {
            if (introActive) return Pose.Ready;
            switch (petBot.State)
            {
                case PetState.Warn: return Pose.Ready;
                case PetState.Strong: return Pose.StrongPull;
                case PetState.Tired: return Pose.Tired;
                case PetState.Recover: return Pose.Recover;
            }
            if (rope > nearWinRope) return Pose.NearWin;
            if (rope > 40f) return Pose.WinningGround;
            if (rope < -40f) return Pose.LosingGround;
            return Pose.NormalPull;
        }

        private static void Tut(System.Action<TutorialManager> call)
        {
            var tm = GameManager.Instance.TutorialManager;
            if (tm != null) call(tm);
        }

        private static void Sound(System.Action<SoundManager> call)
        {
            var sm = GameManager.Instance.SoundManager;
            if (sm != null) call(sm);
            else Debug.LogWarning("TugBattleController: GameManager.SoundManager is not assigned.");
        }
    }
}
