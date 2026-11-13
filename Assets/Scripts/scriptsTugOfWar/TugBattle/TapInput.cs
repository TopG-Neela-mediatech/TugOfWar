using UnityEngine;
using UnityEngine.EventSystems;

namespace tmkoc.tugofwar
{
    /// <summary>
    /// Put on a full-screen transparent UI Image (Raycast Target ON), sibling order BELOW the pause button.
    /// Every finger-down is one tap, so multi-finger tapping works. Space / Enter also taps in the Editor.
    /// </summary>
    public class TapInput : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private TugBattleController controller;

        public void OnPointerDown(PointerEventData eventData)
        {
            controller.OnTap(eventData.position);
        }

#if UNITY_EDITOR && ENABLE_LEGACY_INPUT_MANAGER
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                controller.OnTap(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        }
#endif
    }
}
