using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LabyrinthMover.Core;
using LabyrinthMover.Gameplay;

namespace LabyrinthMover.Core.Editor
{
    /// <summary>
    /// Кастомный редактор для статического генератора уровней
    /// </summary>
    public class EditorLevelGeneratorStaticEditor : EditorWindow
    {
        private int levelWidth = 20;
        private int levelHeight = 20;
        private int tileSize = 32;
        
        // Настройки генерации
        private float staticDensity = 0.20f;
        private int baseBlockCount = 10;
        private int blockCountVariation = 5;
        private float maxEnergy = 50f;
        
        // Промежуточное состояние генерации
        private GeneratedLevel currentLevel;
        private List<Vector2Int> currentPath;
        
        // Информация об энергии
        private int estimatedEnergy;
        private int estimatedMoves;
        
        [MenuItem("LabyrinthMover/Генератор уровней")]
        public static void ShowWindow()
        {
            var window = GetWindow<EditorLevelGeneratorStaticEditor>("Генератор уровней");
            window.minSize = new Vector2(350, 500);
            window.Show();
        }
        
        private void OnGUI()
        {
            GUILayout.Label("🎯 Генератор уровней (от обратного)", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            // Основные параметры уровня
            GUILayout.Label("📊 Параметры уровня:", EditorStyles.boldLabel);
            levelWidth = EditorGUILayout.IntField("Ширина:", levelWidth);
            levelHeight = EditorGUILayout.IntField("Высота:", levelHeight);
            
            GUILayout.Space(10);
            
            // Настройки генерации
            GUILayout.Label("⚙️ Настройки генерации:", EditorStyles.boldLabel);
            
            // Плотность статических препятствий
            staticDensity = EditorGUILayout.Slider("Плотность препятствий:", staticDensity, 0f, 0.5f);
            EditorGUILayout.HelpBox($"Плотность: {(staticDensity * 100):F1}% - {(staticDensity < 0.15f ? "Мало препятствий" : staticDensity < 0.25f ? "Средне препятствий" : "Много препятствий")}", MessageType.Info);
            
            // Количество блоков
            baseBlockCount = EditorGUILayout.IntSlider("Базовое кол-во блоков:", baseBlockCount, 5, 25);
            blockCountVariation = EditorGUILayout.IntSlider("Вариация блоков (±):", blockCountVariation, 0, 10);
            EditorGUILayout.HelpBox($"Блоков будет: {baseBlockCount - blockCountVariation}-{baseBlockCount + blockCountVariation} (базовое: {baseBlockCount})", MessageType.Info);
            
            // Максимальная энергия
            maxEnergy = EditorGUILayout.Slider("Макс. энергия:", maxEnergy, 20f, 100f);
            EditorGUILayout.HelpBox($"Максимальная энергия: {maxEnergy:F0} - {(maxEnergy < 40f ? "Легко" : maxEnergy < 60f ? "Средне" : "Сложно")}", MessageType.Info);
            
            GUILayout.Space(10);
            
            // Визуальные настройки
            GUILayout.Label("🎨 Визуальные настройки:", EditorStyles.boldLabel);
            tileSize = EditorGUILayout.IntSlider("Размер тайла:", tileSize, 16, 128);
            EditorGUILayout.HelpBox($"Размер тайла: {tileSize}px. Для сетки {levelWidth}x{levelHeight} рекомендуется размер {CalculateRecommendedTileSize(levelWidth, levelHeight)}px", MessageType.Info);
            
            GUILayout.Space(20);
            
            // Пошаговая генерация
            GUILayout.Label("🎯 Пошаговая генерация:", EditorStyles.boldLabel);
            
            GUI.enabled = !Application.isPlaying;
            
            // Шаг 1: Очистка
            if (GUILayout.Button("1️⃣ Удалить старый уровень", GUILayout.Height(30)))
            {
                ClearOldLevel();
            }
            
            // Шаг 2: Границы и точки входа
            if (GUILayout.Button("2️⃣ Сгенерировать границу и точки входа", GUILayout.Height(30)))
            {
                GenerateBordersAndPoints();
            }
            
            // Шаг 3: Статические препятствия (перенесено вверх)
            if (GUILayout.Button("3️⃣ Разместить статические препятствия", GUILayout.Height(30)))
            {
                PlaceStaticObstacles();
            }
            
            // Шаг 4: Путь с блоками
            if (GUILayout.Button("4️⃣ Сгенерировать путь (с блоками)", GUILayout.Height(30)))
            {
                GeneratePathWithBlocks();
            }
            
            // Шаг 5: Перемешивание блоков
            if (GUILayout.Button("5️⃣ Перемешать блоки (механика игры)", GUILayout.Height(30)))
            {
                ShuffleMovableBlocks();
            }
            
            GUILayout.Space(10);
            
            // Автоматическая генерация
            GUILayout.Label("⚡ Автоматическая генерация:", EditorStyles.boldLabel);
            if (GUILayout.Button("🎯 Сгенерировать весь уровень автоматически", GUILayout.Height(35)))
            {
                GenerateReverseLevel();
            }
            
            GUI.enabled = true;
            
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Генерация работает только в режиме редактора (не в Play mode)", MessageType.Warning);
            }
            
            GUILayout.Space(20);
            
            // Информация об энергии
            if (currentLevel != null)
            {
                GUILayout.Label("⚡ Информация об энергии:", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Оценка энергии:", estimatedEnergy.ToString());
                EditorGUILayout.LabelField("Оценка ходов:", estimatedMoves.ToString());
                EditorGUILayout.EndHorizontal();
                
                // Цветовая индикация сложности
                Color energyColor = GetEnergyColor(estimatedEnergy, maxEnergy);
                GUI.color = energyColor;
                EditorGUILayout.HelpBox($"Сложность: {GetDifficultyText(estimatedEnergy, maxEnergy)}", MessageType.Info);
                GUI.color = Color.white;
            
            GUILayout.Space(10);
            }
            
            // Информация
            GUILayout.Label("ℹ️ Информация:", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Генератор создает уровни 'от обратного' - сначала размещает статические препятствия, " +
                "затем находит путь от старта до финиша и размещает блоки по этому пути. " +
                "Финальный шаг использует механику игры - перемещает блоки на случайное количество ходов " +
                "(1-3) в случайных направлениях на расстояние 1-5 клеток, учитывая препятствия. " +
                "Гарантирует проходимость с первой попытки!", 
                MessageType.Info);
        }
        
        /// <summary>
        /// Шаг 1: Удаляет старый уровень
        /// </summary>
        private void ClearOldLevel()
        {
            Debug.Log("🗑️ Удаляем старый уровень...");
            EditorLevelGeneratorStatic.ClearExistingLevel();
            currentLevel = null;
            currentPath = null;
            Debug.Log("✅ Старый уровень удален");
        }
        
        /// <summary>
        /// Шаг 2: Генерирует границы и точки входа/выхода
        /// </summary>
        private void GenerateBordersAndPoints()
        {
            Debug.Log("🏗️ Генерируем границы и точки входа/выхода...");
            
            // Создаем новый уровень
            currentLevel = new GeneratedLevel
            {
                Params = new LevelGenerationParams
                {
                    Width = levelWidth,
                    Height = levelHeight,
                    StaticDensity = staticDensity,
                    BaseBlockCount = baseBlockCount,
                    BlockCountVariation = blockCountVariation,
                    Emax = maxEnergy,
                    Ksize = 1.0f,
                    Kdist = 0.1f,
                    TargetMinHops = Mathf.RoundToInt(maxEnergy / 5f),
                    TargetMinEnergy = Mathf.RoundToInt(maxEnergy * 0.6f)
                },
                IsSolvable = true
            };
            
            // Генерируем границы
            CreateLevelBorders(currentLevel);
            
            // Размещаем точки входа и выхода
            PlaceStartAndGoal(currentLevel);
            
            // Применяем к сцене
            ApplyLevelToScene(currentLevel);
            
            Debug.Log($"✅ Границы созданы: {currentLevel.StaticPositions.Count} статических тайлов");
            Debug.Log($"✅ Точки размещены: старт {currentLevel.StartPosition}, финиш {currentLevel.GoalPosition}");
            
            // Создаем персонажа в начале уровня
            CreatePlayerAtStart(currentLevel);
        }
        
        /// <summary>
        /// Создает персонажа в точке старта
        /// </summary>
        private void CreatePlayerAtStart(GeneratedLevel level)
        {
            Debug.Log("👤 Создаем персонажа в точке старта...");
            
            // Удаляем существующих персонажей
            var existingPlayers = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var player in existingPlayers)
            {
                if (player != null && player.gameObject != null)
                {
                    Object.DestroyImmediate(player.gameObject);
                }
            }
            
            // Создаем нового персонажа
            var playerObject = new GameObject("Player");
            var playerController = playerObject.AddComponent<PlayerController>();
            
            // Устанавливаем позицию персонажа
            Vector3 startWorldPos = new Vector3(level.StartPosition.x, level.StartPosition.y, 0f);
            playerObject.transform.position = startWorldPos;
            
            // Настраиваем персонажа
            playerController.SetGridPosition(level.StartPosition);
            
            Debug.Log($"✅ Персонаж создан в позиции {level.StartPosition}");
        }
        
        /// <summary>
        /// Шаг 4: Генерирует путь и размещает блоки по пути
        /// </summary>
        private void GeneratePathWithBlocks()
        {
            if (currentLevel == null)
            {
                Debug.LogError("❌ Сначала выполните предыдущие шаги: генерация границ, точек входа и статических препятствий!");
                return;
            }
            
            Debug.Log("🛤️ Генерируем путь и размещаем блоки...");
            
            // Вычисляем путь
            currentPath = FindPath(currentLevel.StartPosition, currentLevel.GoalPosition, currentLevel);
            
            if (currentPath == null || currentPath.Count == 0)
            {
                Debug.LogError("❌ Не удалось найти путь от старта до финиша!");
                return;
            }
            
            Debug.Log($"✅ Найден путь длиной {currentPath.Count} клеток");
            
            // Рассчитываем количество блоков
            CalculateRequiredBlocks(currentLevel, currentPath);
            
            // Размещаем блоки по пути
            PlaceMovableBlocksOnPath(currentLevel, currentPath);
            
            // Применяем к сцене (только обновляем блоки, не очищаем все)
            UpdateBlocksInScene(currentLevel);
            
            Debug.Log($"✅ Размещено {currentLevel.BlockPlacements.Count} блоков по пути");
            
            // Рассчитываем энергию после генерации пути
            CalculateLevelEnergy();
        }
        
        /// <summary>
        /// Шаг 5: Перемешивает подвижные блоки используя механику игры
        /// </summary>
        private void ShuffleMovableBlocks()
        {
            if (currentLevel == null || currentLevel.BlockPlacements.Count == 0)
            {
                Debug.LogError("❌ Сначала выполните шаг 4: генерация пути с блоками!");
                return;
            }
            
            Debug.Log("🎮 Перемешиваем блоки используя механику игры...");
            
            // Создаем копию исходных позиций блоков (это будет "решение")
            var solutionPositions = new List<Vector2Int>();
            foreach (var block in currentLevel.BlockPlacements)
            {
                solutionPositions.Add(block.Position);
            }
            
            Debug.Log($"🎯 Исходное решение: {solutionPositions.Count} блоков на своих местах");
            
            // Перемешиваем каждый блок, используя механику игры
            for (int i = 0; i < currentLevel.BlockPlacements.Count; i++)
            {
                var block = currentLevel.BlockPlacements[i];
                Debug.Log($"🎮 Перемешиваем блок {i + 1} из позиции {block.Position}");
                
                // Выполняем случайное количество ходов (1-3)
                int movesCount = UnityEngine.Random.Range(1, 4);
                Debug.Log($"   Выполняем {movesCount} ходов для блока {i + 1}");
                
                for (int move = 0; move < movesCount; move++)
                {
                    Vector2Int newPosition = TryMoveBlock(block, currentLevel);
                    if (newPosition != Vector2Int.zero)
                    {
                        block.Position = newPosition;
                        Debug.Log($"   Ход {move + 1}: блок {i + 1} перемещен в {newPosition}");
                    }
                    else
                    {
                        Debug.Log($"   Ход {move + 1}: блок {i + 1} не может двигаться дальше");
                        break; // Не можем двигаться дальше
                    }
                }
            }
            
            // Применяем к сцене (только обновляем блоки)
            UpdateBlocksInScene(currentLevel);
            
            Debug.Log($"✅ Блоки перемешаны с использованием механики игры!");
            Debug.Log($"🎯 Решение: переместить блоки в позиции {string.Join(", ", solutionPositions)}");
            
            // Выводим пошаговую инструкцию для игрока
            Debug.Log("📋 ИНСТРУКЦИЯ ДЛЯ ИГРОКА:");
            Debug.Log("   Чтобы решить уровень, переместите блоки в следующем порядке:");
            for (int i = 0; i < solutionPositions.Count; i++)
            {
                Debug.Log($"   Шаг {i + 1}: Переместите блок {i + 1} в позицию {solutionPositions[i]}");
            }
            Debug.Log("   🎯 Цель: добраться от старта (зеленый) до финиша (красный)!");
            
            // Пересчитываем энергию после перемешивания
            CalculateLevelEnergy();
        }
        
        /// <summary>
        /// Пытается переместить блок в случайном направлении на случайное расстояние
        /// </summary>
        private Vector2Int TryMoveBlock(BlockPlacement block, GeneratedLevel level)
        {
            // Возможные направления движения
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            // Выбираем случайное направление
            Vector2Int direction = directions[UnityEngine.Random.Range(0, directions.Length)];
            
            // Выбираем случайное расстояние (1-5 клеток)
            int distance = UnityEngine.Random.Range(1, 6);
            
            Debug.Log($"   Пытаемся переместить блок на {distance} клеток в направлении {direction}");
            
            // Пытаемся найти свободную позицию в выбранном направлении
            for (int step = 1; step <= distance; step++)
            {
                Vector2Int newPos = block.Position + (direction * step);
                
                // Проверяем, можно ли переместиться в эту позицию
                if (CanMoveBlockTo(block, newPos, level))
                {
                    Debug.Log($"   Найдена свободная позиция на расстоянии {step}: {newPos}");
                    return newPos;
                }
            }
            
            Debug.Log($"   Не удалось найти свободную позицию в направлении {direction}");
            return Vector2Int.zero; // Не можем двигаться
        }
        
        /// <summary>
        /// Проверяет, можно ли переместить блок в указанную позицию
        /// </summary>
        private bool CanMoveBlockTo(BlockPlacement block, Vector2Int newPos, GeneratedLevel level)
        {
            // Проверяем границы уровня
            if (newPos.x < 0 || newPos.x >= level.Params.Width || 
                newPos.y < 0 || newPos.y >= level.Params.Height)
            {
                return false;
            }
            
            // Проверяем статические препятствия
            if (level.StaticPositions.Contains(newPos))
            {
                return false;
            }
            
            // Проверяем, не занята ли позиция другим блоком
            foreach (var otherBlock in level.BlockPlacements)
            {
                if (otherBlock != block && otherBlock.Position == newPos)
            {
                    return false;
                }
            }
            
            // Проверяем, не на старте или финише
            if (newPos == level.StartPosition || newPos == level.GoalPosition)
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Шаг 3: Размещает статические препятствия
        /// </summary>
        private void PlaceStaticObstacles()
        {
            if (currentLevel == null)
            {
                Debug.LogError("❌ Сначала выполните шаг 2: генерация границ и точек входа!");
                return;
            }
            
            Debug.Log("🧱 Размещаем статические препятствия...");
            
            // Размещаем препятствия
            PlaceStaticObstaclesInLevel(currentLevel);
            
            // Применяем к сцене (только обновляем статические препятствия)
            UpdateStaticObstaclesInScene(currentLevel);
            
            Debug.Log($"✅ Размещено {currentLevel.StaticPositions.Count} статических препятствий");
        }
        
        /// <summary>
        /// Генерирует уровень от обратного с гарантированной проходимостью
        /// </summary>
        private void GenerateReverseLevel()
        {
            Debug.Log($"🎯 Генерация уровня от обратного:");
            Debug.Log($"   - Размер: {levelWidth}x{levelHeight}");
            Debug.Log($"   - Плотность препятствий: {(staticDensity * 100):F1}%");
            Debug.Log($"   - Базовое количество блоков: {baseBlockCount}");
            Debug.Log($"   - Вариация блоков: ±{blockCountVariation}");
            Debug.Log($"   - Макс. энергия: {maxEnergy:F0}");
            
            // Создаем параметры с пользовательскими настройками
            var parameters = new LevelGenerationParams
            {
                Width = levelWidth,
                Height = levelHeight,
                StaticDensity = staticDensity,
                BaseBlockCount = baseBlockCount,
                BlockCountVariation = blockCountVariation,
                Emax = maxEnergy,
                Ksize = 1.0f,
                Kdist = 0.1f,
                TargetMinHops = Mathf.RoundToInt(maxEnergy / 5f),
                TargetMinEnergy = Mathf.RoundToInt(maxEnergy * 0.6f)
            };
            
            // Генерируем уровень от обратного
            var generatedLevel = ReverseLevelGenerator.GenerateLevel(parameters);
            
            if (generatedLevel != null)
            {
                // Применяем уровень к сцене
                EditorLevelGeneratorStatic.ApplyLevelToSceneWithTileSize(generatedLevel, tileSize);
                Debug.Log("🎯 Генерация от обратного завершена успешно!");
                
                // Сохраняем уровень для расчета энергии
                currentLevel = generatedLevel;
                currentPath = new List<Vector2Int>(); // Создаем пустой путь для базового расчета
                
                // Создаем персонажа в точке старта
                CreatePlayerAtStart(generatedLevel);
                
                // Рассчитываем энергию
                CalculateLevelEnergy();
            }
            else
            {
                Debug.LogError("❌ Не удалось сгенерировать уровень от обратного!");
            }
        }
        
        /// <summary>
        /// Применяет уровень к сцене
        /// </summary>
        private void ApplyLevelToScene(GeneratedLevel level)
        {
            EditorLevelGeneratorStatic.ApplyLevelToSceneWithTileSize(level, tileSize);
        }
        
        /// <summary>
        /// Обновляет только блоки в сцене (не очищает границы)
        /// </summary>
        private void UpdateBlocksInScene(GeneratedLevel level)
        {
            Debug.Log("🔧 Обновляем только блоки в сцене...");
            
            // Удаляем только контейнер блоков
            var blocksContainer = GameObject.Find("Blocks");
            if (blocksContainer != null)
            {
                Object.DestroyImmediate(blocksContainer);
                Debug.Log("🗑️ Удален контейнер Blocks");
            }
            
            // Создаем новый контейнер блоков
            var newBlocksContainer = new GameObject("Blocks");
            
            // Создаем блоки из GeneratedLevel.BlockPlacements
            for (int i = 0; i < level.BlockPlacements.Count; i++)
            {
                var placement = level.BlockPlacements[i];
                var blockRt = new BlockRt(i, new RectInt(placement.Position.x, placement.Position.y, placement.Size.x, placement.Size.y));
                CreateMovableBlock(blockRt, newBlocksContainer.transform, tileSize);
            }
            
            Debug.Log($"✅ Обновлено {level.BlockPlacements.Count} блоков");
        }
        
        /// <summary>
        /// Обновляет только статические препятствия в сцене (не очищает границы)
        /// </summary>
        private void UpdateStaticObstaclesInScene(GeneratedLevel level)
        {
            Debug.Log("🧱 Обновляем только статические препятствия в сцене...");
            
            // Удаляем только контейнер статических объектов
            var staticsContainer = GameObject.Find("Statics");
            if (staticsContainer != null)
            {
                Object.DestroyImmediate(staticsContainer);
                Debug.Log("🗑️ Удален контейнер Statics");
            }
            
            // Создаем новый контейнер статических объектов
            var newStaticsContainer = new GameObject("Statics");
            
            // Создаем все статические тайлы (включая границы)
            for (int x = 0; x < level.Params.Width; x++)
            {
                for (int y = 0; y < level.Params.Height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    if (level.StaticPositions.Contains(pos))
                    {
                        CreateStaticTile(pos, newStaticsContainer.transform, tileSize);
                    }
                }
            }
            
            Debug.Log($"✅ Обновлено {level.StaticPositions.Count} статических препятствий");
        }
        
        /// <summary>
        /// Создает границы уровня
        /// </summary>
        private void CreateLevelBorders(GeneratedLevel level)
        {
            int width = level.Params.Width;
            int height = level.Params.Height;
            
            // Верхняя и нижняя границы
            for (int x = 0; x < width; x++)
            {
                level.StaticPositions.Add(new Vector2Int(x, 0));           // Нижняя граница
                level.StaticPositions.Add(new Vector2Int(x, height - 1));   // Верхняя граница
            }
            
            // Левая и правая границы
            for (int y = 1; y < height - 1; y++)
            {
                level.StaticPositions.Add(new Vector2Int(0, y));           // Левая граница
                level.StaticPositions.Add(new Vector2Int(width - 1, y));    // Правая граница
            }
        }
        
        /// <summary>
        /// Размещает точки старта и финиша
        /// </summary>
        private void PlaceStartAndGoal(GeneratedLevel level)
        {
            int width = level.Params.Width;
            int height = level.Params.Height;
            
            // Старт всегда в левом нижнем углу (внутри границ)
            level.StartPosition = new Vector2Int(1, 1);
            
            // Финиш всегда в правом верхнем углу (внутри границ)
            level.GoalPosition = new Vector2Int(width - 2, height - 2);
        }
        
        /// <summary>
        /// Находит путь от старта до финиша
        /// </summary>
        private List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, GeneratedLevel level)
        {
            // Используем простой алгоритм поиска пути (A*)
            var openSet = new List<Vector2Int> { start };
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float> { { start, 0 } };
            var fScore = new Dictionary<Vector2Int, float> { { start, Heuristic(start, goal) } };
            
            while (openSet.Count > 0)
            {
                // Находим узел с наименьшим fScore
                var current = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (fScore.ContainsKey(openSet[i]) && fScore[openSet[i]] < fScore[current])
                    {
                        current = openSet[i];
                    }
                }
                
                if (current == goal)
                {
                    // Восстанавливаем путь
                    return ReconstructPath(cameFrom, current);
                }
                
                openSet.Remove(current);
                
                // Проверяем соседние клетки
                var neighbors = GetNeighbors(current, level);
                foreach (var neighbor in neighbors)
                {
                    float tentativeGScore = gScore[current] + 1;
                    
                    if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = tentativeGScore + Heuristic(neighbor, goal);
                        
                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }
            
            return null; // Путь не найден
        }
        
        /// <summary>
        /// Получает соседние клетки для A*
        /// </summary>
        private List<Vector2Int> GetNeighbors(Vector2Int pos, GeneratedLevel level)
        {
            var neighbors = new List<Vector2Int>();
            var directions = new Vector2Int[] 
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };
            
            foreach (var dir in directions)
            {
                var neighbor = pos + dir;
                
                // Проверяем границы сетки
                if (neighbor.x < 0 || neighbor.x >= level.Params.Width ||
                    neighbor.y < 0 || neighbor.y >= level.Params.Height)
                    continue;
                
                // Теперь проверяем статические препятствия, так как они размещаются до генерации пути
                if (level.StaticPositions.Contains(neighbor))
                    continue;
                
                neighbors.Add(neighbor);
            }
            
            return neighbors;
        }
        
        /// <summary>
        /// Эвристическая функция для A*
        /// </summary>
        private float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
        
        /// <summary>
        /// Восстанавливает путь из A*
        /// </summary>
        private List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
        {
            var path = new List<Vector2Int> { current };
            
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            
            return path;
        }
        
        /// <summary>
        /// Рассчитывает необходимое количество блоков
        /// </summary>
        private void CalculateRequiredBlocks(GeneratedLevel level, List<Vector2Int> path)
        {
            // Минимальное количество блоков = длина пути - 1 (старт не нужен)
            int minBlocks = Mathf.Max(1, path.Count - 1);
            
            // Добавляем блоки для сложности на основе параметров
            int additionalBlocks = UnityEngine.Random.Range(0, level.Params.BlockCountVariation + 1);
            
            // Используем базовое количество блоков как минимум
            int totalBlocks = Mathf.Max(minBlocks, level.Params.BaseBlockCount) + additionalBlocks;
            
            level.Params.BlockCount = totalBlocks;
            
            Debug.Log($"📊 Длина пути: {path.Count}");
            Debug.Log($"📊 Минимальных блоков: {minBlocks}");
            Debug.Log($"📊 Базовое количество: {level.Params.BaseBlockCount}");
            Debug.Log($"📊 Дополнительных блоков: {additionalBlocks}");
            Debug.Log($"📊 Итого блоков: {level.Params.BlockCount}");
        }
        
        /// <summary>
        /// Размещает подвижные блоки по пути
        /// </summary>
        private void PlaceMovableBlocksOnPath(GeneratedLevel level, List<Vector2Int> path)
        {
            // Размещаем блоки на пути (кроме старта и финиша)
            var pathPositions = new List<Vector2Int>(path);
            pathPositions.Remove(level.StartPosition);
            pathPositions.Remove(level.GoalPosition);
            
            int blocksPlaced = 0;
            int maxBlocks = level.Params.BlockCount;
            
            // Перемешиваем позиции пути
            for (int i = 0; i < pathPositions.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, pathPositions.Count);
                var temp = pathPositions[i];
                pathPositions[i] = pathPositions[randomIndex];
                pathPositions[randomIndex] = temp;
            }
            
            // Размещаем блоки по пути (сколько поместится)
            int blocksOnPath = Mathf.Min(maxBlocks, pathPositions.Count);
            for (int i = 0; i < blocksOnPath; i++)
            {
                var pos = pathPositions[i];
                var blockSize = level.Params.BlockSizes[UnityEngine.Random.Range(0, level.Params.BlockSizes.Count)];
                
                var placement = new BlockPlacement
                {
                    Position = pos,
                    Size = blockSize
                };
                
                level.BlockPlacements.Add(placement);
                blocksPlaced++;
            }
            
            Debug.Log($"✅ Размещено {blocksPlaced} блоков по пути из {maxBlocks} запланированных");
        }
        
        /// <summary>
        /// Получает доступные позиции для размещения блоков
        /// </summary>
        private List<Vector2Int> GetAvailablePositions(GeneratedLevel level)
        {
            var available = new List<Vector2Int>();
            
            for (int x = 1; x < level.Params.Width - 1; x++)
            {
                for (int y = 1; y < level.Params.Height - 1; y++)
                {
                    var pos = new Vector2Int(x, y);
                    
                    // Пропускаем старт и финиш
                    if (pos == level.StartPosition || pos == level.GoalPosition)
                        continue;
                    
                    // Пропускаем статические препятствия
                    if (level.StaticPositions.Contains(pos))
                        continue;
                    
                    // Пропускаем уже занятые позиции блоков
                    bool occupied = false;
                    foreach (var block in level.BlockPlacements)
                    {
                        if (block.Position == pos)
                        {
                            occupied = true;
                            break;
                        }
                    }
                    
                    if (!occupied)
                    {
                        available.Add(pos);
                    }
                }
            }
            
            return available;
        }
        
        /// <summary>
        /// Размещает статические препятствия
        /// </summary>
        private void PlaceStaticObstaclesInLevel(GeneratedLevel level)
        {
            var availablePositions = GetAvailablePositions(level);
            
            // Рассчитываем количество препятствий на основе внутренней области (без границ)
            int innerWidth = level.Params.Width - 2; // Исключаем границы
            int innerHeight = level.Params.Height - 2; // Исключаем границы
            int innerArea = innerWidth * innerHeight;
            
            // Вычитаем позиции старта, финиша и блоков
            int occupiedPositions = 2 + level.BlockPlacements.Count; // старт + финиш + блоки
            int freePositions = innerArea - occupiedPositions;
            
            int targetObstacles = Mathf.RoundToInt(freePositions * level.Params.StaticDensity);
            targetObstacles = Mathf.Min(targetObstacles, availablePositions.Count);
            
            // Перемешиваем доступные позиции
            for (int i = 0; i < availablePositions.Count; i++)
            {
                int randomIndex = UnityEngine.Random.Range(i, availablePositions.Count);
                var temp = availablePositions[i];
                availablePositions[i] = availablePositions[randomIndex];
                availablePositions[randomIndex] = temp;
            }
            
            // Размещаем препятствия
            for (int i = 0; i < targetObstacles && i < availablePositions.Count; i++)
            {
                level.StaticPositions.Add(availablePositions[i]);
            }
        }
        
        /// <summary>
        /// Создает подвижный блок
        /// </summary>
        private void CreateMovableBlock(BlockRt block, Transform parent, int tileSize)
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
        /// Создает статический тайл
        /// </summary>
        private void CreateStaticTile(Vector2Int pos, Transform parent, int tileSize)
        {
            var staticTile = new GameObject($"StaticTile_{pos.x}_{pos.y}");
            staticTile.transform.SetParent(parent);
            
            // Масштабируем позицию в соответствии с размером тайла
            float worldX = pos.x * (tileSize / 100f);
            float worldY = pos.y * (tileSize / 100f);
            staticTile.transform.position = new Vector3(worldX, worldY, 0);
            
            staticTile.AddComponent<StaticTile>();
            
            // Добавляем визуальный компонент
            var renderer = staticTile.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSquareSprite(tileSize);
            renderer.color = Color.gray;
        }
        
        /// <summary>
        /// Создает квадратный спрайт с настраиваемым размером
        /// </summary>
        private Sprite CreateSquareSprite(int size)
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
        /// Рассчитывает энергию, необходимую для прохождения уровня
        /// </summary>
        private void CalculateLevelEnergy()
        {
            if (currentLevel == null || currentPath == null)
            {
                estimatedEnergy = 0;
                estimatedMoves = 0;
                return;
            }
            
            Debug.Log("⚡ Рассчитываем энергию для прохождения уровня...");
            
            // Базовая энергия = количество ходов по пути
            int baseEnergy = currentPath.Count;
            
            // Дополнительная энергия за статические препятствия
            int obstaclePenalty = currentLevel.StaticPositions.Count / 4; // Каждое 4-е препятствие добавляет 1 энергию
            
            // Дополнительная энергия за количество блоков (сложность манипуляций)
            int blockComplexity = currentLevel.BlockPlacements.Count / 2; // Каждые 2 блока добавляют 1 энергию
            
            // Итоговая энергия
            estimatedEnergy = baseEnergy + obstaclePenalty + blockComplexity;
            estimatedMoves = currentPath.Count;
            
            Debug.Log($"⚡ Расчет энергии:");
            Debug.Log($"   - Базовая энергия (путь): {baseEnergy}");
            Debug.Log($"   - Штраф за препятствия: {obstaclePenalty}");
            Debug.Log($"   - Сложность блоков: {blockComplexity}");
            Debug.Log($"   - Итого энергии: {estimatedEnergy}");
            Debug.Log($"   - Количество ходов: {estimatedMoves}");
        }
        
        /// <summary>
        /// Получает цвет для индикации сложности
        /// </summary>
        private Color GetEnergyColor(int energy, float maxEnergy)
        {
            float ratio = energy / maxEnergy;
            
            if (ratio <= 0.4f)
                return Color.green; // Легко
            else if (ratio <= 0.7f)
                return Color.yellow; // Средне
            else
                return Color.red; // Сложно
        }
        
        /// <summary>
        /// Получает текстовое описание сложности
        /// </summary>
        private string GetDifficultyText(int energy, float maxEnergy)
        {
            float ratio = energy / maxEnergy;
            
            if (ratio <= 0.4f)
                return $"Легко ({energy}/{maxEnergy:F0}) - энергия в пределах нормы";
            else if (ratio <= 0.7f)
                return $"Средне ({energy}/{maxEnergy:F0}) - умеренная сложность";
            else if (ratio <= 1.0f)
                return $"Сложно ({energy}/{maxEnergy:F0}) - требует мастерства";
            else
                return $"Очень сложно ({energy}/{maxEnergy:F0}) - может быть непроходимо!";
        }
        
        /// <summary>
        /// Вычисляет рекомендуемый размер тайла для заданных размеров сетки
        /// </summary>
        private int CalculateRecommendedTileSize(int width, int height)
        {
            int maxDimension = Mathf.Max(width, height);
            
            if (maxDimension <= 8)
                return 64;
            else if (maxDimension <= 12)
                return 48;
            else if (maxDimension <= 16)
                return 40;
            else if (maxDimension <= 20)
                return 32;
            else if (maxDimension <= 25)
                return 28;
            else
                return 24;
        }
    }
}
