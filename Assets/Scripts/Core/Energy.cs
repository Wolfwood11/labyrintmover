using System;
using UnityEngine;

namespace LabyrinthMover.Core
{
    public static class Energy
    {
        public static float StepCost(int S, int d, float Ksize, float Kdist)
        {
            return (Ksize * S) * (1f + Kdist * d);
        }

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
    }
}
