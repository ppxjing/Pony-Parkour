using UnityEngine;
using UnityEngine.InputSystem;

namespace Adventure.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        private InputActionAsset runtimeActions;
        private InputAction move, jump, look, orbit;
        public Vector2 Move => move != null && move.enabled ? move.ReadValue<Vector2>() : Vector2.zero;
        public bool JumpPressed => jump != null && jump.enabled && jump.WasPressedThisFrame();
        public Vector2 Look => look != null && look.enabled ? look.ReadValue<Vector2>() : Vector2.zero;
        public bool OrbitHeld => orbit != null && orbit.enabled && orbit.IsPressed();

        private void Awake()
        {
            if (actions == null) { Debug.LogError("Assign PlayerControls to PlayerInputReader.", this); enabled = false; return; }
            // Each player owns its action state; disabling one never disables the asset for others.
            runtimeActions = Instantiate(actions);
            move = runtimeActions.FindAction("Gameplay/Move", true);
            jump = runtimeActions.FindAction("Gameplay/Jump", true);
            look = runtimeActions.FindAction("Gameplay/Look", true);
            orbit = runtimeActions.FindAction("Gameplay/Orbit", true);
        }
        private void OnEnable() { if (runtimeActions != null) runtimeActions.Enable(); }
        private void OnDisable() { if (runtimeActions != null) runtimeActions.Disable(); }
        private void OnDestroy() { if (runtimeActions != null) Destroy(runtimeActions); }
    }
}
