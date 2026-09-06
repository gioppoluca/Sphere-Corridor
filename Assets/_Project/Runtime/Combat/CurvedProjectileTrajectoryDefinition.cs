using UnityEngine;

namespace SphereCorridor.Combat
{
    /// <summary>
    /// Optional authored curve proving that trajectory is a replaceable strategy.
    /// It is available for experiments but is not selected by the M2 plasma weapon.
    /// </summary>
    [CreateAssetMenu(fileName = "CurvedTrajectory", menuName = "Sphere Corridor/Combat/Trajectories/Curved")]
    public sealed class CurvedProjectileTrajectoryDefinition : ProjectileTrajectoryDefinition
    {
        [SerializeField] private float lateralOffset = 2f;
        [SerializeField, Min(0f)] private float arcHeight = 1.5f;

        public override Vector3 EvaluatePosition(
            Vector3 origin,
            Vector3 forward,
            float speed,
            float elapsedSeconds,
            float lifetimeSeconds)
        {
            Vector3 direction = forward.normalized;
            Vector3 lateral = Vector3.Cross(Vector3.up, direction).normalized;
            float normalizedTime = Mathf.Clamp01(elapsedSeconds / Mathf.Max(0.01f, lifetimeSeconds));
            float smoothProgress = normalizedTime * normalizedTime * (3f - 2f * normalizedTime);
            float verticalArc = 4f * arcHeight * normalizedTime * (1f - normalizedTime);
            return origin + direction * speed * elapsedSeconds +
                   lateral * lateralOffset * smoothProgress + Vector3.up * verticalArc;
        }
    }
}
