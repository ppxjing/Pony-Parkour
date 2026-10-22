using System;
using System.Collections.Generic;
using System.IO;
using Adventure.Player;
using Adventure.Obstacles;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class PlayerDeathSmokeTest
{
    private static PlayerDeath death;
    private static PlayerRespawn respawn;
    private static bool respawnWasEnabled;
    private static PlayerInputReader input;
    private static ThirdPersonMotor motor;
    private static CharacterController controller;
    private static Rigidbody corpse;
    private static InflatingBounceBall ball;
    private static Keyboard keyboard;
    private static Mouse mouse;
    private static bool stationary, placed, oldBackground;
    private static float began, diedAt, maximumTilt, maximumIdleInput;
    private static Vector3 livingStart, deadRoot;
    private static Quaternion cameraRotation;
    private static readonly List<string> results = new List<string>();
    public static string Status { get; private set; } = "Not run";

    [MenuItem("Tools/Adventure/Test Death - Walk Into Ball (Play Mode)")]
    public static void RunWalking() { Run(false); }
    [MenuItem("Tools/Adventure/Test Death - Ball Hits Idle Player (Play Mode)")]
    public static void RunStationary() { Run(true); }
    public static void Run(bool idle)
    {
        if (!EditorApplication.isPlaying || Status == "Running") throw new InvalidOperationException("Enter a fresh Play session first.");
        death = UnityEngine.Object.FindFirstObjectByType<PlayerDeath>();
        if (death == null || death.IsDead) throw new InvalidOperationException("A living player is required; restart Play Mode.");
        respawn = death.GetComponent<PlayerRespawn>();
        respawnWasEnabled = respawn != null && respawn.enabled;
        if (respawn != null) respawn.enabled = false;
        ball = UnityEngine.Object.FindFirstObjectByType<InflatingBounceBall>();
        input = death.GetComponent<PlayerInputReader>(); motor = death.GetComponent<ThirdPersonMotor>();
        controller = death.GetComponent<CharacterController>(); corpse = death.transform.Find("Visuals").GetComponent<Rigidbody>();
        stationary = idle; placed = !idle; diedAt = -1f; maximumTilt = 0f; maximumIdleInput = 0f; results.Clear();
        oldBackground = Application.runInBackground; Application.runInBackground = true;
        keyboard = InputSystem.AddDevice<Keyboard>("DeathTestKeyboard");
        mouse = InputSystem.AddDevice<Mouse>("DeathTestMouse");
        Vector3 position = ball.transform.position;
        position.y = ball.GroundY + 0.05f;
        position.z -= idle ? 4f : 3f;
        PlacePlayer(position); livingStart = position;
        if (!idle)
        {
            ball.enabled = false;
            var ballBody = ball.GetComponent<Rigidbody>();
            var p = ballBody.position; p.y = ball.GroundY + 1.2f; ballBody.position = p;
        }
        began = Time.time; Status = "Running";
        InputSystem.onBeforeUpdate += Feed;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += PlayChanged;
    }
    private static void PlacePlayer(Vector3 position)
    {
        controller.enabled = false; death.transform.position = position; controller.enabled = true;
        Physics.SyncTransforms();
    }
    private static void Feed()
    {
        if (InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(death.IsDead ? new[] { Key.W, Key.A, Key.Space } : stationary ? Array.Empty<Key>() : new[] { Key.W }));
        var state = new MouseState();
        if (death.IsDead) { state.delta = new Vector2(50f, 25f); state = state.WithButton(MouseButton.Left); }
        InputSystem.QueueStateEvent(mouse, state);
    }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying || death == null) { Finish("Interrupted"); return; }
        if (stationary && !placed && ball.GetComponent<Rigidbody>().position.y > ball.BouncePlaneY + 2f)
        {
            var p = ball.transform.position; p.y = ball.GroundY + 0.05f;
            PlacePlayer(p); livingStart = p; placed = true;
        }
        if (!death.IsDead)
        {
            if (stationary) maximumIdleInput = Mathf.Max(maximumIdleInput, input.Move.magnitude);
            if (Time.time - began > 8f) { Check(false, "Contact must kill player"); Finish("FAILED"); }
            return;
        }
        if (diedAt < 0f)
        {
            diedAt = Time.time; deadRoot = death.transform.position; cameraRotation = Camera.main.transform.rotation;
            Check(placed, stationary ? "Descending ball killed idle player" : "Walking into ball killed player");
            // Kinematic contact can depenetrate the CharacterController. Verify input, not contact displacement.
            Check(stationary ? maximumIdleInput < 0.001f : Vector3.Distance(deadRoot, livingStart) > 0.5f,
                stationary ? "No movement input needed; maximum input magnitude " + maximumIdleInput.ToString("F4") : "Actual movement preceded contact");
            Check(!input.enabled && !motor.enabled && !controller.enabled, "Input reader, motor and CharacterController disabled");
            Check(!corpse.isKinematic && corpse.useGravity && corpse.GetComponent<CapsuleCollider>().enabled, "Physical ragdoll and collider activated");
            var before = corpse.linearVelocity;
            death.RequestDeath(ball.transform.position);
            Check(corpse.linearVelocity == before, "Repeated lethal contact does not apply another impulse");
        }
        maximumTilt = Mathf.Max(maximumTilt, Vector3.Angle(corpse.transform.up, Vector3.up));
        if (Time.time - diedAt < 4f) return;
        Check(input.Move == Vector2.zero && input.Look == Vector2.zero && !input.JumpPressed, "WASD, jump and mouse events ignored after death");
        Check(Vector3.Distance(deadRoot, death.transform.position) < 0.001f, "Disabled motor does not move player root");
        Check(Cursor.lockState == CursorLockMode.None && Cursor.visible, "Cursor released; clicking does not relock");
        Check(Quaternion.Angle(cameraRotation, Camera.main.transform.rotation) < 0.5f, "Mouse does not rotate death camera");
        Check(maximumTilt > 50f, "Capsule tumbles; measured maximum tilt " + maximumTilt.ToString("F1") + " degrees");
        Check(corpse.GetComponent<CapsuleCollider>().bounds.min.y > ball.GroundY - 0.1f, "Corpse remains above floor");
        Finish(results.Exists(x => x.StartsWith("FAIL")) ? "FAILED" : "PASSED");
    }
    private static void Check(bool pass, string message) { results.Add((pass ? "PASS: " : "FAIL: ") + message); }
    private static void PlayChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode && Status == "Running") Finish("Interrupted");
    }
    private static void Finish(string status)
    {
        InputSystem.onBeforeUpdate -= Feed; EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= PlayChanged;
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
        keyboard = null; mouse = null; Application.runInBackground = oldBackground;
        if (respawn != null) respawn.enabled = respawnWasEnabled;
        Status = status;
        string report = "Player death " + (stationary ? "stationary" : "walking") + ": " + status + "\n" + string.Join("\n", results);
        Directory.CreateDirectory("Docs"); File.WriteAllText("Docs/PLAYER_DEATH_" + (stationary ? "IDLE" : "WALK") + "_VALIDATION.txt", report);
        Debug.Log(report);
    }
}
