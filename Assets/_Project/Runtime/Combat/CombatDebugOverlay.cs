using UnityEngine;

namespace SphereCorridor.Combat
{
    /// <summary>Displays stable combat state without flooding the Console every frame.</summary>
    public sealed class CombatDebugOverlay : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private PlayerWeaponController weapon;

        /// <summary>Assigns the generated player combat components.</summary>
        public void Configure(Health health, PlayerWeaponController weaponController)
        {
            playerHealth = health;
            weapon = weaponController;
        }

        /// <summary>Draws only in Editor and Development Builds.</summary>
        private void OnGUI()
        {
            if (!Debug.isDebugBuild || playerHealth == null || weapon == null)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(20f, 280f, 470f, 135f), GUI.skin.box);
            GUILayout.Label("COMBAT DIAGNOSTICS");
            GUILayout.Label($"Integrity: {playerHealth.CurrentHealth:F0}/{playerHealth.MaximumHealth:F0}");
            GUILayout.Label($"Weapon: {weapon.Definition?.StableId ?? "unavailable"}");
            GUILayout.Label($"Mode: {weapon.Definition?.FireMode.ToString() ?? "unavailable"}");
            GUILayout.Label($"Shots fired: {weapon.ShotsFired}");
            GUILayout.Label("Orange block: plasma immune   Magenta target: 3 plasma hits");
            GUILayout.EndArea();
        }
    }
}
