using SphereCorridor.Foundation;
using UnityEngine;

namespace SphereCorridor.Combat
{
    /// <summary>Implements the M2 response; future response assets share the same contract.</summary>
    [CreateAssetMenu(fileName = "DestroyOnDeath", menuName = "Sphere Corridor/Combat/Death Effects/Destroy")]
    public sealed class DestroyOnDeathEffectDefinition : DeathEffectDefinition
    {
        private const string LogSubsystem = "Combat";

        /// <summary>Destroys only the dead runtime instance, never its definition asset.</summary>
        public override void Execute(in DeathContext context)
        {
            if (context.Target == null)
            {
                return;
            }

            AppLog.Info(LogSubsystem, $"Destroying dead target '{context.Target.name}'.", context.Target);
            Object.Destroy(context.Target);
        }
    }
}
