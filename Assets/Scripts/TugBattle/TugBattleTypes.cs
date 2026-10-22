using System;
using UnityEngine;

namespace tmkoc.tugofwar
{
    public enum MatchState { Idle, Countdown, Playing, Paused, Ending, Result }

    public enum PetState { Normal, Warn, Strong, Tired, Recover }

    /// <summary>Tap speed band: Slow / Normal / Fast / Very Fast (taps in the last second: &lt;3, &lt;6, &lt;9, 9+).</summary>
    public enum TapTier { Weak = 0, Normal = 1, Fast = 2, Power = 3 }

    /// <summary>Shared pose list for Player and Pet. Not every pose is used by both - leave unused ones empty.</summary>
    public enum Pose
    {
        Idle, Ready, RopeHold,
        NormalPull, StrongPull, VeryStrongPull, PowerPull,
        NearWin, FinalPull, Celebrate,
        Tired, Recover,
        WinningGround, LosingGround,
        Lose
    }

    public enum RopeState { Neutral, PlayerPull, PetPull, Stretch, StrongPull, PowerPull }

    public enum BannerKind { TapFast, KeepTapping, StrongPull, PetTired, PowerPull }

    public enum PopKind { Good, Great, SuperPull }

    /// <summary>Per level difficulty. Defaults match the HTML prototype (5 levels).</summary>
    [Serializable]
    public class TugLevelConfig
    {
        [Tooltip("Pet's base pull force (rope % per second).")]
        public float petPull = 6f;
        [Tooltip("Pull multiplier while in Strong Pull.")]
        public float strongMultiplier = 1.8f;
        [Tooltip("Random gap between strong pulls (seconds).")]
        public Vector2 strongGap = new Vector2(7f, 9f);
        [Tooltip("Warning time before strong pull starts.")]
        public float warnTime = 0.85f;
        public float strongDuration = 1.6f;
        [Tooltip("How long the pet stays tired - the player's attack window.")]
        public float tiredDuration = 3.2f;

        public static TugLevelConfig[] CreateDefaults()
        {
            return new[]
            {
                //  pull  strong  gap(min,max)   warn  strongDur  tired
                New(6f,    1.8f,  7f,   9f,      .85f, 1.6f,      3.2f),    // 1: gentle (intro)
                New(14.5f, 2.15f, 5.1f, 6.75f,   .68f, 2.1f,      2.25f),   // 2: halfway to level 3
                New(18.5f, 2.3f,  3.8f, 5.2f,    .6f,  2.3f,      1.8f),    // 3: challenging (eased from 23)
                New(21f,   2.4f,  3.5f, 4.8f,    .55f, 2.5f,      1.5f),    // 4: harder
                New(23.5f, 2.5f,  3.2f, 4.4f,    .5f,  2.6f,      1.3f),    // 5: hardest
            };
        }

        private static TugLevelConfig New(float pull, float strong, float gapMin, float gapMax, float warn, float strongDur, float tired)
        {
            return new TugLevelConfig
            {
                petPull = pull, strongMultiplier = strong, strongGap = new Vector2(gapMin, gapMax),
                warnTime = warn, strongDuration = strongDur, tiredDuration = tired
            };
        }
    }
}
