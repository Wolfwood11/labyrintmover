using UnityEngine;
using UnityEngine.UI;
using LabyrinthMover.Core;

namespace LabyrinthMover.UI
{
    /// <summary>
    /// Главное меню игры
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        [Header("UI Элементы")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button levelsButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Text versionText;
        [SerializeField] private Text progressText;
        
        [Header("Подменю")]
        [SerializeField] private LevelMenu levelMenu;
        [SerializeField] private SettingsMenu settingsMenu;
        [SerializeField] private TutorialManager tutorialManager;
        
        [Header("Настройки")]
        [SerializeField] private string gameVersion = "1.0.0";
        
        private void Start()
        {
            // Настраиваем кнопки
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);
                
            if (levelsButton != null)
                levelsButton.onClick.AddListener(OnLevelsClicked);
                
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OnSettingsClicked);
                
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
            
            // Инициализируем UI
            InitializeUI();
            
            // Проверяем, нужно ли показать обучение
            CheckTutorial();
        }
        
        private void InitializeUI()
        {
            // Устанавливаем версию
            if (versionText != null)
                versionText.text = $"Версия {gameVersion}";
            
            // Обновляем прогресс
            UpdateProgressDisplay();
        }
        
        private void UpdateProgressDisplay()
        {
            if (progressText != null && LevelProgressManager.Instance != null)
            {
                var stats = LevelProgressManager.Instance.GetDifficultyStats(LevelProgressManager.Instance.CurrentDifficulty);
                progressText.text = $"Прогресс: {stats.CompletedLevels}/{stats.TotalLevels} " +
                                  $"({stats.CompletionRate:P0}) - {stats.TotalStars} ⭐";
            }
        }
        
        private void CheckTutorial()
        {
            if (tutorialManager != null && tutorialManager.ShouldShowTutorial())
            {
                // Показываем обучение
                tutorialManager.StartTutorial();
            }
        }
        
        private void OnPlayClicked()
        {
            Debug.Log("Нажата кнопка 'Играть'");
            
            // Загружаем текущий уровень
            if (LevelProgressManager.Instance != null)
            {
                int level = LevelProgressManager.Instance.CurrentLevel;
                LevelDifficulty difficulty = LevelProgressManager.Instance.CurrentDifficulty;
                
                Debug.Log($"Загружаем уровень {level} сложности {difficulty}");
                
                // Здесь можно загрузить уровень или перейти к игре
                // Например: SceneManager.LoadScene("GameScene");
            }
        }
        
        private void OnLevelsClicked()
        {
            Debug.Log("Нажата кнопка 'Уровни'");
            
            // Показываем меню уровней
            if (levelMenu != null)
            {
                levelMenu.gameObject.SetActive(true);
                gameObject.SetActive(false); // Скрываем главное меню
            }
        }
        
        private void OnSettingsClicked()
        {
            Debug.Log("Нажата кнопка 'Настройки'");
            
            // Показываем меню настроек
            if (settingsMenu != null)
            {
                settingsMenu.ShowSettings();
            }
        }
        
        private void OnQuitClicked()
        {
            Debug.Log("Нажата кнопка 'Выход'");
            
            // Выходим из игры
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        
        /// <summary>
        /// Возвращается в главное меню
        /// </summary>
        public void ReturnToMainMenu()
        {
            // Показываем главное меню
            gameObject.SetActive(true);
            
            // Скрываем подменю
            if (levelMenu != null)
                levelMenu.gameObject.SetActive(false);
                
            if (settingsMenu != null)
                settingsMenu.HideSettings();
            
            // Обновляем прогресс
            UpdateProgressDisplay();
        }
        
        /// <summary>
        /// Обновляет отображение прогресса
        /// </summary>
        public void RefreshProgress()
        {
            UpdateProgressDisplay();
        }
    }
}
