using System;
using System.Collections.Generic;
using UnityEngine;

namespace LabyrinthMover.Core
{
    /// <summary>
    /// Генератор уровней "от обратного" - гарантированно создает проходимые уровни
    /// </summary>
    public static class ReverseLevelGenerator
    {
        /// <summary>
        /// Генерирует уровень с гарантированной проходимостью
        /// </summary>
        public static GeneratedLevel GenerateLevel(LevelGenerationParams parameters, int seed = -1)
        {
            if (seed == -1)
                seed = UnityEngine.Random.Range(0, int.MaxValue);
                
            UnityEngine.Random.InitState(seed);
            
            Debug.Log($"🎯 Начинаем генерацию уровня {parameters.Width}x{parameters.Height} от обратного (seed: {seed})");
            Debug.Log($"📊 Параметры: плотность {parameters.StaticDensity:F2}, базовых блоков {parameters.BaseBlockCount}, вариация ±{parameters.BlockCountVariation}");
            
            var level = new GeneratedLevel
            {
                Seed = seed,
                Params = parameters,
                IsSolvable = true // Гарантированно проходимый!
            };

            try
            {
                // Шаг 1: Генерируем область уровня с границами
                GenerateLevelArea(level);
                
                // Шаг 2: Размещаем точки входа и выхода
                PlaceStartAndGoal(level);
                
                // Шаг 3: Вычисляем минимальный путь
                var path = CalculateMinimumPath(level);
                
                // Шаг 4: Вычисляем необходимое количество блоков для прохождения
                CalculateRequiredBlocks(level, path);
                
                // Шаг 5: Размещаем подвижные блоки по пути
                PlaceMovableBlocksOnPath(level, path);
                
                // Шаг 6: Добавляем дополнительные блоки для сложности
                AddAdditionalBlocks(level);
                
                // Шаг 7: Размещаем статические препятствия
                PlaceStaticObstacles(level);
                
                // Шаг 8: Валидируем и оцениваем уровень
                ValidateAndRateLevel(level);
                
                Debug.Log($"✅ Уровень успешно сгенерирован от обратного!");
                Debug.Log($"   - Путь: {path.Count} клеток");
                Debug.Log($"   - Блоков по пути: {level.BlockPlacements.Count}");
                Debug.Log($"   - Препятствий: {level.StaticPositions.Count}");
                Debug.Log($"   - Сложность: {level.Difficulty}");
                Debug.Log($"   - Старт: {level.StartPosition}, Финиш: {level.GoalPosition}");
                
                // Детальная информация о блоках
                Debug.Log($"🔍 Детали блоков:");
                for (int i = 0; i < level.BlockPlacements.Count; i++)
                {
                    var block = level.BlockPlacements[i];
                    Debug.Log($"   Блок {i + 1}: позиция {block.Position}, размер {block.Size}");
                }
                
                return level;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Ошибка при генерации уровня от обратного: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Генерирует область уровня с границами
        /// </summary>
        private static void GenerateLevelArea(GeneratedLevel level)
        {
            Debug.Log("🏗️ Генерируем область уровня...");
            
            // Создаем пустую область
            level.StaticPositions.Clear();
            level.BlockPlacements.Clear();
            
            // Добавляем границы (стены по периметру)
            CreateLevelBorders(level);
            
            Debug.Log($"✅ Область создана: {level.Params.Width}x{level.Params.Height}");
        }
        
        /// <summary>
        /// Создает границы уровня (стены по периметру)
        /// </summary>
        private static void CreateLevelBorders(GeneratedLevel level)
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
            
            Debug.Log($"🔲 Созданы границы: {level.StaticPositions.Count} статических тайлов");
        }
        
        /// <summary>
        /// Размещает точки входа и выхода
        /// </summary>
        private static void PlaceStartAndGoal(GeneratedLevel level)
        {
            int width = level.Params.Width;
            int height = level.Params.Height;
            
            // Старт всегда в левом нижнем углу (внутри границ)
            level.StartPosition = new Vector2Int(1, 1);
            
            // Финиш всегда в правом верхнем углу (внутри границ)
            level.GoalPosition = new Vector2Int(width - 2, height - 2);
            
            Debug.Log($"🚀 Старт: {level.StartPosition}, Финиш: {level.GoalPosition}");
        }
        
        /// <summary>
        /// Вычисляет минимальный путь от старта до финиша
        /// </summary>
        private static List<Vector2Int> CalculateMinimumPath(GeneratedLevel level)
        {
            Debug.Log("🛤️ Вычисляем минимальный путь...");
            
            // Используем простой алгоритм поиска пути (A* или BFS)
            var path = FindPath(level.StartPosition, level.GoalPosition, level);
            
            if (path == null || path.Count == 0)
            {
                Debug.LogError("❌ Не удалось найти путь от старта до финиша!");
                return new List<Vector2Int>();
            }
            
            Debug.Log($"✅ Найден путь длиной {path.Count} клеток");
            return path;
        }
        
        /// <summary>
        /// Находит путь от старта до финиша используя алгоритм A*
        /// </summary>
        private static List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, GeneratedLevel level)
        {
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
        private static List<Vector2Int> GetNeighbors(Vector2Int pos, GeneratedLevel level)
        {
            var neighbors = new List<Vector2Int>();
            var directions = new Vector2Int[] 
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };
            
            foreach (var dir in directions)
            {
                var neighbor = pos + dir;
                
                // Проверяем границы
                if (neighbor.x < 0 || neighbor.x >= level.Params.Width ||
                    neighbor.y < 0 || neighbor.y >= level.Params.Height)
                    continue;
                
                // Проверяем, что это не статическое препятствие
                if (level.StaticPositions.Contains(neighbor))
                    continue;
                
                neighbors.Add(neighbor);
            }
            
            return neighbors;
        }
        
        /// <summary>
        /// Эвристическая функция для A*
        /// </summary>
        private static float Heuristic(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
        
        /// <summary>
        /// Восстанавливает путь из A*
        /// </summary>
        private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
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
        /// Вычисляет необходимое количество блоков для прохождения
        /// </summary>
        private static void CalculateRequiredBlocks(GeneratedLevel level, List<Vector2Int> path)
        {
            Debug.Log("🧮 Вычисляем необходимое количество блоков...");
            
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
        private static void PlaceMovableBlocksOnPath(GeneratedLevel level, List<Vector2Int> path)
        {
            Debug.Log("🎯 Размещаем подвижные блоки по пути...");
            
            // Размещаем блоки на пути (кроме старта и финиша)
            var pathPositions = new List<Vector2Int>(path);
            pathPositions.Remove(level.StartPosition);
            pathPositions.Remove(level.GoalPosition);
            
            Debug.Log($"🛤️ Позиций пути для блоков: {pathPositions.Count}");
            
            int blocksPlaced = 0;
            int maxBlocks = level.Params.BlockCount; // Используем полное количество блоков
            
            Debug.Log($"🎯 Нужно разместить блоков: {maxBlocks}");
            Debug.Log($"🎯 Доступных позиций пути: {pathPositions.Count}");
            
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
                
                Debug.Log($"🎯 Размещен блок {blocksPlaced} по пути в позиции {pos}");
            }
            
            Debug.Log($"✅ Размещено {blocksPlaced} блоков по пути из {maxBlocks} запланированных");
        }
        
        /// <summary>
        /// Добавляет дополнительные блоки для сложности
        /// </summary>
        private static void AddAdditionalBlocks(GeneratedLevel level)
        {
            Debug.Log("🎲 Добавляем дополнительные блоки...");
            
            int additionalBlocks = level.Params.BlockCount - level.BlockPlacements.Count;
            Debug.Log($"🎲 Нужно добавить дополнительных блоков: {additionalBlocks}");
            
            if (additionalBlocks <= 0) 
            {
                Debug.Log("✅ Дополнительные блоки не нужны");
                return;
            }
            
            var availablePositions = GetAvailablePositions(level);
            Debug.Log($"🎲 Доступных позиций для дополнительных блоков: {availablePositions.Count}");
            
            for (int i = 0; i < additionalBlocks && availablePositions.Count > 0; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, availablePositions.Count);
                var pos = availablePositions[randomIndex];
                availablePositions.RemoveAt(randomIndex);
                
                var blockSize = level.Params.BlockSizes[UnityEngine.Random.Range(0, level.Params.BlockSizes.Count)];
                
                var placement = new BlockPlacement
                {
                    Position = pos,
                    Size = blockSize
                };
                
                level.BlockPlacements.Add(placement);
                
                Debug.Log($"🎲 Добавлен дополнительный блок {i + 1} в позиции {pos}");
            }
            
            Debug.Log($"✅ Добавлено {Mathf.Min(additionalBlocks, availablePositions.Count)} дополнительных блоков");
            Debug.Log($"📊 Итого блоков в уровне: {level.BlockPlacements.Count}");
        }
        
        /// <summary>
        /// Получает доступные позиции для размещения блоков
        /// </summary>
        private static List<Vector2Int> GetAvailablePositions(GeneratedLevel level)
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
        private static void PlaceStaticObstacles(GeneratedLevel level)
        {
            Debug.Log("🧱 Размещаем статические препятствия...");
            
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
            
            Debug.Log($"🧱 Внутренняя область: {innerArea} клеток");
            Debug.Log($"🧱 Занятых позиций: {occupiedPositions}");
            Debug.Log($"🧱 Свободных позиций: {freePositions}");
            Debug.Log($"🧱 Целевых препятствий: {targetObstacles}");
            
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
            
            Debug.Log($"✅ Размещено {targetObstacles} статических препятствий");
        }
        
        /// <summary>
        /// Валидирует и оценивает уровень
        /// </summary>
        private static void ValidateAndRateLevel(GeneratedLevel level)
        {
            Debug.Log("📊 Валидируем и оцениваем уровень...");
            
            // Проверяем проходимость
            var testPath = FindPath(level.StartPosition, level.GoalPosition, level);
            level.IsSolvable = testPath != null && testPath.Count > 0;
            
            if (!level.IsSolvable)
            {
                Debug.LogError("❌ Уровень не проходим после генерации!");
                return;
            }
            
            // Оцениваем сложность
            level.EstimatedMinHops = testPath.Count;
            level.EstimatedMinEnergy = Mathf.RoundToInt(testPath.Count * 1.5f);
            
            // Определяем сложность
            if (level.EstimatedMinEnergy <= 20)
                level.Difficulty = LevelDifficulty.Easy;
            else if (level.EstimatedMinEnergy <= 50)
                level.Difficulty = LevelDifficulty.Medium;
            else
                level.Difficulty = LevelDifficulty.Hard;
            
            Debug.Log($"✅ Уровень валиден: {level.EstimatedMinHops} ходов, {level.EstimatedMinEnergy} энергии, сложность: {level.Difficulty}");
        }
    }
}
