using UnityEngine;
using UnityEngine.InputSystem;

namespace Adventure.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        private InputActionAsset runtimeActions;
        private InputAction move, jump, sprint, look, releaseCursor, captureCursor;
        public Vector2 Move => move != null && move.enabled ? move.ReadValue<Vector2>() : Vector2.zero;
        public bool JumpPressed => jump != null && jump.enabled && jump.WasPressedThisFrame();
        public bool SprintPressed => sprint != null && sprint.enabled && sprint.WasPressedThisFrame();
        public Vector2 Look => look != null && look.enabled ? look.ReadValue<Vector2>() : Vector2.zero;
        public bool ReleaseCursorPressed => releaseCursor != null && releaseCursor.enabled && releaseCursor.WasPressedThisFrame();
        public bool CaptureCursorPressed => captureCursor != null && captureCursor.enabled && captureCursor.WasPressedThisFrame();

        private void Awake()
        {
            if (actions == null) { Debug.LogError("Assign PlayerControls to PlayerInputReader.", this); enabled = false; return; }
            // Each player owns its action state; disabling one never disables the asset for others.
            runtimeActions = Instantiate(actions);
            move = runtimeActions.FindAction("Gameplay/Move", true);
            jump = runtimeActions.FindAction("Gameplay/Jump", true);
            sprint = runtimeActions.FindAction("Gameplay/Sprint", true);
            look = runtimeActions.FindAction("Gameplay/Look", true);
            releaseCursor = runtimeActions.FindAction("Gameplay/ReleaseCursor", true);
            captureCursor = runtimeActions.FindAction("Gameplay/CaptureCursor", true);
        }
        private void OnEnable() { if (runtimeActions != null) runtimeActions.Enable(); }
        private void OnDisable() { if (runtimeActions != null) runtimeActions.Disable(); }
        private void OnDestroy() { if (runtimeActions != null) Destroy(runtimeActions); }
    }
}
