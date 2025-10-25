using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LabyrinthMover.Core
{
    /// <summary>
    /// Простой решатель уровней с использованием A* алгоритма
    /// </summary>
    public static class LevelSolver
    {
        /// <summary>
        /// Результат решения уровня
        /// </summary>
        public class SolveResult
        {
            public bool IsSolvable;
            public int MinEnergy;
            public int MinMoves;
            public List<SolverMove> Solution;
            public float SolveTime;
            public int NodesExplored;
        }

        /// <summary>
        /// Ход в решении
        /// </summary>
        public class SolverMove
        {
            public int BlockId;
            public Vector2Int Direction;
            public float Cost;
            public Vector2Int CharacterPosition;
        }

        /// <summary>
        /// Состояние уровня для поиска
        /// </summary>
        private class SolverState : IEquatable<SolverState>
        {
            public GridSnapshot GridSnapshot;
            public Vector2Int CharacterPosition;
            public float EnergyRemaining;
            public float GCost; // Стоимость пути до этого состояния
            public float FCost; // Общая оценка f = g + h
            public SolverState Parent;
            public SolverMove Move;

            public bool Equals(SolverState other)
            {
                if (other == null) return false;
                
                // Сравниваем позицию персонажа и состояние блоков
                if (CharacterPosition != other.CharacterPosition) return false;
                
                // Сравниваем позиции всех блоков
                if (GridSnapshot.Blocks.Count != other.GridSnapshot.Blocks.Count) return false;
                
                foreach (var kvp in GridSnapshot.Blocks)
                {
                    if (!other.GridSnapshot.Blocks.TryGetValue(kvp.Key, out var otherBlock) ||
                        kvp.Value.Rect != otherBlock.Rect)
                    {
                        return false;
                    }
                }
                
                return true;
            }

            public override int GetHashCode()
            {
                int hash = CharacterPosition.GetHashCode();
                foreach (var kvp in GridSnapshot.Blocks)
                {
                    hash ^= kvp.Key.GetHashCode() ^ kvp.Value.Rect.GetHashCode();
                }
                return hash;
            }
        }

        /// <summary>
        /// Решает уровень и возвращает минимальную стоимость
        /// </summary>
        public static SolveResult SolveLevel(GridModel grid, Vector2Int startPos, Vector2Int goalPos, 
                                           float maxEnergy, float ksize, float kdist, float timeLimit = 0.3f)
        {
            var result = new SolveResult
            {
                IsSolvable = false,
                MinEnergy = int.MaxValue,
                MinMoves = int.MaxValue,
                Solution = new List<SolverMove>(),
                SolveTime = 0f,
                NodesExplored = 0
            };

            var startTime = Time.realtimeSinceStartup;

            try
            {
                Debug.Log($"🔍 Запуск решателя: Start={startPos}, Goal={goalPos}, Energy={maxEnergy}");
                Debug.Log($"   - Размер сетки: {grid.Width}x{grid.Height}");
                Debug.Log($"   - Количество блоков: {grid.Blocks.Count}");
                Debug.Log($"   - Таймаут: {timeLimit}s");
                
                var solution = FindSolution(grid, startPos, goalPos, maxEnergy, ksize, kdist, timeLimit, out int nodesExplored);
                
                result.NodesExplored = nodesExplored;
                
                if (solution != null)
                {
                    result.IsSolvable = true;
                    result.Solution = ReconstructSolution(solution);
                    result.MinEnergy = Mathf.RoundToInt(maxEnergy - solution.EnergyRemaining);
                    result.MinMoves = result.Solution.Count;
                    Debug.Log($"✅ Решение найдено: Energy={result.MinEnergy}, Moves={result.MinMoves}");
                }
                else
                {
                    Debug.LogWarning("❌ Решение не найдено");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Ошибка при решении уровня: {e.Message}");
            }

            result.SolveTime = Time.realtimeSinceStartup - startTime;
            Debug.Log($"⏱️ Время решения: {result.SolveTime:F2}s, Узлов исследовано: {result.NodesExplored}");
            return result;
        }

        private static SolverState FindSolution(GridModel grid, Vector2Int startPos, Vector2Int goalPos,
                                               float maxEnergy, float ksize, float kdist, float timeLimit, out int nodesExplored)
        {
            var openSet = new List<SolverState>();
            var closedSet = new HashSet<SolverState>();
            var startTime = Time.realtimeSinceStartup;
            nodesExplored = 0; // Initialize the out parameter

            var startState = new SolverState
            {
                GridSnapshot = grid.CreateSnapshot(),
                CharacterPosition = startPos,
                EnergyRemaining = maxEnergy,
                GCost = 0f,
                FCost = Heuristic(startPos, goalPos),
                Parent = null,
                Move = null
            };

            Debug.Log($"🚀 Начальное состояние: Pos={startPos}, Energy={maxEnergy}, FCost={startState.FCost}");
            Debug.Log($"   - Блоков в снапшоте: {startState.GridSnapshot.Blocks.Count}");

            openSet.Add(startState);

            while (openSet.Count > 0 && (Time.realtimeSinceStartup - startTime) < timeLimit)
            {
                // Выбираем состояние с наименьшей f-стоимостью
                var current = openSet.OrderBy(s => s.FCost).First();
                openSet.Remove(current);
                closedSet.Add(current);
                nodesExplored++;

                Debug.Log($"🔍 Исследуем узел {nodesExplored}: Pos={current.CharacterPosition}, Energy={current.EnergyRemaining:F1}, FCost={current.FCost:F1}");

                // Проверяем, достигли ли цели
                if (current.CharacterPosition == goalPos)
                {
                    Debug.Log($"🎯 Цель достигнута! Узлов исследовано: {nodesExplored}");
                    return current;
                }

                // Генерируем возможные ходы
                var moves = GenerateMoves(current, grid, ksize, kdist);
                Debug.Log($"   - Сгенерировано ходов: {moves.Count}");
                
                foreach (var move in moves)
                {
                    var newState = ApplyMove(current, move, goalPos);
                    
                    if (newState == null)
                    {
                        Debug.Log($"   - Ход {move.BlockId}->{move.Direction} отклонен (недостаточно энергии или невалиден)");
                        continue;
                    }
                    
                    if (closedSet.Contains(newState))
                    {
                        Debug.Log($"   - Ход {move.BlockId}->{move.Direction} уже исследован");
                        continue;
                    }

                    var existingState = openSet.FirstOrDefault(s => s.Equals(newState));
                    if (existingState != null)
                    {
                        if (newState.GCost < existingState.GCost)
                        {
                            openSet.Remove(existingState);
                            openSet.Add(newState);
                            Debug.Log($"   - Ход {move.BlockId}->{move.Direction} улучшен (GCost: {existingState.GCost:F1} -> {newState.GCost:F1})");
                        }
                    }
                    else
                    {
                        openSet.Add(newState);
                        Debug.Log($"   - Ход {move.BlockId}->{move.Direction} добавлен (Cost={move.Cost:F1}, Energy={newState.EnergyRemaining:F1})");
                    }
                }
            }

            Debug.LogWarning($"⏰ Таймаут или нет ходов. Узлов исследовано: {nodesExplored}, Открытых: {openSet.Count}");
            return null; // Решение не найдено
        }

        private static List<SolverMove> GenerateMoves(SolverState state, GridModel originalGrid, float ksize, float kdist)
        {
            var moves = new List<SolverMove>();
            var grid = new GridModel(state.GridSnapshot.Width, state.GridSnapshot.Height);
            grid.RestoreSnapshot(state.GridSnapshot);

            Debug.Log($"🔧 Генерация ходов для состояния с {state.GridSnapshot.Blocks.Count} блоками");

            // Сначала генерируем ходы персонажа (бесплатные)
            var characterMoves = GenerateCharacterMoves(state, grid);
            moves.AddRange(characterMoves);
            Debug.Log($"   - Ходов персонажа: {characterMoves.Count}");

            // Затем генерируем ходы для каждого блока
            foreach (var kvp in state.GridSnapshot.Blocks)
            {
                var blockId = kvp.Key;
                var block = kvp.Value;

                Debug.Log($"   - Блок {blockId}: Rect={block.Rect}");

                // Пробуем все четыре направления
                Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                
                foreach (var dir in directions)
                {
                    var cost = SimulateMove(grid, blockId, dir, state.CharacterPosition, ksize, kdist);
                    Debug.Log($"     - Направление {dir}: Cost={cost:F1}, Energy={state.EnergyRemaining:F1}");
                    
                    if (cost > 0 && cost <= state.EnergyRemaining)
                    {
                        moves.Add(new SolverMove
                        {
                            BlockId = blockId,
                            Direction = dir,
                            Cost = cost,
                            CharacterPosition = state.CharacterPosition
                        });
                        Debug.Log($"       ✅ Ход добавлен");
                    }
                    else
                    {
                        Debug.Log($"       ❌ Ход отклонен (cost={cost:F1}, energy={state.EnergyRemaining:F1})");
                    }
                }
            }

            Debug.Log($"🔧 Итого сгенерировано ходов: {moves.Count}");
            return moves;
        }

        private static List<SolverMove> GenerateCharacterMoves(SolverState state, GridModel grid)
        {
            var moves = new List<SolverMove>();
            var currentPos = state.CharacterPosition;
            
            // Пробуем все четыре направления для персонажа
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            foreach (var dir in directions)
            {
                var newPos = currentPos + dir;
                
                // Проверяем границы
                if (newPos.x < 0 || newPos.y < 0 || newPos.x >= grid.Width || newPos.y >= grid.Height)
                {
                    continue;
                }
                
                // Проверяем, что клетка пустая (не статическое препятствие и не блок)
                if (grid.cells[newPos.x, newPos.y] == CellType.Empty)
                {
                    moves.Add(new SolverMove
                    {
                        BlockId = -1, // -1 означает ход персонажа
                        Direction = dir,
                        Cost = 0f, // Движение персонажа бесплатное
                        CharacterPosition = newPos
                    });
                }
            }
            
            return moves;
        }

        private static float SimulateMove(GridModel grid, int blockId, Vector2Int dir, Vector2Int charPos, float ksize, float kdist)
        {
            if (!grid.Blocks.TryGetValue(blockId, out var block))
            {
                Debug.Log($"❌ Блок {blockId} не найден в сетке");
                return 0f;
            }

            var currentRect = block.Rect;
            float totalCost = 0f;
            int steps = 0;

            Debug.Log($"🎯 Симуляция хода блока {blockId} в направлении {dir}");

            while (true)
            {
                var nextRect = new RectInt(currentRect.x + dir.x, currentRect.y + dir.y, currentRect.width, currentRect.height);
                
                // Проверяем границы
                if (nextRect.xMin < 0 || nextRect.yMin < 0 || 
                    nextRect.xMax > grid.Width || nextRect.yMax > grid.Height)
                {
                    Debug.Log($"   - Шаг {steps}: Выход за границы");
                    break;
                }

                // Проверяем столкновения
                bool blockedByStatic = false;
                int? touchedId = null;
                
                for (int x = nextRect.xMin; x < nextRect.xMax && !blockedByStatic; x++)
                {
                    for (int y = nextRect.yMin; y < nextRect.yMax; y++)
                    {
                        if (grid.cells[x, y] == CellType.Static)
                        {
                            blockedByStatic = true;
                            break;
                        }

                        var id = grid.blockIds[x, y];
                        if (id.HasValue && id.Value != blockId)
                        {
                            if (touchedId.HasValue && touchedId.Value != id.Value)
                            {
                                Debug.Log($"   - Шаг {steps}: Множественное столкновение");
                                return 0f; // Множественное столкновение
                            }
                            touchedId = id.Value;
                        }
                    }
                }

                if (blockedByStatic)
                {
                    Debug.Log($"   - Шаг {steps}: Заблокирован статическим препятствием");
                    break;
                }

                if (touchedId.HasValue)
                {
                    // Объединение блоков
                    if (!grid.Blocks.TryGetValue(touchedId.Value, out var touchedBlock))
                    {
                        Debug.Log($"   - Шаг {steps}: Касаемый блок {touchedId.Value} не найден");
                        break;
                    }

                    currentRect = BoundingBox(nextRect, touchedBlock.Rect);
                    Debug.Log($"   - Шаг {steps}: Объединение с блоком {touchedId.Value}, новый размер: {currentRect}");
                }
                else
                {
                    currentRect = nextRect;
                }

                // Вычисляем стоимость шага
                int S = Energy.GetArea(currentRect);
                int d = Energy.ManhattanToNearestCell(charPos, currentRect);
                float stepCost = Energy.StepCost(S, d, ksize, kdist);
                totalCost += stepCost;
                steps++;
                
                Debug.Log($"   - Шаг {steps}: S={S}, d={d}, cost={stepCost:F1}, total={totalCost:F1}");
            }

            Debug.Log($"🎯 Итого: {steps} шагов, стоимость {totalCost:F1}");
            return steps > 0 ? totalCost : 0f;
        }

        private static SolverState ApplyMove(SolverState parentState, SolverMove move, Vector2Int goalPos)
        {
            var newGrid = new GridModel(parentState.GridSnapshot.Width, parentState.GridSnapshot.Height);
            newGrid.RestoreSnapshot(parentState.GridSnapshot);

            // Используем уже вычисленную стоимость хода
            var cost = move.Cost;
            if (cost < 0 || cost > parentState.EnergyRemaining)
            {
                Debug.Log($"❌ Ход {move.BlockId}->{move.Direction} отклонен в ApplyMove: cost={cost:F1}, energy={parentState.EnergyRemaining:F1}");
                return null;
            }

            // Обрабатываем ход персонажа (BlockId = -1)
            if (move.BlockId == -1)
            {
                var characterNewPos = move.CharacterPosition;
                Debug.Log($"🚶 Персонаж перемещен: {parentState.CharacterPosition} -> {characterNewPos}");
                
                return new SolverState
                {
                    GridSnapshot = newGrid.CreateSnapshot(),
                    CharacterPosition = characterNewPos,
                    EnergyRemaining = parentState.EnergyRemaining, // Движение персонажа бесплатное
                    GCost = parentState.GCost, // Движение персонажа не увеличивает g-стоимость
                    FCost = parentState.GCost + Heuristic(characterNewPos, goalPos),
                    Parent = parentState,
                    Move = move
                };
            }

            // Обрабатываем ход блока
            if (newGrid.Blocks.TryGetValue(move.BlockId, out var block))
            {
                var newRect = new RectInt(block.Rect.x + move.Direction.x, block.Rect.y + move.Direction.y, 
                                        block.Rect.width, block.Rect.height);
                
                // Удаляем блок из старой позиции
                newGrid.RemoveBlock(move.BlockId);
                
                // Добавляем блок в новую позицию
                var newBlock = new BlockRt(move.BlockId, newRect);
                newGrid.AddBlock(newBlock);
                
                Debug.Log($"🔄 Блок {move.BlockId} перемещен: {block.Rect} -> {newRect}");
            }
            else
            {
                Debug.LogError($"❌ Блок {move.BlockId} не найден в сетке!");
                return null;
            }

            // Обновляем позицию персонажа (упрощенная версия - персонаж остается на месте)
            var newCharacterPos = parentState.CharacterPosition;

            Debug.Log($"✅ Применяем ход {move.BlockId}->{move.Direction}: cost={cost:F1}, newEnergy={parentState.EnergyRemaining - cost:F1}");

            return new SolverState
            {
                GridSnapshot = newGrid.CreateSnapshot(),
                CharacterPosition = newCharacterPos,
                EnergyRemaining = parentState.EnergyRemaining - cost,
                GCost = parentState.GCost + cost,
                FCost = parentState.GCost + cost + Heuristic(newCharacterPos, goalPos),
                Parent = parentState,
                Move = move
            };
        }

        private static float Heuristic(Vector2Int charPos, Vector2Int goalPos)
        {
            // Простая эвристика - манхэттен-расстояние до цели
            return Mathf.Abs(charPos.x - goalPos.x) + Mathf.Abs(charPos.y - goalPos.y);
        }

        private static List<SolverMove> ReconstructSolution(SolverState goalState)
        {
            var solution = new List<SolverMove>();
            var current = goalState;

            while (current.Parent != null)
            {
                solution.Insert(0, current.Move);
                current = current.Parent;
            }

            return solution;
        }

        private static RectInt BoundingBox(RectInt a, RectInt b)
        {
            int xMin = Mathf.Min(a.xMin, b.xMin);
            int yMin = Mathf.Min(a.yMin, b.yMin);
            int xMax = Mathf.Max(a.xMax, b.xMax);
            int yMax = Mathf.Max(a.yMax, b.yMax);
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }
    }
}
