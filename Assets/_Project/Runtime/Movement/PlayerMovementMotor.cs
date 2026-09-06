using SphereCorridor.Foundation;
using UnityEngine;

namespace SphereCorridor.Movement
{
    /// <summary>
    /// Implements the motor-controlled dynamic Rigidbody specified by the GDD.
    /// The root translates without physical rolling; a child shell supplies visual rotation.
    /// </summary>
    public sealed class PlayerMovementMotor : MonoBehaviour
    {
        private const string LogSubsystem = "Movement";
        private const int GroundHitCapacity = 8;

        [SerializeField] private PlayerMovementDefinition definition;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Rigidbody body;
        [SerializeField] private SphereCollider bodyCollider;
        [SerializeField] private Transform visualShell;
        [SerializeField] private LayerMask groundMask = ~0;

        private readonly RaycastHit[] groundHits = new RaycastHit[GroundHitCapacity];
        private float coyoteTimeRemaining;
        private float jumpBufferRemaining;
        private bool wasGrounded;
        private bool configurationValid;
        private bool groundCapacityWarningIssued;

        /// <summary>
        /// Notifies presentation components after airborne movement reaches valid ground.
        /// The value is the positive downward impact speed measured before adhesion.
        /// </summary>
        public event System.Action<float> Landed;

        /// <summary>
        /// Gets the measured world speed along the corridor's positive X axis.
        /// </summary>
        public float CurrentSpeed => body == null ? 0f : body.linearVelocity.x;

        /// <summary>
        /// Gets the current world vertical speed for the debug overlay and camera diagnostics.
        /// </summary>
        public float VerticalSpeed => body == null ? 0f : body.linearVelocity.y;

        /// <summary>Gets measured cross-corridor speed on the world Z axis.</summary>
        public float LateralSpeed => body == null ? 0f : body.linearVelocity.z;

        /// <summary>
        /// Gets whether the most recent physics probe found a valid walkable surface.
        /// </summary>
        public bool IsGrounded { get; private set; }

        /// <summary>
        /// Gets the last target speed selected by the hybrid movement rule.
        /// </summary>
        public float TargetSpeed { get; private set; }

        /// <summary>
        /// Gets the current normalized horizontal input for diagnostics.
        /// </summary>
        public float HorizontalInput => input == null ? 0f : input.Horizontal;

        /// <summary>Gets current cross-corridor input for diagnostics.</summary>
        public float LateralInput => input == null ? 0f : input.Lateral;

        /// <summary>
        /// Assigns generated scene references without exposing writable public fields.
        /// </summary>
        public void Configure(
            PlayerMovementDefinition movementDefinition,
            PlayerInputReader inputReader,
            Rigidbody rigidbodyComponent,
            SphereCollider sphereCollider,
            Transform shell)
        {
            definition = movementDefinition;
            input = inputReader;
            body = rigidbodyComponent;
            bodyCollider = sphereCollider;
            visualShell = shell;
        }

        /// <summary>
        /// Verifies authoring references and configures the Rigidbody as a stable 2.5D motor.
        /// </summary>
        private void Awake()
        {
            configurationValid = ValidateConfiguration();
            if (!configurationValid)
            {
                enabled = false;
                return;
            }

            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            // Rotation remains presentation-only, but Z is now a deliberate gameplay axis.
            body.constraints = RigidbodyConstraints.FreezeRotation;
            AppLog.Info(LogSubsystem, "M2 motor initialized with forward, lateral, and jump movement.", this);
        }

        /// <summary>
        /// Advances all motor state at Unity's fixed physics cadence.
        /// </summary>
        private void FixedUpdate()
        {
            if (!configurationValid)
            {
                return;
            }

            float deltaTime = Time.fixedDeltaTime;
            UpdateGroundState();
            UpdateForgivenessTimers(deltaTime);

            Vector3 velocity = body.linearVelocity;
            float acceleration;
            TargetSpeed = PlayerMovementMath.CalculateTargetSpeed(
                velocity.x,
                input.Horizontal,
                definition,
                out acceleration);

            if (!IsGrounded)
            {
                acceleration *= definition.AirControlMultiplier;
            }

            velocity.x = Mathf.MoveTowards(velocity.x, TargetSpeed, acceleration * deltaTime);
            float lateralAcceleration = definition.LateralAcceleration;
            if (!IsGrounded)
            {
                lateralAcceleration *= definition.AirControlMultiplier;
            }

            float lateralTarget = input.Lateral * definition.MaximumLateralSpeed;
            velocity.z = Mathf.MoveTowards(velocity.z, lateralTarget, lateralAcceleration * deltaTime);
            ApplyJumpAndGravity(ref velocity, deltaTime);

            // This is an intentional motor, not a force-only simulation: it owns
            // horizontal response and custom gravity while collisions remain dynamic.
            body.linearVelocity = velocity;
            RotateVisualShell(new Vector3(velocity.x, 0f, velocity.z), deltaTime);
        }

        /// <summary>
        /// Updates jump buffering and coyote time before evaluating a launch.
        /// </summary>
        private void UpdateForgivenessTimers(float deltaTime)
        {
            if (input.ConsumeJumpPressed())
            {
                jumpBufferRemaining = definition.JumpBufferTime;
            }
            else
            {
                jumpBufferRemaining = Mathf.Max(0f, jumpBufferRemaining - deltaTime);
            }

            if (IsGrounded)
            {
                coyoteTimeRemaining = definition.CoyoteTime;
            }
            else
            {
                coyoteTimeRemaining = Mathf.Max(0f, coyoteTimeRemaining - deltaTime);
            }
        }

        /// <summary>
        /// Applies buffered launch, variable jump height, custom gravity, and ground adhesion.
        /// </summary>
        private void ApplyJumpAndGravity(ref Vector3 velocity, float deltaTime)
        {
            bool canLaunch = jumpBufferRemaining > 0f && coyoteTimeRemaining > 0f;
            if (canLaunch)
            {
                velocity.y = definition.JumpLaunchSpeed;
                jumpBufferRemaining = 0f;
                coyoteTimeRemaining = 0f;
                IsGrounded = false;
                AppLog.Info(LogSubsystem, $"Jump launched at {definition.JumpLaunchSpeed:F2} m/s.", this);
            }

            float gravityMultiplier = !input.JumpHeld && velocity.y > 0f
                ? definition.ReleasedJumpGravityMultiplier
                : 1f;

            velocity.y += definition.Gravity * gravityMultiplier * deltaTime;

            // A small downward velocity maintains stable contact without accumulating fall speed.
            if (IsGrounded && velocity.y < 0f)
            {
                velocity.y = -1f;
            }
        }

        /// <summary>
        /// Uses a non-allocating sphere cast and validated normals rather than collision flags.
        /// </summary>
        private void UpdateGroundState()
        {
            float probeRadius = definition.SphereRadius * definition.GroundProbeRadiusScale;
            float radiusDifference = definition.SphereRadius - probeRadius;
            Vector3 origin = body.position + Vector3.up * definition.GroundProbeStartOffset;
            float distance = definition.GroundProbeStartOffset + radiusDifference + definition.GroundProbeDistance;

            int hitCount = Physics.SphereCastNonAlloc(
                origin,
                probeRadius,
                Vector3.down,
                groundHits,
                distance,
                groundMask,
                QueryTriggerInteraction.Ignore);

            IsGrounded = false;
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit hit = groundHits[index];
                if (hit.collider == null || hit.collider == bodyCollider || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (hit.normal.y >= definition.MinimumGroundNormalY)
                {
                    IsGrounded = true;
                    break;
                }
            }

            if (hitCount == GroundHitCapacity && !groundCapacityWarningIssued)
            {
                groundCapacityWarningIssued = true;
                AppLog.Warning(LogSubsystem, "Ground probe buffer reached capacity; inspect overlapping colliders.", this);
            }

            if (IsGrounded != wasGrounded)
            {
                if (IsGrounded)
                {
                    float impactSpeed = Mathf.Max(0f, -body.linearVelocity.y);
                    Landed?.Invoke(impactSpeed);
                }

                AppLog.Development(LogSubsystem, IsGrounded ? "Ground contact acquired." : "Ground contact lost.", this);
                wasGrounded = IsGrounded;
            }
        }

        /// <summary>
        /// Rotates only the visual shell from measured ground speed, avoiding torque instability.
        /// </summary>
        private void RotateVisualShell(Vector3 planarVelocity, float deltaTime)
        {
            if (!IsGrounded || visualShell == null || planarVelocity.sqrMagnitude < 0.0001f)
            {
                return;
            }

            // A rolling sphere rotates around the axis perpendicular to its travel.
            Vector3 rollingAxis = Vector3.Cross(Vector3.up, planarVelocity.normalized);
            float degrees = planarVelocity.magnitude * deltaTime / definition.SphereRadius * Mathf.Rad2Deg;
            visualShell.Rotate(rollingAxis, degrees, Space.World);
        }

        /// <summary>
        /// Reports every missing dependency once instead of failing later with null references.
        /// </summary>
        private bool ValidateConfiguration()
        {
            if (definition == null || input == null || body == null || bodyCollider == null || visualShell == null)
            {
                AppLog.Error(LogSubsystem, "Movement motor references are incomplete.", this);
                return false;
            }

            if (!definition.IsValid(out string reason))
            {
                AppLog.Error(LogSubsystem, $"Movement definition is invalid: {reason}", definition);
                return false;
            }

            if (!Mathf.Approximately(bodyCollider.radius, definition.SphereRadius))
            {
                AppLog.Error(LogSubsystem, "SphereCollider radius differs from the movement definition.", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Draws the current ground probe only when the object is selected in the Editor.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (definition == null || body == null)
            {
                return;
            }

            float probeRadius = definition.SphereRadius * definition.GroundProbeRadiusScale;
            float radiusDifference = definition.SphereRadius - probeRadius;
            Vector3 origin = body.position + Vector3.up * definition.GroundProbeStartOffset;
            float distance = definition.GroundProbeStartOffset + radiusDifference + definition.GroundProbeDistance;

            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(origin + Vector3.down * distance, probeRadius);
        }
    }
}
