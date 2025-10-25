using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LabyrinthMover.UI
{
    /// <summary>
    /// Система обучения согласно ТЗ (3-4 шага)
    /// </summary>
    public class TutorialController : MonoBehaviour
    {
        [Header("UI элементы")]
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private Text tutorialText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;
        
        [Header("Настройки")]
        [SerializeField] private float messageDuration = 3f;
        [SerializeField] private bool autoAdvance = true;
        
        private int currentStep = 0;
        private bool isTutorialActive = false;
        
        private readonly List<TutorialStep> tutorialSteps = new List<TutorialStep>
        {
            new TutorialStep
            {
                Title = "Добро пожаловать!",
                Message = "Добро пожаловать в A→B: Переставь и Дойди!\n\n" +
                         "Ваша цель: дойти от точки A (зеленая) до точки B (красная), " +
                         "перемещая синие блоки.",
                Action = TutorialAction.None
            },
            new TutorialStep
            {
                Title = "Двигай блоки",
                Message = "Нажмите и удерживайте на синем блоке, затем проведите пальцем " +
                         "в нужном направлении. Блок будет двигаться до упора.\n\n" +
                         "Энергия тратится за каждый шаг блока!",
                Action = TutorialAction.HighlightBlocks
            },
            new TutorialStep
            {
                Title = "Объединяй блоки",
                Message = "Когда блоки сталкиваются, они объединяются в один!\n\n" +
                         "Это может помочь создать более длинные пути или наоборот - " +
                         "создать препятствия.",
                Action = TutorialAction.HighlightMerging
            },
            new TutorialStep
            {
                Title = "Экономь энергию",
                Message = "Подходите ближе к блокам перед их перемещением!\n\n" +
                         "Стоимость зависит от расстояния до персонажа. " +
                         "Чем ближе вы к блоку, тем дешевле его двигать.",
                Action = TutorialAction.HighlightDistance
            },
            new TutorialStep
            {
                Title = "Достигни цели",
                Message = "Теперь попробуйте дойти до красной точки B!\n\n" +
                         "Используйте кнопку 'Undo' для отмены ходов, " +
                         "если что-то пошло не так.",
                Action = TutorialAction.HighlightGoal
            }
        };
        
        public static TutorialController Instance { get; private set; }
        
        public event Action TutorialCompleted;
        public event Action TutorialSkipped;
        
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
                return;
            }
            
            SetupUI();
        }
        
        private void SetupUI()
        {
            if (tutorialPanel == null)
            {
                CreateTutorialUI();
            }
            
            if (nextButton != null)
            {
                nextButton.onClick.AddListener(NextStep);
            }
            
            if (skipButton != null)
            {
                skipButton.onClick.AddListener(SkipTutorial);
            }
            
            HideTutorial();
        }
        
        private void CreateTutorialUI()
        {
            // Создаем панель обучения
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("TutorialCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000; // Поверх всего
            }
            
            tutorialPanel = new GameObject("TutorialPanel");
            tutorialPanel.transform.SetParent(canvas.transform, false);
            
            var panelRect = tutorialPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            var panelImage = tutorialPanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);
            
            // Создаем контент
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(tutorialPanel.transform, false);
            
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.1f, 0.3f);
            contentRect.anchorMax = new Vector2(0.9f, 0.7f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            
            var contentImage = contentGO.AddComponent<Image>();
            contentImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            
            // Создаем текст
            var textGO = new GameObject("TutorialText");
            textGO.transform.SetParent(contentGO.transform, false);
            
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.05f, 0.2f);
            textRect.anchorMax = new Vector2(0.95f, 0.8f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            tutorialText = textGO.AddComponent<Text>();
            tutorialText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tutorialText.fontSize = 24;
            tutorialText.color = Color.white;
            tutorialText.alignment = TextAnchor.MiddleCenter;
            
            // Создаем кнопки
            CreateButton("NextButton", "Далее", new Vector2(0.7f, 0.1f), new Vector2(0.3f, 0.1f));
            CreateButton("SkipButton", "Пропустить", new Vector2(0.3f, 0.1f), new Vector2(0.1f, 0.1f));
        }
        
        private void CreateButton(string name, string text, Vector2 anchorMin, Vector2 size)
        {
            var buttonGO = new GameObject(name);
            buttonGO.transform.SetParent(tutorialPanel.transform, false);
            
            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = anchorMin;
            buttonRect.anchorMax = anchorMin + size;
            buttonRect.offsetMin = Vector2.zero;
            buttonRect.offsetMax = Vector2.zero;
            
            var buttonImage = buttonGO.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 0.9f);
            
            var button = buttonGO.AddComponent<Button>();
            
            if (name == "NextButton")
                nextButton = button;
            else if (name == "SkipButton")
                skipButton = button;
            
            // Текст кнопки
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            var buttonText = textGO.AddComponent<Text>();
            buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonText.text = text;
            buttonText.fontSize = 20;
            buttonText.color = Color.white;
            buttonText.alignment = TextAnchor.MiddleCenter;
        }
        
        public void StartTutorial()
        {
            if (isTutorialActive)
                return;
                
            isTutorialActive = true;
            currentStep = 0;
            ShowTutorial();
            ShowCurrentStep();
        }
        
        private void ShowTutorial()
        {
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(true);
            }
        }
        
        private void HideTutorial()
        {
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }
        }
        
        private void ShowCurrentStep()
        {
            if (currentStep >= tutorialSteps.Count)
            {
                CompleteTutorial();
                return;
            }
            
            var step = tutorialSteps[currentStep];
            
            if (tutorialText != null)
            {
                tutorialText.text = $"<b>{step.Title}</b>\n\n{step.Message}";
            }
            
            // Выполняем действие шага
            ExecuteStepAction(step.Action);
            
            // Автоматическое продвижение
            if (autoAdvance && step.Action == TutorialAction.None)
            {
                StartCoroutine(AutoAdvanceCoroutine());
            }
        }
        
        private IEnumerator AutoAdvanceCoroutine()
        {
            yield return new WaitForSeconds(messageDuration);
            NextStep();
        }
        
        private void ExecuteStepAction(TutorialAction action)
        {
            switch (action)
            {
                case TutorialAction.HighlightBlocks:
                    // Подсвечиваем блоки
                    HighlightMovableBlocks();
                    break;
                case TutorialAction.HighlightMerging:
                    // Подсвечиваем возможность объединения
                    HighlightMerging();
                    break;
                case TutorialAction.HighlightDistance:
                    // Подсвечиваем важность расстояния
                    HighlightDistance();
                    break;
                case TutorialAction.HighlightGoal:
                    // Подсвечиваем цель
                    HighlightGoal();
                    break;
            }
        }
        
        private void HighlightMovableBlocks()
        {
            var blocks = FindObjectsByType<LabyrinthMover.Gameplay.MovableBlock>(FindObjectsSortMode.None);
            foreach (var block in blocks)
            {
                var renderer = block.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    StartCoroutine(HighlightCoroutine(renderer, Color.cyan, 2f));
                }
            }
        }
        
        private void HighlightMerging()
        {
            // Подсвечиваем возможность объединения блоков
            Debug.Log("Tutorial: Highlighting block merging");
        }
        
        private void HighlightDistance()
        {
            // Подсвечиваем важность расстояния
            Debug.Log("Tutorial: Highlighting distance importance");
        }
        
        private void HighlightGoal()
        {
            var goalMarker = GameObject.Find("MarkerGoal");
            if (goalMarker != null)
            {
                var renderer = goalMarker.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    StartCoroutine(HighlightCoroutine(renderer, Color.yellow, 2f));
                }
            }
        }
        
        private IEnumerator HighlightCoroutine(SpriteRenderer renderer, Color highlightColor, float duration)
        {
            var originalColor = renderer.color;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float t = Mathf.PingPong(elapsed * 2f, 1f);
                renderer.color = Color.Lerp(originalColor, highlightColor, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            renderer.color = originalColor;
        }
        
        public void NextStep()
        {
            currentStep++;
            ShowCurrentStep();
        }
        
        public void SkipTutorial()
        {
            CompleteTutorial();
            TutorialSkipped?.Invoke();
        }
        
        private void CompleteTutorial()
        {
            isTutorialActive = false;
            HideTutorial();
            TutorialCompleted?.Invoke();
        }
        
        public bool IsTutorialActive => isTutorialActive;
    }
    
    [Serializable]
    public class TutorialStep
    {
        public string Title;
        public string Message;
        public TutorialAction Action;
    }
    
    public enum TutorialAction
    {
        None,
        HighlightBlocks,
        HighlightMerging,
        HighlightDistance,
        HighlightGoal
    }
}
