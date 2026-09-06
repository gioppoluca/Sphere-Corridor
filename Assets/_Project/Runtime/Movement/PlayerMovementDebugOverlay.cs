using SphereCorridor.Corridor;
using UnityEngine;

namespace SphereCorridor.Movement
{
    /// <summary>
    /// Shows live M1 tuning evidence in the Editor and Development Builds.
    /// It is intentionally read-only and does not participate in movement logic.
    /// </summary>
    public sealed class PlayerMovementDebugOverlay : MonoBehaviour
    {
        [SerializeField] private PlayerMovementMotor motor;
        [SerializeField] private CorridorSegmentStreamer segmentStreamer;

        /// <summary>
        /// Assigns the generated player motor displayed by the overlay.
        /// </summary>
        /// <param name="movementMotor">The active M1 motor.</param>
        public void Configure(
            PlayerMovementMotor movementMotor,
            CorridorSegmentStreamer corridorStreamer)
        {
            motor = movementMotor;
            segmentStreamer = corridorStreamer;
        }

        /// <summary>
        /// Draws a compact diagnostic panel without generating per-frame Console spam.
        /// </summary>
        private void OnGUI()
        {
            if (!Debug.isDebugBuild || motor == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(20f, 20f, 470f, 250f), GUI.skin.box);
            GUILayout.Label("M2 COMBAT LAB — STREAMED CORRIDOR");
            GUILayout.Label($"Forward speed:    {motor.CurrentSpeed,6:F2} m/s");
            GUILayout.Label($"Target speed:     {motor.TargetSpeed,6:F2} m/s");
            GUILayout.Label($"Lateral speed:   {motor.LateralSpeed,6:F2} m/s");
            GUILayout.Label($"Vertical speed:   {motor.VerticalSpeed,6:F2} m/s");
            GUILayout.Label($"Speed input:      {motor.HorizontalInput,6:F2}");
            GUILayout.Label($"Lateral input:    {motor.LateralInput,6:F2}");
            GUILayout.Label($"Grounded:         {motor.IsGrounded}");
            GUILayout.Label($"Live segments:     {segmentStreamer?.ActiveSegmentCount ?? 0}");
            GUILayout.Label($"Current type:      {segmentStreamer?.CurrentSegmentId ?? "unavailable"}");
            GUILayout.Label("Arrows / WASD: speed and lateral movement   Space: jump");
            GUILayout.Label("Gamepad: left stick or triggers move   A: jump   X/RB: fire");
            GUILayout.Label("Keyboard fire: Left Mouse / Left Ctrl");
            GUILayout.EndArea();
        }
    }
}
