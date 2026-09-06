using SphereCorridor.Foundation;
using UnityEngine;

namespace SphereCorridor.Movement
{
    /// <summary>
    /// Resets the player after falling through the M1 test gap so handling can be
    /// evaluated repeatedly. Final defeat ownership belongs to a later game-state system.
    /// </summary>
    public sealed class MovementLabRespawner : MonoBehaviour
    {
        private const string LogSubsystem = "MovementLab";

        [SerializeField] private Rigidbody body;
        [SerializeField] private Vector3 respawnPosition;
        [SerializeField] private float resetBelowY = -8f;

        /// <summary>
        /// Assigns the generated body, reset point, and fall threshold.
        /// </summary>
        public void Configure(Rigidbody rigidbodyComponent, Vector3 position, float threshold)
        {
            body = rigidbodyComponent;
            respawnPosition = position;
            resetBelowY = threshold;
        }

        /// <summary>
        /// Restores a deterministic state on a physics boundary after a failed gap jump.
        /// </summary>
        private void FixedUpdate()
        {
            if (body == null || body.position.y >= resetBelowY)
            {
                return;
            }

            body.position = respawnPosition;
            body.rotation = Quaternion.identity;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            AppLog.Warning(LogSubsystem, "Player fell from the lab and was reset to the start.", this);
        }
    }
}
