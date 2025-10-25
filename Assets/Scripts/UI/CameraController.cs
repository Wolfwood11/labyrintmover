using UnityEngine;
using UnityEngine.InputSystem;

namespace LabyrinthMover.UI
{
    /// <summary>
    /// Управление камерой с пинч-зумом и перетаскиванием согласно ТЗ
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Настройки зума")]
        [SerializeField] private float minZoom = 2f;
        [SerializeField] private float maxZoom = 8f;
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float zoomSmoothTime = 0.3f;
        
        [Header("Настройки перетаскивания")]
        [SerializeField] private float dragSpeed = 1f;
        [SerializeField] private float dragSmoothTime = 0.2f;
        
        [Header("Границы камеры")]
        [SerializeField] private float cameraBounds = 5f;
        
        [Header("Следование за игроком")]
        [SerializeField] private bool followPlayer = true;
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private float deadZone = 2f; // Мертвая зона - камера не двигается если игрок в радиусе
        
        private Camera cam;
        private Vector3 targetPosition;
        private float targetZoom;
        private Vector3 velocity;
        private float zoomVelocity;
        
        private Vector3 lastPanPosition;
        private bool isPanning = false;
        
        private Transform playerTarget;
        
        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = Camera.main;
            }
            
            targetPosition = transform.position;
            targetZoom = cam.orthographicSize;
        }
        
        private void Update()
        {
            HandleInput();
            UpdateCamera();
        }
        
        private void HandleInput()
        {
            // Обработка зума (колесо мыши и пинч-жесты)
            HandleZoom();
            
            // Обработка перетаскивания
            HandlePanning();
            
            // Обработка клавиатуры (WASD)
            HandleKeyboardInput();
        }
        
        private void HandleZoom()
        {
            float zoomInput = 0f;
            
            // Колесо мыши через новый Input System
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var scrollDelta = mouse.scroll.ReadValue();
                if (scrollDelta.y != 0)
                {
                    zoomInput = -scrollDelta.y * zoomSpeed;
                }
            }
            
            // Пинч-жесты на мобильных устройствах
            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.touches.Count == 2)
            {
                var touch1 = touchscreen.touches[0];
                var touch2 = touchscreen.touches[1];
                
                Vector2 touch1PrevPos = touch1.position.ReadValue() - touch1.delta.ReadValue();
                Vector2 touch2PrevPos = touch2.position.ReadValue() - touch2.delta.ReadValue();
                
                float prevTouchDeltaMag = (touch1PrevPos - touch2PrevPos).magnitude;
                float touchDeltaMag = (touch1.position.ReadValue() - touch2.position.ReadValue()).magnitude;
                
                float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
                zoomInput = deltaMagnitudeDiff * zoomSpeed * 0.01f;
            }
            
            if (zoomInput != 0)
            {
                targetZoom = Mathf.Clamp(targetZoom + zoomInput, minZoom, maxZoom);
            }
        }
        
        private void HandlePanning()
        {
            Vector3 inputPosition = Vector3.zero;
            bool inputActive = false;
            
            // Проверяем, не взаимодействует ли игрок с игровыми объектами
            if (IsInteractingWithGameObjects())
            {
                return; // Не перетаскиваем камеру, если идет взаимодействие с игрой
            }
            
            // Мышь через новый Input System (используем правую кнопку для перетаскивания камеры)
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    lastPanPosition = mouse.position.ReadValue();
                    isPanning = true;
                }
                else if (mouse.rightButton.isPressed && isPanning)
                {
                    inputPosition = mouse.position.ReadValue();
                    inputActive = true;
                }
                else if (mouse.rightButton.wasReleasedThisFrame)
                {
                    isPanning = false;
                }
            }
            
            // Тач через новый Input System
            var touchscreen = Touchscreen.current;
            if (touchscreen != null && touchscreen.touches.Count == 1 && !isPanning)
            {
                var touch = touchscreen.touches[0];
                var phase = touch.phase.ReadValue();
                
                if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    lastPanPosition = touch.position.ReadValue();
                    isPanning = true;
                }
                else if (phase == UnityEngine.InputSystem.TouchPhase.Moved && isPanning)
                {
                    inputPosition = touch.position.ReadValue();
                    inputActive = true;
                }
                else if (phase == UnityEngine.InputSystem.TouchPhase.Ended || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    isPanning = false;
                }
            }
            
            if (inputActive)
            {
                Vector3 panDelta = lastPanPosition - inputPosition;
                Vector3 worldDelta = cam.ScreenToWorldPoint(panDelta) - cam.ScreenToWorldPoint(Vector3.zero);
                worldDelta.z = 0; // Сохраняем Z-координату камеры
                
                targetPosition += worldDelta * dragSpeed;
                lastPanPosition = inputPosition;
            }
        }
        
        private void UpdateCamera()
        {
            // Временно отключаем следование за игроком для отладки
            if (false && followPlayer && playerTarget != null)
            {
                Vector3 playerPos = playerTarget.position;
                playerPos.z = transform.position.z; // Сохраняем Z координату камеры
                
                if (isPanning)
                {
                    // Если пользователь перетаскивает камеру, временно отключаем следование
                    targetPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, dragSmoothTime);
                }
                else
                {
                    // Проверяем расстояние до игрока
                    float distanceToPlayer = Vector3.Distance(transform.position, playerPos);
                    
                    if (distanceToPlayer > deadZone)
                    {
                        // Плавно следуем за игроком только если он далеко
                        targetPosition = Vector3.Lerp(targetPosition, playerPos, followSpeed * Time.deltaTime);
                    }
                    else
                    {
                        // Если игрок в мертвой зоне, используем обычное движение камеры
                        targetPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, dragSmoothTime);
                    }
                }
            }
            else
            {
                // Обычное движение камеры
                targetPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, dragSmoothTime);
            }
            
            // Применяем позицию
            transform.position = targetPosition;
            
            // Применяем зум
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, targetZoom, ref zoomVelocity, zoomSmoothTime);
            
            // Ограничиваем позицию камеры
            ClampCameraPosition();
        }
        
        private void ClampCameraPosition()
        {
            // Получаем границы видимой области
            float cameraHeight = cam.orthographicSize;
            float cameraWidth = cameraHeight * cam.aspect;
            
            // Ограничиваем позицию камеры
            float clampedX = Mathf.Clamp(targetPosition.x, -cameraBounds + cameraWidth, cameraBounds - cameraWidth);
            float clampedY = Mathf.Clamp(targetPosition.y, -cameraBounds + cameraHeight, cameraBounds - cameraHeight);
            
            targetPosition = new Vector3(clampedX, clampedY, targetPosition.z);
        }
        
        /// <summary>
        /// Устанавливает позицию камеры
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            targetPosition = position;
            ClampCameraPosition();
        }
        
        /// <summary>
        /// Устанавливает уровень зума
        /// </summary>
        public void SetZoom(float zoom)
        {
            targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        }
        
        /// <summary>
        /// Центрирует камеру на позиции
        /// </summary>
        public void CenterOn(Vector3 worldPosition)
        {
            SetPosition(worldPosition);
        }
        
        /// <summary>
        /// Центрирует камеру на игровой сетке
        /// </summary>
        public void CenterOnGrid(int width, int height)
        {
            Vector3 centerPosition = new Vector3(width * 0.5f - 0.5f, height * 0.5f - 0.5f, transform.position.z);
            CenterOn(centerPosition);
        }
        
        /// <summary>
        /// Сброс камеры к начальному состоянию
        /// </summary>
        public void ResetCamera()
        {
            targetPosition = Vector3.zero;
            targetZoom = (minZoom + maxZoom) * 0.5f;
        }
        
        /// <summary>
        /// Устанавливает цель для следования камеры
        /// </summary>
        public void FollowTarget(Transform target)
        {
            playerTarget = target;
            if (target != null)
            {
                targetPosition = target.position;
                targetPosition.z = transform.position.z;
            }
        }
        
        /// <summary>
        /// Отключает следование за целью
        /// </summary>
        public void StopFollowing()
        {
            playerTarget = null;
        }
        
        /// <summary>
        /// Включает/отключает следование за игроком
        /// </summary>
        public void SetFollowPlayer(bool follow)
        {
            followPlayer = follow;
        }
        
        /// <summary>
        /// Проверяет, взаимодействует ли игрок с игровыми объектами
        /// </summary>
        private bool IsInteractingWithGameObjects()
        {
            // Проверяем, есть ли InputRouter и активен ли он
            var inputRouter = FindFirstObjectByType<LabyrinthMover.UI.InputRouter>();
            if (inputRouter != null)
            {
                // Проверяем, нажата ли мышь/тач и есть ли объекты под курсором
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.isPressed)
                {
                    Vector2 mousePos = mouse.position.ReadValue();
                    return IsGameObjectUnderCursor(mousePos);
                }
                
                var touchscreen = Touchscreen.current;
                if (touchscreen != null && touchscreen.touches.Count > 0)
                {
                    Vector2 touchPos = touchscreen.touches[0].position.ReadValue();
                    return IsGameObjectUnderCursor(touchPos);
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Проверяет, есть ли игровые объекты под курсором
        /// </summary>
        private bool IsGameObjectUnderCursor(Vector2 screenPosition)
        {
            if (cam == null) return false;
            
            Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -cam.transform.position.z));
            Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
            
            // Проверяем, есть ли игровые объекты в этой позиции
            var staticTiles = FindObjectsByType<LabyrinthMover.Gameplay.StaticTile>(FindObjectsSortMode.None);
            foreach (var tile in staticTiles)
            {
                Vector2Int tilePos = new Vector2Int(Mathf.RoundToInt(tile.transform.position.x), Mathf.RoundToInt(tile.transform.position.y));
                if (tilePos == gridPos)
                    return true;
            }
            
            var movableBlocks = FindObjectsByType<LabyrinthMover.Gameplay.MovableBlock>(FindObjectsSortMode.None);
            foreach (var block in movableBlocks)
            {
                Vector2Int blockPos = new Vector2Int(Mathf.RoundToInt(block.transform.position.x), Mathf.RoundToInt(block.transform.position.y));
                if (blockPos == gridPos)
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Обработка клавиатуры для управления камерой
        /// </summary>
        private void HandleKeyboardInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            
            Vector2 moveDirection = Vector2.zero;
            float moveSpeed = 5f; // Скорость движения камеры
            
            if (keyboard.wKey.isPressed) moveDirection.y += 1f;
            if (keyboard.sKey.isPressed) moveDirection.y -= 1f;
            if (keyboard.aKey.isPressed) moveDirection.x -= 1f;
            if (keyboard.dKey.isPressed) moveDirection.x += 1f;
            
            if (moveDirection != Vector2.zero)
            {
                moveDirection = moveDirection.normalized * moveSpeed * Time.deltaTime;
                targetPosition += new Vector3(moveDirection.x, moveDirection.y, 0);
            }
        }
    }
}
