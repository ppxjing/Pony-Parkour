using System;
using System.Collections.Generic;
using System.IO;
using Adventure.Obstacles;
using UnityEditor;
using UnityEngine;

public static class BounceBallSmokeTest
{
    private static InflatingBounceBall ball;
    private static Rigidbody body;
    private static SphereCollider sphere;
    private static readonly List<string> results = new List<string>();
    private static readonly List<float> peaks = new List<float>();
    private static float oldStep, oldHeight, oldGravity, oldInflation, oldRecovery, oldHold;
    private static bool oldBackground;
    private static int phase, previousBounce;
    private static float lastFixed, lastVelocity, peak, minGap, minScale, maxScale, maxGravityError;
    private static float expectedHeight, expectedGravity, phaseStart;
    private static bool descentInflation, ascentRecovery;
    public static string Status { get; private set; } = "Not run";

    [MenuItem("Tools/Adventure/Run Bounce Ball Smoke Test (Play Mode)")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (Status == "Running") throw new InvalidOperationException("Test already running.");
        ball = UnityEngine.Object.FindFirstObjectByType<InflatingBounceBall>();
        if (ball == null || !ball.enabled) throw new InvalidOperationException("Active bounce ball not found.");
        body = ball.GetComponent<Rigidbody>(); sphere = ball.GetComponent<SphereCollider>();
        var so = new SerializedObject(ball);
        oldHeight = so.FindProperty("bounceHeight").floatValue;
        oldGravity = so.FindProperty("gravity").floatValue;
        oldInflation = so.FindProperty("inflatedScale").floatValue;
        oldRecovery = so.FindProperty("recoveryTime").floatValue;
        oldHold = so.FindProperty("inflatedHoldTime").floatValue;
        oldStep = Time.fixedDeltaTime; oldBackground = Application.runInBackground;
        Application.runInBackground = true;
        results.Clear(); phase = 0; Status = "Running";
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += PlayChanged;
        StartPhase();
    }

    private static void StartPhase()
    {
        expectedHeight = phase == 2 ? 2f : 3f;
        expectedGravity = phase == 2 ? 20f : 9.81f;
        Time.fixedDeltaTime = phase == 0 ? 0.02f : phase == 1 ? 0.05f : 0.01f;
        var so = new SerializedObject(ball);
        so.FindProperty("bounceHeight").floatValue = expectedHeight;
        so.FindProperty("gravity").floatValue = expectedGravity;
        so.FindProperty("inflatedScale").floatValue = 1.5f;
        so.FindProperty("recoveryTime").floatValue = 0.25f;
        so.FindProperty("inflatedHoldTime").floatValue = 0.08f;
        so.ApplyModifiedPropertiesWithoutUndo();
        ball.RestartMotion();
        peaks.Clear(); previousBounce = 0; lastFixed = -1f; peak = float.NegativeInfinity;
        minGap = float.PositiveInfinity; minScale = float.PositiveInfinity; maxScale = 0f;
        maxGravityError = 0f; descentInflation = false; ascentRecovery = false; phaseStart = Time.time;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || ball == null) { Finish("Interrupted"); return; }
        if (Time.time - phaseStart > 30f) { results.Add("FAIL: phase timed out."); Finish("FAILED"); return; }
        float now = Time.fixedTime;
        if (now == lastFixed) return;
        float y = body.position.y;
        minGap = Mathf.Min(minGap, y - sphere.radius * ball.transform.lossyScale.x - ball.GroundY);
        minScale = Mathf.Min(minScale, ball.CurrentScale); maxScale = Mathf.Max(maxScale, ball.CurrentScale);
        if (ball.VerticalVelocity < 0f && ball.CurrentScale > 1.2f) descentInflation = true;
        if (ball.VerticalVelocity > 0f && ball.CurrentScale < 1.01f && ball.BounceCount > 0) ascentRecovery = true;
        if (lastFixed >= 0f && ball.BounceCount == previousBounce)
            maxGravityError = Mathf.Max(maxGravityError, Mathf.Abs((ball.VerticalVelocity - lastVelocity) / (now - lastFixed) + expectedGravity));
        if (ball.BounceCount != previousBounce)
        {
            if (previousBounce > 0) peaks.Add(peak - ball.BouncePlaneY);
            previousBounce = ball.BounceCount;
            peak = y;
        }
        else peak = Mathf.Max(peak, y);
        lastFixed = now; lastVelocity = ball.VerticalVelocity;
        if (peaks.Count < 3) return;
        float lo = Mathf.Min(peaks.ToArray()), hi = Mathf.Max(peaks.ToArray());
        Check(lo > expectedHeight - 0.06f && hi < expectedHeight + 0.02f && hi - lo < 0.04f,
            "3 measured apex rises: " + string.Join(", ", peaks.ConvertAll(x => x.ToString("F4"))) + " m; target " + expectedHeight + " m");
        Check(maxGravityError < 0.05f, "gravity acceleration maximum error " + maxGravityError.ToString("F4") + " m/s²");
        Check(minGap >= 0.029f, "minimum actual sphere/floor gap " + minGap.ToString("F4") + " m");
        Check(descentInflation && maxScale >= 1.49f, "inflates during descent; max scale " + maxScale.ToString("F3"));
        Check(ascentRecovery && minScale < 1.01f, "returns to normal size during ascent");
        phase++;
        if (phase < 3) StartPhase(); else Finish(results.Exists(x => x.StartsWith("FAIL")) ? "FAILED" : "PASSED");
    }

    private static void Check(bool passed, string message)
    {
        results.Add((passed ? "PASS" : "FAIL") + " [dt=" + Time.fixedDeltaTime + ", g=" + expectedGravity + "] " + message);
    }

    private static void PlayChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode && Status == "Running") Finish("Interrupted");
    }

    private static void Finish(string status)
    {
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= PlayChanged;
        Time.fixedDeltaTime = oldStep; Application.runInBackground = oldBackground;
        if (ball != null)
        {
            var so = new SerializedObject(ball);
            so.FindProperty("bounceHeight").floatValue = oldHeight;
            so.FindProperty("gravity").floatValue = oldGravity;
            so.FindProperty("inflatedScale").floatValue = oldInflation;
            so.FindProperty("recoveryTime").floatValue = oldRecovery;
            so.FindProperty("inflatedHoldTime").floatValue = oldHold;
            so.ApplyModifiedPropertiesWithoutUndo();
            ball.RestartMotion();
        }
        Status = status;
        string report = "Bounce ball runtime validation: " + status + "\n" + string.Join("\n", results);
        Directory.CreateDirectory("Docs");
        File.WriteAllText("Docs/BOUNCE_BALL_VALIDATION.txt", report);
        Debug.Log(report);
    }
}
