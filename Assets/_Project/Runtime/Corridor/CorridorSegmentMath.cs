namespace SphereCorridor.Corridor
{
    /// <summary>
    /// Holds deterministic segment-boundary calculations outside scene lifecycle code.
    /// </summary>
    public static class CorridorSegmentMath
    {
        /// <summary>
        /// Determines whether a segment is completely behind the visible rear boundary.
        /// </summary>
        public static bool IsReadyToRetire(float segmentEndX, float rearBoundaryX, float safetyMargin)
        {
            return segmentEndX < rearBoundaryX - safetyMargin;
        }

        /// <summary>
        /// Places a variable-length new segment directly after the current farthest edge.
        /// </summary>
        public static float CalculateNextCenter(float farthestEndX, float nextSegmentLength)
        {
            return farthestEndX + nextSegmentLength * 0.5f;
        }
    }
}
