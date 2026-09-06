using System;
using UnityEngine;

namespace SphereCorridor.Combat
{
    /// <summary>
    /// Immutable health policy. Every damage type feeds one health pool; resistance
    /// entries only transform incoming damage and never create parallel health bars.
    /// </summary>
    [CreateAssetMenu(fileName = "Health", menuName = "Sphere Corridor/Combat/Health Definition")]
    public sealed class HealthDefinition : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float maximumHealth = 3f;
        [SerializeField] private CombatFaction faction = CombatFaction.Environment;
        [SerializeField] private bool allowFriendlyFire;
        [SerializeField] private DamageResistanceEntry[] resistances = Array.Empty<DamageResistanceEntry>();
        [SerializeField] private DeathEffectDefinition[] deathEffects = Array.Empty<DeathEffectDefinition>();

        public float MaximumHealth => maximumHealth;
        public CombatFaction Faction => faction;
        public bool AllowFriendlyFire => allowFriendlyFire;
        public DamageResistanceEntry[] Resistances => resistances;
        public DeathEffectDefinition[] DeathEffects => deathEffects;

        /// <summary>Assigns all M2 lab health policy in one deliberate editor operation.</summary>
        public void Configure(
            float health,
            CombatFaction owningFaction,
            bool friendlyFire,
            DamageResistanceEntry[] resistanceEntries,
            DeathEffectDefinition[] effects)
        {
            maximumHealth = Mathf.Max(0.01f, health);
            faction = owningFaction;
            allowFriendlyFire = friendlyFire;
            resistances = resistanceEntries ?? Array.Empty<DamageResistanceEntry>();
            deathEffects = effects ?? Array.Empty<DeathEffectDefinition>();
        }

        /// <summary>Returns the multiplier for one damage family; absent types remain normal.</summary>
        public float GetDamageMultiplier(DamageTypeDefinition damageType)
        {
            return DamageResistanceMath.FindMultiplier(damageType, resistances);
        }

        /// <summary>Rejects authoring states that would create undefined runtime behavior.</summary>
        public bool IsValid(out string reason)
        {
            if (maximumHealth <= 0f)
            {
                reason = "Maximum health must be positive.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
