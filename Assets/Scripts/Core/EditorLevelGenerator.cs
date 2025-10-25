using UnityEngine;
using LabyrinthMover.Core;
using LabyrinthMover.Gameplay;
using LabyrinthMover.UI;

namespace LabyrinthMover.Core
{
    /// <summary>
    /// Генератор уровней для редактора Unity с ограничением попыток
    /// </summary>
    public class EditorLevelGenerator : MonoBehaviour
    {
        [Header("Настройки генерации")]
        [SerializeField] public int maxAttempts = 100;
        [SerializeField] public bool generateOnStart = false;
        
        [Header("Параметры уровня")]
        [SerializeField] public int levelWidth = 20;
        [SerializeField] public int levelHeight = 20;
        [SerializeField] public LevelDifficulty difficulty = LevelDifficulty.Medium;
        
        [Header("Компоненты")]
        [SerializeField] private GridConfig gridConfig;
        [SerializeField] private CameraController cameraController;
        
        private int currentAttempt = 0;
        private bool isGenerating = false;
        
        private void Start()
        {
            if (generateOnStart)
            {
                GenerateLevelInEditor();
            }
        }
        
        /// <summary>
        /// Генерирует уровень в редакторе с ограничением попыток
        /// </summary>
        [ContextMenu("Сгенерировать уровень")]
        public void GenerateLevelInEditor()
        {
            if (isGenerating)
            {
                Debug.LogWarning("Генерация уже выполняется!");
                return;
            }
            
            Debug.Log($"🎮 Начинаем генерацию уровня {levelWidth}x{levelHeight} (сложность: {difficulty})");
            Debug.Log($"📊 Максимум попыток: {maxAttempts}");
            
            isGenerating = true;
            currentAttempt = 0;
            
            // Запускаем генерацию в корутине для возможности прерывания
            StartCoroutine(GenerateLevelCoroutine());
        }
        
        /// <summary>
        /// Корутина для генерации уровня с ограничением попыток
        /// </summary>
        private System.Collections.IEnumerator GenerateLevelCoroutine()
        {
            GeneratedLevel lastValidLevel = null;
            
            while (currentAttempt < maxAttempts && isGenerating)
            {
                currentAttempt++;
                Debug.Log($"🔄 Попытка {currentAttempt}/{maxAttempts}");
                
                // Создаем параметры для текущей попытки
                var parameters = CreateLevelParameters();
                
                // Генерируем уровень
                var generatedLevel = LevelGenerator.GenerateLevel(parameters);
                
                // Проверяем, проходим ли уровень
                if (generatedLevel.IsSolvable)
                {
                    Debug.Log($"✅ Успешно сгенерирован проходимый уровень на попытке {currentAttempt}!");
                    Debug.Log($"📊 Статистика:");
                    Debug.Log($"   - Сложность: {generatedLevel.Difficulty}");
                    Debug.Log($"   - Оценка энергии: {generatedLevel.EstimatedMinEnergy}");
                    Debug.Log($"   - Оценка ходов: {generatedLevel.EstimatedMinHops}");
                    Debug.Log($"   - Статических препятствий: {generatedLevel.StaticPositions.Count}");
                    Debug.Log($"   - Подвижных блоков: {generatedLevel.BlockPlacements.Count}");
                    Debug.Log($"   - Seed: {generatedLevel.Seed}");
                    
                    // Применяем уровень к сцене
                    ApplyLevelToScene(generatedLevel);
                    
                    // Настраиваем камеру
                    SetupCamera();
                    
                    isGenerating = false;
                    yield break; // Успешно завершаем генерацию
                }
                else
                {
                    Debug.LogWarning($"❌ Попытка {currentAttempt}: Уровень не проходим");
                    lastValidLevel = generatedLevel; // Сохраняем последний сгенерированный уровень
                }
                
                // Небольшая пауза между попытками для предотвращения зависания редактора
                yield return new WaitForSeconds(0.01f);
            }
            
            // Если не удалось сгенерировать проходимый уровень
            if (currentAttempt >= maxAttempts)
            {
                Debug.LogError($"❌ Не удалось сгенерировать проходимый уровень за {maxAttempts} попыток!");
                
                if (lastValidLevel != null)
                {
                    Debug.LogWarning("⚠️ Применяем последний сгенерированный уровень (может быть непроходимым)");
                    ApplyLevelToScene(lastValidLevel);
                    SetupCamera();
                }
                else
                {
                    Debug.LogError("❌ Не было сгенерировано ни одного уровня!");
                }
            }
            
            isGenerating = false;
        }
        
        /// <summary>
        /// Создает параметры для генерации уровня
        /// </summary>
        private LevelGenerationParams CreateLevelParameters()
        {
            var parameters = new LevelGenerationParams
            {
                Width = levelWidth,
                Height = levelHeight,
                StaticDensity = GetStaticDensityForDifficulty(difficulty),
                BlockSizes = GetBlockSizesForDifficulty(difficulty),
                BaseBlockCount = GetBlockCountForDifficulty(difficulty),
                BlockCountVariation = 2, // Небольшая вариация для старого генератора
                TargetMinHops = GetTargetHopsForDifficulty(difficulty),
                TargetMinEnergy = GetTargetEnergyForDifficulty(difficulty),
                Ksize = 1.0f,
                Kdist = 0.1f,
                Emax = GetMaxEnergyForDifficulty(difficulty)
            };
            
            return parameters;
        }
        
        /// <summary>
        /// Получает плотность статических препятствий для сложности
        /// </summary>
        private float GetStaticDensityForDifficulty(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Easy: return 0.1f;
                case LevelDifficulty.Medium: return 0.15f;
                case LevelDifficulty.Hard: return 0.2f;
                default: return 0.15f;
            }
        }
        
        /// <summary>
        /// Получает размеры блоков для сложности
        /// </summary>
        private System.Collections.Generic.List<Vector2Int> GetBlockSizesForDifficulty(LevelDifficulty difficulty)
        {
            var sizes = new System.Collections.Generic.List<Vector2Int>();
            
            switch (difficulty)
            {
                case LevelDifficulty.Easy:
                    sizes.Add(Vector2Int.one); // 1x1
                    sizes.Add(new Vector2Int(2, 1)); // 2x1
                    sizes.Add(new Vector2Int(1, 2)); // 1x2
                    break;
                    
                case LevelDifficulty.Medium:
                    sizes.Add(Vector2Int.one); // 1x1
                    sizes.Add(new Vector2Int(2, 1)); // 2x1
                    sizes.Add(new Vector2Int(1, 2)); // 1x2
                    sizes.Add(new Vector2Int(2, 2)); // 2x2
                    sizes.Add(new Vector2Int(3, 1)); // 3x1
                    break;
                    
                case LevelDifficulty.Hard:
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
        
        /// <summary>
        /// Получает количество блоков для сложности
        /// </summary>
        private int GetBlockCountForDifficulty(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Easy: return 5;
                case LevelDifficulty.Medium: return 8;
                case LevelDifficulty.Hard: return 12;
                default: return 8;
            }
        }
        
        /// <summary>
        /// Получает целевое количество ходов для сложности
        /// </summary>
        private int GetTargetHopsForDifficulty(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Easy: return 8;
                case LevelDifficulty.Medium: return 15;
                case LevelDifficulty.Hard: return 25;
                default: return 15;
            }
        }
        
        /// <summary>
        /// Получает целевую энергию для сложности
        /// </summary>
        private int GetTargetEnergyForDifficulty(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Easy: return 20;
                case LevelDifficulty.Medium: return 40;
                case LevelDifficulty.Hard: return 70;
                default: return 40;
            }
        }
        
        /// <summary>
        /// Получает максимальную энергию для сложности
        /// </summary>
        private float GetMaxEnergyForDifficulty(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Easy: return 30f;
                case LevelDifficulty.Medium: return 60f;
                case LevelDifficulty.Hard: return 100f;
                default: return 60f;
            }
        }
        
        /// <summary>
        /// Применяет сгенерированный уровень к сцене
        /// </summary>
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
            
            // Создаем необходимые компоненты
            CreateHUD();
            CreateLevelManager();
            CreateInputRouter();
            
            Debug.Log("🎮 Уровень успешно применен к сцене!");
        }
        
        /// <summary>
        /// Создает статический тайл
        /// </summary>
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
        
        /// <summary>
        /// Создает подвижный блок
        /// </summary>
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
        
        /// <summary>
        /// Создает маркер
        /// </summary>
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
            Debug.Log("Создаем GridModel для сгенерированного уровня...");
            
            // Создаем собственный GridModel
            var gridModel = new GridModel(levelWidth, levelHeight);
            
            // Добавляем статические препятствия
            var staticTiles = FindObjectsByType<StaticTile>(FindObjectsSortMode.None);
            foreach (var tile in staticTiles)
            {
                Vector2Int pos = new Vector2Int(Mathf.RoundToInt(tile.transform.position.x), Mathf.RoundToInt(tile.transform.position.y));
                if (gridModel.InBounds(pos.x, pos.y))
                {
                    gridModel.PlaceStatic(pos);
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
                }
            }
            
            // Сохраняем GridModel для использования LevelManager'ом
            TestLevelGenerator.LastGeneratedGridModel = gridModel;
        }
        
        /// <summary>
        /// Создает HUD
        /// </summary>
        private void CreateHUD()
        {
            var hudGO = new GameObject("HUD");
            hudGO.AddComponent<HudController>();
        }
        
        /// <summary>
        /// Создает LevelManager
        /// </summary>
        private void CreateLevelManager()
        {
            var levelManagerGO = new GameObject("LevelManager");
            levelManagerGO.AddComponent<LevelManager>();
        }
        
        /// <summary>
        /// Создает InputRouter
        /// </summary>
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
        
        /// <summary>
        /// Настраивает камеру
        /// </summary>
        private void SetupCamera()
        {
            if (cameraController == null)
            {
                cameraController = FindFirstObjectByType<CameraController>();
            }
            
            if (cameraController != null)
            {
                cameraController.CenterOnGrid(levelWidth, levelHeight);
                cameraController.SetZoom(12f);
            }
        }
        
        /// <summary>
        /// Очищает сцену от существующих объектов уровня
        /// </summary>
        private void ClearScene()
        {
            var existingObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in existingObjects)
            {
                if (obj.name.Contains("GridRoot") || obj.name.Contains("StaticTile") || 
                    obj.name.Contains("MovableBlock") || obj.name.Contains("Marker") ||
                    obj.name.Contains("HUD") || obj.name.Contains("LevelManager") ||
                    obj.name.Contains("InputRouter"))
                {
                    if (Application.isPlaying)
                        Destroy(obj);
                    else
                        DestroyImmediate(obj);
                }
            }
        }
        
        /// <summary>
        /// Создает квадратный спрайт
        /// </summary>
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
        
        /// <summary>
        /// Останавливает генерацию
        /// </summary>
        [ContextMenu("Остановить генерацию")]
        public void StopGeneration()
        {
            if (isGenerating)
            {
                Debug.Log("⏹️ Остановка генерации...");
                isGenerating = false;
                StopAllCoroutines();
            }
            else
            {
                Debug.Log("ℹ️ Генерация не выполняется");
            }
        }
        
        /// <summary>
        /// Принудительно останавливает генерацию (для использования из редактора)
        /// </summary>
        public void ForceStopGeneration()
        {
            Debug.Log("🛑 Принудительная остановка генерации!");
            isGenerating = false;
            StopAllCoroutines();
        }
        
        /// <summary>
        /// Быстрая генерация Easy уровня
        /// </summary>
        [ContextMenu("Сгенерировать Easy уровень")]
        public void GenerateEasyLevel()
        {
            difficulty = LevelDifficulty.Easy;
            GenerateLevelInEditor();
        }
        
        /// <summary>
        /// Быстрая генерация Medium уровня
        /// </summary>
        [ContextMenu("Сгенерировать Medium уровень")]
        public void GenerateMediumLevel()
        {
            difficulty = LevelDifficulty.Medium;
            GenerateLevelInEditor();
        }
        
        /// <summary>
        /// Быстрая генерация Hard уровня
        /// </summary>
        [ContextMenu("Сгенерировать Hard уровень")]
        public void GenerateHardLevel()
        {
            difficulty = LevelDifficulty.Hard;
            GenerateLevelInEditor();
        }
    }
}
