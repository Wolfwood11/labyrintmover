using UnityEngine;
using UnityEditor;
using LabyrinthMover.Core;
using LabyrinthMover.Gameplay;
using LabyrinthMover.UI;
using System.Collections.Generic;

namespace LabyrinthMover.Core.Editor
{
    /// <summary>
    /// Упрощенный генератор уровней для быстрой работы в редакторе
    /// </summary>
    public static class SimpleEditorLevelGenerator
    {
        /// <summary>
        /// Быстро генерирует простой уровень без сложной валидации
        /// </summary>
        public static void GenerateSimpleLevel(int width = 10, int height = 10, int tileSize = 32)
        {
            Debug.Log($"🎮 Быстрая генерация простого уровня {width}x{height} (размер тайла: {tileSize}px)...");
            
            // Создаем простой уровень без решателя
            var gridModel = CreateSimpleLevel(width, height);
            
            // Применяем к сцене
            ApplySimpleLevelToScene(gridModel, tileSize);
            
            Debug.Log("✅ Простой уровень создан!");
        }
        
        /// <summary>
        /// Создает простой уровень без сложной валидации
        /// </summary>
        private static GridModel CreateSimpleLevel(int width, int height)
        {
            var grid = new GridModel(width, height);
            
            // Создаем границы уровня
            CreateLevelBorders(grid, width, height);
            
            // Размещаем старт и цель (внутри границ)
            var startPos = new Vector2Int(1, 1); // Левый нижний угол внутри границ
            var goalPos = new Vector2Int(width - 2, height - 2); // Правый верхний угол внутри границ
            
            // Размещаем статические препятствия внутри границ
            int staticCount = Mathf.RoundToInt((width - 2) * (height - 2) * 0.15f); // 15% от внутренней площади
            for (int i = 0; i < staticCount; i++)
            {
                var pos = new Vector2Int(
                    Random.Range(2, width - 2), // Внутри границ
                    Random.Range(2, height - 2)
                );
                
                // Не размещаем на старте и цели
                if (pos != startPos && pos != goalPos)
                {
                    grid.PlaceStatic(pos);
                }
            }
            
            // Размещаем подвижные блоки внутри границ
            // Увеличиваем количество блоков в зависимости от размера уровня
            int blockCount = Mathf.Min(Mathf.Max(5, (width * height) / 20), 15); // От 5 до 15 блоков
            Debug.Log($"📦 Создаем {blockCount} подвижных блоков для уровня {width}x{height}");
            
            for (int i = 0; i < blockCount; i++)
            {
                var pos = new Vector2Int(
                    Random.Range(2, width - 3), // Внутри границ с запасом
                    Random.Range(2, height - 3)
                );
                
                // Не размещаем на старте и цели
                if (pos != startPos && pos != goalPos)
                {
                    var rect = new RectInt(pos.x, pos.y, 1, 1);
                    var blockRt = new BlockRt(i + 1, rect);
                    grid.AddBlock(blockRt);
                }
            }
            
            return grid;
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
            
            Debug.Log($"🛡️ Созданы границы уровня: {width}x{height} (внутренняя область: {width-2}x{height-2})");
        }
        
        /// <summary>
        /// Применяет простой уровень к сцене
        /// </summary>
        private static void ApplySimpleLevelToScene(GridModel gridModel, int tileSize)
        {
            Debug.Log("🎨 Применяем простой уровень к сцене...");
            
            // Очищаем существующие объекты
            ClearExistingLevel();
            
            // Создаем или обновляем GridConfig с правильными размерами
            var gridConfig = FindOrCreateGridConfig();
            gridConfig.Width = gridModel.Width;
            gridConfig.Height = gridModel.Height;
            gridConfig.Ksize = 1.0f;
            gridConfig.Kdist = 0.1f;
            gridConfig.Emax = 60f;
            gridConfig.TileSize = tileSize;
            Debug.Log($"✅ GridConfig обновлен: {gridConfig.Width}x{gridConfig.Height}, размер тайла: {gridConfig.TileSize}");
            
            // Создаем необходимые компоненты
            var levelManager = FindOrCreateLevelManager();
            var hud = FindOrCreateHUD();
            var inputRouter = FindOrCreateInputRouter();
            var cameraController = FindOrCreateCameraController();
            
            // Создаем визуальные объекты на основе GridModel
            CreateVisualObjectsFromGridModel(gridModel);
            
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
                
                // НЕ вызываем BuildGridModel, так как мы уже создали визуальные объекты
                // и установили GridModel напрямую
                Debug.Log("✅ LevelManager настроен с готовым GridModel");
            }
            
            Debug.Log("🎯 Простой уровень применен к сцене!");
        }
        
        /// <summary>
        /// Очищает существующие объекты уровня
        /// </summary>
        private static void ClearExistingLevel()
        {
            Debug.Log("🧹 Полная очистка старого уровня...");
            
            // Удаляем контейнеры уровня
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
            
            // Удаляем все статические тайлы
            var staticTiles = Object.FindObjectsByType<StaticTile>(FindObjectsSortMode.None);
            foreach (var tile in staticTiles)
            {
                if (tile != null && tile.gameObject != null)
                {
                    Object.DestroyImmediate(tile.gameObject);
                }
            }
            
            // Удаляем все подвижные блоки
            var movableBlocks = Object.FindObjectsByType<MovableBlock>(FindObjectsSortMode.None);
            foreach (var block in movableBlocks)
            {
                if (block != null && block.gameObject != null)
                {
                    Object.DestroyImmediate(block.gameObject);
                }
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
            
            // Удаляем все объекты с именами, содержащими "StaticTile", "MovableBlock", "Marker"
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj != null && (obj.name.Contains("StaticTile") || 
                                   obj.name.Contains("MovableBlock") || 
                                   obj.name.Contains("Marker")))
                {
                    Object.DestroyImmediate(obj);
                }
            }
            
            Debug.Log($"✅ Полная очистка завершена: {staticTiles.Length} статических тайлов, {movableBlocks.Length} блоков, {players.Length} персонажей");
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
        /// Создает визуальные объекты на основе GridModel
        /// </summary>
        private static void CreateVisualObjectsFromGridModel(GridModel gridModel)
        {
            Debug.Log("🎨 Создаем визуальные объекты из GridModel...");
            
            // Получаем GridConfig для размера тайлов
            var gridConfig = Object.FindFirstObjectByType<GridConfig>();
            int tileSize = gridConfig != null ? gridConfig.TileSize : 64;
            
            // Создаем контейнеры
            var staticsContainer = new GameObject("Statics");
            var blocksContainer = new GameObject("Blocks");
            
            // Создаем статические тайлы
            for (int x = 0; x < gridModel.Width; x++)
            {
                for (int y = 0; y < gridModel.Height; y++)
                {
                    if (gridModel.cells[x, y] == CellType.Static)
                    {
                        CreateStaticTile(new Vector2Int(x, y), staticsContainer.transform, tileSize);
                    }
                }
            }
            
            // Создаем подвижные блоки
            foreach (var kvp in gridModel.Blocks)
            {
                var block = kvp.Value;
                CreateMovableBlock(block, blocksContainer.transform, tileSize);
            }
            
            // Создаем маркеры старта и цели
            CreateMarker("MarkerStart", new Vector2Int(1, 1), Color.green, tileSize);
            CreateMarker("MarkerGoal", new Vector2Int(gridModel.Width - 2, gridModel.Height - 2), Color.red, tileSize);
            
            Debug.Log($"✅ Создано визуальных объектов: статических тайлов, блоков, маркеров (размер тайла: {tileSize})");
        }
        
        /// <summary>
        /// Создает статический тайл
        /// </summary>
        private static void CreateStaticTile(Vector2Int pos, Transform parent, int tileSize)
        {
            var staticTile = new GameObject($"StaticTile_{pos.x}_{pos.y}");
            staticTile.transform.SetParent(parent);
            
            // Масштабируем позицию в соответствии с размером тайла
            float worldX = pos.x * (tileSize / 100f); // Конвертируем в мировые координаты
            float worldY = pos.y * (tileSize / 100f);
            staticTile.transform.position = new Vector3(worldX, worldY, 0);
            
            staticTile.AddComponent<StaticTile>();
            
            // Добавляем визуальный компонент
            var renderer = staticTile.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSquareSprite(tileSize);
            renderer.color = Color.gray;
        }
        
        /// <summary>
        /// Создает подвижный блок
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
        
        /// <summary>
        /// Вычисляет оптимальный размер тайла на основе размера сетки
        /// </summary>
        private static int CalculateOptimalTileSize(int width, int height)
        {
            // Базовый размер для сетки 8x8
            int baseSize = 64;
            
            // Для больших сеток уменьшаем размер тайла
            int maxDimension = Mathf.Max(width, height);
            
            if (maxDimension <= 8)
                return baseSize; // 64px
            else if (maxDimension <= 12)
                return 48; // 48px
            else if (maxDimension <= 16)
                return 40; // 40px
            else if (maxDimension <= 20)
                return 32; // 32px
            else if (maxDimension <= 25)
                return 28; // 28px
            else
                return 24; // 24px для очень больших сеток
        }
    }
}


