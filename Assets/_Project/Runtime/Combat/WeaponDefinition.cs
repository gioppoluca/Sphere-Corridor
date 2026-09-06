using UnityEngine;

namespace SphereCorridor.Combat
{
    /// <summary>
    /// Immutable weapon recipe. Cadence, payload, projectile, trajectory, and lifetime
    /// remain data so combinations do not require new weapon-controller classes.
    /// </summary>
    [CreateAssetMenu(fileName = "Weapon", menuName = "Sphere Corridor/Combat/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [Header("Identity and payload")]
        [SerializeField] private string stableId = "player_plasma_pulse";
        [SerializeField] private DamageTypeDefinition damageType;
        [SerializeField, Min(0.01f)] private float damage = 1f;

        [Header("Cadence")]
        [SerializeField] private WeaponFireMode fireMode = WeaponFireMode.Automatic;
        [SerializeField, Min(0.01f)] private float fireInterval = 0.25f;
        [SerializeField, Min(1)] private int burstShotCount = 3;
        [SerializeField, Min(0.01f)] private float burstShotInterval = 0.08f;

        [Header("Projectile")]
        [SerializeField] private ProjectileInstance projectilePrefab;
        [SerializeField] private ProjectileTrajectoryDefinition trajectory;
        [SerializeField, Min(0.01f)] private float projectileSpeed = 24f;
        [SerializeField, Min(0.01f)] private float projectileLifetime = 2f;

        public string StableId => stableId;
        public DamageTypeDefinition DamageType => damageType;
        public float Damage => damage;
        public WeaponFireMode FireMode => fireMode;
        public float FireInterval => fireInterval;
        public int BurstShotCount => burstShotCount;
        public float BurstShotInterval => burstShotInterval;
        public ProjectileInstance ProjectilePrefab => projectilePrefab;
        public ProjectileTrajectoryDefinition Trajectory => trajectory;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;

        /// <summary>Creates the GDD's first automatic plasma pulse recipe.</summary>
        public void ConfigurePlasmaLab(
            DamageTypeDefinition plasmaType,
            ProjectileInstance prefab,
            ProjectileTrajectoryDefinition trajectoryDefinition)
        {
            stableId = "player_plasma_pulse";
            damageType = plasmaType;
            damage = 1f;
            fireMode = WeaponFireMode.Automatic;
            fireInterval = 0.25f;
            burstShotCount = 3;
            burstShotInterval = 0.08f;
            projectilePrefab = prefab;
            trajectory = trajectoryDefinition;
            projectileSpeed = 24f;
            projectileLifetime = 2f;
        }

        /// <summary>Validates all relationships required to produce a shot.</summary>
        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(stableId) || damageType == null ||
                projectilePrefab == null || trajectory == null)
            {
                reason = "Weapon identity, damage type, projectile prefab, and trajectory are required.";
                return false;
            }

            if (damage <= 0f || fireInterval <= 0f || projectileSpeed <= 0f || projectileLifetime <= 0f)
            {
                reason = "Damage and timing values must be positive.";
                return false;
            }

            if (fireMode == WeaponFireMode.Burst && (burstShotCount < 1 || burstShotInterval <= 0f))
            {
                reason = "Burst mode requires a positive shot count and interval.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
