using System.Collections;
using System.Collections.Generic;
using LabyrinthMover.Core;
using UnityEngine;

namespace LabyrinthMover.Gameplay
{
    [RequireComponent(typeof(Transform))]
    public class CharacterController2D : MonoBehaviour
    {
        private GridModel grid;
        public Vector2Int GridPos { get; private set; }

        [SerializeField]
        private float moveDuration = 0.15f;

        public void Init(GridModel gridModel, Vector2Int start)
        {
            grid = gridModel;
            GridPos = start;
            transform.position = new Vector3(start.x, start.y, transform.position.z);
        }

        public bool TryPathTo(Vector2Int target, out List<Vector2Int> path)
        {
            path = null;
            if (grid == null)
            {
                return false;
            }

            if (!grid.InBounds(target.x, target.y) || !grid.IsCellFree(target))
            {
                return false;
            }

            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            var parents = new Dictionary<Vector2Int, Vector2Int>();

            queue.Enqueue(GridPos);
            visited.Add(GridPos);

            Vector2Int[] dirs =
            {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == target)
                {
                    break;
                }

                foreach (var dir in dirs)
                {
                    var next = current + dir;
                    if (!grid.InBounds(next.x, next.y) || visited.Contains(next))
                    {
                        continue;
                    }

                    if (!grid.IsCellFree(next))
                    {
                        continue;
                    }

                    visited.Add(next);
                    parents[next] = current;
                    queue.Enqueue(next);
                }
            }

            if (!visited.Contains(target))
            {
                return false;
            }

            var result = new List<Vector2Int>();
            var cursor = target;
            while (cursor != GridPos)
            {
                result.Add(cursor);
                cursor = parents[cursor];
            }

            result.Reverse();
            path = result;
            return true;
        }

        public IEnumerator PlayPath(List<Vector2Int> path)
        {
            if (path == null || path.Count == 0)
            {
                yield break;
            }

            foreach (var step in path)
            {
                Vector3 start = transform.position;
                Vector3 end = new Vector3(step.x, step.y, start.z);
                float elapsed = 0f;
                while (elapsed < moveDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / moveDuration);
                    transform.position = Vector3.Lerp(start, end, t);
                    yield return null;
                }

                transform.position = end;
                GridPos = new Vector2Int(step.x, step.y);
            }
        }
    }
}
