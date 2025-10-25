using System;
using System.Collections.Generic;
using UnityEngine;

namespace LabyrinthMover.Core
{
    /// <summary>
    /// Параметры генерации уровня
    /// </summary>
    [Serializable]
    public class LevelGenerationParams
    {
        [Header("Размеры поля")]
        public int Width = 8;
        public int Height = 8;
        
        [Header("Статические препятствия")]
        [Range(0f, 0.5f)]
        public float StaticDensity = 0.2f;
        
        [Header("Подвижные блоки")]
        public List<Vector2Int> BlockSizes = new List<Vector2Int> { Vector2Int.one, new Vector2Int(2, 1), new Vector2Int(1, 2) };
        public int BaseBlockCount = 10; // Базовое количество блоков для прохождения
        public int BlockCountVariation = 5; // Вариация ±5 блоков
        public int BlockCount = 10; // Фактическое количество блоков (вычисляется при генерации)
        
        [Header("Энергетические параметры")]
        public float Ksize = 1.0f;
        public float Kdist = 0.1f;
        public float Emax = 40f;
        
        [Header("Профили сложности")]
        public int TargetMinHops = 6;  // Hmin
        public int TargetMinEnergy = 12; // Emin
    }

    /// <summary>
    /// Результат генерации уровня
    /// </summary>
    [Serializable]
    public class GeneratedLevel
    {
        public int Seed;
        public LevelGenerationParams Params;
        public Vector2Int StartPosition;
        public Vector2Int GoalPosition;
        public List<Vector2Int> StaticPositions = new List<Vector2Int>();
        public List<BlockPlacement> BlockPlacements = new List<BlockPlacement>();
        public LevelDifficulty Difficulty;
        public int EstimatedMinEnergy;
        public int EstimatedMinHops;
        public bool IsSolvable;
    }

    /// <summary>
    /// Размещение блока на уровне
    /// </summary>
    [Serializable]
    public class BlockPlacement
    {
        public Vector2Int Position;
        public Vector2Int Size;
        public int Id;
    }


    /// <summary>
    /// Генератор уровней с валидатором
    /// </summary>
    public static class LevelGenerator
    {
        private static System.Random random;

        /// <summary>
        /// Генерирует уровень с заданными параметрами
        /// </summary>
        public static GeneratedLevel GenerateLevel(LevelGenerationParams parameters, int seed = -1)
        {
            if (seed == -1)
                seed = UnityEngine.Random.Range(0, int.MaxValue);
                
            random = new System.Random(seed);
            
            // Вычисляем фактическое количество блоков
            CalculateBlockCount(parameters);
            
            var level = new GeneratedLevel
            {
                Seed = seed,
                Params = parameters,
                IsSolvable = false
            };

            // Попытки генерации (максимум 5 попыток для быстрой работы)
            for (int attempt = 0; attempt < 5; attempt++)
            {
                if (TryGenerateLevel(level))
                {
                    level.IsSolvable = true;
                    break;
                }
            }

            return level;
        }
        
        /// <summary>
        /// Вычисляет фактическое количество блоков на основе базового количества и вариации
        /// </summary>
        private static void CalculateBlockCount(LevelGenerationParams parameters)
        {
            parameters.BlockCount = parameters.BaseBlockCount + UnityEngine.Random.Range(-parameters.BlockCountVariation, parameters.BlockCountVariation + 1);
        }

        private static bool TryGenerateLevel(GeneratedLevel level)
        {
            var parameters = level.Params;
            var startTime = Time.realtimeSinceStartup;
            const float maxGenerationTime = 2.0f; // Максимум 2 секунды на генерацию
            
            Debug.Log($"Попытка генерации уровня: {parameters.Width}x{parameters.Height}");
            
            // 1. Выбор позиций A и B
            if (!SelectStartAndGoal(level))
            {
                Debug.LogWarning("Не удалось выбрать позиции A и B");
                return false;
            }
            Debug.Log($"Выбраны позиции: A={level.StartPosition}, B={level.GoalPosition}");

            // Проверяем таймаут
            if (Time.realtimeSinceStartup - startTime > maxGenerationTime)
            {
                Debug.LogWarning("❌ Таймаут генерации: выбор позиций занял слишком много времени");
                return false;
            }

            // 2. Раскладка статических препятствий
            GenerateStaticObstacles(level);
            Debug.Log($"Размещено статических препятствий: {level.StaticPositions.Count}");

            // Проверяем таймаут
            if (Time.realtimeSinceStartup - startTime > maxGenerationTime)
            {
                Debug.LogWarning("❌ Таймаут генерации: размещение препятствий заняло слишком много времени");
                return false;
            }

            // 3. Расстановка подвижных блоков
            if (!PlaceMovableBlocks(level))
            {
                Debug.LogWarning("Не удалось разместить подвижные блоки");
                return false;
            }
            Debug.Log($"Размещено подвижных блоков: {level.BlockPlacements.Count}");

            // Проверяем таймаут
            if (Time.realtimeSinceStartup - startTime > maxGenerationTime)
            {
                Debug.LogWarning("❌ Таймаут генерации: размещение блоков заняло слишком много времени");
                return false;
            }

            // 4. Валидация и оценка сложности
            bool isValid = ValidateAndRateLevel(level);
            Debug.Log($"Валидация уровня: {(isValid ? "Успешно" : "Неудачно")}");
            
            float totalTime = Time.realtimeSinceStartup - startTime;
            Debug.Log($"⏱️ Время генерации: {totalTime:F2}s");
            
            return isValid;
        }

        private static bool SelectStartAndGoal(GeneratedLevel level)
        {
            var parameters = level.Params;
            
            // Размещаем старт и финиш в углах внутри границ (как в быстром генераторе)
            level.StartPosition = new Vector2Int(1, 1); // Левый нижний угол внутри границ
            level.GoalPosition = new Vector2Int(parameters.Width - 2, parameters.Height - 2); // Правый верхний угол внутри границ
            
            Debug.Log($"Размещены старт и финиш: Start={level.StartPosition}, Goal={level.GoalPosition}");
            return true;
        }

        private static void GenerateStaticObstacles(GeneratedLevel level)
        {
            var parameters = level.Params;
            // Рассчитываем количество препятствий только для внутренней области (без границ)
            int innerWidth = parameters.Width - 2;
            int innerHeight = parameters.Height - 2;
            int innerCells = innerWidth * innerHeight;
            int staticCount = Mathf.RoundToInt(innerCells * parameters.StaticDensity);
            
            level.StaticPositions.Clear();
            
            // Исключаем стартовую и конечную позиции, а также границы
            var availablePositions = new List<Vector2Int>();
            for (int x = 1; x < parameters.Width - 1; x++) // Исключаем границы
            {
                for (int y = 1; y < parameters.Height - 1; y++) // Исключаем границы
                {
                    var pos = new Vector2Int(x, y);
                    if (pos != level.StartPosition && pos != level.GoalPosition)
                    {
                        availablePositions.Add(pos);
                    }
                }
            }

            // Размещаем статические препятствия
            for (int i = 0; i < staticCount && availablePositions.Count > 0; i++)
            {
                int index = random.Next(availablePositions.Count);
                level.StaticPositions.Add(availablePositions[index]);
                availablePositions.RemoveAt(index);
            }
        }

        private static bool PlaceMovableBlocks(GeneratedLevel level)
        {
            var parameters = level.Params;
            level.BlockPlacements.Clear();
            
            var availablePositions = new List<Vector2Int>();
            // Исключаем границы при размещении блоков
            for (int x = 1; x < parameters.Width - 1; x++) // Исключаем границы
            {
                for (int y = 1; y < parameters.Height - 1; y++) // Исключаем границы
                {
                    var pos = new Vector2Int(x, y);
                    if (pos != level.StartPosition && pos != level.GoalPosition && 
                        !level.StaticPositions.Contains(pos))
                    {
                        availablePositions.Add(pos);
                    }
                }
            }

            Debug.Log($"Доступно позиций для блоков: {availablePositions.Count}, нужно разместить: {parameters.BlockCount}");
            
            // Проверяем, достаточно ли места для всех блоков
            if (availablePositions.Count < parameters.BlockCount)
            {
                Debug.LogWarning($"❌ Недостаточно места для размещения {parameters.BlockCount} блоков (доступно: {availablePositions.Count})");
                return false;
            }

            // Размещаем блоки
            for (int i = 0; i < parameters.BlockCount; i++)
            {
                var blockSize = parameters.BlockSizes[random.Next(parameters.BlockSizes.Count)];
                
                // Ищем подходящее место для блока (уменьшаем количество попыток)
                bool placed = false;
                for (int attempts = 0; attempts < 20 && !placed; attempts++) // Уменьшили с 50 до 20
                {
                    if (availablePositions.Count == 0)
                        break;
                        
                    int index = random.Next(availablePositions.Count);
                    var pos = availablePositions[index];
                    
                    // Проверяем, помещается ли блок
                    if (CanPlaceBlock(pos, blockSize, level))
                    {
                        var placement = new BlockPlacement
                        {
                            Position = pos,
                            Size = blockSize,
                            Id = i
                        };
                        level.BlockPlacements.Add(placement);
                        
                        // Удаляем занятые позиции
                        RemoveOccupiedPositions(pos, blockSize, availablePositions);
                        placed = true;
                    }
                }
                
                // Если не удалось разместить блок, прерываем генерацию
                if (!placed)
                {
                    Debug.LogWarning($"❌ Не удалось разместить блок {i + 1}/{parameters.BlockCount}");
                    return false;
                }
            }
            
            Debug.Log($"✅ Успешно размещено {level.BlockPlacements.Count} блоков");

            return true;
        }

        private static bool CanPlaceBlock(Vector2Int pos, Vector2Int size, GeneratedLevel level)
        {
            var parameters = level.Params;
            
            // Проверяем границы
            if (pos.x + size.x > parameters.Width || pos.y + size.y > parameters.Height)
                return false;
                
            // Проверяем пересечения со статическими препятствиями
            for (int x = pos.x; x < pos.x + size.x; x++)
            {
                for (int y = pos.y; y < pos.y + size.y; y++)
                {
                    var checkPos = new Vector2Int(x, y);
                    if (level.StaticPositions.Contains(checkPos))
                        return false;
                }
            }
            
            return true;
        }

        private static void RemoveOccupiedPositions(Vector2Int pos, Vector2Int size, List<Vector2Int> availablePositions)
        {
            for (int x = pos.x; x < pos.x + size.x; x++)
            {
                for (int y = pos.y; y < pos.y + size.y; y++)
                {
                    var occupiedPos = new Vector2Int(x, y);
                    availablePositions.Remove(occupiedPos);
                }
            }
        }

        private static bool ValidateAndRateLevel(GeneratedLevel level)
        {
            Debug.Log($"🔍 Начинаем валидацию уровня...");
            Debug.Log($"   - Старт: {level.StartPosition}, Цель: {level.GoalPosition}");
            Debug.Log($"   - Статических препятствий: {level.StaticPositions.Count}");
            Debug.Log($"   - Подвижных блоков: {level.BlockPlacements.Count}");
            
            // Создаем модель сетки для валидации
            var gridModel = CreateGridModelFromLevel(level);
            
            // Быстрая проверка связности (можно ли дойти от A до B)
            bool isReachable = IsReachable(gridModel, level.StartPosition, level.GoalPosition);
            Debug.Log($"Связность A->B: {isReachable}");
            
            if (!isReachable)
            {
                Debug.LogWarning("❌ Уровень не проходим: нет пути от A до B");
                return false;
            }
            
            // Дополнительная быстрая проверка: достаточно ли блоков для создания пути?
            if (!HasEnoughBlocksForPath(level))
            {
                Debug.LogWarning("❌ Уровень не проходим: недостаточно блоков для создания пути");
                return false;
            }
                
            // Используем решатель для точной оценки сложности (только для проходимых уровней)
            Debug.Log("Запуск решателя...");
            var solveResult = LevelSolver.SolveLevel(gridModel, level.StartPosition, level.GoalPosition,
                                                   level.Params.Emax, level.Params.Ksize, level.Params.Kdist, 0.5f); // Уменьшаем таймаут до 0.5 секунд для быстрой генерации
            
            Debug.Log($"Результат решателя: Solvable={solveResult.IsSolvable}, Time={solveResult.SolveTime:F2}s, NodesExplored={solveResult.NodesExplored}");
            
            if (!solveResult.IsSolvable)
            {
                Debug.LogWarning("❌ Решатель не смог найти решение");
                return false;
            }
                
            // Обновляем оценки на основе решения
            level.EstimatedMinEnergy = solveResult.MinEnergy;
            level.EstimatedMinHops = solveResult.MinMoves;
            
            // Определяем сложность согласно ТЗ
            DetermineDifficulty(level);
            
            Debug.Log($"✅ Валидация успешна: Energy={level.EstimatedMinEnergy}, Hops={level.EstimatedMinHops}");
            return true;
        }
        
        /// <summary>
        /// Быстрая проверка: достаточно ли блоков для создания пути
        /// </summary>
        private static bool HasEnoughBlocksForPath(GeneratedLevel level)
        {
            var parameters = level.Params;
            
            // Рассчитываем минимальное расстояние между стартом и финишем
            int distance = Mathf.Abs(level.GoalPosition.x - level.StartPosition.x) + 
                          Mathf.Abs(level.GoalPosition.y - level.StartPosition.y);
            
            // Для создания пути нужно минимум distance/2 блоков (грубая оценка)
            int minBlocksNeeded = Mathf.Max(1, distance / 3);
            
            bool hasEnoughBlocks = level.BlockPlacements.Count >= minBlocksNeeded;
            Debug.Log($"Проверка блоков: нужно минимум {minBlocksNeeded}, есть {level.BlockPlacements.Count} - {(hasEnoughBlocks ? "✅" : "❌")}");
            
            return hasEnoughBlocks;
        }

        private static GridModel CreateGridModelFromLevel(GeneratedLevel level)
        {
            var parameters = level.Params;
            var gridModel = new GridModel(parameters.Width, parameters.Height);
            
            // Размещаем статические препятствия
            foreach (var pos in level.StaticPositions)
            {
                gridModel.PlaceStatic(pos);
            }
            
            // Размещаем подвижные блоки
            foreach (var placement in level.BlockPlacements)
            {
                var rect = new RectInt(placement.Position.x, placement.Position.y, 
                                     placement.Size.x, placement.Size.y);
                var block = new BlockRt(placement.Id, rect);
                gridModel.AddBlock(block);
            }
            
            return gridModel;
        }

        public static bool IsReachable(GridModel grid, Vector2Int start, Vector2Int goal)
        {
            Debug.Log($"🔍 Проверка связности: {start} -> {goal}");
            
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            
            queue.Enqueue(start);
            visited.Add(start);
            
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            int steps = 0;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                steps++;
                
                if (current == goal)
                {
                    Debug.Log($"✅ Связность найдена за {steps} шагов");
                    return true;
                }
                    
                foreach (var dir in directions)
                {
                    var next = current + dir;
                    if (grid.InBounds(next.x, next.y) && !visited.Contains(next) && grid.IsCellFree(next))
                    {
                        visited.Add(next);
                        queue.Enqueue(next);
                        Debug.Log($"   - Добавлена позиция: {next}");
                    }
                }
            }
            
            Debug.LogWarning($"❌ Связность не найдена. Исследовано {steps} позиций");
            return false;
        }

        private static void DetermineDifficulty(GeneratedLevel level)
        {
            // Определяем сложность согласно ТЗ на основе точных оценок решателя
            if (level.EstimatedMinHops <= 6 && level.EstimatedMinEnergy <= 12)
            {
                level.Difficulty = LevelDifficulty.Easy;
            }
            else if ((level.EstimatedMinHops >= 7 && level.EstimatedMinHops <= 12) || 
                     (level.EstimatedMinEnergy >= 13 && level.EstimatedMinEnergy <= 25))
            {
                level.Difficulty = LevelDifficulty.Medium;
            }
            else
            {
                level.Difficulty = LevelDifficulty.Hard;
            }
        }
    }
}
