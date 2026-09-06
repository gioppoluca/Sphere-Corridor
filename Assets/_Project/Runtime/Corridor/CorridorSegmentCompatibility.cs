using System;
using UnityEngine;

namespace SphereCorridor.Corridor
{
    /// <summary>
    /// Pure compatibility rules shared by the temporary streamer and the future
    /// seeded planner. Keeping this deterministic makes it straightforward to test.
    /// </summary>
    public static class CorridorSegmentCompatibility
    {
        private const float ConnectorTolerance = 0.01f;

        /// <summary>Checks whether <paramref name="next"/> may legally follow <paramref name="previous"/>.</summary>
        public static bool CanFollow(
            CorridorSegmentDefinition previous,
            CorridorSegmentDefinition next,
            out string reason)
        {
            if (previous == null || next == null)
            {
                reason = "Both segment definitions are required.";
                return false;
            }

            CorridorConnectorProfile exit = previous.ExitProfile;
            CorridorConnectorProfile entry = next.EntryProfile;
            if (!string.Equals(exit.ProfileId, entry.ProfileId, StringComparison.Ordinal) ||
                Mathf.Abs(exit.Width - entry.Width) > ConnectorTolerance ||
                Mathf.Abs(exit.FloorElevation - entry.FloorElevation) > ConnectorTolerance)
            {
                reason = "Connector profile, width, or floor elevation does not match.";
                return false;
            }

            if (!TransitionClassesOverlap(exit.PermittedTransitionClasses, entry.PermittedTransitionClasses))
            {
                reason = "Connector transition classes do not overlap.";
                return false;
            }

            if (previous.AllowedFollowingSegmentIds.Length > 0 &&
                !Contains(previous.AllowedFollowingSegmentIds, next.SegmentId))
            {
                reason = $"'{next.SegmentId}' is not in '{previous.SegmentId}' allowed-following list.";
                return false;
            }

            if (Contains(previous.ProhibitedFollowingSegmentIds, next.SegmentId) ||
                Contains(previous.AdjacencyExclusions, next.SegmentId) ||
                Contains(next.AdjacencyExclusions, previous.SegmentId))
            {
                reason = "The pair is prohibited by an adjacency rule.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>Returns true when both connector class lists share one stable key.</summary>
        private static bool TransitionClassesOverlap(string[] left, string[] right)
        {
            if (left == null || right == null || left.Length == 0 || right.Length == 0)
            {
                return false;
            }

            for (int leftIndex = 0; leftIndex < left.Length; leftIndex++)
            {
                if (Contains(right, left[leftIndex]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Performs an allocation-free ordinal lookup in serialized ID arrays.</summary>
        private static bool Contains(string[] values, string expected)
        {
            if (values == null)
            {
                return false;
            }

            for (int index = 0; index < values.Length; index++)
            {
                if (string.Equals(values[index], expected, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
