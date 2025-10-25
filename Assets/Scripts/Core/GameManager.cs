using UnityEngine;
using LabyrinthMover.Core;
using LabyrinthMover.UI;
using LabyrinthMover.Gameplay;

namespace LabyrinthMover.Core
{
    /// <summary>
    /// Главный менеджер игры, координирующий все системы
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Компоненты")]
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private MoveExecutor moveExecutor;
        [SerializeField] private HudController hudController;
        [SerializeField] private CameraController cameraController;
        
        [Header("UI")]
        [SerializeField] private MainMenu mainMenu;
        [SerializeField] private LevelMenu levelMenu;
        [SerializeField] private SettingsMenu settingsMenu;
        [SerializeField] private TutorialManager tutorialManager;
        [SerializeField] private GameOverScreen gameOverScreen;
        
        [Header("Настройки")]
        [SerializeField] private bool autoStartLevel = false;
        [SerializeField] private int testLevel = 1;
        [SerializeField] private LevelDifficulty testDifficulty = LevelDifficulty.Easy;
        
        private bool isGameActive = false;
        private bool isLevelCompleted = false;
        private float levelStartTime;
        private int initialEnergy;
        private int initialMoves;
        
        public static GameManager Instance { get; private set; }
        
        public event System.Action OnGameStarted;
        public event System.Action OnGamePaused;
        public event System.Action OnGameResumed;
        public event System.Action OnGameEnded;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            InitializeGame();
            
            if (autoStartLevel)
            {
                StartLevel(testLevel, testDifficulty);
            }
        }
        
        private void InitializeGame()
        {
            // Инициализируем менеджер прогресса
            if (LevelProgressManager.Instance == null)
            {
                var progressManagerObj = new GameObject("LevelProgressManager");
                progressManagerObj.AddComponent<LevelProgressManager>();
            }
            
            // Настраиваем UI
            SetupUI();
            
            // Подписываемся на события
            SubscribeToEvents();
            
            Debug.Log("Игра инициализирована");
        }
        
        private void SetupUI()
        {
            // Настраиваем главное меню
            if (mainMenu != null)
            {
                mainMenu.gameObject.SetActive(true);
            }
            
            // Скрываем остальные UI
            if (levelMenu != null)
                levelMenu.gameObject.SetActive(false);
                
            if (settingsMenu != null)
                settingsMenu.HideSettings();
                
            if (gameOverScreen != null)
                gameOverScreen.HideScreen();
        }
        
        private void SubscribeToEvents()
        {
            // Подписываемся на события уровня
            if (levelManager != null)
            {
                levelManager.OnLevelCompleted += OnLevelCompleted;
                levelManager.OnLevelFailed += OnLevelFailed;
            }
            
            // Подписываемся на события игрока
            if (playerController != null)
            {
                playerController.OnPlayerReachedGoal += OnPlayerReachedGoal;
            }
            
            // Подписываемся на события движения
            if (moveExecutor != null)
            {
                moveExecutor.OnMoveExecuted += OnMoveExecuted;
            }
        }
        
        /// <summary>
        /// Запускает уровень
        /// </summary>
        public void StartLevel(int levelNumber, LevelDifficulty difficulty)
        {
            Debug.Log($"Запускаем уровень {levelNumber} сложности {difficulty}");
            
            // Скрываем меню
            if (mainMenu != null)
                mainMenu.gameObject.SetActive(false);
                
            if (levelMenu != null)
                levelMenu.gameObject.SetActive(false);
            
            // Инициализируем уровень
            if (levelManager != null)
            {
                levelManager.InitializeLevel(levelNumber, difficulty);
            }
            
            // Сбрасываем состояние игры
            isGameActive = true;
            isLevelCompleted = false;
            levelStartTime = Time.time;
            
            // Получаем начальные значения
            if (hudController != null)
            {
                initialEnergy = hudController.GetCurrentEnergy();
                initialMoves = hudController.GetCurrentMoves();
            }
            
            OnGameStarted?.Invoke();
        }
        
        /// <summary>
        /// Паузирует игру
        /// </summary>
        public void PauseGame()
        {
            if (!isGameActive) return;
            
            Time.timeScale = 0f;
            OnGamePaused?.Invoke();
        }
        
        /// <summary>
        /// Возобновляет игру
        /// </summary>
        public void ResumeGame()
        {
            if (!isGameActive) return;
            
            Time.timeScale = 1f;
            OnGameResumed?.Invoke();
        }
        
        /// <summary>
        /// Завершает игру
        /// </summary>
        public void EndGame()
        {
            isGameActive = false;
            Time.timeScale = 1f;
            
            OnGameEnded?.Invoke();
        }
        
        /// <summary>
        /// Возвращается в главное меню
        /// </summary>
        public void ReturnToMainMenu()
        {
            EndGame();
            
            if (mainMenu != null)
            {
                mainMenu.ReturnToMainMenu();
            }
        }
        
        // Обработчики событий
        private void OnLevelCompleted()
        {
            if (isLevelCompleted) return;
            
            isLevelCompleted = true;
            
            // Вычисляем результаты
            float timeSpent = Time.time - levelStartTime;
            int energyUsed = initialEnergy - (hudController != null ? hudController.GetCurrentEnergy() : 0);
            int movesUsed = hudController != null ? hudController.GetCurrentMoves() : 0;
            
            // Вычисляем звезды
            int stars = CalculateStars(energyUsed, movesUsed, timeSpent, testDifficulty);
            
            // Сохраняем прогресс
            if (LevelProgressManager.Instance != null)
            {
                LevelProgressManager.Instance.CompleteLevel(testLevel, testDifficulty, energyUsed, movesUsed, timeSpent);
            }
            
            // Показываем экран завершения
            if (gameOverScreen != null)
            {
                gameOverScreen.ShowLevelCompleted(testLevel, testDifficulty, energyUsed, movesUsed, timeSpent, stars);
            }
            
            Debug.Log($"Уровень {testLevel} завершен! Энергия: {energyUsed}, Ходы: {movesUsed}, Время: {timeSpent:F1}с, Звезды: {stars}");
        }
        
        private void OnLevelFailed()
        {
            if (isLevelCompleted) return;
            
            // Вычисляем результаты
            float timeSpent = Time.time - levelStartTime;
            int energyUsed = initialEnergy - (hudController != null ? hudController.GetCurrentEnergy() : 0);
            int movesUsed = hudController != null ? hudController.GetCurrentMoves() : 0;
            
            // Показываем экран поражения
            if (gameOverScreen != null)
            {
                gameOverScreen.ShowLevelFailed(testLevel, testDifficulty, energyUsed, movesUsed, timeSpent);
            }
            
            Debug.Log($"Уровень {testLevel} не пройден. Энергия: {energyUsed}, Ходы: {movesUsed}, Время: {timeSpent:F1}с");
        }
        
        private void OnPlayerReachedGoal()
        {
            OnLevelCompleted();
        }
        
        private void OnMoveExecuted(int energyCost)
        {
            // Обновляем HUD
            if (hudController != null)
            {
                hudController.UpdateEnergy(energyCost);
                hudController.UpdateMoves(1);
            }
        }
        
        private int CalculateStars(int energyUsed, int movesUsed, float timeSpent, LevelDifficulty difficulty)
        {
            // Упрощенная система звезд
            int stars = 1; // Минимум 1 звезда за завершение
            
            // Бонус за эффективность энергии
            int targetEnergy = GetTargetEnergy(difficulty);
            if (energyUsed <= targetEnergy * 0.7f)
                stars++;
                
            // Бонус за минимальные ходы
            int targetMoves = GetTargetMoves(difficulty);
            if (movesUsed <= targetMoves * 0.8f)
                stars++;
                
            return Mathf.Min(stars, 3);
        }
        
        private int GetTargetEnergy(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Easy: return 12;
                case LevelDifficulty.Medium: return 25;
                case LevelDifficulty.Hard: return 40;
                default: return 20;
            }
        }
        
        private int GetTargetMoves(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Easy: return 6;
                case LevelDifficulty.Medium: return 12;
                case LevelDifficulty.Hard: return 20;
                default: return 10;
            }
        }
        
        // Геттеры
        public bool IsGameActive => isGameActive;
        public bool IsLevelCompleted => isLevelCompleted;
        public float LevelTime => Time.time - levelStartTime;
    }
}
