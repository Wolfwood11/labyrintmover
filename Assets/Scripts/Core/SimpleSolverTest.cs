using UnityEngine;
using LabyrinthMover.Core;

namespace LabyrinthMover.Core
{
    /// <summary>
    /// Простой тест решателя для отладки
    /// </summary>
    public class SimpleSolverTest : MonoBehaviour
    {
        [ContextMenu("Test Simple Solver")]
        public void TestSimpleSolver()
        {
            Debug.Log("🧪 Тестирование простого решателя...");
            
            // Создаем очень простой уровень 3x3
            var grid = new GridModel(3, 3);
            
            // Размещаем старт и цель
            var startPos = new Vector2Int(0, 0);
            var goalPos = new Vector2Int(2, 2);
            
            // Размещаем один статический блок в центре
            grid.PlaceStatic(new Vector2Int(1, 1));
            
            // Размещаем один подвижный блок
            var block = new BlockRt(1, new RectInt(0, 2, 1, 1));
            grid.AddBlock(block);
            
            Debug.Log($"Простой тестовый уровень создан:");
            Debug.Log($"  - Размер: {grid.Width}x{grid.Height}");
            Debug.Log($"  - Старт: {startPos}");
            Debug.Log($"  - Цель: {goalPos}");
            Debug.Log($"  - Статических препятствий: 1");
            Debug.Log($"  - Подвижных блоков: {grid.Blocks.Count}");
            
            // Выводим состояние сетки
            for (int y = grid.Height - 1; y >= 0; y--)
            {
                string row = "";
                for (int x = 0; x < grid.Width; x++)
                {
                    if (x == startPos.x && y == startPos.y)
                        row += "A";
                    else if (x == goalPos.x && y == goalPos.y)
                        row += "B";
                    else if (grid.cells[x, y] == CellType.Static)
                        row += "#";
                    else if (grid.cells[x, y] == CellType.Movable)
                        row += "M";
                    else
                        row += ".";
                }
                Debug.Log($"  {row}");
            }
            
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


