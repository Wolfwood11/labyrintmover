using UnityEngine;
using UnityEditor;
using LabyrinthMover.Core;
using LabyrinthMover.Gameplay;
using LabyrinthMover.UI;
using System.Collections.Generic;

namespace LabyrinthMover.Core.Editor
{
    /// <summary>
    /// Статический генератор уровней для работы в редакторе Unity
    /// </summary>
    public static class EditorLevelGeneratorStatic
    {
        /// <summary>
        /// Генерирует уровень с пользовательскими параметрами
        /// </summary>
        public static void GenerateLevelWithCustomParams(LevelGenerationParams parameters, int maxAttempts = 100, int tileSize = 64)
        {
            Debug.Log($"🎮 Начинаем генерацию уровня с пользовательскими параметрами");
            Debug.Log($"📊 Максимум попыток: {maxAttempts}");
            Debug.Log($"⚙️ Параметры: {parameters.Width}x{parameters.Height}, плотность: {(parameters.StaticDensity * 100):F1}%, блоков: {parameters.BaseBlockCount}±{parameters.BlockCountVariation}, энергия: {parameters.Emax}");

            // Генерируем уровень без применения к сцене сначала
            GeneratedLevel successfulLevel = null;
            
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                // Показываем прогресс в консоли
                if (attempt % 10 == 0 || attempt <= 5)
                {
                    Debug.Log($"🔄 Попытка {attempt}/{maxAttempts} ({(attempt * 100 / maxAttempts)}%)");
                }
                
                try
                {
                    var generatedLevel = LevelGenerator.GenerateLevel(parameters);
                    
                    if (generatedLevel != null && generatedLevel.IsSolvable)
                    {
                        Debug.Log($"✅ Уровень успешно сгенерирован на попытке {attempt}!");
                        Debug.Log($"   - Проходим: {generatedLevel.IsSolvable}");
                        Debug.Log($"   - Минимальная энергия: {generatedLevel.EstimatedMinEnergy}");
                        Debug.Log($"   - Минимальные ходы: {generatedLevel.EstimatedMinHops}");
                        Debug.Log($"   - Сложность: {generatedLevel.Difficulty}");
                        
                        successfulLevel = generatedLevel;
                        break;
                    }
                    else
                    {
                        if (attempt <= 5 || attempt % 20 == 0) // Логируем только первые 5 попыток и каждую 20-ю
                        {
                            Debug.LogWarning($"❌ Попытка {attempt}: Уровень не проходим");
                            if (generatedLevel != null)
                            {
                                Debug.LogWarning($"   - Solvable: {generatedLevel.IsSolvable}");
                                Debug.LogWarning($"   - MinEnergy: {generatedLevel.EstimatedMinEnergy}");
                                Debug.LogWarning($"   - MinHops: {generatedLevel.EstimatedMinHops}");
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ Ошибка на попытке {attempt}: {e.Message}");
                }
            }
            
            if (successfulLevel != null)
            {
                // Применяем уровень к сцене только после успешной генерации
                ApplyLevelToSceneWithTileSize(successfulLevel, tileSize);
                Debug.Log("🎯 Генерация завершена успешно!");
            }
            else
            {
                Debug.LogError($"❌ Не удалось сгенерировать проходимый уровень за {maxAttempts} попыток!");
                Debug.LogError("💡 Попробуйте уменьшить плотность препятствий или количество блоков");
            }
        }
        
        /// <summary>
        /// Применяет сгенерированный уровень к сцене с указанным размером тайла
        /// </summary>
        public static void ApplyLevelToSceneWithTileSize(GeneratedLevel level, int tileSize)
        {
            Debug.Log("🎨 Применяем уровень к сцене...");
            
            // Очищаем существующие объекты
            ClearExistingLevel();
            
            // Небольшая задержка для обеспечения правильной очистки
            System.Threading.Thread.Sleep(10);
            
            // Создаем или обновляем GridConfig с правильными размерами
            var gridConfig = FindOrCreateGridConfig();
            gridConfig.Width = level.Params.Width;
            gridConfig.Height = level.Params.Height;
            gridConfig.Ksize = level.Params.Ksize;
            gridConfig.Kdist = level.Params.Kdist;
            gridConfig.Emax = level.Params.Emax;
            gridConfig.TileSize = tileSize; // Используем пользовательский размер тайла
            Debug.Log($"✅ GridConfig обновлен: {gridConfig.Width}x{gridConfig.Height}, размер тайла: {gridConfig.TileSize}");
            
            // Создаем необходимые компоненты
            var levelManager = FindOrCreateLevelManager();
            var hud = FindOrCreateHUD();
            var inputRouter = FindOrCreateInputRouter();
            var cameraController = FindOrCreateCameraController();
            
            // Создаем GridModel из GeneratedLevel
            var gridModel = CreateGridModelFromGeneratedLevel(level);
            if (gridModel != null)
            {
                Debug.Log($"📊 Создан GridModel: {gridModel.Width}x{gridModel.Height}");
                
                // Создаем визуальные объекты напрямую
                CreateVisualObjectsFromGridModel(gridModel, level, tileSize);
                
                // Устанавливаем GridModel в LevelManager
                if (levelManager != null)
                {
                    var field = typeof(LevelManager).GetField("gridModel", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(levelManager, gridModel);
                        Debug.Log("✅ GridModel установлен в LevelManager");
                    }
                }
            }
            
            Debug.Log("🎯 Уровень успешно применен к сцене!");
        }
        public static void GenerateLevelInEditor(int width = 20, int height = 20, 
            LevelDifficulty difficulty = LevelDifficulty.Medium, int maxAttempts = 100)
        {
            Debug.Log($"🎮 Начинаем генерацию уровня {width}x{height} (сложность: {difficulty})");
            Debug.Log($"📊 Максимум попыток: {maxAttempts}");

            var parameters = GetDifficultyParameters(difficulty, width, height);
            
            // Генерируем уровень без применения к сцене сначала
            GeneratedLevel successfulLevel = null;
            
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                // Показываем прогресс в консоли
                if (attempt % 10 == 0 || attempt <= 5)
                {
                    Debug.Log($"🔄 Попытка {attempt}/{maxAttempts} ({(attempt * 100 / maxAttempts)}%)");
                }
                
                try
                {
                    var generatedLevel = LevelGenerator.GenerateLevel(parameters);
                    
                    if (generatedLevel != null && generatedLevel.IsSolvable)
                    {
                        Debug.Log($"✅ Уровень успешно сгенерирован на попытке {attempt}!");
                        Debug.Log($"   - Проходим: {generatedLevel.IsSolvable}");
                        Debug.Log($"   - Минимальная энергия: {generatedLevel.EstimatedMinEnergy}");
                        Debug.Log($"   - Минимальные ходы: {generatedLevel.EstimatedMinHops}");
                        Debug.Log($"   - Сложность: {generatedLevel.Difficulty}");
                        
                        successfulLevel = generatedLevel;
                        break;
                    }
                    else
                    {
                        if (attempt <= 5 || attempt % 20 == 0) // Логируем только первые 5 попыток и каждую 20-ю
                    {
                        Debug.LogWarning($"❌ Попытка {attempt}: Уровень не проходим");
                        if (generatedLevel != null)
                        {
                            Debug.LogWarning($"   - Solvable: {generatedLevel.IsSolvable}");
                            Debug.LogWarning($"   - MinEnergy: {generatedLevel.EstimatedMinEnergy}");
                            Debug.LogWarning($"   - MinHops: {generatedLevel.EstimatedMinHops}");
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ Ошибка на попытке {attempt}: {e.Message}");
                }
            }
            
            if (successfulLevel != null)
            {
                // Применяем уровень к сцене только после успешной генерации
                ApplyLevelToScene(successfulLevel);
                Debug.Log("🎯 Генерация завершена успешно!");
            }
            else
            {
                Debug.LogError($"❌ Не удалось сгенерировать проходимый уровень за {maxAttempts} попыток!");
                Debug.LogError("💡 Попробуйте уменьшить сложность или увеличить размер уровня");
            }
        }
        
        /// <summary>
        /// Применяет сгенерированный уровень к сцене
        /// </summary>
        private static void ApplyLevelToScene(GeneratedLevel level)
        {
            Debug.Log("🎨 Применяем уровень к сцене...");
            
            // Очищаем существующие объекты
            ClearExistingLevel();
            
            // Создаем или обновляем GridConfig с правильными размерами
            var gridConfig = FindOrCreateGridConfig();
            gridConfig.Width = level.Params.Width;
            gridConfig.Height = level.Params.Height;
            gridConfig.Ksize = level.Params.Ksize;
            gridConfig.Kdist = level.Params.Kdist;
            gridConfig.Emax = level.Params.Emax;
            gridConfig.TileSize = 64; // Стандартный размер тайла
            Debug.Log($"✅ GridConfig обновлен: {gridConfig.Width}x{gridConfig.Height}");
            
            // Создаем необходимые компоненты
            var levelManager = FindOrCreateLevelManager();
            var hud = FindOrCreateHUD();
            var inputRouter = FindOrCreateInputRouter();
            var cameraController = FindOrCreateCameraController();
            
                // Создаем GridModel из GeneratedLevel
                var gridModel = CreateGridModelFromGeneratedLevel(level);
                if (gridModel != null)
                {
                    Debug.Log($"📊 Создан GridModel: {gridModel.Width}x{gridModel.Height}");
                    
                // Создаем визуальные объекты напрямую
                CreateVisualObjectsFromGridModel(gridModel, level, gridConfig.TileSize);
                
                // Устанавливаем GridModel в LevelManager
                if (levelManager != null)
                {
                    var field = typeof(LevelManager).GetField("gridModel", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(levelManager, gridModel);
                        Debug.Log("✅ GridModel установлен в LevelManager");
                    }
                }
            }
            
            Debug.Log("🎯 Уровень успешно применен к сцене!");
        }
        
        /// <summary>
        /// Очищает существующие объекты уровня
        /// </summary>
        public static void ClearExistingLevel()
        {
            Debug.Log("🧹 Полная очистка старого уровня...");
            
            // Удаляем контейнеры уровня - это автоматически удалит все дочерние объекты
            var staticsContainer = GameObject.Find("Statics");
            if (staticsContainer != null)
            {
                Object.DestroyImmediate(staticsContainer);
                Debug.Log("🗑️ Удален контейнер Statics");
            }
            
            var blocksContainer = GameObject.Find("Blocks");
            if (blocksContainer != null)
            {
                Object.DestroyImmediate(blocksContainer);
                Debug.Log("🗑️ Удален контейнер Blocks");
            }
            
            // Удаляем маркеры старта и финиша
            var markerStart = GameObject.Find("MarkerStart");
            if (markerStart != null)
            {
                Object.DestroyImmediate(markerStart);
                Debug.Log("🗑️ Удален маркер старта");
            }
            
            var markerGoal = GameObject.Find("MarkerGoal");
            if (markerGoal != null)
            {
                Object.DestroyImmediate(markerGoal);
                Debug.Log("🗑️ Удален маркер финиша");
            }
            
            // Удаляем всех персонажей
            var players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player != null && player.gameObject != null)
                {
                    Object.DestroyImmediate(player.gameObject);
                }
            }
            
            Debug.Log($"✅ Полная очистка завершена: {players.Length} персонажей");
        }
        
        /// <summary>
        /// Находит или создает GridConfig
        /// </summary>
        private static GridConfig FindOrCreateGridConfig()
        {
            var gridConfig = Object.FindFirstObjectByType<GridConfig>();
            if (gridConfig == null)
            {
                var go = new GameObject("GridConfig");
                gridConfig = go.AddComponent<GridConfig>();
                Debug.Log("✅ Создан GridConfig");
            }
            return gridConfig;
        }
        
        /// <summary>
        /// Находит или создает LevelManager
        /// </summary>
        private static LevelManager FindOrCreateLevelManager()
        {
            var levelManager = Object.FindFirstObjectByType<LevelManager>();
            if (levelManager == null)
            {
                var go = new GameObject("LevelManager");
                levelManager = go.AddComponent<LevelManager>();
                Debug.Log("✅ Создан LevelManager");
            }
            return levelManager;
        }
        
        /// <summary>
        /// Находит или создает HUD
        /// </summary>
        private static HudController FindOrCreateHUD()
        {
            var hud = Object.FindFirstObjectByType<HudController>();
            if (hud == null)
            {
                var go = new GameObject("HUD");
                hud = go.AddComponent<HudController>();
                Debug.Log("✅ Создан HUD");
            }
            return hud;
        }
        
        /// <summary>
        /// Находит или создает InputRouter
        /// </summary>
        private static InputRouter FindOrCreateInputRouter()
        {
            var inputRouter = Object.FindFirstObjectByType<InputRouter>();
            if (inputRouter == null)
            {
                var go = new GameObject("InputRouter");
                inputRouter = go.AddComponent<InputRouter>();
                Debug.Log("✅ Создан InputRouter");
            }
            return inputRouter;
        }
        
        /// <summary>
        /// Находит или создает CameraController
        /// </summary>
        private static CameraController FindOrCreateCameraController()
        {
            var cameraController = Object.FindFirstObjectByType<CameraController>();
            if (cameraController == null)
            {
                var go = new GameObject("CameraController");
                cameraController = go.AddComponent<CameraController>();
                Debug.Log("✅ Создан CameraController");
            }
            return cameraController;
        }
        
        /// <summary>
        /// Создает GridModel из GeneratedLevel
        /// </summary>
        private static GridModel CreateGridModelFromGeneratedLevel(GeneratedLevel level)
        {
            var gridModel = new GridModel(level.Params.Width, level.Params.Height);
            
            // Создаем границы уровня
            CreateLevelBorders(gridModel, level.Params.Width, level.Params.Height);
            
            // Размещаем статические препятствия (исключая границы)
            foreach (var pos in level.StaticPositions)
            {
                // Проверяем, что позиция не на границе (границы уже созданы в CreateLevelBorders)
                if (!IsOnBorder(pos, level.Params.Width, level.Params.Height))
                {
                    Debug.Log($"🧱 Размещаем статическое препятствие в позиции {pos}");
                    gridModel.PlaceStatic(pos);
                }
                else
                {
                    Debug.Log($"🔲 Пропускаем позицию {pos} - это граница (уже создана)");
                }
            }
            
            // Размещаем подвижные блоки
            foreach (var placement in level.BlockPlacements)
            {
                var rect = new RectInt(placement.Position.x, placement.Position.y, placement.Size.x, placement.Size.y);
                var blockRt = new BlockRt(placement.Id, rect);
                gridModel.AddBlock(blockRt);
            }
            
            return gridModel;
        }
        
        /// <summary>
        /// Создает границы уровня из неподвижных тайлов
        /// </summary>
        private static void CreateLevelBorders(GridModel grid, int width, int height)
        {
            // Верхняя и нижняя границы
            for (int x = 0; x < width; x++)
            {
                grid.PlaceStatic(new Vector2Int(x, 0)); // Нижняя граница
                grid.PlaceStatic(new Vector2Int(x, height - 1)); // Верхняя граница
            }
            
            // Левая и правая границы
            for (int y = 1; y < height - 1; y++)
            {
                grid.PlaceStatic(new Vector2Int(0, y)); // Левая граница
                grid.PlaceStatic(new Vector2Int(width - 1, y)); // Правая граница
            }
            
            int totalBorderTiles = (width * 2) + (height * 2) - 4; // Исключаем углы (они считаются дважды)
            Debug.Log($"🛡️ Созданы границы уровня: {width}x{height} (внутренняя область: {width-2}x{height-2})");
            Debug.Log($"🛡️ Всего блоков границы: {totalBorderTiles}");
        }
        
        /// <summary>
        /// Проверяет, находится ли позиция на границе уровня
        /// </summary>
        private static bool IsOnBorder(Vector2Int pos, int width, int height)
        {
            return pos.x == 0 || pos.x == width - 1 || pos.y == 0 || pos.y == height - 1;
        }
        
        /// <summary>
        /// Создает визуальные объекты на основе GridModel и GeneratedLevel
        /// </summary>
        private static void CreateVisualObjectsFromGridModel(GridModel gridModel, GeneratedLevel generatedLevel, int tileSize)
        {
            Debug.Log("🎨 Создаем визуальные объекты из GridModel и GeneratedLevel...");
            
            // Создаем контейнеры
            var staticsContainer = new GameObject("Statics");
            var blocksContainer = new GameObject("Blocks");
            Debug.Log($"📦 Созданы контейнеры: Statics и Blocks");
            
            // Создаем статические тайлы
            int staticTilesCreated = 0;
            int borderTilesCreated = 0;
            for (int x = 0; x < gridModel.Width; x++)
            {
                for (int y = 0; y < gridModel.Height; y++)
                {
                    if (gridModel.cells[x, y] == CellType.Static)
                    {
                        CreateStaticTile(new Vector2Int(x, y), staticsContainer.transform, tileSize);
                        staticTilesCreated++;
                        
                        // Логируем границы для диагностики
                        if (x == 0 || x == gridModel.Width - 1 || y == 0 || y == gridModel.Height - 1)
                        {
                            borderTilesCreated++;
                            Debug.Log($"🔲 Создан блок границы в позиции ({x}, {y}) - всего границ: {borderTilesCreated}");
                        }
                    }
                }
            }
            
            Debug.Log($"🔲 Создано {staticTilesCreated} статических тайлов (включая {borderTilesCreated} границ)");
            
            // Создаем подвижные блоки из GeneratedLevel.BlockPlacements
            Debug.Log($"🔧 Создаем {generatedLevel.BlockPlacements.Count} подвижных блоков из GeneratedLevel...");
            for (int i = 0; i < generatedLevel.BlockPlacements.Count; i++)
            {
                var placement = generatedLevel.BlockPlacements[i];
                Debug.Log($"🔧 Создаем блок {i + 1}: позиция {placement.Position}, размер {placement.Size}");
                
                // Создаем BlockRt для совместимости с существующим методом
                var blockRt = new BlockRt(i, new RectInt(placement.Position.x, placement.Position.y, placement.Size.x, placement.Size.y));
                
                CreateMovableBlock(blockRt, blocksContainer.transform, tileSize);
            }
            
            // Создаем маркеры старта и цели
            CreateMarker("MarkerStart", generatedLevel.StartPosition, Color.green, tileSize);
            CreateMarker("MarkerGoal", generatedLevel.GoalPosition, Color.red, tileSize);
            
            Debug.Log($"✅ Создано визуальных объектов: {staticTilesCreated} статических тайлов, {generatedLevel.BlockPlacements.Count} блоков, 2 маркера (размер тайла: {tileSize})");
        }
        
        /// <summary>
        /// Создает статический тайл
        /// </summary>
        private static void CreateStaticTile(Vector2Int pos, Transform parent, int tileSize)
        {
            var staticTile = new GameObject($"StaticTile_{pos.x}_{pos.y}");
            staticTile.transform.SetParent(parent);
            
            // Масштабируем позицию в соответствии с размером тайла
            float worldX = pos.x * (tileSize / 100f);
            float worldY = pos.y * (tileSize / 100f);
            staticTile.transform.position = new Vector3(worldX, worldY, 0);
            
            // Логируем создание блоков верхней границы
            if (pos.y == 19) // Для уровня 20x20 верхняя граница на y=19
            {
                Debug.Log($"🔲 CreateStaticTile: Создан визуальный блок верхней границы ({pos.x}, {pos.y}) в позиции ({worldX}, {worldY})");
            }
            
            staticTile.AddComponent<StaticTile>();
            
            // Добавляем визуальный компонент
            var renderer = staticTile.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSquareSprite(tileSize);
            renderer.color = Color.gray;
        }
        
        /// <summary>
        /// Создает подвижный блок с улучшенной системой управления
        /// </summary>
        private static void CreateMovableBlock(BlockRt block, Transform parent, int tileSize)
        {
            var blockObj = new GameObject($"MovableBlock_{block.Id}");
            blockObj.transform.SetParent(parent);
            
            // Масштабируем позицию в соответствии с размером тайла
            float worldX = block.Rect.x * (tileSize / 100f);
            float worldY = block.Rect.y * (tileSize / 100f);
            blockObj.transform.position = new Vector3(worldX, worldY, 0);
            
            var movableBlock = blockObj.AddComponent<MovableBlock>();
            movableBlock.Size = new Vector2Int(block.Rect.width, block.Rect.height);
            
            // Добавляем визуальный компонент
            var renderer = blockObj.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSquareSprite(tileSize);
            renderer.color = Color.blue;
            
            // Добавляем компоненты для улучшенного управления
            var dragController = blockObj.AddComponent<LabyrinthMover.UI.EnhancedDragController>();
            var blockAnimator = blockObj.AddComponent<LabyrinthMover.Gameplay.BlockAnimator>();
            
            // Добавляем коллайдер для обработки касаний
            var collider = blockObj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(block.Rect.width, block.Rect.height);
            collider.isTrigger = false;
            
            Debug.Log($"✅ Создан улучшенный блок {block.Id} с компонентами управления");
        }
        
        /// <summary>
        /// Создает маркер
        /// </summary>
        private static void CreateMarker(string name, Vector2Int pos, Color color, int tileSize)
        {
            // Проверяем, не существует ли уже маркер с таким именем
            var existingMarker = GameObject.Find(name);
            if (existingMarker != null)
            {
                Object.DestroyImmediate(existingMarker);
                Debug.Log($"🗑️ Удален существующий маркер {name}");
            }
            
            var marker = new GameObject(name);
            
            // Масштабируем позицию в соответствии с размером тайла
            float worldX = pos.x * (tileSize / 100f);
            float worldY = pos.y * (tileSize / 100f);
            marker.transform.position = new Vector3(worldX, worldY, 0);
            
            var renderer = marker.AddComponent<SpriteRenderer>();
            
            // Для старта и финиша используем специальные цвета и размеры
            if (name == "MarkerStart")
            {
                renderer.sprite = CreateSquareSprite(tileSize);
                renderer.color = new Color(0f, 1f, 0f, 0.9f); // Ярко-зеленый
                Debug.Log($"🟢 Создан маркер старта в позиции ({pos.x}, {pos.y})");
            }
            else if (name == "MarkerGoal")
            {
                renderer.sprite = CreateSquareSprite(tileSize);
                renderer.color = new Color(1f, 0f, 0f, 0.9f); // Ярко-красный
                Debug.Log($"🔴 Создан маркер финиша в позиции ({pos.x}, {pos.y})");
            }
            else
            {
                renderer.sprite = CreateSquareSprite(tileSize);
                renderer.color = color;
            }
        }
        
        /// <summary>
        /// Создает квадратный спрайт с настраиваемым размером
        /// </summary>
        private static Sprite CreateSquareSprite(int size)
        {
            var texture = new Texture2D(size, size);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
        private static LevelGenerationParams GetDifficultyParameters(LevelDifficulty difficulty, int width, int height)
        {
            var parameters = new LevelGenerationParams
            {
                Width = width,
                Height = height
            };
            
            switch (difficulty)
            {
                case LevelDifficulty.Easy:
                    parameters.StaticDensity = 0.15f;
                    parameters.BaseBlockCount = Mathf.Max(8, (width * height) / 30); // Базовое количество блоков
                    parameters.BlockCountVariation = 3; // ±3 блока для Easy
                    parameters.TargetMinHops = 6;
                    parameters.TargetMinEnergy = 12;
                    parameters.Emax = 30f; // Меньше энергии для Easy
                    break;
                    
                case LevelDifficulty.Medium:
                    parameters.StaticDensity = 0.20f;
                    parameters.BaseBlockCount = Mathf.Max(10, (width * height) / 25); // Базовое количество блоков
                    parameters.BlockCountVariation = 5; // ±5 блоков для Medium
                    parameters.TargetMinHops = 10;
                    parameters.TargetMinEnergy = 20;
                    parameters.Emax = 50f; // Средняя энергия для Medium
                    break;
                    
                case LevelDifficulty.Hard:
                    parameters.StaticDensity = 0.25f;
                    parameters.BaseBlockCount = Mathf.Max(12, (width * height) / 20); // Базовое количество блоков
                    parameters.BlockCountVariation = 7; // ±7 блоков для Hard
                    parameters.TargetMinHops = 15;
                    parameters.TargetMinEnergy = 30;
                    parameters.Emax = 70f; // Больше энергии для Hard
                    break;
                    
                default:
                    parameters.StaticDensity = 0.20f;
                    parameters.BaseBlockCount = Mathf.Max(10, (width * height) / 25);
                    parameters.BlockCountVariation = 5;
                    parameters.TargetMinHops = 10;
                    parameters.TargetMinEnergy = 20;
                    parameters.Emax = 50f;
                    break;
            }
            
            return parameters;
        }
    }
}
