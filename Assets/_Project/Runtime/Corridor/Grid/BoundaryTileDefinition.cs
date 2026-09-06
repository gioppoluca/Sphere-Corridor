using UnityEngine;

namespace SphereCorridor.Corridor.Grid
{
    /// <summary>Defines one reusable wall/breach/door tile for a boundary strip.</summary>
    [CreateAssetMenu(fileName = "BoundaryTile", menuName = "Sphere Corridor/Grid/Boundary Tile")]
    public sealed class BoundaryTileDefinition : ScriptableObject
    {
        [SerializeField] private string stableId = "structural_wall";
        [SerializeField] private BoundaryKind boundaryKind = BoundaryKind.Structural;
        [SerializeField] private GameObject prefabOverride;
        [SerializeField] private GridPrimitiveShape fallbackShape = GridPrimitiveShape.Cube;
        [SerializeField] private Material material;
        [SerializeField, Min(0.01f)] private float height = 0.9f;
        [SerializeField, Min(0.01f)] private float thickness = 0.5f;
        [SerializeField] private bool blocksActors = true;
        [SerializeField] private bool blocksProjectiles = true;
        [SerializeField, Tooltip("Merge adjacent identical boundary cells into one static object.")]
        private bool mergeAdjacentCells = true;

        public string StableId => stableId;
        public BoundaryKind Kind => boundaryKind;
        public GameObject PrefabOverride => prefabOverride;
        public GridPrimitiveShape FallbackShape => fallbackShape;
        public Material Material => material;
        public float Height => height;
        public float Thickness => thickness;
        public bool BlocksActors => blocksActors;
        public bool BlocksProjectiles => blocksProjectiles;
        public bool MergeAdjacentCells => mergeAdjacentCells;

        /// <summary>Configures the first indestructible corridor wall vocabulary entry.</summary>
        public void ConfigureStructural(
            string id,
            Material renderMaterial,
            float wallHeight,
            float wallThickness)
        {
            stableId = id;
            boundaryKind = BoundaryKind.Structural;
            prefabOverride = null;
            fallbackShape = GridPrimitiveShape.Cube;
            material = renderMaterial;
            height = Mathf.Max(0.01f, wallHeight);
            thickness = Mathf.Max(0.01f, wallThickness);
            blocksActors = true;
            blocksProjectiles = true;
            mergeAdjacentCells = true;
        }

        /// <summary>Checks the geometry required by the runtime assembler.</summary>
        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(stableId) || height <= 0f || thickness <= 0f)
            {
                reason = "Boundary ID, height, and thickness must be valid.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
