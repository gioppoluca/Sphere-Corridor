using NUnit.Framework;
using SphereCorridor.Movement;
using UnityEngine;

namespace SphereCorridor.Tests.EditMode
{
    /// <summary>
    /// Locks the intent-selection rules independently from Rigidbody simulation.
    /// Physics feel remains a playtest concern, but these decisions must stay deterministic.
    /// </summary>
    public sealed class PlayerMovementMathTests
    {
        private PlayerMovementDefinition definition;

        /// <summary>
        /// Creates a fresh default definition before every behavior example.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            definition = ScriptableObject.CreateInstance<PlayerMovementDefinition>();
        }

        /// <summary>
        /// Releases the temporary ScriptableObject so EditMode tests leave no native objects behind.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(definition);
        }

        /// <summary>
        /// Verifies that releasing controls restores the defining forward-pressure behavior.
        /// </summary>
        [Test]
        public void NoInputTargetsCruiseSpeed()
        {
            float target = PlayerMovementMath.CalculateTargetSpeed(0f, 0f, definition, out float acceleration);

            Assert.That(target, Is.EqualTo(definition.CruiseSpeed));
            Assert.That(acceleration, Is.EqualTo(definition.ReturnToCruiseAcceleration));
        }

        /// <summary>
        /// Verifies that full positive input requests the configured risk/reward maximum.
        /// </summary>
        [Test]
        public void FullAccelerationTargetsMaximumSpeed()
        {
            float target = PlayerMovementMath.CalculateTargetSpeed(7f, 1f, definition, out float acceleration);

            Assert.That(target, Is.EqualTo(definition.MaximumSpeed));
            Assert.That(acceleration, Is.EqualTo(definition.ForwardAcceleration));
        }

        /// <summary>
        /// Verifies that reverse input first brakes a forward-moving sphere to zero.
        /// </summary>
        [Test]
        public void ReverseInputWhileMovingForwardTargetsStop()
        {
            float target = PlayerMovementMath.CalculateTargetSpeed(7f, -1f, definition, out float acceleration);

            Assert.That(target, Is.Zero);
            Assert.That(acceleration, Is.EqualTo(definition.BrakeDeceleration));
        }

        /// <summary>
        /// Verifies that continued reverse input requests the deliberately limited reverse speed.
        /// </summary>
        [Test]
        public void ReverseInputFromStopTargetsLimitedReverse()
        {
            float target = PlayerMovementMath.CalculateTargetSpeed(0f, -1f, definition, out float acceleration);

            Assert.That(target, Is.EqualTo(definition.ReverseSpeed));
            Assert.That(acceleration, Is.EqualTo(definition.ForwardAcceleration));
        }

        /// <summary>
        /// Verifies that the committed default relationships are internally safe.
        /// </summary>
        [Test]
        public void DefaultDefinitionIsValid()
        {
            Assert.That(definition.IsValid(out string reason), Is.True, reason);
        }
    }
}
