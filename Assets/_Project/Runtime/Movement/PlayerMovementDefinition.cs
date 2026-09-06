using UnityEngine;

namespace SphereCorridor.Movement
{
    /// <summary>
    /// Stores every M1 handling value in one asset so playtesting can tune movement
    /// without changing code or scattering unexplained constants across components.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerMovementDefinition", menuName = "Sphere Corridor/Movement Definition")]
    public sealed class PlayerMovementDefinition : ScriptableObject
    {
        [Header("Body")]
        [SerializeField, Min(0.1f)] private float sphereRadius = 0.6f;

        [Header("Horizontal speed")]
        [SerializeField, Min(0f)] private float cruiseSpeed = 7f;
        [SerializeField, Min(0f)] private float maximumSpeed = 11f;
        [SerializeField, Min(0f)] private float reverseSpeedMagnitude = 2.5f;
        [SerializeField, Min(0f)] private float inputDeadZone = 0.1f;

        [Header("Horizontal response")]
        [SerializeField, Min(0.01f)] private float forwardAcceleration = 18f;
        [SerializeField, Min(0.01f)] private float brakeDeceleration = 24f;
        [SerializeField, Min(0.01f)] private float returnToCruiseAcceleration = 10f;
        [SerializeField, Range(0f, 1f)] private float airControlMultiplier = 0.45f;

        [Header("Lateral response")]
        [SerializeField, Min(0f)] private float maximumLateralSpeed = 5f;
        [SerializeField, Min(0.01f)] private float lateralAcceleration = 22f;

        [Header("Jump")]
        [SerializeField, Min(0.01f)] private float jumpLaunchSpeed = 9.5f;
        [SerializeField, Tooltip("Use a negative value so gravity accelerates downward.")]
        private float gravity = -25f;
        [SerializeField, Min(1f)] private float releasedJumpGravityMultiplier = 2.2f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.1f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;

        [Header("Ground probe")]
        [SerializeField, Range(0.5f, 0.99f)] private float groundProbeRadiusScale = 0.9f;
        [SerializeField, Min(0f)] private float groundProbeStartOffset = 0.05f;
        [SerializeField, Min(0.01f)] private float groundProbeDistance = 0.12f;
        [SerializeField, Range(0f, 1f)] private float minimumGroundNormalY = 0.65f;

        public float SphereRadius => sphereRadius;
        public float CruiseSpeed => cruiseSpeed;
        public float MaximumSpeed => maximumSpeed;
        public float ReverseSpeed => -reverseSpeedMagnitude;
        public float InputDeadZone => inputDeadZone;
        public float ForwardAcceleration => forwardAcceleration;
        public float BrakeDeceleration => brakeDeceleration;
        public float ReturnToCruiseAcceleration => returnToCruiseAcceleration;
        public float AirControlMultiplier => airControlMultiplier;
        public float MaximumLateralSpeed => maximumLateralSpeed;
        public float LateralAcceleration => lateralAcceleration;
        public float JumpLaunchSpeed => jumpLaunchSpeed;
        public float Gravity => gravity;
        public float ReleasedJumpGravityMultiplier => releasedJumpGravityMultiplier;
        public float CoyoteTime => coyoteTime;
        public float JumpBufferTime => jumpBufferTime;
        public float GroundProbeRadiusScale => groundProbeRadiusScale;
        public float GroundProbeStartOffset => groundProbeStartOffset;
        public float GroundProbeDistance => groundProbeDistance;
        public float MinimumGroundNormalY => minimumGroundNormalY;

        /// <summary>
        /// Detects invalid relationships that individual Inspector ranges cannot express.
        /// </summary>
        /// <param name="reason">A developer-facing explanation when validation fails.</param>
        /// <returns>True when the definition is safe for the M1 motor.</returns>
        public bool IsValid(out string reason)
        {
            if (cruiseSpeed > maximumSpeed)
            {
                reason = "Cruise speed cannot exceed maximum speed.";
                return false;
            }

            if (gravity >= 0f)
            {
                reason = "Gravity must be negative.";
                return false;
            }

            if (sphereRadius <= 0f || jumpLaunchSpeed <= 0f)
            {
                reason = "Sphere radius and jump launch speed must be positive.";
                return false;
            }


            if (maximumLateralSpeed <= 0f || lateralAcceleration <= 0f)
            {
                reason = "Lateral speed and acceleration must be positive.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>
        /// Repairs simple Inspector relationships immediately after authoring changes.
        /// </summary>
        private void OnValidate()
        {
            maximumSpeed = Mathf.Max(maximumSpeed, cruiseSpeed);
            maximumLateralSpeed = Mathf.Max(0.01f, maximumLateralSpeed);
            lateralAcceleration = Mathf.Max(0.01f, lateralAcceleration);
            gravity = Mathf.Min(gravity, -0.01f);
        }
    }
}
