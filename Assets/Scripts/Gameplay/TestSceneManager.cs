using UnityEngine;
using LabyrinthMover.Core;
using LabyrinthMover.Gameplay;
using LabyrinthMover.UI;

namespace LabyrinthMover.Gameplay
{
    /// <summary>
    /// Менеджер тестовой сцены для уровня 20x20
    /// </summary>
    public class TestSceneManager : MonoBehaviour
    {
        [Header("Компоненты")]
        [SerializeField] private TestLevelGenerator testLevelGenerator;
        [SerializeField] private LevelManager levelManager;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private TutorialController tutorialController;
        
        [Header("Настройки")]
        [SerializeField] private bool autoGenerateLevel = true;
        [SerializeField] private bool showTutorial = false;
        
        private void Start()
        {
            SetupTestScene();
        }
        
        private void SetupTestScene()
        {
            Debug.Log("🎮 Настройка тестовой сцены для уровня 20x20...");
            
            // Настраиваем камеру для большого уровня
            SetupCamera();
            
            // Генерируем тестовый уровень
            if (autoGenerateLevel)
            {
                GenerateTestLevel();
            }
            
            // Показываем обучение, если нужно
            if (showTutorial)
            {
                ShowTutorial();
            }
        }
        
        private void SetupCamera()
        {
            if (cameraController == null)
            {
                // Создаем камеру с контроллером
                var cameraGO = new GameObject("Main Camera");
                cameraGO.AddComponent<Camera>();
                cameraController = cameraGO.AddComponent<CameraController>();
                
                // Настраиваем камеру для ортогонального вида
                var camera = cameraGO.GetComponent<Camera>();
                camera.orthographic = true;
                camera.orthographicSize = 12f; // Подходящий размер для обзора 20x20
                camera.transform.position = new Vector3(10f, 10f, -10f); // Центр уровня
            }
        }
        
        private void GenerateTestLevel()
        {
            if (testLevelGenerator == null)
            {
                // Создаем генератор тестового уровня
                var generatorGO = new GameObject("TestLevelGenerator");
                testLevelGenerator = generatorGO.AddComponent<TestLevelGenerator>();
            }
            
            // Генерируем уровень средней сложности
            testLevelGenerator.GenerateMediumLevel();
        }
        
        private void ShowTutorial()
        {
            if (tutorialController == null)
            {
                // Создаем контроллер обучения
                var tutorialGO = new GameObject("TutorialController");
                tutorialController = tutorialGO.AddComponent<TutorialController>();
            }
            
            tutorialController.StartTutorial();
        }
        
        [ContextMenu("Перегенерировать уровень")]
        public void RegenerateLevel()
        {
            if (testLevelGenerator != null)
            {
                testLevelGenerator.GenerateTestLevel();
            }
        }
        
        [ContextMenu("Показать обучение")]
        public void ShowTutorialManual()
        {
            ShowTutorial();
        }
        
        [ContextMenu("Сбросить камеру")]
        public void ResetCamera()
        {
            if (cameraController != null)
            {
                cameraController.CenterOnGrid(20, 20);
                cameraController.SetZoom(12f);
            }
        }
        
        private void Update()
        {
            // Горячие клавиши для тестирования
            if (Input.GetKeyDown(KeyCode.R))
            {
                RegenerateLevel();
            }
            
            if (Input.GetKeyDown(KeyCode.T))
            {
                ShowTutorialManual();
            }
            
            if (Input.GetKeyDown(KeyCode.C))
            {
                ResetCamera();
            }
        }
        
        private void OnGUI()
        {
            // Простой GUI для тестирования
            GUILayout.BeginArea(new Rect(10, 10, 200, 150));
            GUILayout.Label("Тестовый уровень 20x20", GUI.skin.box);
            
            if (GUILayout.Button("Перегенерировать (R)"))
            {
                RegenerateLevel();
            }
            
            if (GUILayout.Button("Показать обучение (T)"))
            {
                ShowTutorialManual();
            }
            
            if (GUILayout.Button("Сбросить камеру (C)"))
            {
                ResetCamera();
            }
            
            GUILayout.Space(10);
            GUILayout.Label("Горячие клавиши:");
            GUILayout.Label("R - Перегенерировать");
            GUILayout.Label("T - Обучение");
            GUILayout.Label("C - Сброс камеры");
            
            GUILayout.EndArea();
        }
    }
}



