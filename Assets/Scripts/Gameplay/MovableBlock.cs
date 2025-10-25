using LabyrinthMover.Core;
using UnityEngine;

namespace LabyrinthMover.Gameplay
{
    [DisallowMultipleComponent]
    public class MovableBlock : MonoBehaviour
    {
        [SerializeField]
        private Vector2Int size = Vector2Int.one;

        public Vector2Int Size
        {
            get => new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
            set => size = new Vector2Int(Mathf.Max(1, value.x), Mathf.Max(1, value.y));
        }

        public Vector2Int Anchor
        {
            get
            {
                var p = transform.position;
                float scale = GetGridScale();
                if (Mathf.Approximately(scale, 0f))
                {
                    scale = 1f;
                }

                return new Vector2Int(
                    Mathf.RoundToInt(p.x / scale),
                    Mathf.RoundToInt(p.y / scale));
            }
        }

        private static float GetGridScale()
        {
            var gridConfig = Object.FindFirstObjectByType<GridConfig>();
            return gridConfig != null ? gridConfig.TileSize / 100f : 1f;
        }
    }
}
