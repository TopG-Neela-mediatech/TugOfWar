using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace tmkoc.tugofwar
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private Button playSchoolBackButton;
        [Tooltip("Optional per-level scenery. Entry N is shown on level N (others hidden).")]
        [SerializeField] private GameObject[] levels;
        [Tooltip("One entry per level. Leave empty to use the 5 default levels.")]
        [SerializeField] private TugLevelConfig[] levelConfigs;
        [SerializeField] private float resultPanelDelay = 1f;

        public int currentLevelIndex { get; private set; }
        public bool HasLevelStarted { get; private set; }
        public bool IsFinalLevel => currentLevelIndex == TotalLevels - 1;
        private int TotalLevels => levelConfigs.Length;
        private bool isRetry;

        private void StartLevel()
        {
            HasLevelStarted = true;
            GameManager.Instance.InvokeLevelStart();
        }

        private void Awake()
        {
            if (levelConfigs == null || levelConfigs.Length == 0) levelConfigs = TugLevelConfig.CreateDefaults();
            SetDataSaver();
            playSchoolBackButton.onClick.AddListener(() => SceneManager.LoadScene(TMKOCPlaySchoolConstants.TMKOCPlayMainMenu));
        }
        private void Start()
        {
            GameManager.Instance.OnLevelStart += OnLevelStart;
            GameManager.Instance.OnLevelWin += OnLevelWin;
            GameManager.Instance.OnLevelLose += OnLevelLose;

            // Starts straight away on the level saved by HelperGameCategoryDataSaver (level 1 on a fresh start).
            StartLevel();
        }
        private void OnLevelStart()
        {
            ShowLevelScenery();
            bool isFinal = currentLevelIndex == TotalLevels - 1;
            GameManager.Instance.BattleController.BeginLevel(currentLevelIndex, levelConfigs[currentLevelIndex], isFinal, isRetry);
            isRetry = false;
        }
        private void OnLevelWin()
        {
            StartCoroutine(LoadWinPanelWithDelay(resultPanelDelay));
        }
        private void OnLevelLose()
        {
            HelperGameCategoryDataSaver.AddAttempt();
            StartCoroutine(LoadLosePanelWithDelay(resultPanelDelay * 0.6f));
        }
        private IEnumerator LoadWinPanelWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            GameManager.Instance.EndPanelScript.ShowWin();
        }
        private IEnumerator LoadLosePanelWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            GameManager.Instance.EndPanelScript.ShowLose();
        }
        private void ShowLevelScenery()
        {
            if (levels == null) return;
            for (int i = 0; i < levels.Length; i++)
                if (levels[i] != null) levels[i].SetActive(i == currentLevelIndex);
        }
        private void SetDataSaver()
        {
            HelperGameCategoryDataSaver.Init(TotalLevels);
            currentLevelIndex = HelperGameCategoryDataSaver.GetStartLevel();
        }
        /// <returns>True when every level has been won and the game is being ended/restarted.</returns>
        private bool SaveLevel()
        {
            currentLevelIndex++;
            // Saved even past the last level; GetStartLevel() wraps a saved index >= level count back to 0.
            HelperGameCategoryDataSaver.LevelCompleted(currentLevelIndex);
            if (currentLevelIndex < TotalLevels) return false;

#if PLAYSCHOOL_MAIN
                EffectParticleControll.Instance.SpawnGameEndPanel();
                GameOverEndPanel.Instance.AddTheListnerRetryGame();
                return true;
#else
            // Not running inside Playschool Main: restart the whole game from level 1.
            currentLevelIndex = 0;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return true;
#endif
        }
        public void LoadNextLevel()
        {
            if (SaveLevel()) return;
            RestartLevel();
        }
        public void RetryLevel()
        {
            isRetry = true;
            RestartLevel();
        }
        // Restarts the current level index in place (index 0 after the last level) without reloading the scene.
        private void RestartLevel()
        {
            if (currentLevelIndex >= TotalLevels)
            {
                currentLevelIndex = 0;
            }
            HasLevelStarted = false;
            GameManager.Instance.SoundManager.StopAllExceptMusic();
            GameManager.Instance.InvokeLevelReset();
            StartLevel();
        }
        private void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnLevelStart -= OnLevelStart;
            GameManager.Instance.OnLevelWin -= OnLevelWin;
            GameManager.Instance.OnLevelLose -= OnLevelLose;
        }
    }
}
