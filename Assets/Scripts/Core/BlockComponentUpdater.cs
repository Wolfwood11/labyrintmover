using UnityEngine;
using LabyrinthMover.Gameplay;
using LabyrinthMover.UI;

namespace LabyrinthMover.Core
{
    /// <summary>
    /// Утилита для добавления недостающих компонентов к существующим блокам
    /// </summary>
    public class BlockComponentUpdater : MonoBehaviour
    {
        [Header("Настройки обновления")]
        [SerializeField] private bool updateOnStart = true;
        [SerializeField] private bool showDebugLogs = true;
        
        private void Start()
        {
            if (updateOnStart)
            {
                UpdateAllBlocks();
            }
        }
        
        /// <summary>
        /// Обновляет все блоки в сцене, добавляя недостающие компоненты
        /// </summary>
        [ContextMenu("Обновить все блоки")]
        public void UpdateAllBlocks()
        {
            Debug.Log("🔧 Начинаем обновление блоков...");
            
            // Находим все блоки в сцене
            var blocks = FindObjectsByType<MovableBlock>(FindObjectsSortMode.None);
            Debug.Log($"📦 Найдено блоков: {blocks.Length}");
            
            int updatedCount = 0;
            
            foreach (var block in blocks)
            {
                if (UpdateBlock(block))
                {
                    updatedCount++;
                }
            }
            
            // Создаем PathVisualizer если его нет
            CreatePathVisualizerIfNeeded();
            
            // Создаем EventSystem если его нет
            CreateEventSystemIfNeeded();
            
            Debug.Log($"✅ Обновление завершено! Обновлено блоков: {updatedCount}/{blocks.Length}");
        }
        
        /// <summary>
        /// Обновляет конкретный блок
        /// </summary>
        private bool UpdateBlock(MovableBlock block)
        {
            bool wasUpdated = false;
            
            // Проверяем и добавляем EnhancedDragController
            if (block.GetComponent<EnhancedDragController>() == null)
            {
                var dragController = block.gameObject.AddComponent<EnhancedDragController>();
                if (showDebugLogs)
                {
                    Debug.Log($"✅ Добавлен EnhancedDragController к блоку {block.name}");
                }
                wasUpdated = true;
            }
            
            // Проверяем и добавляем BlockAnimator
            if (block.GetComponent<BlockAnimator>() == null)
            {
                var blockAnimator = block.gameObject.AddComponent<BlockAnimator>();
                if (showDebugLogs)
                {
                    Debug.Log($"✅ Добавлен BlockAnimator к блоку {block.name}");
                }
                wasUpdated = true;
            }
            
            // Проверяем и добавляем BoxCollider2D
            if (block.GetComponent<BoxCollider2D>() == null)
            {
                var collider = block.gameObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(block.Size.x, block.Size.y);
                collider.isTrigger = false;
                if (showDebugLogs)
                {
                    Debug.Log($"✅ Добавлен BoxCollider2D к блоку {block.name}");
                }
                wasUpdated = true;
            }
            
            return wasUpdated;
        }
        
        /// <summary>
        /// Создает PathVisualizer если его нет в сцене
        /// </summary>
        private void CreatePathVisualizerIfNeeded()
        {
            var existingVisualizer = FindFirstObjectByType<PathVisualizer>();
            if (existingVisualizer == null)
            {
                var visualizerObject = new GameObject("PathVisualizer");
                visualizerObject.AddComponent<PathVisualizer>();
                if (showDebugLogs)
                {
                    Debug.Log("✅ Создан PathVisualizer");
                }
            }
        }
        
        /// <summary>
        /// Создает EventSystem если его нет в сцене
        /// </summary>
        private void CreateEventSystemIfNeeded()
        {
            var existingEventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existingEventSystem == null)
            {
                var eventSystemObject = new GameObject("EventSystem");
                eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
                
                // Добавляем InputModule в зависимости от настроек проекта
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                eventSystemObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
                
                if (showDebugLogs)
                {
                    Debug.Log("✅ Создан EventSystem");
                }
            }
        }
        
        /// <summary>
        /// Удаляет все добавленные компоненты (для отладки)
        /// </summary>
        [ContextMenu("Удалить все добавленные компоненты")]
        public void RemoveAddedComponents()
        {
            Debug.Log("🗑️ Удаляем добавленные компоненты...");
            
            var blocks = FindObjectsByType<MovableBlock>(FindObjectsSortMode.None);
            
            foreach (var block in blocks)
            {
                // Удаляем EnhancedDragController
                var dragController = block.GetComponent<EnhancedDragController>();
                if (dragController != null)
                {
                    DestroyImmediate(dragController);
                }
                
                // Удаляем BlockAnimator
                var blockAnimator = block.GetComponent<BlockAnimator>();
                if (blockAnimator != null)
                {
                    DestroyImmediate(blockAnimator);
                }
                
                // Удаляем BoxCollider2D
                var collider = block.GetComponent<BoxCollider2D>();
                if (collider != null)
                {
                    DestroyImmediate(collider);
                }
            }
            
            // Удаляем PathVisualizer
            var visualizer = FindFirstObjectByType<PathVisualizer>();
            if (visualizer != null)
            {
                DestroyImmediate(visualizer.gameObject);
            }
            
            Debug.Log("✅ Удаление завершено");
        }
    }
}
