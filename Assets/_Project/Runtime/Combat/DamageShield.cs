using SphereCorridor.Foundation;
using UnityEngine;

namespace SphereCorridor.Combat
{
    /// <summary>
    /// Runtime shield state owned by one entity. It mitigates before health; a future
    /// visible physical shield can instead carry its own collider and Health component.
    /// </summary>
    public sealed class DamageShield : MonoBehaviour
    {
        private const string LogSubsystem = "Combat";

        [SerializeField] private ShieldDefinition definition;

        public float CurrentCapacity { get; private set; }
        public float MaximumCapacity => definition == null ? 0f : definition.Capacity;
        public bool IsDepleted => CurrentCapacity <= 0f;

        /// <summary>Assigns the authored policy and restores full runtime capacity.</summary>
        public void Configure(ShieldDefinition shieldDefinition)
        {
            definition = shieldDefinition;
            CurrentCapacity = definition == null ? 0f : definition.Capacity;
        }

        /// <summary>
        /// Absorbs as much post-resistance damage as capacity permits and returns the
        /// remainder that should continue into the one health pool.
        /// </summary>
        public float Absorb(DamageTypeDefinition damageType, float incomingDamage, out float absorbed)
        {
            absorbed = 0f;
            if (definition == null || CurrentCapacity <= 0f || incomingDamage <= 0f)
            {
                return incomingDamage;
            }

            float shieldDemand = incomingDamage * definition.GetDamageMultiplier(damageType);
            if (shieldDemand <= 0f)
            {
                // A shield immunity means this type passes through unchanged rather
                // than consuming capacity. Health resistance still has the final say.
                return incomingDamage;
            }

            absorbed = Mathf.Min(CurrentCapacity, shieldDemand);
            CurrentCapacity -= absorbed;
            float absorbedIncomingEquivalent = absorbed / shieldDemand * incomingDamage;
            float remaining = Mathf.Max(0f, incomingDamage - absorbedIncomingEquivalent);
            AppLog.Development(
                LogSubsystem,
                $"Shield absorbed {absorbed:F2}; {CurrentCapacity:F2}/{definition.Capacity:F2} remains.",
                this);
            return remaining;
        }

        /// <summary>Initializes capacity after Unity deserializes the definition.</summary>
        private void Awake()
        {
            CurrentCapacity = definition == null ? 0f : definition.Capacity;
        }
    }
}
