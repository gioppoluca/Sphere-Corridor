using SphereCorridor.Foundation;
using UnityEngine;

namespace SphereCorridor.Movement
{
    /// <summary>
    /// Briefly squashes the collider-free visual shell when the player lands.
    /// The physical root remains unchanged, so feedback cannot destabilize collisions.
    /// </summary>
    public sealed class PlayerLandingFeedback : MonoBehaviour
    {
        private const string LogSubsystem = "MovementFeedback";

        [SerializeField] private PlayerMovementMotor motor;
        [SerializeField] private Transform visualShell;
        [SerializeField, Min(0.01f)] private float minimumVisibleImpactSpeed = 2f;
        [SerializeField, Min(0.01f)] private float fullFeedbackImpactSpeed = 12f;
        [SerializeField, Range(0f, 0.35f)] private float minimumSquash = 0.03f;
        [SerializeField, Range(0f, 0.35f)] private float maximumSquash = 0.18f;
        [SerializeField, Min(0.01f)] private float recoverySpeed = 14f;

        private Vector3 restingScale;
        private bool subscribed;

        /// <summary>
        /// Assigns the movement event source and the safe-to-scale presentation child.
        /// </summary>
        public void Configure(PlayerMovementMotor movementMotor, Transform shell)
        {
            motor = movementMotor;
            visualShell = shell;
            restingScale = shell == null ? Vector3.one : shell.localScale;
            Subscribe();
        }

        /// <summary>
        /// Handles scene-authored instances that were configured before Play mode.
        /// </summary>
        private void OnEnable()
        {
            Subscribe();
        }

        /// <summary>
        /// Starts from the serialized shell scale after Unity has initialized the scene.
        /// </summary>
        private void Start()
        {
            if (visualShell != null)
            {
                restingScale = visualShell.localScale;
            }

            Subscribe();
        }

        /// <summary>
        /// Releases the event subscription so disabled or destroyed objects receive no callbacks.
        /// </summary>
        private void OnDisable()
        {
            if (subscribed && motor != null)
            {
                motor.Landed -= HandleLanding;
            }

            subscribed = false;
        }

        /// <summary>
        /// Smoothly restores the shell after the one-frame impact squash.
        /// </summary>
        private void Update()
        {
            if (visualShell == null)
            {
                return;
            }

            float blend = 1f - Mathf.Exp(-recoverySpeed * Time.deltaTime);
            visualShell.localScale = Vector3.Lerp(visualShell.localScale, restingScale, blend);
        }

        /// <summary>
        /// Safely subscribes once after both generated references are available.
        /// </summary>
        private void Subscribe()
        {
            if (subscribed || motor == null)
            {
                return;
            }

            motor.Landed += HandleLanding;
            subscribed = true;
        }

        /// <summary>
        /// Converts downward impact speed into a small, capped visual deformation.
        /// </summary>
        private void HandleLanding(float impactSpeed)
        {
            if (visualShell == null || impactSpeed < minimumVisibleImpactSpeed)
            {
                return;
            }

            float normalizedImpact = Mathf.InverseLerp(
                minimumVisibleImpactSpeed,
                fullFeedbackImpactSpeed,
                impactSpeed);
            float squash = Mathf.Lerp(minimumSquash, maximumSquash, normalizedImpact);

            visualShell.localScale = Vector3.Scale(
                restingScale,
                new Vector3(1f + squash, 1f - squash, 1f + squash));

            AppLog.Development(LogSubsystem, $"Landing feedback applied at {impactSpeed:F2} m/s.", this);
        }
    }
}
