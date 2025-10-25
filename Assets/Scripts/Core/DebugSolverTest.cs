using UnityEngine;
using LabyrinthMover.Core;

namespace LabyrinthMover.Core
{
    /// <summary>
    /// Простой тест решателя для отладки
    /// </summary>
    public class DebugSolverTest : MonoBehaviour
    {
        [ContextMenu("Тест решателя - простой уровень")]
        public void TestSolverSimple()
        {
            Debug.Log("🧪 Тестируем решатель с простым уровнем 5x5...");
            
            // Создаем простой уровень 5x5
            var grid = new GridModel(5, 5);
            
            // Размещаем старт и цель
            Vector2Int startPos = new Vector2Int(0, 0);
            Vector2Int goalPos = new Vector2Int(4, 4);
            
            // Размещаем несколько статических препятствий
            grid.PlaceStatic(new Vector2Int(1, 1));
            grid.PlaceStatic(new Vector2Int(2, 2));
            grid.PlaceStatic(new Vector2Int(3, 3));
            
            // Размещаем один подвижный блок
            var block = new BlockRt(1, new RectInt(1, 3, 1, 1));
            grid.AddBlock(block);
            
            Debug.Log($"📊 Уровень создан:");
            Debug.Log($"   - Размер: {grid.Width}x{grid.Height}");
            Debug.Log($"   - Старт: {startPos}");
            Debug.Log($"   - Цель: {goalPos}");
            Debug.Log($"   - Статических препятствий: 3");
            Debug.Log($"   - Подвижных блоков: {grid.Blocks.Count}");
            
            // Проверяем связность A->B
            bool isReachable = LevelGenerator.IsReachable(grid, startPos, goalPos);
            Debug.Log($"🔗 Связность A->B: {isReachable}");
            
            // Тестируем решатель
            var result = LevelSolver.SolveLevel(grid, startPos, goalPos, 50f, 1.0f, 0.1f, 5.0f);
            
            Debug.Log($"🎯 Результат решения:");
            Debug.Log($"   - Проходим: {result.IsSolvable}");
            Debug.Log($"   - Минимальная энергия: {result.MinEnergy}");
            Debug.Log($"   - Минимальные ходы: {result.MinMoves}");
            Debug.Log($"   - Время решения: {result.SolveTime:F2}s");
            Debug.Log($"   - Узлов исследовано: {result.NodesExplored}");
            
            if (result.IsSolvable && result.Solution != null)
            {
                Debug.Log($"✅ Решение найдено! Ходов: {result.Solution.Count}");
                for (int i = 0; i < result.Solution.Count; i++)
                {
                    var move = result.Solution[i];
                    Debug.Log($"   Ход {i + 1}: Блок {move.BlockId} -> {move.Direction} (стоимость: {move.Cost:F1})");
                }
            }
            else
            {
                Debug.LogError("❌ Решение не найдено!");
            }
        }
        
        [ContextMenu("Тест решателя - пустой уровень")]
        public void TestSolverEmpty()
        {
            Debug.Log("🧪 Тестируем решатель с пустым уровнем 3x3...");
            
            // Создаем пустой уровень 3x3
            var grid = new GridModel(3, 3);
            
            Vector2Int startPos = new Vector2Int(0, 0);
            Vector2Int goalPos = new Vector2Int(2, 2);
            
            Debug.Log($"📊 Пустой уровень создан:");
            Debug.Log($"   - Размер: {grid.Width}x{grid.Height}");
            Debug.Log($"   - Старт: {startPos}");
            Debug.Log($"   - Цель: {goalPos}");
            Debug.Log($"   - Блоков: {grid.Blocks.Count}");
            
            // Проверяем связность A->B
            bool isReachable = LevelGenerator.IsReachable(grid, startPos, goalPos);
            Debug.Log($"🔗 Связность A->B: {isReachable}");
            
            // Тестируем решатель
            var result = LevelSolver.SolveLevel(grid, startPos, goalPos, 50f, 1.0f, 0.1f, 5.0f);
            
            Debug.Log($"🎯 Результат решения:");
            Debug.Log($"   - Проходим: {result.IsSolvable}");
            Debug.Log($"   - Минимальная энергия: {result.MinEnergy}");
            Debug.Log($"   - Минимальные ходы: {result.MinMoves}");
            Debug.Log($"   - Время решения: {result.SolveTime:F2}s");
            Debug.Log($"   - Узлов исследовано: {result.NodesExplored}");
        }
        
        [ContextMenu("Тест решателя - уровень с блоком")]
        public void TestSolverWithBlock()
        {
            Debug.Log("🧪 Тестируем решатель с уровнем с одним блоком...");
            
            // Создаем уровень 4x4 с одним блоком
            var grid = new GridModel(4, 4);
            
            Vector2Int startPos = new Vector2Int(0, 0);
            Vector2Int goalPos = new Vector2Int(3, 3);
            
            // Размещаем один подвижный блок
            var block = new BlockRt(1, new RectInt(1, 1, 1, 1));
            grid.AddBlock(block);
            
            Debug.Log($"📊 Уровень с блоком создан:");
            Debug.Log($"   - Размер: {grid.Width}x{grid.Height}");
            Debug.Log($"   - Старт: {startPos}");
            Debug.Log($"   - Цель: {goalPos}");
            Debug.Log($"   - Блоков: {grid.Blocks.Count}");
            
            // Проверяем связность A->B
            bool isReachable = LevelGenerator.IsReachable(grid, startPos, goalPos);
            Debug.Log($"🔗 Связность A->B: {isReachable}");
            
            // Тестируем решатель
            var result = LevelSolver.SolveLevel(grid, startPos, goalPos, 50f, 1.0f, 0.1f, 5.0f);
            
            Debug.Log($"🎯 Результат решения:");
            Debug.Log($"   - Проходим: {result.IsSolvable}");
            Debug.Log($"   - Минимальная энергия: {result.MinEnergy}");
            Debug.Log($"   - Минимальные ходы: {result.MinMoves}");
            Debug.Log($"   - Время решения: {result.SolveTime:F2}s");
            Debug.Log($"   - Узлов исследовано: {result.NodesExplored}");
        }
        
        [ContextMenu("Тест решателя - только движение персонажа")]
        public void TestSolverCharacterOnly()
        {
            Debug.Log("🧪 Тестируем решатель с уровнем где нужно только двигать персонажа...");
            
            // Создаем уровень 3x3 без блоков
            var grid = new GridModel(3, 3);
            
            Vector2Int startPos = new Vector2Int(0, 0);
            Vector2Int goalPos = new Vector2Int(2, 2);
            
            Debug.Log($"📊 Простой уровень создан:");
            Debug.Log($"   - Размер: {grid.Width}x{grid.Height}");
            Debug.Log($"   - Старт: {startPos}");
            Debug.Log($"   - Цель: {goalPos}");
            Debug.Log($"   - Блоков: {grid.Blocks.Count}");
            
            // Проверяем связность A->B
            bool isReachable = LevelGenerator.IsReachable(grid, startPos, goalPos);
            Debug.Log($"🔗 Связность A->B: {isReachable}");
            
            // Тестируем решатель
            var result = LevelSolver.SolveLevel(grid, startPos, goalPos, 50f, 1.0f, 0.1f, 5.0f);
            
            Debug.Log($"🎯 Результат решения:");
            Debug.Log($"   - Проходим: {result.IsSolvable}");
            Debug.Log($"   - Минимальная энергия: {result.MinEnergy}");
            Debug.Log($"   - Минимальные ходы: {result.MinMoves}");
            Debug.Log($"   - Время решения: {result.SolveTime:F2}s");
            Debug.Log($"   - Узлов исследовано: {result.NodesExplored}");
        }
    }
}
