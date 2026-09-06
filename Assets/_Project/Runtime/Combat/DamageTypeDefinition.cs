using UnityEngine;

namespace SphereCorridor.Combat
{
    /// <summary>
    /// Gives a damage family a stable data identity. Adding ion, kinetic, or thermal
    /// damage therefore creates an asset rather than changing an enum and recompiling.
    /// </summary>
    [CreateAssetMenu(fileName = "DamageType", menuName = "Sphere Corridor/Combat/Damage Type")]
    public sealed class DamageTypeDefinition : ScriptableObject
    {
        [SerializeField] private string stableId = "plasma";
        [SerializeField] private string displayName = "Plasma";

        public string StableId => stableId;
        public string DisplayName => displayName;

        /// <summary>Assigns generated laboratory data while runtime consumers stay read-only.</summary>
        public void Configure(string id, string label)
        {
            stableId = id;
            displayName = label;
        }

        /// <summary>Checks the stable identity required by saves, logging, and matching.</summary>
        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(stableId) || string.IsNullOrWhiteSpace(displayName))
            {
                reason = "Damage type ID and display name are required.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
