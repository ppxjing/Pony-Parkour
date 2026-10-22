using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Adventure.AI
{
    [ExecuteAlways, DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshModifierVolume))]
    public sealed class NavMeshNoWalkZone : MonoBehaviour
    {
        [Tooltip("Seconds to combine repeated transform edits before updating affected NavMeshes.")]
        [SerializeField, Min(0.05f)] private float rebuildDelay = 0.35f;
        private NavMeshModifierVolume volume;
        private Matrix4x4 lastMatrix;
        private Vector3 lastSize;
        private Vector3 lastCenter;
        private Bounds lastBounds;
        private bool registered;
        internal static readonly List<NavMeshNoWalkZone> Zones = new List<NavMeshNoWalkZone>();

        internal bool IsSceneZone
        {
            get
            {
                if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return false;
#if UNITY_EDITOR
                if (EditorUtility.IsPersistent(this) || PrefabStageUtility.GetPrefabStage(gameObject) != null) return false;
#endif
                return true;
            }
        }

        private void Reset()
        {
            volume = GetComponent<NavMeshModifierVolume>();
            volume.center = Vector3.zero;
            volume.size = Vector3.one;
            volume.area = 1;
        }

        private void OnEnable()
        {
            volume = GetComponent<NavMeshModifierVolume>();
            volume.area = 1;
            volume.enabled = true;
            if (!IsSceneZone) return;
            if (!Zones.Contains(this)) Zones.Add(this);
            registered = true;
            Capture();
            NavMeshZoneBaker.Install();
            Request(lastBounds);
        }

        private void Update() { PollChanges(); }

        internal void PollChanges()
        {
            if (!registered || volume == null || !IsSceneZone) return;
            if (transform.localToWorldMatrix == lastMatrix && volume.size == lastSize
                && volume.center == lastCenter && volume.area == 1 && volume.enabled) return;
            Bounds old = lastBounds;
            volume.area = 1;
            volume.enabled = true;
            Capture();
            Request(old);
            Request(lastBounds);
        }

        private void Capture()
        {
            lastMatrix = transform.localToWorldMatrix;
            lastSize = volume.size;
            lastCenter = volume.center;
            lastBounds = WorldBounds;
        }

        private void Request(Bounds bounds)
        {
            NavMeshZoneBaker.Request(gameObject.scene, bounds, rebuildDelay);
            NavMeshZoneLinkGuard.RefreshSceneLinks(gameObject.scene);
        }

        [ContextMenu("Rebuild Affected NavMeshes")]
        public void RebuildNow()
        {
            if (IsSceneZone)
                NavMeshZoneBaker.Request(gameObject.scene, WorldBounds, 0f);
        }

        private void OnDisable()
        {
            if (!registered) return;
            registered = false;
            Zones.Remove(this);
            if (volume != null) volume.enabled = false;
            if (IsSceneZone) Request(lastBounds);
        }

        private Vector3 WorldSize
        {
            get
            {
                Vector3 s = Vector3.Scale(volume.size, transform.lossyScale);
                return new Vector3(Mathf.Max(.001f, Mathf.Abs(s.x)), Mathf.Max(.001f, Mathf.Abs(s.y)), Mathf.Max(.001f, Mathf.Abs(s.z)));
            }
        }

        internal Bounds WorldBounds
        {
            get
            {
                if (volume == null) volume = GetComponent<NavMeshModifierVolume>();
                Matrix4x4 m = Matrix4x4.TRS(transform.TransformPoint(volume.center), transform.rotation, WorldSize);
                Bounds b = new Bounds(m.MultiplyPoint3x4(Vector3.zero), Vector3.zero);
                for (int i = 0; i < 8; i++)
                    b.Encapsulate(m.MultiplyPoint3x4(new Vector3((i & 1) == 0 ? -.5f : .5f, (i & 2) == 0 ? -.5f : .5f, (i & 4) == 0 ? -.5f : .5f)));
                return b;
            }
        }

        internal bool IntersectsLink(Vector3 start, Vector3 end, float width)
        {
            if (!isActiveAndEnabled || !IsSceneZone || volume == null) return false;
            Vector3 size = WorldSize;
            Matrix4x4 inverse = Matrix4x4.TRS(transform.TransformPoint(volume.center), transform.rotation, size).inverse;
            Vector3 a = inverse.MultiplyPoint3x4(start);
            Vector3 delta = inverse.MultiplyPoint3x4(end) - a;
            float lo = 0f, hi = 1f;
            for (int axis = 0; axis < 3; axis++)
            {
                float extent = .5f + width * .5f / size[axis];
                if (Mathf.Abs(delta[axis]) < .00001f)
                {
                    if (Mathf.Abs(a[axis]) > extent) return false;
                    continue;
                }
                float p = (-extent - a[axis]) / delta[axis];
                float q = (extent - a[axis]) / delta[axis];
                lo = Mathf.Max(lo, Mathf.Min(p, q));
                hi = Mathf.Min(hi, Mathf.Max(p, q));
                if (lo > hi) return false;
            }
            return true;
        }

        private void OnDrawGizmos()
        {
            if (Application.IsPlaying(gameObject)) return;
            if (volume == null) volume = GetComponent<NavMeshModifierVolume>();
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Color oldColor = Gizmos.color;
            Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(volume.center), transform.rotation, WorldSize);
            Gizmos.color = new Color(.6f, .15f, 1f, .18f);
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
            Gizmos.color = new Color(.6f, .15f, 1f, .95f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = oldMatrix;
            Gizmos.color = oldColor;
        }
    }
}
