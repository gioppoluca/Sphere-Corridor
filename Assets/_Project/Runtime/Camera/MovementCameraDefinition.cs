using UnityEngine;

namespace SphereCorridor.CameraSystem
{
    /// <summary>
    /// Stores the elevated three-quarter camera composition requested for the corridor.
    /// It keeps both side walls readable while preserving visible jump height.
    /// </summary>
    [CreateAssetMenu(fileName = "MovementCameraDefinition", menuName = "Sphere Corridor/Movement Camera Definition")]
    public sealed class MovementCameraDefinition : ScriptableObject
    {
        [Header("Overhead composition")]
        [SerializeField, Min(1f)] private float height = 14f;
        [SerializeField, Min(1f)] private float depthDistance = 11f;
        [SerializeField] private float focusHeight = 0.4f;
        [SerializeField, Range(0f, 0.4f)] private float rearViewportMargin = 0.08f;

        [Header("Speed look-ahead")]
        [SerializeField, Min(0f)] private float minimumLookAhead = 3f;
        [SerializeField, Min(0f)] private float maximumLookAhead = 6f;
        [SerializeField, Min(0.01f)] private float speedForMaximumLookAhead = 11f;
        [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.14f;

        public float Height => height;
        public float DepthDistance => depthDistance;
        public float FocusHeight => focusHeight;
        public float RearViewportMargin => rearViewportMargin;
        public float MinimumLookAhead => minimumLookAhead;
        public float MaximumLookAhead => maximumLookAhead;
        public float SpeedForMaximumLookAhead => speedForMaximumLookAhead;
        public float PositionSmoothTime => positionSmoothTime;

        /// <summary>
        /// Prevents a maximum range smaller than the minimum range.
        /// </summary>
        private void OnValidate()
        {
            maximumLookAhead = Mathf.Max(maximumLookAhead, minimumLookAhead);
        }
    }
}
