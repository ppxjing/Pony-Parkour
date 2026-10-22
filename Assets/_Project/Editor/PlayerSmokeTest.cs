using System;
using System.Collections.Generic;
using System.IO;
using Adventure.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

// Editor-only integration checks: feed real Input System events through the runtime actions.
public static class PlayerSmokeTest
{
    private static Keyboard keyboard;
    private static Mouse mouse;
    private static ThirdPersonMotor motor;
    private static Transform player, camera;
    private static readonly List<string> results = new List<string>();
    private static Key[] keys = Array.Empty<Key>();
    private static float began, peak;
    private static Vector3 start, cameraStart;
    private static int phase;
    private static bool oldRunInBackground;
    private static GameObject obstruction;
    private static float straightDistance;
    private static Quaternion cameraRotation;
    private static bool pulseSent;
    private static float firstTurn;
    public static string Status { get; private set; } = "Not run";

    [MenuItem("Tools/Adventure/Run Player Smoke Test (Play Mode)")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode in Kitchen_Playground first.");
        if (keyboard != null) throw new InvalidOperationException("Test already running.");
        motor = UnityEngine.Object.FindFirstObjectByType<ThirdPersonMotor>();
        if (motor == null) throw new InvalidOperationException("Player not found.");
        player = motor.transform; camera = Camera.main.transform;
        oldRunInBackground = Application.runInBackground; Application.runInBackground = true;
        keyboard = InputSystem.AddDevice<Keyboard>("PlayerSmokeTestKeyboard");
        mouse = InputSystem.AddDevice<Mouse>("PlayerSmokeTestMouse");
        results.Clear(); phase = -1; Status = "Running";
        InputSystem.onBeforeUpdate += FeedInput;
        EditorApplication.update += Tick;
        Next();
    }
    private static void FeedInput()
    {
        if (keyboard != null && keyboard.added && InputState.currentUpdateType == InputUpdateType.Dynamic)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            if (mouse != null)
            {
                var state = new MouseState();
                if (phase == 0 || phase == 18) state = state.WithButton(MouseButton.Left);
                if ((phase == 15 || phase == 19) && !pulseSent)
                { state.delta = new Vector2(160, 0); pulseSent = true; }
                if (phase == 17) state.delta = new Vector2(3, 0);
                InputSystem.QueueStateEvent(mouse, state);
            }
        }
    }
    private static void Next()
    {
        phase++; began = Time.time; keys = Array.Empty<Key>();
        if (phase % 2 == 0 && phase <= 10) motor.Respawn();
        start = player.position; cameraStart = camera.position; peak = start.y;
        if (phase == 1) keys = new[] { Key.W };
        if (phase == 3) keys = new[] { Key.S };
        if (phase == 5) keys = new[] { Key.A };
        if (phase == 7) keys = new[] { Key.D };
        if (phase == 9) keys = new[] { Key.W, Key.D };
        if (phase == 11) keys = new[] { Key.Space };
        if (phase == 12)
        {
            obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstruction.name = "SmokeTest_TemporaryCameraObstacle";
            var target = player.Find("CameraTarget");
            obstruction.transform.position = target.position - camera.forward * 2.5f;
            obstruction.transform.localScale = new Vector3(2, 2, 0.25f);
            Physics.SyncTransforms();
        }
        if (phase == 13)
        {
            motor.Respawn();
            obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstruction.name = "SmokeTest_TemporaryWall";
            obstruction.transform.position = player.position + new Vector3(0, 1, 2);
            obstruction.transform.localScale = new Vector3(3, 2, 0.2f);
            Physics.SyncTransforms();
        }
        if (phase == 14) keys = new[] {Key.W};
        if (phase >= 15) { cameraRotation = camera.rotation; pulseSent = false; firstTurn = float.NaN; }
        if (phase == 16) keys = new[] { Key.Escape };
    }
    private static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying || player == null) { Finish("Cancelled"); return; }
            float elapsed = Time.time - began;
            peak = Mathf.Max(peak, player.position.y);
            if (phase <= 10 && phase % 2 == 0) { if (elapsed >= 0.5f) Next(); return; }
            if (phase <= 9)
            {
                if (elapsed < 0.6f) return;
                Vector3 d = Vector3.ProjectOnPlane(player.position - start, Vector3.up);
                Vector3 expected = phase == 1 ? Vector3.forward : phase == 3 ? Vector3.back : phase == 5 ? Vector3.left : phase == 7 ? Vector3.right : new Vector3(1,0,1).normalized;
                Check(Vector3.Dot(d.normalized, expected) > 0.9f && d.magnitude > 1.5f, "Movement " + string.Join("+", keys) + " distance=" + d.magnitude.ToString("F2"));
                if (phase == 1)
                {
                    straightDistance = d.magnitude;
                    Check(Vector3.Distance(camera.position, cameraStart) > 1f, "Camera follows player");
                }
                if (phase == 9) Check(d.magnitude <= straightDistance * 1.15f, "Diagonal speed is normalized");
                Next(); return;
            }
            if (phase == 11)
            {
                // Release and press again in the air: must not create a second jump.
                keys = elapsed < 0.08f || (elapsed > 0.3f && elapsed < 0.38f) ? new[] { Key.Space } : Array.Empty<Key>();
                if (elapsed < 1.8f) return;
                float rise = peak - start.y;
                Check(rise > 0.8f && rise < 1.65f, "Space jump and no double jump: peak=" + rise.ToString("F2"));
                Check(motor.IsGrounded && Mathf.Abs(player.position.y - start.y) < 0.15f, "Lands on kitchen floor");
                Next(); return;
            }
            if (phase == 12 && elapsed > 0.5f)
            {
                Check(Vector3.Distance(camera.position, player.Find("CameraTarget").position) < 2.5f, "Camera shortens distance at obstruction");
                UnityEngine.Object.DestroyImmediate(obstruction); obstruction = null;
                Next(); return;
            }
            if (phase == 13 && elapsed > 0.5f) { Next(); return; }
            if (phase == 14 && elapsed > 0.8f)
            {
                float travel = player.position.z - start.z;
                Check(travel > 0.5f && travel < 1.8f, "CharacterController blocks movement through wall: distance=" + travel.ToString("F2"));
                UnityEngine.Object.DestroyImmediate(obstruction); obstruction = null;
                motor.Respawn(); Next(); return;
            }
            if (phase == 15 || phase == 19)
            {
                float angle = Quaternion.Angle(cameraRotation, camera.rotation);
                if (float.IsNaN(firstTurn) && angle > 0.01f) firstTurn = angle;
                if (elapsed <= 0.6f) return;
                Check(angle > 15f && angle < 20f, phase == 15 ? "Mouse delta alone orbits camera (no right button)" : "Click recaptures cursor and restores mouse look");
                Check(firstTurn > 0 && firstTurn < 19f, "Rotation interpolates instead of snapping: first angle=" + firstTurn.ToString("F2"));
                if (phase == 15) { Next(); return; }
                Finish(results.Exists(s => s.StartsWith("FAIL")) ? "FAILED" : "PASSED");
            }
            if (phase == 16 && elapsed > 0.5f) { Check(Cursor.lockState == CursorLockMode.None && Cursor.visible, "Escape releases cursor"); Next(); return; }
            if (phase == 17 && elapsed > 0.5f) { Check(Quaternion.Angle(cameraRotation, camera.rotation) < 0.2f, "Released cursor does not rotate camera"); Next(); return; }
            if (phase == 18 && elapsed > 0.2f) { Check(Cursor.lockState == CursorLockMode.Locked, "Left click locks cursor"); Next(); return; }
        }
        catch (Exception ex) { results.Add("FAIL " + ex); Finish("FAILED"); }
    }
    private static void Check(bool ok, string label) { results.Add((ok ? "PASS " : "FAIL ") + label); }
    private static void Finish(string status)
    {
        InputSystem.onBeforeUpdate -= FeedInput;
        EditorApplication.update -= Tick;
        if (keyboard != null) { InputSystem.RemoveDevice(keyboard); keyboard = null; }
        if (mouse != null) { InputSystem.RemoveDevice(mouse); mouse = null; }
        if (obstruction != null) UnityEngine.Object.DestroyImmediate(obstruction);
        Application.runInBackground = oldRunInBackground;
        keys = Array.Empty<Key>(); Status = status;
        string report = "Player smoke test: " + status + "\nUnity " + Application.unityVersion + "\n" + string.Join("\n", results);
        Directory.CreateDirectory("Docs"); File.WriteAllText("Docs/PLAYER_VALIDATION.txt", report);
        Debug.Log(report);
    }
}
