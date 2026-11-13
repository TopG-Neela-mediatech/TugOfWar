using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace tmkoc.threeinarow
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private Button playSchoolBackButton;
        [Tooltip("Optional per-level scenery. Entry N is shown on level N (others hidden).")]
        [SerializeField] private GameObject[] levels;
        [Tooltip("One entry per level. Leave empty to use the 5 default levels.")]
        [SerializeField] private ThreeLevelConfig[] levelConfigs;
        [SerializeField] private float resultPanelDelay = 3.2f;

        public int currentLevelIndex { get; private set; }
        public bool HasLevelStarted { get; private set; }
        public bool IsFinalLevel => currentLevelIndex == TotalLevels - 1;
        private int TotalLevels => levelConfigs.Length;
        private bool isRetry;
        private bool resultPending;
        private const int WinStars = 5;
#if PLAYSCHOOL_MAIN
        private PetRoomStarRewards starRewards;
#endif

        private void StartLevel()
        {
            resultPending = false;
#if PLAYSCHOOL_MAIN
            starRewards = new PetRoomStarRewards();
#endif
            HasLevelStarted = true;
            GameManager.Instance.InvokeLevelStart();
        }

        private void Awake()
        {
            if (levelConfigs == null || levelConfigs.Length == 0) levelConfigs = ThreeLevelConfig.CreateDefaults();
            SetDataSaver();
            playSchoolBackButton.onClick.AddListener(() => SceneManager.LoadScene(TMKOCPlaySchoolConstants.TMKOCPlayRoomScene));
        }
        private void Start()
        {
            GameManager.Instance.OnLevelStart += OnLevelStart;
            GameManager.Instance.OnLevelWin += OnLevelWin;
            GameManager.Instance.OnLevelLose += OnLevelLose;

#if PLAYSCHOOL_MAIN
            // Startup is automatic when launched from the pet room; if it ever needs calling by hand:
            // PetPanelMinGameEndScript.Initialize(petActivityData);
#endif
            // Starts straight away on the level saved by HelperGameCategoryDataSaver (level 1 on a fresh start).
            StartLevel();
        }
        private void OnLevelStart()
        {
            ShowLevelScenery();
            bool isFinal = currentLevelIndex == TotalLevels - 1;
            GameManager.Instance.BoardController.BeginLevel(currentLevelIndex, levelConfigs[currentLevelIndex], isFinal, isRetry);
            isRetry = false;
        }
        private void OnLevelWin()
        {
            if (resultPending)
                return;
            resultPending = true;
#if PLAYSCHOOL_MAIN
            starRewards.AwardTotal(WinStars);
#endif
            StartCoroutine(LoadWinPanelWithDelay(resultPanelDelay));
        }
        private void OnLevelLose()
        {
            if (resultPending)
                return;
            resultPending = true;
            HelperGameCategoryDataSaver.AddAttempt();
            StartCoroutine(LoadLosePanelWithDelay(resultPanelDelay));
        }
        private IEnumerator LoadWinPanelWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            GameManager.Instance.BoardController.HideCelebration();
#if PLAYSCHOOL_MAIN
            // The result displays the same five stars already credited for this win.
            if (PetPanelMinGameEndScript.IsConfigured)
            {
                currentLevelIndex++;
                SaveCompletedLevel(currentLevelIndex);
                if (PetPanelMinGameEndScript.ShowGameEnd(RestartLevel, WinStars))
                    yield break;
                // The local panel advances the level itself when it closes.
                currentLevelIndex--;
            }
#endif
            GameManager.Instance.EndPanelScript.ShowWin();
        }
        private IEnumerator LoadLosePanelWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            GameManager.Instance.BoardController.HideCelebration();
#if PLAYSCHOOL_MAIN
            // 0 points for a lost level; the pet panel's retry button replays the same level.
            if (PetPanelMinGameEndScript.ShowRetry(RetryLevel, 0))
                yield break;
#endif
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
            SaveCompletedLevel(currentLevelIndex);
            if (currentLevelIndex < TotalLevels) return false;

            // The local result panel also wraps the final level back to level 1.
            currentLevelIndex = 0;
#if PLAYSCHOOL_MAIN
            return false;
#else
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return true;
#endif
        }

        private void SaveCompletedLevel(int completedLevels)
        {
#if PLAYSCHOOL_MAIN
            HelperGameCategoryDataSaver.SaveCategoryJson(completedLevels);
#else
            HelperGameCategoryDataSaver.LevelCompleted(completedLevels);
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
