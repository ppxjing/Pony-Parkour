using UnityEngine;

namespace Adventure.Player
{
    /// <summary>Excludes this hierarchy from camera obstruction only; gameplay collisions remain active.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Adventure/Camera/Ignore Camera Obstruction")]
    public sealed class CameraObstructionIgnore : MonoBehaviour
    {
    }
}