using UnityEngine;
using UnityEngine.UI;
using LabyrinthMover.Core;

namespace LabyrinthMover.UI
{
    /// <summary>
    /// Компонент для выбора уровня в меню
    /// </summary>
    public class LevelSelector : MonoBehaviour
    {
        [Header("UI Элементы")]
        [SerializeField] private Button levelButton;
        [SerializeField] private Text levelNumberText;
        [SerializeField] private Text difficultyText;
        [SerializeField] private Image[] starImages;
        [SerializeField] private Image lockImage;
        [SerializeField] private Image completedImage;
        
        [Header("Настройки")]
        [SerializeField] private Color lockedColor = Color.gray;
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private Color completedColor = Color.green;
        
        private int levelNumber;
        private LevelDifficulty difficulty;
        private bool isUnlocked;
        private LevelProgressManager.LevelProgress progress;
        
        public System.Action<int, LevelDifficulty> OnLevelSelected;
        
        /// <summary>
        /// Инициализирует селектор уровня
        /// </summary>
        public void Initialize(int level, LevelDifficulty diff, bool unlocked, LevelProgressManager.LevelProgress levelProgress)
        {
            levelNumber = level;
            difficulty = diff;
            isUnlocked = unlocked;
            progress = levelProgress;
            
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            // Обновляем текст
            if (levelNumberText != null)
                levelNumberText.text = levelNumber.ToString();
                
            if (difficultyText != null)
                difficultyText.text = GetDifficultyText(difficulty);
            
            // Обновляем состояние кнопки
            if (levelButton != null)
            {
                levelButton.interactable = isUnlocked;
                
                // Обновляем цвет кнопки
                var colors = levelButton.colors;
                if (!isUnlocked)
                {
                    colors.normalColor = lockedColor;
                }
                else if (progress != null && progress.IsCompleted)
                {
                    colors.normalColor = completedColor;
                }
                else
                {
                    colors.normalColor = unlockedColor;
                }
                levelButton.colors = colors;
            }
            
            // Обновляем изображения
            if (lockImage != null)
                lockImage.gameObject.SetActive(!isUnlocked);
                
            if (completedImage != null)
                completedImage.gameObject.SetActive(progress != null && progress.IsCompleted);
            
            // Обновляем звезды
            UpdateStars();
        }
        
        private void UpdateStars()
        {
            if (starImages == null || starImages.Length == 0) return;
            
            int starsToShow = 0;
            if (progress != null && progress.IsCompleted)
            {
                starsToShow = progress.StarsEarned;
            }
            
            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    starImages[i].gameObject.SetActive(i < starsToShow);
                }
            }
        }
        
        private string GetDifficultyText(LevelDifficulty diff)
        {
            switch (diff)
            {
                case LevelDifficulty.Easy: return "Легкий";
                case LevelDifficulty.Medium: return "Средний";
                case LevelDifficulty.Hard: return "Сложный";
                default: return "Неизвестно";
            }
        }
        
        public void OnButtonClicked()
        {
            if (isUnlocked && OnLevelSelected != null)
            {
                OnLevelSelected.Invoke(levelNumber, difficulty);
            }
        }
    }
}
