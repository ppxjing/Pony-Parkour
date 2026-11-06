using UnityEngine;

namespace Adventure.Obstacles
{
    // Retains the existing beach prefab script reference and serialized settings.
    [RequireComponent(typeof(SphereCollider))]
    public sealed class BeachBumperBall : CompressibleBumper { }
}
