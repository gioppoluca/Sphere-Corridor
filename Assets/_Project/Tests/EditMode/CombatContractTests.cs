using NUnit.Framework;
using SphereCorridor.Combat;
using UnityEngine;

namespace SphereCorridor.Tests.EditMode
{
    /// <summary>Protects typed resistance, faction filtering, and idempotent death behavior.</summary>
    public sealed class CombatContractTests
    {
        private DamageTypeDefinition plasma;
        private HealthDefinition healthDefinition;
        private GameObject target;

        /// <summary>Creates isolated runtime objects rather than depending on generated assets.</summary>
        [SetUp]
        public void SetUp()
        {
            plasma = ScriptableObject.CreateInstance<DamageTypeDefinition>();
            plasma.Configure("plasma", "Plasma");
            healthDefinition = ScriptableObject.CreateInstance<HealthDefinition>();
            target = new GameObject("Combat Contract Test Target");
        }

        /// <summary>Destroys all temporary Unity objects after each assertion.</summary>
        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(healthDefinition);
            Object.DestroyImmediate(plasma);
        }

        /// <summary>Confirms multiplier zero grants immunity without creating another health pool.</summary>
        [Test]
        public void PlasmaImmunityPreservesSingleHealthPool()
        {
            healthDefinition.Configure(
                3f,
                CombatFaction.Environment,
                false,
                new[] { new DamageResistanceEntry(plasma, 0f) },
                System.Array.Empty<DeathEffectDefinition>());
            Health health = target.AddComponent<Health>();
            health.Configure(healthDefinition);

            DamageResult result = health.ApplyDamage(CreatePacket(CombatFaction.Player));

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Immune, Is.True);
            Assert.That(result.HealthDamage, Is.Zero);
            Assert.That(health.CurrentHealth, Is.EqualTo(3f));
        }

        /// <summary>Confirms three ordinary plasma packets exhaust three points of health once.</summary>
        [Test]
        public void VulnerableTargetDiesOnThirdPlasmaHit()
        {
            healthDefinition.Configure(
                3f,
                CombatFaction.Environment,
                false,
                System.Array.Empty<DamageResistanceEntry>(),
                System.Array.Empty<DeathEffectDefinition>());
            Health health = target.AddComponent<Health>();
            health.Configure(healthDefinition);
            int deathCount = 0;
            health.Died += _ => deathCount++;

            health.ApplyDamage(CreatePacket(CombatFaction.Player));
            health.ApplyDamage(CreatePacket(CombatFaction.Player));
            DamageResult lethal = health.ApplyDamage(CreatePacket(CombatFaction.Player));
            DamageResult duplicate = health.ApplyDamage(CreatePacket(CombatFaction.Player));

            Assert.That(lethal.Killed, Is.True);
            Assert.That(duplicate.Accepted, Is.False);
            Assert.That(deathCount, Is.EqualTo(1));
        }

        /// <summary>Confirms friendly fire is rejected before shield or health mutation.</summary>
        [Test]
        public void MatchingFactionIsFilteredByDefault()
        {
            healthDefinition.Configure(
                5f,
                CombatFaction.Player,
                false,
                System.Array.Empty<DamageResistanceEntry>(),
                System.Array.Empty<DeathEffectDefinition>());
            Health health = target.AddComponent<Health>();
            health.Configure(healthDefinition);

            DamageResult result = health.ApplyDamage(CreatePacket(CombatFaction.Player));

            Assert.That(result.Accepted, Is.False);
            Assert.That(health.CurrentHealth, Is.EqualTo(5f));
        }

        /// <summary>Builds one complete GDD damage packet for the test target.</summary>
        private DamagePacket CreatePacket(CombatFaction faction)
        {
            return new DamagePacket(1f, null, faction, Vector3.zero, Vector3.right, plasma);
        }
    }
}
