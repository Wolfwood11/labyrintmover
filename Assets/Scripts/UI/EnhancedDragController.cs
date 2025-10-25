using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using LabyrinthMover.Core;
using LabyrinthMover.Gameplay;
using LabyrinthMover.UI;

namespace LabyrinthMover.UI
{
    /// <summary>
    /// Улучшенная система управления перетягиванием блоков с визуальной обратной связью
    /// </summary>
    public class EnhancedDragController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Настройки перетягивания")]
        [SerializeField] private bool showPathPreview = true;
        
        [Header("Визуальные эффекты")]
        [SerializeField] private Color highlightColor = Color.cyan;
        [SerializeField] private Color invalidColor = Color.red;
        [SerializeField] private float highlightIntensity = 1.5f;
        
        private GridModel gridModel;
        private MoveExecutor moveExecutor;
        private HudController hudController;
        private PathVisualizer pathVisualizer;
        private float gridScale = 1f;

        // Состояние перетягивания
        private bool isDragging = false;
        private int? selectedBlockId = null;
        private Vector2Int startCell;
        private Vector2Int targetCell;
        private Vector2Int lastTargetCell;
        private List<Vector2Int> currentPath = new List<Vector2Int>();
        
        // Визуальные компоненты
        private SpriteRenderer blockRenderer;
        private Color originalColor;
        private bool isHighlighted = false;
        
        private void Start()
        {
            InitializeComponents();
            SetupPathVisualizer();
        }
        
        /// <summary>
        /// Инициализирует необходимые компоненты
        /// </summary>
        private void InitializeComponents()
        {
            // Находим компоненты в сцене
            var levelManager = FindFirstObjectByType<LevelManager>();
            if (levelManager != null)
            {
                gridModel = levelManager.GetGridModel();
                moveExecutor = levelManager.GetComponent<MoveExecutor>();
            }

            var gridConfig = FindFirstObjectByType<GridConfig>();
            if (gridConfig != null)
            {
                gridScale = Mathf.Max(0.0001f, gridConfig.TileSize / 100f);
            }

            hudController = FindFirstObjectByType<HudController>();
            blockRenderer = GetComponent<SpriteRenderer>();

            if (blockRenderer != null)
            {
                originalColor = blockRenderer.color;
            }
        }
        
        /// <summary>
        /// Настраивает визуализатор пути
        /// </summary>
        private void SetupPathVisualizer()
        {
            // Создаем или находим PathVisualizer
            pathVisualizer = FindFirstObjectByType<PathVisualizer>();
            if (pathVisualizer == null)
            {
                var visualizerObject = new GameObject("PathVisualizer");
                pathVisualizer = visualizerObject.AddComponent<PathVisualizer>();
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            Vector2Int cellPos = ScreenToCell(eventData.position);
            int? blockId = GetBlockAtCell(cellPos);
            
            Debug.Log($"🎯 OnPointerDown: позиция {cellPos}, найден блок ID: {blockId}");
            
            if (blockId.HasValue)
            {
                // Дополнительная проверка: убеждаемся, что блок действительно существует
                if (IsValidBlockId(blockId.Value))
                {
                    StartDrag(blockId.Value, cellPos);
                }
                else
                {
                    Debug.LogWarning($"❌ Блок {blockId.Value} не найден в GridModel");
                }
            }
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            if (isDragging)
            {
                EndDrag(eventData.position);
            }
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (isDragging)
            {
                UpdateDrag(eventData.position);
            }
        }
        
        /// <summary>
        /// Начинает перетягивание блока
        /// </summary>
        private void StartDrag(int blockId, Vector2Int cellPos)
        {
            Debug.Log($"🎯 Начинаем перетягивание блока {blockId} из позиции {cellPos}");
            
            isDragging = true;
            selectedBlockId = blockId;
            startCell = cellPos;
            targetCell = cellPos;
            lastTargetCell = cellPos;
            
            // Подсвечиваем блок
            HighlightBlock(true);
            
            // Показываем информацию о блоке
            if (hudController != null)
            {
                hudController.ShowPreview($"Выбран блок {blockId}\nПеретащите в нужное место");
            }
        }
        
        /// <summary>
        /// Обновляет состояние перетягивания
        /// </summary>
        private void UpdateDrag(Vector2 screenPosition)
        {
            Vector2Int newTargetCell = ScreenToCell(screenPosition);
            
            // Если целевая клетка не изменилась, ничего не делаем
            if (newTargetCell == lastTargetCell)
            {
                return;
            }
            
            targetCell = newTargetCell;
            lastTargetCell = newTargetCell;
            
            // Проверяем, можно ли переместить блок в эту позицию
            if (CanMoveBlockTo(selectedBlockId.Value, targetCell))
            {
                // Вычисляем путь движения
                CalculateMovementPath();
                
                // Показываем превью пути
                if (showPathPreview && currentPath.Count > 0)
                {
                    ShowPathPreview();
                }
                
                // Обновляем информацию в HUD
                UpdateHudPreview();
            }
            else
            {
                // Показываем, что движение невозможно
                HidePathPreview();
                ShowInvalidPreview();
            }
        }
        
        /// <summary>
        /// Завершает перетягивание
        /// </summary>
        private void EndDrag(Vector2 screenPosition)
        {
            if (!isDragging || !selectedBlockId.HasValue)
            {
                return;
            }
            
            Debug.Log($"🎯 Завершаем перетягивание блока {selectedBlockId.Value} к позиции {targetCell}");
            
            // Скрываем превью
            HidePathPreview();
            HighlightBlock(false);
            
            // Выполняем движение, если это возможно
            if (CanMoveBlockTo(selectedBlockId.Value, targetCell) && currentPath.Count > 0)
            {
                ExecuteMovement();
            }
            else
            {
                Debug.Log("❌ Движение невозможно, отменяем");
                if (hudController != null)
                {
                    hudController.ResetPreview();
                }
            }
            
            // Сбрасываем состояние
            ResetDragState();
        }
        
        /// <summary>
        /// Вычисляет путь движения блока
        /// </summary>
        private void CalculateMovementPath()
        {
            if (!selectedBlockId.HasValue || gridModel == null)
            {
                currentPath.Clear();
                return;
            }
            
            // Получаем текущую позицию блока
            Vector2Int currentPos = GetBlockPosition(selectedBlockId.Value);
            
            // Вычисляем направление движения
            Vector2Int direction = CalculateDirection(currentPos, targetCell);
            
            if (direction == Vector2Int.zero)
            {
                currentPath.Clear();
                return;
            }
            
            // Симулируем движение для получения пути
            var preview = moveExecutor.PreviewSwipe(selectedBlockId.Value, direction);
            
            if (preview.steps > 0)
            {
                // Создаем путь на основе симуляции
                currentPath = GeneratePathFromSimulation(currentPos, direction, preview.steps);
            }
            else
            {
                currentPath.Clear();
            }
        }
        
        /// <summary>
        /// Генерирует путь на основе симуляции
        /// </summary>
        private List<Vector2Int> GeneratePathFromSimulation(Vector2Int startPos, Vector2Int direction, int steps)
        {
            var path = new List<Vector2Int>();
            Vector2Int currentPos = startPos;
            
            for (int i = 0; i < steps; i++)
            {
                currentPos += direction;
                path.Add(currentPos);
            }
            
            return path;
        }
        
        /// <summary>
        /// Показывает превью пути
        /// </summary>
        private void ShowPathPreview()
        {
            if (pathVisualizer != null && currentPath.Count > 0)
            {
                Vector2Int startPos = GetBlockPosition(selectedBlockId.Value);
                pathVisualizer.ShowPath(currentPath, startPos);
            }
        }
        
        /// <summary>
        /// Скрывает превью пути
        /// </summary>
        private void HidePathPreview()
        {
            if (pathVisualizer != null)
            {
                pathVisualizer.HidePath();
            }
        }
        
        /// <summary>
        /// Показывает превью невозможного движения
        /// </summary>
        private void ShowInvalidPreview()
        {
            if (hudController != null)
            {
                hudController.ShowPreview($"❌ Невозможно переместить блок в позицию {targetCell}");
            }
        }
        
        /// <summary>
        /// Обновляет превью в HUD
        /// </summary>
        private void UpdateHudPreview()
        {
            if (hudController == null || currentPath.Count == 0)
            {
                return;
            }
            
            var preview = moveExecutor.PreviewSwipe(selectedBlockId.Value, CalculateDirection(GetBlockPosition(selectedBlockId.Value), targetCell));
            
            if (preview.steps > 0)
            {
                hudController.ShowPreview(preview.steps, preview.startDistance, preview.endDistance, preview.cost);
            }
        }
        
        /// <summary>
        /// Выполняет движение блока
        /// </summary>
        private void ExecuteMovement()
        {
            Vector2Int direction = CalculateDirection(GetBlockPosition(selectedBlockId.Value), targetCell);
            
            if (direction != Vector2Int.zero)
            {
                float cost = moveExecutor.CommitSwipe(selectedBlockId.Value, direction);
                Debug.Log($"✅ Движение выполнено, стоимость: {cost}");
                
                if (hudController != null)
                {
                    hudController.ResetPreview();
                }
            }
        }
        
        /// <summary>
        /// Подсвечивает блок
        /// </summary>
        private void HighlightBlock(bool highlight)
        {
            if (blockRenderer == null) return;
            
            isHighlighted = highlight;
            
            if (highlight)
            {
                blockRenderer.color = highlightColor * highlightIntensity;
            }
            else
            {
                blockRenderer.color = originalColor;
            }
        }
        
        /// <summary>
        /// Сбрасывает состояние перетягивания
        /// </summary>
        private void ResetDragState()
        {
            isDragging = false;
            selectedBlockId = null;
            currentPath.Clear();
        }
        
        /// <summary>
        /// Преобразует экранные координаты в координаты сетки
        /// </summary>
        private Vector2Int ScreenToCell(Vector2 screenPosition)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                return Vector2Int.zero;
            }

            Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -cam.transform.position.z));
            int cellX = Mathf.RoundToInt(worldPos.x / gridScale);
            int cellY = Mathf.RoundToInt(worldPos.y / gridScale);
            return new Vector2Int(cellX, cellY);
        }
        
        /// <summary>
        /// Получает блок в указанной клетке
        /// </summary>
        private int? GetBlockAtCell(Vector2Int cellPos)
        {
            if (gridModel == null) return null;
            
            return gridModel.GetBlockIdAt(cellPos.x, cellPos.y);
        }
        
        /// <summary>
        /// Получает позицию блока
        /// </summary>
        private Vector2Int GetBlockPosition(int blockId)
        {
            if (gridModel == null || !gridModel.Blocks.TryGetValue(blockId, out var block))
            {
                return Vector2Int.zero;
            }
            
            return new Vector2Int(block.Rect.x, block.Rect.y);
        }
        
        /// <summary>
        /// Вычисляет направление движения
        /// </summary>
        private Vector2Int CalculateDirection(Vector2Int from, Vector2Int to)
        {
            Vector2Int delta = to - from;
            
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                return new Vector2Int(delta.x > 0 ? 1 : -1, 0);
            }
            else if (Mathf.Abs(delta.y) > 0)
            {
                return new Vector2Int(0, delta.y > 0 ? 1 : -1);
            }
            
            return Vector2Int.zero;
        }
        
        /// <summary>
        /// Проверяет, можно ли переместить блок в указанную позицию
        /// </summary>
        private bool CanMoveBlockTo(int blockId, Vector2Int targetPos)
        {
            if (gridModel == null) return false;
            
            Vector2Int currentPos = GetBlockPosition(blockId);
            Vector2Int direction = CalculateDirection(currentPos, targetPos);
            
            if (direction == Vector2Int.zero) return false;
            
            // Проверяем, есть ли путь в этом направлении
            var preview = moveExecutor.PreviewSwipe(blockId, direction);
            return preview.steps > 0;
        }
        
        /// <summary>
        /// Проверяет, является ли ID блока валидным
        /// </summary>
        private bool IsValidBlockId(int blockId)
        {
            if (gridModel == null) return false;
            
            return gridModel.Blocks.ContainsKey(blockId);
        }
    }
}
