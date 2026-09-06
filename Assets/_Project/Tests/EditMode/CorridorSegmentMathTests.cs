using NUnit.Framework;
using SphereCorridor.Corridor;

namespace SphereCorridor.Tests.EditMode
{
    /// <summary>
    /// Protects segment lifecycle calculations without instantiating scene geometry.
    /// </summary>
    public sealed class CorridorSegmentMathTests
    {
        /// <summary>
        /// Confirms that partly visible geometry cannot be retired.
        /// </summary>
        [Test]
        public void VisibleSegmentIsNotReadyToRetire()
        {
            bool shouldRetire = CorridorSegmentMath.IsReadyToRetire(
                segmentEndX: -8f,
                rearBoundaryX: -10f,
                safetyMargin: 2f);

            Assert.That(shouldRetire, Is.False);
        }

        /// <summary>
        /// Confirms that the full segment plus safety margin must leave the camera.
        /// </summary>
        [Test]
        public void FullyHiddenSegmentIsReadyToRetire()
        {
            bool shouldRetire = CorridorSegmentMath.IsReadyToRetire(
                segmentEndX: -13f,
                rearBoundaryX: -10f,
                safetyMargin: 2f);

            Assert.That(shouldRetire, Is.True);
        }

        /// <summary>
        /// Confirms that different-length types connect without overlaps or gaps.
        /// </summary>
        [Test]
        public void NextCenterSupportsVariableSegmentLength()
        {
            float center = CorridorSegmentMath.CalculateNextCenter(
                farthestEndX: 40f,
                nextSegmentLength: 30f);

            Assert.That(center, Is.EqualTo(55f));
        }
    }
}
