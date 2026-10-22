using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Adventure.AI
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(NavMeshLink))]
    public sealed class NavMeshZoneLinkGuard : MonoBehaviour
    {
        [Tooltip("Desired link state when no forbidden zone covers this link.")]
        [SerializeField] private bool requestedActive = true;
        [SerializeField, HideInInspector] private bool initialized;
        private NavMeshLink link;

        private void OnEnable()
        {
            link = GetComponent<NavMeshLink>();
            if (!initialized)
            {
                requestedActive = link.activated;
                initialized = true;
            }
            Refresh();
        }

        private void Update() { Refresh(); }

        private void Refresh()
        {
            if (link == null) return;
            bool active = requestedActive && !IsBlocked(link);
            if (link.activated != active) link.activated = active;
        }

        private void OnDisable()
        {
            if (link != null && initialized) link.activated = requestedActive;
        }

        private static bool IsBlocked(NavMeshLink candidate)
        {
            Vector3 start = candidate.startTransform != null ? candidate.startTransform.position :
                candidate.transform.position + candidate.transform.rotation * candidate.startPoint;
            Vector3 end = candidate.endTransform != null ? candidate.endTransform.position :
                candidate.transform.position + candidate.transform.rotation * candidate.endPoint;
            foreach (NavMeshNoWalkZone zone in NavMeshNoWalkZone.Zones)
                if (zone != null && zone.gameObject.scene == candidate.gameObject.scene
                    && zone.IntersectsLink(start, end, candidate.width)) return true;
            return false;
        }

        internal static void RefreshSceneLinks(Scene scene)
        {
            if (NavMeshZoneBaker.IsTransitioning) return;
            foreach (NavMeshLink candidate in Object.FindObjectsByType<NavMeshLink>(FindObjectsSortMode.None))
            {
                if (candidate.gameObject.scene != scene) continue;
                NavMeshZoneLinkGuard guard = candidate.GetComponent<NavMeshZoneLinkGuard>();
                if (guard == null && IsBlocked(candidate))
                {
                    guard = candidate.gameObject.AddComponent<NavMeshZoneLinkGuard>();
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        UnityEditor.EditorUtility.SetDirty(guard);
#endif
                }
                if (guard != null && guard.isActiveAndEnabled) guard.Refresh();
            }
        }
    }
}
