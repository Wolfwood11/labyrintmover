using System;
using System.Collections.Generic;
using UnityEngine;

namespace LabyrinthMover.Core
{
    public class GridModel
    {
        public readonly int Width;
        public readonly int Height;

        public CellType[,] cells;
        public int?[,] blockIds;
        public Dictionary<int, BlockRt> blocks = new Dictionary<int, BlockRt>();

        public GridModel(int width, int height)
        {
            Width = width;
            Height = height;
            cells = new CellType[width, height];
            blockIds = new int?[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    cells[x, y] = CellType.Empty;
                    blockIds[x, y] = null;
                }
            }
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public void PlaceStatic(Vector2Int p)
        {
            if (!InBounds(p.x, p.y))
            {
                throw new ArgumentOutOfRangeException(nameof(p));
            }

            cells[p.x, p.y] = CellType.Static;
            blockIds[p.x, p.y] = null;
        }

        public void AddBlock(BlockRt block)
        {
            blocks[block.Id] = block;
            SetBlockArea(block.Rect, block.Id);
        }

        public void RemoveBlockArea(RectInt rect)
        {
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                for (int y = rect.yMin; y < rect.yMax; y++)
                {
                    if (!InBounds(x, y))
                    {
                        continue;
                    }

                    if (cells[x, y] == CellType.Static)
                    {
                        continue;
                    }

                    cells[x, y] = CellType.Empty;
                    blockIds[x, y] = null;
                }
            }
        }

        public void SetBlockArea(RectInt rect, int id)
        {
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                for (int y = rect.yMin; y < rect.yMax; y++)
                {
                    if (!InBounds(x, y))
                    {
                        throw new ArgumentOutOfRangeException(nameof(rect), $"Cell ({x},{y}) outside bounds");
                    }

                    cells[x, y] = CellType.Movable;
                    blockIds[x, y] = id;
                }
            }
        }

        public bool IsCellFree(Vector2Int pos)
        {
            if (!InBounds(pos.x, pos.y))
            {
                return false;
            }

            return cells[pos.x, pos.y] == CellType.Empty;
        }

        public int? GetBlockId(Vector2Int pos)
        {
            if (!InBounds(pos.x, pos.y))
            {
                return null;
            }

            return blockIds[pos.x, pos.y];
        }

        public BlockRt GetBlock(int id)
        {
            return blocks[id];
        }

        public void UpdateBlock(BlockRt block)
        {
            blocks[block.Id] = block;
        }

        public GridSnapshot CreateSnapshot()
        {
            var snapshot = new GridSnapshot(Width, Height);
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    snapshot.Cells[x, y] = cells[x, y];
                    snapshot.BlockIds[x, y] = blockIds[x, y];
                }
            }

            foreach (var kv in blocks)
            {
                snapshot.Blocks[kv.Key] = kv.Value;
            }

            return snapshot;
        }

        public void RestoreSnapshot(GridSnapshot snapshot)
        {
            if (snapshot.Width != Width || snapshot.Height != Height)
            {
                throw new ArgumentException("Snapshot dimensions do not match grid", nameof(snapshot));
            }

            Array.Copy(snapshot.Cells, cells, snapshot.Cells.Length);
            Array.Copy(snapshot.BlockIds, blockIds, snapshot.BlockIds.Length);

            blocks.Clear();
            foreach (var kv in snapshot.Blocks)
            {
                blocks[kv.Key] = kv.Value;
            }
        }
    }

    public class GridSnapshot
    {
        public int Width { get; }
        public int Height { get; }
        public CellType[,] Cells { get; }
        public int?[,] BlockIds { get; }
        public Dictionary<int, BlockRt> Blocks { get; }

        public GridSnapshot(int width, int height)
        {
            Width = width;
            Height = height;
            Cells = new CellType[width, height];
            BlockIds = new int?[width, height];
            Blocks = new Dictionary<int, BlockRt>();
        }
    }
}
