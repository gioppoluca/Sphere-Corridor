using UnityEngine;

namespace SphereCorridor.Foundation
{
    /// <summary>
    /// Provides the shared log format used by Sphere Corridor systems.
    /// Centralizing the prefix makes Console output searchable and consistent.
    /// </summary>
    public static class AppLog
    {
        private const string ProductPrefix = "SphereCorridor";

        /// <summary>
        /// Records a meaningful lifecycle event or state transition.
        /// </summary>
        /// <param name="subsystem">Short owner name, such as Bootstrap or Input.</param>
        /// <param name="message">Human-readable diagnostic information.</param>
        /// <param name="context">Optional Unity object that can be selected from the Console.</param>
        public static void Info(string subsystem, string message, Object context = null)
        {
            Debug.Log(Format(subsystem, message), context);
        }

        /// <summary>
        /// Records verbose evidence only in the Editor or a Development Build.
        /// This avoids shipping diagnostic noise in a release build.
        /// </summary>
        /// <param name="subsystem">Short owner name, such as Bootstrap or Input.</param>
        /// <param name="message">Human-readable diagnostic information.</param>
        /// <param name="context">Optional Unity object that can be selected from the Console.</param>
        public static void Development(string subsystem, string message, Object context = null)
        {
            // Unity 6.6 deprecates the old development-build scripting symbol.
            // isDebugBuild expresses the intent directly without a deprecated
            // compile-time branch and remains false in normal release players.
            if (!Debug.isDebugBuild)
            {
                return;
            }

            Debug.Log(Format(subsystem, message), context);
        }

        /// <summary>
        /// Records an unexpected but recoverable condition.
        /// </summary>
        /// <param name="subsystem">Short owner name, such as Bootstrap or Input.</param>
        /// <param name="message">Human-readable diagnostic information.</param>
        /// <param name="context">Optional Unity object that can be selected from the Console.</param>
        public static void Warning(string subsystem, string message, Object context = null)
        {
            Debug.LogWarning(Format(subsystem, message), context);
        }

        /// <summary>
        /// Records a condition that prevents the requested operation from continuing.
        /// </summary>
        /// <param name="subsystem">Short owner name, such as Bootstrap or Input.</param>
        /// <param name="message">Human-readable diagnostic information.</param>
        /// <param name="context">Optional Unity object that can be selected from the Console.</param>
        public static void Error(string subsystem, string message, Object context = null)
        {
            Debug.LogError(Format(subsystem, message), context);
        }

        /// <summary>
        /// Creates the canonical searchable message prefix.
        /// </summary>
        private static string Format(string subsystem, string message)
        {
            string safeSubsystem = string.IsNullOrWhiteSpace(subsystem) ? "Unknown" : subsystem;
            return $"[{ProductPrefix}][{safeSubsystem}] {message}";
        }
    }
}
