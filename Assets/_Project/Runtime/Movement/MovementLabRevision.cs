using UnityEngine;

namespace SphereCorridor.Movement
{
    /// <summary>
    /// Records which automatically generated movement-lab layout is present.
    /// Editor generators read this marker to avoid rebuilding an already-current
    /// scene every time Unity recompiles scripts.
    /// </summary>
    public sealed class MovementLabRevision : MonoBehaviour
    {
        [SerializeField, Min(1)] private int revision = 1;

        /// <summary>Gets the generated scene-layout revision.</summary>
        public int Revision => revision;

        /// <summary>
        /// Assigns the generated layout revision before the scene is saved.
        /// </summary>
        public void Configure(int generatedRevision)
        {
            revision = generatedRevision;
        }
    }
}
