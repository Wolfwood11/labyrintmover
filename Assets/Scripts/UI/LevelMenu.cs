using UnityEngine;
using UnityEngine.UI;
using LabyrinthMover.Core;
using System.Collections.Generic;

namespace LabyrinthMover.UI
{
    /// <summary>
    /// Главное меню выбора уровней
    /// </summary>
    public class LevelMenu : MonoBehaviour
    {
        [Header("UI Элементы")]
        [SerializeField] private Transform levelContainer;
        [SerializeField] private GameObject levelSelectorPrefab;
        [SerializeField] private Text difficultyText;
        [SerializeField] private Button previousDifficultyButton;
        [SerializeField] private Button nextDifficultyButton;
        [SerializeField] private Text progressText;
        
        [Header("Настройки")]
        [SerializeField] private int levelsPerDifficulty = 10;
        
        private LevelDifficulty currentDifficulty = LevelDifficulty.Easy;
        private List<LevelSelector> levelSelectors = new List<LevelSelector>();
        
        private void Start()
        {
            InitializeMenu();
            UpdateDifficultyDisplay();
        }
        
        private void InitializeMenu()
        {
            // Очищаем контейнер
            if (levelContainer != null)
            {
                foreach (Transform child in levelContainer)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
            
            // Создаем селекторы уровней
            for (int i = 1; i <= levelsPerDifficulty; i++)
            {
                CreateLevelSelector(i);
            }
            
            // Настраиваем кнопки
            if (previousDifficultyButton != null)
                previousDifficultyButton.onClick.AddListener(PreviousDifficulty);
                
            if (nextDifficultyButton != null)
                nextDifficultyButton.onClick.AddListener(NextDifficulty);
        }
        
        private void CreateLevelSelector(int levelNumber)
        {
            if (levelSelectorPrefab == null || levelContainer == null) return;
            
            GameObject selectorObj = Instantiate(levelSelectorPrefab, levelContainer);
            LevelSelector selector = selectorObj.GetComponent<LevelSelector>();
            
            if (selector != null)
            {
                // Проверяем, разблокирован ли уровень
                bool isUnlocked = IsLevelUnlocked(levelNumber, currentDifficulty);
                
                // Получаем прогресс уровня
                var progress = LevelProgressManager.Instance?.GetLevelProgress(levelNumber, currentDifficulty);
                
                // Инициализируем селектор
                selector.Initialize(levelNumber, currentDifficulty, isUnlocked, progress);
                selector.OnLevelSelected += OnLevelSelected;
                
                levelSelectors.Add(selector);
            }
        }
        
        private bool IsLevelUnlocked(int levelNumber, LevelDifficulty difficulty)
        {
            // Первый уровень всегда разблокирован
            if (levelNumber == 1) return true;
            
            // Проверяем, завершен ли предыдущий уровень
            var previousProgress = LevelProgressManager.Instance?.GetLevelProgress(levelNumber - 1, difficulty);
            return previousProgress != null && previousProgress.IsCompleted;
        }
        
        private void OnLevelSelected(int levelNumber, LevelDifficulty difficulty)
        {
            Debug.Log($"Выбран уровень {levelNumber} сложности {difficulty}");
            
            // Здесь можно загрузить уровень или перейти к игре
            // Например: SceneManager.LoadScene("GameScene");
        }
        
        private void PreviousDifficulty()
        {
            currentDifficulty = GetPreviousDifficulty(currentDifficulty);
            UpdateDifficultyDisplay();
            RefreshLevelSelectors();
        }
        
        private void NextDifficulty()
        {
            currentDifficulty = GetNextDifficulty(currentDifficulty);
            UpdateDifficultyDisplay();
            RefreshLevelSelectors();
        }
        
        private LevelDifficulty GetPreviousDifficulty(LevelDifficulty current)
        {
            switch (current)
            {
                case LevelDifficulty.Medium: return LevelDifficulty.Easy;
                case LevelDifficulty.Hard: return LevelDifficulty.Medium;
                default: return LevelDifficulty.Easy;
            }
        }
        
        private LevelDifficulty GetNextDifficulty(LevelDifficulty current)
        {
            switch (current)
            {
                case LevelDifficulty.Easy: return LevelDifficulty.Medium;
                case LevelDifficulty.Medium: return LevelDifficulty.Hard;
                default: return LevelDifficulty.Hard;
            }
        }
        
        private void UpdateDifficultyDisplay()
        {
            if (difficultyText != null)
            {
                difficultyText.text = GetDifficultyText(currentDifficulty);
            }
            
            // Обновляем состояние кнопок
            if (previousDifficultyButton != null)
                previousDifficultyButton.interactable = currentDifficulty != LevelDifficulty.Easy;
                
            if (nextDifficultyButton != null)
                nextDifficultyButton.interactable = currentDifficulty != LevelDifficulty.Hard;
            
            // Обновляем прогресс
            UpdateProgressDisplay();
        }
        
        private void UpdateProgressDisplay()
        {
            if (progressText != null && LevelProgressManager.Instance != null)
            {
                var stats = LevelProgressManager.Instance.GetDifficultyStats(currentDifficulty);
                progressText.text = $"Прогресс: {stats.CompletedLevels}/{stats.TotalLevels} " +
                                  $"({stats.CompletionRate:P0}) - {stats.TotalStars} ⭐";
            }
        }
        
        private void RefreshLevelSelectors()
        {
            foreach (var selector in levelSelectors)
            {
                if (selector != null)
                {
                    int levelNumber = GetLevelNumberFromSelector(selector);
                    bool isUnlocked = IsLevelUnlocked(levelNumber, currentDifficulty);
                    var progress = LevelProgressManager.Instance?.GetLevelProgress(levelNumber, currentDifficulty);
                    
                    selector.Initialize(levelNumber, currentDifficulty, isUnlocked, progress);
                }
            }
        }
        
        private int GetLevelNumberFromSelector(LevelSelector selector)
        {
            // Простое решение - используем индекс в списке + 1
            int index = levelSelectors.IndexOf(selector);
            return index + 1;
        }
        
        private string GetDifficultyText(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Easy: return "Легкий";
                case LevelDifficulty.Medium: return "Средний";
                case LevelDifficulty.Hard: return "Сложный";
                default: return "Неизвестно";
            }
        }
    }
}
