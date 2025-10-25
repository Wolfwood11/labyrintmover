using UnityEngine;
using UnityEngine.UI;
using LabyrinthMover.Core;

namespace LabyrinthMover.UI
{
    /// <summary>
    /// Менеджер обучения для новых игроков
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        [Header("UI Элементы")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private Text tutorialText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Image[] highlightImages;
        
        [Header("Настройки")]
        [SerializeField] private float highlightDuration = 2f;
        [SerializeField] private Color highlightColor = Color.yellow;
        
        private int currentStep = 0;
        private bool isTutorialActive = false;
        
        private TutorialStep[] tutorialSteps = new TutorialStep[]
        {
            new TutorialStep
            {
                Text = "Добро пожаловать в Labyrinth Mover!",
                HighlightObjects = new string[0]
            },
            new TutorialStep
            {
                Text = "Ваша цель - добраться от зеленой точки (старт) до красной точки (финиш)",
                HighlightObjects = new string[] { "StartMarker", "GoalMarker" }
            },
            new TutorialStep
            {
                Text = "Перетаскивайте синие блоки, чтобы построить путь",
                HighlightObjects = new string[] { "MovableBlock" }
            },
            new TutorialStep
            {
                Text = "Каждое движение блока тратит энергию. Следите за счетчиком энергии!",
                HighlightObjects = new string[] { "EnergyText" }
            },
            new TutorialStep
            {
                Text = "Старайтесь использовать минимальное количество энергии для получения больше звезд",
                HighlightObjects = new string[] { "StarsText" }
            },
            new TutorialStep
            {
                Text = "Готовы начать? Удачи!",
                HighlightObjects = new string[0]
            }
        };
        
        public System.Action OnTutorialCompleted;
        
        private void Start()
        {
            // Настраиваем кнопки
            if (nextButton != null)
                nextButton.onClick.AddListener(NextStep);
                
            if (skipButton != null)
                skipButton.onClick.AddListener(SkipTutorial);
            
            // Скрываем панель по умолчанию
            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);
        }
        
        /// <summary>
        /// Запускает обучение
        /// </summary>
        public void StartTutorial()
        {
            if (isTutorialActive) return;
            
            isTutorialActive = true;
            currentStep = 0;
            
            if (tutorialPanel != null)
                tutorialPanel.SetActive(true);
            
            ShowCurrentStep();
        }
        
        /// <summary>
        /// Показывает текущий шаг обучения
        /// </summary>
        private void ShowCurrentStep()
        {
            if (currentStep >= tutorialSteps.Length)
            {
                CompleteTutorial();
                return;
            }
            
            var step = tutorialSteps[currentStep];
            
            // Обновляем текст
            if (tutorialText != null)
                tutorialText.text = step.Text;
            
            // Подсвечиваем объекты
            HighlightObjects(step.HighlightObjects);
            
            // Обновляем состояние кнопок
            if (nextButton != null)
                nextButton.gameObject.SetActive(currentStep < tutorialSteps.Length - 1);
                
            if (skipButton != null)
                skipButton.gameObject.SetActive(true);
        }
        
        /// <summary>
        /// Подсвечивает указанные объекты
        /// </summary>
        private void HighlightObjects(string[] objectNames)
        {
            // Сначала убираем подсветку со всех объектов
            foreach (var image in highlightImages)
            {
                if (image != null)
                    image.gameObject.SetActive(false);
            }
            
            // Подсвечиваем нужные объекты
            for (int i = 0; i < objectNames.Length && i < highlightImages.Length; i++)
            {
                if (highlightImages[i] != null)
                {
                    highlightImages[i].gameObject.SetActive(true);
                    highlightImages[i].color = highlightColor;
                    
                    // Используем highlightDuration для анимации подсветки
                    StartCoroutine(AnimateHighlight(highlightImages[i], highlightDuration));
                }
            }
        }
        
        /// <summary>
        /// Анимирует подсветку объекта
        /// </summary>
        private System.Collections.IEnumerator AnimateHighlight(Image image, float duration)
        {
            float elapsed = 0f;
            Color originalColor = image.color;
            
            while (elapsed < duration)
            {
                float alpha = Mathf.PingPong(elapsed * 2f, 1f);
                image.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            image.color = originalColor;
        }
        
        /// <summary>
        /// Переходит к следующему шагу
        /// </summary>
        private void NextStep()
        {
            currentStep++;
            ShowCurrentStep();
        }
        
        /// <summary>
        /// Пропускает обучение
        /// </summary>
        private void SkipTutorial()
        {
            CompleteTutorial();
        }
        
        /// <summary>
        /// Завершает обучение
        /// </summary>
        private void CompleteTutorial()
        {
            isTutorialActive = false;
            
            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);
            
            // Убираем подсветку
            foreach (var image in highlightImages)
            {
                if (image != null)
                    image.gameObject.SetActive(false);
            }
            
            // Отмечаем обучение как завершенное
            if (LevelProgressManager.Instance != null)
                LevelProgressManager.Instance.CompleteTutorial();
            
            OnTutorialCompleted?.Invoke();
        }
        
        /// <summary>
        /// Проверяет, нужно ли показать обучение
        /// </summary>
        public bool ShouldShowTutorial()
        {
            return LevelProgressManager.Instance != null && 
                   LevelProgressManager.Instance.ShouldShowTutorial();
        }
        
        [System.Serializable]
        private class TutorialStep
        {
            public string Text;
            public string[] HighlightObjects;
        }
    }
}
