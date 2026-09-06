using SphereCorridor.CameraSystem;
using SphereCorridor.Foundation;
using UnityEngine;

namespace SphereCorridor.Movement
{
    /// <summary>
    /// Prevents indefinite backtracking by keeping the player inside the camera's
    /// measured rear viewport margin. It does not affect ordinary forward movement.
    /// </summary>
    public sealed class PlayerRearBoundary : MonoBehaviour
    {
        private const string LogSubsystem = "MovementBoundary";

        [SerializeField] private Rigidbody body;
        [SerializeField] private OverheadFollowCamera followCamera;

        private bool wasConstrained;

        /// <summary>
        /// Assigns the physics body and the camera that owns screen composition.
        /// </summary>
        public void Configure(Rigidbody rigidbodyComponent, OverheadFollowCamera cameraController)
        {
            body = rigidbodyComponent;
            followCamera = cameraController;
        }

        /// <summary>
        /// Clamps only after the motor has calculated velocity for this physics step.
        /// </summary>
        private void FixedUpdate()
        {
            if (body == null || followCamera == null || !followCamera.HasRearBoundary)
            {
                return;
            }

            bool isOutsideRearBoundary = body.position.x < followCamera.RearBoundaryX;
            if (isOutsideRearBoundary)
            {
                Vector3 correctedPosition = body.position;
                correctedPosition.x = followCamera.RearBoundaryX;
                body.position = correctedPosition;

                Vector3 correctedVelocity = body.linearVelocity;
                correctedVelocity.x = Mathf.Max(0f, correctedVelocity.x);
                body.linearVelocity = correctedVelocity;

                if (!wasConstrained)
                {
                    AppLog.Development(LogSubsystem, "Rear viewport boundary engaged.", this);
                }
            }

            wasConstrained = isOutsideRearBoundary;
        }
    }
}
