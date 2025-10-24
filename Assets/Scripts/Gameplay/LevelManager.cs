using System.Collections.Generic;
using LabyrinthMover.Core;
using LabyrinthMover.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LabyrinthMover.Gameplay
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField]
        private GridConfig gridConfig;

        [SerializeField]
        private CharacterController2D characterPrefab;

        [SerializeField]
        private MoveExecutor moveExecutor;

        [SerializeField]
        private HudController hud;

        [SerializeField]
        private InputRouter inputRouter;

        private CharacterController2D characterInstance;
        private GridModel gridModel;
        private Vector2Int goalCell;

        private readonly List<StaticTile> staticTiles = new List<StaticTile>();
        private readonly List<MovableBlock> movableBlocks = new List<MovableBlock>();

        private void Awake()
        {
            gridConfig = gridConfig ?? FindObjectOfType<GridConfig>();
            moveExecutor = moveExecutor ?? FindObjectOfType<MoveExecutor>();
            hud = hud ?? FindObjectOfType<HudController>();
            inputRouter = inputRouter ?? FindObjectOfType<InputRouter>();
        }

        private void Start()
        {
            if (gridConfig == null)
            {
                Debug.LogError("GridConfig not found in scene");
                return;
            }

            BuildGridModel();
            SpawnCharacter();
            SetupGoal();

            moveExecutor.Init(gridModel, characterInstance, gridConfig.Emax, gridConfig.Ksize, gridConfig.Kdist);
            moveExecutor.EnergyChanged += OnEnergyChanged;
            OnEnergyChanged(moveExecutor.EnergyRemaining, moveExecutor.EnergyMax);

            if (inputRouter != null)
            {
                inputRouter.Init(gridModel, moveExecutor, characterInstance, hud, goalCell, OnGoalReached, ResetLevel);
            }

            if (hud != null)
            {
                hud.ResetPreview();
            }
        }

        private void OnDestroy()
        {
            if (moveExecutor != null)
            {
                moveExecutor.EnergyChanged -= OnEnergyChanged;
            }
        }

        private void BuildGridModel()
        {
            gridModel = new GridModel(gridConfig.Width, gridConfig.Height);

            staticTiles.AddRange(FindObjectsOfType<StaticTile>());
            foreach (var tile in staticTiles)
            {
                Vector2Int pos = RoundToCell(tile.transform.position);
                gridModel.PlaceStatic(pos);
            }

            movableBlocks.AddRange(FindObjectsOfType<MovableBlock>());
            foreach (var block in movableBlocks)
            {
                Vector2Int anchor = block.Anchor;
                Vector2Int size = block.Size;
                var rect = new RectInt(anchor.x, anchor.y, size.x, size.y);
                var blockRt = new BlockRt(block.GetInstanceID(), rect);
                gridModel.AddBlock(blockRt);
            }
        }

        private void SpawnCharacter()
        {
            Vector2Int startCell = FindMarker("MarkerStart");

            if (characterPrefab == null)
            {
                Debug.LogError("Character prefab is not assigned");
                return;
            }

            characterInstance = Instantiate(characterPrefab, new Vector3(startCell.x, startCell.y, 0f), Quaternion.identity);
            characterInstance.Init(gridModel, startCell);
        }

        private void SetupGoal()
        {
            goalCell = FindMarker("MarkerGoal");
        }

        private Vector2Int FindMarker(string markerName)
        {
            var marker = GameObject.Find(markerName);
            if (marker == null)
            {
                Debug.LogWarning($"Marker {markerName} not found; defaulting to (0,0)");
                return Vector2Int.zero;
            }

            return RoundToCell(marker.transform.position);
        }

        private void OnGoalReached()
        {
            Debug.Log("Goal reached! Level complete.");
        }

        private void OnEnergyChanged(float remaining, float max)
        {
            hud?.SetEnergy(remaining, max);
            if (remaining < 0f)
            {
                Debug.LogWarning("Energy depleted. Level failed.");
            }
        }

        private void ResetLevel()
        {
            Scene current = SceneManager.GetActiveScene();
            SceneManager.LoadScene(current.buildIndex);
        }

        private static Vector2Int RoundToCell(Vector3 position)
        {
            return new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y));
        }
    }
}
