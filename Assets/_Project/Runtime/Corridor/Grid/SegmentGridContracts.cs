using System;
using UnityEngine;

namespace SphereCorridor.Corridor.Grid
{
    /// <summary>Describes how a floor cell participates in traversal and validation.</summary>
    public enum FloorTraversalKind
    {
        Walkable = 0,
        Gap = 1,
        Hazard = 2,
        Special = 3
    }

    /// <summary>Describes the gameplay role of a generated corridor boundary tile.</summary>
    public enum BoundaryKind
    {
        Structural = 0,
        Protective = 1,
        Breach = 2,
        Door = 3
    }

    /// <summary>
    /// Selects a zero-asset placeholder shape. A prefab override can replace it later
    /// without changing the blueprint or assembler contract.
    /// </summary>
    public enum GridPrimitiveShape
    {
        Cube = 0,
        Sphere = 1,
        Capsule = 2,
        Cylinder = 3
    }

    /// <summary>One authored floor cell in the flattened longitudinal/lateral matrix.</summary>
    [Serializable]
    public struct FloorGridCell
    {
        [SerializeField] private FloorTileDefinition definition;
        [SerializeField, Range(0, 3)] private int quarterTurns;

        public FloorGridCell(FloorTileDefinition tileDefinition, int rotationQuarterTurns = 0)
        {
            definition = tileDefinition;
            quarterTurns = Mathf.Abs(rotationQuarterTurns) % 4;
        }

        public FloorTileDefinition Definition => definition;
        public int QuarterTurns => quarterTurns;
    }

    /// <summary>One optional object anchor in the content matrix.</summary>
    [Serializable]
    public struct ContentGridCell
    {
        [SerializeField] private SegmentPlaceableDefinition definition;
        [SerializeField, Range(0, 3)] private int quarterTurns;

        public ContentGridCell(SegmentPlaceableDefinition placeableDefinition, int rotationQuarterTurns = 0)
        {
            definition = placeableDefinition;
            quarterTurns = Mathf.Abs(rotationQuarterTurns) % 4;
        }

        public SegmentPlaceableDefinition Definition => definition;
        public int QuarterTurns => quarterTurns;
    }

    /// <summary>One entry in a near-wall or far-wall longitudinal strip.</summary>
    [Serializable]
    public struct BoundaryGridCell
    {
        [SerializeField] private BoundaryTileDefinition definition;

        public BoundaryGridCell(BoundaryTileDefinition tileDefinition)
        {
            definition = tileDefinition;
        }

        public BoundaryTileDefinition Definition => definition;
    }
}
