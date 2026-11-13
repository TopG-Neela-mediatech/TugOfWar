using UnityEngine;

namespace tmkoc.threeinarow
{
    /// <summary>
    /// The sprites the game needs - kept to the minimum. Pops, hops, glows, bounces, the winning line and confetti are tweens.
    /// Anything left empty keeps its placeholder. Applied at runtime by ThreeAssetsApplier (GameManager.Assets).
    /// The background is NOT here: it is whatever you assign on the BG object in the scene.
    /// </summary>
    [CreateAssetMenu(fileName = "ThreeInARowAssetsSO", menuName = "Three In A Row/AssetsSO")]
    public class AssetsSO : ScriptableObject
    {
        [Header("BOARD")]
        [Tooltip("Optional board plate behind the 3x3 squares.")]
        public Sprite board;
        [Tooltip("The # lines drawn over the board plate.")]
        public Sprite gridLines;
        [Tooltip("Optional: a visible empty square. Leave empty for plain tap areas on the board.")]
        public Sprite emptySquare;

        [Header("FACE MARKERS (placed inside a square)")]
        public Sprite tapuFace;
        public Sprite petFace;
        [Tooltip("Used instead of the pet face when Playschool Main says the selected pet is the cat.")]
        public Sprite petFaceCat;

        [Header("CHARACTERS (standing beside the board)")]
        public Sprite tapuCharacter;
        public Sprite petCharacter;

        [Header("WINNING LINE (optional - a plain bar is used when empty)")]
        public Sprite winLine;

        [Header("TUTORIAL")]
        [Tooltip("Hand that taps the squares on level 1 and when the player waits too long.")]
        public Sprite tutorialHand;
    }
}
