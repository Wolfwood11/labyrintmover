using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LabyrinthMover.Core;
using LabyrinthMover.UI;

namespace LabyrinthMover.Gameplay
{
    /// <summary>
    /// Контроллер персонажа - управляет движением и анимацией
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Настройки движения")]
        [SerializeField] private float moveSpeed = 3f;
        
        [Header("Компоненты")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Animator animator;
        
        [Header("Спрайты")]
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite moveSprite;
        
        private Vector2Int currentGridPos;
        private Vector2Int targetGridPos;
        private bool isMoving = false;
        private List<Vector2Int> currentPath = new List<Vector2Int>();
        private int pathIndex = 0;
        
        private GridModel gridModel;
        private CameraController cameraController;
        private Vector2Int goalPosition;
        
        public Vector2Int GridPos => currentGridPos;
        public bool IsMoving => isMoving;
        
        public event System.Action OnPlayerReachedGoal;
        
        private void Start()
        {
            // Получаем компоненты
            gridModel = FindFirstObjectByType<LevelManager>()?.GetGridModel();
            cameraController = FindFirstObjectByType<CameraController>();
            
            // Устанавливаем правильную позицию персонажа
            currentGridPos = new Vector2Int(1, 1);
            targetGridPos = currentGridPos;
            SetGridPosition(currentGridPos);
            
            // Находим позицию цели
            goalPosition = FindGoalPosition();
            
            // Устанавливаем спрайт по умолчанию
            if (spriteRenderer != null && idleSprite != null)
            {
                spriteRenderer.sprite = idleSprite;
            }
            
            Debug.Log($"Персонаж создан в позиции: {currentGridPos}");
        }
        
        private void Update()
        {
            if (isMoving && currentPath.Count > 0)
            {
                MoveAlongPath();
            }
        }
        
        /// <summary>
        /// Устанавливает позицию персонажа на сетке
        /// </summary>
        public void SetGridPosition(Vector2Int newPos)
        {
            currentGridPos = newPos;
            targetGridPos = newPos;
            transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);
            
            // Обновляем камеру
            if (cameraController != null)
            {
                cameraController.FollowTarget(transform);
            }
        }
        
        /// <summary>
        /// Пытается найти путь к указанной позиции
        /// </summary>
        public bool TryMoveTo(Vector2Int targetPos)
        {
            if (gridModel == null)
            {
                Debug.LogWarning("GridModel не найден!");
                return false;
            }
            
            if (isMoving)
            {
                Debug.Log("Персонаж уже движется!");
                return false;
            }
            
            if (currentGridPos == targetPos)
            {
                Debug.Log("Персонаж уже в целевой позиции!");
                return true;
            }
            
            // Находим путь
            var path = FindPath(currentGridPos, targetPos);
            if (path == null || path.Count == 0)
            {
                Debug.Log($"Не удалось найти путь от {currentGridPos} до {targetPos}");
                return false;
            }
            
            // Начинаем движение
            currentPath = path;
            pathIndex = 0;
            isMoving = true;
            targetGridPos = targetPos;
            
            Debug.Log($"Найден путь из {currentGridPos} в {targetPos}, длина: {path.Count}");
            return true;
        }
        
        /// <summary>
        /// Движение по пути
        /// </summary>
        private void MoveAlongPath()
        {
            if (pathIndex >= currentPath.Count)
            {
                // Достигли конца пути
                isMoving = false;
                currentPath.Clear();
                pathIndex = 0;
                currentGridPos = targetGridPos;
                
                // Возвращаемся к idle спрайту
                if (spriteRenderer != null && idleSprite != null)
                {
                    spriteRenderer.sprite = idleSprite;
                }
                
                Debug.Log($"Персонаж достиг цели: {currentGridPos}");
                CheckGoalReached();
                return;
            }
            
            Vector2Int nextPos = currentPath[pathIndex];
            Vector3 targetWorldPos = new Vector3(nextPos.x, nextPos.y, transform.position.z);
            
            // Плавное движение к следующей точке
            float step = moveSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, step);
            
            // Обновляем спрайт движения
            if (spriteRenderer != null && moveSprite != null)
            {
                spriteRenderer.sprite = moveSprite;
            }
            
            // Проверяем, достигли ли следующей точки
            if (Vector3.Distance(transform.position, targetWorldPos) < 0.1f)
            {
                transform.position = targetWorldPos;
                currentGridPos = nextPos;
                pathIndex++;
                
                Debug.Log($"Персонаж перешел в позицию: {currentGridPos}");
            }
        }
        
        /// <summary>
        /// Поиск пути от start до end
        /// </summary>
        private List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
        {
            if (gridModel == null) 
            {
                Debug.LogWarning("GridModel is null!");
                return null;
            }
            
            Debug.Log($"Поиск пути от {start} до {end}");
            
            // Простой BFS для поиска пути
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            
            queue.Enqueue(start);
            visited.Add(start);
            
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            int steps = 0;
            int maxSteps = 1000; // Ограничение для отладки
            
            while (queue.Count > 0 && steps < maxSteps)
            {
                steps++;
                Vector2Int current = queue.Dequeue();
                
                if (current == end)
                {
                    // Восстанавливаем путь
                    List<Vector2Int> path = new List<Vector2Int>();
                    Vector2Int pathPos = end;
                    
                    while (pathPos != start)
                    {
                        path.Add(pathPos);
                        pathPos = cameFrom[pathPos];
                    }
                    path.Reverse();
                    Debug.Log($"Путь найден! Длина: {path.Count}, шагов поиска: {steps}");
                    return path;
                }
                
                // Проверяем соседние клетки
                foreach (Vector2Int dir in directions)
                {
                    Vector2Int next = current + dir;
                    
                    if (!visited.Contains(next) && 
                        gridModel.InBounds(next.x, next.y) && 
                        !gridModel.IsStatic(next.x, next.y) &&
                        !IsBlockAt(next))
                    {
                        visited.Add(next);
                        cameFrom[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }
            
            Debug.LogWarning($"Путь не найден! Проверено {steps} позиций, посещено {visited.Count} клеток");
            Debug.LogWarning($"Доступные позиции рядом со стартом: {string.Join(", ", GetAdjacentPositions(start))}");
            return null; // Путь не найден
        }
        
        /// <summary>
        /// Получает соседние позиции для отладки
        /// </summary>
        private List<Vector2Int> GetAdjacentPositions(Vector2Int pos)
        {
            List<Vector2Int> adjacent = new List<Vector2Int>();
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            foreach (Vector2Int dir in directions)
            {
                Vector2Int next = pos + dir;
                if (gridModel != null && gridModel.InBounds(next.x, next.y))
                {
                    bool isStatic = gridModel.IsStatic(next.x, next.y);
                    bool hasBlock = IsBlockAt(next);
                    adjacent.Add(next);
                    Debug.Log($"Позиция {next}: статическая={isStatic}, блок={hasBlock}");
                }
            }
            
            return adjacent;
        }
        
        /// <summary>
        /// Проверяет, есть ли блок в указанной позиции
        /// </summary>
        private bool IsBlockAt(Vector2Int pos)
        {
            if (gridModel == null) return false;
            
            foreach (var block in gridModel.Blocks.Values)
            {
                if (block.Rect.Contains(pos))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Проверяет, соединен ли блок с позицией персонажа
        /// </summary>
        public bool IsBlockConnectedToPlayer(Vector2Int blockPos)
        {
            if (gridModel == null) return false;
            
            // Находим блок, который содержит blockPos
            foreach (var block in gridModel.Blocks.Values)
            {
                if (block.Rect.Contains(blockPos))
                {
                    return IsBlockConnectedToPlayer(block);
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Проверяет, соединен ли блок с позицией персонажа
        /// </summary>
        public bool IsBlockConnectedToPlayer(BlockRt block)
        {
            if (gridModel == null) return false;
            
            // Блок считается соединенным с игроком только если:
            // 1. Игрок находится непосредственно рядом с блоком (соседние клетки)
            // 2. Или игрок находится на пути к блоку (в процессе движения к нему)
            
            // Проверяем, находится ли игрок рядом с блоком
            for (int x = block.Rect.xMin - 1; x <= block.Rect.xMax; x++)
            {
                for (int y = block.Rect.yMin - 1; y <= block.Rect.yMax; y++)
                {
                    var pos = new Vector2Int(x, y);
                    if (pos == currentGridPos)
                    {
                        Debug.Log($"Блок {block.Id} соединен с игроком - игрок находится рядом в позиции {pos}");
                        return true;
                    }
                }
            }
            
            // Проверяем, движется ли игрок к блоку
            if (isMoving && currentPath.Count > 0)
            {
                foreach (var pathPos in currentPath)
                {
                    for (int x = block.Rect.xMin - 1; x <= block.Rect.xMax; x++)
                    {
                        for (int y = block.Rect.yMin - 1; y <= block.Rect.yMax; y++)
                        {
                            var pos = new Vector2Int(x, y);
                            if (pos == pathPos)
                            {
                                Debug.Log($"Блок {block.Id} соединен с игроком - игрок движется к позиции {pos}");
                                return true;
                            }
                        }
                    }
                }
            }
            
            Debug.Log($"Блок {block.Id} НЕ соединен с игроком. Позиция игрока: {currentGridPos}, блок: {block.Rect}");
            return false;
        }
        
        /// <summary>
        /// Останавливает движение персонажа
        /// </summary>
        public void StopMovement()
        {
            isMoving = false;
            currentPath.Clear();
            pathIndex = 0;
            
            if (spriteRenderer != null && idleSprite != null)
            {
                spriteRenderer.sprite = idleSprite;
            }
        }
        
        /// <summary>
        /// Получает все позиции, до которых персонаж может дойти
        /// </summary>
        public HashSet<Vector2Int> GetReachablePositions()
        {
            if (gridModel == null) return new HashSet<Vector2Int>();
            
            HashSet<Vector2Int> reachable = new HashSet<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            
            queue.Enqueue(currentGridPos);
            visited.Add(currentGridPos);
            
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                reachable.Add(current);
                
                foreach (Vector2Int dir in directions)
                {
                    Vector2Int next = current + dir;
                    
                    if (!visited.Contains(next) && 
                        gridModel.InBounds(next.x, next.y) && 
                        !gridModel.IsStatic(next.x, next.y) &&
                        !IsBlockAt(next))
                    {
                        visited.Add(next);
                        queue.Enqueue(next);
                    }
                }
            }
            
            return reachable;
        }
        
        /// <summary>
        /// Находит позицию цели
        /// </summary>
        private Vector2Int FindGoalPosition()
        {
            var goalMarker = GameObject.Find("MarkerGoal");
            if (goalMarker != null)
            {
                return new Vector2Int(
                    Mathf.RoundToInt(goalMarker.transform.position.x),
                    Mathf.RoundToInt(goalMarker.transform.position.y)
                );
            }
            
            // Если маркер не найден, возвращаем позицию по умолчанию
            return new Vector2Int(18, 18); // Для уровня 20x20
        }
        
        /// <summary>
        /// Проверяет, достиг ли персонаж цели
        /// </summary>
        private void CheckGoalReached()
        {
            if (currentGridPos == goalPosition)
            {
                Debug.Log("Персонаж достиг цели!");
                OnPlayerReachedGoal?.Invoke();
            }
        }
    }
}
