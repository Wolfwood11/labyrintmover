using UnityEngine;
using LabyrinthMover.Core;

namespace LabyrinthMover.Core
{
    /// <summary>
    /// Простой тест для проверки работы EditorLevelGenerator
    /// </summary>
    public class EditorLevelGeneratorTest : MonoBehaviour
    {
        [Header("Тест генератора")]
        [SerializeField] private EditorLevelGenerator generator;
        
        private void Start()
        {
            if (generator == null)
            {
                generator = FindFirstObjectByType<EditorLevelGenerator>();
            }
            
            if (generator != null)
            {
                Debug.Log("✅ EditorLevelGenerator найден и готов к работе");
                Debug.Log("🎮 Используйте кнопки в Inspector для генерации уровней");
            }
            else
            {
                Debug.LogWarning("⚠️ EditorLevelGenerator не найден в сцене");
                Debug.Log("💡 Создайте GameObject с компонентом EditorLevelGenerator");
            }
        }
        
        /// <summary>
        /// Тест быстрой генерации Easy уровня
        /// </summary>
        [ContextMenu("Тест: Сгенерировать Easy уровень")]
        public void TestGenerateEasyLevel()
        {
            if (generator != null)
            {
                Debug.Log("🧪 Тест: Генерация Easy уровня");
                generator.GenerateEasyLevel();
            }
            else
            {
                Debug.LogError("❌ Тест не может быть выполнен: EditorLevelGenerator не найден");
            }
        }
        
        /// <summary>
        /// Тест быстрой генерации Medium уровня
        /// </summary>
        [ContextMenu("Тест: Сгенерировать Medium уровень")]
        public void TestGenerateMediumLevel()
        {
            if (generator != null)
            {
                Debug.Log("🧪 Тест: Генерация Medium уровня");
                generator.GenerateMediumLevel();
            }
            else
            {
                Debug.LogError("❌ Тест не может быть выполнен: EditorLevelGenerator не найден");
            }
        }
        
        /// <summary>
        /// Тест быстрой генерации Hard уровня
        /// </summary>
        [ContextMenu("Тест: Сгенерировать Hard уровень")]
        public void TestGenerateHardLevel()
        {
            if (generator != null)
            {
                Debug.Log("🧪 Тест: Генерация Hard уровня");
                generator.GenerateHardLevel();
            }
            else
            {
                Debug.LogError("❌ Тест не может быть выполнен: EditorLevelGenerator не найден");
            }
        }
    }
}


