using System.Collections.Generic;
using UnityEngine;
using LabyrinthMover.Core;

namespace LabyrinthMover.UI
{
    /// <summary>
    /// Компонент для визуализации пути движения блока
    /// </summary>
    public class PathVisualizer : MonoBehaviour
    {
        [Header("Настройки визуализации")]
        [SerializeField] private Material pathMaterial;
        [SerializeField] private Color pathColor = Color.yellow;
        [SerializeField] private float pathWidth = 0.1f;
        
        [Header("Анимация")]
        [SerializeField] private float animationSpeed = 2f;
        [SerializeField] private bool showAnimation = true;
        
        private LineRenderer lineRenderer;
        private List<Vector2Int> currentPath = new List<Vector2Int>();
        private bool isAnimating = false;
        private float animationProgress = 0f;
        
        private void Awake()
        {
            SetupLineRenderer();
        }
        
        private void Update()
        {
            if (isAnimating && showAnimation)
            {
                UpdateAnimation();
            }
        }
        
        /// <summary>
        /// Настраивает LineRenderer для отображения пути
        /// </summary>
        private void SetupLineRenderer()
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = pathMaterial ?? CreateDefaultMaterial();
            lineRenderer.startColor = pathColor;
            lineRenderer.endColor = pathColor;
            lineRenderer.startWidth = pathWidth;
            lineRenderer.endWidth = pathWidth;
            lineRenderer.positionCount = 0;
            lineRenderer.useWorldSpace = true;
            lineRenderer.sortingOrder = 10; // Поверх других объектов
            lineRenderer.enabled = false;
        }
        
        /// <summary>
        /// Создает материал по умолчанию
        /// </summary>
        private Material CreateDefaultMaterial()
        {
            var material = new Material(Shader.Find("Sprites/Default"));
            material.color = pathColor;
            return material;
        }
        
        /// <summary>
        /// Показывает путь движения блока
        /// </summary>
        public void ShowPath(List<Vector2Int> path, Vector2Int startPos)
        {
            if (path == null || path.Count == 0)
            {
                HidePath();
                return;
            }
            
            currentPath = new List<Vector2Int>(path);
            
            // Создаем точки для LineRenderer
            var points = new Vector3[path.Count + 1];
            points[0] = new Vector3(startPos.x, startPos.y, 0f);
            
            for (int i = 0; i < path.Count; i++)
            {
                points[i + 1] = new Vector3(path[i].x, path[i].y, 0f);
            }
            
            lineRenderer.positionCount = points.Length;
            lineRenderer.SetPositions(points);
            lineRenderer.enabled = true;
            
            // Запускаем анимацию
            if (showAnimation)
            {
                StartAnimation();
            }
            
            Debug.Log($"🎯 Показан путь движения: {path.Count} шагов от {startPos}");
        }
        
        /// <summary>
        /// Скрывает путь
        /// </summary>
        public void HidePath()
        {
            lineRenderer.enabled = false;
            isAnimating = false;
            animationProgress = 0f;
            currentPath.Clear();
        }
        
        /// <summary>
        /// Запускает анимацию пути
        /// </summary>
        private void StartAnimation()
        {
            isAnimating = true;
            animationProgress = 0f;
        }
        
        /// <summary>
        /// Обновляет анимацию пути
        /// </summary>
        private void UpdateAnimation()
        {
            animationProgress += Time.deltaTime * animationSpeed;
            
            if (animationProgress >= 1f)
            {
                animationProgress = 0f; // Зацикливаем анимацию
            }
            
            // Обновляем цвет в зависимости от прогресса анимации
            float alpha = Mathf.PingPong(animationProgress * 2f, 1f);
            Color animatedColor = pathColor;
            animatedColor.a = alpha;
            lineRenderer.startColor = animatedColor;
            lineRenderer.endColor = animatedColor;
        }
        
        /// <summary>
        /// Обновляет цвет пути
        /// </summary>
        public void SetPathColor(Color color)
        {
            pathColor = color;
            if (lineRenderer != null)
            {
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
            }
        }
        
        /// <summary>
        /// Обновляет ширину пути
        /// </summary>
        public void SetPathWidth(float width)
        {
            pathWidth = width;
            if (lineRenderer != null)
            {
                lineRenderer.startWidth = width;
                lineRenderer.endWidth = width;
            }
        }
    }
}
