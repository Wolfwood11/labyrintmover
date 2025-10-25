using System.Collections;
using System.Collections.Generic;
using LabyrinthMover.Core;
using LabyrinthMover.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LabyrinthMover.UI
{
    public class InputRouter : MonoBehaviour
    {
        [SerializeField]
        private float swipeThreshold = 0.1f; // Уменьшаем порог для более чувствительного перетаскивания
        
        // Принудительно устанавливаем значение в Awake для предотвращения сериализации
        private void Awake()
        {
            swipeThreshold = 0.1f;
            Debug.Log($"InputRouter Awake: swipeThreshold принудительно установлен в {swipeThreshold}");
        }

        private GridModel grid;
        private MoveExecutor moveExecutor;
        private LabyrinthMover.Gameplay.PlayerController character;
        private HudController hud;
        private Vector2Int goalCell;
        private System.Action onGoalReached;
        private System.Action onResetRequested;

        private Camera mainCamera;
        private float gridScale = 1f;

        private int? selectedBlockId;
        private Vector2 pointerDownScreen;
        private Vector2Int currentDir;
        private Vector2Int? targetCell; // Целевая клетка для перетаскивания
        private Coroutine moveRoutine;

        private void Start()
        {
            Debug.Log($"InputRouter Start: swipeThreshold = {swipeThreshold}");
            Debug.Log($"InputRouter Start: this.GetInstanceID() = {this.GetInstanceID()}");
        }

        public void Init(GridModel gridModel, MoveExecutor executor, LabyrinthMover.Gameplay.PlayerController characterController, HudController hudController, Vector2Int goal, System.Action goalCallback, System.Action resetCallback)
        {
            Debug.Log($"InputRouter Init: swipeThreshold = {swipeThreshold}");
            
            grid = gridModel;
            moveExecutor = executor;
            character = characterController;
            hud = hudController;
            goalCell = goal;
            onGoalReached = goalCallback;
            onResetRequested = resetCallback;

            mainCamera = Camera.main;

            var gridConfig = FindFirstObjectByType<GridConfig>();
            if (gridConfig != null)
            {
                gridScale = Mathf.Max(0.0001f, gridConfig.TileSize / 100f);
            }
            else
            {
                gridScale = 1f;
            }

            if (hud != null)
            {
                hud.UndoRequested += OnUndoRequested;
                hud.ResetRequested += OnResetRequested;
            }
        }

        private void OnDestroy()
        {
            if (hud != null)
            {
                hud.UndoRequested -= OnUndoRequested;
                hud.ResetRequested -= OnResetRequested;
            }
        }

        private void Update()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touch = touchscreen.primaryTouch;
                if (touch.press.wasPressedThisFrame || touch.press.isPressed || touch.press.wasReleasedThisFrame)
                {
                    HandleTouchInput();
                    return;
                }
            }

            HandleMouseInput();
        }

        private void HandleMouseInput()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 position = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame)
            {
                OnPointerDown(position);
            }

            if (mouse.leftButton.isPressed)
            {
                OnPointerMove(position);
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                OnPointerUp(position);
            }
        }

        private void HandleTouchInput()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return;
            }

            var touch = touchscreen.primaryTouch;
            var phase = touch.phase.ReadValue();
            Vector2 position = touch.position.ReadValue();

            switch (phase)
            {
                case UnityEngine.InputSystem.TouchPhase.None:
                    break;
                case UnityEngine.InputSystem.TouchPhase.Began:
                    OnPointerDown(position);
                    break;
                case UnityEngine.InputSystem.TouchPhase.Moved:
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    OnPointerMove(position);
                    break;
                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    OnPointerUp(position);
                    break;
            }
        }

        private void OnPointerDown(Vector2 screenPosition)
        {
            pointerDownScreen = screenPosition;
            currentDir = Vector2Int.zero;
            targetCell = null; // Сбрасываем целевую клетку
            
            Vector2Int cellPos = ScreenToCell(screenPosition);
            selectedBlockId = grid?.GetBlockId(cellPos);
            
            Debug.Log($"OnPointerDown: screenPosition=({screenPosition.x:F1}, {screenPosition.y:F1}), cellPos={cellPos}, selectedBlockId={selectedBlockId}, pointerDownScreen=({pointerDownScreen.x:F1}, {pointerDownScreen.y:F1}), swipeThreshold={swipeThreshold}");
            
            // Если тапнули по блоку, подсвечиваем его
            if (selectedBlockId.HasValue)
            {
                Debug.Log($"Выбран блок {selectedBlockId.Value}");
                HighlightBlock(selectedBlockId.Value, true);
            }
            else
            {
                Debug.Log($"Тап по пустой клетке {cellPos}");
                // Очищаем подсветку предыдущего блока
                ClearAllHighlights();
                // Если тапнули по пустой клетке, проверяем можно ли дойти до блока
                TryMoveCharacterToBlock(cellPos);
            }
        }

        private void OnPointerMove(Vector2 screenPosition)
        {
            Vector2 delta = screenPosition - pointerDownScreen;
            Debug.Log($"OnPointerMove: selectedBlockId={selectedBlockId}, pointerDownScreen=({pointerDownScreen.x:F1}, {pointerDownScreen.y:F1}), currentScreen=({screenPosition.x:F1}, {screenPosition.y:F1}), delta=({delta.x:F3}, {delta.y:F3}), delta.magnitude={delta.magnitude:F3}, swipeThreshold={swipeThreshold}");
            
            if (!selectedBlockId.HasValue)
            {
                Debug.Log("OnPointerMove: нет выбранного блока, игнорируем");
                return;
            }

            // Проверяем, действительно ли указатель движется
            if (Vector2.Distance(screenPosition, pointerDownScreen) < 0.01f)
            {
                Debug.Log($"OnPointerMove: указатель не движется (расстояние < 0.01), игнорируем");
                return;
            }

            if (delta.magnitude < swipeThreshold)
            {
                Debug.Log($"OnPointerMove: delta.magnitude ({delta.magnitude:F3}) < swipeThreshold ({swipeThreshold}), игнорируем");
                return;
            }

            // Вычисляем целевую клетку под указателем
            Vector2Int newTargetCell = ScreenToCell(screenPosition);
            
            // Если целевая клетка не изменилась, игнорируем
            if (targetCell.HasValue && targetCell.Value == newTargetCell)
            {
                Debug.Log($"OnPointerMove: целевая клетка не изменилась ({newTargetCell}), игнорируем");
                return;
            }

            targetCell = newTargetCell;
            Debug.Log($"OnPointerMove: новая целевая клетка={newTargetCell}, delta={delta}");

            // Проверяем, можно ли перетащить блок (не соединен с игроком)
            if (CanDragBlock(selectedBlockId.Value))
            {
                Debug.Log($"Блок {selectedBlockId.Value} можно перетащить к позиции {newTargetCell}");
                
                // Вычисляем направление движения к целевой клетке
                Vector2Int dir = CalculateDirectionToTarget(selectedBlockId.Value, newTargetCell);
                if (dir != Vector2Int.zero)
                {
                    var preview = moveExecutor.PreviewSwipe(selectedBlockId.Value, dir);
                    if (preview.steps > 0)
                    {
                        hud?.ShowPreview(preview.steps, preview.startDistance, preview.endDistance, preview.cost);
                    }
                    else
                    {
                        hud?.ResetPreview();
                    }
                }
                else
                {
                    hud?.ShowPreview($"Целевая позиция {newTargetCell} недоступна для блока");
                }
            }
            else
            {
                Debug.Log($"Блок {selectedBlockId.Value} соединен с игроком");
                // Блок соединен с игроком - показываем сообщение
                hud?.ShowPreview("Блок соединен с игроком!\nПодойдите ближе к блоку для его перемещения.");
            }
        }

        private void OnPointerUp(Vector2 screenPosition)
        {
            Vector2Int releaseCell = ScreenToCell(screenPosition);
            Debug.Log($"OnPointerUp: selectedBlockId={selectedBlockId}, targetCell={targetCell}, releaseCell={releaseCell}");

            if (selectedBlockId.HasValue && targetCell.HasValue)
            {
                Debug.Log($"Попытка перетащить блок {selectedBlockId.Value} к позиции {targetCell.Value}");
                // Перетаскивание блока к целевой позиции
                if (CanDragBlock(selectedBlockId.Value))
                {
                    // Вычисляем направление движения к целевой клетке
                    Vector2Int dir = CalculateDirectionToTarget(selectedBlockId.Value, targetCell.Value);
                    if (dir != Vector2Int.zero)
                    {
                        float cost = moveExecutor.CommitSwipe(selectedBlockId.Value, dir);
                        Debug.Log($"Перетаскивание выполнено в направлении {dir}, стоимость: {cost}");
                        if (cost > 0f)
                        {
                            hud?.ResetPreview();
                        }
                    }
                    else
                    {
                        Debug.Log("Блок уже в целевой позиции или направление не определено");
                        hud?.ResetPreview();
                    }
                }
                else
                {
                    Debug.Log("Блок соединен с игроком, перетаскивание заблокировано");
                    hud?.ShowPreview("Блок соединен с игроком!\nПодойдите ближе к блоку для его перемещения.");
                }
            }
            else if (!selectedBlockId.HasValue)
            {
                Debug.Log($"Тап по пустой клетке {releaseCell}");
                // Тап по пустой клетке - движение персонажа
                TryMoveCharacter(releaseCell);
            }

            selectedBlockId = null;
            currentDir = Vector2Int.zero;
            targetCell = null;
            
            // Убираем подсветку всех блоков
            ClearAllHighlights();
        }

        private void TryMoveCharacter(Vector2Int targetCell)
        {
            if (character == null)
            {
                return;
            }

            if (targetCell == character.GridPos)
            {
                return;
            }

            if (character.TryMoveTo(targetCell))
            {
                Debug.Log($"Персонаж начал движение к позиции {targetCell}");
            }
            else
            {
                Debug.Log($"Не удалось найти путь к позиции {targetCell}");
            }
        }

        private Vector2Int ScreenToCell(Vector2 screenPosition)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            Vector3 world = mainCamera != null
                ? mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -mainCamera.transform.position.z))
                : Vector3.zero;

            int cellX = Mathf.RoundToInt(world.x / gridScale);
            int cellY = Mathf.RoundToInt(world.y / gridScale);
            Vector2Int cellPos = new Vector2Int(cellX, cellY);
            Debug.Log($"ScreenToCell: screen=({screenPosition.x:F1}, {screenPosition.y:F1}) -> world=({world.x:F2}, {world.y:F2}) -> cell=({cellPos.x}, {cellPos.y})");
            return cellPos;
        }
        
        /// <summary>
        /// Пытается переместить персонажа к блоку
        /// </summary>
        private void TryMoveCharacterToBlock(Vector2Int targetPos)
        {
            if (character == null || grid == null) 
            {
                Debug.LogWarning("Character or grid is null!");
                return;
            }
            
            Debug.Log($"Попытка движения к блоку в позиции {targetPos}");
            Debug.Log($"Текущая позиция персонажа: {character.GridPos}");
            
            // Проверяем, есть ли блок в целевой позиции
            int? blockId = grid.GetBlockId(targetPos);
            Debug.Log($"BlockId в позиции {targetPos}: {blockId}");
            
            if (!blockId.HasValue) 
            {
                Debug.Log("Блок не найден в целевой позиции");
                return;
            }
            
            // Находим ближайшую доступную позицию рядом с блоком
            Vector2Int? nearestPos = FindNearestAccessiblePositionToBlock(targetPos);
            if (nearestPos.HasValue)
            {
                Debug.Log($"Персонаж идет к позиции {nearestPos.Value} рядом с блоком в {targetPos}");
                bool moveSuccess = character.TryMoveTo(nearestPos.Value);
                Debug.Log($"Результат попытки движения: {moveSuccess}");
            }
            else
            {
                Debug.Log($"Персонаж не может дойти до блока в позиции {targetPos}");
            }
        }
        
        /// <summary>
        /// Находит ближайшую доступную позицию рядом с блоком
        /// </summary>
        private Vector2Int? FindNearestAccessiblePositionToBlock(Vector2Int blockPos)
        {
            if (grid == null || character == null) return null;
            
            // Получаем блок
            int? blockId = grid.GetBlockId(blockPos);
            if (!blockId.HasValue || !grid.Blocks.TryGetValue(blockId.Value, out var block))
            {
                Debug.LogWarning($"Блок не найден в позиции {blockPos}");
                return null;
            }
            
            Debug.Log($"Ищем доступные позиции рядом с блоком {blockId.Value} в {block.Rect}");
            
            var reachablePositions = character.GetReachablePositions();
            Debug.Log($"Доступных позиций для персонажа: {reachablePositions.Count}");
            
            Vector2Int? nearestPos = null;
            float minDistance = float.MaxValue;
            
            // Проверяем позиции вокруг блока
            for (int x = block.Rect.xMin - 1; x <= block.Rect.xMax; x++)
            {
                for (int y = block.Rect.yMin - 1; y <= block.Rect.yMax; y++)
                {
                    var pos = new Vector2Int(x, y);
                    
                    // Пропускаем позиции внутри блока
                    if (block.Rect.Contains(pos)) continue;
                    
                    // Проверяем, доступна ли позиция
                    if (reachablePositions.Contains(pos))
                    {
                        float distance = Vector2Int.Distance(character.GridPos, pos);
                        Debug.Log($"Доступная позиция {pos} на расстоянии {distance:F2} от персонажа");
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestPos = pos;
                        }
                    }
                }
            }
            
            if (nearestPos.HasValue)
            {
                Debug.Log($"Найдена ближайшая доступная позиция: {nearestPos.Value} на расстоянии {minDistance:F2}");
            }
            else
            {
                Debug.LogWarning("Не найдено доступных позиций рядом с блоком");
            }
            
            return nearestPos;
        }
        
        /// <summary>
        /// Вычисляет направление движения блока к целевой позиции
        /// </summary>
        private Vector2Int CalculateDirectionToTarget(int blockId, Vector2Int targetCell)
        {
            if (grid == null || !grid.Blocks.TryGetValue(blockId, out var block))
            {
                return Vector2Int.zero;
            }

            var currentPos = new Vector2Int(block.Rect.x, block.Rect.y);
            Vector2Int delta = targetCell - currentPos;
            
            // Определяем основное направление (горизонтальное или вертикальное)
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            {
                return new Vector2Int(delta.x > 0 ? 1 : -1, 0);
            }
            else if (Mathf.Abs(delta.y) > 0)
            {
                return new Vector2Int(0, delta.y > 0 ? 1 : -1);
            }
            
            return Vector2Int.zero; // Блок уже в целевой позиции
        }

        /// <summary>
        /// Проверяет, можно ли перетащить блок (не соединен с игроком)
        /// </summary>
        private bool CanDragBlock(int blockId)
        {
            if (character == null || grid == null) return true;
            
            if (grid.Blocks.TryGetValue(blockId, out var block))
            {
                return !character.IsBlockConnectedToPlayer(block);
            }
            
            return true;
        }

        private void OnUndoRequested()
        {
            moveExecutor?.Undo();
            hud?.ResetPreview();
        }

        private void OnResetRequested()
        {
            hud?.ResetPreview();
            onResetRequested?.Invoke();
        }
        
        /// <summary>
        /// Подсвечивает блок
        /// </summary>
        private void HighlightBlock(int blockId, bool highlight)
        {
            // Находим GameObject блока по ID
            var movableBlocks = FindObjectsByType<LabyrinthMover.Gameplay.MovableBlock>(FindObjectsSortMode.None);
            foreach (var block in movableBlocks)
            {
                if (block.GetInstanceID() == blockId)
                {
                    var renderer = block.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        renderer.color = highlight ? Color.yellow : Color.white;
                    }
                    break;
                }
            }
        }
        
        /// <summary>
        /// Убирает подсветку всех блоков
        /// </summary>
        private void ClearAllHighlights()
        {
            // Очищаем подсветку только у выбранного блока, если он есть
            if (selectedBlockId.HasValue)
            {
                HighlightBlock(selectedBlockId.Value, false);
            }
        }
    }
}
