using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#endif

namespace LabyrinthMover.UI
{
    public class HudController : MonoBehaviour
    {
        [SerializeField]
        private Text energyText;

        [SerializeField]
        private Text previewText;

        [SerializeField]
        private Button undoButton;

        [SerializeField]
        private Button resetButton;

        public event Action UndoRequested;
        public event Action ResetRequested;

        private void Awake()
        {
            EnsureRuntimeUi();

            if (undoButton != null)
            {
                undoButton.onClick.AddListener(() => UndoRequested?.Invoke());
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(() => ResetRequested?.Invoke());
            }
        }

        public void SetEnergy(float remaining, float max)
        {
            if (energyText != null)
            {
                energyText.text = $"Energy: {remaining:F1}/{max:F1}";
            }
        }

        public void ShowPreview(int steps, int d0, int dEnd, float cost)
        {
            if (previewText == null)
            {
                return;
            }

            if (steps > 0)
            {
                string hint = "";
                if (dEnd > d0)
                {
                    hint = "\n💡 Подойди ближе — будет дешевле";
                }
                
                previewText.text = $"🎯 Путь: {steps} шагов\n" +
                                 $"📏 Расстояние: {d0}→{dEnd}\n" +
                                 $"⚡ Стоимость: {cost:F2}{hint}";
            }
            else
            {
                previewText.text = string.Empty;
            }
        }
        
        /// <summary>
        /// Показывает текстовое сообщение в превью
        /// </summary>
        public void ShowPreview(string message)
        {
            if (previewText == null)
            {
                return;
            }

            previewText.text = message;
        }

        public void ResetPreview()
        {
            if (previewText != null)
            {
                previewText.text = string.Empty;
            }
        }
        
        /// <summary>
        /// Получает текущую энергию
        /// </summary>
        public int GetCurrentEnergy()
        {
            // Парсим энергию из текста или возвращаем значение по умолчанию
            if (energyText != null && energyText.text.Contains("/"))
            {
                string[] parts = energyText.text.Split('/');
                if (parts.Length >= 1 && int.TryParse(parts[0].Split(':')[1].Trim(), out int energy))
                {
                    return energy;
                }
            }
            return 50; // Значение по умолчанию
        }
        
        /// <summary>
        /// Получает текущее количество ходов
        /// </summary>
        public int GetCurrentMoves()
        {
            // Пока что возвращаем значение по умолчанию
            // В будущем можно добавить счетчик ходов
            return 0;
        }
        
        /// <summary>
        /// Обновляет энергию
        /// </summary>
        public void UpdateEnergy(int energyCost)
        {
            // Здесь можно добавить логику обновления энергии
            Debug.Log($"Обновление энергии: -{energyCost}");
        }
        
        /// <summary>
        /// Обновляет количество ходов
        /// </summary>
        public void UpdateMoves(int moves)
        {
            // Здесь можно добавить логику обновления ходов
            Debug.Log($"Обновление ходов: +{moves}");
        }

        private void EnsureRuntimeUi()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                es.AddComponent<InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }

            if (energyText == null)
            {
                energyText = CreateText("EnergyText", new Vector2(10f, -10f), new Vector2(0f, 1f));
            }

            if (previewText == null)
            {
                previewText = CreateText("PreviewText", new Vector2(10f, -60f), new Vector2(0f, 1f));
            }

            if (undoButton == null)
            {
                undoButton = CreateButton("UndoButton", "Undo", new Vector2(-220f, 60f), new Vector2(1f, 0f));
            }

            if (resetButton == null)
            {
                resetButton = CreateButton("ResetButton", "Reset", new Vector2(-80f, 60f), new Vector2(1f, 0f));
            }
        }

        private Text CreateText(string name, Vector2 anchoredPosition, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(260f, 40f);

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = string.Empty;
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;

            return text;
        }

        private Button CreateButton(string name, string label, Vector2 anchoredPosition, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(120f, 40f);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.85f);

            var button = go.GetComponent<Button>();

            var text = CreateText(name + "Label", Vector2.zero, new Vector2(0.5f, 0.5f));
            text.transform.SetParent(go.transform, false);
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;

            return button;
        }
    }
}
