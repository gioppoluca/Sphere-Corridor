using System;
using UnityEngine;

namespace SphereCorridor.Combat
{
    /// <summary>
    /// Optional absorption-layer policy. It can use different resistance multipliers
    /// from health while still preserving a single underlying health pool.
    /// </summary>
    [CreateAssetMenu(fileName = "Shield", menuName = "Sphere Corridor/Combat/Shield Definition")]
    public sealed class ShieldDefinition : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float capacity = 3f;
        [SerializeField] private DamageResistanceEntry[] resistances = Array.Empty<DamageResistanceEntry>();

        public float Capacity => capacity;
        public DamageResistanceEntry[] Resistances => resistances;

        /// <summary>Configures a reusable shield policy for editor setup or later authoring tools.</summary>
        public void Configure(float shieldCapacity, DamageResistanceEntry[] resistanceEntries)
        {
            capacity = Mathf.Max(0.01f, shieldCapacity);
            resistances = resistanceEntries ?? Array.Empty<DamageResistanceEntry>();
        }

        /// <summary>Returns how strongly this shield reacts to one damage family.</summary>
        public float GetDamageMultiplier(DamageTypeDefinition damageType)
        {
            return DamageResistanceMath.FindMultiplier(damageType, resistances);
        }
    }
}
