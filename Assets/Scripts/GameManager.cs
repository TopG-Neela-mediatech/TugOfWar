using System;
using UnityEngine;

namespace tmkoc.tugofwar
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private LevelManager levelManager;     
        [SerializeField] private SoundManager soundManager;
        [SerializeField] private TugBattleController battleController;
        [SerializeField] private EndPanelScript endPanelScript;
        private static GameManager instance;


        public static GameManager Instance { get { return instance; } }
        public LevelManager LevelManager { get { return levelManager; } }
        public SoundManager SoundManager { get { return soundManager; } }
        public TugBattleController BattleController { get { return battleController; } }
        public EndPanelScript EndPanelScript { get { return endPanelScript; } }
     
      
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }


        #region Events
        public event Action OnLevelWin;      
        public event Action OnLevelLose;
        public event Action OnLevelStart;
        public event Action OnLevelReset;
        public void InvokeLevelReset() => OnLevelReset?.Invoke();
        public void InvokeLevelStart() => OnLevelStart?.Invoke();
        public void InvokeLevelWin() => OnLevelWin?.Invoke();      
        public void InvokeLevelLose() => OnLevelLose?.Invoke();
        #endregion
    }
}
