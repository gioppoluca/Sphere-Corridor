using UnityEngine;
using SphereCorridor.Corridor.Grid;

namespace SphereCorridor.Corridor
{
    /// <summary>
    /// Identifies one live prefab instance and exposes its world-space boundaries.
    /// Instance state is separate from the reusable segment definition asset.
    /// </summary>
    public sealed class CorridorSegmentInstance : MonoBehaviour
    {
        [SerializeField] private CorridorSegmentDefinition definition;
        [SerializeField] private int sequenceNumber;

        /// <summary>Gets the immutable type data used to create this occurrence.</summary>
        public CorridorSegmentDefinition Definition => definition;

        /// <summary>Gets this occurrence's order in the current run.</summary>
        public int SequenceNumber => sequenceNumber;

        /// <summary>Gets the world-space entry edge along the travel axis.</summary>
        public float StartX => transform.position.x - definition.Length * 0.5f;

        /// <summary>Gets the world-space exit edge along the travel axis.</summary>
        public float EndX => transform.position.x + definition.Length * 0.5f;

        /// <summary>
        /// Associates a newly instantiated prefab with its selected definition and run order.
        /// </summary>
        public void Configure(CorridorSegmentDefinition segmentDefinition, int sequence)
        {
            definition = segmentDefinition;
            sequenceNumber = sequence;
            name = $"Segment {sequenceNumber:000} — {definition.SegmentId}";

            GridSegmentAssembler assembler = GetComponent<GridSegmentAssembler>();
            if (assembler != null && definition.Blueprint != null)
            {
                assembler.ConfigureAndBuild(definition.Blueprint);
            }
        }
    }
}
