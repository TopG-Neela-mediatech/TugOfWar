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
        [SerializeField] private TugBattleController battle;
        [Tooltip("One entry per level. Leave empty to use the 5 default levels.")]
        [SerializeField] private TugLevelConfig[] levelConfigs;
        [SerializeField] private float resultPanelDelay = 1.8f;

        public int currentLevelIndex { get; private set; }
        private int TotalLevels => levelConfigs.Length;
        private bool isRetry;

        private void Awake()
        {
            if (levelConfigs == null || levelConfigs.Length == 0) levelConfigs = TugLevelConfig.CreateDefaults();
            SetDataSaver();
            PlayschoolCommon.Instance.SpawnplayschoolWinLosePanel();
            playSchoolBackButton.onClick.AddListener(() => SceneManager.LoadScene(TMKOCPlaySchoolConstants.TMKOCPlayMainMenu));
        }
        private void Start()
        {
            GameManager.Instance.OnLevelStart += OnLevelStart;
            GameManager.Instance.OnLevelWin += OnLevelWin;
            GameManager.Instance.OnLevelLose += OnLevelLose;
            StartLevel();
        }
        private void StartLevel() => GameManager.Instance.InvokeLevelStart();

        private void OnLevelStart()
        {
            ShowLevelScenery();
            bool isFinal = currentLevelIndex == TotalLevels - 1;
            battle.BeginLevel(currentLevelIndex, levelConfigs[currentLevelIndex], isFinal, isRetry);
            isRetry = false;
        }
        private void OnLevelWin()
        {
            SaveLevel();
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
            SoundManager.Instance?.StopAllExceptMusic();
            WinLosePanelScript.Instance.ShowNextLevelPopUp(LoadNextLevel);
        }
        private IEnumerator LoadLosePanelWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SoundManager.Instance?.StopAllExceptMusic();
            WinLosePanelScript.Instance.ShowRetryPopUp(RetryLevel);
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
        private void SaveLevel()
        {
            currentLevelIndex++;
            // Helper convention: LevelCompleted(n) = n levels finished (n == total -> 100% / 5 stars)
            HelperGameCategoryDataSaver.LevelCompleted(currentLevelIndex);
            if (currentLevelIndex >= TotalLevels)
            {
#if PLAYSCHOOL_MAIN
                    EffectParticleControll.Instance.SpawnGameEndPanel();
                   //GameManager.Instance.SoundManager.PlayFinalOutro();
                    GameOverEndPanel.Instance.AddTheListnerRetryGame();
                    return;
#endif
                currentLevelIndex = 0; // all levels done - "next" replays from level 1
            }
        }
        public void LoadNextLevel()
        {
            if (currentLevelIndex < TotalLevels)
            {
                StartLevel();
            }
        }
        private void RetryLevel()
        {
            isRetry = true;
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
