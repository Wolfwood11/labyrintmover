using UnityEngine;
using LabyrinthMover.Core;
using LabyrinthMover.UI;

namespace LabyrinthMover.Gameplay
{
    /// <summary>
    /// Упрощенный генератор тестового уровня 20x20 без решателя
    /// </summary>
    public class SimpleTestLevelGenerator : MonoBehaviour
    {
        [Header("Настройки")]
        [SerializeField] private bool generateOnStart = true;
        
        private void Start()
        {
            if (generateOnStart)
            {
                GenerateSimpleLevel();
            }
        }
        
        [ContextMenu("Сгенерировать простой уровень 20x20")]
        public void GenerateSimpleLevel()
        {
            Debug.Log("Генерация простого уровня 20x20...");
            
            // Очищаем сцену
            ClearScene();
            
            // Создаем GridRoot
            var gridRoot = new GameObject("GridRoot");
            var gridConfig = gridRoot.AddComponent<GridConfig>();
            gridConfig.Width = 20;
            gridConfig.Height = 20;
            gridConfig.Ksize = 1.0f;
            gridConfig.Kdist = 0.1f;
            gridConfig.Emax = 60f;
            
            // Создаем контейнеры
            var staticsContainer = new GameObject("Statics");
            staticsContainer.transform.SetParent(gridRoot.transform);
            
            var blocksContainer = new GameObject("Blocks");
            blocksContainer.transform.SetParent(gridRoot.transform);
            
            // Размещаем статические препятствия (10% плотность)
            int staticCount = Mathf.RoundToInt(20 * 20 * 0.1f); // 40 препятствий
            var availablePositions = new System.Collections.Generic.List<Vector2Int>();
            
            // Заполняем доступные позиции (исключаем края для старта и цели)
            for (int x = 1; x < 19; x++)
            {
                for (int y = 1; y < 19; y++)
                {
                    availablePositions.Add(new Vector2Int(x, y));
                }
            }
            
            // Размещаем препятствия
            for (int i = 0; i < staticCount && availablePositions.Count > 0; i++)
            {
                int index = Random.Range(0, availablePositions.Count);
                var pos = availablePositions[index];
                availablePositions.RemoveAt(index);
                
                CreateStaticTile(pos, staticsContainer.transform);
            }
            
            // Размещаем подвижные блоки
            var blockPositions = new System.Collections.Generic.List<Vector2Int>();
            var startPos = new Vector2Int(0, 0);
            var goalPos = new Vector2Int(19, 19);
            
            for (int x = 2; x < 18; x++)
            {
                for (int y = 2; y < 18; y++)
                {
                    var pos = new Vector2Int(x, y);
                    // Исключаем позиции рядом со стартом и целью
                    if (pos != startPos && pos != goalPos && 
                        !IsPositionOccupied(pos, staticsContainer.transform))
                    {
                        blockPositions.Add(pos);
                    }
                }
            }
            
            // Размещаем 8 блоков
            for (int i = 0; i < 8 && blockPositions.Count > 0; i++)
            {
                int index = Random.Range(0, blockPositions.Count);
                var pos = blockPositions[index];
                blockPositions.RemoveAt(index);
                
                CreateMovableBlock(pos, i, blocksContainer.transform);
            }
            
            // Размещаем маркеры старта и цели
            var finalStartPos = new Vector2Int(0, 0);
            var finalGoalPos = new Vector2Int(19, 19);
            
            CreateMarker("MarkerStart", finalStartPos, Color.green);
            CreateMarker("MarkerGoal", finalGoalPos, Color.red);
            
            // Создаем HUD
            CreateHUD();
            
            // Настраиваем камеру
            SetupCamera();
            
            Debug.Log("✅ Простой уровень 20x20 создан!");
            Debug.Log($"📊 Статистика:");
            Debug.Log($"   - Статических препятствий: {staticCount}");
            Debug.Log($"   - Подвижных блоков: 8");
            Debug.Log($"   - Старт: {finalStartPos}");
            Debug.Log($"   - Цель: {finalGoalPos}");
        }
        
        private bool IsPositionOccupied(Vector2Int pos, Transform staticsContainer)
        {
            foreach (Transform child in staticsContainer)
            {
                var childPos = new Vector2Int(
                    Mathf.RoundToInt(child.position.x),
                    Mathf.RoundToInt(child.position.y)
                );
                if (childPos == pos)
                    return true;
            }
            return false;
        }
        
        private void CreateStaticTile(Vector2Int pos, Transform parent)
        {
            var staticTile = new GameObject($"StaticTile_{pos.x}_{pos.y}");
            staticTile.transform.SetParent(parent);
            staticTile.transform.position = new Vector3(pos.x, pos.y, 0);
            staticTile.AddComponent<StaticTile>();
            
            var renderer = staticTile.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSquareSprite();
            renderer.color = Color.gray;
        }
        
        private void CreateMovableBlock(Vector2Int pos, int id, Transform parent)
        {
            var block = new GameObject($"MovableBlock_{id}");
            block.transform.SetParent(parent);
            block.transform.position = new Vector3(pos.x, pos.y, 0);
            
            var movableBlock = block.AddComponent<MovableBlock>();
            movableBlock.Size = Vector2Int.one; // Простые блоки 1x1
            
            var renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSquareSprite();
            renderer.color = Color.blue;
        }
        
        private void CreateMarker(string name, Vector2Int pos, Color color)
        {
            var marker = new GameObject(name);
            marker.transform.position = new Vector3(pos.x, pos.y, 0);
            var renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSquareSprite();
            renderer.color = color;
        }
        
        private void CreateHUD()
        {
            var hudGO = new GameObject("HUD");
            hudGO.AddComponent<HudController>();
        }
        
        private void SetupCamera()
        {
            var cameraController = FindFirstObjectByType<CameraController>();
            if (cameraController != null)
            {
                cameraController.CenterOnGrid(20, 20);
                cameraController.SetZoom(12f);
            }
        }
        
        private void ClearScene()
        {
            var existingObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in existingObjects)
            {
                if (obj.name.Contains("GridRoot") || obj.name.Contains("StaticTile") || 
                    obj.name.Contains("MovableBlock") || obj.name.Contains("Marker") ||
                    obj.name.Contains("HUD"))
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
}
