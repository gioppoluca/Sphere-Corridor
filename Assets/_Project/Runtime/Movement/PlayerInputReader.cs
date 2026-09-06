using SphereCorridor.Foundation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SphereCorridor.Movement
{
    /// <summary>
    /// Converts Input System actions into a small gameplay-facing state.
    /// The motor depends on intent, not on specific keys or controller devices.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        private const string LogSubsystem = "Input";

        [SerializeField] private InputActionAsset inputActions;

        private InputActionMap playerMap;
        private InputAction moveHorizontalAction;
        private InputAction moveLateralAction;
        private InputAction jumpAction;
        private InputAction fireAction;
        private bool jumpPressedQueued;
        private bool firePressedQueued;

        /// <summary>
        /// Gets normalized corridor input where -1 brakes/reverses and +1 accelerates.
        /// </summary>
        public float Horizontal { get; private set; }

        /// <summary>
        /// Gets normalized cross-corridor intent where -1 moves toward the near wall
        /// and +1 moves toward the far wall.
        /// </summary>
        public float Lateral { get; private set; }

        /// <summary>
        /// Gets whether jump remains held, which controls variable jump height.
        /// </summary>
        public bool JumpHeld { get; private set; }

        /// <summary>Gets whether the primary-fire control remains held.</summary>
        public bool FireHeld { get; private set; }

        /// <summary>
        /// Assigns the shared project input asset when the movement lab is generated.
        /// </summary>
        /// <param name="asset">The project-specific input action asset.</param>
        public void Configure(InputActionAsset asset)
        {
            inputActions = asset;
        }

        /// <summary>
        /// Resolves logical actions once so per-frame input does not perform string searches.
        /// </summary>
        private void Awake()
        {
            ResolveActions();
        }

        /// <summary>
        /// Enables only the Player map while this reader is active.
        /// </summary>
        private void OnEnable()
        {
            ResolveActions();
            playerMap?.Enable();
            AppLog.Development(LogSubsystem, "Player input map enabled.", this);
        }

        /// <summary>
        /// Disables the map and clears transient state when gameplay stops.
        /// </summary>
        private void OnDisable()
        {
            playerMap?.Disable();
            Horizontal = 0f;
            Lateral = 0f;
            JumpHeld = false;
            FireHeld = false;
            jumpPressedQueued = false;
            firePressedQueued = false;
            AppLog.Development(LogSubsystem, "Player input map disabled.", this);
        }

        /// <summary>
        /// Samples edge-sensitive input in Update so a press cannot disappear between physics ticks.
        /// </summary>
        private void Update()
        {
            if (moveHorizontalAction == null || moveLateralAction == null ||
                jumpAction == null || fireAction == null)
            {
                return;
            }

            Horizontal = moveHorizontalAction.ReadValue<float>();
            Lateral = moveLateralAction.ReadValue<float>();
            JumpHeld = jumpAction.IsPressed();
            FireHeld = fireAction.IsPressed();

            if (jumpAction.WasPressedThisFrame())
            {
                jumpPressedQueued = true;
                AppLog.Development(LogSubsystem, "Jump press queued for the physics motor.", this);
            }


            if (fireAction.WasPressedThisFrame())
            {
                firePressedQueued = true;
                AppLog.Development(LogSubsystem, "Primary-fire press queued for the weapon.", this);
            }
        }

        /// <summary>
        /// Returns one queued jump press exactly once to the physics motor.
        /// </summary>
        /// <returns>True when an unconsumed jump press was available.</returns>
        public bool ConsumeJumpPressed()
        {
            if (!jumpPressedQueued)
            {
                return false;
            }

            jumpPressedQueued = false;
            return true;
        }


        /// <summary>
        /// Returns one queued primary-fire press exactly once. Single and burst weapons
        /// use this edge, while automatic weapons also inspect <see cref="FireHeld"/>.
        /// </summary>
        public bool ConsumeFirePressed()
        {
            if (!firePressedQueued)
            {
                return false;
            }

            firePressedQueued = false;
            return true;
        }

        /// <summary>
        /// Finds the required M1 actions and reports a clear setup error if the asset is incomplete.
        /// </summary>
        private void ResolveActions()
        {
            if (playerMap != null)
            {
                return;
            }

            if (inputActions == null)
            {
                AppLog.Error(LogSubsystem, "Input action asset is not assigned.", this);
                enabled = false;
                return;
            }

            playerMap = inputActions.FindActionMap("Player", throwIfNotFound: false);
            moveHorizontalAction = playerMap?.FindAction("MoveHorizontal", throwIfNotFound: false);
            moveLateralAction = playerMap?.FindAction("MoveLateral", throwIfNotFound: false);
            jumpAction = playerMap?.FindAction("Jump", throwIfNotFound: false);
            fireAction = playerMap?.FindAction("Fire", throwIfNotFound: false);

            if (playerMap == null || moveHorizontalAction == null || moveLateralAction == null ||
                jumpAction == null || fireAction == null)
            {
                AppLog.Error(
                    LogSubsystem,
                    "Player movement, lateral, jump, or fire actions are missing from the input asset.",
                    this);
                enabled = false;
            }
        }
    }
}
