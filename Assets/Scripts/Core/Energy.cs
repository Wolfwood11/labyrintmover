using System;
using UnityEngine;

namespace LabyrinthMover.Core
{
    public static class Energy
    {
        /// <summary>
        /// Вычисляет стоимость одного шага (1 клетка) перемещения блока
        /// S = w*h - текущая площадь объекта на шаге
        /// d = manhattan(character, nearestCell(block)) - манхэттен-расстояние до ближайшей клетки блока
        /// cost_cell = (Ksize * S) * (1 + Kdist * d)
        /// </summary>
        public static float StepCost(int S, int d, float Ksize, float Kdist)
        {
            float result = (Ksize * S) * (1f + Kdist * d);
            Debug.Log($"Energy.StepCost: S={S}, d={d}, Ksize={Ksize}, Kdist={Kdist} -> {result:F2}");
            return result;
        }

        /// <summary>
        /// Быстрое вычисление стоимости свайпа без объединений (закрытая форма)
        /// Для движения строго "к себе" или "от себя"
        /// Cost = (Ksize * S) * [ L + Kdist * L * (d0 ± (L-1)/2) ]
        /// </summary>
        public static float FastSwipeCost(int S, int L, int d0, bool towardsCharacter, float Ksize, float Kdist)
        {
            float distanceFactor = towardsCharacter ? -(L - 1) / 2f : (L - 1) / 2f;
            return (Ksize * S) * (L + Kdist * L * (d0 + distanceFactor));
        }

        /// <summary>
        /// Вычисляет манхэттен-расстояние от персонажа до ближайшей клетки прямоугольника
        /// </summary>
        public static int ManhattanToNearestCell(Vector2Int charPos, RectInt rect)
        {
            int min = int.MaxValue;
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                for (int y = rect.yMin; y < rect.yMax; y++)
                {
                    int dist = Mathf.Abs(charPos.x - x) + Mathf.Abs(charPos.y - y);
                    if (dist < min)
                    {
                        min = dist;
                    }
                }
            }

            return min == int.MaxValue ? 0 : min;
        }

        /// <summary>
        /// Вычисляет площадь прямоугольника
        /// </summary>
        public static int GetArea(RectInt rect)
        {
            return rect.width * rect.height;
        }

        /// <summary>
        /// Определяет направление движения относительно персонажа
        /// </summary>
        public static bool IsTowardsCharacter(Vector2Int charPos, RectInt startRect, RectInt endRect, Vector2Int direction)
        {
            int startDist = ManhattanToNearestCell(charPos, startRect);
            int endDist = ManhattanToNearestCell(charPos, endRect);
            return endDist < startDist;
        }
    }
}
