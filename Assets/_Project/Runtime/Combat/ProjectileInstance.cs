using SphereCorridor.Foundation;
using UnityEngine;

namespace SphereCorridor.Combat
{
    /// <summary>
    /// Runtime projectile driven by a trajectory asset. A swept sphere query prevents
    /// fast shots from tunnelling without giving the visual projectile physical force.
    /// </summary>
    public sealed class ProjectileInstance : MonoBehaviour
    {
        private const string LogSubsystem = "Combat";
        private const int HitCapacity = 12;

        [SerializeField, Min(0.01f)] private float collisionRadius = 0.15f;
        [SerializeField] private LayerMask collisionMask = ~0;

        private readonly RaycastHit[] hits = new RaycastHit[HitCapacity];
        private ProjectilePoolFactory owningPool;
        private ProjectileSpawnRequest request;
        private Vector3 origin;
        private Vector3 direction;
        private float elapsed;
        private bool activeShot;
        private bool capacityWarningIssued;

        /// <summary>Associates an instantiated projectile with the factory that reclaims it.</summary>
        public void ConfigurePool(ProjectilePoolFactory pool)
        {
            owningPool = pool;
        }

        /// <summary>Initializes all mutable state whenever a pooled instance is fired.</summary>
        public void Launch(in ProjectileSpawnRequest spawnRequest)
        {
            request = spawnRequest;
            origin = spawnRequest.Position;
            direction = spawnRequest.Direction.normalized;
            elapsed = 0f;
            activeShot = true;
            transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction, Vector3.up));
            gameObject.SetActive(true);
        }

        /// <summary>Evaluates trajectory and collision at the fixed simulation cadence.</summary>
        private void FixedUpdate()
        {
            if (!activeShot || request.Weapon == null)
            {
                return;
            }

            float nextElapsed = elapsed + Time.fixedDeltaTime;
            if (nextElapsed >= request.Weapon.ProjectileLifetime)
            {
                ReturnToPool();
                return;
            }

            Vector3 desiredPosition = request.Weapon.Trajectory.EvaluatePosition(
                origin,
                direction,
                request.Weapon.ProjectileSpeed,
                nextElapsed,
                request.Weapon.ProjectileLifetime);
            Vector3 displacement = desiredPosition - transform.position;

            if (TryFindClosestHit(displacement, out RaycastHit hit))
            {
                ApplyHit(hit);
                ReturnToPool();
                return;
            }

            transform.position = desiredPosition;
            if (displacement.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(displacement.normalized, Vector3.up);
            }

            elapsed = nextElapsed;
        }

        /// <summary>Finds the nearest non-source collider along this frame's swept path.</summary>
        private bool TryFindClosestHit(Vector3 displacement, out RaycastHit closest)
        {
            closest = default;
            float distance = displacement.magnitude;
            if (distance <= 0.0001f)
            {
                return false;
            }

            int count = Physics.SphereCastNonAlloc(
                transform.position,
                collisionRadius,
                displacement / distance,
                hits,
                distance,
                collisionMask,
                QueryTriggerInteraction.Ignore);

            if (count == HitCapacity && !capacityWarningIssued)
            {
                capacityWarningIssued = true;
                AppLog.Warning(LogSubsystem, "Projectile hit buffer reached capacity.", this);
            }

            float closestDistance = float.PositiveInfinity;
            bool found = false;
            for (int index = 0; index < count; index++)
            {
                Collider collider = hits[index].collider;
                if (collider == null || IsPartOfSource(collider.transform))
                {
                    continue;
                }

                if (hits[index].distance < closestDistance)
                {
                    closestDistance = hits[index].distance;
                    closest = hits[index];
                    found = true;
                }
            }

            return found;
        }

        /// <summary>Rejects the firing entity and all children without relying on layers alone.</summary>
        private bool IsPartOfSource(Transform candidate)
        {
            if (request.Source == null || candidate == null)
            {
                return false;
            }

            Transform sourceTransform = request.Source.transform;
            return candidate == sourceTransform || candidate.IsChildOf(sourceTransform);
        }

        /// <summary>Builds the shared packet and lets the receiver own all mitigation rules.</summary>
        private void ApplyHit(RaycastHit hit)
        {
            Health health = hit.collider.GetComponentInParent<Health>();
            if (health == null)
            {
                AppLog.Development(LogSubsystem, $"Projectile blocked by '{hit.collider.name}'.", this);
                return;
            }

            DamagePacket packet = new DamagePacket(
                request.Weapon.Damage,
                request.Source,
                request.Faction,
                hit.point,
                direction,
                request.Weapon.DamageType);
            DamageResult result = health.ApplyDamage(packet);
            AppLog.Development(
                LogSubsystem,
                $"Projectile hit '{health.name}': accepted={result.Accepted}, immune={result.Immune}, " +
                $"healthDamage={result.HealthDamage:F2}.",
                health);
        }

        /// <summary>Returns through the owner so the weapon never knows pooling details.</summary>
        private void ReturnToPool()
        {
            activeShot = false;
            if (owningPool != null)
            {
                owningPool.Release(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>Shows the swept collision radius for tuning in the Scene view.</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, collisionRadius);
        }
    }
}
