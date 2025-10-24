using System;
using System.Collections.Generic;
using LabyrinthMover.Core;
using UnityEngine;

namespace LabyrinthMover.Gameplay
{
    public struct PreviewResult
    {
        public int steps;
        public float cost;
        public RectInt finalRect;
        public int startDistance;
        public int endDistance;
    }

    public class MoveExecutor : MonoBehaviour
    {
        private GridModel grid;
        private CharacterController2D character;
        private float emax;
        private float ksize;
        private float kdist;

        private float energyRemaining;

        private readonly Stack<MoveRecord> history = new Stack<MoveRecord>();

        public event Action<float, float> EnergyChanged;

        private struct MoveRecord
        {
            public GridSnapshot Snapshot;
            public float EnergyBefore;
        }

        public float EnergyRemaining => energyRemaining;
        public float EnergyMax => emax;

        public void Init(GridModel gridModel, CharacterController2D characterController, float emaxValue, float ksizeValue, float kdistValue)
        {
            grid = gridModel ?? throw new ArgumentNullException(nameof(gridModel));
            character = characterController ?? throw new ArgumentNullException(nameof(characterController));
            emax = emaxValue;
            ksize = ksizeValue;
            kdist = kdistValue;
            energyRemaining = emax;
            history.Clear();
            RaiseEnergyChanged();
        }

        public PreviewResult PreviewSwipe(int blockId, Vector2Int dir)
        {
            if (grid == null || dir == Vector2Int.zero)
            {
                return default;
            }

            var snapshot = grid.CreateSnapshot();
            var sim = SimulateSwipe(snapshot, blockId, dir, character.GridPos);
            return new PreviewResult
            {
                steps = sim.steps,
                cost = sim.cost,
                finalRect = sim.finalRect,
                startDistance = sim.startDistance,
                endDistance = sim.endDistance
            };
        }

        public float CommitSwipe(int blockId, Vector2Int dir)
        {
            if (grid == null || dir == Vector2Int.zero)
            {
                return 0f;
            }

            var beforeSnapshot = grid.CreateSnapshot();
            var workingSnapshot = grid.CreateSnapshot();
            var sim = SimulateSwipe(workingSnapshot, blockId, dir, character.GridPos);
            if (sim.steps == 0)
            {
                return 0f;
            }

            grid.RestoreSnapshot(workingSnapshot);

            float energyBefore = energyRemaining;
            energyRemaining = energyBefore - sim.cost;

            history.Push(new MoveRecord
            {
                Snapshot = beforeSnapshot,
                EnergyBefore = energyBefore
            });

            RaiseEnergyChanged();
            return sim.cost;
        }

        public void Undo()
        {
            if (history.Count == 0)
            {
                return;
            }

            var record = history.Pop();
            grid.RestoreSnapshot(record.Snapshot);
            energyRemaining = record.EnergyBefore;
            RaiseEnergyChanged();
        }

        private void RaiseEnergyChanged()
        {
            EnergyChanged?.Invoke(energyRemaining, emax);
        }

        private SimulationResult SimulateSwipe(GridSnapshot snapshot, int blockId, Vector2Int dir, Vector2Int charPos)
        {
            var result = new SimulationResult();
            if (!snapshot.Blocks.TryGetValue(blockId, out var block))
            {
                return result;
            }

            dir = new Vector2Int(Mathf.Clamp(dir.x, -1, 1), Mathf.Clamp(dir.y, -1, 1));
            if (dir == Vector2Int.zero)
            {
                return result;
            }

            var currentRect = block.Rect;
            result.startDistance = Energy.ManhattanToNearestCell(charPos, currentRect);

            ClearRect(snapshot, currentRect);

            float cost = 0f;
            int steps = 0;

            while (true)
            {
                var nextRect = new RectInt(currentRect.x + dir.x, currentRect.y + dir.y, currentRect.width, currentRect.height);
                if (!IsRectInBounds(snapshot, nextRect))
                {
                    break;
                }

                bool blockedByStatic = false;
                int? touchedId = null;
                for (int x = nextRect.xMin; x < nextRect.xMax && !blockedByStatic; x++)
                {
                    for (int y = nextRect.yMin; y < nextRect.yMax; y++)
                    {
                        if (snapshot.Cells[x, y] == CellType.Static)
                        {
                            blockedByStatic = true;
                            break;
                        }

                        var id = snapshot.BlockIds[x, y];
                        if (id.HasValue && id.Value != blockId)
                        {
                            if (touchedId.HasValue && touchedId.Value != id.Value)
                            {
                                throw new InvalidOperationException("Multiple block collision detected");
                            }

                            touchedId = id.Value;
                        }
                    }
                }

                if (blockedByStatic)
                {
                    break;
                }

                if (touchedId.HasValue)
                {
                    if (!snapshot.Blocks.TryGetValue(touchedId.Value, out var touchedBlock))
                    {
                        break;
                    }

                    ClearRect(snapshot, touchedBlock.Rect);
                    snapshot.Blocks.Remove(touchedId.Value);
                    currentRect = BoundingBox(nextRect, touchedBlock.Rect);
                    block.Rect = currentRect;
                }
                else
                {
                    currentRect = nextRect;
                    block.Rect = currentRect;
                }

                int S = currentRect.width * currentRect.height;
                int d = Energy.ManhattanToNearestCell(charPos, currentRect);
                cost += Energy.StepCost(S, d, ksize, kdist);
                steps++;
            }

            result.steps = steps;
            result.cost = cost;
            result.finalRect = currentRect;
            result.endDistance = Energy.ManhattanToNearestCell(charPos, currentRect);

            snapshot.Blocks[blockId] = block;
            FillRect(snapshot, currentRect, blockId);

            return result;
        }

        private static bool IsRectInBounds(GridSnapshot snapshot, RectInt rect)
        {
            return rect.xMin >= 0 && rect.yMin >= 0 && rect.xMax <= snapshot.Width && rect.yMax <= snapshot.Height;
        }

        private static void ClearRect(GridSnapshot snapshot, RectInt rect)
        {
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                for (int y = rect.yMin; y < rect.yMax; y++)
                {
                    snapshot.Cells[x, y] = CellType.Empty;
                    snapshot.BlockIds[x, y] = null;
                }
            }
        }

        private static void FillRect(GridSnapshot snapshot, RectInt rect, int blockId)
        {
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                for (int y = rect.yMin; y < rect.yMax; y++)
                {
                    snapshot.Cells[x, y] = CellType.Movable;
                    snapshot.BlockIds[x, y] = blockId;
                }
            }
        }

        public bool CanStep(RectInt rect, Vector2Int dir)
        {
            if (grid == null)
            {
                return false;
            }

            var next = new RectInt(rect.x + dir.x, rect.y + dir.y, rect.width, rect.height);
            if (next.xMin < 0 || next.yMin < 0 || next.xMax > grid.Width || next.yMax > grid.Height)
            {
                return false;
            }

            for (int x = next.xMin; x < next.xMax; x++)
            {
                for (int y = next.yMin; y < next.yMax; y++)
                {
                    if (grid.cells[x, y] == CellType.Static)
                    {
                        return false;
                    }

                    var id = grid.blockIds[x, y];
                    if (id.HasValue)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public int? TouchMovable(RectInt rect, Vector2Int dir)
        {
            if (grid == null)
            {
                return null;
            }

            var edge = new RectInt(rect.x + dir.x, rect.y + dir.y, rect.width, rect.height);
            if (edge.xMin < 0 || edge.yMin < 0 || edge.xMax > grid.Width || edge.yMax > grid.Height)
            {
                return null;
            }

            for (int x = edge.xMin; x < edge.xMax; x++)
            {
                for (int y = edge.yMin; y < edge.yMax; y++)
                {
                    var id = grid.blockIds[x, y];
                    if (id.HasValue)
                    {
                        return id.Value;
                    }
                }
            }

            return null;
        }

        public static RectInt BoundingBox(RectInt a, RectInt b)
        {
            int xMin = Mathf.Min(a.xMin, b.xMin);
            int yMin = Mathf.Min(a.yMin, b.yMin);
            int xMax = Mathf.Max(a.xMax, b.xMax);
            int yMax = Mathf.Max(a.yMax, b.yMax);
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private struct SimulationResult
        {
            public int steps;
            public float cost;
            public RectInt finalRect;
            public int startDistance;
            public int endDistance;
        }
    }
}
