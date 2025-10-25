using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using LabyrinthMover.Gameplay;

namespace LabyrinthMover.Core
{
    /// <summary>
    /// Компонент для генерации уровней в редакторе
    /// </summary>
    public class LevelGeneratorComponent : MonoBehaviour
    {
        [Header("Параметры генерации")]
        public LevelGenerationParams GenerationParams = new LevelGenerationParams();
        
        [Header("Результат генерации")]
        public GeneratedLevel LastGeneratedLevel;
        
        [Header("Настройки")]
        public bool AutoValidate = true;
        public int MaxGenerationAttempts = 10;
        
        [ContextMenu("Сгенерировать уровень")]
        public void GenerateLevel()
        {
            int seed = Random.Range(0, int.MaxValue);
            GenerateLevel(seed);
        }
        
        public void GenerateLevel(int seed)
        {
            Debug.Log($"Генерация уровня с seed: {seed}");
            
            LastGeneratedLevel = LevelGenerator.GenerateLevel(GenerationParams, seed);
            
            if (LastGeneratedLevel.IsSolvable)
            {
                Debug.Log($"Уровень успешно сгенерирован! Сложность: {LastGeneratedLevel.Difficulty}, " +
                         $"Оценка энергии: {LastGeneratedLevel.EstimatedMinEnergy}, " +
                         $"Оценка ходов: {LastGeneratedLevel.EstimatedMinHops}");
            }
            else
            {
                Debug.LogWarning("Не удалось сгенерировать решаемый уровень");
            }
        }
        
        [ContextMenu("Применить уровень к сцене")]
        public void ApplyLevelToScene()
        {
            if (LastGeneratedLevel == null || !LastGeneratedLevel.IsSolvable)
            {
                Debug.LogWarning("Нет валидного уровня для применения");
                return;
            }
            
            ApplyLevelToScene(LastGeneratedLevel);
        }
        
        public void ApplyLevelToScene(GeneratedLevel level)
        {
            // Очищаем текущую сцену от существующих объектов
            ClearScene();
            
            // Создаем GridRoot с GridConfig
            var gridRoot = new GameObject("GridRoot");
            var gridConfig = gridRoot.AddComponent<GridConfig>();
            gridConfig.Width = level.Params.Width;
            gridConfig.Height = level.Params.Height;
            gridConfig.Ksize = level.Params.Ksize;
            gridConfig.Kdist = level.Params.Kdist;
            gridConfig.Emax = level.Params.Emax;
            
            // Создаем контейнеры
            var staticsContainer = new GameObject("Statics");
            staticsContainer.transform.SetParent(gridRoot.transform);
            
            var blocksContainer = new GameObject("Blocks");
            blocksContainer.transform.SetParent(gridRoot.transform);
            
            // Размещаем статические препятствия
            foreach (var pos in level.StaticPositions)
            {
                var staticTile = new GameObject($"StaticTile_{pos.x}_{pos.y}");
                staticTile.transform.SetParent(staticsContainer.transform);
                staticTile.transform.position = new Vector3(pos.x, pos.y, 0);
                staticTile.AddComponent<StaticTile>();
                
                // Добавляем визуальный компонент (квадрат)
                var renderer = staticTile.AddComponent<SpriteRenderer>();
                renderer.sprite = CreateSquareSprite();
                renderer.color = Color.gray;
            }
            
            // Размещаем подвижные блоки
            foreach (var placement in level.BlockPlacements)
            {
                var block = new GameObject($"MovableBlock_{placement.Id}");
                block.transform.SetParent(blocksContainer.transform);
                block.transform.position = new Vector3(placement.Position.x, placement.Position.y, 0);
                
                var movableBlock = block.AddComponent<MovableBlock>();
                movableBlock.Size = placement.Size;
                
                // Добавляем визуальный компонент
                var renderer = block.AddComponent<SpriteRenderer>();
                renderer.sprite = CreateSquareSprite();
                renderer.color = Color.blue;
                
                // Добавляем компоненты для улучшенного управления
                var dragController = block.AddComponent<LabyrinthMover.UI.EnhancedDragController>();
                var blockAnimator = block.AddComponent<LabyrinthMover.Gameplay.BlockAnimator>();
                
                // Добавляем коллайдер для обработки касаний
                var collider = block.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(placement.Size.x, placement.Size.y);
                collider.isTrigger = false;
            }
            
            // Размещаем маркеры старта и цели
            var startMarker = new GameObject("MarkerStart");
            startMarker.transform.position = new Vector3(level.StartPosition.x, level.StartPosition.y, 0);
            var startRenderer = startMarker.AddComponent<SpriteRenderer>();
            startRenderer.sprite = CreateSquareSprite();
            startRenderer.color = Color.green;
            
            var goalMarker = new GameObject("MarkerGoal");
            goalMarker.transform.position = new Vector3(level.GoalPosition.x, level.GoalPosition.y, 0);
            var goalRenderer = goalMarker.AddComponent<SpriteRenderer>();
            goalRenderer.sprite = CreateSquareSprite();
            goalRenderer.color = Color.red;
            
            Debug.Log("Уровень применен к сцене!");
        }
        
        private void ClearScene()
        {
            // Удаляем существующие объекты уровня
            var existingObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in existingObjects)
            {
                if (obj.name.Contains("GridRoot") || obj.name.Contains("StaticTile") || 
                    obj.name.Contains("MovableBlock") || obj.name.Contains("Marker"))
                {
                    if (Application.isPlaying)
                        Destroy(obj);
                    else
                        DestroyImmediate(obj);
                }
            }
        }
        
        private Sprite CreateSquareSprite()
        {
            var texture = new Texture2D(32, 32);
            var pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(LevelGeneratorComponent))]
    public class LevelGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            var generator = (LevelGeneratorComponent)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Генерация уровней", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Сгенерировать уровень"))
            {
                generator.GenerateLevel();
            }
            
            if (generator.LastGeneratedLevel != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Последний результат:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Seed: {generator.LastGeneratedLevel.Seed}");
                EditorGUILayout.LabelField($"Сложность: {generator.LastGeneratedLevel.Difficulty}");
                EditorGUILayout.LabelField($"Решаемость: {(generator.LastGeneratedLevel.IsSolvable ? "Да" : "Нет")}");
                EditorGUILayout.LabelField($"Оценка энергии: {generator.LastGeneratedLevel.EstimatedMinEnergy}");
                EditorGUILayout.LabelField($"Оценка ходов: {generator.LastGeneratedLevel.EstimatedMinHops}");
                
                if (generator.LastGeneratedLevel.IsSolvable && GUILayout.Button("Применить к сцене"))
                {
                    generator.ApplyLevelToScene();
                }
            }
        }
    }
#endif
}
