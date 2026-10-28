using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tmkoc.threeinarow
{
    /// <summary>
    /// Three in a Row game flow: Observe board -> player taps a square -> face pops -> pet thinks and answers -> repeat
    /// until three matching faces make a line (win / lose) or the board is full (draw).
    /// LevelManager starts it through BeginLevel(); the result goes back through GameManager.InvokeLevelWin/Lose/Draw.
    /// </summary>
    public class ThreeInARowController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ThreeInARowUI ui;
        [Tooltip("The 9 squares, row by row, top-left first.")]
        [SerializeField] private CellView[] cells = new CellView[9];
        [SerializeField] private WinLineView winLine;
        [SerializeField] private SideCharacter tapu;
        [SerializeField] private SideCharacter pet;

        [Header("Timing")]
        [SerializeField] private float introTime = 2.4f;
        [SerializeField] private float resultCelebrateDelay = 0.35f;
        [SerializeField] private Color tapuLineColor = new Color(1f, 0.82f, 0.2f);
        [SerializeField] private Color petLineColor = new Color(1f, 0.45f, 0.5f);

        public MatchState State { get; private set; } = MatchState.Idle;

        private readonly Mark[] board = new Mark[9];
        private ThreeLevelConfig cfg;
        private int currentLevel;
        private bool isFinalLevel;
        private bool firstTurn;
        private float idleTimer;
        private bool hinting;
        private int turnsPlayed;
        private Coroutine flow;

        private void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnLevelReset += ResetState;
            for (int i = 0; i < cells.Length; i++) if (cells[i] != null) cells[i].Init(i, OnCellClicked);
            ResetState();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnLevelReset -= ResetState;
        }

        // ------------------------------------------------------------------ Helpers

        private void Sound(Action<SoundManager> a)
        {
            var s = GameManager.Instance != null ? GameManager.Instance.SoundManager : null;
            if (s != null) a(s);
        }

        private void Tut(Action<TutorialManager> a)
        {
            var t = GameManager.Instance != null ? GameManager.Instance.TutorialManager : null;
            if (t != null) a(t);
        }

        // ------------------------------------------------------------------ Level lifecycle

        /// <summary>Back to a clean board: no faces, no line, no confetti, characters idle.</summary>
        private void ResetState()
        {
            if (flow != null) { StopCoroutine(flow); flow = null; }
            for (int i = 0; i < board.Length; i++) board[i] = Mark.None;
            foreach (var c in cells) if (c != null) c.Clear();
            if (winLine != null) winLine.Hide();
            if (ui != null) { ui.ClearConfetti(); ui.HideBanner(false); ui.SetTurn(Mark.None); }
            if (tapu != null) tapu.Idle();
            if (pet != null) pet.Idle();
            Tut(t => t.Hide());
            hinting = false;
            State = MatchState.Idle;
        }

        public void BeginLevel(int levelIndex, ThreeLevelConfig config, bool finalLevel, bool retry)
        {
            ResetState();
            currentLevel = levelIndex;
            cfg = config;
            isFinalLevel = finalLevel;
            turnsPlayed = 0;
            firstTurn = true;
            State = MatchState.Intro;

            Tut(t => t.BeginLevel(levelIndex));
            SetCellsInteractable(false);
            ui.BoardAppear();
            ui.ShowBanner("Let's play!", introTime - 0.4f);
            Sound(s => { s.StopVoice(); s.PlayVoice(VoiceId.LevelIntro, VoicePriority.High); });
            flow = StartCoroutine(IntroRoutine());
        }

        private IEnumerator IntroRoutine()
        {
            yield return new WaitForSeconds(introTime);
            flow = null;
            StartPlayerTurn();
        }

        // ------------------------------------------------------------------ Player turn

        private void StartPlayerTurn()
        {
            State = MatchState.PlayerTurn;
            idleTimer = 0f;
            hinting = false;
            SetCellsInteractable(true);
            ui.SetTurn(Mark.Tapu);
            tapu.Nod();
            pet.Idle();

            bool tutorialTurn = firstTurn && currentLevel == 0;
            if (tutorialTurn)
            {
                Sound(s => s.PlayVoice(VoiceId.Tutorial));
                Tut(t => { if (t.ShowsOnFirstTurn) t.ShowAt(cells[4].Rect); });
            }
            else
            {
                ui.ShowBanner("Your turn!", 1.2f);
                if (turnsPlayed % 2 == 0) Sound(s => s.PlayVoice(VoiceId.YourTurn, VoicePriority.Low));
            }
            firstTurn = false;
        }

        private void OnCellClicked(int index)
        {
            if (State != MatchState.PlayerTurn || board[index] != Mark.None) return;

            ClearHints();
            Tut(t => t.Hide());
            ui.HideBanner();
            Place(index, Mark.Tapu);
            Sound(s => s.PlaySfx(SfxId.TapuPop));
            if (UnityEngine.Random.value < 0.45f) Sound(s => s.PlayVoice(VoiceId.Placed, VoicePriority.Low));
            turnsPlayed++;

            if (!Evaluate(Mark.Tapu)) flow = StartCoroutine(PetTurnRoutine());
        }

        private void Update()
        {
            if (State != MatchState.PlayerTurn || cfg == null) return;
            idleTimer += Time.deltaTime;
            if (!hinting && idleTimer >= cfg.hintDelay) ShowHint();
        }

        // Idle player: pulse the free squares; on the easy levels light the square that wins (or blocks) and show the hand.
        private void ShowHint()
        {
            hinting = true;
            int target = -1;
            if (cfg.hintWinningSquare)
            {
                target = BotAI.CompletingSquare(board, Mark.Tapu);
                if (target < 0) target = BotAI.CompletingSquare(board, Mark.Pet);
            }

            foreach (var c in cells) if (c != null && c.IsEmpty) c.PulseHint(false);
            if (target >= 0)
            {
                cells[target].PulseHint(true);
                Tut(t => t.ShowAt(cells[target].Rect));
            }
            else
            {
                int any = BotAI.RandomEmpty(board);
                if (any >= 0) Tut(t => t.ShowAt(cells[any].Rect));
            }
            Sound(s => s.PlayVoice(VoiceId.Hint));
        }

        private void ClearHints()
        {
            hinting = false;
            idleTimer = 0f;
            foreach (var c in cells) if (c != null) c.StopPulse();
        }

        // ------------------------------------------------------------------ Pet turn

        private IEnumerator PetTurnRoutine()
        {
            State = MatchState.PetThinking;
            SetCellsInteractable(false);
            ui.SetTurn(Mark.Pet);
            ui.ShowBanner("Pet is thinking...", 0f);
            tapu.Idle();
            pet.Think();
            Sound(s => s.PlayVoice(VoiceId.PetThinking, VoicePriority.Low));

            yield return new WaitForSeconds(UnityEngine.Random.Range(cfg.thinkTime.x, cfg.thinkTime.y));

            int move = BotAI.ChoosePetMove(board, cfg);
            ui.HideBanner();
            pet.Idle();
            if (move >= 0)
            {
                Place(move, Mark.Pet);
                Sound(s => s.PlaySfx(SfxId.PetPop));
            }
            flow = null;

            if (Evaluate(Mark.Pet)) yield break;

            // what the board looks like now
            if (BotAI.CompletingSquare(board, Mark.Pet) >= 0)
            {
                ui.ShowBanner("Stop the pet!", 1.6f);
                Sound(s => s.PlayVoice(VoiceId.StopPet));
            }
            else if (BotAI.CompletingSquare(board, Mark.Tapu) >= 0)
            {
                ui.ShowBanner("Two in a row!", 1.6f);
                tapu.Excited();
                Sound(s => s.PlayVoice(VoiceId.TwoInARow));
            }
            StartPlayerTurn();
        }

        private void Place(int index, Mark mark)
        {
            board[index] = mark;
            cells[index].Place(mark);
        }

        private void SetCellsInteractable(bool on)
        {
            foreach (var c in cells) if (c != null) c.SetInteractable(on);
        }

        // ------------------------------------------------------------------ Result

        /// <returns>True if the round ended.</returns>
        private bool Evaluate(Mark who)
        {
            var line = BotAI.FindWin(board, who);
            if (line != null) { Finish(who == Mark.Tapu ? RoundResult.Tapu : RoundResult.Pet, line); return true; }
            if (BotAI.IsFull(board)) { Finish(RoundResult.Draw, null); return true; }
            return false;
        }

        private void Finish(RoundResult result, int[] line)
        {
            State = MatchState.Result;
            SetCellsInteractable(false);
            ClearHints();
            Tut(t => t.Hide());
            ui.SetTurn(Mark.None);
            ui.HideBanner();
            flow = StartCoroutine(ResultRoutine(result, line));
        }

        private IEnumerator ResultRoutine(RoundResult result, int[] line)
        {
            yield return new WaitForSeconds(resultCelebrateDelay);

            switch (result)
            {
                case RoundResult.Tapu:
                    Celebrate(line, tapuLineColor);
                    tapu.Win();
                    pet.Surprise();
                    ui.Confetti(70);
                    Sound(s =>
                    {
                        s.PlaySfx(SfxId.WinLine);
                        s.PlaySfx(SfxId.Victory, 1f, 1f, 0.3f);
                        s.PlayVoice(isFinalLevel ? VoiceId.FinalWin : VoiceId.Win, VoicePriority.High, 0.35f, currentLevel);
                    });
                    GameManager.Instance.InvokeLevelWin();
                    break;

                case RoundResult.Pet:
                    Celebrate(line, petLineColor);
                    pet.Win();
                    tapu.Slump();
                    Sound(s =>
                    {
                        s.PlaySfx(SfxId.WinLine);
                        s.PlaySfx(SfxId.Lose, 1f, 1f, 0.3f);
                        s.PlayVoice(VoiceId.Lose, VoicePriority.High, 0.5f, currentLevel);
                    });
                    GameManager.Instance.InvokeLevelLose();
                    break;

                default:   // draw
                    for (int i = 0; i < cells.Length; i++) cells[i].DrawCelebrate(i * 0.06f);
                    tapu.Clap();
                    pet.Clap();
                    ui.Confetti(25);
                    Sound(s =>
                    {
                        s.PlaySfx(SfxId.Draw);
                        s.PlayVoice(VoiceId.Draw, VoicePriority.High, 0.35f);
                    });
                    GameManager.Instance.InvokeLevelDraw();
                    break;
            }
            flow = null;
        }

        // Winning squares glow + bounce together, and a bright line connects them.
        private void Celebrate(int[] line, Color color)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                bool inLine = Array.IndexOf(line, i) >= 0;
                if (inLine) cells[i].WinCelebrate(Array.IndexOf(line, i) * 0.08f);
                else cells[i].Dim(true);
            }
            if (winLine != null) winLine.Draw(cells[line[0]].Rect, cells[line[2]].Rect, color);
        }

        /// <summary>Called by LevelManager right before the end panel appears: calm everything down.</summary>
        public void HideCelebration()
        {
            if (winLine != null) winLine.Hide();
            if (ui != null) ui.ClearConfetti();
            if (tapu != null) tapu.Hide();   // the animated rigs would draw over the end panel
            if (pet != null) pet.Hide();
        }
    }
}
