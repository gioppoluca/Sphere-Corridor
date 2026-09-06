using System.Collections.Generic;

namespace SphereCorridor.Corridor.Grid
{
    /// <summary>
    /// Validates authored matrices before runtime. The same deterministic checks can
    /// later reject procedurally generated layouts before the player sees them.
    /// </summary>
    public static class SegmentBlueprintValidator
    {
        /// <summary>Checks dimensions, definitions, safety zones, footprints, and reachability.</summary>
        public static bool IsValid(SegmentBlueprintDefinition blueprint, out string reason)
        {
            if (blueprint == null || string.IsNullOrWhiteSpace(blueprint.StableId))
            {
                reason = "Blueprint and stable ID are required.";
                return false;
            }

            int expectedCells = blueprint.LengthCells * blueprint.WidthCells;
            if (blueprint.FloorCellCount != expectedCells || blueprint.ContentCellCount != expectedCells)
            {
                reason = $"Blueprint '{blueprint.StableId}' floor/content arrays must each contain {expectedCells} cells.";
                return false;
            }

            if (blueprint.NearBoundaryCount != blueprint.LengthCells ||
                blueprint.FarBoundaryCount != blueprint.LengthCells)
            {
                reason = $"Blueprint '{blueprint.StableId}' boundary strips must match its length.";
                return false;
            }

            if (blueprint.SafeEntryColumns + blueprint.SafeExitColumns > blueprint.LengthCells)
            {
                reason = "Entry and exit safety zones overlap.";
                return false;
            }

            bool[] blockedCells = new bool[expectedCells];
            if (!ValidateDefinitionsAndFootprints(blueprint, blockedCells, out reason))
            {
                return false;
            }

            if (!ValidateSafetyZones(blueprint, out reason))
            {
                return false;
            }

            if (!HasTraversableRoute(blueprint, blockedCells))
            {
                reason = $"Blueprint '{blueprint.StableId}' has no route from entry to exit.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>Validates referenced assets and reserves every blocking footprint cell.</summary>
        private static bool ValidateDefinitionsAndFootprints(
            SegmentBlueprintDefinition blueprint,
            bool[] blockedCells,
            out string reason)
        {
            bool[] occupiedCells = new bool[blockedCells.Length];
            for (int lengthIndex = 0; lengthIndex < blueprint.LengthCells; lengthIndex++)
            {
                BoundaryTileDefinition near = blueprint.GetNearBoundary(lengthIndex).Definition;
                BoundaryTileDefinition far = blueprint.GetFarBoundary(lengthIndex).Definition;
                if (near != null && !near.IsValid(out reason))
                {
                    return false;
                }

                if (far != null && !far.IsValid(out reason))
                {
                    return false;
                }

                for (int widthIndex = 0; widthIndex < blueprint.WidthCells; widthIndex++)
                {
                    FloorTileDefinition floor = blueprint.GetFloorCell(lengthIndex, widthIndex).Definition;
                    if (floor == null)
                    {
                        reason = $"Floor cell [{lengthIndex},{widthIndex}] has no definition. Use an explicit Gap tile.";
                        return false;
                    }

                    if (!floor.IsValid(out reason))
                    {
                        return false;
                    }

                    ContentGridCell contentCell = blueprint.GetContentCell(lengthIndex, widthIndex);
                    SegmentPlaceableDefinition placeable = contentCell.Definition;
                    if (placeable == null)
                    {
                        continue;
                    }

                    if (!placeable.IsValid(out reason))
                    {
                        return false;
                    }

                    int footprintLength = placeable.FootprintLengthCells;
                    int footprintWidth = placeable.FootprintWidthCells;
                    if ((contentCell.QuarterTurns & 1) != 0)
                    {
                        (footprintLength, footprintWidth) = (footprintWidth, footprintLength);
                    }

                    // The authored placement point remains the selected cell centre.
                    // Larger footprints reserve neighbouring cells around that anchor;
                    // even dimensions intentionally bias toward the positive axis.
                    int footprintStartLength = lengthIndex - (footprintLength - 1) / 2;
                    int footprintStartWidth = widthIndex - (footprintWidth - 1) / 2;

                    for (int lengthOffset = 0; lengthOffset < footprintLength; lengthOffset++)
                    {
                        for (int widthOffset = 0; widthOffset < footprintWidth; widthOffset++)
                        {
                            int occupiedLength = footprintStartLength + lengthOffset;
                            int occupiedWidth = footprintStartWidth + widthOffset;
                            if (occupiedLength < 0 || occupiedWidth < 0 ||
                                occupiedLength >= blueprint.LengthCells || occupiedWidth >= blueprint.WidthCells)
                            {
                                reason = $"Placeable '{placeable.StableId}' footprint leaves the grid.";
                                return false;
                            }

                            int occupiedIndex = blueprint.GetIndex(occupiedLength, occupiedWidth);
                            if (occupiedCells[occupiedIndex])
                            {
                                reason = $"Placeable '{placeable.StableId}' overlaps another footprint.";
                                return false;
                            }

                            occupiedCells[occupiedIndex] = true;
                            if (placeable.BlocksNavigation)
                            {
                                blockedCells[occupiedIndex] = true;
                            }
                        }
                    }
                }
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>Requires safe columns to be walkable and free from content anchors.</summary>
        private static bool ValidateSafetyZones(SegmentBlueprintDefinition blueprint, out string reason)
        {
            for (int lengthIndex = 0; lengthIndex < blueprint.LengthCells; lengthIndex++)
            {
                bool isSafetyColumn = lengthIndex < blueprint.SafeEntryColumns ||
                                      lengthIndex >= blueprint.LengthCells - blueprint.SafeExitColumns;
                if (!isSafetyColumn)
                {
                    continue;
                }

                for (int widthIndex = 0; widthIndex < blueprint.WidthCells; widthIndex++)
                {
                    FloorTileDefinition floor = blueprint.GetFloorCell(lengthIndex, widthIndex).Definition;
                    if (!floor.IsWalkable || blueprint.GetContentCell(lengthIndex, widthIndex).Definition != null)
                    {
                        reason = $"Safety cell [{lengthIndex},{widthIndex}] must be walkable and empty.";
                        return false;
                    }
                }
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>
        /// Performs a bounded graph search. Forward/backward hops can cross the authored
        /// number of unavailable cells, modelling the baseline jump without physics.
        /// </summary>
        private static bool HasTraversableRoute(SegmentBlueprintDefinition blueprint, bool[] blockedCells)
        {
            bool[] visited = new bool[blockedCells.Length];
            Queue<int> frontier = new Queue<int>();

            for (int widthIndex = 0; widthIndex < blueprint.WidthCells; widthIndex++)
            {
                if (IsTraversable(blueprint, blockedCells, 0, widthIndex))
                {
                    int index = blueprint.GetIndex(0, widthIndex);
                    visited[index] = true;
                    frontier.Enqueue(index);
                }
            }

            while (frontier.Count > 0)
            {
                int current = frontier.Dequeue();
                int lengthIndex = current / blueprint.WidthCells;
                int widthIndex = current % blueprint.WidthCells;
                if (lengthIndex == blueprint.LengthCells - 1)
                {
                    return true;
                }

                TryEnqueue(blueprint, blockedCells, visited, frontier, lengthIndex, widthIndex - 1);
                TryEnqueue(blueprint, blockedCells, visited, frontier, lengthIndex, widthIndex + 1);
                TryEnqueueLongitudinal(blueprint, blockedCells, visited, frontier, lengthIndex, widthIndex, 1);
                TryEnqueueLongitudinal(blueprint, blockedCells, visited, frontier, lengthIndex, widthIndex, -1);
            }

            return false;
        }

        /// <summary>Finds the first legal landing in one longitudinal direction.</summary>
        private static void TryEnqueueLongitudinal(
            SegmentBlueprintDefinition blueprint,
            bool[] blockedCells,
            bool[] visited,
            Queue<int> frontier,
            int lengthIndex,
            int widthIndex,
            int direction)
        {
            int maximumStep = blueprint.MaximumJumpGapCells + 1;
            for (int step = 1; step <= maximumStep; step++)
            {
                int candidateLength = lengthIndex + direction * step;
                if (candidateLength < 0 || candidateLength >= blueprint.LengthCells)
                {
                    return;
                }

                if (IsTraversable(blueprint, blockedCells, candidateLength, widthIndex))
                {
                    TryEnqueue(
                        blueprint,
                        blockedCells,
                        visited,
                        frontier,
                        candidateLength,
                        widthIndex);
                    return;
                }
            }
        }

        /// <summary>Adds one valid, previously unseen cell to the route frontier.</summary>
        private static void TryEnqueue(
            SegmentBlueprintDefinition blueprint,
            bool[] blockedCells,
            bool[] visited,
            Queue<int> frontier,
            int lengthIndex,
            int widthIndex)
        {
            if (!IsTraversable(blueprint, blockedCells, lengthIndex, widthIndex))
            {
                return;
            }

            int index = blueprint.GetIndex(lengthIndex, widthIndex);
            if (!visited[index])
            {
                visited[index] = true;
                frontier.Enqueue(index);
            }
        }

        /// <summary>Combines floor and footprint state for one grid coordinate.</summary>
        private static bool IsTraversable(
            SegmentBlueprintDefinition blueprint,
            bool[] blockedCells,
            int lengthIndex,
            int widthIndex)
        {
            if (lengthIndex < 0 || lengthIndex >= blueprint.LengthCells ||
                widthIndex < 0 || widthIndex >= blueprint.WidthCells)
            {
                return false;
            }

            int index = blueprint.GetIndex(lengthIndex, widthIndex);
            FloorTileDefinition floor = blueprint.GetFloorCell(lengthIndex, widthIndex).Definition;
            return floor != null && floor.IsWalkable && !blockedCells[index];
        }
    }
}
