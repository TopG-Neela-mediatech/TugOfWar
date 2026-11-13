using System;
using UnityEngine;

namespace tmkoc.threeinarow
{
    public enum Mark { None = 0, Tapu = 1, Pet = 2 }

    public enum MatchState { Idle, Intro, PlayerTurn, PetThinking, Result }

    public enum RoundResult { None, Tapu, Pet, Draw }

    /// <summary>What the pet says / does about the current board (used for hints and voice cues).</summary>
    public enum BannerKind { YourTurn, PetThinking, TwoInARow, StopPet, LetsPlay }

    /// <summary>Per level difficulty. Easy = random moves, Hard = blocks everything (with a small chance to slip so it stays winnable).</summary>
    [Serializable]
    public class ThreeLevelConfig
    {
        [Tooltip("Chance (0-1) the pet ignores strategy and just picks any empty square.")]
        [Range(0f, 1f)] public float randomMove = 1f;
        [Tooltip("Chance (0-1) the pet completes its own line when it can.")]
        [Range(0f, 1f)] public float takeWin = 0f;
        [Tooltip("Chance (0-1) the pet blocks the player's two-in-a-row.")]
        [Range(0f, 1f)] public float block = 0f;
        [Tooltip("Seconds the pet 'thinks' (head tilt) before placing its face.")]
        public Vector2 thinkTime = new Vector2(1.2f, 1.8f);
        [Tooltip("Highlight the square that wins (or blocks) after the player has been idle - helps the youngest kids.")]
        public bool hintWinningSquare = true;
        [Tooltip("Seconds of waiting before the hint / available-square pulse appears.")]
        public float hintDelay = 5f;

        public static ThreeLevelConfig[] CreateDefaults()
        {
            return new[]
            {
                //  random  win   block  think(min,max)  hint  hintDelay
                New(1.00f, 0.00f, 0.00f, 1.3f, 1.8f,     true,  4f),   // 1 Easy: random moves
                New(0.75f, 0.40f, 0.10f, 1.1f, 1.6f,     true,  5f),   // 2 Easy+
                New(0.35f, 0.90f, 0.50f, 0.9f, 1.4f,     false, 7f),   // 3 Medium: sometimes blocks
                New(0.20f, 1.00f, 0.80f, 0.8f, 1.3f,     false, 8f),   // 4 Medium+
                New(0.10f, 1.00f, 1.00f, 0.7f, 1.2f,     false, 9f),   // 5 Hard: blocks and wins (still slips now and then)
            };
        }

        private static ThreeLevelConfig New(float random, float win, float block, float thinkMin, float thinkMax, bool hint, float hintDelay)
        {
            return new ThreeLevelConfig
            {
                randomMove = random, takeWin = win, block = block,
                thinkTime = new Vector2(thinkMin, thinkMax), hintWinningSquare = hint, hintDelay = hintDelay
            };
        }
    }
}
