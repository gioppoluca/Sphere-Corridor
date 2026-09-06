using SphereCorridor.Foundation;
using SphereCorridor.Movement;
using UnityEngine;

namespace SphereCorridor.CameraSystem
{
    /// <summary>
    /// Follows the player from an elevated three-quarter angle with speed-based
    /// look-ahead. World X remains screen-horizontal and both corridor walls stay visible.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(100)]
    public sealed class OverheadFollowCamera : MonoBehaviour
    {
        private const string LogSubsystem = "Camera";

        [SerializeField] private Transform target;
        [SerializeField] private PlayerMovementMotor motor;
        [SerializeField] private MovementCameraDefinition definition;

        private Vector3 positionVelocity;
        private bool configurationValid;
        private float forwardFocusX;

        /// <summary>
        /// Gets the current world X coordinate of the permitted rear screen margin.
        /// Movement constraints and segment retirement share this measured boundary.
        /// </summary>
        public float RearBoundaryX { get; private set; }

        /// <summary>Gets whether the camera has successfully projected its rear boundary.</summary>
        public bool HasRearBoundary { get; private set; }

        /// <summary>
        /// Assigns the generated movement-lab target, motor, and tuning asset.
        /// </summary>
        public void Configure(
            Transform followTarget,
            PlayerMovementMotor movementMotor,
            MovementCameraDefinition cameraDefinition)
        {
            target = followTarget;
            motor = movementMotor;
            definition = cameraDefinition;
        }

        /// <summary>
        /// Snaps to the initial composition before smooth following begins.
        /// </summary>
        private void Start()
        {
            configurationValid = target != null && motor != null && definition != null;
            if (!configurationValid)
            {
                AppLog.Error(LogSubsystem, "Overhead camera references are incomplete.", this);
                enabled = false;
                return;
            }

            forwardFocusX = target.position.x;
            ApplyCameraPose(useSmoothing: false);
            UpdateRearBoundary();
            AppLog.Info(LogSubsystem, "Elevated three-quarter corridor camera initialized.", this);
        }

        /// <summary>
        /// Updates after movement so interpolation does not introduce visible follow jitter.
        /// </summary>
        private void LateUpdate()
        {
            if (configurationValid)
            {
                ApplyCameraPose(useSmoothing: true);
                UpdateRearBoundary();
            }
        }

        /// <summary>
        /// Computes a forward focus point and looks downward across the corridor width.
        /// </summary>
        private void ApplyCameraPose(bool useSmoothing)
        {
            float speedRatio = Mathf.Clamp01(Mathf.Abs(motor.CurrentSpeed) / definition.SpeedForMaximumLookAhead);
            float lookAhead = Mathf.Lerp(definition.MinimumLookAhead, definition.MaximumLookAhead, speedRatio);

            // Keep camera height tied to the corridor rather than to the jumping player.
            // This makes the sphere visibly rise and fall instead of having the camera
            // cancel its vertical motion.
            float requestedFocusX = target.position.x + lookAhead;
            forwardFocusX = Mathf.Max(forwardFocusX, requestedFocusX);

            Vector3 focusPoint = new Vector3(
                forwardFocusX,
                definition.FocusHeight,
                0f);
            Vector3 desiredPosition = focusPoint + Vector3.up * definition.Height + Vector3.back * definition.DepthDistance;

            transform.position = useSmoothing
                ? Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, definition.PositionSmoothTime)
                : desiredPosition;

            transform.rotation = Quaternion.LookRotation(focusPoint - transform.position, Vector3.up);
        }

        /// <summary>
        /// Projects the configured viewport margin onto the player's movement plane.
        /// Using the real camera ray keeps the boundary correct after aspect-ratio changes.
        /// </summary>
        private void UpdateRearBoundary()
        {
            UnityEngine.Camera sceneCamera = GetComponent<UnityEngine.Camera>();
            Ray boundaryRay = sceneCamera.ViewportPointToRay(
                new Vector3(definition.RearViewportMargin, 0.5f, 0f));
            Plane movementPlane = new Plane(Vector3.up, new Vector3(0f, definition.FocusHeight, 0f));

            if (movementPlane.Raycast(boundaryRay, out float enter))
            {
                RearBoundaryX = boundaryRay.GetPoint(enter).x;
                HasRearBoundary = true;
            }
        }
    }
}
