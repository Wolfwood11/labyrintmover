using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LabyrinthMover.UI
{
    public class BootstrapMenu : MonoBehaviour
    {
        [SerializeField]
        private string levelSceneName = "Level_001";

        [SerializeField]
        private Button playButton;

        private void Awake()
        {
            EnsureUi();
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayClicked);
            }
        }

        private void OnDestroy()
        {
            if (playButton != null)
            {
                playButton.onClick.RemoveListener(OnPlayClicked);
            }
        }

        private void OnPlayClicked()
        {
            if (!string.IsNullOrEmpty(levelSceneName))
            {
                SceneManager.LoadScene(levelSceneName);
            }
        }

        private void EnsureUi()
        {
            if (playButton != null)
            {
                return;
            }

            Canvas canvas = GetComponent<Canvas>();
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

            if (FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var buttonGo = new GameObject("PlayButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(transform, false);

            var rect = buttonGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(240f, 60f);
            rect.anchoredPosition = Vector2.zero;

            var image = buttonGo.GetComponent<Image>();
            image.color = new Color(0.25f, 0.25f, 0.25f, 0.85f);

            playButton = buttonGo.GetComponent<Button>();

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGo.transform.SetParent(buttonGo.transform, false);

            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textGo.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = "Play";
            text.fontSize = 32;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
        }
    }
}
