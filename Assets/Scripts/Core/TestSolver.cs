using UnityEngine;
using LabyrinthMover.Core;

namespace LabyrinthMover.Core
{
    /// <summary>
    /// Тестовый скрипт для проверки решателя
    /// </summary>
    public class TestSolver : MonoBehaviour
    {
        [ContextMenu("Test Solver")]
        public void TestSolverMethod()
        {
            Debug.Log("🧪 Тестирование решателя...");
            
            // Создаем простой тестовый уровень
            var grid = new GridModel(5, 5);
            
            // Размещаем старт и цель
            var startPos = new Vector2Int(0, 0);
            var goalPos = new Vector2Int(4, 4);
            
            // Размещаем несколько статических препятствий
            grid.PlaceStatic(new Vector2Int(1, 1));
            grid.PlaceStatic(new Vector2Int(2, 2));
            grid.PlaceStatic(new Vector2Int(3, 3));
            
            // Размещаем один подвижный блок
            var block = new BlockRt(1, new RectInt(1, 3, 1, 1));
            grid.AddBlock(block);
            
            Debug.Log($"Тестовый уровень создан:");
            Debug.Log($"  - Размер: {grid.Width}x{grid.Height}");
            Debug.Log($"  - Старт: {startPos}");
            Debug.Log($"  - Цель: {goalPos}");
            Debug.Log($"  - Блоков: {grid.Blocks.Count}");
            
            // Тестируем решатель
            var result = LevelSolver.SolveLevel(grid, startPos, goalPos, 50f, 1f, 0.1f, 5f);
            
            Debug.Log($"Результат тестирования:");
            Debug.Log($"  - Решаем: {result.IsSolvable}");
            Debug.Log($"  - Время: {result.SolveTime:F2}s");
            Debug.Log($"  - Узлов: {result.NodesExplored}");
            Debug.Log($"  - Энергия: {result.MinEnergy}");
            Debug.Log($"  - Ходов: {result.MinMoves}");
        }
    }
}


