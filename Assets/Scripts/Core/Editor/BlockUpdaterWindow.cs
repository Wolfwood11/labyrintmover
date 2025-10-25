using UnityEngine;
using UnityEditor;
using LabyrinthMover.Gameplay;
using LabyrinthMover.UI;

namespace LabyrinthMover.Core
{
    /// <summary>
    /// Редакторская утилита для обновления блоков в сцене
    /// </summary>
    public class BlockUpdaterWindow : EditorWindow
    {
        [MenuItem("LabyrinthMover/Обновить блоки в сцене")]
        public static void ShowWindow()
        {
            GetWindow<BlockUpdaterWindow>("Обновление блоков");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("🔧 Обновление блоков в сцене", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label("Эта утилита добавит новые компоненты ко всем блокам в сцене:");
            GUILayout.Label("• EnhancedDragController - управление перетягиванием");
            GUILayout.Label("• BlockAnimator - анимация движения");
            GUILayout.Label("• BoxCollider2D - обработка касаний");
            GUILayout.Label("• PathVisualizer - визуализация пути");
            GUILayout.Label("• EventSystem - обработка событий");
            
            GUILayout.Space(20);
            
            if (GUILayout.Button("🚀 Обновить все блоки", GUILayout.Height(30)))
            {
                UpdateAllBlocksInScene();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("🗑️ Удалить добавленные компоненты", GUILayout.Height(30)))
            {
                RemoveAddedComponents();
            }
            
            GUILayout.Space(20);
            
            // Показываем статистику
            var blocks = FindObjectsByType<MovableBlock>(FindObjectsSortMode.None);
            var blocksWithDragController = 0;
            var blocksWithAnimator = 0;
            var blocksWithCollider = 0;
            
            foreach (var block in blocks)
            {
                if (block.GetComponent<EnhancedDragController>() != null) blocksWithDragController++;
                if (block.GetComponent<BlockAnimator>() != null) blocksWithAnimator++;
                if (block.GetComponent<BoxCollider2D>() != null) blocksWithCollider++;
            }
            
            GUILayout.Label("📊 Статистика блоков:", EditorStyles.boldLabel);
            GUILayout.Label($"Всего блоков: {blocks.Length}");
            GUILayout.Label($"С EnhancedDragController: {blocksWithDragController}");
            GUILayout.Label($"С BlockAnimator: {blocksWithAnimator}");
            GUILayout.Label($"С BoxCollider2D: {blocksWithCollider}");
            
            var pathVisualizer = FindFirstObjectByType<PathVisualizer>();
            var eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            
            GUILayout.Space(10);
            GUILayout.Label("🌐 Компоненты сцены:", EditorStyles.boldLabel);
            GUILayout.Label($"PathVisualizer: {(pathVisualizer != null ? "✅ Есть" : "❌ Нет")}");
            GUILayout.Label($"EventSystem: {(eventSystem != null ? "✅ Есть" : "❌ Нет")}");
        }
        
        private void UpdateAllBlocksInScene()
        {
            Debug.Log("🔧 Начинаем обновление блоков в сцене...");
            
            // Находим все блоки
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
            
            // Создаем PathVisualizer
            CreatePathVisualizerIfNeeded();
            
            // Создаем EventSystem
            CreateEventSystemIfNeeded();
            
            Debug.Log($"✅ Обновление завершено! Обновлено блоков: {updatedCount}/{blocks.Length}");
            
            // Обновляем окно
            Repaint();
        }
        
        private bool UpdateBlock(MovableBlock block)
        {
            bool wasUpdated = false;
            
            // EnhancedDragController
            if (block.GetComponent<EnhancedDragController>() == null)
            {
                var dragController = block.gameObject.AddComponent<EnhancedDragController>();
                Debug.Log($"✅ Добавлен EnhancedDragController к блоку {block.name}");
                wasUpdated = true;
            }
            
            // BlockAnimator
            if (block.GetComponent<BlockAnimator>() == null)
            {
                var blockAnimator = block.gameObject.AddComponent<BlockAnimator>();
                Debug.Log($"✅ Добавлен BlockAnimator к блоку {block.name}");
                wasUpdated = true;
            }
            
            // BoxCollider2D
            if (block.GetComponent<BoxCollider2D>() == null)
            {
                var collider = block.gameObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(block.Size.x, block.Size.y);
                collider.isTrigger = false;
                Debug.Log($"✅ Добавлен BoxCollider2D к блоку {block.name}");
                wasUpdated = true;
            }
            
            return wasUpdated;
        }
        
        private void CreatePathVisualizerIfNeeded()
        {
            var existingVisualizer = FindFirstObjectByType<PathVisualizer>();
            if (existingVisualizer == null)
            {
                var visualizerObject = new GameObject("PathVisualizer");
                visualizerObject.AddComponent<PathVisualizer>();
                Debug.Log("✅ Создан PathVisualizer");
            }
        }
        
        private void CreateEventSystemIfNeeded()
        {
            var existingEventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existingEventSystem == null)
            {
                var eventSystemObject = new GameObject("EventSystem");
                eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
                
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                eventSystemObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
                
                Debug.Log("✅ Создан EventSystem");
            }
        }
        
        private void RemoveAddedComponents()
        {
            Debug.Log("🗑️ Удаляем добавленные компоненты...");
            
            var blocks = FindObjectsByType<MovableBlock>(FindObjectsSortMode.None);
            
            foreach (var block in blocks)
            {
                // Удаляем компоненты
                var dragController = block.GetComponent<EnhancedDragController>();
                if (dragController != null)
                {
                    DestroyImmediate(dragController);
                }
                
                var blockAnimator = block.GetComponent<BlockAnimator>();
                if (blockAnimator != null)
                {
                    DestroyImmediate(blockAnimator);
                }
                
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
            Repaint();
        }
    }
}
