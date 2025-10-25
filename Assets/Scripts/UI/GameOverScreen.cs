using UnityEngine;
using UnityEngine.UI;
using LabyrinthMover.Core;

namespace LabyrinthMover.UI
{
    /// <summary>
    /// Экран завершения уровня с результатами
    /// </summary>
    public class GameOverScreen : MonoBehaviour
    {
        [Header("UI Элементы")]
        [SerializeField] private GameObject screenPanel;
        [SerializeField] private Text resultText;
        [SerializeField] private Text energyText;
        [SerializeField] private Text movesText;
        [SerializeField] private Text timeText;
        [SerializeField] private Text starsText;
        [SerializeField] private Image[] starImages;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;
        
        [Header("Настройки")]
        [SerializeField] private Color successColor = Color.green;
        [SerializeField] private Color failureColor = Color.red;
        
        private bool isLevelCompleted = false;
        private int energyUsed = 0;
        private int movesUsed = 0;
        private float timeSpent = 0f;
        private int starsEarned = 0;
        private int currentLevel = 1;
        private LevelDifficulty currentDifficulty = LevelDifficulty.Easy;
        
        public System.Action OnNextLevel;
        public System.Action OnRetry;
        public System.Action OnMenu;
        
        private void Start()
        {
            // Настраиваем кнопки
            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(() => OnNextLevel?.Invoke());
                
            if (retryButton != null)
                retryButton.onClick.AddListener(() => OnRetry?.Invoke());
                
            if (menuButton != null)
                menuButton.onClick.AddListener(() => OnMenu?.Invoke());
            
            // Скрываем экран по умолчанию
            if (screenPanel != null)
                screenPanel.SetActive(false);
        }
        
        /// <summary>
        /// Показывает экран завершения уровня
        /// </summary>
        public void ShowLevelCompleted(int level, LevelDifficulty difficulty, int energy, int moves, float time, int stars)
        {
            isLevelCompleted = true;
            currentLevel = level;
            currentDifficulty = difficulty;
            energyUsed = energy;
            movesUsed = moves;
            timeSpent = time;
            starsEarned = stars;
            
            UpdateUI();
            ShowScreen();
        }
        
        /// <summary>
        /// Показывает экран поражения
        /// </summary>
        public void ShowLevelFailed(int level, LevelDifficulty difficulty, int energy, int moves, float time)
        {
            isLevelCompleted = false;
            currentLevel = level;
            currentDifficulty = difficulty;
            energyUsed = energy;
            movesUsed = moves;
            timeSpent = time;
            starsEarned = 0;
            
            UpdateUI();
            ShowScreen();
        }
        
        private void UpdateUI()
        {
            // Обновляем основной текст
            if (resultText != null)
            {
                if (isLevelCompleted)
                {
                    resultText.text = "Уровень пройден!";
                    resultText.color = successColor;
                }
                else
                {
                    resultText.text = "Уровень не пройден";
                    resultText.color = failureColor;
                }
            }
            
            // Обновляем статистику
            if (energyText != null)
                energyText.text = $"Энергия: {energyUsed}";
                
            if (movesText != null)
                movesText.text = $"Ходы: {movesUsed}";
                
            if (timeText != null)
                timeText.text = $"Время: {timeSpent:F1}с";
            
            // Обновляем звезды
            UpdateStars();
            
            // Обновляем состояние кнопок
            if (nextLevelButton != null)
                nextLevelButton.gameObject.SetActive(isLevelCompleted);
                
            if (retryButton != null)
                retryButton.gameObject.SetActive(!isLevelCompleted);
        }
        
        private void UpdateStars()
        {
            if (starsText != null)
            {
                if (isLevelCompleted)
                {
                    starsText.text = $"Звезды: {starsEarned}/3";
                }
                else
                {
                    starsText.text = "Звезды: 0/3";
                }
            }
            
            // Обновляем изображения звезд
            if (starImages != null)
            {
                for (int i = 0; i < starImages.Length; i++)
                {
                    if (starImages[i] != null)
                    {
                        starImages[i].gameObject.SetActive(i < starsEarned);
                    }
                }
            }
        }
        
        private void ShowScreen()
        {
            if (screenPanel != null)
                screenPanel.SetActive(true);
        }
        
        public void HideScreen()
        {
            if (screenPanel != null)
                screenPanel.SetActive(false);
        }
    }
}
