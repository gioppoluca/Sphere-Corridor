using System;
using SphereCorridor.Corridor.Grid;
using UnityEngine;

namespace SphereCorridor.Corridor
{
    /// <summary>
    /// Describes one end of a corridor segment. Matching profiles prevent visible
    /// gaps and illegal transitions before runtime geometry is instantiated.
    /// </summary>
    [Serializable]
    public sealed class CorridorConnectorProfile
    {
        [SerializeField] private string profileId = "standard";
        [SerializeField, Min(0.1f)] private float width = 12f;
        [SerializeField] private float floorElevation;
        [SerializeField] private string[] permittedTransitionClasses = { "standard" };

        public string ProfileId => profileId;
        public float Width => width;
        public float FloorElevation => floorElevation;
        public string[] PermittedTransitionClasses => permittedTransitionClasses;

    }

    /// <summary>
    /// Keeps pressure dimensions independent so a generator does not collapse very
    /// different experiences into one misleading difficulty number.
    /// </summary>
    [Serializable]
    public struct SegmentChallengeVector
    {
        [SerializeField, Min(0f)] private float movement;
        [SerializeField, Min(0f)] private float combat;
        [SerializeField, Min(0f)] private float speed;
        [SerializeField, Min(0f)] private float environment;
        [SerializeField, Min(0f)] private float resource;

        public float Movement => movement;
        public float Combat => combat;
        public float Speed => speed;
        public float Environment => environment;
        public float Resource => resource;
    }

    /// <summary>
    /// Defines a typed local spawn point without coupling the generator to a prefab
    /// hierarchy name. Clearance and safety values are authoring-validation data.
    /// </summary>
    [Serializable]
    public struct CorridorSocketDescriptor
    {
        [SerializeField] private string socketId;
        [SerializeField] private string socketType;
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Vector3 localEulerAngles;
        [SerializeField, Min(0f)] private float clearanceRadius;
        [SerializeField, Min(0f)] private float connectorSafetyDistance;

        public string SocketId => socketId;
        public string SocketType => socketType;
        public Vector3 LocalPosition => localPosition;
        public Quaternion LocalRotation => Quaternion.Euler(localEulerAngles);
        public float ClearanceRadius => clearanceRadius;
        public float ConnectorSafetyDistance => connectorSafetyDistance;
    }

    /// <summary>
    /// Immutable authored metadata for one segment type. Runtime instances refer to
    /// this asset but never write run state back into it.
    /// </summary>
    [CreateAssetMenu(fileName = "CorridorSegment", menuName = "Sphere Corridor/Corridor Segment Definition")]
    public sealed class CorridorSegmentDefinition : ScriptableObject
    {
        [Header("Identity and geometry")]
        [SerializeField] private string segmentId = "movement_training";
        [SerializeField, Min(1f)] private float length = 40f;
        [SerializeField] private GameObject prefab;
        [SerializeField] private SegmentBlueprintDefinition blueprint;

        [Header("Connection")]
        [SerializeField] private CorridorConnectorProfile entryProfile = new CorridorConnectorProfile();
        [SerializeField] private CorridorConnectorProfile exitProfile = new CorridorConnectorProfile();

        [Header("Generation vocabulary")]
        [SerializeField] private SegmentChallengeVector challenge;
        [SerializeField] private string[] tags = { "movement", "combat_training" };
        [SerializeField] private CorridorSocketDescriptor[] sockets = Array.Empty<CorridorSocketDescriptor>();

        [Header("Eligibility")]
        [SerializeField, Min(0)] private int minimumTier;
        [SerializeField, Min(0)] private int maximumTier = 3;
        [SerializeField, Min(0.01f)] private float selectionWeight = 1f;
        [SerializeField] private string[] requiredUnlocks = Array.Empty<string>();

        [Header("Usage rules")]
        [SerializeField, Min(0)] private int minimumAppearances;
        [SerializeField, Min(0), Tooltip("Zero means no authored maximum.")]
        private int maximumAppearances;
        [SerializeField, Min(0)] private int cooldownSegments;
        [SerializeField] private string[] allowedFollowingSegmentIds = Array.Empty<string>();
        [SerializeField] private string[] prohibitedFollowingSegmentIds = Array.Empty<string>();
        [SerializeField] private string[] adjacencyExclusions = Array.Empty<string>();

        public string SegmentId => segmentId;
        public float Length => length;
        public GameObject Prefab => prefab;
        public SegmentBlueprintDefinition Blueprint => blueprint;
        public CorridorConnectorProfile EntryProfile => entryProfile;
        public CorridorConnectorProfile ExitProfile => exitProfile;
        public SegmentChallengeVector Challenge => challenge;
        public string[] Tags => tags;
        public CorridorSocketDescriptor[] Sockets => sockets;
        public int MinimumTier => minimumTier;
        public int MaximumTier => maximumTier;
        public float SelectionWeight => selectionWeight;
        public string[] RequiredUnlocks => requiredUnlocks;
        public int MinimumAppearances => minimumAppearances;
        public int MaximumAppearances => maximumAppearances;
        public int CooldownSegments => cooldownSegments;
        public string[] AllowedFollowingSegmentIds => allowedFollowingSegmentIds;
        public string[] ProhibitedFollowingSegmentIds => prohibitedFollowingSegmentIds;
        public string[] AdjacencyExclusions => adjacencyExclusions;

        /// <summary>
        /// Assigns the identity, physical length, and visual prefab used by the
        /// corridor planner. Connector and selection settings remain authored data.
        /// </summary>
        public void Configure(string id, float segmentLength, GameObject segmentPrefab)
        {
            segmentId = id;
            length = segmentLength;
            prefab = segmentPrefab;
        }

        /// <summary>
        /// Connects this planner-facing definition to the recipe that builds its
        /// floor, walls, and content. Length comes from the grid so the streamer and
        /// assembler cannot disagree about where the next segment must begin.
        /// </summary>
        public void ConfigureBlueprint(SegmentBlueprintDefinition segmentBlueprint, GameObject segmentPrefab)
        {
            blueprint = segmentBlueprint;
            prefab = segmentPrefab;
            if (blueprint != null)
            {
                length = blueprint.LengthMeters;
            }
        }

        /// <summary>
        /// Rejects incomplete authoring before the planner or streamer uses this
        /// asset. Failing here produces one useful setup error instead of a later
        /// null reference while the game is running.
        /// </summary>
        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(segmentId))
            {
                reason = "Segment ID is empty.";
                return false;
            }

            if (length <= 0f || prefab == null)
            {
                reason = "Segment length must be positive and a prefab must be assigned.";
                return false;
            }

            if (entryProfile == null || exitProfile == null ||
                string.IsNullOrWhiteSpace(entryProfile.ProfileId) ||
                string.IsNullOrWhiteSpace(exitProfile.ProfileId))
            {
                reason = "Entry and exit connector profiles require stable IDs.";
                return false;
            }

            if (minimumTier > maximumTier || selectionWeight <= 0f)
            {
                reason = "Tier bounds or selection weight are invalid.";
                return false;
            }


            if (blueprint != null && !blueprint.IsValid(out reason))
            {
                reason = $"Grid blueprint is invalid: {reason}";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>
        /// Keeps dependent Inspector values legal as they are edited. This is a
        /// current-data constraint, not a conversion of old project structures.
        /// </summary>
        private void OnValidate()
        {
            maximumTier = Mathf.Max(maximumTier, minimumTier);
            selectionWeight = Mathf.Max(0.01f, selectionWeight);
        }
    }
}
