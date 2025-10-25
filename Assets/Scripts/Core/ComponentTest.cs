using UnityEngine;
using UnityEditor;
using LabyrinthMover.Gameplay;
using LabyrinthMover.UI;

namespace LabyrinthMover.Core
{
    /// <summary>
    /// Простой тест для проверки добавления компонентов
    /// </summary>
    public class ComponentTest : MonoBehaviour
    {
        [ContextMenu("Тест добавления компонентов")]
        public void TestAddComponents()
        {
            Debug.Log("🧪 Начинаем тест добавления компонентов...");
            
            // Создаем тестовый объект
            var testObject = new GameObject("TestBlock");
            var movableBlock = testObject.AddComponent<MovableBlock>();
            movableBlock.Size = new Vector2Int(2, 1);
            
            Debug.Log("✅ Создан тестовый блок");
            
            try
            {
                // Пытаемся добавить EnhancedDragController
                var dragController = testObject.AddComponent<EnhancedDragController>();
                Debug.Log("✅ EnhancedDragController добавлен успешно");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Ошибка при добавлении EnhancedDragController: {e.Message}");
            }
            
            try
            {
                // Пытаемся добавить BlockAnimator
                var blockAnimator = testObject.AddComponent<BlockAnimator>();
                Debug.Log("✅ BlockAnimator добавлен успешно");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Ошибка при добавлении BlockAnimator: {e.Message}");
            }
            
            try
            {
                // Пытаемся добавить BoxCollider2D
                var collider = testObject.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(2, 1);
                collider.isTrigger = false;
                Debug.Log("✅ BoxCollider2D добавлен успешно");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ Ошибка при добавлении BoxCollider2D: {e.Message}");
            }
            
            Debug.Log("🧪 Тест завершен");
            
            // Удаляем тестовый объект
            DestroyImmediate(testObject);
        }
    }
}
