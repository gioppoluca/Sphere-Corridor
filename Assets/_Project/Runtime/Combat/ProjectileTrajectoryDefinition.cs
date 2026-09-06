using UnityEngine;

namespace SphereCorridor.Combat
{
    /// <summary>
    /// Stateless trajectory strategy. Every projectile evaluates its own elapsed time,
    /// so the same immutable asset can safely drive many concurrent shots.
    /// </summary>
    public abstract class ProjectileTrajectoryDefinition : ScriptableObject
    {
        /// <summary>Returns world position at one elapsed time from immutable launch data.</summary>
        public abstract Vector3 EvaluatePosition(
            Vector3 origin,
            Vector3 forward,
            float speed,
            float elapsedSeconds,
            float lifetimeSeconds);
    }
}
