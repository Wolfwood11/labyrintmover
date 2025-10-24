using UnityEngine;

namespace LabyrinthMover.Core
{
    public class GridConfig : MonoBehaviour
    {
        [Min(1)]
        public int Width = 8;

        [Min(1)]
        public int Height = 8;

        [Min(0f)]
        public float Ksize = 1f;

        [Min(0f)]
        public float Kdist = 0.1f;

        [Min(0f)]
        public float Emax = 40f;
    }
}
