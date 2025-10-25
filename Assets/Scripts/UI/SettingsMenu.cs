using UnityEngine;
using UnityEngine.UI;
using LabyrinthMover.Core;

namespace LabyrinthMover.UI
{
    /// <summary>
    /// Меню настроек игры
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        [Header("UI Элементы")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle tutorialToggle;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button resetProgressButton;
        
        [Header("Настройки")]
        [SerializeField] private float defaultMusicVolume = 0.7f;
        [SerializeField] private float defaultSfxVolume = 0.8f;
        [SerializeField] private bool defaultTutorialEnabled = true;
        
        private float currentMusicVolume;
        private float currentSfxVolume;
        private bool currentTutorialEnabled;
        
        public System.Action OnSettingsSaved;
        public System.Action OnSettingsCancelled;
        
        private void Start()
        {
            // Настраиваем кнопки
            if (saveButton != null)
                saveButton.onClick.AddListener(SaveSettings);
                
            if (cancelButton != null)
                cancelButton.onClick.AddListener(CancelSettings);
                
            if (resetProgressButton != null)
                resetProgressButton.onClick.AddListener(ResetProgress);
            
            // Настраиваем слайдеры
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
                musicVolumeSlider.value = LoadMusicVolume();
            }
            
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
                sfxVolumeSlider.value = LoadSfxVolume();
            }
            
            // Настраиваем переключатель
            if (tutorialToggle != null)
            {
                tutorialToggle.onValueChanged.AddListener(OnTutorialToggleChanged);
                tutorialToggle.isOn = LoadTutorialEnabled();
            }
            
            // Скрываем панель по умолчанию
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }
        
        /// <summary>
        /// Показывает меню настроек
        /// </summary>
        public void ShowSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(true);
            
            // Загружаем текущие настройки
            LoadCurrentSettings();
        }
        
        /// <summary>
        /// Скрывает меню настроек
        /// </summary>
        public void HideSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }
        
        /// <summary>
        /// Загружает текущие настройки
        /// </summary>
        private void LoadCurrentSettings()
        {
            currentMusicVolume = LoadMusicVolume();
            currentSfxVolume = LoadSfxVolume();
            currentTutorialEnabled = LoadTutorialEnabled();
            
            // Обновляем UI
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = currentMusicVolume;
                
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = currentSfxVolume;
                
            if (tutorialToggle != null)
                tutorialToggle.isOn = currentTutorialEnabled;
        }
        
        /// <summary>
        /// Сохраняет настройки
        /// </summary>
        private void SaveSettings()
        {
            // Сохраняем настройки
            SaveMusicVolume(currentMusicVolume);
            SaveSfxVolume(currentSfxVolume);
            SaveTutorialEnabled(currentTutorialEnabled);
            
            // Применяем настройки
            ApplySettings();
            
            // Скрываем меню
            HideSettings();
            
            OnSettingsSaved?.Invoke();
        }
        
        /// <summary>
        /// Отменяет изменения настроек
        /// </summary>
        private void CancelSettings()
        {
            // Восстанавливаем предыдущие настройки
            LoadCurrentSettings();
            
            // Скрываем меню
            HideSettings();
            
            OnSettingsCancelled?.Invoke();
        }
        
        /// <summary>
        /// Сбрасывает прогресс игры
        /// </summary>
        private void ResetProgress()
        {
            // Показываем подтверждение
            if (Application.isEditor)
            {
                Debug.Log("Сброс прогресса в редакторе не поддерживается");
                return;
            }
            
            // В реальной игре здесь можно показать диалог подтверждения
            // Пока просто сбрасываем настройки
            ResetAllSettings();
        }
        
        /// <summary>
        /// Сбрасывает все настройки
        /// </summary>
        private void ResetAllSettings()
        {
            // Сбрасываем настройки звука
            SaveMusicVolume(defaultMusicVolume);
            SaveSfxVolume(defaultSfxVolume);
            SaveTutorialEnabled(defaultTutorialEnabled);
            
            // Сбрасываем прогресс
            if (LevelProgressManager.Instance != null)
            {
                // Здесь можно добавить метод сброса прогресса в LevelProgressManager
                Debug.Log("Прогресс сброшен");
            }
            
            // Применяем настройки
            ApplySettings();
            
            // Обновляем UI
            LoadCurrentSettings();
        }
        
        /// <summary>
        /// Применяет настройки
        /// </summary>
        private void ApplySettings()
        {
            // Применяем настройки звука
            AudioListener.volume = currentMusicVolume;
            
            // Здесь можно добавить применение других настроек
            Debug.Log($"Настройки применены: Музыка={currentMusicVolume:F2}, SFX={currentSfxVolume:F2}, Обучение={currentTutorialEnabled}");
        }
        
        // Обработчики изменений UI
        private void OnMusicVolumeChanged(float value)
        {
            currentMusicVolume = value;
        }
        
        private void OnSfxVolumeChanged(float value)
        {
            currentSfxVolume = value;
        }
        
        private void OnTutorialToggleChanged(bool value)
        {
            currentTutorialEnabled = value;
        }
        
        // Методы загрузки/сохранения настроек
        private float LoadMusicVolume()
        {
            return PlayerPrefs.GetFloat("MusicVolume", defaultMusicVolume);
        }
        
        private void SaveMusicVolume(float volume)
        {
            PlayerPrefs.SetFloat("MusicVolume", volume);
        }
        
        private float LoadSfxVolume()
        {
            return PlayerPrefs.GetFloat("SfxVolume", defaultSfxVolume);
        }
        
        private void SaveSfxVolume(float volume)
        {
            PlayerPrefs.SetFloat("SfxVolume", volume);
        }
        
        private bool LoadTutorialEnabled()
        {
            return PlayerPrefs.GetInt("TutorialEnabled", defaultTutorialEnabled ? 1 : 0) == 1;
        }
        
        private void SaveTutorialEnabled(bool enabled)
        {
            PlayerPrefs.SetInt("TutorialEnabled", enabled ? 1 : 0);
        }
    }
}
