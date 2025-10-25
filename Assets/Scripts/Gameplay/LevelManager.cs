using System.Collections.Generic;
using LabyrinthMover.Core;
using LabyrinthMover.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LabyrinthMover.Gameplay
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField]
        private GridConfig gridConfig;

        [SerializeField]
        private PlayerController characterPrefab;

        [SerializeField]
        private MoveExecutor moveExecutor;

        [SerializeField]
        private HudController hud;

        [SerializeField]
        private InputRouter inputRouter;

        private PlayerController characterInstance;
        private GridModel gridModel;
        private Vector2Int goalCell;

        public event System.Action OnLevelCompleted;
        public event System.Action OnLevelFailed;
        
        private int currentLevelNumber;
        private LevelDifficulty currentDifficulty;

        private void Awake()
        {
            gridConfig = gridConfig ?? FindFirstObjectByType<GridConfig>();
            moveExecutor = moveExecutor ?? FindFirstObjectByType<MoveExecutor>();
            hud = hud ?? FindFirstObjectByType<HudController>();
            inputRouter = inputRouter ?? FindFirstObjectByType<InputRouter>();
        }

        private void Start()
        {
            if (gridConfig == null)
            {
                Debug.LogError("GridConfig not found in scene");
                return;
            }

            BuildGridModel();
            SpawnCharacter();
            SetupGoal();

            moveExecutor.Init(gridModel, characterInstance, gridConfig.Emax, gridConfig.Ksize, gridConfig.Kdist);
            moveExecutor.EnergyChanged += OnEnergyChanged;
            OnEnergyChanged(moveExecutor.EnergyRemaining, moveExecutor.EnergyMax);

            if (inputRouter != null)
            {
                inputRouter.Init(gridModel, moveExecutor, characterInstance, hud, goalCell, OnGoalReached, ResetLevel);
            }

            if (hud != null)
            {
                hud.ResetPreview();
            }
            
            // Настраиваем камеру
            SetupCamera();
            
            // Запускаем обучение, если это первый уровень
            StartTutorialIfNeeded();
        }
        
        private void SetupCamera()
        {
            var cameraController = FindFirstObjectByType<LabyrinthMover.UI.CameraController>();
            if (cameraController != null)
            {
                cameraController.CenterOnGrid(gridConfig.Width, gridConfig.Height);
            }
        }
        
        private void StartTutorialIfNeeded()
        {
            // Проверяем, нужно ли показать обучение
            var tutorialController = LabyrinthMover.UI.TutorialController.Instance;
            if (tutorialController != null && !tutorialController.IsTutorialActive)
            {
                // Можно добавить проверку на первый запуск или определенные условия
                tutorialController.StartTutorial();
            }
        }

        private void OnDestroy()
        {
            if (moveExecutor != null)
            {
                moveExecutor.EnergyChanged -= OnEnergyChanged;
            }
        }

        private void BuildGridModel()
        {
            // Проверяем, есть ли уже GridModel с данными (например, от TestLevelGenerator)
            if (TestLevelGenerator.LastGeneratedGridModel != null)
            {
                Debug.Log("Найден существующий GridModel от TestLevelGenerator, используем его");
                gridModel = TestLevelGenerator.LastGeneratedGridModel;
                return;
            }
            
            // Создаем новый GridModel только если его нет
            gridModel = new GridModel(gridConfig.Width, gridConfig.Height);

            var staticTiles = FindObjectsByType<StaticTile>(FindObjectsSortMode.None);
            foreach (var tile in staticTiles)
            {
                // Проверяем, что объект не был уничтожен
                if (tile == null || tile.gameObject == null)
                {
                    Debug.LogWarning("Найден уничтоженный StaticTile, пропускаем");
                    continue;
                }
                
                Vector2Int pos = RoundToCell(tile.transform.position);
                // Проверяем границы перед размещением
                if (gridModel.InBounds(pos.x, pos.y))
                {
                    gridModel.PlaceStatic(pos);
                }
                else
                {
                    Debug.LogWarning($"Статический тайл {tile.name} находится за границами сетки: {pos}");
                }
            }

            var movableBlocks = FindObjectsByType<MovableBlock>(FindObjectsSortMode.None);
            foreach (var block in movableBlocks)
            {
                // Проверяем, что объект не был уничтожен
                if (block == null || block.gameObject == null)
                {
                    Debug.LogWarning("Найден уничтоженный MovableBlock, пропускаем");
                    continue;
                }
                
                Vector2Int anchor = block.Anchor;
                Vector2Int size = block.Size;
                var rect = new RectInt(anchor.x, anchor.y, size.x, size.y);
                
                // Проверяем, что блок помещается в сетку
                if (rect.xMin >= 0 && rect.yMin >= 0 && rect.xMax <= gridModel.Width && rect.yMax <= gridModel.Height)
                {
                    var blockRt = new BlockRt(block.GetInstanceID(), rect);
                    gridModel.AddBlock(blockRt);
                }
                else
                {
                    Debug.LogWarning($"Подвижный блок {block.name} выходит за границы сетки: {rect}");
                }
            }
        }

        private void SpawnCharacter()
        {
            Vector2Int startCell = FindMarker("MarkerStart");

            if (characterPrefab != null)
            {
                characterInstance = Instantiate(characterPrefab, new Vector3(startCell.x, startCell.y, 0f), Quaternion.identity);
                characterInstance.SetGridPosition(startCell);
                
                // Настраиваем камеру для следования за персонажем
                var cameraController = FindFirstObjectByType<CameraController>();
                if (cameraController != null)
                {
                    cameraController.FollowTarget(characterInstance.transform);
                }
            }
            else
            {
                characterInstance = FindFirstObjectByType<PlayerController>();
                if (characterInstance == null)
                {
                    Debug.LogWarning("Character prefab is not assigned; creating runtime character instance");
                    var go = new GameObject("RuntimeCharacter");
                    go.transform.position = new Vector3(startCell.x, startCell.y, 0f);
                    characterInstance = go.AddComponent<PlayerController>();
                    characterInstance.SetGridPosition(startCell);
                    
                    // Настраиваем камеру для следования за персонажем
                    var cameraController = FindFirstObjectByType<CameraController>();
                    if (cameraController != null)
                    {
                        cameraController.FollowTarget(characterInstance.transform);
                    }
                }
            }

            if (characterInstance == null)
            {
                Debug.LogError("Unable to create or locate a character instance");
                return;
            }

            characterInstance.transform.position = new Vector3(startCell.x, startCell.y, characterInstance.transform.position.z);
            characterInstance.SetGridPosition(startCell);
        }

        private void SetupGoal()
        {
            goalCell = FindMarker("MarkerGoal");
        }

        private Vector2Int FindMarker(string markerName)
        {
            var marker = GameObject.Find(markerName);
            if (marker == null)
            {
                Debug.LogWarning($"Marker {markerName} not found; defaulting to (0,0)");
                return Vector2Int.zero;
            }

            return RoundToCell(marker.transform.position);
        }

        private void OnGoalReached()
        {
            Debug.Log("Goal reached! Level complete.");
            OnLevelCompleted?.Invoke();
        }

        private void OnEnergyChanged(float remaining, float max)
        {
            hud?.SetEnergy(remaining, max);
            if (remaining < 0f)
            {
                Debug.LogWarning("Energy depleted. Level failed.");
                OnLevelFailed?.Invoke();
            }
        }

        public GridModel GetGridModel()
        {
            return gridModel;
        }
        
        /// <summary>
        /// Инициализирует уровень с заданными параметрами
        /// </summary>
        public void InitializeLevel(int levelNumber, LevelDifficulty difficulty)
        {
            currentLevelNumber = levelNumber;
            currentDifficulty = difficulty;
            
            Debug.Log($"Инициализация уровня {levelNumber} сложности {difficulty}");
            
            // Здесь можно добавить логику генерации уровня или загрузки
            // Пока что просто логируем
        }

        private void ResetLevel()
        {
            Scene current = SceneManager.GetActiveScene();
            SceneManager.LoadScene(current.buildIndex);
        }

        private static Vector2Int RoundToCell(Vector3 position)
        {
            // Получаем GridConfig для масштабирования
            var gridConfig = Object.FindFirstObjectByType<GridConfig>();
            float scale = gridConfig != null ? gridConfig.TileSize / 100f : 1f;
            
            // Конвертируем мировые координаты обратно в координаты сетки
            int cellX = Mathf.RoundToInt(position.x / scale);
            int cellY = Mathf.RoundToInt(position.y / scale);
            
            return new Vector2Int(cellX, cellY);
        }
    }
}
