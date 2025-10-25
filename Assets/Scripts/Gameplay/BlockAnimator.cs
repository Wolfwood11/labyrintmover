using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LabyrinthMover.Core;

namespace LabyrinthMover.Gameplay
{
    /// <summary>
    /// Компонент для анимации движения блока по пути
    /// </summary>
    public class BlockAnimator : MonoBehaviour
    {
        [Header("Настройки анимации")]
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private bool smoothMovement = true;
        
        [Header("Визуальные эффекты")]
        [SerializeField] private ParticleSystem moveEffect;
        [SerializeField] private AudioSource moveSound;
        
        private bool isAnimating = false;
        private List<Vector2Int> currentPath = new List<Vector2Int>();
        private int currentStep = 0;
        private Vector3 startPosition;
        private Vector3 targetPosition;
        private float animationProgress = 0f;
        
        public bool IsAnimating => isAnimating;
        
        /// <summary>
        /// Запускает анимацию движения блока по пути
        /// </summary>
        public void AnimateMovement(List<Vector2Int> path, Vector2Int startPos)
        {
            if (path == null || path.Count == 0)
            {
                Debug.LogWarning("Путь для анимации пуст!");
                return;
            }
            
            if (isAnimating)
            {
                Debug.LogWarning("Анимация уже выполняется!");
                return;
            }
            
            currentPath = new List<Vector2Int>(path);
            currentStep = 0;
            animationProgress = 0f;
            isAnimating = true;
            
            // Устанавливаем начальную позицию
            transform.position = new Vector3(startPos.x, startPos.y, transform.position.z);
            startPosition = transform.position;
            
            // Устанавливаем целевую позицию для первого шага
            if (currentPath.Count > 0)
            {
                targetPosition = new Vector3(currentPath[0].x, currentPath[0].y, transform.position.z);
            }
            
            Debug.Log($"🎬 Запущена анимация движения блока: {path.Count} шагов");
            
            // Запускаем визуальные эффекты
            if (moveEffect != null)
            {
                moveEffect.Play();
            }
            
            if (moveSound != null)
            {
                moveSound.Play();
            }
        }
        
        private void Update()
        {
            if (!isAnimating) return;
            
            if (smoothMovement)
            {
                AnimateSmoothMovement();
            }
            else
            {
                AnimateStepMovement();
            }
        }
        
        /// <summary>
        /// Плавная анимация движения
        /// </summary>
        private void AnimateSmoothMovement()
        {
            animationProgress += Time.deltaTime * moveSpeed;
            
            if (animationProgress >= 1f)
            {
                // Переходим к следующему шагу
                animationProgress = 0f;
                currentStep++;
                
                if (currentStep >= currentPath.Count)
                {
                    // Анимация завершена
                    FinishAnimation();
                    return;
                }
                
                // Обновляем позиции для следующего шага
                startPosition = transform.position;
                targetPosition = new Vector3(currentPath[currentStep].x, currentPath[currentStep].y, transform.position.z);
            }
            
            // Интерполируем позицию
            float t = moveCurve.Evaluate(animationProgress);
            transform.position = Vector3.Lerp(startPosition, targetPosition, t);
        }
        
        /// <summary>
        /// Пошаговая анимация движения
        /// </summary>
        private void AnimateStepMovement()
        {
            if (currentStep >= currentPath.Count)
            {
                FinishAnimation();
                return;
            }
            
            Vector3 targetPos = new Vector3(currentPath[currentStep].x, currentPath[currentStep].y, transform.position.z);
            float step = moveSpeed * Time.deltaTime;
            
            transform.position = Vector3.MoveTowards(transform.position, targetPos, step);
            
            if (Vector3.Distance(transform.position, targetPos) < 0.1f)
            {
                transform.position = targetPos;
                currentStep++;
            }
        }
        
        /// <summary>
        /// Завершает анимацию
        /// </summary>
        private void FinishAnimation()
        {
            isAnimating = false;
            currentStep = 0;
            animationProgress = 0f;
            currentPath.Clear();
            
            // Останавливаем визуальные эффекты
            if (moveEffect != null)
            {
                moveEffect.Stop();
            }
            
            Debug.Log("✅ Анимация движения блока завершена");
        }
        
        /// <summary>
        /// Останавливает анимацию
        /// </summary>
        public void StopAnimation()
        {
            if (isAnimating)
            {
                FinishAnimation();
            }
        }
        
        /// <summary>
        /// Устанавливает скорость анимации
        /// </summary>
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = speed;
        }
        
        /// <summary>
        /// Устанавливает кривую анимации
        /// </summary>
        public void SetMoveCurve(AnimationCurve curve)
        {
            moveCurve = curve;
        }
    }
}
