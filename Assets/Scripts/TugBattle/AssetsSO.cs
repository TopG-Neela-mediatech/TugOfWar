using UnityEngine;

namespace tmkoc.tugofwar
{
    /// <summary>
    /// The sprites the game needs - kept to the minimum. Poses, stretch, taps, countdown and prompts are all
    /// tweens / Fredoka text, so no animation frames or text images are required.
    /// Anything left empty keeps its placeholder. Applied at runtime by TugAssetsApplier (GameManager.Assets).
    /// </summary>
    [CreateAssetMenu(fileName = "AssetsSO", menuName = "Tug Battle/AssetsSO")]
    public class AssetsSO : ScriptableObject
    {
        [Header("SCENE")]
        public Sprite background;
        public Sprite ground;
        public Sprite playerWinLine;
        public Sprite petWinLine;

        [Header("ROPE")]
        public Sprite rope;
        public Sprite centreFlag;

        [Header("CHARACTERS (one sprite each - poses are tweens)")]
        public Sprite player;
        public Sprite pet;

        [Header("TUTORIAL")]
        [Tooltip("Hand that taps in sets of 3 on level 1 and when the player starts losing.")]
        public Sprite tutorialHand;
    }
}
