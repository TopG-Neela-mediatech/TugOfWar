using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.threeinarow
{
    /// <summary>
    /// Pushes the sprites from GameManager.Assets (AssetsSO) into the scene: board plate, squares, face markers,
    /// the two characters, the winning line and the tutorial hand. Anything empty keeps its placeholder.
    /// The background is never touched: whatever you assign on the BG object stays.
    /// </summary>
    public class ThreeAssetsApplier : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        [Header("Targets")]
        [SerializeField] private Image boardPlate;
        [SerializeField] private Image gridImage;
        [SerializeField] private CellView[] cells;
        [SerializeField] private SideCharacter tapu;
        [SerializeField] private SideCharacter pet;
        [SerializeField] private WinLineView winLine;
        [SerializeField] private TutorialManager tutorial;

        private void Awake()
        {
            AssetsSO a = gameManager != null ? gameManager.Assets : null;
            if (a == null)
            {
                Debug.LogWarning("ThreeAssetsApplier: GameManager has no AssetsSO assigned - using placeholders.");
                return;
            }

            if (a.board != null && boardPlate != null)
            {
                boardPlate.sprite = a.board;
                boardPlate.type = Image.Type.Simple;
                boardPlate.preserveAspect = true;
                boardPlate.color = Color.white;
            }
            if (a.gridLines != null && gridImage != null)
            {
                gridImage.sprite = a.gridLines;
                gridImage.type = Image.Type.Simple;
                gridImage.preserveAspect = true;
                gridImage.color = Color.white;
            }
            Sprite petFace = a.petFace;
#if PLAYSCHOOL_MAIN
            PetType selectedPet = PlayroomSaveService.GetSelectedPet();

            if (selectedPet == PetType.Cat)
            {
                // Handle the cat activity.
                if (a.petFaceCat != null) petFace = a.petFaceCat;
            }
            else
            {
                // handle the dog
            }
#endif
            foreach (var c in cells) if (c != null) c.ApplyArt(a.emptySquare, a.tapuFace, petFace);
            if (tapu != null) tapu.ApplyArt(a.tapuCharacter);
            if (pet != null) pet.ApplyArt(a.petCharacter);
            if (winLine != null) winLine.ApplyArt(a.winLine);
            if (tutorial != null) tutorial.ApplyHand(a.tutorialHand);
        }
    }
}
