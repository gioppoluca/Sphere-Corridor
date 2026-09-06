using UnityEngine;

namespace SphereCorridor.Combat
{
    /// <summary>
    /// Strategy asset invoked after death. A health definition can combine several
    /// effects, such as explosion plus bonus release, without a growing switch block.
    /// </summary>
    public abstract class DeathEffectDefinition : ScriptableObject
    {
        /// <summary>Executes one authored response to an idempotent death.</summary>
        public abstract void Execute(in DeathContext context);
    }
}
