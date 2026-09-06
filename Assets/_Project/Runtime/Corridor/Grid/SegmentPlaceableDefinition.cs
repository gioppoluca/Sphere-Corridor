using SphereCorridor.Combat;
using UnityEngine;

namespace SphereCorridor.Corridor.Grid
{
    /// <summary>
    /// Describes a content-grid object independently from its cell position. Footprint,
    /// health, visuals, and difficulty cost can be recombined without new code.
    /// </summary>
    [CreateAssetMenu(fileName = "Placeable", menuName = "Sphere Corridor/Grid/Placeable")]
    public sealed class SegmentPlaceableDefinition : ScriptableObject
    {
        [SerializeField] private string stableId = "obstacle";
        [SerializeField] private GameObject prefabOverride;
        [SerializeField] private GridPrimitiveShape fallbackShape = GridPrimitiveShape.Cube;
        [SerializeField] private Material material;
        [SerializeField] private Vector3 localScale = Vector3.one;
        [SerializeField] private Vector3 localOffset;
        [SerializeField, Min(1)] private int footprintLengthCells = 1;
        [SerializeField, Min(1)] private int footprintWidthCells = 1;
        [SerializeField] private bool blocksNavigation = true;
        [SerializeField, Min(0f)] private float difficultyCost = 1f;
        [SerializeField] private HealthDefinition healthDefinition;

        public string StableId => stableId;
        public GameObject PrefabOverride => prefabOverride;
        public GridPrimitiveShape FallbackShape => fallbackShape;
        public Material Material => material;
        public Vector3 LocalScale => localScale;
        public Vector3 LocalOffset => localOffset;
        public int FootprintLengthCells => footprintLengthCells;
        public int FootprintWidthCells => footprintWidthCells;
        public bool BlocksNavigation => blocksNavigation;
        public float DifficultyCost => difficultyCost;
        public HealthDefinition HealthDefinition => healthDefinition;

        /// <summary>Assigns one fully parameterized geometric object vocabulary entry.</summary>
        public void Configure(
            string id,
            GameObject prefab,
            GridPrimitiveShape shape,
            Material renderMaterial,
            Vector3 scale,
            Vector3 offset,
            int footprintLength,
            int footprintWidth,
            bool blocks,
            float cost,
            HealthDefinition health)
        {
            stableId = id;
            prefabOverride = prefab;
            fallbackShape = shape;
            material = renderMaterial;
            localScale = scale;
            localOffset = offset;
            footprintLengthCells = Mathf.Max(1, footprintLength);
            footprintWidthCells = Mathf.Max(1, footprintWidth);
            blocksNavigation = blocks;
            difficultyCost = Mathf.Max(0f, cost);
            healthDefinition = health;
        }

        /// <summary>Rejects content which cannot be placed or identified reliably.</summary>
        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                reason = "Placeable stable ID is empty.";
                return false;
            }

            if (footprintLengthCells < 1 || footprintWidthCells < 1 ||
                localScale.x <= 0f || localScale.y <= 0f || localScale.z <= 0f)
            {
                reason = $"Placeable '{stableId}' has an invalid footprint or scale.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
