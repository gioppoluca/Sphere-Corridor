using System;
using UnityEngine;

namespace SphereCorridor.Combat
{
    /// <summary>Identifies teams without tying combat code to player or enemy classes.</summary>
    public enum CombatFaction
    {
        Neutral = 0,
        Player = 1,
        Enemy = 2,
        Environment = 3
    }

    /// <summary>Represents the supported trigger cadence of a weapon definition.</summary>
    public enum WeaponFireMode
    {
        Single = 0,
        Burst = 1,
        Automatic = 2
    }

    /// <summary>
    /// Carries every fact required by the GDD damage boundary. Receivers decide how
    /// faction, type, shields, and resistances transform this immutable request.
    /// </summary>
    public readonly struct DamagePacket
    {
        public DamagePacket(
            float amount,
            GameObject source,
            CombatFaction faction,
            Vector3 hitPosition,
            Vector3 direction,
            DamageTypeDefinition damageType)
        {
            Amount = amount;
            Source = source;
            Faction = faction;
            HitPosition = hitPosition;
            Direction = direction;
            DamageType = damageType;
        }

        public float Amount { get; }
        public GameObject Source { get; }
        public CombatFaction Faction { get; }
        public Vector3 HitPosition { get; }
        public Vector3 Direction { get; }
        public DamageTypeDefinition DamageType { get; }
    }

    /// <summary>Returns an inspectable outcome instead of making callers infer one.</summary>
    public readonly struct DamageResult
    {
        public DamageResult(bool accepted, bool immune, float shieldDamage, float healthDamage, bool killed)
        {
            Accepted = accepted;
            Immune = immune;
            ShieldDamage = shieldDamage;
            HealthDamage = healthDamage;
            Killed = killed;
        }

        public bool Accepted { get; }
        public bool Immune { get; }
        public float ShieldDamage { get; }
        public float HealthDamage { get; }
        public bool Killed { get; }

        /// <summary>Creates the standard result for faction-filtered or dead targets.</summary>
        public static DamageResult Rejected => new DamageResult(false, false, 0f, 0f, false);
    }

    /// <summary>Common contract used by obstacles, enemies, the player, and physical shields.</summary>
    public interface IDamageable
    {
        DamageResult ApplyDamage(in DamagePacket packet);
    }

    /// <summary>Data passed to composable death effects after the one idempotent death event.</summary>
    public readonly struct DeathContext
    {
        public DeathContext(GameObject target, DamagePacket killingDamage)
        {
            Target = target;
            KillingDamage = killingDamage;
        }

        public GameObject Target { get; }
        public DamagePacket KillingDamage { get; }
    }

    /// <summary>Factory request keeps weapon callers independent from pooling policy.</summary>
    public readonly struct ProjectileSpawnRequest
    {
        public ProjectileSpawnRequest(
            Vector3 position,
            Vector3 direction,
            GameObject source,
            CombatFaction faction,
            WeaponDefinition weapon)
        {
            Position = position;
            Direction = direction;
            Source = source;
            Faction = faction;
            Weapon = weapon;
        }

        public Vector3 Position { get; }
        public Vector3 Direction { get; }
        public GameObject Source { get; }
        public CombatFaction Faction { get; }
        public WeaponDefinition Weapon { get; }
    }

    /// <summary>Allows pooling to change without modifying the weapon controller.</summary>
    public interface IProjectileFactory
    {
        bool TrySpawn(in ProjectileSpawnRequest request);
    }

    /// <summary>One damage-type multiplier in a health or shield resistance table.</summary>
    [Serializable]
    public struct DamageResistanceEntry
    {
        [SerializeField] private DamageTypeDefinition damageType;
        [SerializeField, Min(0f)] private float multiplier;

        public DamageResistanceEntry(DamageTypeDefinition type, float damageMultiplier)
        {
            damageType = type;
            multiplier = Mathf.Max(0f, damageMultiplier);
        }

        public DamageTypeDefinition DamageType => damageType;
        public float Multiplier => multiplier;
    }

    /// <summary>Pure resistance lookup shared by health and shields.</summary>
    public static class DamageResistanceMath
    {
        public static float FindMultiplier(
            DamageTypeDefinition damageType,
            DamageResistanceEntry[] entries,
            float defaultMultiplier = 1f)
        {
            if (entries == null)
            {
                return Mathf.Max(0f, defaultMultiplier);
            }

            for (int index = 0; index < entries.Length; index++)
            {
                if (entries[index].DamageType == damageType)
                {
                    return entries[index].Multiplier;
                }
            }

            return Mathf.Max(0f, defaultMultiplier);
        }
    }
}
