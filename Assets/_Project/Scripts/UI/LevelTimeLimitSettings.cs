using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Adventure/Level Time Limits", fileName = "LevelTimeLimits")]
public sealed class LevelTimeLimitSettings : ScriptableObject
{
    [Serializable]
    public sealed class LevelLimit
    {
        [Tooltip("Exact scene name, without .unity.")]
        public string sceneName;
        [Min(1f), Tooltip("Time available after the starting countdown, in seconds.")]
        public float durationSeconds = 180f;

        public LevelLimit(string sceneName) { this.sceneName = sceneName; }
    }

    [Min(1f), Tooltip("Used for scenes without an entry below.")]
    public float defaultDurationSeconds = 180f;
    public List<LevelLimit> levels = new List<LevelLimit>
    {
        new LevelLimit("Beach"),
        new LevelLimit("Kitchen_Adventure"),
        new LevelLimit("Bedroom")
    };

    public float GetDuration(string sceneName)
    {
        if (levels != null)
            foreach (LevelLimit level in levels)
                if (level != null && level.sceneName == sceneName)
                    return Mathf.Max(1f, level.durationSeconds);
        return Mathf.Max(1f, defaultDurationSeconds);
    }
}