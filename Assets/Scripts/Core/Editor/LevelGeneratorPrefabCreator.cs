using UnityEngine;
using UnityEditor;
using LabyrinthMover.Core;

namespace LabyrinthMover.Core.Editor
{
    /// <summary>
    /// Утилиты для создания префабов генератора уровней
    /// </summary>
    public static class LevelGeneratorPrefabCreator
    {
        /// <summary>
        /// Создает GameObject с EditorLevelGenerator
        /// </summary>
        [MenuItem("Tools/LabyrinthMover/Create Level Generator")]
        public static void CreateLevelGenerator()
        {
            // Создаем GameObject
            var generatorGO = new GameObject("LevelGenerator");
            
            // Добавляем компонент EditorLevelGenerator
            var generator = generatorGO.AddComponent<EditorLevelGenerator>();
            
            // Настраиваем параметры по умолчанию
            generator.maxAttempts = 100;
            generator.levelWidth = 20;
            generator.levelHeight = 20;
            generator.difficulty = LevelDifficulty.Medium;
            generator.generateOnStart = false;
            
            // Выбираем созданный объект в Hierarchy
            Selection.activeGameObject = generatorGO;
            
            Debug.Log("✅ Создан LevelGenerator с настройками по умолчанию");
            Debug.Log("🎮 Используйте кнопки в Inspector для генерации уровней");
        }
        
        /// <summary>
        /// Создает префаб LevelGenerator
        /// </summary>
        [MenuItem("Tools/LabyrinthMover/Create Level Generator Prefab")]
        public static void CreateLevelGeneratorPrefab()
        {
            // Создаем GameObject
            var generatorGO = new GameObject("LevelGenerator");
            
            // Добавляем компонент EditorLevelGenerator
            var generator = generatorGO.AddComponent<EditorLevelGenerator>();
            
            // Настраиваем параметры по умолчанию
            generator.maxAttempts = 100;
            generator.levelWidth = 20;
            generator.levelHeight = 20;
            generator.difficulty = LevelDifficulty.Medium;
            generator.generateOnStart = false;
            
            // Создаем префаб
            string prefabPath = "Assets/Prefabs/LevelGenerator.prefab";
            
            // Создаем папку Prefabs, если её нет
            if (!System.IO.Directory.Exists("Assets/Prefabs"))
            {
                System.IO.Directory.CreateDirectory("Assets/Prefabs");
            }
            
            // Создаем префаб
            var prefab = PrefabUtility.SaveAsPrefabAsset(generatorGO, prefabPath);
            
            // Удаляем временный GameObject
            Object.DestroyImmediate(generatorGO);
            
            // Выбираем созданный префаб
            Selection.activeObject = prefab;
            
            Debug.Log($"✅ Создан префаб LevelGenerator: {prefabPath}");
            Debug.Log("🎮 Перетащите префаб в сцену для использования");
        }
    }
}
