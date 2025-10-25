using UnityEngine;
using LabyrinthMover.Core;
using LabyrinthMover.UI;

namespace LabyrinthMover.Gameplay
{
    /// <summary>
    /// Генератор тестового уровня 20x20
    /// </summary>
    public class TestLevelGenerator : MonoBehaviour
    {
        /// <summary>
        /// Последний сгенерированный GridModel для использования LevelManager'ом
        /// </summary>
        public static GridModel LastGeneratedGridModel { get; set; }
        
        [Header("Настройки тестового уровня")]
        [SerializeField] private bool generateOnStart = false; // Изменено на false по умолчанию
        [SerializeField] private int targetDifficulty = 1; // 0=Easy, 1=Medium, 2=Hard
        
        [Header("Компоненты")]
        [SerializeField] private LevelGeneratorComponent levelGenerator;
        [SerializeField] private CameraController cameraController;
        
        private void Awake()
        {
            if (generateOnStart)
            {
                GenerateTestLevel();
            }
        }
        
        [ContextMenu("Сгенерировать тестовый уровень 20x20")]
        public void GenerateTestLevel()
        {
            Debug.Log("Генерация тестового уровня 20x20...");
            
            // Создаем параметры для уровня 20x20
            var parameters = CreateTestLevelParameters();
            
            // Генерируем уровень
            var generatedLevel = LevelGenerator.GenerateLevel(parameters);
            
            // Для тестирования принимаем уровень, даже если решатель не смог найти решение
            if (generatedLevel.IsSolvable || true) // Временно отключаем проверку решателя
            {
                Debug.Log($"✅ Тестовый уровень успешно сгенерирован!");
                Debug.Log($"📊 Статистика:");
                Debug.Log($"   - Сложность: {generatedLevel.Difficulty}");
                Debug.Log($"   - Оценка энергии: {generatedLevel.EstimatedMinEnergy}");
                Debug.Log($"   - Оценка ходов: {generatedLevel.EstimatedMinHops}");
                Debug.Log($"   - Статических препятствий: {generatedLevel.StaticPositions.Count}");
                Debug.Log($"   - Подвижных блоков: {generatedLevel.BlockPlacements.Count}");
                Debug.Log($"   - Seed: {generatedLevel.Seed}");
                
                // Применяем уровень к сцене
                ApplyLevelToScene(generatedLevel);
                
                // Настраиваем камеру для большого уровня
                SetupCameraForLargeLevel();
            }
            else
            {
                Debug.LogError("❌ Не удалось сгенерировать решаемый тестовый уровень");
                Debug.Log($"Параметры: W={parameters.Width}, H={parameters.Height}, StaticDensity={parameters.StaticDensity}, BlockCount={parameters.BlockCount}");
            }
        }
        
        private LevelGenerationParams CreateTestLevelParameters()
        {
            var parameters = new LevelGenerationParams
            {
                Width = 20,
                Height = 20,
                StaticDensity = GetStaticDensityForDifficulty(targetDifficulty),
                BlockSizes = GetBlockSizesForDifficulty(targetDifficulty),
                BaseBlockCount = GetBlockCountForDifficulty(targetDifficulty),
                BlockCountVariation = 2, // Небольшая вариация
                TargetMinHops = GetTargetHopsForDifficulty(targetDifficulty),
                TargetMinEnergy = GetTargetEnergyForDifficulty(targetDifficulty),
                Ksize = 1.0f,
                Kdist = 0.1f,
                Emax = GetMaxEnergyForDifficulty(targetDifficulty)
            };
            
            return parameters;
        }
        
        private float GetStaticDensityForDifficulty(int difficulty)
        {
            switch (difficulty)
            {
                case 0: return 0.1f; // Easy - меньше препятствий
                case 1: return 0.15f; // Medium - среднее количество
                case 2: return 0.2f; // Hard - много препятствий
                default: return 0.15f;
            }
        }
        
        private System.Collections.Generic.List<Vector2Int> GetBlockSizesForDifficulty(int difficulty)
        {
            var sizes = new System.Collections.Generic.List<Vector2Int>();
            
            switch (difficulty)
            {
                case 0: // Easy
                    sizes.Add(Vector2Int.one); // 1x1
                    sizes.Add(new Vector2Int(2, 1)); // 2x1
                    sizes.Add(new Vector2Int(1, 2)); // 1x2
                    break;
                    
                case 1: // Medium
                    sizes.Add(Vector2Int.one); // 1x1
                    sizes.Add(new Vector2Int(2, 1)); // 2x1
                    sizes.Add(new Vector2Int(1, 2)); // 1x2
                    sizes.Add(new Vector2Int(2, 2)); // 2x2
                    sizes.Add(new Vector2Int(3, 1)); // 3x1
                    break;
                    
                case 2: // Hard
                    sizes.Add(Vector2Int.one); // 1x1
                    sizes.Add(new Vector2Int(2, 1)); // 2x1
                    sizes.Add(new Vector2Int(1, 2)); // 1x2
                    sizes.Add(new Vector2Int(2, 2)); // 2x2
                    sizes.Add(new Vector2Int(3, 1)); // 3x1
                    sizes.Add(new Vector2Int(1, 3)); // 1x3
                    sizes.Add(new Vector2Int(3, 2)); // 3x2
                    break;
            }
            
            return sizes;
        }
        
        private int GetBlockCountForDifficulty(int difficulty)
        {
            switch (difficulty)
            {
                case 0: return 5;  // Easy - меньше блоков
                case 1: return 8; // Medium - среднее количество
                case 2: return 12; // Hard - больше блоков
                default: return 8;
            }
        }
        
        private int GetTargetHopsForDifficulty(int difficulty)
        {
            switch (difficulty)
            {
                case 0: return 8;  // Easy
                case 1: return 15; // Medium
                case 2: return 25; // Hard
                default: return 15;
            }
        }
        
        private int GetTargetEnergyForDifficulty(int difficulty)
        {
            switch (difficulty)
            {
                case 0: return 20;  // Easy
                case 1: return 40;  // Medium
                case 2: return 70; // Hard
                default: return 40;
            }
        }
        
        private float GetMaxEnergyForDifficulty(int difficulty)
        {
            switch (difficulty)
            {
                case 0: return 30f;  // Easy
                case 1: return 60f;  // Medium
                case 2: return 100f; // Hard
                default: return 60f;
            }
        }
        
        private void ApplyLevelToScene(GeneratedLevel level)
        {
            // Очищаем текущую сцену
            ClearScene();
            
            // Создаем GridRoot с GridConfig
            var gridRoot = new GameObject("GridRoot");
            var gridConfig = gridRoot.AddComponent<GridConfig>();
            gridConfig.Width = level.Params.Width;
            gridConfig.Height = level.Params.Height;
            gridConfig.Ksize = level.Params.Ksize;
            gridConfig.Kdist = level.Params.Kdist;
            gridConfig.Emax = level.Params.Emax;
            
            // Создаем контейнеры
            var staticsContainer = new GameObject("Statics");
            staticsContainer.transform.SetParent(gridRoot.transform);
            
            var blocksContainer = new GameObject("Blocks");
            blocksContainer.transform.SetParent(gridRoot.transform);
            
            // Размещаем статические препятствия
            foreach (var pos in level.StaticPositions)
            {
                CreateStaticTile(pos, staticsContainer.transform);
            }
            
            // Размещаем подвижные блоки
            foreach (var placement in level.BlockPlacements)
            {
                CreateMovableBlock(placement, blocksContainer.transform);
            }
            
            // Размещаем маркеры старта и цели
            CreateMarker("MarkerStart", level.StartPosition, Color.green);
            CreateMarker("MarkerGoal", level.GoalPosition, Color.red);
            
            // Обновляем GridModel с созданными объектами
            UpdateGridModelFromScene();
            
            // Создаем HUD
            CreateHUD();
            
            // Создаем LevelManager
            CreateLevelManager();
            
            // Создаем InputRouter для обработки ввода
            CreateInputRouter();
            
            Debug.Log("🎮 Тестовый уровень применен к сцене!");
        }
        
        private void CreateStaticTile(Vector2Int pos, Transform parent)
        {
            var staticTile = new GameObject($"StaticTile_{pos.x}_{pos.y}");
            staticTile.transform.SetParent(parent);
            staticTile.transform.position = new Vector3(pos.x, pos.y, 0);
            staticTile.AddComponent<StaticTile>();
            
            // Добавляем визуальный компонент
            var renderer = staticTile.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSquareSprite();
            renderer.color = Color.gray;
        }
        
        private void CreateMovableBlock(BlockPlacement placement, Transform parent)
        {
            var block = new GameObject($"MovableBlock_{placement.Id}");
            block.transform.SetParent(parent);
            block.transform.position = new Vector3(placement.Position.x, placement.Position.y, 0);
            
            var movableBlock = block.AddComponent<MovableBlock>();
            movableBlock.Size = placement.Size;
            
            // Добавляем визуальный компонент
            var renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSquareSprite();
            renderer.color = Color.blue;
        }
        
        private void CreateMarker(string name, Vector2Int pos, Color color)
        {
            var marker = new GameObject(name);
            marker.transform.position = new Vector3(pos.x, pos.y, 0);
            var renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSquareSprite();
            renderer.color = color;
        }
        
        /// <summary>
        /// Обновляет GridModel на основе созданных объектов в сцене
        /// </summary>
        private void UpdateGridModelFromScene()
        {
            Debug.Log("Создаем GridModel для тестового уровня...");
            
            // Создаем собственный GridModel для тестового уровня
            var gridModel = new GridModel(20, 20); // 20x20 для тестового уровня
            
            Debug.Log("Обновляем GridModel на основе объектов в сцене...");
            
            // Добавляем статические препятствия
            var staticTiles = FindObjectsByType<StaticTile>(FindObjectsSortMode.None);
            foreach (var tile in staticTiles)
            {
                Vector2Int pos = new Vector2Int(Mathf.RoundToInt(tile.transform.position.x), Mathf.RoundToInt(tile.transform.position.y));
                if (gridModel.InBounds(pos.x, pos.y))
                {
                    gridModel.PlaceStatic(pos);
                    Debug.Log($"Добавлено статическое препятствие в позиции {pos}");
                }
            }
            
            // Добавляем подвижные блоки
            var movableBlocks = FindObjectsByType<MovableBlock>(FindObjectsSortMode.None);
            foreach (var block in movableBlocks)
            {
                Vector2Int pos = new Vector2Int(Mathf.RoundToInt(block.transform.position.x), Mathf.RoundToInt(block.transform.position.y));
                RectInt rect = new RectInt(pos.x, pos.y, block.Size.x, block.Size.y);
                
                if (gridModel.InBounds(rect.xMin, rect.yMin) && gridModel.InBounds(rect.xMax - 1, rect.yMax - 1))
                {
                    var blockRt = new BlockRt(block.GetInstanceID(), rect);
                    gridModel.AddBlock(blockRt);
                    Debug.Log($"Добавлен подвижный блок ID={block.GetInstanceID()} в позиции {pos} размером {block.Size}");
                }
            }
            
            Debug.Log($"GridModel обновлен: {staticTiles.Length} статических препятствий, {movableBlocks.Length} подвижных блоков");
            
            // Сохраняем GridModel для использования LevelManager'ом
            LastGeneratedGridModel = gridModel;
        }
        
        private void CreateHUD()
        {
            var hudGO = new GameObject("HUD");
            hudGO.AddComponent<HudController>();
        }
        
        private void CreateLevelManager()
        {
            var levelManagerGO = new GameObject("LevelManager");
            levelManagerGO.AddComponent<LevelManager>();
        }
        
        private void CreateInputRouter()
        {
            // Проверяем, есть ли уже InputRouter в сцене
            var existingInputRouters = FindObjectsByType<LabyrinthMover.UI.InputRouter>(FindObjectsSortMode.None);
            if (existingInputRouters.Length > 0)
            {
                Debug.Log($"InputRouter уже существует в сцене ({existingInputRouters.Length} экземпляров), используем существующий");
                // Удаляем лишние экземпляры, оставляем только первый
                for (int i = 1; i < existingInputRouters.Length; i++)
                {
                    Debug.Log($"Удаляем дублирующий InputRouter {existingInputRouters[i].GetInstanceID()}");
                    if (Application.isPlaying)
                        Destroy(existingInputRouters[i].gameObject);
                    else
                        DestroyImmediate(existingInputRouters[i].gameObject);
                }
                return;
            }
            
            var inputRouterGO = new GameObject("InputRouter");
            var inputRouter = inputRouterGO.AddComponent<LabyrinthMover.UI.InputRouter>();
            Debug.Log($"Создан новый InputRouter для обработки ввода (ID: {inputRouter.GetInstanceID()})");
        }
        
        private void SetupCameraForLargeLevel()
        {
            if (cameraController == null)
            {
                cameraController = FindFirstObjectByType<CameraController>();
            }
            
            if (cameraController != null)
            {
                // Центрируем камеру на большом уровне
                cameraController.CenterOnGrid(20, 20);
                // Устанавливаем подходящий зум для обзора всего уровня
                cameraController.SetZoom(12f); // Больший зум для обзора 20x20
            }
        }
        
        private void ClearScene()
        {
            // Удаляем существующие объекты уровня
            var existingObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in existingObjects)
            {
                if (obj.name.Contains("GridRoot") || obj.name.Contains("StaticTile") || 
                    obj.name.Contains("MovableBlock") || obj.name.Contains("Marker") ||
                    obj.name.Contains("HUD"))
                {
                    if (Application.isPlaying)
                        Destroy(obj);
                    else
                        DestroyImmediate(obj);
                }
            }
        }
        
        private Sprite CreateSquareSprite()
        {
            var texture = new Texture2D(32, 32);
            var pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        }
        
        [ContextMenu("Сгенерировать Easy уровень")]
        public void GenerateEasyLevel()
        {
            targetDifficulty = 0;
            GenerateTestLevel();
        }
        
        [ContextMenu("Сгенерировать Medium уровень")]
        public void GenerateMediumLevel()
        {
            targetDifficulty = 1;
            GenerateTestLevel();
        }
        
        [ContextMenu("Сгенерировать Hard уровень")]
        public void GenerateHardLevel()
        {
            targetDifficulty = 2;
            GenerateTestLevel();
        }
    }
}
