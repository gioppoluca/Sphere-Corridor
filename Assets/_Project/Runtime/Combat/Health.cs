using System;
using SphereCorridor.Foundation;
using UnityEngine;

namespace SphereCorridor.Combat
{
    /// <summary>
    /// Reusable, single-pool health runtime. It owns faction filtering, optional shield
    /// mitigation, typed resistance, and one idempotent death transition.
    /// </summary>
    public sealed class Health : MonoBehaviour, IDamageable
    {
        private const string LogSubsystem = "Combat";

        [SerializeField] private HealthDefinition definition;
        [SerializeField] private DamageShield shield;

        private bool isDead;

        public event Action<DamageResult> Damaged;
        public event Action<DamagePacket> Died;

        public float CurrentHealth { get; private set; }
        public float MaximumHealth => definition == null ? 0f : definition.MaximumHealth;
        public bool IsDead => isDead;
        public CombatFaction Faction => definition == null ? CombatFaction.Neutral : definition.Faction;

        /// <summary>Assigns an immutable policy and optional runtime shield layer.</summary>
        public void Configure(HealthDefinition healthDefinition, DamageShield damageShield = null)
        {
            definition = healthDefinition;
            shield = damageShield;
            enabled = definition != null;
            ResetRuntimeState();
        }

        /// <summary>Applies filtering, shield absorption, resistance, and death exactly once.</summary>
        public DamageResult ApplyDamage(in DamagePacket packet)
        {
            if (isDead || definition == null || packet.Amount <= 0f || packet.DamageType == null)
            {
                return DamageResult.Rejected;
            }

            if (!definition.AllowFriendlyFire && packet.Faction == definition.Faction &&
                packet.Faction != CombatFaction.Neutral)
            {
                AppLog.Development(LogSubsystem, $"Friendly damage rejected by '{name}'.", this);
                return DamageResult.Rejected;
            }

            float remainingDamage = packet.Amount;
            float shieldDamage = 0f;
            if (shield != null)
            {
                remainingDamage = shield.Absorb(packet.DamageType, remainingDamage, out shieldDamage);
            }

            float multiplier = definition.GetDamageMultiplier(packet.DamageType);
            float healthDamage = remainingDamage * multiplier;
            bool immune = multiplier <= 0f && remainingDamage > 0f;
            if (immune)
            {
                AppLog.Info(LogSubsystem, $"'{name}' is immune to {packet.DamageType.DisplayName}.", this);
            }
            else if (healthDamage > 0f)
            {
                CurrentHealth = Mathf.Max(0f, CurrentHealth - healthDamage);
                AppLog.Info(
                    LogSubsystem,
                    $"'{name}' took {healthDamage:F2} {packet.DamageType.DisplayName}; " +
                    $"{CurrentHealth:F2}/{definition.MaximumHealth:F2} health remains.",
                    this);
            }

            bool killed = !isDead && CurrentHealth <= 0f;
            DamageResult result = new DamageResult(true, immune, shieldDamage, healthDamage, killed);
            Damaged?.Invoke(result);

            if (killed)
            {
                Die(packet);
            }

            return result;
        }

        /// <summary>Restores authoring state when the component awakens or is pooled later.</summary>
        public void ResetRuntimeState()
        {
            isDead = false;
            CurrentHealth = definition == null ? 0f : definition.MaximumHealth;
        }

        /// <summary>Emits one death event before executing the ordered response array.</summary>
        private void Die(in DamagePacket killingDamage)
        {
            isDead = true;
            AppLog.Info(LogSubsystem, $"'{name}' died.", this);
            Died?.Invoke(killingDamage);

            DeathContext context = new DeathContext(gameObject, killingDamage);
            DeathEffectDefinition[] effects = definition.DeathEffects;
            for (int index = 0; index < effects.Length; index++)
            {
                if (effects[index] != null)
                {
                    effects[index].Execute(context);
                }
            }
        }

        /// <summary>Initializes serialized health without rejecting runtime-added components.</summary>
        private void Awake()
        {
            if (definition != null)
            {
                ResetRuntimeState();
            }
        }

        /// <summary>
        /// Validates after all Awake calls. Runtime assemblers add and configure Health
        /// before Start, so this avoids a false missing-definition error during creation.
        /// </summary>
        private void Start()
        {
            if (definition == null)
            {
                AppLog.Error(LogSubsystem, $"Health definition is missing on '{name}'.", this);
                enabled = false;
            }
        }
    }
}
