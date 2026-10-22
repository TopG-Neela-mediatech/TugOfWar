using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.tugofwar
{
    /// <summary>
    /// Pushes the character art (bodies + faces) and the tutorial hand from GameManager.Assets (AssetsSO) into the scene.
    /// Scene art (background, ground, win lines, rope, flag) is never touched: whatever you assign in the editor stays.
    /// </summary>
    public class TugAssetsApplier : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        [Header("Targets")]
        [SerializeField] private CharacterView player;
        [SerializeField] private CharacterView pet;
        [SerializeField] private TutorialManager tutorial;

        private void Awake()
        {
            AssetsSO a = gameManager != null ? gameManager.Assets : null;
            if (a == null)
            {
                Debug.LogWarning("TugAssetsApplier: GameManager has no AssetsSO assigned - using placeholders.");
                return;
            }

            // body + faces. Faces: calm / effort / strain (2nd frame of the strain animation) / sad
            player.ApplyArt(a.playerBody, a.playerFaceHappy, a.playerFaceStruggle, a.playerFaceWorried, a.playerFaceWorried);
            pet.ApplyArt(a.petBody, a.petFaceDetermined, a.petFaceStruggle, a.petFaceDetermined, a.petFaceSad);

            if (tutorial != null) tutorial.ApplyHand(a.tutorialHand);
        }
    }
}
