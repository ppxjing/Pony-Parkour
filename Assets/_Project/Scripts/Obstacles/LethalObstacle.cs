using Adventure.Player;
using UnityEngine;

namespace Adventure.Obstacles
{
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    public sealed class LethalObstacle : MonoBehaviour
    {
        [SerializeField] private SphereCollider solidSphere;
        [SerializeField] private SphereCollider contactTrigger;
        [Tooltip("Small detection margin to include CharacterController skin contact.")]
        [SerializeField, Min(0f)] private float contactPadding = 0.05f;

        private void Awake()
        {
            if (solidSphere == null || contactTrigger == null || !contactTrigger.isTrigger)
            {
                Debug.LogError("LethalObstacle needs solid sphere and child contact trigger references.", this);
                enabled = false;
            }
        }
        private void FixedUpdate()
        {
            // Runs after the bounce component so lethal contact follows inflation this physics step.
            contactTrigger.center = solidSphere.center;
            contactTrigger.radius = solidSphere.radius + contactPadding;
        }
        private void OnTriggerEnter(Collider other) { Touch(other); }
        private void OnTriggerStay(Collider other) { Touch(other); }
private void Touch(Collider other)
        {
            if (!isActiveAndEnabled) return;
            ThirdPersonMotor player = other.GetComponentInParent<ThirdPersonMotor>();
            if (player != null) player.RespawnFromObstacle();
        }
    }
}
