using UnityEngine;

namespace tmkoc.tugofwar
{
    /// <summary>
    /// The sprites the game needs - kept to the minimum. Leaning, shaking, rope stretch, taps, countdown and prompts are
    /// all tweens / Fredoka text. Characters are a BODY plus swappable FACES (the face swap is the "animation").
    /// Anything left empty keeps its placeholder. Applied at runtime by TugAssetsApplier (GameManager.Assets).
    /// Scene art (background, ground, win lines, rope, flag) is NOT here: it is whatever you assign on those objects in the
    /// scene, and no script touches it.
    /// </summary>
    [CreateAssetMenu(fileName = "AssetsSO", menuName = "Tug Battle/AssetsSO")]
    public class AssetsSO : ScriptableObject
    {
        [Header("PLAYER (kid) - body + faces")]
        public Sprite playerBody;
        [Tooltip("Idle / ready / celebrating.")]
        public Sprite playerFaceHappy;
        [Tooltip("Pulling hard (alternates with the worried face while straining).")]
        public Sprite playerFaceStruggle;
        [Tooltip("Tired / losing ground / lose, and the 2nd frame of the strain animation.")]
        public Sprite playerFaceWorried;

        [Header("PET (dog) - body + faces")]
        public Sprite petBody;
        [Tooltip("Calm pull (rope in mouth). Also the 2nd frame of the strain animation.")]
        public Sprite petFaceDetermined;
        [Tooltip("Pulling hard.")]
        public Sprite petFaceStruggle;
        [Tooltip("Tired / losing ground / lose.")]
        public Sprite petFaceSad;

        [Header("TUTORIAL")]
        [Tooltip("Hand that taps in sets of 3 on level 1 and when the player starts losing.")]
        public Sprite tutorialHand;
    }
}
