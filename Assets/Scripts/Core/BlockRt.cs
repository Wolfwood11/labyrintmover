using UnityEngine;

namespace LabyrinthMover.Core
{
    public struct BlockRt
    {
        public int Id;
        public RectInt Rect;

        public BlockRt(int id, RectInt rect)
        {
            Id = id;
            Rect = rect;
        }
    }
}
