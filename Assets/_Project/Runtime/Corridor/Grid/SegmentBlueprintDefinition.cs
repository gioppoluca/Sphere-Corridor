using System;
using UnityEngine;

namespace SphereCorridor.Corridor.Grid
{
    /// <summary>
    /// Stores the complete cell-based recipe for one segment. Unity serializes the
    /// matrices as flat arrays; accessor methods preserve row/column semantics.
    /// </summary>
    [CreateAssetMenu(fileName = "SegmentBlueprint", menuName = "Sphere Corridor/Grid/Segment Blueprint")]
    public sealed class SegmentBlueprintDefinition : ScriptableObject
    {
        [Header("Grid dimensions")]
        [SerializeField] private string stableId = "training_combat_grid";
        [SerializeField, Min(1)] private int lengthCells = 20;
        [SerializeField, Min(1)] private int widthCells = 6;
        [SerializeField, Min(0.25f)] private float cellSize = 2f;

        [Header("Safety and traversal")]
        [SerializeField, Min(1)] private int safeEntryColumns = 2;
        [SerializeField, Min(1)] private int safeExitColumns = 2;
        [SerializeField, Min(0)] private int maximumJumpGapCells = 1;

        [Header("Flattened matrices")]
        [SerializeField] private FloorGridCell[] floorCells = Array.Empty<FloorGridCell>();
        [SerializeField] private ContentGridCell[] contentCells = Array.Empty<ContentGridCell>();
        [SerializeField] private BoundaryGridCell[] nearBoundary = Array.Empty<BoundaryGridCell>();
        [SerializeField] private BoundaryGridCell[] farBoundary = Array.Empty<BoundaryGridCell>();

        public string StableId => stableId;
        public int LengthCells => lengthCells;
        public int WidthCells => widthCells;
        public float CellSize => cellSize;
        public float LengthMeters => lengthCells * cellSize;
        public float WidthMeters => widthCells * cellSize;
        public int SafeEntryColumns => safeEntryColumns;
        public int SafeExitColumns => safeExitColumns;
        public int MaximumJumpGapCells => maximumJumpGapCells;

        /// <summary>
        /// Replaces the complete recipe in one operation. Editor generation uses this
        /// method so dimensions and all four arrays are saved consistently; gameplay
        /// only reads the resulting ScriptableObject.
        /// </summary>
        public void Configure(
            string id,
            int longitudinalCells,
            int lateralCells,
            float metresPerCell,
            int entrySafetyColumns,
            int exitSafetyColumns,
            int jumpGapCells,
            FloorGridCell[] floors,
            ContentGridCell[] contents,
            BoundaryGridCell[] nearWalls,
            BoundaryGridCell[] farWalls)
        {
            stableId = id;
            lengthCells = Mathf.Max(1, longitudinalCells);
            widthCells = Mathf.Max(1, lateralCells);
            cellSize = Mathf.Max(0.25f, metresPerCell);
            safeEntryColumns = Mathf.Max(1, entrySafetyColumns);
            safeExitColumns = Mathf.Max(1, exitSafetyColumns);
            maximumJumpGapCells = Mathf.Max(0, jumpGapCells);
            floorCells = floors ?? Array.Empty<FloorGridCell>();
            contentCells = contents ?? Array.Empty<ContentGridCell>();
            nearBoundary = nearWalls ?? Array.Empty<BoundaryGridCell>();
            farBoundary = farWalls ?? Array.Empty<BoundaryGridCell>();
        }

        /// <summary>
        /// Converts matrix coordinates to Unity's one-dimensional serialized array.
        /// Length is the row and width is the column, so each X slice occupies one
        /// contiguous block of <see cref="WidthCells"/> entries.
        /// </summary>
        public int GetIndex(int lengthIndex, int widthIndex)
        {
            return lengthIndex * widthCells + widthIndex;
        }

        /// <summary>Returns one floor cell after bounds validation.</summary>
        public FloorGridCell GetFloorCell(int lengthIndex, int widthIndex)
        {
            return floorCells[GetIndex(lengthIndex, widthIndex)];
        }

        /// <summary>Returns one content anchor after bounds validation.</summary>
        public ContentGridCell GetContentCell(int lengthIndex, int widthIndex)
        {
            return contentCells[GetIndex(lengthIndex, widthIndex)];
        }

        /// <summary>Returns the near-wall tile for one longitudinal column.</summary>
        public BoundaryGridCell GetNearBoundary(int lengthIndex)
        {
            return nearBoundary[lengthIndex];
        }

        /// <summary>Returns the far-wall tile for one longitudinal column.</summary>
        public BoundaryGridCell GetFarBoundary(int lengthIndex)
        {
            return farBoundary[lengthIndex];
        }

        /// <summary>
        /// Converts a matrix coordinate into segment-local metres. Subtracting half
        /// the total dimensions centres the grid on the prefab origin, which keeps
        /// streamer placement independent from grid resolution.
        /// </summary>
        public Vector3 GetLocalCellCenter(int lengthIndex, int widthIndex)
        {
            float x = (lengthIndex + 0.5f) * cellSize - LengthMeters * 0.5f;
            float z = (widthIndex + 0.5f) * cellSize - WidthMeters * 0.5f;
            return new Vector3(x, 0f, z);
        }

        /// <summary>Runs structural and reachability validation before the blueprint is used.</summary>
        public bool IsValid(out string reason)
        {
            return SegmentBlueprintValidator.IsValid(this, out reason);
        }

        /// <summary>Clamps simple relationships immediately after Inspector edits.</summary>
        private void OnValidate()
        {
            safeEntryColumns = Mathf.Clamp(safeEntryColumns, 1, lengthCells);
            safeExitColumns = Mathf.Clamp(safeExitColumns, 1, lengthCells);
            maximumJumpGapCells = Mathf.Max(0, maximumJumpGapCells);
        }

        /// <summary>Exposes array sizes to the validator without returning mutable arrays.</summary>
        public int FloorCellCount => floorCells == null ? 0 : floorCells.Length;
        public int ContentCellCount => contentCells == null ? 0 : contentCells.Length;
        public int NearBoundaryCount => nearBoundary == null ? 0 : nearBoundary.Length;
        public int FarBoundaryCount => farBoundary == null ? 0 : farBoundary.Length;
    }
}
