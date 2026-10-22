using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Adventure.AI
{
    // Coalesces edits from every zone and serializes updates per surface.
    internal static class NavMeshZoneBaker
    {
        private sealed class Work
        {
            public NavMeshSurface surface;
            public bool dirty;
            public double due;
            public AsyncOperation operation;
            public NavMeshData original;
            public NavMeshData runtimeCopy;
        }
        private static readonly Dictionary<NavMeshSurface, Work> WorkBySurface = new Dictionary<NavMeshSurface, Work>();
        private static readonly List<NavMeshSurface> Removed = new List<NavMeshSurface>();
        public static bool IsTransitioning { get; private set; }
        public static int CompletedBuildCount { get; private set; }
        public static int PendingCount
        {
            get
            {
                int count = 0;
                foreach (Work work in WorkBySurface.Values)
                    if (work.dirty || work.operation != null) count++;
                return count;
            }
        }
        private static double Now
        {
            get
            {
#if UNITY_EDITOR
                return EditorApplication.timeSinceStartup;
#else
                return Time.realtimeSinceStartupAsDouble;
#endif
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void StartRuntime()
        {
            IsTransitioning = false;
            Install();
            foreach (NavMeshNoWalkZone zone in Object.FindObjectsByType<NavMeshNoWalkZone>(FindObjectsSortMode.None))
                zone.RebuildNow();
        }

        internal static void Install()
        {
            NavMesh.onPreUpdate -= RuntimeTick;
            NavMesh.onPreUpdate += RuntimeTick;
#if UNITY_EDITOR
            EditorApplication.update -= EditorTick;
            EditorApplication.update += EditorTick;
            EditorApplication.playModeStateChanged -= PlayModeChanged;
            EditorApplication.playModeStateChanged += PlayModeChanged;
#endif
        }

        internal static void Request(Scene scene, Bounds changedBounds, float delay)
        {
            if (IsTransitioning || !scene.IsValid() || !scene.isLoaded) return;
            Install();
            foreach (NavMeshSurface surface in Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None))
            {
                if (!surface.isActiveAndEnabled || surface.gameObject.scene != scene || surface.navMeshData == null) continue;
                Bounds local = surface.navMeshData.sourceBounds;
                Bounds world = new Bounds(surface.transform.position + surface.transform.rotation * local.center, Vector3.zero);
                for (int i = 0; i < 8; i++)
                {
                    Vector3 v = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    world.Encapsulate(surface.transform.position + surface.transform.rotation * v);
                }
                world.Expand(surface.GetBuildSettings().agentRadius * 2f + .5f);
                if (!world.Intersects(changedBounds)) continue;
                Work work;
                if (!WorkBySurface.TryGetValue(surface, out work))
                {
                    work = new Work { surface = surface };
                    WorkBySurface.Add(surface, work);
                }
                work.dirty = true;
                work.due = Now + delay;
            }
        }

        private static void RuntimeTick()
        {
            if (Application.isPlaying) Pump();
        }

#if UNITY_EDITOR
        private static void EditorTick()
        {
            if (Application.isPlaying || IsTransitioning || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            for (int i = NavMeshNoWalkZone.Zones.Count - 1; i >= 0; i--)
                if (NavMeshNoWalkZone.Zones[i] != null) NavMeshNoWalkZone.Zones[i].PollChanges();
            Pump();
        }

        private static void PlayModeChanged(PlayModeStateChange state)
        {
            IsTransitioning = state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode;
            if (IsTransitioning)
            {
                foreach (Work work in WorkBySurface.Values)
                {
                    if (work.operation != null && work.surface != null && work.surface.navMeshData != null)
                        NavMeshBuilder.Cancel(work.surface.navMeshData);
                    if (work.runtimeCopy != null)
                    {
                        if (work.surface != null)
                        {
                            work.surface.RemoveData();
                            work.surface.navMeshData = work.original;
                            if (work.surface.isActiveAndEnabled) work.surface.AddData();
                        }
                        Object.DestroyImmediate(work.runtimeCopy);
                    }
                }
                WorkBySurface.Clear();
            }
            else
            {
                foreach (NavMeshNoWalkZone zone in Object.FindObjectsByType<NavMeshNoWalkZone>(FindObjectsSortMode.None))
                    if (zone.isActiveAndEnabled) zone.RebuildNow();
            }
        }
#endif

        private static void Pump()
        {
            if (IsTransitioning) return;
            Removed.Clear();
            foreach (KeyValuePair<NavMeshSurface, Work> entry in WorkBySurface)
            {
                Work work = entry.Value;
                NavMeshSurface surface = work.surface;
                if (surface == null)
                {
                    if (work.runtimeCopy != null)
                    {
                        NavMeshBuilder.Cancel(work.runtimeCopy);
                        Object.Destroy(work.runtimeCopy);
                    }
                    Removed.Add(entry.Key);
                    continue;
                }
                if (work.operation != null)
                {
                    if (!work.operation.isDone) continue;
                    work.operation = null;
                    CompletedBuildCount++;
#if UNITY_EDITOR
                    if (!Application.isPlaying && surface.navMeshData != null)
                    {
                        EditorUtility.SetDirty(surface.navMeshData);
                        AssetDatabase.SaveAssetIfDirty(surface.navMeshData);
                        SceneView.RepaintAll();
                    }
#endif
                }
                if (!work.dirty || Now < work.due || !surface.isActiveAndEnabled) continue;
#if UNITY_EDITOR
                if (!Application.isPlaying && Unity.AI.Navigation.Editor.NavMeshAssetManager.instance.IsSurfaceBaking(surface)) continue;
#endif
                if (surface.navMeshData == null) { work.dirty = false; continue; }
                if (Application.isPlaying && work.runtimeCopy == null)
                {
                    work.original = surface.navMeshData;
                    work.runtimeCopy = Object.Instantiate(work.original);
                    work.runtimeCopy.name = work.original.name + " (Runtime Zones)";
                    surface.RemoveData();
                    surface.navMeshData = work.runtimeCopy;
                    surface.AddData();
                }
                work.dirty = false;
                work.operation = surface.UpdateNavMesh(surface.navMeshData);
            }
            foreach (NavMeshSurface key in Removed) WorkBySurface.Remove(key);
        }
    }
}
