using SphereCorridor.Foundation;
using SphereCorridor.Movement;
using UnityEngine;

namespace SphereCorridor.Combat
{
    /// <summary>
    /// Converts logical fire intent into single, burst, or automatic cadence. It knows
    /// only the factory contract and therefore remains independent from pooling details.
    /// </summary>
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        private const string LogSubsystem = "Combat";

        [SerializeField] private WeaponDefinition definition;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform muzzle;
        [SerializeField] private MonoBehaviour projectileFactory;
        [SerializeField] private CombatFaction faction = CombatFaction.Player;

        private IProjectileFactory factory;
        private float nextAllowedShotTime;
        private float nextBurstShotTime;
        private int burstShotsRemaining;
        private bool configurationValid;

        public int ShotsFired { get; private set; }
        public WeaponDefinition Definition => definition;

        /// <summary>Assigns generated scene dependencies through stable contracts.</summary>
        public void Configure(
            WeaponDefinition weaponDefinition,
            PlayerInputReader inputReader,
            Transform firingTransform,
            MonoBehaviour factoryComponent,
            CombatFaction owningFaction)
        {
            definition = weaponDefinition;
            input = inputReader;
            muzzle = firingTransform;
            projectileFactory = factoryComponent;
            faction = owningFaction;
        }

        /// <summary>Validates once and resolves the serialized factory component to its interface.</summary>
        private void Awake()
        {
            factory = projectileFactory as IProjectileFactory;
            string reason = string.Empty;
            configurationValid = definition != null && input != null && muzzle != null && factory != null &&
                                 definition.IsValid(out reason);
            if (!configurationValid)
            {
                string detail = definition == null ? "Weapon definition is missing." : reason;
                AppLog.Error(LogSubsystem, $"Weapon configuration is invalid: {detail}", this);
                enabled = false;
                return;
            }

            AppLog.Info(LogSubsystem, $"Weapon '{definition.StableId}' initialized in {definition.FireMode} mode.", this);
        }

        /// <summary>Evaluates input edges and held state without allocating per frame.</summary>
        private void Update()
        {
            if (!configurationValid)
            {
                return;
            }

            bool pressed = input.ConsumeFirePressed();
            switch (definition.FireMode)
            {
                case WeaponFireMode.Single:
                    if (pressed)
                    {
                        TryFireStandardCadence();
                    }
                    break;

                case WeaponFireMode.Automatic:
                    if (input.FireHeld)
                    {
                        TryFireStandardCadence();
                    }
                    break;

                case WeaponFireMode.Burst:
                    UpdateBurst(pressed);
                    break;
            }
        }

        /// <summary>Fires once when the standard rate limiter permits it.</summary>
        private void TryFireStandardCadence()
        {
            if (Time.time < nextAllowedShotTime)
            {
                return;
            }

            if (FireOneShot())
            {
                nextAllowedShotTime = Time.time + definition.FireInterval;
            }
        }

        /// <summary>Starts and advances a fixed-size burst using its independent shot interval.</summary>
        private void UpdateBurst(bool pressed)
        {
            if (pressed && burstShotsRemaining == 0 && Time.time >= nextAllowedShotTime)
            {
                burstShotsRemaining = definition.BurstShotCount;
                nextBurstShotTime = Time.time;
            }

            if (burstShotsRemaining <= 0 || Time.time < nextBurstShotTime)
            {
                return;
            }

            if (FireOneShot())
            {
                burstShotsRemaining--;
                nextBurstShotTime = Time.time + definition.BurstShotInterval;
                if (burstShotsRemaining == 0)
                {
                    nextAllowedShotTime = Time.time + definition.FireInterval;
                }
            }
        }

        /// <summary>Creates one immutable factory request from the current firing transform.</summary>
        private bool FireOneShot()
        {
            ProjectileSpawnRequest request = new ProjectileSpawnRequest(
                muzzle.position,
                muzzle.forward,
                gameObject,
                faction,
                definition);
            if (!factory.TrySpawn(request))
            {
                AppLog.Warning(LogSubsystem, "Projectile factory rejected a shot.", this);
                return false;
            }

            ShotsFired++;
            AppLog.Development(LogSubsystem, $"Fired {definition.DamageType.DisplayName} shot #{ShotsFired}.", this);
            return true;
        }
    }
}
