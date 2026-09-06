using UnityEngine;

namespace SphereCorridor.Movement
{
    /// <summary>
    /// Converts player intent into a target horizontal speed and response rate.
    /// Keeping this decision pure makes the hybrid cruise/brake/reverse rule testable.
    /// </summary>
    public static class PlayerMovementMath
    {
        /// <summary>
        /// Chooses the target speed required by the approved hybrid movement rule.
        /// </summary>
        /// <param name="currentSpeed">Current world-space velocity along the corridor.</param>
        /// <param name="horizontalInput">Normalized input from -1 to +1.</param>
        /// <param name="definition">Central movement tuning asset.</param>
        /// <param name="acceleration">Returned positive rate used to approach the target.</param>
        /// <returns>The desired corridor speed for this physics step.</returns>
        public static float CalculateTargetSpeed(
            float currentSpeed,
            float horizontalInput,
            PlayerMovementDefinition definition,
            out float acceleration)
        {
            float input = Mathf.Clamp(horizontalInput, -1f, 1f);

            if (input > definition.InputDeadZone)
            {
                // Light positive analog input preserves cruise; full input requests maximum speed.
                float normalizedInput = Mathf.InverseLerp(definition.InputDeadZone, 1f, input);
                acceleration = definition.ForwardAcceleration;
                return Mathf.Lerp(definition.CruiseSpeed, definition.MaximumSpeed, normalizedInput);
            }

            if (input < -definition.InputDeadZone)
            {
                acceleration = currentSpeed > 0f
                    ? definition.BrakeDeceleration
                    : definition.ForwardAcceleration;

                // Braking first targets zero. Continued input requests deliberately limited reverse.
                if (currentSpeed > 0f)
                {
                    return 0f;
                }

                float normalizedInput = Mathf.InverseLerp(definition.InputDeadZone, 1f, -input);
                return Mathf.Lerp(0f, definition.ReverseSpeed, normalizedInput);
            }

            acceleration = definition.ReturnToCruiseAcceleration;
            return definition.CruiseSpeed;
        }
    }
}
