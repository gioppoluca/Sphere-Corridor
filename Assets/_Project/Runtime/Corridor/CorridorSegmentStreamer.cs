using System.Collections.Generic;
using SphereCorridor.CameraSystem;
using SphereCorridor.Foundation;
using UnityEngine;

namespace SphereCorridor.Corridor
{
    /// <summary>
    /// Maintains three live corridor instances around the camera. A retired rear
    /// instance is destroyed and a newly selected prefab type is instantiated ahead.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public sealed class CorridorSegmentStreamer : MonoBehaviour
    {
        private const string LogSubsystem = "SegmentStream";

        [SerializeField] private Transform player;
        [SerializeField] private OverheadFollowCamera followCamera;
        [SerializeField] private Transform instanceRoot;
        [SerializeField] private CorridorSegmentDefinition[] availableDefinitions;
        [SerializeField, Min(3)] private int liveSegmentCount = 3;
        [SerializeField, Min(0f)] private float retirementSafetyMargin = 2f;

        private readonly List<CorridorSegmentInstance> activeSegments = new List<CorridorSegmentInstance>();
        private int nextDefinitionIndex;
        private int nextSequenceNumber;
        private bool configurationValid;

        /// <summary>Gets the number of live segment instances tracked by the streamer.</summary>
        public int ActiveSegmentCount => activeSegments.Count;

        /// <summary>
        /// Gets the identifier of the segment currently containing the player.
        /// </summary>
        public string CurrentSegmentId
        {
            get
            {
                if (player == null)
                {
                    return "unconfigured";
                }

                for (int index = 0; index < activeSegments.Count; index++)
                {
                    CorridorSegmentInstance segment = activeSegments[index];
                    if (player.position.x >= segment.StartX && player.position.x < segment.EndX)
                    {
                        return segment.Definition.SegmentId;
                    }
                }

                return "outside";
            }
        }

        /// <summary>
        /// Assigns the generated scene references and the available data-driven types.
        /// </summary>
        public void Configure(
            Transform playerTransform,
            OverheadFollowCamera cameraController,
            Transform segmentsParent,
            CorridorSegmentDefinition[] definitions)
        {
            player = playerTransform;
            followCamera = cameraController;
            instanceRoot = segmentsParent;
            availableDefinitions = definitions;
        }

        /// <summary>
        /// Adopts the three scene-preview instances or creates them if none exist.
        /// </summary>
        private void Start()
        {
            configurationValid = ValidateConfiguration();
            if (!configurationValid)
            {
                enabled = false;
                return;
            }

            activeSegments.AddRange(instanceRoot.GetComponentsInChildren<CorridorSegmentInstance>());
            activeSegments.Sort((left, right) => left.StartX.CompareTo(right.StartX));

            if (activeSegments.Count == 0)
            {
                CreateInitialSegments();
            }

            nextSequenceNumber = activeSegments.Count;
            nextDefinitionIndex = activeSegments.Count % availableDefinitions.Length;
            AppLog.Info(LogSubsystem, $"Segment streamer initialized with {activeSegments.Count} live instances.", this);
        }

        /// <summary>
        /// Retires geometry only after the camera has updated its measured rear boundary.
        /// </summary>
        private void LateUpdate()
        {
            if (!configurationValid || !followCamera.HasRearBoundary)
            {
                return;
            }

            while (activeSegments.Count > 0 &&
                   CorridorSegmentMath.IsReadyToRetire(
                       activeSegments[0].EndX,
                       followCamera.RearBoundaryX,
                       retirementSafetyMargin))
            {
                RetireRearAndCreateNext();
            }
        }

        /// <summary>
        /// Removes the old instance and asks the isolated selector for a new type.
        /// </summary>
        private void RetireRearAndCreateNext()
        {
            CorridorSegmentInstance retired = activeSegments[0];
            activeSegments.RemoveAt(0);

            float farthestEndX = activeSegments.Count == 0
                ? retired.EndX
                : activeSegments[activeSegments.Count - 1].EndX;
            CorridorSegmentDefinition previousDefinition = activeSegments.Count == 0
                ? retired.Definition
                : activeSegments[activeSegments.Count - 1].Definition;
            CorridorSegmentDefinition nextDefinition = SelectNextDefinition(previousDefinition);
            if (nextDefinition == null)
            {
                // Restore ownership of the still-live rear instance and halt cleanly.
                // Continuing would either leave a gap or violate authored grammar.
                activeSegments.Insert(0, retired);
                enabled = false;
                return;
            }
            CorridorSegmentInstance replacement = InstantiateSegment(
                nextDefinition,
                CorridorSegmentMath.CalculateNextCenter(farthestEndX, nextDefinition.Length),
                nextSequenceNumber++);
            activeSegments.Add(replacement);

            AppLog.Info(
                LogSubsystem,
                $"Retired segment {retired.SequenceNumber:000}; created {replacement.SequenceNumber:000} " +
                $"from type '{nextDefinition.SegmentId}'.",
                replacement);
            Destroy(retired.gameObject);
        }

        /// <summary>
        /// Uses round-robin selection for the lab. M4 can replace this method's policy
        /// with the seeded level planner without changing instance lifecycle code.
        /// </summary>
        private CorridorSegmentDefinition SelectNextDefinition(CorridorSegmentDefinition previous = null)
        {
            for (int offset = 0; offset < availableDefinitions.Length; offset++)
            {
                int candidateIndex = (nextDefinitionIndex + offset) % availableDefinitions.Length;
                CorridorSegmentDefinition candidate = availableDefinitions[candidateIndex];
                if (previous == null || CorridorSegmentCompatibility.CanFollow(previous, candidate, out _))
                {
                    nextDefinitionIndex = (candidateIndex + 1) % availableDefinitions.Length;
                    return candidate;
                }
            }

            // Failing loudly is safer than silently hiding an invalid content graph.
            AppLog.Error(LogSubsystem, $"No legal successor exists after '{previous?.SegmentId}'.", this);
            return null;
        }

        /// <summary>
        /// Creates three contiguous instances centered around the initial player position.
        /// </summary>
        private void CreateInitialSegments()
        {
            float nextStartX = player.position.x - availableDefinitions[0].Length * 1.5f;
            for (int index = 0; index < liveSegmentCount; index++)
            {
                CorridorSegmentDefinition previous = activeSegments.Count == 0
                    ? null
                    : activeSegments[activeSegments.Count - 1].Definition;
                CorridorSegmentDefinition definition = SelectNextDefinition(previous);
                if (definition == null)
                {
                    return;
                }
                float centerX = nextStartX + definition.Length * 0.5f;
                CorridorSegmentInstance instance = InstantiateSegment(definition, centerX, index);
                activeSegments.Add(instance);
                nextStartX += definition.Length;
            }
        }

        /// <summary>
        /// Creates a fresh runtime object from the chosen type-specific prefab.
        /// </summary>
        private CorridorSegmentInstance InstantiateSegment(
            CorridorSegmentDefinition definition,
            float centerX,
            int sequenceNumber)
        {
            GameObject instanceObject = Instantiate(
                definition.Prefab,
                new Vector3(centerX, 0f, 0f),
                Quaternion.identity,
                instanceRoot);
            CorridorSegmentInstance instance = instanceObject.GetComponent<CorridorSegmentInstance>();
            instance.Configure(definition, sequenceNumber);
            return instance;
        }

        /// <summary>
        /// Reports incomplete setup before any runtime creation or destruction occurs.
        /// </summary>
        private bool ValidateConfiguration()
        {
            if (player == null || followCamera == null || instanceRoot == null ||
                availableDefinitions == null || availableDefinitions.Length == 0)
            {
                AppLog.Error(LogSubsystem, "Segment streamer references are incomplete.", this);
                return false;
            }

            for (int index = 0; index < availableDefinitions.Length; index++)
            {
                CorridorSegmentDefinition definition = availableDefinitions[index];
                if (definition == null)
                {
                    AppLog.Error(LogSubsystem, $"Segment definition {index} is missing.", this);
                    return false;
                }

                if (!definition.IsValid(out string reason))
                {
                    AppLog.Error(LogSubsystem, $"Segment definition {index} is invalid: {reason}", this);
                    return false;
                }

                if (definition.Prefab.GetComponent<CorridorSegmentInstance>() == null)
                {
                    AppLog.Error(LogSubsystem, $"Prefab for '{definition.SegmentId}' lacks CorridorSegmentInstance.", definition);
                    return false;
                }

                bool hasLegalSuccessor = false;
                for (int candidateIndex = 0; candidateIndex < availableDefinitions.Length; candidateIndex++)
                {
                    if (CorridorSegmentCompatibility.CanFollow(
                            definition,
                            availableDefinitions[candidateIndex],
                            out _))
                    {
                        hasLegalSuccessor = true;
                        break;
                    }
                }

                if (!hasLegalSuccessor)
                {
                    AppLog.Error(
                        LogSubsystem,
                        $"Segment '{definition.SegmentId}' has no legal successor in this catalog.",
                        definition);
                    return false;
                }
            }

            return true;
        }
    }
}
