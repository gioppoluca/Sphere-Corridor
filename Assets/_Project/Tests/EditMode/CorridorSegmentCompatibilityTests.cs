using NUnit.Framework;
using SphereCorridor.Corridor;
using UnityEngine;

namespace SphereCorridor.Tests.EditMode
{
    /// <summary>Protects the compatibility gate used before segment instantiation.</summary>
    public sealed class CorridorSegmentCompatibilityTests
    {
        private CorridorSegmentDefinition previous;
        private CorridorSegmentDefinition next;
        private GameObject prefab;

        /// <summary>Creates two standard-profile segment definitions.</summary>
        [SetUp]
        public void SetUp()
        {
            prefab = new GameObject("Compatibility Test Prefab");
            previous = ScriptableObject.CreateInstance<CorridorSegmentDefinition>();
            next = ScriptableObject.CreateInstance<CorridorSegmentDefinition>();
            previous.Configure("previous", 40f, prefab);
            next.Configure("next", 30f, prefab);
        }

        /// <summary>Removes temporary ScriptableObjects and geometry.</summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(previous);
            Object.DestroyImmediate(next);
            Object.DestroyImmediate(prefab);
        }

        /// <summary>Confirms standard connectors form a legal seam despite different lengths.</summary>
        [Test]
        public void StandardProfilesCanFollow()
        {
            bool compatible = CorridorSegmentCompatibility.CanFollow(previous, next, out string reason);

            Assert.That(compatible, Is.True, reason);
        }
    }
}
