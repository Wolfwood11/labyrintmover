using System.Collections;
using System.Collections.Generic;
using LabyrinthMover.Core;
using LabyrinthMover.Gameplay;
using UnityEngine;

namespace LabyrinthMover.UI
{
    public class InputRouter : MonoBehaviour
    {
        [SerializeField]
        private float swipeThreshold = 0.25f;

        private GridModel grid;
        private MoveExecutor moveExecutor;
        private CharacterController2D character;
        private HudController hud;
        private Vector2Int goalCell;
        private System.Action onGoalReached;
        private System.Action onResetRequested;

        private Camera mainCamera;

        private int? selectedBlockId;
        private Vector2 pointerDownScreen;
        private Vector2Int currentDir;
        private Coroutine moveRoutine;

        public void Init(GridModel gridModel, MoveExecutor executor, CharacterController2D characterController, HudController hudController, Vector2Int goal, System.Action goalCallback, System.Action resetCallback)
        {
            grid = gridModel;
            moveExecutor = executor;
            character = characterController;
            hud = hudController;
            goalCell = goal;
            onGoalReached = goalCallback;
            onResetRequested = resetCallback;

            mainCamera = Camera.main;

            if (hud != null)
            {
                hud.UndoRequested += OnUndoRequested;
                hud.ResetRequested += OnResetRequested;
            }
        }

        private void OnDestroy()
        {
            if (hud != null)
            {
                hud.UndoRequested -= OnUndoRequested;
                hud.ResetRequested -= OnResetRequested;
            }
        }

        private void Update()
        {
            if (Input.touchSupported)
            {
                HandleTouchInput();
            }
            else
            {
                HandleMouseInput();
            }
        }

        private void HandleMouseInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                OnPointerDown(Input.mousePosition);
            }

            if (Input.GetMouseButton(0))
            {
                OnPointerMove(Input.mousePosition);
            }

            if (Input.GetMouseButtonUp(0))
            {
                OnPointerUp(Input.mousePosition);
            }
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount == 0)
            {
                return;
            }

            var touch = Input.GetTouch(0);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    OnPointerDown(touch.position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    OnPointerMove(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    OnPointerUp(touch.position);
                    break;
            }
        }

        private void OnPointerDown(Vector2 screenPosition)
        {
            pointerDownScreen = screenPosition;
            currentDir = Vector2Int.zero;
            selectedBlockId = grid?.GetBlockId(ScreenToCell(screenPosition));
        }

        private void OnPointerMove(Vector2 screenPosition)
        {
            if (!selectedBlockId.HasValue)
            {
                return;
            }

            Vector2 delta = screenPosition - pointerDownScreen;
            if (delta.magnitude < swipeThreshold)
            {
                return;
            }

            Vector2Int dir = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                ? new Vector2Int(delta.x > 0 ? 1 : -1, 0)
                : new Vector2Int(0, delta.y > 0 ? 1 : -1);

            if (dir == currentDir)
            {
                return;
            }

            currentDir = dir;

            var preview = moveExecutor.PreviewSwipe(selectedBlockId.Value, dir);
            if (preview.steps > 0)
            {
                hud?.ShowPreview(preview.steps, preview.startDistance, preview.endDistance, preview.cost);
            }
            else
            {
                hud?.ResetPreview();
            }
        }

        private void OnPointerUp(Vector2 screenPosition)
        {
            Vector2Int releaseCell = ScreenToCell(screenPosition);

            if (selectedBlockId.HasValue && currentDir != Vector2Int.zero)
            {
                float cost = moveExecutor.CommitSwipe(selectedBlockId.Value, currentDir);
                if (cost > 0f)
                {
                    hud?.ResetPreview();
                }
            }
            else
            {
                TryMoveCharacter(releaseCell);
            }

            selectedBlockId = null;
            currentDir = Vector2Int.zero;
            if (!selectedBlockId.HasValue)
            {
                hud?.ResetPreview();
            }
        }

        private void TryMoveCharacter(Vector2Int targetCell)
        {
            if (character == null)
            {
                return;
            }

            if (targetCell == character.GridPos)
            {
                return;
            }

            if (character.TryPathTo(targetCell, out var path) && path.Count > 0)
            {
                if (moveRoutine != null)
                {
                    StopCoroutine(moveRoutine);
                }

                moveRoutine = StartCoroutine(PlayCharacterPath(path));
            }
        }

        private IEnumerator PlayCharacterPath(List<Vector2Int> path)
        {
            yield return character.PlayPath(path);
            if (character.GridPos == goalCell)
            {
                onGoalReached?.Invoke();
            }
        }

        private Vector2Int ScreenToCell(Vector2 screenPosition)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            Vector3 world = mainCamera != null
                ? mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -mainCamera.transform.position.z))
                : Vector3.zero;

            return new Vector2Int(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));
        }

        private void OnUndoRequested()
        {
            moveExecutor?.Undo();
            hud?.ResetPreview();
        }

        private void OnResetRequested()
        {
            hud?.ResetPreview();
            onResetRequested?.Invoke();
        }
    }
}
