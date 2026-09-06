using System.Collections.Generic;
using SphereCorridor.Foundation;
using UnityEngine;

namespace SphereCorridor.Combat
{
    /// <summary>
    /// Small single-prefab pool implementing the projectile factory seam mandated by
    /// the GDD. Later weapon-specific pools can replace it without changing callers.
    /// </summary>
    public sealed class ProjectilePoolFactory : MonoBehaviour, IProjectileFactory
    {
        private const string LogSubsystem = "Combat";

        [SerializeField] private ProjectileInstance projectilePrefab;
        [SerializeField] private Transform inactiveRoot;
        [SerializeField, Min(0)] private int prewarmCount = 12;
        [SerializeField, Min(1)] private int maximumRetained = 32;

        private readonly Queue<ProjectileInstance> available = new Queue<ProjectileInstance>();
        private bool initialized;

        /// <summary>Assigns the laboratory prefab and capacity without exposing mutable fields.</summary>
        public void Configure(ProjectileInstance prefab, Transform poolRoot, int initialCount, int retainedLimit)
        {
            projectilePrefab = prefab;
            inactiveRoot = poolRoot;
            prewarmCount = Mathf.Max(0, initialCount);
            maximumRetained = Mathf.Max(1, retainedLimit);
        }

        /// <summary>Obtains an inactive instance or expands the pool when all are in flight.</summary>
        public bool TrySpawn(in ProjectileSpawnRequest request)
        {
            EnsureInitialized();
            if (projectilePrefab == null || request.Weapon == null)
            {
                AppLog.Error(LogSubsystem, "Projectile pool or spawn request is incomplete.", this);
                return false;
            }

            ProjectileInstance projectile = available.Count > 0 ? available.Dequeue() : CreateInstance();
            projectile.Launch(request);
            return true;
        }

        /// <summary>Reclaims an instance and caps retained memory after temporary bursts.</summary>
        public void Release(ProjectileInstance projectile)
        {
            if (projectile == null)
            {
                return;
            }

            projectile.gameObject.SetActive(false);
            projectile.transform.SetParent(inactiveRoot == null ? transform : inactiveRoot, false);
            if (available.Count < maximumRetained)
            {
                available.Enqueue(projectile);
            }
            else
            {
                Destroy(projectile.gameObject);
            }
        }

        /// <summary>Prepares reusable instances once after scene deserialization.</summary>
        private void Awake()
        {
            EnsureInitialized();
        }

        /// <summary>Idempotently validates and prewarms the pool.</summary>
        private void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            if (projectilePrefab == null)
            {
                AppLog.Error(LogSubsystem, "Projectile prefab is missing from the pool.", this);
                return;
            }

            for (int index = 0; index < prewarmCount; index++)
            {
                ProjectileInstance projectile = CreateInstance();
                projectile.gameObject.SetActive(false);
                available.Enqueue(projectile);
            }

            AppLog.Info(LogSubsystem, $"Projectile pool prewarmed with {prewarmCount} instances.", this);
        }

        /// <summary>Creates one owned instance and records its return path.</summary>
        private ProjectileInstance CreateInstance()
        {
            ProjectileInstance projectile = Instantiate(
                projectilePrefab,
                inactiveRoot == null ? transform : inactiveRoot);
            projectile.ConfigurePool(this);
            projectile.gameObject.SetActive(false);
            return projectile;
        }
    }
}
