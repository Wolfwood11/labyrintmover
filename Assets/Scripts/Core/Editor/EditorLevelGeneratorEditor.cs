using UnityEngine;
using UnityEditor;
using LabyrinthMover.Core;

namespace LabyrinthMover.Core.Editor
{
    /// <summary>
    /// Кастомный редактор для EditorLevelGenerator
    /// </summary>
    [CustomEditor(typeof(EditorLevelGenerator))]
    public class EditorLevelGeneratorEditor : UnityEditor.Editor
    {
        private EditorLevelGenerator generator;
        
        private void OnEnable()
        {
            generator = (EditorLevelGenerator)target;
        }
        
        public override void OnInspectorGUI()
        {
            // Рисуем стандартный Inspector
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            
            // Добавляем заголовок для кнопок
            EditorGUILayout.LabelField("Генерация уровней", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            // Кнопка основной генерации
            GUI.enabled = !Application.isPlaying || !IsGenerating();
            if (GUILayout.Button("🎮 Сгенерировать уровень", GUILayout.Height(30)))
            {
                generator.GenerateLevelInEditor();
            }
            GUI.enabled = true;
            
            // Кнопка остановки генерации
            GUI.enabled = Application.isPlaying && IsGenerating();
            if (GUILayout.Button("⏹️ Остановить", GUILayout.Height(30)))
            {
                generator.StopGeneration();
            }
            GUI.enabled = true;
            
            // Кнопка принудительной остановки
            GUI.enabled = Application.isPlaying && IsGenerating();
            if (GUILayout.Button("🛑 Принудительно", GUILayout.Height(30)))
            {
                generator.ForceStopGeneration();
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Кнопки быстрой генерации
            EditorGUILayout.LabelField("Быстрая генерация:", EditorStyles.miniLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🟢 Easy", GUILayout.Height(25)))
            {
                generator.GenerateEasyLevel();
            }
            
            if (GUILayout.Button("🟡 Medium", GUILayout.Height(25)))
            {
                generator.GenerateMediumLevel();
            }
            
            if (GUILayout.Button("🔴 Hard", GUILayout.Height(25)))
            {
                generator.GenerateHardLevel();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Информационная панель
            DrawInfoPanel();
            
            // Обновляем Inspector при изменениях
            if (GUI.changed)
            {
                EditorUtility.SetDirty(generator);
            }
        }
        
        /// <summary>
        /// Рисует информационную панель
        /// </summary>
        private void DrawInfoPanel()
        {
            EditorGUILayout.LabelField("Информация", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField($"Размер уровня: {generator.levelWidth}x{generator.levelHeight}");
            EditorGUILayout.LabelField($"Сложность: {generator.difficulty}");
            EditorGUILayout.LabelField($"Максимум попыток: {generator.maxAttempts}");
            
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField($"Статус: {(IsGenerating() ? "🔄 Генерируется..." : "✅ Готов")}");
            }
            else
            {
                EditorGUILayout.LabelField("Статус: ⏸️ Редактор (запустите игру для генерации)");
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            // Подсказки
            EditorGUILayout.LabelField("Подсказки:", EditorStyles.miniLabel);
            EditorGUILayout.HelpBox(
                "• Генерация работает только в режиме Play\n" +
                "• Уровень генерируется до тех пор, пока не будет проходимым\n" +
                "• Максимум попыток можно настроить в настройках\n" +
                "• Используйте кнопки быстрой генерации для тестирования",
                MessageType.Info
            );
        }
        
        /// <summary>
        /// Проверяет, выполняется ли генерация
        /// </summary>
        private bool IsGenerating()
        {
            // Используем рефлексию для проверки приватного поля isGenerating
            var field = typeof(EditorLevelGenerator).GetField("isGenerating", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                return (bool)field.GetValue(generator);
            }
            
            return false;
        }
    }
}
