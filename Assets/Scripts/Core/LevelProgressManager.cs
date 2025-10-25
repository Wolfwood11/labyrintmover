using System;
using System.Collections.Generic;
using UnityEngine;

namespace LabyrinthMover.Core
{
    /// <summary>
    /// Менеджер прогресса и уровней игрока
    /// </summary>
    public class LevelProgressManager : MonoBehaviour
    {
        [Header("Настройки")]
        [SerializeField] private bool enableTutorial = true;
        [SerializeField] private int maxLevelsPerDifficulty = 10;
        
        [Header("Текущий прогресс")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private LevelDifficulty currentDifficulty = LevelDifficulty.Easy;
        [SerializeField] private bool tutorialCompleted = false;
        
        private Dictionary<int, LevelProgress> levelProgress = new Dictionary<int, LevelProgress>();
        
        public static LevelProgressManager Instance { get; private set; }
        
        public event Action<int, LevelDifficulty> LevelCompleted;
        public event Action<LevelDifficulty> DifficultyChanged;
        
        [Serializable]
        public class LevelProgress
        {
            public int LevelNumber;
            public LevelDifficulty Difficulty;
            public bool IsCompleted;
            public int BestEnergyUsed;
            public int BestMovesUsed;
            public float BestTime;
            public int StarsEarned; // 1-3 звезды
            public DateTime CompletionDate;
        }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadProgress();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Завершает уровень с результатами
        /// </summary>
        public void CompleteLevel(int levelNumber, LevelDifficulty difficulty, int energyUsed, int movesUsed, float timeSpent)
        {
            var progress = GetOrCreateLevelProgress(levelNumber, difficulty);
            
            // Обновляем лучшие результаты
            if (!progress.IsCompleted || energyUsed < progress.BestEnergyUsed)
            {
                progress.BestEnergyUsed = energyUsed;
            }
            
            if (!progress.IsCompleted || movesUsed < progress.BestMovesUsed)
            {
                progress.BestMovesUsed = movesUsed;
            }
            
            if (!progress.IsCompleted || timeSpent < progress.BestTime)
            {
                progress.BestTime = timeSpent;
            }
            
            progress.IsCompleted = true;
            progress.CompletionDate = DateTime.Now;
            
            // Вычисляем звезды
            progress.StarsEarned = CalculateStars(energyUsed, movesUsed, timeSpent, difficulty);
            
            // Проверяем, нужно ли разблокировать следующий уровень
            if (levelNumber == currentLevel && difficulty == currentDifficulty)
            {
                UnlockNextLevel();
            }
            
            LevelCompleted?.Invoke(levelNumber, difficulty);
            SaveProgress();
            
            Debug.Log($"Уровень {levelNumber} ({difficulty}) завершен! " +
                     $"Энергия: {energyUsed}, Ходы: {movesUsed}, Время: {timeSpent:F1}с, Звезды: {progress.StarsEarned}");
        }
        
        private int CalculateStars(int energyUsed, int movesUsed, float timeSpent, LevelDifficulty difficulty)
        {
            // Упрощенная система звезд
            int stars = 1; // Минимум 1 звезда за завершение
            
            // Бонус за эффективность энергии
            if (energyUsed <= GetTargetEnergy(difficulty) * 0.7f)
                stars++;
                
            // Бонус за минимальные ходы
            if (movesUsed <= GetTargetMoves(difficulty) * 0.8f)
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
        
        private void UnlockNextLevel()
        {
            currentLevel++;
            
            // Проверяем, нужно ли перейти к следующей сложности
            if (currentLevel > maxLevelsPerDifficulty)
            {
                currentLevel = 1;
                currentDifficulty = GetNextDifficulty(currentDifficulty);
                DifficultyChanged?.Invoke(currentDifficulty);
            }
        }
        
        private LevelDifficulty GetNextDifficulty(LevelDifficulty current)
        {
            switch (current)
            {
                case LevelDifficulty.Easy: return LevelDifficulty.Medium;
                case LevelDifficulty.Medium: return LevelDifficulty.Hard;
                case LevelDifficulty.Hard: return LevelDifficulty.Hard; // Максимальная сложность
                default: return LevelDifficulty.Easy;
            }
        }
        
        private LevelProgress GetOrCreateLevelProgress(int levelNumber, LevelDifficulty difficulty)
        {
            int key = GetLevelKey(levelNumber, difficulty);
            
            if (!levelProgress.ContainsKey(key))
            {
                levelProgress[key] = new LevelProgress
                {
                    LevelNumber = levelNumber,
                    Difficulty = difficulty,
                    IsCompleted = false,
                    BestEnergyUsed = int.MaxValue,
                    BestMovesUsed = int.MaxValue,
                    BestTime = float.MaxValue,
                    StarsEarned = 0
                };
            }
            
            return levelProgress[key];
        }
        
        private int GetLevelKey(int levelNumber, LevelDifficulty difficulty)
        {
            return levelNumber * 100 + (int)difficulty;
        }
        
        /// <summary>
        /// Проверяет, завершен ли уровень
        /// </summary>
        public bool IsLevelCompleted(int levelNumber, LevelDifficulty difficulty)
        {
            int key = GetLevelKey(levelNumber, difficulty);
            return levelProgress.ContainsKey(key) && levelProgress[key].IsCompleted;
        }
        
        /// <summary>
        /// Получает прогресс уровня
        /// </summary>
        public LevelProgress GetLevelProgress(int levelNumber, LevelDifficulty difficulty)
        {
            int key = GetLevelKey(levelNumber, difficulty);
            return levelProgress.ContainsKey(key) ? levelProgress[key] : null;
        }
        
        /// <summary>
        /// Проверяет, нужно ли показать обучение
        /// </summary>
        public bool ShouldShowTutorial()
        {
            return enableTutorial && !tutorialCompleted && currentLevel == 1 && currentDifficulty == LevelDifficulty.Easy;
        }
        
        /// <summary>
        /// Отмечает обучение как завершенное
        /// </summary>
        public void CompleteTutorial()
        {
            tutorialCompleted = true;
            SaveProgress();
        }
        
        /// <summary>
        /// Получает статистику по сложности
        /// </summary>
        public DifficultyStats GetDifficultyStats(LevelDifficulty difficulty)
        {
            var stats = new DifficultyStats { Difficulty = difficulty };
            
            foreach (var progress in levelProgress.Values)
            {
                if (progress.Difficulty == difficulty)
                {
                    stats.TotalLevels++;
                    if (progress.IsCompleted)
                    {
                        stats.CompletedLevels++;
                        stats.TotalStars += progress.StarsEarned;
                        stats.TotalEnergyUsed += progress.BestEnergyUsed;
                        stats.TotalMovesUsed += progress.BestMovesUsed;
                    }
                }
            }
            
            return stats;
        }
        
        private void SaveProgress()
        {
            // Сохранение в PlayerPrefs (в реальной игре лучше использовать JSON файл)
            PlayerPrefs.SetInt("CurrentLevel", currentLevel);
            PlayerPrefs.SetInt("CurrentDifficulty", (int)currentDifficulty);
            PlayerPrefs.SetInt("TutorialCompleted", tutorialCompleted ? 1 : 0);
            
            // Сохранение прогресса уровней (упрощенная версия)
            PlayerPrefs.SetInt("LevelProgressCount", levelProgress.Count);
            int index = 0;
            foreach (var kvp in levelProgress)
            {
                var progress = kvp.Value;
                string prefix = $"LevelProgress_{index}_";
                PlayerPrefs.SetInt(prefix + "Key", kvp.Key);
                PlayerPrefs.SetInt(prefix + "LevelNumber", progress.LevelNumber);
                PlayerPrefs.SetInt(prefix + "Difficulty", (int)progress.Difficulty);
                PlayerPrefs.SetInt(prefix + "IsCompleted", progress.IsCompleted ? 1 : 0);
                PlayerPrefs.SetInt(prefix + "BestEnergy", progress.BestEnergyUsed);
                PlayerPrefs.SetInt(prefix + "BestMoves", progress.BestMovesUsed);
                PlayerPrefs.SetFloat(prefix + "BestTime", progress.BestTime);
                PlayerPrefs.SetInt(prefix + "Stars", progress.StarsEarned);
                index++;
            }
            
            PlayerPrefs.Save();
        }
        
        private void LoadProgress()
        {
            currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
            currentDifficulty = (LevelDifficulty)PlayerPrefs.GetInt("CurrentDifficulty", 0);
            tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
            
            // Загрузка прогресса уровней
            int count = PlayerPrefs.GetInt("LevelProgressCount", 0);
            for (int i = 0; i < count; i++)
            {
                string prefix = $"LevelProgress_{i}_";
                int key = PlayerPrefs.GetInt(prefix + "Key", -1);
                if (key >= 0)
                {
                    var progress = new LevelProgress
                    {
                        LevelNumber = PlayerPrefs.GetInt(prefix + "LevelNumber", 1),
                        Difficulty = (LevelDifficulty)PlayerPrefs.GetInt(prefix + "Difficulty", 0),
                        IsCompleted = PlayerPrefs.GetInt(prefix + "IsCompleted", 0) == 1,
                        BestEnergyUsed = PlayerPrefs.GetInt(prefix + "BestEnergy", int.MaxValue),
                        BestMovesUsed = PlayerPrefs.GetInt(prefix + "BestMoves", int.MaxValue),
                        BestTime = PlayerPrefs.GetFloat(prefix + "BestTime", float.MaxValue),
                        StarsEarned = PlayerPrefs.GetInt(prefix + "Stars", 0)
                    };
                    levelProgress[key] = progress;
                }
            }
        }
        
        // Геттеры для UI
        public int CurrentLevel => currentLevel;
        public LevelDifficulty CurrentDifficulty => currentDifficulty;
        public bool TutorialCompleted => tutorialCompleted;
    }
    
    [Serializable]
    public class DifficultyStats
    {
        public LevelDifficulty Difficulty;
        public int TotalLevels;
        public int CompletedLevels;
        public int TotalStars;
        public int TotalEnergyUsed;
        public int TotalMovesUsed;
        
        public float CompletionRate => TotalLevels > 0 ? (float)CompletedLevels / TotalLevels : 0f;
        public float AverageStars => CompletedLevels > 0 ? (float)TotalStars / CompletedLevels : 0f;
    }
}
