using UnityEngine;

namespace SphereCorridor.Combat
{
    /// <summary>Implements the fixed forward trajectory selected for the M2 plasma shot.</summary>
    [CreateAssetMenu(fileName = "StraightTrajectory", menuName = "Sphere Corridor/Combat/Trajectories/Straight")]
    public sealed class StraightProjectileTrajectoryDefinition : ProjectileTrajectoryDefinition
    {
        public override Vector3 EvaluatePosition(
            Vector3 origin,
            Vector3 forward,
            float speed,
            float elapsedSeconds,
            float lifetimeSeconds)
        {
            return origin + forward.normalized * speed * elapsedSeconds;
        }
    }
}
