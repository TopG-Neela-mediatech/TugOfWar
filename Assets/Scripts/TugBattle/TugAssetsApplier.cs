using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.tugofwar
{
    /// <summary>
    /// Pushes the sprites from GameManager.Assets (AssetsSO) into the scene when it loads.
    /// Every slot you leave empty in AssetsSO simply keeps the placeholder shape.
    /// </summary>
    public class TugAssetsApplier : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        [Header("Targets")]
        [SerializeField] private RopeView ropeView;
        [SerializeField] private CharacterView player;
        [SerializeField] private CharacterView pet;
        [SerializeField] private TutorialManager tutorial;

        [Header("Scene images")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image groundImage;
        [SerializeField] private Image playerWinLineImage;
        [SerializeField] private Image petWinLineImage;

        private void Awake()
        {
            AssetsSO a = gameManager != null ? gameManager.Assets : null;
            if (a == null)
            {
                Debug.LogWarning("TugAssetsApplier: GameManager has no AssetsSO assigned - using placeholders.");
                return;
            }

            player.SetSprite(a.player);
            pet.SetSprite(a.pet);
            ropeView.ApplySprites(a.rope, a.centreFlag);
            if (tutorial != null) tutorial.ApplyHand(a.tutorialHand);

            Skin(backgroundImage, a.background);
            Skin(groundImage, a.ground);
            Skin(playerWinLineImage, a.playerWinLine);
            Skin(petWinLineImage, a.petWinLine);
        }

        private static void Skin(Image img, Sprite sprite)
        {
            if (img == null || sprite == null) return;
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.color = Color.white;
        }
    }
}
