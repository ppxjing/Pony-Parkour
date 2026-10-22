using System;
using System.Collections.Generic;
using System.IO;
using Adventure.Checkpoints;
using Adventure.Obstacles;
using Adventure.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class CheckpointSmokeTest
{
    private static PlayerRespawn player;
    private static PlayerDeath death;
    private static CharacterController controller;
    private static PlayerInputReader input;
    private static CheckpointRegistry registry;
    private static RespawnCheckpoint a, b, extra;
    private static Keyboard keyboard;
    private static Mouse mouse;
    private static Key[] keys = Array.Empty<Key>();
    private static readonly List<string> results = new List<string>();
    private static int phase, originalCount;
    private static float began, deathObserved, revivedAt, delay, protection, jumpPeak;
    private static Vector3 initialPosition, moveStart;
    private static Vector3 cameraMoveStart;
    private static float maximumVisualDrift;
    private static Quaternion viewStart;
    private static bool oldBackground, mouseSent;
    public static string Status { get; private set; } = "Not run";

    [MenuItem("Tools/Adventure/Run Checkpoint Respawn Test (Play Mode)")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying || Status == "Running") throw new InvalidOperationException("Use a fresh Play session.");
        registry = UnityEngine.Object.FindFirstObjectByType<CheckpointRegistry>();
        if (registry == null || registry.Checkpoints.Count < 2) throw new InvalidOperationException("Two registered checkpoints required.");
        player = registry.Player; death = player.GetComponent<PlayerDeath>();
        if (!death.IsAlive || player.CurrentCheckpoint != null || player.RespawnCount != 0) throw new InvalidOperationException("Restart Play before testing.");
        a = registry.Checkpoints[0]; b = registry.Checkpoints[1]; originalCount = registry.Checkpoints.Count;
        controller = player.GetComponent<CharacterController>(); input = player.GetComponent<PlayerInputReader>();
        var so = new SerializedObject(player); delay = so.FindProperty("respawnDelay").floatValue; protection = so.FindProperty("protectionTime").floatValue;
        initialPosition = player.SavedPosition; results.Clear(); phase = 0; began = Time.time;
        oldBackground = Application.runInBackground; Application.runInBackground = true;
        keyboard = InputSystem.AddDevice<Keyboard>("CheckpointTestKeyboard"); mouse = InputSystem.AddDevice<Mouse>("CheckpointTestMouse");
        keys = Array.Empty<Key>(); mouseSent = false; Status = "Running";
        InputSystem.onBeforeUpdate += Feed; EditorApplication.update += Tick; EditorApplication.playModeStateChanged += PlayChanged;
        var ball = UnityEngine.Object.FindFirstObjectByType<InflatingBounceBall>();
        Vector3 p = ball.transform.position; p.y = ball.GroundY + 0.05f; Place(p);
    }
    private static void Feed()
    {
        if (InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
        var state = new MouseState();
        if (phase == 16)
        {
            state = state.WithButton(MouseButton.Left);
            if (Time.time - began > 0.1f && !mouseSent) { state.delta = new Vector2(100f, 0f); mouseSent = true; }
        }
        InputSystem.QueueStateEvent(mouse, state);
    }
    private static void Place(Vector3 position)
    {
        bool enabled = controller.enabled; controller.enabled = false;
        player.transform.position = position; controller.enabled = enabled; Physics.SyncTransforms();
    }
    private static void Enter(RespawnCheckpoint checkpoint) { Place(checkpoint.SpawnPosition); }
    private static void Next() { phase++; began = Time.time; }
    private static void Check(bool success, string detail) { results.Add((success ? "PASS: " : "FAIL: ") + detail); }
    private static void VerifyRestored(Vector3 expected, string label)
    {
        Check(Vector3.Distance(player.transform.position, expected) < 0.15f, label + " position restored: " + player.transform.position.ToString("F3"));
        var body = player.transform.Find("Visuals").GetComponent<Rigidbody>();
        Check(body.isKinematic && !body.useGravity && !body.GetComponent<Collider>().enabled, label + " ragdoll deactivated");
        Check(body.transform.localPosition.sqrMagnitude < 0.0001f && Quaternion.Angle(body.transform.localRotation, Quaternion.identity) < 0.1f, label + " standing pose restored");
        Check(input.enabled && controller.enabled && player.GetComponent<ThirdPersonMotor>().enabled, label + " input and controller enabled");
        Physics.SyncTransforms();
        Vector3 center = player.transform.TransformPoint(controller.center);
        float halfSegment = controller.height * 0.5f - controller.radius;
        var overlaps = Physics.OverlapCapsule(center + Vector3.up * halfSegment, center - Vector3.up * halfSegment,
            controller.radius * 0.95f, ~0, QueryTriggerInteraction.Ignore);
        bool clear = Array.TrueForAll(overlaps, c => c.transform.IsChildOf(player.transform));
        Check(clear, label + " spawn capsule has no environment overlap");
        Check(Time.time - deathObserved >= delay - 0.1f, label + " waited configured delay: " + (Time.time - deathObserved).ToString("F2") + " seconds");
        revivedAt = Time.time;
    }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying || player == null) { Finish("Interrupted"); return; }
        if (phase >= 14)
            maximumVisualDrift = Mathf.Max(maximumVisualDrift, player.transform.Find("Visuals").localPosition.magnitude);
        if (Time.time - began > 12f + delay)
        { Check(false, "Timed out in phase " + phase); Finish("FAILED"); return; }
        switch (phase)
        {
            case 0:
                if (!death.IsDead) return;
                Check(!input.enabled && !controller.enabled, "Real ball contact starts death with inputs disabled");
                deathObserved = Time.time; Next(); break;
            case 1:
                if (death.IsDead || player.RespawnCount != 1) return;
                VerifyRestored(initialPosition, "No-checkpoint fallback");
                death.RequestDeath(player.transform.position);
                Check(death.IsAlive, "Respawn protection rejects immediate lethal contact");
                Enter(a); Next(); break;
            case 2:
                if (player.CurrentCheckpoint != a) return;
                Check(true, "Entering A records A"); Enter(b); Next(); break;
            case 3:
                if (player.CurrentCheckpoint != b || Time.time - revivedAt <= protection + 0.1f) return;
                Check(true, "Entering B after A replaces saved checkpoint with B");
                death.RequestDeath(player.transform.position + Vector3.forward); Next(); break;
            case 4:
                if (!death.IsDead) return;
                deathObserved = Time.time; Enter(a); Next(); break;
            case 5:
                if (death.IsDead)
                {
                    if (Time.time - began > 0.2f && player.CurrentCheckpoint != b)
                    { Check(false, "A corpse must not activate A"); Finish("FAILED"); }
                    return;
                }
                if (player.RespawnCount != 2) return;
                Check(player.CurrentCheckpoint == b, "Dead body touching A does not overwrite B");
                VerifyRestored(b.SpawnPosition, "Latest checkpoint B");
                Check(Quaternion.Angle(player.transform.rotation, b.SpawnRotation) < 0.1f, "SpawnPoint facing restored");
                Enter(a); Next(); break;
            case 6:
                if (player.CurrentCheckpoint != a || Time.time - revivedAt <= protection + 0.1f) return;
                Check(true, "Revisiting A makes A latest, independent of list order");
                death.RequestDeath(player.transform.position + Vector3.forward); Next(); break;
            case 7:
                if (!death.IsDead) return;
                deathObserved = Time.time; Next(); break;
            case 8:
                if (death.IsDead || player.RespawnCount != 3) return;
                VerifyRestored(a.SpawnPosition, "Revisited checkpoint A");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_prefab/复活存档点/PF_RespawnCheckpoint.prefab");
                var clone = UnityEngine.Object.Instantiate(prefab, new Vector3(-12f, -0.33f, -26f), Quaternion.identity);
                clone.name = "Temporary_Checkpoint_Test"; extra = clone.GetComponent<RespawnCheckpoint>();
                Enter(extra); Next(); break;
            case 9:
                if (Time.time - began < 0.25f) return;
                Check(player.CurrentCheckpoint == a, "Unregistered checkpoint is ignored");
                Enter(a);
                var registrySO = new SerializedObject(registry); var list = registrySO.FindProperty("checkpoints");
                list.arraySize = originalCount + 1; list.GetArrayElementAtIndex(originalCount).objectReferenceValue = extra;
                registrySO.ApplyModifiedPropertiesWithoutUndo(); Next(); break;
            case 10:
                if (Time.time - began < 0.2f) return;
                Enter(extra); Next(); break;
            case 11:
                if (player.CurrentCheckpoint != extra || Time.time - revivedAt <= protection + 0.1f) return;
                Check(true, "Appending a third checkpoint to List makes its trigger work");
                Place(new Vector3(extra.SpawnPosition.x, -20f, extra.SpawnPosition.z)); Next(); break;
            case 12:
                if (!death.IsDead) return;
                Check(true, "Falling below recovery height uses death/respawn flow"); deathObserved = Time.time; Next(); break;
            case 13:
                if (death.IsDead || player.RespawnCount != 4) return;
                VerifyRestored(extra.SpawnPosition, "Appended checkpoint after fall");
                moveStart = player.transform.position; cameraMoveStart = Camera.main.transform.position;
                maximumVisualDrift = 0f; keys = new[] { Key.W }; Next(); break;
            case 14:
                if (Time.time - began < 0.35f) return;
                Check(Vector3.ProjectOnPlane(player.transform.position - moveStart, Vector3.up).magnitude > 0.6f, "WASD moves player after repeated revives");
                Vector3 playerTravel = Vector3.ProjectOnPlane(player.transform.position - moveStart, Vector3.up);
                Vector3 cameraTravel = Vector3.ProjectOnPlane(Camera.main.transform.position - cameraMoveStart, Vector3.up);
                Check(cameraTravel.magnitude > playerTravel.magnitude * 0.5f && (cameraTravel - playerTravel).magnitude < 0.6f,
                    "Camera follows movement after respawn: player=" + playerTravel.magnitude.ToString("F3") + " m, camera=" + cameraTravel.magnitude.ToString("F3") + " m");
                keys = new[] { Key.Space }; moveStart = player.transform.position; jumpPeak = moveStart.y; Next(); break;
            case 15:
                jumpPeak = Mathf.Max(jumpPeak, player.transform.position.y);
                if (Time.time - began > 0.1f) keys = Array.Empty<Key>();
                if (Time.time - began < 0.65f) return;
                Check(jumpPeak - moveStart.y > 0.6f, "Jump works after revival; rise " + (jumpPeak - moveStart.y).ToString("F2") + " m");
                viewStart = Camera.main.transform.rotation; Next(); break;
            case 16:
                if (Time.time - began < 0.5f) return;
                Check(Quaternion.Angle(viewStart, Camera.main.transform.rotation) > 2f, "Mouse camera control restored");
                Check(maximumVisualDrift < 0.05f, "Capsule stays with controller during movement/jump after revival; max drift " + maximumVisualDrift.ToString("F4") + " m");
                Check(player.RespawnCount == 4, "Four consecutive recovery cycles completed once each");
                Finish(results.Exists(x => x.StartsWith("FAIL")) ? "FAILED" : "PASSED"); break;
        }
    }
    private static void PlayChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode && Status == "Running") Finish("Interrupted");
    }
    private static void Finish(string status)
    {
        InputSystem.onBeforeUpdate -= Feed; EditorApplication.update -= Tick; EditorApplication.playModeStateChanged -= PlayChanged;
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        keyboard = null; mouse = null; Application.runInBackground = oldBackground;
        if (registry != null && extra != null)
        {
            var so = new SerializedObject(registry); so.FindProperty("checkpoints").arraySize = originalCount; so.ApplyModifiedPropertiesWithoutUndo();
            UnityEngine.Object.Destroy(extra.gameObject);
        }
        Status = status;
        string report = "Checkpoint and respawn runtime validation: " + status + "\n" + string.Join("\n", results);
        Directory.CreateDirectory("Docs"); File.WriteAllText("Docs/CHECKPOINT_VALIDATION.txt", report); Debug.Log(report);
    }
}
